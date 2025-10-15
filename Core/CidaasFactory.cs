using System;
using cidaas_net_sdk.options;
using Microsoft.Extensions.Logging;

namespace cidaas_net_sdk.core
{
    public static class CidaasFactory
    {
        private static CidaasAuthService? _instance;

        public static CidaasAuthService Initialize(
            CidaasOptions options,
            ILogger<CidaasAuthService> logger
        )
        {
            options.Validate();

            _instance = new CidaasAuthService(options, logger);

            return _instance;
        }

        public static CidaasAuthService GetInstance()
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    "CidaasFactory no fue inicializado. Llama a Initialize() primero."
                );
            }

            return _instance;
        }
    }
}
