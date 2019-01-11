// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the APIs to manipulate variables in the Runspace.
    /// </summary>
    public sealed class PSVariableIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private PSVariableIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Constructs a facade for the specified session.
        /// </summary>
        /// <param name="sessionState">
        /// The session for which the facade wraps.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal PSVariableIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentException("sessionState");
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the specified variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to get. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <returns>
        /// The specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        public PSVariable Get(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            // Null is returned whenever the requested variable is string.Empty.
            // As per Powershell V1 implementation:
            // 1. If the requested variable exists in the session scope, the variable value is returned.
            // 2. If the requested variable is not null and does not exist in the session scope, then a null value is returned to the pipeline.
            // 3. If the requested variable is null then an NewArgumentNullException is thrown.
            // PowerShell V3 has the similar experience.
            if (name != null && name.Equals(string.Empty))
            {
                return null;
            }

            return _sessionState.GetVariable(name);
        }

        /// <summary>
        /// Gets the specified variable from session state in the specified scope.
        /// If the variable doesn't exist in the specified scope no additional lookup
        /// will be done.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to get. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to do the lookup in.
        /// </param>
        /// <returns>
        /// The specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        internal PSVariable GetAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableAtScope(name, scope);
        }

        /// <summary>
        /// Gets the specified variable value from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to get. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public object GetValue(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValue(name);
        }

        /// <summary>
        /// Gets the specified variable from session state. If the variable
        /// is not found the default value is returned.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to get. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <param name="defaultValue">
        /// The default value returned if the variable could not be found.
        /// </param>
        /// <returns>
        /// The value of the specified variable or the default value if the variable
        /// is not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public object GetValue(string name, object defaultValue)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValue(name) ?? defaultValue;
        }

        /// <summary>
        /// Gets the specified variable from session state in the specified scope.
        /// If the variable doesn't exist in the specified scope no additional lookup
        /// will be done.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to get. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to do the lookup in.
        /// </param>
        /// <returns>
        /// The value of the specified variable.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="scope"/> is less than zero, or not
        /// a number and not "script", "global", "local", or "private"
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetValueAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetVariableValueAtScope(name, scope);
        }

        /// <summary>
        /// Sets the variable to the specified value.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to be set. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <param name="value">
        /// The value to set the variable to.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Set(string name, object value)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetVariableValue(name, value, CommandOrigin.Internal);
        }

        /// <summary>
        /// Sets the variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to set
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        public void Set(PSVariable variable)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetVariable(variable, false, CommandOrigin.Internal);
        }

        /// <summary>
        /// Removes the specified variable from session state.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to be removed. The name can contain drive and/or
        /// scope specifiers like "ENV:path" or "global:myvar".
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="name"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="name"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="name"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Remove(string name)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariable(name);
        }

        /// <summary>
        /// Removes the specified variable from session state.
        /// </summary>
        /// <param name="variable">
        /// The variable to be removed. It is removed based on the name of the variable.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        public void Remove(PSVariable variable)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariable(variable);
        }

        /// <summary>
        /// Removes the specified variable from the specified scope.
        /// </summary>
        /// <param name="name">
        /// The name of the variable to remove.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to do the lookup in. The ID is a zero based index
        /// of the scope tree with the current scope being zero, its parent scope
        /// being 1 and so on.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If <paramref name="name"/> refers to an MSH path (not a variable)
        /// and the provider throws an exception.
        /// </exception>
        internal void RemoveAtScope(string name, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariableAtScope(name, scope);
        }

        /// <summary>
        /// Removes the specified variable from the specified scope.
        /// </summary>
        /// <param name="variable">
        /// The variable to be removed. It is removed based on the name of the variable.
        /// </param>
        /// <param name="scope">
        /// The ID of the scope to do the lookup in. The ID is a zero based index
        /// of the scope tree with the current scope being zero, its parent scope
        /// being 1 and so on.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="variable"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="scopeID"/> is less than zero or greater than the number of currently
        /// active scopes.
        /// </exception>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// if the variable is constant.
        /// </exception>
        internal void RemoveAtScope(PSVariable variable, string scope)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveVariableAtScope(variable, scope);
        }

        #endregion Public methods

        #region private data

        private SessionStateInternal _sessionState;

        #endregion private data
    }
}

