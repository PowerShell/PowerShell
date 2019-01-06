// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace System.Management.Automation
{
    internal sealed class SessionStateScopeEnumerator : IEnumerator<SessionStateScope>, IEnumerable<SessionStateScope>
    {
        /// <summary>
        /// Constructs an enumerator for enumerating through the session state scopes
        /// using the appropriate scoping rules (default to dynamic scoping).
        /// </summary>
        /// <param name="scope">
        ///   The starting scope to start the enumeration from.
        /// </param>
        internal SessionStateScopeEnumerator(SessionStateScope scope)
        {
            Diagnostics.Assert(scope != null, "Caller to verify scope argument");
            _initialScope = scope;
        }

        /// <summary>
        /// Uses the proper scoping rules to get the next scope to do the lookup in.
        /// </summary>
        /// <returns>
        /// True if the enumerator was advanced to the next scope, or false otherwise.
        /// </returns>
        public bool MoveNext()
        {
            // On the first call to MoveNext the enumerator should be before
            // the first scope in the lookup and then advance to the first
            // scope in the lookup

            _currentEnumeratedScope = _currentEnumeratedScope == null ? _initialScope : _currentEnumeratedScope.Parent;

            // If the current scope is the global scope there is nowhere else
            // to do the lookup, so return false.
            return (_currentEnumeratedScope != null);
        }

        /// <summary>
        /// Sets the enumerator to before the first scope.
        /// </summary>
        public void Reset()
        {
            _currentEnumeratedScope = null;
        }

        /// <summary>
        /// Gets the current lookup scope.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The enumerator is positioned before the first element of the
        /// collection or after the last element.
        /// </exception>
        SessionStateScope IEnumerator<SessionStateScope>.Current
        {
            get
            {
                if (_currentEnumeratedScope == null)
                {
                    throw PSTraceSource.NewInvalidOperationException();
                }

                return _currentEnumeratedScope;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<SessionStateScope>)this).Current;
            }
        }

        /// <summary>
        /// Gets the IEnumerator for this class.
        /// </summary>
        /// <returns>
        /// The IEnumerator interface for this class.
        /// </returns>
        System.Collections.Generic.IEnumerator<SessionStateScope> System.Collections.Generic.IEnumerable<SessionStateScope>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        public void Dispose()
        {
            Reset();
        }

        private readonly SessionStateScope _initialScope;
        private SessionStateScope _currentEnumeratedScope;
    }
}

