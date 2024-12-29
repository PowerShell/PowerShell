// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Blocks caller trying to get the value of an object of type T
    /// until the value is set. After the set all future gets are
    /// unblocked.
    /// </summary>
    internal class AsyncObject<T> where T : class
    {
        /// <summary>
        /// Value.
        /// </summary>
        private T _value;

        /// <summary>
        /// Value was set.
        /// </summary>
        private readonly ManualResetEvent _valueWasSet;

        /// <summary>
        /// Value.
        /// </summary>
        internal T Value
        {
            get
            {
                bool result = _valueWasSet.WaitOne();
                if (!result)
                {
                    _value = null;
                }

                return _value;
            }

            set
            {
                _value = value;
                _valueWasSet.Set();
            }
        }

        /// <summary>
        /// Constructor for AsyncObject.
        /// </summary>
        internal AsyncObject()
        {
            _valueWasSet = new ManualResetEvent(false);
        }
    }
}
