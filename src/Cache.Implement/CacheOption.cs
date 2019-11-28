using System;
using System.Collections.Generic;
using System.Text;

namespace Cache.Implement
{
    public class CacheOption
    {
        public CacheType CacheType { get; set; }

        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    }
}
