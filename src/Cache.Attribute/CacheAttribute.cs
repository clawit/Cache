namespace Cache.Attribute
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false,Inherited = false)]
    public class CacheAttribute : Attribute
    {
        public CacheAttribute() : this(Members.All)
        {

        }

        public CacheAttribute(Members membersToCache)
        {

        }
    }
}
