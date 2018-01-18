using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Cache
{
    [Flags]
    public enum Members
    {
        Methods = 1,
        Properties = 2,
        All = 3
    }
}
