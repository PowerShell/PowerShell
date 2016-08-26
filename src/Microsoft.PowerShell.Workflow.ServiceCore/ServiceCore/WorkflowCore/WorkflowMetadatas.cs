/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System.Collections.Generic;

    /// <summary>
    /// Define all the metadatas.
    /// </summary>
    public class PSWorkflowContext
    {
        /// <summary>
        /// The parameters, which need to be passed to the workflow engine.
        /// </summary>
        private Dictionary<string, object> _workflowParameters;

        /// <summary>
        /// The ubiquitous parameters, which are also passed to the engine.
        /// </summary>
        private Dictionary<string, object> _psWorkflowCommonParameters;
     
        /// <summary>
        /// The metadata, which contains all the information related to job and client like, job-id, connection-id and application-id etc.
        /// </summary>
        private Dictionary<string, object> jobMetadata;
        
        /// <summary>
        /// The metadata, which is specific to the caller and doesn't contain any information related to workflow execution.
        /// </summary>
        private Dictionary<string, object> privateMetadata;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public PSWorkflowContext()
        {
            this._workflowParameters = null;
            this._psWorkflowCommonParameters = null;
            this.jobMetadata = null;
            this.privateMetadata = null;
        }

        /// <summary>
        /// Workflow metadatas constructor.
        /// </summary>
        /// <param name="workflowParameters">The parameters, which need to be passed to the workflow engine.</param>
        /// <param name="workflowCommonParameters">The ubiquitous parameters, which are also passed to the engine.</param>
        /// <param name="jobMetadata">The metadata, which contains all the information related to job and client like, job-id, connection-id and application-id etc.</param>
        /// <param name="privateMetadata">The metadata, which is specific to the caller and doesn't contain any information related to workflow execution.</param>
        public PSWorkflowContext(
                                    Dictionary<string, object> workflowParameters,
                                    Dictionary<string, object> workflowCommonParameters,
                                    Dictionary<string, object> jobMetadata,
                                    Dictionary<string, object> privateMetadata)
        {
            this._workflowParameters = workflowParameters;
            this._psWorkflowCommonParameters = workflowCommonParameters;
            this.jobMetadata = jobMetadata;
            this.privateMetadata = privateMetadata;
        }

        /// <summary>
        /// Gets the input to workflow.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, object> WorkflowParameters
        {
            get { return this._workflowParameters; }
            set { this._workflowParameters = value; }
        }

        /// <summary>
        /// Gets the input to workflow.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, object> PSWorkflowCommonParameters
        {
            get { return this._psWorkflowCommonParameters; }
            set { this._psWorkflowCommonParameters = value; }
        }

        /// <summary>
        /// Gets the input to workflow.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, object> JobMetadata
        {
            get { return this.jobMetadata; }
            set { this.jobMetadata = value; }
        }

        /// <summary>
        /// Gets the input to workflow.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, object> PrivateMetadata
        {
            get { return this.privateMetadata; }
            set { this.privateMetadata = value; }
        }

    }
}
