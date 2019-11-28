using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using SystemCommonLibrary.Json;

namespace Cache.Implement.DistributedMemory
{
    public static class RedisValueExtension
    {
        public static bool TryParseMsg(this RedisValue value, out CacheSyncMsg msg)
        {
            bool result = false;
            msg = null;
            if (!value.IsNullOrEmpty && value.HasValue)
            {
                try
                {
                    msg = DynamicJson.Parse(value).Deserialize<CacheSyncMsg>();
                    if (msg != null)
                    {
                        result = true;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidCastException($"TryParseMsg error: {ex.Message} value: {value}", ex);
                }
            }

            return result;
        }
    }
}
