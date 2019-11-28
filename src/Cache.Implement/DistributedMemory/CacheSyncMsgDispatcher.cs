using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SystemCommonLibrary.Data.Connector;
using SystemCommonLibrary.Json;

namespace Cache.Implement.DistributedMemory
{
    public class CacheSyncMsgDispatcher
    {
        private static string _redisConnector;
        private static Guid _sender;
        private static IConnectionMultiplexer _redis;
        private static ISubscriber _subscriber;
        private static RedisChannel _channel = new RedisChannel("DistributedMemoryCacheRunner", RedisChannel.PatternMode.Literal);

        public static void Initialize(string redisConnector, Guid sender)
        {
            _redisConnector = redisConnector;
            _sender = sender;

            //创建连接
            _redis = RedisConnector.Open(redisConnector);

            _subscriber = _redis.GetSubscriber();
        }

        public static void CleanKey(string key, bool sync = true)
        {
            CacheProvider.Provider.Remove(key);
            if (sync)
            {
                var msg = CacheSyncMsg.CreateRemoveMsg(_sender, key);
                _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
            }
        }

        public static void CleanBatch(string prefix, bool sync = true)
        {
            var keys = CacheProvider.Provider.Keys(k => k.StartsWith(prefix));
            Parallel.ForEach(keys, (key) => { CacheProvider.Provider.Remove(key); });

            if (sync)
            {
                var msg = CacheSyncMsg.CreateRemoveBatchMsg(_sender, prefix);
                _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
            }
        }

        public static void Clear(bool sync = true)
        {
            CacheProvider.Provider.Clear();

            if (sync)
            {
                var msg = CacheSyncMsg.CreateClearMsg(_sender);
                _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
            }
        }

        public static void UpdateExpire(string key, int duration, bool sync = true)
        {
            var data = CacheProvider.Provider.Retrieve<object>(key);
            CacheProvider.Provider.Remove(key);
            var parms = new Dictionary<string, object>();
            parms.Add("Duration", duration);
            parms.Add("Preset", null);
            CacheProvider.Provider.Store(key, data, parms);

            if (sync)
            {
                var msg = CacheSyncMsg.CreateUpdateExpireMsg(_sender, key, duration);
                _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
            }
        }

    }
}
