using System;
using System.Collections.Generic;
using System.Text;

namespace Cache.Implement.DistributedMemory
{
    public enum CacheAction
    {
        Remove,
        UpdateExpire,
        Store
    }
}
