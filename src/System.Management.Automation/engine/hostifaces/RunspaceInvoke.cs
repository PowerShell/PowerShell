/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Runspaces;

    /// <summary>
    /// Defines a class which allows simple execution of commands from CLR languages
    /// </summary>
    public class RunspaceInvoke : IDisposable
    {
        #region constructors

        /// <summary>
        /// Runspace on which commands are invoked
        /// </summary>
        private Runspace _runspace;

        /// <summary>
        /// Create a RunspaceInvoke for invoking commands. This uses
        /// a runspace with default PSSnapins.
        /// </summary>
        public RunspaceInvoke()
        {
            RunspaceConfiguration rc = RunspaceConfiguration.Create();
            _runspace = RunspaceFactory.CreateRunspace(rc);
            _runspace.Open();
            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = _runspace;
            }
        }

        /// <summary>
        /// Creates a RunspaceInvoke for invoking commands. Underlying Runspace is created using
        /// specified RunspaceConfiguration
        /// </summary>
        /// <param name="runspaceConfiguration">RunspaceConfiguration used for creating the runspace
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when runspaceConfiguration is null
        /// </exception>    
        public RunspaceInvoke(RunspaceConfiguration runspaceConfiguration)
        {
            if (runspaceConfiguration == null)
            {
                throw PSTraceSource.NewArgumentNullException("runspaceConfiguration");
            }
            _runspace = RunspaceFactory.CreateRunspace(runspaceConfiguration);
            _runspace.Open();
            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = _runspace;
            }
        }

        /// <summary>
        /// Creates a RunspaceInvoke for invoking commands. Underlying Runspace is created using the
        /// specified console file.
        /// </summary>
        /// <param name="consoleFilePath">Console file used for creating the underlying 
        /// runspace.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when consoleFilePath is null
        /// </exception>    
        /// <exception cref="PSConsoleLoadException">
        /// Thrown when errors occurs in loading one or more PSSnapins.
        /// </exception>    
        public RunspaceInvoke(string consoleFilePath)
        {
            if (consoleFilePath == null)
            {
                throw PSTraceSource.NewArgumentNullException("consoleFilePath");
            }

            PSConsoleLoadException warnings;
            RunspaceConfiguration rc = RunspaceConfiguration.Create(consoleFilePath, out warnings);
            if (warnings != null)
            {
                throw warnings;
            }
            _runspace = RunspaceFactory.CreateRunspace(rc);
            _runspace.Open();
            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = _runspace;
            }
        }

        /// <summary>
        /// Create RunspaceInvoke for invoking command in specified 
        /// runspace.
        /// </summary>
        /// <param name="runspace"></param>
        /// <remarks>Runspace must be opened state</remarks>
        public RunspaceInvoke(Runspace runspace)
        {
            if (runspace == null)
            {
                throw PSTraceSource.NewArgumentNullException("runspace");
            }
            _runspace = runspace;
            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = _runspace;
            }
        }

        #endregion  constructors

        #region invoke

        /// <summary>
        /// Invoke the specified script
        /// </summary>
        /// <param name="script">msh script to invoke</param>
        /// <returns>Output of invocation</returns>
        public Collection<PSObject> Invoke(string script)
        {
            return Invoke(script, null);
        }

        /// <summary>
        /// Invoke the specified script and passes specified input to the script
        /// </summary>
        /// <param name="script">msh script to invoke</param>
        /// <param name="input">input to script</param>
        /// <returns>Output of invocation</returns>
        public Collection<PSObject> Invoke(string script, IEnumerable input)
        {
            if (_disposed == true)
            {
                throw PSTraceSource.NewObjectDisposedException("runspace");
            }

            if (script == null)
            {
                throw PSTraceSource.NewArgumentNullException("script");
            }
            Pipeline p = _runspace.CreatePipeline(script);
            return p.Invoke(input);
        }

        /// <summary>
        /// Invoke the specified script and passes specified input to the script.
        /// </summary>
        /// <param name="script">msh script to invoke</param>
        /// <param name="input">input to script</param>
        /// <param name="errors">this gets errors from script</param>
        /// <returns>output of invocation</returns>
        /// <remarks>
        /// <paramref name="errors"/> is the non-terminating error stream
        /// from the command.
        /// In this release, the objects read from this PipelineReader
        /// are PSObjects wrapping ErrorRecords.
        /// </remarks>
        public Collection<PSObject> Invoke(string script, IEnumerable input, out IList errors)
        {
            if (_disposed == true)
            {
                throw PSTraceSource.NewObjectDisposedException("runspace");
            }

            if (script == null)
            {
                throw PSTraceSource.NewArgumentNullException("script");
            }
            Pipeline p = _runspace.CreatePipeline(script);
            Collection<PSObject> output = p.Invoke(input);
            // 2004/06/30-JonN was ReadAll() which was non-blocking
            errors = p.Error.NonBlockingRead();
            return output;
        }

        #endregion invoke

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Dispose underlying Runspace
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose which can be overridden by derived classes.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing)
                {
                    _runspace.Close();
                    _runspace = null;
                }
            }
            _disposed = true;
        }

        #endregion IDisposable Members
    }
}




