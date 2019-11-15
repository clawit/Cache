using Cache;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cache.Implement.Runtime
{
    public class RuntimeCache : ICacheProvider
    {
        public RuntimeCache()
        {
            Storage = new Dictionary<string, object>();
        }

        private Dictionary<string, object> Storage { get; set; }

        public bool Contains(string key)
        {
            return Storage.ContainsKey(key);
        }

        public T Retrieve<T>(string key)
        {
            return (T)Storage[key];
        }

        // Remove is needed for writeable properties which must invalidate the Cache
        // You can skip this method but then only readonly properties are supported
        public void Remove(string key)
        {
            Storage.Remove(key);
        }

        public void Clear()
        {
            Storage.Clear();
        }

        public IEnumerable<string> Keys(Func<string, bool> predicate)
        {
            if (predicate == null)
                return Storage.Keys;
            else
                return Storage.Keys.Where(predicate);

        }

        public void Store(string key, object data, IDictionary<string, object> parameters)
        {
            Storage[key] = data;
        }
    }
}
