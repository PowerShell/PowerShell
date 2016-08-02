/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation
{
    /// <summary>
    /// Command factory provides a generic interface to create different types of commands.
    /// </summary>
    internal class CommandFactory
    {
        #region private_members

        private ExecutionContext _context;

        #endregion private_members

        #region public_properties

        /// <summary>
        /// Execution context under which the command should be created.
        /// </summary>
        internal ExecutionContext Context
        {
            get { return _context; }
            set { _context = value; }
        }

        #endregion public_properties

        #region ctor

        /// <summary>
        /// Initializes the new instance of CommandFactory class.
        /// </summary>
        internal CommandFactory()
        {
        }

        /// <summary>
        /// Initializes the new instance of CommandFactory class.
        /// </summary>
        /// <param name="context">Execution context.</param>
        internal CommandFactory(ExecutionContext context)
        {
            this.Context = context;
        }

        #endregion ctor

        #region public_methods

        /// <summary>
        /// Creates a command object corresponing to specified name. The command processor will use global scope.
        /// </summary>
        /// <param name="commandName">Creates a command object corresponing to specified name.</param>
        /// <param name="commandOrigin"> Location where the command was dispatched from. </param>
        /// <returns>Created command processor object.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if session state does not contain the CommandDiscovery instance.
        /// </exception>
        internal CommandProcessorBase CreateCommand(string commandName, CommandOrigin commandOrigin)
        {
            return _CreateCommand(commandName, commandOrigin, false);
        }

        /// <summary>
        /// Creates a command object corresponing to specified name.
        /// </summary>
        /// <param name="commandName">Creates a command object corresponing to specified name.</param>
        /// <param name="commandOrigin"> Location where the command was dispatched from. </param>
        /// <param name="useLocalScope"> 
        /// True if command processor should use local scope to execute the command,
        /// False otherwise.
        /// </param>
        /// <returns>Created command processor object.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if session state does not contain the CommandDiscovery instance.
        /// </exception>
        internal CommandProcessorBase CreateCommand(string commandName,
            CommandOrigin commandOrigin, bool? useLocalScope)
        {
            return _CreateCommand(commandName, commandOrigin, useLocalScope);
        }

        /// <summary>
        /// Creates a command object corresponing to specified name.
        /// </summary>
        /// <param name="commandName">Creates a command object corresponing to specified name.</param>
        /// <param name="executionContext">Execution Context.</param>
        /// <param name="commandOrigin"> Location where the command was dispatched from. </param>
        /// <returns>Created command processor object.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if session state does not contain the CommandDiscovery instance.
        /// </exception>
        internal CommandProcessorBase CreateCommand(string commandName, ExecutionContext executionContext, CommandOrigin commandOrigin)
        {
            this.Context = executionContext;
            return _CreateCommand(commandName, commandOrigin, false);
        }
        #endregion public_methods

        #region helper_methods

        private CommandProcessorBase _CreateCommand(string commandName,
            CommandOrigin commandOrigin, bool? useLocalScope)
        {
            if (_context == null)
            {
                throw PSTraceSource.NewInvalidOperationException(DiscoveryExceptions.ExecutionContextNotSet);
            }

            // Look for a cmdlet...
            CommandDiscovery discovery = _context.CommandDiscovery;

            if (discovery == null)
            {
                throw PSTraceSource.NewInvalidOperationException(DiscoveryExceptions.CommandDiscoveryMissing);
            }

            // Look for the command using command discovery mechanisms.  This will resolve
            // aliases, functions, filters, cmdlets, scripts, and applications.

            return discovery.LookupCommandProcessor(commandName, commandOrigin, useLocalScope);
        }
        #endregion helper_methods
    }
}

