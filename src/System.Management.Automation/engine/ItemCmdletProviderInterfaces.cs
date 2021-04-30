// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the Item noun of the Cmdlet Providers to the Cmdlet base class. The methods of this class
    /// use the providers to perform operations.
    /// </summary>
    public sealed class ItemCmdletProviderIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private ItemCmdletProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Constructs a facade over the "real" session state API.
        /// </summary>
        /// <param name="cmdlet">
        /// An instance of the cmdlet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="cmdlet"/> is null.
        /// </exception>
        internal ItemCmdletProviderIntrinsics(Cmdlet cmdlet)
        {
            if (cmdlet == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(cmdlet));
            }

            _cmdlet = cmdlet;
            _sessionState = cmdlet.Context.EngineSessionState;
        }

        /// <summary>
        /// Constructs a facade over the "real" session state API.
        /// </summary>
        /// <param name="sessionState">
        /// An instance of the "real" session state class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal ItemCmdletProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }

        #endregion Constructors

        #region Public methods

        #region GetItem

        /// <summary>
        /// Gets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <returns>
        /// The object(s) at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Get(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetItem(new string[] { path }, false, false);
        }

        /// <summary>
        /// Gets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to retrieve. They may be a drive or provider-qualified path(s) and may include
        /// glob characters.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The object(s) at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Get(string[] path, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetItem(path, force, literalPath);
        }

        /// <summary>
        /// Gets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The object(s) at the specified path are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Get(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetItem(new string[] { path }, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object GetItemDynamicParameters(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetItemDynamicParameters(path, context);
        }

        #endregion GetItem

        #region SetItem

        /// <summary>
        /// Sets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="value">
        /// The new value to set the item to.
        /// </param>
        /// <returns>
        /// The object(s) set at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Set(string path, object value)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetItem(new string[] { path }, value, false, false);
        }

        /// <summary>
        /// Sets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to set. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="value">
        /// The new value to set the item to.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The object(s) set at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Set(string[] path, object value, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetItem(path, value, force, literalPath);
        }

        /// <summary>
        /// Sets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="value">
        /// The new value to set the item to.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The object(s) set at the specified path are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Set(string path, object value, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.SetItem(new string[] { path }, value, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="value">
        /// The new value of the item at the specified path.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object SetItemDynamicParameters(
            string path,
            object value,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.SetItemDynamicParameters(path, value, context);
        }

        #endregion SetItem

        #region ClearItem

        /// <summary>
        /// Clears the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to clear. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <returns>
        /// The object(s) cleared at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Clear(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ClearItem(new string[] { path }, false, false);
        }

        /// <summary>
        /// Clears the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item to clear. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The object(s) cleared at the specified path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Clear(string[] path, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ClearItem(path, force, literalPath);
        }

        /// <summary>
        /// Clears the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to be cleared. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The object(s) cleared at the specified path are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Clear(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.ClearItem(new string[] { path }, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object ClearItemDynamicParameters(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ClearItemDynamicParameters(path, context);
        }

        #endregion ClearItem

        #region InvokeDefaultAction

        /// <summary>
        /// Invokes the default action of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to invoke. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Invoke(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.InvokeDefaultAction(new string[] { path }, false);
        }

        /// <summary>
        /// Invokes the default action of the item(s) at the specified path(s).
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to invoke. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Invoke(string[] path, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.InvokeDefaultAction(path, literalPath);
        }

        /// <summary>
        /// Invokes the default action for the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to be invoked. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Invoke(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.InvokeDefaultAction(new string[] { path }, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the invoke-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object InvokeItemDynamicParameters(string path, CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.InvokeDefaultActionDynamicParameters(path, context);
        }

        #endregion InvokeDefaultAction

        #region RenameItem

        /// <summary>
        /// Renames the item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
        /// </param>
        /// <returns>
        /// The item(s) that were renamed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Rename(string path, string newName)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenameItem(path, newName, false);
        }

        /// <summary>
        /// Renames the item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <returns>
        /// The item(s) that were renamed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Rename(string path, string newName, bool force)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenameItem(path, newName, force);
        }

        /// <summary>
        /// Renames the item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing.  The item(s) that get renamed are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Rename(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RenameItem(path, newName, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object RenameItemDynamicParameters(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RenameItemDynamicParameters(path, newName, context);
        }

        #endregion RenameItem

        #region NewItem

        /// <summary>
        /// Creates a new item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the container to create item in. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="name">
        /// The name of the new item to create.
        /// </param>
        /// <param name="itemTypeName">
        /// The type of the new item to create.
        /// </param>
        /// <param name="content">
        /// The content of the new item to create.
        /// </param>
        /// <returns>
        /// The item that was created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> New(
            string path,
            string name,
            string itemTypeName,
            object content)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewItem(new string[] { path }, name, itemTypeName, content, false);
        }

        /// <summary>
        /// Creates a new item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the container to create item in. They may be drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="name">
        /// The name of the new item to create.
        /// </param>
        /// <param name="itemTypeName">
        /// The type of the new item to create.
        /// </param>
        /// <param name="content">
        /// The content of the new item to create.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <returns>
        /// The item(s) that was created.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> New(
            string[] path,
            string name,
            string itemTypeName,
            object content,
            bool force)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewItem(path, name, itemTypeName, content, force);
        }

        /// <summary>
        /// Creates a new item at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the container to create item in. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="name">
        /// The name of the new item to create.
        /// </param>
        /// <param name="type">
        /// The type of the new item to create.
        /// </param>
        /// <param name="content">
        /// The content of the new item to create.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing.  The new item is written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void New(
            string path,
            string name,
            string type,
            object content,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.NewItem(new string[] { path }, name, type, content, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="type">
        /// The type of the new item to create.
        /// </param>
        /// <param name="content">
        /// The content of the new item to create.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object NewItemDynamicParameters(
            string path,
            string type,
            object content,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.NewItemDynamicParameters(path, type, content, context);
        }

        #endregion NewItem

        #region RemoveItem

        /// <summary>
        /// Removes the items at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to remove. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, removes all the children in all the sub-containers of the specified
        /// container. If false, only removes the immediate children of the specified
        /// container.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Remove(string path, bool recurse)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveItem(new string[] { path }, recurse, false, false);
        }

        /// <summary>
        /// Removes the items at the given path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to remove. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, removes all the children in all the sub-containers of the specified
        /// container. If false, only removes the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public void Remove(string[] path, bool recurse, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveItem(path, recurse, force, literalPath);
        }

        /// <summary>
        /// Removes the items at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to remove. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, removes all the children in all the sub-containers of the specified
        /// container. If false, only removes the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Remove(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.RemoveItem(new string[] { path }, recurse, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true, removes all the children in all the sub-containers of the specified
        /// container. If false, only removes the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object RemoveItemDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.RemoveItemDynamicParameters(path, recurse, context);
        }

        #endregion RemoveItem

        #region CopyItem

        /// <summary>
        /// Copy item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to copy. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="destinationPath">
        /// The path to copy the item to.
        /// </param>
        /// <param name="recurse">
        /// If true, copies all the children in all the sub-containers of the specified
        /// container. If false, only copies the specified item.
        /// </param>
        /// <param name="copyContainers">
        /// Determines how the source container is used in the copy operation.
        /// </param>
        /// <returns>
        /// The item(s) that were copied.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Copy(
            string path,
            string destinationPath,
            bool recurse,
            CopyContainers copyContainers)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.CopyItem(new string[] { path }, destinationPath, recurse, copyContainers, false, false);
        }

        /// <summary>
        /// Copy item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to copy. They may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="destinationPath">
        /// The path to copy the item to.
        /// </param>
        /// <param name="recurse">
        /// If true, copies all the children in all the sub-containers of the specified
        /// container. If false, only copies the specified item.
        /// </param>
        /// <param name="copyContainers">
        /// Determines how the source container is used in the copy operation.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The item(s) that were copied.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Copy(
            string[] path,
            string destinationPath,
            bool recurse,
            CopyContainers copyContainers,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.CopyItem(path, destinationPath, recurse, copyContainers, force, literalPath);
        }

        /// <summary>
        /// Copy item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to copy. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="destinationPath">
        /// The path to copy the item to.
        /// </param>
        /// <param name="recurse">
        /// If true, copies all the children in all the sub-containers of the specified
        /// container. If false, only copies the specified item.
        /// </param>
        /// <param name="copyContainers">
        /// Determines how the source container is used in the copy operation.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The item(s) that were copied are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Copy(
            string path,
            string destinationPath,
            bool recurse,
            CopyContainers copyContainers,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.CopyItem(new string[] { path }, destinationPath, recurse, copyContainers, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="destination">
        /// The path to copy the item to.
        /// </param>
        /// <param name="recurse">
        /// If true, copies all the children in all the sub-containers of the specified
        /// container. If false, only copies the specified item.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.CopyItemDynamicParameters(path, destination, recurse, context);
        }

        #endregion CopyItem

        #region MoveItem

        /// <summary>
        /// Moves the item at the specified path to the specified destination.
        /// </summary>
        /// <param name="path">
        /// The path to the item to move.
        /// </param>
        /// <param name="destination">
        /// The path to the location that the item will be moved.
        /// </param>
        /// <returns>
        /// The item(s) that were moved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destination"/> resolves to multiple paths.
        /// or
        /// If <paramref name="destination"/> and <paramref name="path"/> don't resolve
        /// to the same provider.
        /// or
        /// If <paramref name="path"/> resolves to multiple paths and <paramref name="destination"/>
        /// is not a container.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Move(string path, string destination)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MoveItem(new string[] { path }, destination, false, false);
        }

        /// <summary>
        /// Moves the item at the specified path to the specified destination.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item to move.
        /// </param>
        /// <param name="destination">
        /// The path to the location that the item will be moved.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The item(s) that were moved.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destination"/> resolves to multiple paths.
        /// or
        /// If <paramref name="destination"/> and <paramref name="path"/> don't resolve
        /// to the same provider.
        /// or
        /// If <paramref name="path"/> resolves to multiple paths and <paramref name="destination"/>
        /// is not a container.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public Collection<PSObject> Move(string[] path, string destination, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MoveItem(path, destination, force, literalPath);
        }

        /// <summary>
        /// Moves the item at the specified path to the specified destination.
        /// </summary>
        /// <param name="path">
        /// The path to the item to move.
        /// </param>
        /// <param name="destination">
        /// The path to the location that the item will be moved.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The object that is moved is written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void Move(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.MoveItem(new string[] { path }, destination, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the move-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="destination">
        /// The path to move the item to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object MoveItemDynamicParameters(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.MoveItemDynamicParameters(path, destination, context);
        }

        #endregion MoveItem

        #region Exists

        /// <summary>
        /// Determines if an item at the given path exits.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it exists. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <returns>
        /// True if the item at the specified path exists. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public bool Exists(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ItemExists(path, false, false);
        }

        /// <summary>
        /// Determines if an item at the given path exits.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it exists. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// True if the item at the specified path exists. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public bool Exists(string path, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ItemExists(path, force, literalPath);
        }

        /// <summary>
        /// Determines if an item at the given path exits.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it exists. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// True if the item at the specified path exists. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal bool Exists(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ItemExists(path, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the test-path cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal object ItemExistsDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.ItemExistsDynamicParameters(path, context);
        }
        #endregion ItemExists

        #region IsContainer

        /// <summary>
        /// Determines if the specified path is to an item that is a container.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it is a container.
        /// </param>
        /// <returns>
        /// True if the path is to an item that is a container. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        public bool IsContainer(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsItemContainer(path);
        }

        /// <summary>
        /// Determines if the specified path is to an item that is a container.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it is a container.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// True if the path is to an item that is a container. False otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal bool IsContainer(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.IsItemContainer(path, context);
        }

        #endregion IsItemContainer

        #endregion Public methods

        #region private data

        private readonly Cmdlet _cmdlet;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }

    /// <summary>
    /// Determines how the source container of a copy operation
    /// will be used.
    /// </summary>
    public enum CopyContainers
    {
        /// <summary>
        /// The source container is copied.
        /// </summary>
        CopyTargetContainer,

        /// <summary>
        /// The children of the source container are copied.
        /// </summary>
        CopyChildrenOfTargetContainer
    }
}
