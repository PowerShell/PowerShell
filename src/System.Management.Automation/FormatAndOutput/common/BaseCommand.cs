// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Facade class to provide context information to process
    /// exceptions.
    /// </summary>
    internal sealed class TerminatingErrorContext
    {
        internal TerminatingErrorContext(PSCmdlet command)
        {
            if (command == null)
                throw PSTraceSource.NewArgumentNullException(nameof(command));
            _command = command;
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        internal void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            _command.ThrowTerminatingError(errorRecord);
        }

        private readonly PSCmdlet _command;
    }

    /// <summary>
    /// Helper class to invoke a command in a secondary pipeline.
    /// NOTE: this implementation does not return any error messages
    /// that invoked pipelines might generate.
    /// </summary>
    internal sealed class CommandWrapper : IDisposable
    {
        /// <summary>
        /// Initialize the command before executing.
        /// </summary>
        /// <param name="execContext">ExecutionContext used to create sub pipeline.</param>
        /// <param name="nameOfCommand">Name of the command to run.</param>
        /// <param name="typeOfCommand">Type of the command to run.</param>
        internal void Initialize(ExecutionContext execContext, string nameOfCommand, Type typeOfCommand)
        {
            _context = execContext;
            _commandName = nameOfCommand;
            _commandType = typeOfCommand;
        }

        /// <summary>
        /// Add a parameter to the command invocation.
        /// It needs to be called before any execution takes place.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">Value of the parameter.</param>
        internal void AddNamedParameter(string parameterName, object parameterValue)
        {
            _commandParameterList.Add(
                CommandParameterInternal.CreateParameterWithArgument(
                    /*parameterAst*/null, parameterName, null,
                    /*argumentAst*/null, parameterValue,
                    false));
        }

        /// <summary>
        /// Send an object to the pipeline.
        /// </summary>
        /// <param name="o">Object to process.</param>
        /// <returns>Array of objects out of the success pipeline.</returns>
        internal Array Process(object o)
        {
            if (_pp == null)
            {
                // if this is the first call, we need to initialize the
                // pipeline underneath
                DelayedInternalInitialize();
            }

            return _pp.Step(o);
        }

        /// <summary>
        /// Shut down the pipeline.
        /// </summary>
        /// <returns>Array of objects out of the success pipeline.</returns>
        internal Array ShutDown()
        {
            if (_pp == null)
            {
                // if Process() never got called, no sub pipeline
                // ever got created, hence we just return an empty array
                return Array.Empty<object>();
            }

            PipelineProcessor ppTemp = _pp;

            _pp = null;
            return ppTemp.SynchronousExecuteEnumerate(AutomationNull.Value);
        }

        private void DelayedInternalInitialize()
        {
            _pp = new PipelineProcessor();

            CmdletInfo cmdletInfo = new CmdletInfo(_commandName, _commandType, null, null, _context);

            CommandProcessor cp = new CommandProcessor(cmdletInfo, _context);

            foreach (CommandParameterInternal par in _commandParameterList)
            {
                cp.AddParameter(par);
            }

            _pp.Add(cp);
        }

        /// <summary>
        /// Just dispose the pipeline processor.
        /// </summary>
        public void Dispose()
        {
            if (_pp == null)
                return;

            _pp.Dispose();
            _pp = null;
        }

        private PipelineProcessor _pp = null;

        private string _commandName = null;
        private Type _commandType;
        private readonly List<CommandParameterInternal> _commandParameterList = new List<CommandParameterInternal>();

        private ExecutionContext _context = null;
    }

    /// <summary>
    /// Base class for the command-let's we expose
    /// it contains a reference to the implementation
    /// class it wraps.
    /// </summary>
    public abstract class FrontEndCommandBase : PSCmdlet, IDisposable
    {
        #region Command Line Switches
        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        #endregion

        /// <summary>
        /// Hook up the calls from the implementation object
        /// and then call the implementation's Begin()
        /// </summary>
        protected override void BeginProcessing()
        {
            Diagnostics.Assert(this.implementation != null, "this.implementation is null");
            this.implementation.OuterCmdletCall = new ImplementationCommandBase.OuterCmdletCallback(this.OuterCmdletCall);
            this.implementation.InputObjectCall = new ImplementationCommandBase.InputObjectCallback(this.InputObjectCall);
            this.implementation.WriteObjectCall = new ImplementationCommandBase.WriteObjectCallback(this.WriteObjectCall);

            this.implementation.CreateTerminatingErrorContext();

            implementation.BeginProcessing();
        }

        /// <summary>
        /// Call the implementation.
        /// </summary>
        protected override void ProcessRecord()
        {
            implementation.ProcessRecord();
        }

        /// <summary>
        /// Call the implementation.
        /// </summary>
        protected override void EndProcessing()
        {
            implementation.EndProcessing();
        }

        /// <summary>
        /// Call the implementation.
        /// </summary>
        protected override void StopProcessing()
        {
            implementation.StopProcessing();
        }

        /// <summary>
        /// Callback for the implementation to obtain a reference to the Cmdlet object.
        /// </summary>
        /// <returns>Cmdlet reference.</returns>
        protected virtual PSCmdlet OuterCmdletCall()
        {
            return this;
        }

        /// <summary>
        /// Callback for the implementation to get the current pipeline object.
        /// </summary>
        /// <returns>Current object from the pipeline.</returns>
        protected virtual PSObject InputObjectCall()
        {
            // just bind to the input object parameter
            return this.InputObject;
        }

        /// <summary>
        /// Callback for the implementation to write objects.
        /// </summary>
        /// <param name="value">Object to be written.</param>
        protected virtual void WriteObjectCall(object value)
        {
            // just call Monad API
            this.WriteObject(value);
        }

        /// <summary>
        /// Reference to the implementation command that this class
        /// is wrapping.
        /// </summary>
        internal ImplementationCommandBase implementation = null;

        #region IDisposable Implementation

        /// <summary>
        /// Default implementation just delegates to internal helper.
        /// </summary>
        /// <remarks>This method calls GC.SuppressFinalize</remarks>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalDispose();
            }
        }

        /// <summary>
        /// Do-nothing implementation: derived classes will override as see fit.
        /// </summary>
        protected virtual void InternalDispose()
        {
            if (this.implementation == null)
                return;

            this.implementation.Dispose();
            this.implementation = null;
        }
        #endregion
    }

    /// <summary>
    /// Implementation class to be called by the outer command
    /// In order to properly work, the callbacks have to be properly set by the outer command.
    /// </summary>
    internal class ImplementationCommandBase : IDisposable
    {
        /// <summary>
        /// Inner version of CommandBase.BeginProcessing()
        /// </summary>
        internal virtual void BeginProcessing()
        {
        }

        /// <summary>
        /// Inner version of CommandBase.ProcessRecord()
        /// </summary>
        internal virtual void ProcessRecord()
        {
        }

        /// <summary>
        /// Inner version of CommandBase.EndProcessing()
        /// </summary>
        internal virtual void EndProcessing()
        {
        }

        /// <summary>
        /// Inner version of CommandBase.StopProcessing()
        /// </summary>
        internal virtual void StopProcessing()
        {
        }

        /// <summary>
        /// Retrieve the current input pipeline object.
        /// </summary>
        internal virtual PSObject ReadObject()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.InputObjectCall != null, "this.InputObjectCall is null");
            return this.InputObjectCall();
        }

        /// <summary>
        /// Write an object to the pipeline.
        /// </summary>
        /// <param name="o">Object to write to the pipeline.</param>
        internal virtual void WriteObject(object o)
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.WriteObjectCall != null, "this.WriteObjectCall is null");
            this.WriteObjectCall(o);
        }

        // callback methods to get to the outer Monad Cmdlet
        /// <summary>
        /// Get a hold of the Monad outer Cmdlet.
        /// </summary>
        /// <returns></returns>
        internal virtual PSCmdlet OuterCmdlet()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert(this.OuterCmdletCall != null, "this.OuterCmdletCall is null");
            return this.OuterCmdletCall();
        }

        protected TerminatingErrorContext TerminatingErrorContext { get; private set; }

        internal void CreateTerminatingErrorContext()
        {
            TerminatingErrorContext = new TerminatingErrorContext(this.OuterCmdlet());
        }

        /// <summary>
        /// Delegate definition to get to the outer command-let.
        /// </summary>
        internal delegate PSCmdlet OuterCmdletCallback();

        /// <summary>
        /// Callback to get to the outer command-let.
        /// </summary>
        internal OuterCmdletCallback OuterCmdletCall;

        // callback to the methods to get an object and write an object
        /// <summary>
        /// Delegate definition to get to the current pipeline input object.
        /// </summary>
        internal delegate PSObject InputObjectCallback();

        /// <summary>
        /// Delegate definition to write object.
        /// </summary>
        internal delegate void WriteObjectCallback(object o);

        /// <summary>
        /// Callback to read object.
        /// </summary>
        internal InputObjectCallback InputObjectCall;

        /// <summary>
        /// Callback to write object.
        /// </summary>
        internal WriteObjectCallback WriteObjectCall;

        #region IDisposable Implementation

        /// <summary>
        /// Default implementation just delegates to internal helper.
        /// </summary>
        /// <remarks>This method calls GC.SuppressFinalize</remarks>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                InternalDispose();
            }
        }

        /// <summary>
        /// Do-nothing implementation: derived classes will override as see fit.
        /// </summary>
        protected virtual void InternalDispose()
        {
        }
        #endregion

    }
}
