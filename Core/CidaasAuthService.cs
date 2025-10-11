using cidaas_net_sdk.options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace cidaas_net_sdk.core
{
    public class CidaasAuthService
    {
        private readonly CidaasOptions _options;

        public CidaasAuthService(CidaasOptions options)
        {
            _options = options;
        }

        public void ConfigureAuth(WebApplicationBuilder builder)
        {
            builder
                .Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    options.DefaultAuthenticateScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie()
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

                        oidcOptions.CallbackPath = "/cidaas/callback";
                        oidcOptions.SignedOutRedirectUri = _options.PostLogoutRedirectUri;
                    }
                );
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
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = _options.PostLogoutRedirectUri }
            );
        }
    }
}
