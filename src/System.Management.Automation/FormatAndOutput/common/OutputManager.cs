// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Inner command class used to manage the sub pipelines
    /// it determines which command should process the incoming objects
    /// based on the object type
    ///
    /// This class is the implementation class for out-console and out-file.
    /// </summary>
    internal sealed class OutputManagerInner : ImplementationCommandBase
    {
        #region tracer
        [TraceSource("format_out_OutputManagerInner", "OutputManagerInner")]
        internal static PSTraceSource tracer = PSTraceSource.GetTracer("format_out_OutputManagerInner", "OutputManagerInner");
        #endregion tracer

        #region LineOutput
        internal LineOutput LineOutput
        {
            set
            {
                lock (_syncRoot)
                {
                    _lo = value;

                    if (_isStopped)
                    {
                        _lo.StopProcessing();
                    }
                }
            }
        }

        private LineOutput _lo = null;
        #endregion

        /// <summary>
        /// Handler for processing each object coming through the pipeline
        /// it forwards the call to the pipeline manager object.
        /// </summary>
        internal override void ProcessRecord()
        {
            PSObject so = this.ReadObject();

            if (so == null || so == AutomationNull.Value)
            {
                return;
            }

            // on demand initialization when the first pipeline
            // object is initialized
            if (_mgr == null)
            {
                _mgr = new SubPipelineManager();
                _mgr.Initialize(_lo, this.OuterCmdlet().Context);
            }

#if false
            // if the object supports IEnumerable,
            // unpack the object and process each member separately
            IEnumerable e = PSObjectHelper.GetEnumerable (so);

            if (e == null)
            {
                this.mgr.Process (so);
            }
            else
            {
                foreach (object obj in e)
                {
                    this.mgr.Process (PSObjectHelper.AsPSObject (obj));
                }
            }
#else
            _mgr.Process(so);
#endif
        }

        /// <summary>
        /// Handler for processing shut down. It forwards the call to the
        /// pipeline manager object.
        /// </summary>
        internal override void EndProcessing()
        {
            // shut down only if we ever processed a pipeline object
            if (_mgr != null)
                _mgr.ShutDown();
        }

        internal override void StopProcessing()
        {
            lock (_syncRoot)
            {
                if (_lo != null)
                {
                    _lo.StopProcessing();
                }

                _isStopped = true;
            }
        }

        /// <summary>
        /// Make sure we dispose of the sub pipeline manager.
        /// </summary>
        protected override void InternalDispose()
        {
            base.InternalDispose();
            if (_mgr != null)
            {
                _mgr.Dispose();
                _mgr = null;
            }
        }

        /// <summary>
        /// Instance of the pipeline manager object.
        /// </summary>
        private SubPipelineManager _mgr = null;

        /// <summary>
        /// True if the cmdlet has been stopped.
        /// </summary>
        private bool _isStopped = false;

        /// <summary>
        /// Lock object.
        /// </summary>
        private object _syncRoot = new object();
    }

    /// <summary>
    /// Object managing the sub-pipelines that execute
    /// different output commands (or different instances of the
    /// default one)
    /// </summary>
    internal sealed class SubPipelineManager : IDisposable
    {
        /// <summary>
        /// Entry defining a command to be run in a separate pipeline.
        /// </summary>
        private sealed class CommandEntry : IDisposable
        {
            /// <summary>
            /// Instance of pipeline wrapper object.
            /// </summary>
            internal CommandWrapper command = new CommandWrapper();

            /// <summary>
            /// </summary>
            /// <param name="typeName">ETS type name of the object to process.</param>
            /// <returns>True if there is a match.</returns>
            internal bool AppliesToType(string typeName)
            {
                foreach (string s in _applicableTypes)
                {
                    if (string.Equals(s, typeName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Just dispose of the inner command wrapper.
            /// </summary>
            public void Dispose()
            {
                if (this.command == null)
                    return;

                this.command.Dispose();
                this.command = null;
            }

            /// <summary>
            /// Ordered list of ETS type names this object is handling.
            /// </summary>
            private StringCollection _applicableTypes = new StringCollection();
        }

        /// <summary>
        /// Initialize the pipeline manager before any object is processed.
        /// </summary>
        /// <param name="lineOutput">LineOutput to pass to the child pipelines.</param>
        /// <param name="context">ExecutionContext to pass to the child pipelines.</param>
        internal void Initialize(LineOutput lineOutput, ExecutionContext context)
        {
            _lo = lineOutput;
            InitializeCommandsHardWired(context);
        }

        /// <summary>
        /// Hard wired registration helper for specialized types.
        /// </summary>
        /// <param name="context">ExecutionContext to pass to the child pipeline.</param>
        private void InitializeCommandsHardWired(ExecutionContext context)
        {
            // set the default handler
            RegisterCommandDefault(context, "out-lineoutput", typeof(OutLineOutputCommand));
            /*
            NOTE:
            This is the spot where we could add new specialized handlers for
            additional types. Adding a handler here would cause a new sub-pipeline
            to be created.

            For example, the following line would add a new handler named "out-foobar"
            to be invoked when the incoming object type is "MyNamespace.Whatever.FooBar"

            RegisterCommandForTypes (context, "out-foobar", new string[] { "MyNamespace.Whatever.FooBar" });

            And the method can be like this:
            private void RegisterCommandForTypes (ExecutionContext context, string commandName, Type commandType, string[] types)
            {
                CommandEntry ce = new CommandEntry ();

                ce.command.Initialize (context, commandName, commandType);
                ce.command.AddNamedParameter ("LineOutput", this.lo);
                for (int k = 0; k < types.Length; k++)
                {
                    ce.AddApplicableType (types[k]);
                }

                this.commandEntryList.Add (ce);
            }
            */
        }

        /// <summary>
        /// Register the default output command.
        /// </summary>
        /// <param name="context">ExecutionContext to pass to the child pipeline.</param>
        /// <param name="commandName">Name of the command to execute.</param>
        /// <param name="commandType">Type of the command to execute.</param>
        private void RegisterCommandDefault(ExecutionContext context, string commandName, Type commandType)
        {
            CommandEntry ce = new CommandEntry();

            ce.command.Initialize(context, commandName, commandType);
            ce.command.AddNamedParameter("LineOutput", _lo);
            _defaultCommandEntry = ce;
        }

        /// <summary>
        /// Process an incoming parent pipeline object.
        /// </summary>
        /// <param name="so">Pipeline object to process.</param>
        internal void Process(PSObject so)
        {
            // select which pipeline should handle the object
            CommandEntry ce = this.GetActiveCommandEntry(so);

            Diagnostics.Assert(ce != null, "CommandEntry ce must not be null");

            // delegate the processing
            ce.command.Process(so);
        }

        /// <summary>
        /// Shut down the child pipelines.
        /// </summary>
        internal void ShutDown()
        {
            // we assume that command entries are never null
            foreach (CommandEntry ce in _commandEntryList)
            {
                Diagnostics.Assert(ce != null, "ce != null");
                ce.command.ShutDown();
                ce.command = null;
            }

            // we assume we always have a default command entry
            Diagnostics.Assert(_defaultCommandEntry != null, "defaultCommandEntry != null");
            _defaultCommandEntry.command.ShutDown();
            _defaultCommandEntry.command = null;
        }

        public void Dispose()
        {
            // we assume that command entries are never null
            foreach (CommandEntry ce in _commandEntryList)
            {
                Diagnostics.Assert(ce != null, "ce != null");
                ce.Dispose();
            }

            // we assume we always have a default command entry
            Diagnostics.Assert(_defaultCommandEntry != null, "defaultCommandEntry != null");
            _defaultCommandEntry.Dispose();
        }

        /// <summary>
        /// It selects the applicable out command (it can be the default one)
        /// to process the current pipeline object.
        /// </summary>
        /// <param name="so">Pipeline object to be processed.</param>
        /// <returns>Applicable command entry.</returns>
        private CommandEntry GetActiveCommandEntry(PSObject so)
        {
            string typeName = PSObjectHelper.PSObjectIsOfExactType(so.InternalTypeNames);
            foreach (CommandEntry ce in _commandEntryList)
            {
                if (ce.AppliesToType(typeName))
                    return ce;
            }

            // failed any match: return the default handler
            return _defaultCommandEntry;
        }

        private LineOutput _lo = null;

        /// <summary>
        /// List of command entries, each with a set of applicable types.
        /// </summary>
        private List<CommandEntry> _commandEntryList = new List<CommandEntry>();

        /// <summary>
        /// Default command entry to be executed when all type matches fail.
        /// </summary>
        private CommandEntry _defaultCommandEntry = new CommandEntry();
    }
}
