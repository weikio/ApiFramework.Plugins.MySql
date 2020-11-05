using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.MySql
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddMySql(this IApiFrameworkBuilder builder)
        {
            var assembly = typeof(MySqlOptions).Assembly;
            var apiPlugin = new ApiPlugin { Assembly = assembly };

            builder.Services.AddSingleton(typeof(ApiPlugin), apiPlugin);

            builder.Services.Configure<ApiPluginOptions>(options =>
            {
                if (options.ApiPluginAssemblies.Contains(assembly))
                {
                    return;
                }

                options.ApiPluginAssemblies.Add(assembly);
            });

            return builder;
        }

        public static IApiFrameworkBuilder AddMySql(this IApiFrameworkBuilder builder, string endpoint, MySqlOptions configuration)
        {
            builder.AddMySql();

            builder.Services.RegisterEndpoint(endpoint, "Weikio.ApiFramework.Plugins.MySql", configuration);

            return builder;
        }
    }
}
