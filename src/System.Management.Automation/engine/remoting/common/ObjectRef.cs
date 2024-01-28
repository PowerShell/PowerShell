// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The purpose of this class is to hide an object (mask it) and replace it
    /// with a substitute temporarily. This is used in pushing and popping
    /// runspaces. It is also used to temporarily set a PowerShell object's host as
    /// the Runspace object's host when the PowerShell object is executed.
    /// </summary>
    internal sealed class ObjectRef<T> where T : class
    {
        /// <summary>
        /// New value.
        /// </summary>
        private T _newValue;
        /// <summary>
        /// Old value.
        /// </summary>
        private readonly T _oldValue;

        /// <summary>
        /// Old value.
        /// </summary>
        internal T OldValue
        {
            get
            {
                return _oldValue;
            }
        }

        /// <summary>
        /// Value.
        /// </summary>
        internal T Value
        {
            get
            {
                if (_newValue == null)
                {
                    return _oldValue;
                }
                else
                {
                    return _newValue;
                }
            }
        }

        /// <summary>
        /// Is overridden.
        /// </summary>
        internal bool IsOverridden
        {
            get
            {
                return _newValue != null;
            }
        }

        /// <summary>
        /// Constructor for ObjectRef.
        /// </summary>
        internal ObjectRef(T oldValue)
        {
            Dbg.Assert(oldValue != null, "Expected oldValue != null");
            _oldValue = oldValue;
        }

        /// <summary>
        /// Override.
        /// </summary>
        internal void Override(T newValue)
        {
            Dbg.Assert(newValue != null, "Expected newValue != null");
            _newValue = newValue;
        }

        /// <summary>
        /// Revert.
        /// </summary>
        internal void Revert()
        {
            _newValue = null;
        }
    }
}
