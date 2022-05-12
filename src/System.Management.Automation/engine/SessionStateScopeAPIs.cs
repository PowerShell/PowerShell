// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        /// <summary>
        /// The current scope. It is either the global scope or
        /// a nested scope within the global scope. The current
        /// scope is implied or can be accessed using $local in
        /// the shell.
        /// </summary>
        private SessionStateScope _currentScope;

        /// <summary>
        /// Cmdlet parameter name to return in the error message instead of "scopeID".
        /// </summary>
        internal const string ScopeParameterName = "Scope";

        /// <summary>
        /// Given a scope identifier, returns the proper session state scope.
        /// </summary>
        /// <param name="scopeID">
        /// A scope identifier that is either one of the "special" scopes like
        /// "global", "local", or "private, or a numeric ID of a relative scope
        /// to the current scope.
        /// </param>
        /// <returns>
        /// The scope identified by the scope ID or the current scope if the
        /// scope ID is not defined as a special or numeric scope identifier.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scopeID"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal SessionStateScope GetScopeByID(string scopeID)
        {
            SessionStateScope result = _currentScope;

            if (!string.IsNullOrEmpty(scopeID))
            {
                if (string.Equals(
                        scopeID,
                        StringLiterals.Global,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result = GlobalScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Local,
                            StringComparison.OrdinalIgnoreCase))
                {
                    result = _currentScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Private,
                            StringComparison.OrdinalIgnoreCase))
                {
                    result = _currentScope;
                }
                else if (string.Equals(
                            scopeID,
                            StringLiterals.Script,
                            StringComparison.OrdinalIgnoreCase))
                {
                    // Get the current script scope from the stack.
                    result = _currentScope.ScriptScope;
                }
                else
                {
                    // Since the scope is not any of the special scopes
                    // try parsing it as an ID

                    try
                    {
                        int scopeNumericID = Int32.Parse(scopeID, System.Globalization.CultureInfo.CurrentCulture);

                        if (scopeNumericID < 0)
                        {
                            throw PSTraceSource.NewArgumentOutOfRangeException(ScopeParameterName, scopeID);
                        }

                        result = GetScopeByID(scopeNumericID) ?? _currentScope;
                    }
                    catch (FormatException)
                    {
                        throw PSTraceSource.NewArgumentException(ScopeParameterName, AutomationExceptions.InvalidScopeIdArgument, ScopeParameterName);
                    }
                    catch (OverflowException)
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException(ScopeParameterName, scopeID);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Given a scope ID, walks the scope list to the appropriate scope and returns it.
        /// </summary>
        /// <param name="scopeID">
        /// The numeric indexer to the scope relative to the current scope.
        /// </param>
        /// <returns>
        /// The scope at the index specified.  The index is relative to the current
        /// scope.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal SessionStateScope GetScopeByID(int scopeID)
        {
            SessionStateScope processingScope = _currentScope;
            int originalID = scopeID;

            while (scopeID > 0 && processingScope != null)
            {
                processingScope = processingScope.Parent;
                scopeID--;
            }

            if (processingScope == null && scopeID >= 0)
            {
                ArgumentOutOfRangeException outOfRange =
                    PSTraceSource.NewArgumentOutOfRangeException(
                        ScopeParameterName,
                        originalID,
                        SessionStateStrings.ScopeIDExceedsAvailableScopes,
                        originalID);
                throw outOfRange;
            }

            return processingScope;
        }

        /// <summary>
        /// The global scope of session state.  Can be accessed
        /// using $global in the shell.
        /// </summary>
        internal SessionStateScope GlobalScope { get; }

        /// <summary>
        /// The module scope of a session state. This is only used internally
        /// by the engine. There is no module scope qualifier.
        /// </summary>
        internal SessionStateScope ModuleScope { get; }

        /// <summary>
        /// Gets the session state current scope.
        /// </summary>
        internal SessionStateScope CurrentScope
        {
            get
            {
                return _currentScope;
            }

            set
            {
                Diagnostics.Assert(
                    value != null,
                    "A null scope should never be set");
#if DEBUG
                // This code is ifdef'd for DEBUG because it may pose a significant
                // performance hit and is only really required to validate our internal
                // code. There is no way anyone outside the Monad codebase can cause
                // these error conditions to be hit.

                // Need to make sure the new scope is in the global scope lineage

                SessionStateScope scope = value;
                bool inGlobalScopeLineage = false;

                while (scope != null)
                {
                    if (scope == GlobalScope)
                    {
                        inGlobalScopeLineage = true;
                        break;
                    }

                    scope = scope.Parent;
                }

                Diagnostics.Assert(
                    inGlobalScopeLineage,
                    "The scope specified to be set in CurrentScope is not in the global scope lineage. All scopes must originate from the global scope.");
#endif

                _currentScope = value;
            }
        }

        /// <summary>
        /// Gets the session state current script scope.
        /// </summary>
        internal SessionStateScope ScriptScope { get { return _currentScope.ScriptScope; } }

        /// <summary>
        /// Creates a new scope in the scope tree and assigns the parent
        /// and child scopes appropriately.
        /// </summary>
        /// <param name="isScriptScope">
        /// If true, the new scope is pushed on to the script scope stack and
        /// can be referenced using $script:
        /// </param>
        /// <returns>
        /// A new SessionStateScope which is a child of the current scope.
        /// </returns>
        internal SessionStateScope NewScope(bool isScriptScope)
        {
            Diagnostics.Assert(
                _currentScope != null,
                "The currentScope should always be set.");

            // Create the new child scope.

            SessionStateScope newScope = new SessionStateScope(_currentScope);

            if (isScriptScope)
            {
                newScope.ScriptScope = newScope;
            }

            return newScope;
        }

        /// <summary>
        /// Removes the current scope from the scope tree and
        /// changes the current scope to the parent scope.
        /// </summary>
        /// <param name="scope">
        /// The scope to cleanup and remove.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// The global scope cannot be removed.
        /// </exception>
        internal void RemoveScope(SessionStateScope scope)
        {
            Diagnostics.Assert(
                _currentScope != null,
                "The currentScope should always be set.");

            if (scope == GlobalScope)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            StringLiterals.Global,
                            SessionStateCategory.Scope,
                            "GlobalScopeCannotRemove",
                            SessionStateStrings.GlobalScopeCannotRemove);

                throw e;
            }

            // Give the provider a chance to cleanup the drive data associated
            // with drives in this scope

            foreach (PSDriveInfo drive in scope.Drives)
            {
                if (drive == null)
                {
                    continue;
                }

                CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

                // Call CanRemoveDrive to give the provider a chance to cleanup
                // but ignore the return value and exceptions

                try
                {
                    CanRemoveDrive(drive, context);
                }
                catch (LoopFlowException)
                {
                    throw;
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (ActionPreferenceStopException)
                {
                    throw;
                }
                catch (Exception) // Catch-all OK, 3rd party callout.
                {
                    // Ignore all exceptions from the provider as we are
                    // going to force the removal anyway
                }
            }

            scope.RemoveAllDrives();

            // If the scope being removed is the current scope,
            // then it must be removed from the tree.

            if (scope == _currentScope && _currentScope.Parent != null)
            {
                _currentScope = _currentScope.Parent;
            }

            scope.Parent = null;
        }
    }
}

#pragma warning restore 56500
