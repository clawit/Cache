using Cache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SystemCommonLibrary.Data.Connector;
using SystemCommonLibrary.Json;
using SystemCommonLibrary.Serialization;

namespace Cache.Implement.DistributedMemory
{
    public class DistributedMemoryCache : ICacheProvider
    {
        public static CacheOption CreateCacheOption(string redisConnector)
        {
            var option = new CacheOption();
            option.CacheType = CacheType.DistributedMemoryCache;
            option.Params.Add(_redisConnectorKey, redisConnector);

            return option;
        }

        private MemoryCache _storage { get; set; } = new MemoryCache(new MemoryCacheOptions());
        private ConcurrentDictionary<string, bool> _keys { get; set; } = new ConcurrentDictionary<string, bool>();
        private ConcurrentQueue<CacheSyncMsg> _queueMsg = new ConcurrentQueue<CacheSyncMsg>();
        private static readonly string _redisConnectorKey = "redisConnector";
        private RedisChannel _channel = new RedisChannel("DistributedMemoryCacheRunner", RedisChannel.PatternMode.Literal);

        private IConnectionMultiplexer _redis;
        private ISubscriber _subscriber;

        private static Task _taskRunner = Task.Factory.StartNew(Queue_Proc);
        private static Guid _self = Guid.NewGuid();
        private static ConcurrentQueue<CacheSyncMsg> _queue = new ConcurrentQueue<CacheSyncMsg>();

        public DistributedMemoryCache(CacheOption option)
        {
            string redisConnector = option?.Params?[_redisConnectorKey]?.ToString();
            if (string.IsNullOrEmpty(redisConnector))
            {
                throw new ArgumentNullException(_redisConnectorKey);
            }

            //创建连接
            _redis = RedisConnector.Open(redisConnector);

            _subscriber = _redis.GetSubscriber();

            //订阅CacheRunner的通道
            _subscriber.Subscribe(_channel, Broker_MsgRecevied);

            CacheSyncMsgDispatcher.Initialize(redisConnector, _self);
        }

        public void Clear()
        {
            if (_storage.Count > 0 )
            {
                _keys.Clear();
                _storage = new MemoryCache(new MemoryCacheOptions());
                GC.Collect();

                var msg = CacheSyncMsg.CreateClearMsg(_self);
                _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
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

            var msg = CacheSyncMsg.CreateRemoveMsg(_self, key);
            _subscriber.Publish(_channel, DynamicJson.Serialize(msg));

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

            var msg = CacheSyncMsg.CreateStoreMsg(_self, key, data, parameters);
            _subscriber.Publish(_channel, DynamicJson.Serialize(msg));
        }

        private void Broker_MsgRecevied(RedisChannel channel, RedisValue redisValue)
        {
            if (channel == _channel && redisValue.TryParseMsg(out var msg))
            {
                if (msg.Sender != _self)
                {
                    _queue.Enqueue(msg);
                }
            }
        }

        private static void Queue_Proc()
        {
            while (true)
            {
                if (_queue.TryDequeue(out var msg))
                {
                    switch (msg.Action)
                    {
                        case CacheAction.Remove:
                            if (msg.CreatedAt >= DateTime.Now.AddSeconds(-600))
                            {
                                if (msg.IsBatch)
                                {
                                    if (string.IsNullOrWhiteSpace(msg.CacheKey))
                                    {
                                        //全部清空
                                        CacheProvider.Provider.Clear();
                                        Debug.WriteLine("CacheRunner已全部清空缓存#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    }
                                    else
                                    {
                                        //批量清理
                                        var keys = CacheProvider.Provider.Keys(k => k.StartsWith(msg.CacheKey));
                                        Parallel.ForEach(keys, (key) => { CacheProvider.Provider.Remove(key); });

                                        Debug.WriteLine("CacheRunner批量清理:Key=" + msg.CacheKey + "#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                    }
                                }
                                else
                                {
                                    //清理单个key
                                    if (CacheProvider.Provider.Contains(msg.CacheKey))
                                        CacheProvider.Provider.Remove(msg.CacheKey);

                                    Debug.WriteLine("CacheRunner已删除缓存" + msg.CacheKey);
                                }
                            }
                            break;
                        case CacheAction.UpdateExpire:
                            //用于更新某个缓存的失效时间
                            var data = CacheProvider.Provider.Retrieve<object>(msg.CacheKey);
                            CacheProvider.Provider.Remove(msg.CacheKey);
                            var parms = new Dictionary<string, object>();
                            parms.Add("Duration", msg.Duration);
                            parms.Add("Preset", null);
                            CacheProvider.Provider.Store(msg.CacheKey, data, parms);

                            break;
                        case CacheAction.Store:
                            //用于同步缓存数据
                            if (msg.Options.ContainsKey("DataType")
                                && msg.Options.ContainsKey("Data")
                                && msg.Options.ContainsKey("Parameters"))
                            {
                                var parameters = msg.Options["Parameters"] as IDictionary<string, object>;

                                var type = msg.Options["DataType"] as string;
                                var dataStr = msg.Options["Data"] as string;
                                var dataSerialized = Convert.FromBase64String(dataStr);
                                var dataStore = BitSerializer.Deserialize(type, dataSerialized);

                                CacheProvider.Provider.Store(msg.CacheKey, dataStore, parameters);
                            }

                            break;
                        default:
                            Debug.WriteLine($"CacheRunner接收到未知的消息类别:{msg.Action}");
                            break;
                    }
                }
                else
                {
                    Thread.Sleep(0);
                }
            }
        }
    }
}
