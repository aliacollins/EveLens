using System;

namespace EVEMon.Common.Attributes
{
    /// <summary>
    /// This attribute marks the API methods which are always queried on startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ForcedOnStartupAttribute : Attribute
    {
    }
}