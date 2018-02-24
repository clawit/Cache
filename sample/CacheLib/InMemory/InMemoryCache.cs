using Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CacheLib.InMemory
{
    public class InMemoryCache : ICacheProvider
    {
        private MemoryCache _storage { get; set; }
        private ConcurrentDictionary<string, bool> _keys { get; set; }

        public InMemoryCache()
        {
            _storage = new MemoryCache(new MemoryCacheOptions());
            _keys = new ConcurrentDictionary<string, bool>();
        }

        public void Clear()
        {
            if (_storage.Count > 0 )
            {
                _keys.Clear();
                _storage = new MemoryCache(new MemoryCacheOptions());
                GC.Collect();
            }
        }

        public bool Contains(string key)
        {
            return _storage.TryGetValue(key, out var value);
        }

        public IEnumerable<string> Keys(Func<string, bool> predicate)
        {
            if (predicate == null)
                return _keys.Keys;
            else
                return _keys.Keys.Where(predicate);
        }

        public void Remove(string key)
        {
            _keys.TryRemove(key, out bool value);
            _storage.Remove(key);
            Console.WriteLine("Removed:" + key);
        }

        public T Retrieve<T>(string key)
        {
            if (_storage.TryGetValue<T>(key, out T value))
                return value;
            else
                return default(T);
        }

        public void Store(string key, object data, IDictionary<string, object> parameters)
        {
            if (!_keys.ContainsKey(key))
                _keys.TryAdd(key, false);

            if (parameters != null && parameters.Count > 0
                && parameters.ContainsKey("Duration"))
            {
                int duration = (int)parameters["Duration"];
                var tsDur = TimeSpan.FromSeconds(duration);

                var cts = new CancellationTokenSource();
                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
                options.AbsoluteExpirationRelativeToNow = tsDur;
                options.AddExpirationToken(new CancellationChangeToken(cts.Token));
                options.RegisterPostEvictionCallback(new PostEvictionDelegate((entry, value, reason, state) => {
                    Console.WriteLine("reason:" + reason.ToString());
                    if (reason == EvictionReason.TokenExpired)
                        (state as ICacheProvider).Remove(entry as string);
                }), this);
                
                _storage.Set(key, data, options);
                cts.CancelAfter(tsDur);
            }
            else
                _storage.Set(key, data);

        }
    }
}
