// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the Children noun of the Cmdlet Providers to the Cmdlet base class. The methods of this class
    /// use the providers to perform operations.
    /// </summary>
    public sealed class ChildItemCmdletProviderIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of SessionState.
        /// </summary>
        private ChildItemCmdletProviderIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of SessionState should be called.");
        }

        /// <summary>
        /// Constructs a facade over the "real" session state API.
        /// </summary>
        /// <param name="cmdlet">
        /// An instance of the cmdlet that this class is acting as a facade for.
        /// </param>
        internal ChildItemCmdletProviderIntrinsics(Cmdlet cmdlet)
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
        /// An instance of the "real" session state.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sessionState"/> is null.
        /// </exception>
        internal ChildItemCmdletProviderIntrinsics(SessionStateInternal sessionState)
        {
            if (sessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
            }

            _sessionState = sessionState;
        }
        #endregion Constructors

        #region Public methods

        #region GetChildItems

        /// <summary>
        /// Gets the child items of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        public Collection<PSObject> Get(string path, bool recurse)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItems(new string[] { path }, recurse, uint.MaxValue, false, false);
        }

        /// <summary>
        /// Gets the child items of the container at the given path(s).
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to retrieve. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        public Collection<PSObject> Get(string[] path, bool recurse, uint depth, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItems(path, recurse, depth, force, literalPath);
        }

        /// <summary>
        /// Gets the child items of the container at the given path(s).
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to retrieve. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        public Collection<PSObject> Get(string[] path, bool recurse, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return this.Get(path, recurse, uint.MaxValue, force, literalPath);
        }

        /// <summary>
        /// Gets the child items of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing. The children of the container at the specified path are written to the context.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        internal void Get(
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetChildItems(path, recurse, depth, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
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
        internal object GetChildItemsDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildItemsDynamicParameters(path, recurse, context);
        }

        #endregion GetChildItems

        #region GetChildNames

        /// <summary>
        /// Gets the child names of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
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
        public Collection<string> GetNames(
            string path,
            ReturnContainers returnContainers,
            bool recurse)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildNames(new string[] { path }, returnContainers, recurse, uint.MaxValue, false, false);
        }

        /// <summary>
        /// Gets the child names of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to retrieve. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
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
        public Collection<string> GetNames(
            string[] path,
            ReturnContainers returnContainers,
            bool recurse,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.GetChildNames(path, returnContainers, recurse, uint.MaxValue, force, literalPath);
        }

        /// <summary>
        /// Gets the child names of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path(s) to the item(s) to retrieve. They may be drive or provider-qualified paths and may include
        /// glob characters.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The children of the container at the specified path. The type of the objects returned are
        /// determined by the provider that supports the given path.
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
        public Collection<string> GetNames(
            string[] path,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            bool force,
            bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            return _sessionState.GetChildNames(path, returnContainers, recurse, depth, force, literalPath);
        }

        /// <summary>
        /// Gets the child names of the container at the given path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// Nothing.  The names of the children of the specified container are written to the context.
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
        internal void GetNames(
            string path,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            _sessionState.GetChildNames(path, returnContainers, recurse, depth, context);
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem -name cmdlet.
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
        internal object GetChildNamesDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.GetChildNamesDynamicParameters(path, context);
        }

        #endregion GetChildNames

        #region HasChildItems

        /// <summary>
        /// Determines if an item at the given path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it has children. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <returns>
        /// True if the item at the specified path has children. False otherwise.
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
        public bool HasChild(string path)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, false, false);
        }

        /// <summary>
        /// Determines if an item at the given path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it has children. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// True if the item at the specified path has children. False otherwise.
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
        public bool HasChild(string path, bool force, bool literalPath)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, force, literalPath);
        }

        /// <summary>
        /// Determines if an item at the given path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it has children. It may be a drive or provider-qualified path and may include
        /// glob characters.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// True if the item at the specified path has children. False otherwise.
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
        internal bool HasChild(
            string path,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                _sessionState != null,
                "The only constructor for this class should always set the sessionState field");

            // Parameter validation is done in the session state object

            return _sessionState.HasChildItems(path, context);
        }

        #endregion HasChildItems

        #endregion Public methods

        #region private data

        private readonly Cmdlet _cmdlet;
        private readonly SessionStateInternal _sessionState;

        #endregion private data
    }

    /// <summary>
    /// This enum determines which types of containers are returned from some of
    /// the provider methods.
    /// </summary>
    public enum ReturnContainers
    {
        /// <summary>
        /// Only containers that match the filter(s) are returned.
        /// </summary>
        ReturnMatchingContainers,

        /// <summary>
        /// All containers are returned even if they don't match the filter(s).
        /// </summary>
        ReturnAllContainers
    }
}
