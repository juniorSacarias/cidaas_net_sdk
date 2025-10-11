using cidaas_net_sdk.options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace cidaas_net_sdk.core
{
    public static class CidaasExtensions
    {
        public static WebApplicationBuilder AddCidaasAuth(
            this WebApplicationBuilder builder,
            Action<CidaasOptions> configureOptions
        )
        {
            var options = new CidaasOptions();
            configureOptions(options);

            var service = CidaasFactory.Initialize(options);
            service.ConfigureAuth(builder);

            builder.Services.AddSingleton(service);

            return builder;
        }
    }
}
