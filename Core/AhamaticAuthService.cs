using System.Text;
using System.Text.Json;
using cidaas_net_sdk.core.Models;
using cidaas_net_sdk.options;
using Microsoft.Extensions.Logging;

namespace cidaas_net_sdk.core
{
    public class AhamaticAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AhamaticAuthService> _logger;
        private readonly AhamaticOptions _ahamaticOptions;
        private readonly CidaasOptions _cidaasOptions;

        public AhamaticAuthService(
            HttpClient httpClient,
            ILogger<AhamaticAuthService> logger,
            CidaasOptions options
        )
        {
            _httpClient = httpClient;
            _logger = logger;
            _ahamaticOptions = options.Ahamatic;
            _cidaasOptions = options;
        }

        public async Task<AhamaticLoginResponse?> AuthenticateAhamaticAsync(
            string email,
            string password,
            string access_token
        )
        {
            _logger.LogInformation("AHAMATIC FACADE: Initiating full authentication sequence.");

            var moduleConfig = await GetModuleAuthConfigInternal(_ahamaticOptions.ModuleName);

            if (moduleConfig?.Cidaas?.ApiKey == null)
            {
                _logger.LogError(
                    "AUTHENTICATION FAILED: ApiKey not found in module configuration. Cannot proceed with login."
                );
                return null;
            }

            string apiKey = moduleConfig.Cidaas.ApiKey;

            AhamaticLoginResponse? loginAhamaticResult = await LoginAhamaticInternal(
                email,
                password,
                apiKey
            );

            if (loginAhamaticResult == null)
            {
                _logger.LogError(
                    "AUTHENTICATION FAILED: Ahamatic login (Step 2) failed or returned null tokens."
                );

                return null;
            }

            if (string.IsNullOrEmpty(loginAhamaticResult.Token))
            {
                _logger.LogError(
                    "AUTHENTICATION FAILED: Token nulo o vacío. No se puede continuar con la validación Cidaas."
                );

                return null;
            }

            JsonDocument? ahamaticCidaasVallidationResult = await AhamaticCidaasValidationInternal(
                access_token,
                _cidaasOptions.ClientId,
                _cidaasOptions.Issuer,
                apiKey
            );

            return loginAhamaticResult;
        }

        private async Task<AuthConfigDetail?> GetModuleAuthConfigInternal(string moduleName)
        {
            string validationPath = $"api/validate/app?value={_ahamaticOptions.ApplicationCode}";

            _logger.LogInformation("AHAMATIC: Validating app at {Path}", validationPath);

            try
            {
                var response = await _httpClient.GetAsync(validationPath);

                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();

                var appResponse = JsonSerializer.Deserialize<ApplicationValidationResponse>(
                    jsonContent
                );

                if (appResponse?.Configurations == null)
                {
                    _logger.LogWarning(
                        "AHAMATIC: App validation succeeded but no configurations were returned."
                    );
                    return null;
                }

                var authConfigContainer = appResponse.Configurations.FirstOrDefault(c =>
                    c.Key == "AuthConfig"
                );

                if (authConfigContainer == null)
                {
                    _logger.LogWarning("AHAMATIC: AuthConfig not found in configurations.");
                    return null;
                }

                var authConfigArray = authConfigContainer.Value.Deserialize<
                    List<AuthConfigDetail>
                >();

                if (authConfigArray == null)
                {
                    _logger.LogWarning(
                        "AHAMATIC: AuthConfig Value could not be deserialized into module list."
                    );
                    return null;
                }

                var moduleConfig = authConfigArray.FirstOrDefault(c =>
                    c.Module.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                );

                if (moduleConfig != null)
                {
                    _logger.LogInformation(
                        "AHAMATIC: Found AuthConfig for module {Module}",
                        moduleName
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "AHAMATIC: No AuthConfig found for module {Module}",
                        moduleName
                    );
                }

                return moduleConfig;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "AHAMATIC: HTTP Request failed during app validation. Check API Base URL and connectivity."
                );
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "AHAMATIC: Failed to deserialize response. Check model accuracy against JSON data."
                );
                return null;
            }
        }

        private async Task<AhamaticLoginResponse?> LoginAhamaticInternal(
            string email,
            string password,
            string apiKey
        )
        {
            const string endpointPath = "api/auth/email";

            string requestUrl = $"{_ahamaticOptions.ApiBaseUrl}{endpointPath}";

            var payload = new
            {
                apiKey = apiKey,
                emailAddress = email,
                password = password,
            };

            var jsonPayload = JsonSerializer.Serialize(payload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "AHAMATIC LOGIN: Attempting login for {Email} at {Url}",
                email,
                requestUrl
            );

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var jsonDocument = JsonDocument.Parse(responseContent);

                    var root = jsonDocument.RootElement;

                    if (
                        root.TryGetProperty("token", out var tokenElement)
                        && root.TryGetProperty("refreshToken", out var refreshTokenElement)
                        && root.TryGetProperty("account", out var accountElement)
                        && accountElement.TryGetProperty("PersonId", out var personIdElement)
                    )
                    {
                        var token = tokenElement.GetString();

                        var refreshToken = refreshTokenElement.GetString();

                        var accountId = personIdElement.GetInt32().ToString();

                        if (token != null && refreshToken != null)
                        {
                            return new AhamaticLoginResponse
                            {
                                Token = token,
                                RefreshToken = refreshToken,
                                AccountId = accountId,
                            };
                        }
                    }

                    _logger.LogError(
                        "AHAMATIC LOGIN FAILED: 200 OK but missing essential tokens/PersonId."
                    );

                    return null;
                }
                else
                {
                    _logger.LogError(
                        "AHAMATIC LOGIN FAILED: Status code {StatusCode}. Response: {Response}",
                        response.StatusCode,
                        responseContent
                    );
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AHAMATIC LOGIN EXCEPTION: An unexpected error occurred during the request."
                );
                return null;
            }
        }

        private async Task<JsonDocument?> AhamaticCidaasValidationInternal(
            string cidaasAccessToken,
            string cidaasClientId,
            string cidaasIssuer,
            string apiKey
        )
        {
            const string endpointPath = "api/auth/cidaas";

            string requestUrl = $"{_ahamaticOptions.ApiBaseUrl}{endpointPath}";

            var payload = new Dictionary<string, string>
            {
                { "apiKey", apiKey },
                { "access_token", cidaasAccessToken },
                { "clientId", cidaasClientId },
                { "redirectUrl", "test" },
                { "issuer", cidaasIssuer },
            };

            var jsonPayload = JsonSerializer.Serialize(payload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "AHAMATIC CIDAAS VALIDATION: Attempting token exchange with Cidaas data."
            );

            try
            {
                var response = await _httpClient.PostAsync(requestUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "AHAMATIC CIDAAS VALIDATION SUCCESS: Response JSON: {Json}",
                        responseContent
                    );

                    _logger.LogInformation(
                        "AHAMATIC CIDAAS VALIDATION: Successfully exchanged Cidaas token for Ahamatic tokens. Status: 200 OK."
                    );

                    return JsonDocument.Parse(responseContent);
                }
                else
                {
                    _logger.LogError(
                        "AHAMATIC CIDAAS VALIDATION FAILED: Status code {StatusCode}. Response: {Response}",
                        response.StatusCode,
                        responseContent
                    );

                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AHAMATIC CIDAAS VALIDATION EXCEPTION: An unexpected error occurred."
                );

                return null;
            }
        }
    }
}
