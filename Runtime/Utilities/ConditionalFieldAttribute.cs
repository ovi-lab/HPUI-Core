using System;
using UnityEngine;

namespace ubco.ovilab.HPUI
{
    // Based on https://github.com/Deadcows/MyBox/blob/master/Attributes/ConditionalFieldAttribute.cs
    /// <summary>
    /// Conditionally Show/Hide field in inspector, based on some other field or property value
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ConditionalFieldAttribute : PropertyAttribute
    {
        public string conditionalProp;

        public ConditionalFieldAttribute(string conditionalProp)
        {
            this.conditionalProp = conditionalProp;
        }
    }
}
