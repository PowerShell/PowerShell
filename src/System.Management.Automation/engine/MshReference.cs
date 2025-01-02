// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Dynamic;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;

namespace System.Management.Automation
{
    /// <summary>
    /// Define type for a reference object in PowerShell scripting language.
    /// </summary>
    /// <remarks>
    /// This class is used to describe both kinds of references:
    ///     a. reference to a value: _value will be holding the value being referenced.
    ///     b. reference to a variable: _value will be holding a PSVariable instance for the variable to be referenced.
    ///
    /// A reference is created in following ways,
    ///     a. value reference
    ///         $a = [ref] 3
    ///         [ref] $a = 3
    ///         [ref] $a = $b
    ///     b. variable reference
    ///         $a = [ref] $b
    /// </remarks>
    public class PSReference
    {
        private object _value;

        /// <summary>
        /// Create an instance of PSReference.
        /// </summary>
        /// <param name="value"></param>
        public PSReference(object value)
        {
            _value = value;
        }

        /// <summary>
        /// Get and set value of PSReference.
        /// </summary>
        /// <remarks>
        /// If underlining object is a value, the object itself will be operated on.
        /// If underlining object is a variable, the variable will be operated on.
        /// </remarks>
        public object Value
        {
            get
            {
                PSVariable variable = _value as PSVariable;

                if (variable != null)
                {
                    return variable.Value;
                }

                return _value;
            }

            set
            {
                PSVariable variable = _value as PSVariable;

                if (variable != null)
                {
                    variable.Value = value;
                    return;
                }

                _value = value;
            }
        }

        internal static readonly CallSite<Func<CallSite, object, object, object>> CreatePsReferenceInstance =
                CallSite<Func<CallSite, object, object, object>>.Create(PSCreateInstanceBinder.Get(new CallInfo(1), null));

        internal static PSReference CreateInstance(object value, Type typeOfValue)
        {
            Type psReferType = typeof(PSReference<>).MakeGenericType(typeOfValue);
            return (PSReference)CreatePsReferenceInstance.Target.Invoke(CreatePsReferenceInstance, psReferType, value);
        }
    }

    internal class PSReference<T> : PSReference
    {
        public PSReference(object value) : base(value)
        {
        }
    }
}
