using System;

namespace RLMovie.Common
{
    /// <summary>
    /// Marks serialized UnityEngine.Object references that must be wired for a scenario to be considered valid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RequiredSharedReferenceAttribute : Attribute
    {
        public RequiredSharedReferenceAttribute(string label = null)
        {
            Label = label ?? string.Empty;
        }

        public string Label { get; }
    }
}
