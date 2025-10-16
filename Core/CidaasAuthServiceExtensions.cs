using System;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace cidaas_net_sdk.core
{
    public static class CidaasAuthServiceExtensions
    {
        public static string? GetClaimValue(
            this CidaasAuthService service,
            HttpContext context,
            string claimType
        )
        {
            return context.User.FindFirst(claimType)?.Value;
        }

        public static async Task<string?> GetTokenAsync(
            this CidaasAuthService service,
            HttpContext context,
            string tokenName
        )
        {
            return await context.GetTokenAsync(tokenName);
        }

        public static Task<string?> GetAccessTokenAsync(
            this CidaasAuthService service,
            HttpContext context
        )
        {
            return service.GetTokenAsync(context, "access_token");
        }

        public static Task<string?> GetRefreshTokenAsync(
            this CidaasAuthService service,
            HttpContext context
        )
        {
            return service.GetTokenAsync(context, "refresh_token");
        }

        public static Task<string?> GetIdTokenAsync(
            this CidaasAuthService service,
            HttpContext context
        )
        {
            return service.GetTokenAsync(context, "id_token");
        }

        public static async Task<Dictionary<string, string>> GetCidaasUserInfoAsync(
            this CidaasAuthService service,
            string access_token
        )
        {
            using var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access_token);

            string userInfoEndpoint = service.Options.Issuer + "/users-srv/userinfo";

            var response = await httpClient.GetAsync(userInfoEndpoint);

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();

                using var document = System.Text.Json.JsonDocument.Parse(jsonContent);

                var claimsDictionary = document
                    .RootElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => p.Value.ToString() ?? string.Empty);

                return claimsDictionary;
            }
            else
            {
                service.Logger.LogError(
                    "UserInfo failed with status {StatusCode}. Token might be expired.",
                    response.StatusCode
                );
                return [];
            }
        }

        public static string? GetUserInfoClaim(
            this Dictionary<string, string> userInfo,
            string claimKey
        )
        {
            if (userInfo.TryGetValue(claimKey, out string? value))
            {
                return value;
            }

            return null;
        }

        public static string? GetUserInfoName(this Dictionary<string, string> userInfo)
        {
            return userInfo.GetUserInfoClaim("name");
        }

        public static string? GetUserInfoStatus(this Dictionary<string, string> userInfo)
        {
            return userInfo.GetUserInfoClaim("user_status");
        }

        public static string? GetUserInfoLastAccessed(this Dictionary<string, string> userInfo)
        {
            return userInfo.GetUserInfoClaim("last_accessed_at");
        }
    }
}
