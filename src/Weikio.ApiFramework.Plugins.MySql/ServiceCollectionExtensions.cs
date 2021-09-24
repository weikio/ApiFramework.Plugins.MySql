using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.MySql
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddMySql(this IApiFrameworkBuilder builder, string endpoint  = null, MySqlOptions configuration = null)
        {
            builder.Services.AddMySql(endpoint, configuration);

            return builder;
        }
        
        public static IServiceCollection AddMySql(this IServiceCollection services, string endpoint = null, MySqlOptions configuration =null)
        {
            services.RegisterPlugin(endpoint, configuration);

            return services;
        }
    }
}
