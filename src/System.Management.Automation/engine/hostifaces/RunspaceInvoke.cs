// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Management.Automation.Runspaces;

    /// <summary>
    /// Defines a class which allows simple execution of commands from CLR languages.
    /// </summary>
    public class RunspaceInvoke : IDisposable
    {
        #region constructors

        /// <summary>
        /// Runspace on which commands are invoked.
        /// </summary>
        private Runspace _runspace;

        /// <summary>
        /// Create a RunspaceInvoke for invoking commands. This uses
        /// a runspace with default PSSnapins.
        /// </summary>
        public RunspaceInvoke()
        {
            _runspace = RunspaceFactory.CreateRunspace();
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

        #endregion constructors

        #region invoke

        /// <summary>
        /// Invoke the specified script.
        /// </summary>
        /// <param name="script">Msh script to invoke.</param>
        /// <returns>Output of invocation.</returns>
        public Collection<PSObject> Invoke(string script)
        {
            return Invoke(script, null);
        }

        /// <summary>
        /// Invoke the specified script and passes specified input to the script.
        /// </summary>
        /// <param name="script">Msh script to invoke.</param>
        /// <param name="input">Input to script.</param>
        /// <returns>Output of invocation.</returns>
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
        /// <param name="script">Msh script to invoke.</param>
        /// <param name="input">Input to script.</param>
        /// <param name="errors">This gets errors from script.</param>
        /// <returns>Output of invocation.</returns>
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
        /// Set to true when object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Dispose underlying Runspace.
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

