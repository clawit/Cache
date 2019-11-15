using System;
using System.Collections.Generic;
using System.Text;

namespace Cache.Implement
{
    public class CacheRunnerOption
    {
        public bool UseCacheRunner { get; set; } = false;

        public Dictionary<string, object> Options { get; set; } = new Dictionary<string, object>();
    }
}
