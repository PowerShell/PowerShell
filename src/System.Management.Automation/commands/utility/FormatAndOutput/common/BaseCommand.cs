/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

namespace Microsoft.PowerShell.Commands.Internal.Format
{

    /// <summary>
    /// facade class to provide context information to process
    /// exceptions
    /// </summary>
    internal sealed class TerminatingErrorContext
    {
        internal TerminatingErrorContext (PSCmdlet command)
        {
            if (command == null)
                throw PSTraceSource.NewArgumentNullException ("command");
            _command = command;
        }

        internal void ThrowTerminatingError (ErrorRecord errorRecord)
        {
            _command.ThrowTerminatingError (errorRecord);
        }

        private PSCmdlet _command;
    }


    /// <summary>
    /// helper class to invoke a command in a secondary pipeline.
    /// NOTE: this implementation does not return any error messages
    /// that invoked pipelines might generate
    /// </summary>
    internal sealed class CommandWrapper : IDisposable
    {
        /// <summary>
        /// Initialize the command before executing
        /// </summary>
        /// <param name="execContext">ExecutionContext used to create sub pipeline</param>
        /// <param name="nameOfCommand">name of the command to run</param>
        /// <param name="typeOfCommand">Type of the command to run</param>
        internal void Initialize (ExecutionContext execContext, string nameOfCommand, Type typeOfCommand)
        {
            context = execContext;
            commandName = nameOfCommand;
            commandType = typeOfCommand;
        }

        /// <summary>
        /// add a parameter to the command invocation.
        /// It needs to be called before any execution takes place
        /// </summary>
        /// <param name="parameterName">name of the parameter</param>
        /// <param name="parameterValue">value of the parameter</param>
        internal void AddNamedParameter (string parameterName, object parameterValue)
        {
            commandParameterList.Add(
                CommandParameterInternal.CreateParameterWithArgument(
                    PositionUtilities.EmptyExtent, parameterName, null,
                    PositionUtilities.EmptyExtent, parameterValue,
                    false));
        }


        /// <summary>
        /// send an object to the pipeline
        /// </summary>
        /// <param name="o">object to process</param>
        /// <returns>Array of objects out of the success pipeline</returns>
        internal Array Process (object o)
        {
            if (this.pp == null)
            {
                // if this is the first call, we need to initialize the
                // pipeline underneath
                DelayedInternalInitialize ();
            }

            return this.pp.Step (o);
        }

        /// <summary>
        /// shut down the pipeline
        /// </summary>
        /// <returns>Array of objects out of the success pipeline</returns>
        internal Array ShutDown ()
        {
            if (this.pp == null)
            {
                // if Process() never got called, no sub pipeline
                // ever got created, hence we just return an empty array
                return new object[0];
            }

            PipelineProcessor ppTemp = this.pp;

            this.pp = null;
            return ppTemp.SynchronousExecuteEnumerate(AutomationNull.Value);
        }

        private void DelayedInternalInitialize ()
        {
            this.pp = new PipelineProcessor ();

            CmdletInfo cmdletInfo = new CmdletInfo(this.commandName, this.commandType, null, null, this.context);

            CommandProcessor cp = new CommandProcessor (cmdletInfo, this.context);

            foreach (CommandParameterInternal par in this.commandParameterList)
            {
                cp.AddParameter(par);
            }

            this.pp.Add (cp);
        }

        /// <summary>
        /// just dispose the pipeline processor
        /// </summary>
        public void Dispose()
        {
            if (this.pp == null)
                return;

            this.pp.Dispose ();
            this.pp = null;
        }

        private PipelineProcessor pp = null;

        private string commandName = null;
        private Type commandType;
        private List<CommandParameterInternal> commandParameterList = new List<CommandParameterInternal>();

        private ExecutionContext context = null;
    }

    /// <summary>
    /// base class for the command-let's we expose
    /// it contains a reference to the implementation
    /// class it wraps
    /// </summary>
    public abstract class FrontEndCommandBase : PSCmdlet, IDisposable
    {
        #region Command Line Switches
        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter (ValueFromPipeline = true)]
        public PSObject InputObject
        {
            set { _inputObject = value; }
            get { return _inputObject; }
        }
        private PSObject _inputObject = AutomationNull.Value;
        
        #endregion

        /// <summary>
        /// hook up the calls from the implementation object
        /// and then call the implementation's Begin()
        /// </summary>
        protected override void BeginProcessing ()
        {
            Diagnostics.Assert (this.implementation != null, "this.implementation is null");
            this.implementation.OuterCmdletCall = new ImplementationCommandBase.OuterCmdletCallback (this.OuterCmdletCall);
            this.implementation.InputObjectCall = new ImplementationCommandBase.InputObjectCallback (this.InputObjectCall);
            this.implementation.WriteObjectCall = new ImplementationCommandBase.WriteObjectCallback (this.WriteObjectCall);
            
            this.implementation.CreateTerminatingErrorContext ();
            
            implementation.BeginProcessing ();
        }

        /// <summary>
        /// call the implementation
        /// </summary>
        protected override void ProcessRecord ()
        {
            implementation.ProcessRecord ();
        }

