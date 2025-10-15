// cidaas_net_sdk.core/CidaasExtensions.cs (CORREGIDO)

using System;
using cidaas_net_sdk.options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            builder.Services.AddSingleton(options);

            builder.Services.AddSingleton<CidaasAuthService>();

            using var tempServiceProvider = builder.Services.BuildServiceProvider();
            var service = tempServiceProvider.GetRequiredService<CidaasAuthService>();

            service.ConfigureAuth(builder);

            var factoryLogger = tempServiceProvider.GetRequiredService<
                ILogger<CidaasAuthService>
            >();
            CidaasFactory.Initialize(options, factoryLogger);

            return builder;
        }
    }
}
