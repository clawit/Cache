#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Cache
{
    using System;

    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false,Inherited = false)]
    public class CacheKeyAttribute : Attribute
    {
        public CacheKeyAttribute()
        {

        }
    }
}
