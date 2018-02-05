#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Cache
{
    using System;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false,Inherited = false)]
    public class CacheAttribute : Attribute
    {
        public int Duration { get; set; }

        public CacheAttribute() 
        {

        }

        
    }
}
