// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Exposes the Engine APIs for a particular instance of the engine.
    /// </summary>
    public class EngineIntrinsics
    {
        #region Constructors

        /// <summary>
        /// Hide the default constructor since we always require an instance of ExecutionContext.
        /// </summary>
        private EngineIntrinsics()
        {
            Dbg.Diagnostics.Assert(
                false,
                "This constructor should never be called. Only the constructor that takes an instance of ExecutionContext should be called.");
        }

        /// <summary>
        /// The internal constructor for this object. It should be the only one that gets called.
        /// </summary>
        /// <param name="context">
        /// An instance of ExecutionContext that the APIs should work against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        internal EngineIntrinsics(ExecutionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _host = context.EngineHostInterface;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets engine APIs to access the host.
        /// </summary>
        public PSHost Host
        {
            get
            {
                Dbg.Diagnostics.Assert(
                    _host != null,
                    "The only constructor for this class should always set the host field");

                return _host;
            }
        }

        /// <summary>
        /// Gets engine APIs to access the event manager.
        /// </summary>
        public PSEventManager Events
        {
            get
            {
                return _context.Events;
            }
        }

        /// <summary>
        /// Gets the engine APIs to access providers.
        /// </summary>
        public ProviderIntrinsics InvokeProvider
        {
            get
            {
                return _context.EngineSessionState.InvokeProvider;
            }
        }

        /// <summary>
        /// Gets the engine APIs to access session state.
        /// </summary>
        public SessionState SessionState
        {
            get
            {
                return _context.EngineSessionState.PublicSessionState;
            }
        }

        /// <summary>
        /// Gets the engine APIs to invoke a command.
        /// </summary>
        public CommandInvocationIntrinsics InvokeCommand
        {
            get { return _invokeCommand ??= new CommandInvocationIntrinsics(_context); }
        }

        #endregion Public methods

        #region private data

        private readonly ExecutionContext _context;
        private readonly PSHost _host;
        private CommandInvocationIntrinsics _invokeCommand;
        #endregion private data
    }
}
