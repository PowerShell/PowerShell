// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// Provides an enumerator for iterating through a multi-dimensional array.
    /// This is needed to encode multi-dimensional arrays in remote host methods.
    /// </summary>
    internal class Indexer : IEnumerable, IEnumerator
    {
        /// <summary>
        /// Current.
        /// </summary>
        private readonly int[] _current;

        /// <summary>
        /// Lengths.
        /// </summary>
        private readonly int[] _lengths;

        /// <summary>
        /// Current.
        /// </summary>
        public object Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Constructor for Indexer.
        /// </summary>
        internal Indexer(int[] lengths)
        {
            Dbg.Assert(lengths != null, "Expected lengths != null");
            _lengths = lengths;
            Dbg.Assert(CheckLengthsNonNegative(lengths), "Expected CheckLengthsNonNegative(lengths)");
            _current = new int[lengths.Length];
        }

        /// <summary>
        /// Check lengths non negative.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool CheckLengthsNonNegative(int[] lengths)
        {
            for (int i = 0; i < lengths.Length; ++i)
            {
                if (lengths[i] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            this.Reset();
            return this;
        }

        /// <summary>
        /// Reset.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _current.Length; ++i)
            {
                _current[i] = 0;
            }

            // Set last value to -1 so that MoveNext makes this 0,0,...,0.
            if (_current.Length > 0)
            {
                _current[_current.Length - 1] = -1;
            }
        }

        /// <summary>
        /// Move next.
        /// </summary>
        public bool MoveNext()
        {
            for (int i = _lengths.Length - 1; i >= 0; --i)
            {
                // See if we can increment this dimension.
                if (_current[i] < _lengths[i] - 1)
                {
                    _current[i]++;
                    return true;
                }

                // Otherwise reset i and try to increment the next one.
                _current[i] = 0;
            }

            return false;
        }
    }
}
