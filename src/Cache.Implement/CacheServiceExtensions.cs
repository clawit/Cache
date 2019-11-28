using Cache.Implement.InMemory;
using Cache.Implement.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using Cache;
using Cache.Implement.DistributedMemory;

namespace Cache.Implement
{
    public static class CacheServiceExtensions
    {
        public static IServiceCollection AddCache(this IServiceCollection services, CacheOption option)
        {
            CacheProvider provider = null;
            switch (option.CacheType)
            {
                case CacheType.RuntimeCache:
                    provider = new CacheProvider(new RuntimeCache());
                    break;
                case CacheType.InMemoryCache:
                    provider = new CacheProvider(new InMemoryCache());
                    break;
                case CacheType.DistributedMemoryCache:
                    provider = new CacheProvider(new DistributedMemoryCache(option));
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
