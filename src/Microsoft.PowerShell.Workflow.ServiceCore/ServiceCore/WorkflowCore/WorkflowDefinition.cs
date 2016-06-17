/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Management.Automation;
    using System.Activities;

    /// <summary>
    /// Define all the metadatas.
    /// </summary>
    public sealed class PSWorkflowDefinition
    {
        private Activity workflow;
        private string workflowXaml;
        private string runtimeAssemblyPath;
        private Dictionary<string, string> requiredAssemblies;

        /// <summary>
        /// Workflow instance constructor.
        /// </summary>
        /// <param name="workflow">The workflow definition, represented in object-graph.</param>
        /// <param name="workflowXaml">The workflow xaml definition.</param>
        /// <param name="runtimeAssemblyPath">The path to the runtime assembly.</param>
        /// <param name="requiredAssemblies">Required assemblies paths</param>
        public PSWorkflowDefinition(
                                    Activity workflow,
                                    string workflowXaml,
                                    string runtimeAssemblyPath,
                                    Dictionary<string, string> requiredAssemblies): this(workflow, workflowXaml, runtimeAssemblyPath)
        {
            this.requiredAssemblies = requiredAssemblies;
        }

        /// <summary>
        /// Workflow instance constructor.
        /// </summary>
        /// <param name="workflow">The workflow definition, represented in object-graph.</param>
        /// <param name="workflowXaml">The workflow xaml definition.</param>
        /// <param name="runtimeAssemblyPath">The path to the runtime assembly.</param>
        public PSWorkflowDefinition(
                                    Activity workflow,
                                    string workflowXaml,
                                    string runtimeAssemblyPath)
        {
            this.workflow = workflow;
            this.workflowXaml = workflowXaml;
            this.runtimeAssemblyPath = runtimeAssemblyPath;
        }

        /// <summary>
        /// Gets sets the workflow.
        /// </summary>
        public Activity Workflow
        {
            get { return this.workflow; }
            set { this.workflow = value; }
        }

        /// <summary>
        /// Gets sets the workflow xaml.
        /// </summary>
        public string WorkflowXaml
        {
            get { return this.workflowXaml; }
            set { this.workflowXaml = value; }
        }

        /// <summary>
        /// Gets sets the runtime assembly path.
        /// </summary>
        public string RuntimeAssemblyPath
        {
            get { return this.runtimeAssemblyPath; }
            set { this.runtimeAssemblyPath = value; }
        }

        /// <summary>
        /// Gets sets the required assemblies.
        /// </summary>
        public Dictionary<string, string> RequiredAssemblies
        {
            get { return this.requiredAssemblies; }
        }
    }
}