        /// <summary>
        /// call the implementation
        /// </summary>
        protected override void EndProcessing ()
        {
            implementation.EndProcessing ();
        }

        /// <summary>
        /// call the implementation
        /// </summary>
        protected override void StopProcessing()
        {
            implementation.StopProcessing();
        }

        /// <summary>
        /// callback for the implementation to obtain a reference to the Cmdlet object
        /// </summary>
        /// <returns>Cmdlet reference</returns>
        protected virtual PSCmdlet OuterCmdletCall ()
        {
            return this;
        }

        /// <summary>
        /// callback for the implementation to get the current pipeline object
        /// </summary>
        /// <returns>current object from the pipeline</returns>
        protected virtual PSObject InputObjectCall ()
        {
            // just bind to the input object parameter
            return this.InputObject;
        }

        /// <summary>
        /// callback for the implementation to write objects
        /// </summary>
        /// <param name="value">object to be written</param>
        protected virtual void WriteObjectCall (object value)
        {
            // just call Monad API
            this.WriteObject(value);
        }

        /// <summary>
        /// reference to the implementation command that this class
        /// is wrapping
        /// </summary>
        internal ImplementationCommandBase implementation = null;


        #region IDisposable Implementation

        /// <summary>
        /// default implementation just delegates to internal helper
        /// </summary>
        /// <remarks>This method calls GC.SuppressFinalize</remarks>
        public void Dispose ()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation
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
        /// Do-nothing implementation: derived classes will override as see fit
        /// </summary>
        protected virtual void InternalDispose ()
        {
            if (this.implementation == null)
                return;

            this.implementation.Dispose ();
            this.implementation = null;
        }
        #endregion
    }

    /// <summary>
    /// implementation class to be called by the outer command
    /// In order to properly work, the callbacks have to be properly set by the outer command
    /// </summary>
    internal class ImplementationCommandBase : IDisposable
    {
        /// <summary>
        /// inner version of CommandBase.BeginProcessing()
        /// </summary>
        internal virtual void BeginProcessing ()
        {
        }

        /// <summary>
        /// inner version of CommandBase.ProcessRecord()
        /// </summary>
        internal virtual void ProcessRecord ()
        {
        }

        /// <summary>
        /// inner version of CommandBase.EndProcessing()
        /// </summary>
        internal virtual void EndProcessing ()
        {
        }

        /// <summary>
        /// inner version of CommandBase.StopProcessing()
        /// </summary>
        internal virtual void StopProcessing()
        {
        }

        /// <summary>
        /// retrieve the current input pipeline object
        /// </summary>
        internal virtual PSObject ReadObject ()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert (this.InputObjectCall != null, "this.InputObjectCall is null");
            return this.InputObjectCall ();
        }

        /// <summary>
        /// write an object to the pipeline
        /// </summary>
        /// <param name="o">object to write to the pipeline</param>
        internal virtual void WriteObject (object o)
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert (this.WriteObjectCall != null, "this.WriteObjectCall is null");
            this.WriteObjectCall (o);
        }

        // callback methods to get to the outer Monad Cmdlet
        /// <summary>
        /// get a hold of the Monad outer Cmdlet
        /// </summary>
        /// <returns></returns>
        internal virtual PSCmdlet OuterCmdlet ()
        {
            // delegate to the front end object
            System.Diagnostics.Debug.Assert (this.OuterCmdletCall != null, "this.OuterCmdletCall is null");
            return this.OuterCmdletCall ();
        }

        protected TerminatingErrorContext TerminatingErrorContext
        {
            get
            {
                return _terminatingErrorContext;
            }
        }

        internal void CreateTerminatingErrorContext()
        {
            _terminatingErrorContext = new TerminatingErrorContext (this.OuterCmdlet ());
        }

        private TerminatingErrorContext _terminatingErrorContext;

        /// <summary>
        /// delegate definition to get to the outer command-let
        /// </summary>
        internal delegate PSCmdlet OuterCmdletCallback ();

        /// <summary>
        /// callback to get to the outer command-let
        /// </summary>
        internal OuterCmdletCallback OuterCmdletCall;

        // callback to the methods to get an object and write an object
        /// <summary>
        /// delegate definition to get to the current pipeline input object
        /// </summary>
        internal delegate PSObject InputObjectCallback ();

        /// <summary>
        /// delegate definition to write object
        /// </summary>
        internal delegate void WriteObjectCallback (object o);

        /// <summary>
        /// callback to read object
        /// </summary>
        internal InputObjectCallback InputObjectCall;

        /// <summary>
        /// callback to write object
        /// </summary>
        internal WriteObjectCallback WriteObjectCall;


        #region IDisposable Implementation

        /// <summary>
        /// default implementation just delegates to internal helper
        /// </summary>
        /// <remarks>This method calls GC.SuppressFinalize</remarks>
        public void Dispose ()
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
        /// Do-nothing implementation: derived classes will override as see fit
        /// </summary>
        protected virtual void InternalDispose ()
        {
        }
        #endregion

    }
}

