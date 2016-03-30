/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// This class holds the data for missing mandatory parameters for each parameter set as we
    /// are trying to process which parameter set to use based on the missing mandatory parameters
    /// </summary>
    /// 
    internal class ParameterSetPromptingData
    {
        internal ParameterSetPromptingData(uint parameterSet, bool isDefaultSet)
        {
            this.parameterSet = parameterSet;
            this.isDefaultSet = isDefaultSet;
        }

        /// <summary>
        /// True if this parameter set represents the default parameter set
        /// </summary>
        /// 
        internal bool IsDefaultSet
        {
            get { return isDefaultSet; }
        }
        private bool isDefaultSet;

        /// <summary>
        /// The parameter set this data represents
        /// </summary>
        /// 
        internal uint ParameterSet
        {
            get { return parameterSet; }
        }
        private uint parameterSet = 0;

        /// <summary>
        /// True if the parameter set represents parameters in all the parameter sets
        /// </summary>
        internal bool IsAllSet
        {
            get { return parameterSet == uint.MaxValue; }
        }

        /// <summary>
        /// Gets the parameters that take pipeline input and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryParameters
        {
            get { return pipelineableMandatoryParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> pipelineableMandatoryParameters = 
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by value, and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByValueParameters
        {
            get { return pipelineableMandatoryByValueParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> pipelineableMandatoryByValueParameters =
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by property name, and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByPropertyNameParameters
        {
            get { return pipelineableMandatoryByPropertyNameParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> pipelineableMandatoryByPropertyNameParameters = 
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();


        /// <summary>
        /// Gets the parameters that do not take pipeline input and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> NonpipelineableMandatoryParameters
        {
            get { return nonpipelineableMandatoryParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> nonpipelineableMandatoryParameters = 
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();
    }
}

