using CacheLib.InMemory;
using CacheLib.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using Cache;

namespace CacheLib
{
    public static class CacheServiceExtensions
    {
        public static IServiceCollection AddCache(this IServiceCollection services, CacheType type)
        {
            CacheProvider provider = null;
            switch (type)
            {
                case CacheType.RuntimeCache:
                    provider = new CacheProvider(new RuntimeCache());
                    break;
                case CacheType.InMemoryCache:
                    provider = new CacheProvider(new InMemoryCache());
                    break;
                default:
                    throw new NotImplementedException();
            }

            return services;
        }

        public static IApplicationBuilder UseCache(this IApplicationBuilder app)
        {
            //add some code here if needed 

            return app;
        }
    }
}
