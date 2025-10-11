using System;
using System.Linq;

namespace cidaas_net_sdk.options
{
    public class CidaasOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string PostLogoutRedirectUri { get; set; } = string.Empty;
        public string DiscoveryUrl { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = Array.Empty<string>();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Issuer))
            {
                throw new ArgumentException("Issuer no puede ser nulo o vacío");
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new ArgumentException("ClientId no puede ser nulo o vacío");
            }

            if (string.IsNullOrWhiteSpace(RedirectUri))
            {
                throw new ArgumentException("RedirectUri no puede ser nulo o vacío");
            }

            if (string.IsNullOrWhiteSpace(PostLogoutRedirectUri))
            {
                throw new ArgumentException("PostLogoutRedirectUri no puede ser nulo o vacío");
            }

            if (string.IsNullOrWhiteSpace(DiscoveryUrl))
            {
                throw new ArgumentException("DiscoveryUrl no puede ser nulo o vacío");
            }

            if (Scopes == null || !Scopes.Any())
            {
                throw new ArgumentException(
                    "Scopes es requerido y debe contener al menor un elemento.",
                    nameof(Scopes)
                );
            }

            if (Scopes.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                throw new ArgumentException(
                    "Todos los scopes definidos deben ser cadenas válidas (no nulas ni vacías).",
                    nameof(Scopes)
                );
            }

            if (!Scopes.Any(s => s.Equals("openid", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Advertencia: Se recomienda incluir el scope 'openid'.");
            }
        }
    }
}
