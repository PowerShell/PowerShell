// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a workflow that is stored in session state.
    /// </summary>
    public class WorkflowInfo : FunctionInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the workflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="definition">
        /// The script body defining the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="xamlDefinition">
        /// The XAML used to define the workflow
        /// </param>
        /// <param name="workflowsCalled">
        /// The workflows referenced within <paramref name="xamlDefinition"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, string definition, ScriptBlock workflow, string xamlDefinition, WorkflowInfo[] workflowsCalled)
            : this(name, workflow, (ExecutionContext)null)
        {
            if (string.IsNullOrEmpty(xamlDefinition))
            {
                throw PSTraceSource.NewArgumentNullException("xamlDefinition");
            }

            _definition = definition;
            this.XamlDefinition = xamlDefinition;
            if (workflowsCalled != null)
            {
                _workflowsCalled = new ReadOnlyCollection<WorkflowInfo>(workflowsCalled);
            }
        }

        /// <summary>
        /// Creates an instance of the workflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="definition">
        /// The script body defining the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="xamlDefinition">
        /// The XAML used to define the workflow
        /// </param>
        /// <param name="workflowsCalled">
        /// The workflows referenced within <paramref name="xamlDefinition"/>.
        /// </param>
        /// <param name="module">Module.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, string definition, ScriptBlock workflow, string xamlDefinition, WorkflowInfo[] workflowsCalled, PSModuleInfo module)
            : this(name, definition, workflow, xamlDefinition, workflowsCalled)
        {
            this.Module = module;
        }

        /// <summary>
        /// Creates an instance of the workflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the workflow.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, ScriptBlock workflow, ExecutionContext context) : this(name, workflow, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the workflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the workflow.
        /// </param>
        /// <param name="helpFile">
        /// The helpfile for the workflow.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, ScriptBlock workflow, ExecutionContext context, string helpFile)
            : base(name, workflow, context, helpFile)
        {
            SetCommandType(CommandTypes.Workflow);
        }

        /// <summary>
        /// Creates an instance of the WorkflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the workflow.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, ScriptBlock workflow, ScopedItemOptions options, ExecutionContext context) : this(name, workflow, options, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the WorkflowInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the workflow.
        /// </param>
        /// <param name="workflow">
        /// The ScriptBlock for the workflow
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the workflow.
        /// </param>
        /// <param name="helpFile">
        /// The helpfile for the workflow.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="workflow"/> is null.
        /// </exception>
        internal WorkflowInfo(string name, ScriptBlock workflow, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : base(name, workflow, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Workflow);
        }

        /// <summary>
        /// This is a copy constructor.
        /// </summary>
        internal WorkflowInfo(WorkflowInfo other)
            : base(other)
        {
            SetCommandType(CommandTypes.Workflow);

            CopyFields(other);
        }

        /// <summary>
        /// This is a copy constructor.
        /// </summary>
        internal WorkflowInfo(string name, WorkflowInfo other)
            : base(name, other)
        {
            SetCommandType(CommandTypes.Workflow);

            CopyFields(other);
        }

        private void CopyFields(WorkflowInfo other)
        {
            this.XamlDefinition = other.XamlDefinition;
            this.NestedXamlDefinition = other.NestedXamlDefinition;
            _workflowsCalled = other.WorkflowsCalled;
            _definition = other.Definition;
        }

        /// <summary>
        /// Update a workflow.
        /// </summary>
        /// <param name="function">
        /// The script block that the function should represent.
        /// </param>
        /// <param name="force">
        /// If true, the script block will be applied even if the filter is ReadOnly.
        /// </param>
        /// <param name="options">
        /// Any options to set on the new function, null if none.
        /// </param>
        /// <param name="helpFile">
        /// Helpfile for this function
        /// </param>
        protected internal override void Update(FunctionInfo function, bool force, ScopedItemOptions options, string helpFile)
        {
            var other = function as WorkflowInfo;
            if (other == null)
            {
                throw PSTraceSource.NewArgumentException("function");
            }

            base.Update(function, force, options, helpFile);

            CopyFields(other);
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            WorkflowInfo copy = new WorkflowInfo(this);
            copy.IsGetCommandCopy = true;
            copy.Arguments = arguments;
            return copy;
        }

        #endregion ctor

        /// <summary>
        /// Returns the definition of the workflow.
        /// </summary>
        public override string Definition
        {
            get { return _definition; }
        }

        private string _definition = string.Empty;

        /// <summary>
        /// Gets the XAML that represents the definition of the workflow.
        /// </summary>
        public string XamlDefinition { get; internal set; }

        /// <summary>
        /// Gets or sets the XAML that represents the definition of the workflow
        /// when called from another workflow.
        /// </summary>
        public string NestedXamlDefinition { get; set; }

        /// <summary>
        /// Gets the XAML for workflows called by this workflow.
        /// </summary>
        public ReadOnlyCollection<WorkflowInfo> WorkflowsCalled
        {
            get { return _workflowsCalled ?? Utils.EmptyReadOnlyCollection<WorkflowInfo>(); }
        }

        private ReadOnlyCollection<WorkflowInfo> _workflowsCalled;

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Workflow; }
        }
    }
}
