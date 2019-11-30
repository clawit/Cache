using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemCommonLibrary.Serialization;

namespace Cache.Implement.DistributedMemory
{
    public class CacheSyncMsg
    {
        public CacheSyncMsg()
        {
            CreatedAt = DateTime.Now;
            Options = new Dictionary<string, object>();
        }

        public DateTime CreatedAt { get; set; }

        public string CacheKey { get; set; }

        public bool IsBatch {
            get {
                if (true == Options?.ContainsKey("IsBatch"))
                {
                    return (bool)this.Options["IsBatch"];
                }
                else
                    return false;
            } 
        }

        public CacheAction Action { get; set; }

        public int Duration { get; set; }

        public Dictionary<string, object> Options { get; set; }

        public Guid Sender { get; set; }

        public static CacheSyncMsg CreateRemoveMsg(Guid sender, string key)
        {
            var msg = new CacheSyncMsg() {
                CacheKey = key,
                Action = CacheAction.Remove,
                Sender = sender
            };

            return msg;
        }

        public static CacheSyncMsg CreateRemoveBatchMsg(Guid sender, string prefix)
        {
            var msg = new CacheSyncMsg() {
                CacheKey = prefix,
                Action = CacheAction.Remove,
                Sender = sender
            };
            msg.Options.Add("IsBatch", true);

            return msg;
        }

        public static CacheSyncMsg CreateClearMsg(Guid sender)
        {
            var msg = new CacheSyncMsg() {
                Action = CacheAction.Remove,
                Sender = sender
            };

            return msg;
        }

        public static CacheSyncMsg CreateUpdateExpireMsg(Guid sender, string key, int duration)
        {
            var msg = new CacheSyncMsg() {
                CacheKey = key,
                Action = CacheAction.UpdateExpire,
                Duration = duration,
                Sender = sender
            };

            return msg;
        }

        public static CacheSyncMsg CreateStoreMsg(Guid sender, string key, object data, IDictionary<string, object> parameters)
        {
            var msg = new CacheSyncMsg()
            {
                CacheKey = key,
                Action = CacheAction.Store,
                Sender = sender
            };
            msg.Options.Add("Parameters", parameters);

            if (data != null)
            {
                var dataType = data.GetType().AssemblyQualifiedName;
                msg.Options.Add("DataType", dataType);
                var dataStr = Convert.ToBase64String(BitSerializer.Serialize(data));
                msg.Options.Add("Data", dataStr);
            }
            else
            {
                msg.Options.Add("Data", string.Empty);
            }

            return msg;
        }

    }
}
