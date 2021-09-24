using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weikio.ApiFramework.Plugins.MySql.Configuration;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.MySql.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddApiFrameworkStarterKit()
                .AddMySql("/mysql",
                    new MySqlOptions()
                    {
                        ConnectionString = "server=;port=3306;uid=;pwd=;database=;Convert Zero Datetime=True;Allow Zero Datetime=False",
                        ExcludedTables = new[] { "ROW_BACKUP", "TEMP*", "MAIL*", "ACCESS*", "VIEW_MANAGER", "NOTIFICATION*", "IMPORT*", "*CACHE*" },
                        SqlCommands = new SqlCommands()
                        {
                            { "PRODUCT_STOCK_HISTORY_NON_ZERO", new SqlCommand()
                            {
                                CommandText = "SELECT * FROM PRODUCT_STOCK_HISTORY WHERE free_stock!=0 ORDER BY id ASC",
                                CommandSchemaText = "SELECT * FROM PRODUCT_STOCK_HISTORY WHERE free_stock!=0 ORDER BY id ASC limit 0",
                                DataTypeName = "MY_PRODUCT_STOCK_HISTORYItem"
                            } }
                        }
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
