using System;
using System.Runtime.Serialization;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Cache.Fody
{
    [Serializable]
    public class NoCacheProviderException : Exception
    {
        public NoCacheProviderException()
        {
        }

        public NoCacheProviderException(string message) : base(message)
        {
        }

        public NoCacheProviderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoCacheProviderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}