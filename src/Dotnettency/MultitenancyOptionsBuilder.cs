﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Dotnettency
{

    public class MultitenancyOptionsBuilder<TTenant>
        where TTenant : class
    {

        public MultitenancyOptionsBuilder(IServiceCollection serviceCollection)
        {
            Services = serviceCollection;

            // add default services
            Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            Services.AddScoped<ITenantAccessor<TTenant>, TenantAccessor<TTenant>>();
            // Tenant shell cache is special in that it houses the tenant shell for each tenant, and each 
            // tenant shell has state that needs to be kept local to the applicatioin (i.e the tenants container or middleware pipeline.)
            // Therefore it should always be a local / in-memory based cache as will have will have fundamentally non-serialisable state.
            Services.AddSingleton<ITenantShellCache<TTenant>, ConcurrentDictionaryTenantShellCache<TTenant>>();
            Services.AddSingleton<ITenantShellResolver<TTenant>, TenantShellResolver<TTenant>>();
            Services.AddScoped<TenantDistinguisherAccessor<TTenant>>();
            Services.AddScoped<ITenantShellAccessor<TTenant>, TenantShellAccessor<TTenant>>();

            // Support injection of TTenant (has side effect that may block during injection)
            Services.AddScoped<TTenant>((sp =>
            {
                var accessor = sp.GetRequiredService<ITenantAccessor<TTenant>>();
                var tenant = accessor.CurrentTenant.Value.Result;
                return tenant;
            }));

            // Support injection of Task<TTenant> - a convenience that allows non blocking access to tenant when required 
            // minor contention on Lazy<>
            Services.AddScoped<Task<TTenant>>((sp =>
            {
                return sp.GetRequiredService<ITenantAccessor<TTenant>>().CurrentTenant.Value;             
            }));


        }

        public Func<IServiceProvider> BuildServiceProvider { get; set; }



        //public IServiceProvider ServiceProvider { get; set; }

        public MultitenancyOptionsBuilder<TTenant> DistinguishTenantsWith<T>()
            where T : class, ITenantDistinguisherFactory<TTenant>
        {
            Services.AddSingleton<ITenantDistinguisherFactory<TTenant>, T>();
            return this;

        }

        public MultitenancyOptionsBuilder<TTenant> DistinguishTenantsByHostname()
        {
            Services.AddSingleton<ITenantDistinguisherFactory<TTenant>, HostnameTenantDistinguisherFactory<TTenant>>();
            return this;

        }

        public MultitenancyOptionsBuilder<TTenant> DistinguishTenantsByHostnameWithPort()
        {
            Services.AddSingleton<ITenantDistinguisherFactory<TTenant>, HostnameAndPortTenantDistinguisherFactory<TTenant>>();
            return this;
        }

        public MultitenancyOptionsBuilder<TTenant> DistinguishTenantsBySchemeHostnameAndPort()
        {
            Services.AddSingleton<ITenantDistinguisherFactory<TTenant>, SchemeHostnameAndPortTenantDistinguisherFactory<TTenant>>();
            return this;

        }

        public IServiceCollection Services { get; set; }

        public MultitenancyOptionsBuilder<TTenant> InitialiseTenant<T>()
       where T : class, ITenantShellFactory<TTenant>
        {
            Services.AddSingleton<ITenantShellFactory<TTenant>, T>();
            return this;

        }

        public MultitenancyOptionsBuilder<TTenant> InitialiseTenant(Func<TenantDistinguisher, TenantShell<TTenant>> factoryMethod)
        {
            var factory = new DelegateTenantShellFactory<TTenant>(factoryMethod);
            Services.AddSingleton<ITenantShellFactory<TTenant>>(factory);
            return this;
        }
    }


}