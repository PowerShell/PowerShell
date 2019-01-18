// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// Enumerates the items matching a particular name in the scopes specified using
    /// the appropriate scoping lookup rules.
    /// </summary>
    /// <typeparam name="T">
    /// The type of items that the derived class returns.
    /// </typeparam>
    internal abstract class ScopedItemSearcher<T> : IEnumerator<T>, IEnumerable<T>
    {
        #region ctor

        /// <summary>
        /// Constructs a scoped item searcher.
        /// </summary>
        /// <param name="sessionState">
        /// The state of the engine instance to enumerate through the scopes.
        /// </param>
        /// <param name="lookupPath">
        /// The parsed name of the item to lookup.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> or <paramref name="lookupPath"/>
        /// is null.
        /// </exception>
        internal ScopedItemSearcher(
            SessionStateInternal sessionState,
            VariablePath lookupPath)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException("sessionState");
            }

            if (lookupPath == null)
            {
                throw PSTraceSource.NewArgumentNullException("lookupPath");
            }

            this.sessionState = sessionState;
            _lookupPath = lookupPath;
            InitializeScopeEnumerator();
        }

        #endregion ctor

        #region IEnumerable/IEnumerator members

        /// <summary>
        /// Gets the current object as an IEnumerator.
        /// </summary>
        /// <returns>
        /// The current object as an IEnumerator.
        /// </returns>
        System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Moves the enumerator to the next matching scoped item.
        /// </summary>
        /// <returns>
        /// True if another matching scoped item was found, or false otherwise.
        /// </returns>
        public bool MoveNext()
        {
            bool result = true;

            if (!_isInitialized)
            {
                InitializeScopeEnumerator();
            }

            // Enumerate the scopes until a matching scoped item is found

            while (_scopeEnumerable.MoveNext())
            {
                T newCurrentItem;

                if (TryGetNewScopeItem(((IEnumerator<SessionStateScope>)_scopeEnumerable).Current, out newCurrentItem))
                {
                    _currentScope = ((IEnumerator<SessionStateScope>)_scopeEnumerable).Current;
                    _current = newCurrentItem;
                    result = true;
                    break;
                }

                result = false;

                if (_isSingleScopeLookup)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the current scoped item.
        /// </summary>

        T IEnumerator<T>.Current
        {
            get
            {
                return _current;
            }
        }

        public object Current
        {
            get
            {
                return _current;
            }
        }

        public void Reset()
        {
            InitializeScopeEnumerator();
        }

        public void Dispose()
        {
            _current = default(T);
            _scopeEnumerable.Dispose();
            _scopeEnumerable = null;
            _isInitialized = false;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Derived classes override this method to return their
        /// particular type of scoped item.
        /// </summary>
        /// <param name="scope">
        /// The scope to look the item up in.
        /// </param>
        /// <param name="name">
        /// The name of the item to retrieve.
        /// </param>
        /// <param name="newCurrentItem">
        /// The scope item that the derived class should return.
        /// </param>
        /// <returns>
        /// True if the scope item was found or false otherwise.
        /// </returns>
        protected abstract bool GetScopeItem(
            SessionStateScope scope,
            VariablePath name,
            out T newCurrentItem);

        #endregion IEnumerable/IEnumerator members

        /// <summary>
        /// Gets the lookup scope that the Current item was found in.
        /// </summary>
        internal SessionStateScope CurrentLookupScope
        {
            get { return _currentScope; }
        }

        private SessionStateScope _currentScope;

        /// <summary>
        /// Gets the scope in which the search begins.
        /// </summary>
        internal SessionStateScope InitialScope
        {
            get { return _initialScope; }
        }

        private SessionStateScope _initialScope;

        #region private members

        private bool TryGetNewScopeItem(
            SessionStateScope lookupScope,
            out T newCurrentItem)
        {
            bool result = GetScopeItem(
                lookupScope,
                _lookupPath,
                out newCurrentItem);

            return result;
        }

        private void InitializeScopeEnumerator()
        {
            // Define the lookup scope and if we have to do single
            // level or dynamic lookup based on the lookup variable

            _initialScope = sessionState.CurrentScope;

            if (_lookupPath.IsGlobal)
            {
                _initialScope = sessionState.GlobalScope;
                _isSingleScopeLookup = true;
            }
            else if (_lookupPath.IsLocal ||
                     _lookupPath.IsPrivate)
            {
                _initialScope = sessionState.CurrentScope;
                _isSingleScopeLookup = true;
            }
            else if (_lookupPath.IsScript)
            {
                _initialScope = sessionState.ScriptScope;
                _isSingleScopeLookup = true;
            }

            _scopeEnumerable =
                 new SessionStateScopeEnumerator(_initialScope);

            _isInitialized = true;
        }

        private T _current;
        protected SessionStateInternal sessionState;
        private VariablePath _lookupPath;
        private SessionStateScopeEnumerator _scopeEnumerable;
        private bool _isSingleScopeLookup;
        private bool _isInitialized;

        #endregion private members
    }

    /// <summary>
    /// The scope searcher for variables.
    /// </summary>
    internal class VariableScopeItemSearcher : ScopedItemSearcher<PSVariable>
    {
        public VariableScopeItemSearcher(
            SessionStateInternal sessionState,
            VariablePath lookupPath,
            CommandOrigin origin) : base(sessionState, lookupPath)
        {
            _origin = origin;
        }

        private readonly CommandOrigin _origin;

        /// <summary>
        /// Derived classes override this method to return their
        /// particular type of scoped item.
        /// </summary>
        /// <param name="scope">
        /// The scope to look the item up in.
        /// </param>
        /// <param name="name">
        /// The name of the item to retrieve.
        /// </param>
        /// <param name="variable">
        /// The scope item that the derived class should return.
        /// </param>
        /// <returns>
        /// True if the scope item was found or false otherwise.
        /// </returns>
        protected override bool GetScopeItem(
            SessionStateScope scope,
            VariablePath name,
            out PSVariable variable)
        {
            Diagnostics.Assert(!(name is FunctionLookupPath),
                "name was scanned incorrect if we get here and it is a FunctionLookupPath");

            bool result = true;

            variable = scope.GetVariable(name.QualifiedName, _origin);

            // If the variable is private and the lookup scope
            // isn't the current scope, claim that the variable
            // doesn't exist so that the lookup continues.

            if (variable == null ||
                (variable.IsPrivate &&
                 scope != sessionState.CurrentScope))
            {
                result = false;
            }

            return result;
        }
    }

    /// <summary>
    /// The scope searcher for aliases.
    /// </summary>
    internal class AliasScopeItemSearcher : ScopedItemSearcher<AliasInfo>
    {
        public AliasScopeItemSearcher(
            SessionStateInternal sessionState,
            VariablePath lookupPath) : base(sessionState, lookupPath)
        {
        }

        /// <summary>
        /// Derived classes override this method to return their
        /// particular type of scoped item.
        /// </summary>
        /// <param name="scope">
        /// The scope to look the item up in.
        /// </param>
        /// <param name="name">
        /// The name of the item to retrieve.
        /// </param>
        /// <param name="alias">
        /// The scope item that the derived class should return.
        /// </param>
        /// <returns>
        /// True if the scope item was found or false otherwise.
        /// </returns>
        protected override bool GetScopeItem(
            SessionStateScope scope,
            VariablePath name,
            out AliasInfo alias)
        {
            Diagnostics.Assert(!(name is FunctionLookupPath),
                "name was scanned incorrect if we get here and it is a FunctionLookupPath");

            bool result = true;
            alias = scope.GetAlias(name.QualifiedName);

            // If the alias is private and the lookup scope
            // isn't the current scope, claim that the alias
            // doesn't exist so that the lookup continues.

            if (alias == null ||
                ((alias.Options & ScopedItemOptions.Private) != 0 &&
                 scope != sessionState.CurrentScope))
            {
                result = false;
            }

            return result;
        }
    }

    /// <summary>
    /// The scope searcher for functions.
    /// </summary>
    internal class FunctionScopeItemSearcher : ScopedItemSearcher<FunctionInfo>
    {
        public FunctionScopeItemSearcher(
            SessionStateInternal sessionState,
            VariablePath lookupPath,
            CommandOrigin origin) : base(sessionState, lookupPath)
        {
            _origin = origin;
        }

        private readonly CommandOrigin _origin;

        /// <summary>
        /// Derived classes override this method to return their
        /// particular type of scoped item.
        /// </summary>
        /// <param name="scope">
        /// The scope to look the item up in.
        /// </param>
        /// <param name="path">
        /// The name of the item to retrieve.
        /// </param>
        /// <param name="script">
        /// The scope item that the derived class should return.
        /// </param>
        /// <returns>
        /// True if the scope item was found or false otherwise.
        /// </returns>
        protected override bool GetScopeItem(
            SessionStateScope scope,
            VariablePath path,
            out FunctionInfo script)
        {
            Diagnostics.Assert(path is FunctionLookupPath,
                "name was scanned incorrect if we get here and it is not a FunctionLookupPath");

            bool result = true;

            _name = path.IsFunction ? path.UnqualifiedPath : path.QualifiedName;

            script = scope.GetFunction(_name);

            if (script != null)
            {
                bool isPrivate;
                FilterInfo filterInfo = script as FilterInfo;
                if (filterInfo != null)
                {
                    isPrivate = (filterInfo.Options & ScopedItemOptions.Private) != 0;
                }
                else
                {
                    isPrivate = (script.Options & ScopedItemOptions.Private) != 0;
                }

                // If the function is private and the lookup scope
                // isn't the current scope, claim that the function
                // doesn't exist so that the lookup continues.

                if (isPrivate &&
                    scope != sessionState.CurrentScope)
                {
                    result = false;
                }
                else
                {
                    // Now check the visibility of the variable...
                    SessionState.ThrowIfNotVisible(_origin, script);
                }
            }
            else
            {
                result = false;
            }

            return result;
        }

        internal string Name
        {
            get { return _name; }
        }

        private string _name = string.Empty;
    }

    /// <summary>
    /// The scope searcher for drives.
    /// </summary>
    internal class DriveScopeItemSearcher : ScopedItemSearcher<PSDriveInfo>
    {
        public DriveScopeItemSearcher(
            SessionStateInternal sessionState,
            VariablePath lookupPath) : base(sessionState, lookupPath)
        {
        }

        /// <summary>
        /// Derived classes override this method to return their
        /// particular type of scoped item.
        /// </summary>
        /// <param name="scope">
        /// The scope to look the item up in.
        /// </param>
        /// <param name="name">
        /// The name of the item to retrieve.
        /// </param>
        /// <param name="drive">
        /// The scope item that the derived class should return.
        /// </param>
        /// <returns>
        /// True if the scope item was found or false otherwise.
        /// </returns>
        protected override bool GetScopeItem(
            SessionStateScope scope,
            VariablePath name,
            out PSDriveInfo drive)
        {
            Diagnostics.Assert(!(name is FunctionLookupPath),
                "name was scanned incorrect if we get here and it is a FunctionLookupPath");

            bool result = true;
            drive = scope.GetDrive(name.DriveName);

            if (drive == null)
            {
                result = false;
            }

            return result;
        }
    }
}
