using cidaas_net_sdk.core.Models;

namespace cidaas_net_sdk.core
{
    public static class AhamaticAuthServiceExtensions
    {
        public static string GetAhamaticToken(this AhamaticFullValidationResponse response)
        {
            return response.Token;
        }

        public static string GetAhamaticRefreshToken(this AhamaticFullValidationResponse response)
        {
            return response.RefreshToken;
        }

        public static string? GetAccountFirstName(this AhamaticFullValidationResponse response)
        {
            return response.Account?.FirstName;
        }

        public static string? GetAccountEmail(this AhamaticFullValidationResponse response)
        {
            return response.Account?.EmailAddress;
        }

        public static string GetAccountPersonId(this AhamaticFullValidationResponse response)
        {
            return response.Account?.PersonId.ToString() ?? string.Empty;
        }
    }
}
