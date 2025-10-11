using cidaas_net_sdk.options;

namespace cidaas_net_sdk.core
{
    public static class CidaasFactory
    {
        private static CidaasAuthService _instance;

        public static CidaasAuthService Initialize(CidaasOptions options)
        {
            options.Validate();

            _instance = new CidaasAuthService(options);

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
