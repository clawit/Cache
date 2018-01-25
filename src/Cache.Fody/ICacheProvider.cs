#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cache.Fody
{
    public interface ICacheProvider
    {
        bool Contains(string key);

        T Retrieve<T>(string key);

        void Store(string key, object data);

        // Remove is needed for writeable properties which must invalidate the Cache
        // You can skip this method but then only readonly properties are supported
        void Remove(string key);

        void Clear();
    }
}
