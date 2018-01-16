using System;

namespace Cache.Attribute
{
    [Flags]
    public enum Members
    {
        Methods = 1,
        Properties = 2,
        All = 3
    }
}
