using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
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

        // 💡 Constante para el tiempo de expiración (60 segundos antes de caducar)
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

                    // 💡 IMPLEMENTACIÓN CLAVE: Validar y renovar la sesión en cada petición.
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var accessTokenExpiration = context.Properties.GetTokenValue("expires_at");

                        // 1. Verificar si el token está a punto de caducar (o ya caducó)
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
                            // 2. Si es necesario, renovar el token y la cookie
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
                                        "OIDC PROTOCOLO CORREGIDO: URL modificada de {Original} a {Corregida}",
                                        originalUri,
                                        context.ProtocolMessage.RedirectUri
                                    );
                                }
                                _logger.LogWarning(
                                    "OIDC DIAGNÓSTICO: URL de Autorización Completa Enviada: {Url}",
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
                                        "OIDC VALIDADO FALLIDO: No se pudo obtener la identidad del usuario."
                                    );
                                    return Task.CompletedTask;
                                }

                                if (identity.Claims.Any())
                                {
                                    _logger.LogInformation("--- OIDC CLAIMS RECIBIDOS ---");
                                    _logger.LogInformation(
                                        "CLAIMS: {Count} Claims recibidos y procesados.",
                                        identity.Claims.Count()
                                    );
                                    // ... (Logging detallado de claims)
                                    _logger.LogInformation("--- FIN CLAIMS OIDC ---");
                                }
                                return Task.CompletedTask;
                            },

                            OnTicketReceived = context =>
                            {
                                // Lógica de logging de tokens
                                var refreshToken = context.Properties?.GetTokenValue(
                                    "refresh_token"
                                );
                                var principal = context.Principal;

                                if (refreshToken != null && principal != null)
                                {
                                    // 💡 Iniciamos el temporizador como monitor. La renovación real es en OnValidatePrincipal.
                                    double renewalInterval = 300000;
                                    StartTokenRenewal(refreshToken, renewalInterval, principal);
                                }

                                // ... (Logging detallado de tokens)
                                return Task.CompletedTask;
                            },

                            OnRemoteFailure = context =>
                            {
                                // ... (Lógica de error remoto)
                                return Task.CompletedTask;
                            },

                            OnRedirectToIdentityProviderForSignOut = context =>
                            {
                                var postLogoutUri = _options.PostLogoutRedirectUri;

                                context.ProtocolMessage.PostLogoutRedirectUri = postLogoutUri;

                                _logger.LogWarning(
                                    "OIDC LOGOUT: Redirigiendo a Identity Provider para cerrar sesión. URL de retorno: {Uri}",
                                    postLogoutUri
                                );

                                return Task.CompletedTask;
                            },
                        };
                    }
                );
        }

        // 💡 Implementación del método de actualización de tokens y cookie (se ejecuta en cada petición)
        private async Task PerformTokenRefreshAndCookieUpdate(
            CookieValidatePrincipalContext context
        )
        {
            var refreshToken = context.Properties.GetTokenValue("refresh_token");

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning(
                    "COOKIE RECHAZADA: No se encontró Refresh Token. Forzando reautenticación."
                );
                context.RejectPrincipal();
                return;
            }

            _logger.LogWarning(
                "INICIANDO RENOVACIÓN: Refresh Token encontrado. Llamando al Token Endpoint."
            );

            var tokenRequest = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _options.ClientId },
                    // 🚨 Nota: Si el servidor de Abena requiere ClientSecret (es común), debe ir aquí:
                    // { "client_secret", "TU_SECRETO_AQUÍ" },
                    { "refresh_token", refreshToken },
                }
            );

            // Endpoint: Generalmente es el Issuer URL + /token
            var tokenEndpoint = _options.Issuer + "/token-srv/token";
            var response = await _httpClient.PostAsync(tokenEndpoint, tokenRequest);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(jsonContent).RootElement;

                // 1. Obtener nuevos tokens
                var newAccessToken = tokenData.GetProperty("access_token").GetString();
                var newRefreshToken = tokenData.GetProperty("refresh_token").GetString(); // Nuevo Refresh Token
                var expiresIn = tokenData.GetProperty("expires_in").GetInt32();

                // 2. Actualizar las propiedades de autenticación
                var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToString("o");

                context.Properties.UpdateTokenValue("access_token", newAccessToken!);
                context.Properties.UpdateTokenValue("refresh_token", newRefreshToken!);
                context.Properties.UpdateTokenValue("expires_at", newExpiresAt);

                // 3. Renovar la cookie
                context.ShouldRenew = true; // Indica al middleware que debe reescribir la cookie
                _logger.LogInformation(
                    "RENNOVACIÓN EXITOSA: La cookie de sesión será actualizada con nuevos tokens."
                );
            }
            else
            {
                // El Refresh Token ha caducado o ha sido revocado.
                _logger.LogError(
                    "ERROR DE RENOVACIÓN ({StatusCode}): Refresh Token ya no es válido. Forzando Logout.",
                    response.StatusCode
                );
                context.RejectPrincipal(); // Obliga al usuario a iniciar sesión de nuevo
            }
        }

        // 💡 El temporizador ahora solo sirve como un monitor (su lógica de renovación fue simplificada)
        private async Task RenewTokenLogicAsync(
            string currentRefreshToken,
            ClaimsPrincipal principal
        )
        {
            // Este método se ejecuta en el temporizador, no puede actualizar la cookie.
            _logger.LogWarning(
                "Token monitor: Verificando la sesión. La renovación real se hará en OnValidatePrincipal en la próxima petición."
            );
            await Task.CompletedTask;
        }

        // ... (StartTokenRenewal, StopTokenRenewal, LoginAsync, y LogoutAsync se mantienen como están)

        // El resto de la clase se mantiene igual

        /// <summary>
        /// Inicia el proceso periódico para renovar el Access Token.
        /// </summary>
        /// <param name="refreshToken">El Refresh Token actual para la renovación.</param>
        /// <param name="intervalMilliseconds">Intervalo en milisegundos para ejecutar la renovación.</param>
        /// <param name="userIdentity">La identidad del usuario (ClaimsPrincipal).</param>
        public void StartTokenRenewal(
            string refreshToken,
            double intervalMilliseconds,
            ClaimsPrincipal userIdentity
        )
        {
            StopTokenRenewal();

            _renewalTimer = new System.Timers.Timer(intervalMilliseconds);

            // Usamos Elapsed para el evento que dispara la lógica de renovación.
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

        /// <summary>
        /// Detiene y libera el temporizador de renovación.
        /// </summary>
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
            StopTokenRenewal(); // Detener el temporizador al cerrar sesión
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = _options.PostLogoutRedirectUri }
            );
        }
    }
}
