/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Possible states of workflow instance.
    /// </summary>
    internal enum WorkflowInstanceState
    {
        /// <summary>
        /// Not initialized with any workflow yet.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Workflow execution is loaded but not yet started.
        /// </summary>
        Loaded = 1,

        /// <summary>
        /// Workflow is currently executing.
        /// </summary>
        Executing = 2,

        /// <summary>
        /// Workflow execution is completed.
        /// </summary>
        CompletedAndClosed = 3,

        /// <summary>
        /// Faulted state, once the workflow execution is completed.
        /// </summary>
        Faulted = 4,

        /// <summary>
        /// Canceled state, once the workflow execution is completed.
        /// </summary>
        Canceled = 5,

        /// <summary>
        /// Aborted state, once the workflow execution is completed.
        /// </summary>
        Aborted = 6,

        /// <summary>
        /// Un-handled exception and termination state, once the workflow execution is completed.
        /// </summary>
        UnhandledExceptionAndTermination = 7,

        /// <summary>
        /// Workflow is currently unloaded.
        /// </summary>
        Unloaded = 8,

        /// <summary>
        /// Workflow is currently unknown.
        /// </summary>
        Unknown = 9,
    };

    /// <summary>
    /// Provides an event definition for the status of a workflow.
    /// </summary>
    internal class WorkflowStatusEventArgs : EventArgs
    {
        /// <summary>
        /// The instance id.
        /// </summary>
        private Guid id;

        /// <summary>
        /// The workflow instance state.
        /// </summary>
        private WorkflowInstanceState state;

        /// <summary>
        /// The workflow unhandled exception.
        /// </summary>
        private Exception unhandledException;

        /// <summary>
        /// Constructor with instance id and state.
        /// </summary>
        /// <param name="id">The workflow Id.</param>
        /// <param name="state">The state of workflow</param>
        /// <param name="unhandledException">The unhandled exception, occurs when streams are closed.</param>
        internal WorkflowStatusEventArgs(Guid id, WorkflowInstanceState state, Exception unhandledException)
        {
            this.id = id;
            this.state = state;
            this.unhandledException = unhandledException;
        }

        /// <summary>
        /// Gets instance id.
        /// </summary>
        internal Guid Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets instance state.
        /// </summary>
        internal WorkflowInstanceState State
        {
            get { return this.state; }
        }

        /// <summary>
        /// Gets unhandled exception.
        /// </summary>
        internal Exception UnhandledException
        {
            get { return this.unhandledException; }
        }
    }
}
