using System;
using System.Linq;

namespace cidaas_net_sdk.options
{
    public class AhamaticOptions
    {
        public string Environment { get; set; } = string.Empty;
        public bool Europe { get; set; } = false;
        public string ApplicationCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
    }

    public class CidaasOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string PostLogoutRedirectUri { get; set; } = string.Empty;
        public string DiscoveryUrl { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = Array.Empty<string>();
        public AhamaticOptions Ahamatic { get; set; } = new AhamaticOptions();

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

            if (string.IsNullOrWhiteSpace(Ahamatic.Environment))
            {
                throw new ArgumentException(
                    "Ahamatic Environment es requerido y no puede ser nulo o contener solo espacios."
                );
            }

            if (string.IsNullOrWhiteSpace(Ahamatic.ApplicationCode))
            {
                throw new ArgumentException(
                    "Ahamatic ApplicationCode es requerido y no puede ser nulo o contener solo espacios."
                );
            }

            if (string.IsNullOrWhiteSpace(Ahamatic.ModuleName))
            {
                throw new ArgumentException(
                    "Ahamatic ModuleName es requerido y no puede ser nulo o contener solo espacios."
                );
            }

            if (string.IsNullOrWhiteSpace(Ahamatic.ApiBaseUrl))
            {
                throw new ArgumentException(
                    "Ahamatic ApiBaseUrl es requerido y no puede ser nulo o contener solo espacios."
                );
            }
        }
    }
}
