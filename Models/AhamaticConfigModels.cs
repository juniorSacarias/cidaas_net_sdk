using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cidaas_net_sdk.core.Models
{
    public class PortalAuthentication
    {
        [JsonPropertyName("Email")]
        public bool Email { get; set; }

        [JsonPropertyName("Mobile")]
        public bool Mobile { get; set; }

        [JsonPropertyName("GoogleAuth")]
        public bool GoogleAuth { get; set; }

        [JsonPropertyName("OpenIAmAuth")]
        public bool OpenIAmAuth { get; set; }

        [JsonPropertyName("FacebookAuth")]
        public bool FacebookAuth { get; set; }

        [JsonPropertyName("Cidaas")]
        public bool Cidaas { get; set; }
    }

    public class CidaasModuleConfig
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("redirecUri")]
        public string RedirectUri { get; set; } = string.Empty;
    }

    public class AuthConfigDetail
    {
        [JsonPropertyName("Module")]
        public string Module { get; set; } = string.Empty;

        [JsonPropertyName("HostName")]
        public string HostName { get; set; } = string.Empty;

        [JsonPropertyName("Cidaas")]
        public CidaasModuleConfig? Cidaas { get; set; }

        [JsonPropertyName("Portal Authentication")]
        public PortalAuthentication? PortalAuthentication { get; set; }
    }

    public class ApplicationConfiguration
    {
        [JsonPropertyName("_id")]
        public int Id { get; set; }

        [JsonPropertyName("Key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("Value")]
        public JsonElement Value { get; set; }
    }

    public class ApplicationValidationResponse
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Logo")]
        public string Logo { get; set; } = string.Empty;

        [JsonPropertyName("Database")]
        public string Database { get; set; } = string.Empty;

        [JsonPropertyName("Configurations")]
        public List<ApplicationConfiguration>? Configurations { get; set; }
    }

    public class AhamaticLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
    }

    public class AhamaticAccountInfo
    {
        [JsonPropertyName("PersonId")]
        public int PersonId { get; set; }

        [JsonPropertyName("UserId")]
        public int UserId { get; set; }

        [JsonPropertyName("EmailAddress")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("LastName")]
        public string LastName { get; set; } = string.Empty;
    }

    public class AhamaticFullValidationResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("access_token")]
        public string CidaasAccessToken { get; set; } = string.Empty;

        [JsonPropertyName("account")]
        public AhamaticAccountInfo? Account { get; set; }

        [JsonPropertyName("applicationId")]
        public string ApplicationId { get; set; } = string.Empty;
    }

    public class AhamaticAccountData
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public int PersonId { get; set; }
    }
}
