using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cache
{
    public interface ICacheProvider
    {
        bool Contains(string key);

        T Retrieve<T>(string key);

        void Store(string key, object data);

        // Remove is needed for writeable properties which must invalidate the Cache
        // You can skip this method but then only readonly properties are supported
        void Remove(string key);

    }
}
