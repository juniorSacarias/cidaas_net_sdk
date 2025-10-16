using System.Security.Claims;
using System.Text.Json;
using cidaas_net_sdk.options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace cidaas_net_sdk.core
{
    public class CidaasAuthService
    {
        private readonly CidaasOptions _options;
        private readonly ILogger<CidaasAuthService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient();

        private System.Timers.Timer? _renewalTimer;

        private const int TokenExpirationBufferSeconds = 60;

        public CidaasOptions Options => _options;

        public ILogger<CidaasAuthService> Logger => _logger;

        public CidaasAuthService(CidaasOptions options, ILogger<CidaasAuthService> logger)
        {
            _options = options;
            _logger = logger;
        }

        public void ConfigureAuth(WebApplicationBuilder builder)
        {
            _ = builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "Cidaas.Auth.Session";

                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var accessTokenExpiration = context.Properties.GetTokenValue("expires_at");

                        if (
                            string.IsNullOrEmpty(accessTokenExpiration)
                            || (
                                DateTimeOffset
                                    .Parse(accessTokenExpiration)
                                    .AddSeconds(-TokenExpirationBufferSeconds)
                                < DateTimeOffset.UtcNow
                            )
                        )
                        {
                            await PerformTokenRefreshAndCookieUpdate(context);
                        }
                    };
                })
                .AddOpenIdConnect(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    oidcOptions =>
                    {
                        oidcOptions.Authority = _options.Issuer;
                        oidcOptions.ClientId = _options.ClientId;
                        oidcOptions.ResponseType = "code";
                        oidcOptions.SaveTokens = true;
                        oidcOptions.GetClaimsFromUserInfoEndpoint = true;

                        oidcOptions.Scope.Clear();
                        foreach (var scope in _options.Scopes)
                            oidcOptions.Scope.Add(scope);

                        oidcOptions.MapInboundClaims = false;
                        oidcOptions.CallbackPath = "/cidaas/callback";
                        oidcOptions.SignedOutRedirectUri = _options.PostLogoutRedirectUri;

                        oidcOptions.Events = new OpenIdConnectEvents
                        {
                            OnRedirectToIdentityProvider = context =>
                            {
                                string originalUri = context.ProtocolMessage.RedirectUri;
                                if (originalUri.StartsWith("http://"))
                                {
                                    context.ProtocolMessage.RedirectUri = originalUri.Replace(
                                        "http:",
                                        "https:"
                                    );
                                }
                                if (originalUri != context.ProtocolMessage.RedirectUri)
                                {
                                    _logger.LogWarning(
                                        "OIDC PROTOCOL CORRECTED: URL changed from {Original} to {Corrected}",
                                        originalUri,
                                        context.ProtocolMessage.RedirectUri
                                    );
                                }
                                _logger.LogWarning(
                                    "OIDC DIAGNOSIS: Full Authorization URL Sent: {Url}",
                                    context.ProtocolMessage.CreateAuthenticationRequestUrl()
                                );
                                return Task.CompletedTask;
                            },

                            OnTokenValidated = context =>
                            {
                                var identity = (ClaimsIdentity?)context.Principal?.Identity;

                                if (identity == null)
                                {
                                    _logger.LogError(
                                        "OIDC VALIDATED FAILED: The user's identity could not be obtained."
                                    );
                                    return Task.CompletedTask;
                                }

                                if (identity.Claims.Any())
                                {
                                    _logger.LogInformation("--- OIDC CLAIMS RECEIVED ---");

                                    _logger.LogInformation(
                                        "CLAIMS: {Count} Claims received and processed.",
                                        identity.Claims.Count()
                                    );

                                    _logger.LogInformation("--- END CLAIMS OIDC ---");
                                }
                                return Task.CompletedTask;
                            },

                            OnTicketReceived = context =>
                            {
                                var refreshToken = context.Properties?.GetTokenValue(
                                    "refresh_token"
                                );
                                var principal = context.Principal;

                                if (refreshToken != null && principal != null)
                                {
                                    double renewalInterval = 300000;
                                    StartTokenRenewal(refreshToken, renewalInterval, principal);
                                }

                                return Task.CompletedTask;
                            },

                            OnRemoteFailure = context =>
                            {
                                return Task.CompletedTask;
                            },

                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                var postLogoutUri = _options.PostLogoutRedirectUri;

                                context.ProtocolMessage.PostLogoutRedirectUri = postLogoutUri;

                                _logger.LogWarning(
                                    "OIDC LOGOUT: Redirecting to Identity Provider to log out. Return URL: {Uri}",
                                    postLogoutUri
                                );

                                return Task.CompletedTask;
                            },
                        };
                    }
                );
        }

        private async Task PerformTokenRefreshAndCookieUpdate(
            CookieValidatePrincipalContext context
        )
        {
            var refreshToken = context.Properties.GetTokenValue("refresh_token");

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning(
                    "COOKIE REJECTED: No Refresh Token found. Forcing reauthentication."
                );
                context.RejectPrincipal();
                return;
            }

            _logger.LogWarning("RENEWAL STARTING: Refresh Token found. Calling Token Endpoint.");

            var tokenRequest = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _options.ClientId },
                    { "refresh_token", refreshToken },
                }
            );

            var tokenEndpoint = _options.Issuer + "/token-srv/token";
            var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(jsonContent).RootElement;

                var newAccessToken = tokenData.GetProperty("access_token").GetString();
                var newRefreshToken = tokenData.GetProperty("refresh_token").GetString();
                var expiresIn = tokenData.GetProperty("expires_in").GetInt32();

                var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o");

                context.Properties.UpdateTokenValue("access_token", newAccessToken!);
                context.Properties.UpdateTokenValue("refresh_token", newRefreshToken!);
                context.Properties.UpdateTokenValue("expires_at", newExpiresAt);

                context.ShouldRenew = true;
                _logger.LogInformation(
                    "RENEWAL SUCCESSFUL: The session cookie will be updated with new tokens."
                );
            }
            else
            {
                _logger.LogError(
                    "RENEW ERROR ({StatusCode}): Refresh Token is no longer valid. Forcing Logout.",
                    response.StatusCode
                );
                context.RejectPrincipal();
            }
        }

        private async Task RenewTokenLogicAsync(
            string currentRefreshToken,
            ClaimsPrincipal principal
        )
        {
            _logger.LogWarning(
                "Token monitor: Verifying the session. The actual renewal will be done in OnValidatePrincipal on the next request."
            );
            await Task.CompletedTask;
        }

        public void StartTokenRenewal(
            string refreshToken,
            double intervalMilliseconds,
            ClaimsPrincipal userIdentity
        )
        {
            StopTokenRenewal();

            _renewalTimer = new System.Timers.Timer(intervalMilliseconds);

            _renewalTimer.Elapsed += async (sender, e) =>
            {
                await RenewTokenLogicAsync(refreshToken, userIdentity);
            };

            _renewalTimer.AutoReset = true;
            _renewalTimer.Start();

            _logger.LogInformation(
                "Token renewal timer started. Interval: {Interval} ms.",
                intervalMilliseconds
            );
        }

        public void StopTokenRenewal()
        {
            if (_renewalTimer is not null)
            {
                _renewalTimer.Stop();
                _renewalTimer.Dispose();
                _renewalTimer = null;
                _logger.LogInformation("Token renewal timer stopped.");
            }
        }

        public async Task LoginAsync(HttpContext context)
        {
            await context.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = "/" }
            );
        }

        public async Task LogoutAsync(HttpContext context)
        {
            StopTokenRenewal();
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = _options.PostLogoutRedirectUri }
            );
        }
    }
}
