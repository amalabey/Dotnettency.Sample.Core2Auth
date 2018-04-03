using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dotnettency;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sample.Core2Auth
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();

            var serviceProvider = services.AddMultiTenancy<Tenant>((options) =>
            {
                options
                    .DistinguishTenantsBySchemeHostnameAndPort() // The distinguisher used to identify one tenant from another.
                    .InitialiseTenant<TenantShellFactory>() // factory class to load tenant when it needs to be initialised for the first time. Can use overload to provide a delegate instead.                    
                    .ConfigureTenantMiddleware((middlewareOptions) =>
                    {
                        // This method is called when need to initialise the middleware pipeline for a tenant (i.e on first request for the tenant)
                        middlewareOptions.OnInitialiseTenantPipeline((context, appBuilder) =>
                        {
                            appBuilder.UseAuthentication();

                            appBuilder.UseStaticFiles(); // This demonstrates static files middleware, but below I am also using per tenant hosting environment which means each tenant can see its own static files in addition to the main application level static files.

                            if (context.Tenant?.Name == "Foo")
                            {
                                appBuilder.UseWelcomePage("/welcome");
                            }

                            appBuilder.UseMvc(routes =>
                            {
                                routes.MapRoute(
                                    name: "default",
                                    template: "{controller=Home}/{action=Index}/{id?}");
                            });
                        });
                    }) // Configure per tenant containers.
                    .ConfigureTenantContainers((containerBuilder) =>
                    {
                        // Extension methods available here for supported containers. We are using structuremap..
                        // We are using an overload that allows us to configure structuremap with familiar IServiceCollection.
                        containerBuilder.WithStructureMapServiceCollection((tenant, tenantServices) =>
                        {
                            // tenantServices.AddSingleton<SomeTenantService>();
                            tenantServices.AddMvc();

                            tenantServices.AddAuthentication("Bearer")
                                .AddIdentityServerAuthentication(authOptions =>
                                {
                                    authOptions.Authority = "https://mytenant.login.localtest.me";
                                    authOptions.RequireHttpsMetadata = false;
                                    authOptions.ApiName = "api@mytenant";
                                });
                        });
                    });
            });

            // When using tenant containers, must return IServiceProvider.
            return serviceProvider;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Add the multitenancy middleware.
            app.UseMultitenancy<Tenant>((options) =>
            {
                options
                       .UsePerTenantContainers()
                       .UsePerTenantMiddlewarePipeline();
            });

            //app.UseMvc(routes =>
            //{
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});
        }
    }
}
