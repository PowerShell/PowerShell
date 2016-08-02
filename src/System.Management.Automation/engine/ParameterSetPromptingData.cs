/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;

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
            _parameterSet = parameterSet;
            _isDefaultSet = isDefaultSet;
        }

        /// <summary>
        /// True if this parameter set represents the default parameter set
        /// </summary>
        /// 
        internal bool IsDefaultSet
        {
            get { return _isDefaultSet; }
        }
        private bool _isDefaultSet;

        /// <summary>
        /// The parameter set this data represents
        /// </summary>
        /// 
        internal uint ParameterSet
        {
            get { return _parameterSet; }
        }
        private uint _parameterSet = 0;

        /// <summary>
        /// True if the parameter set represents parameters in all the parameter sets
        /// </summary>
        internal bool IsAllSet
        {
            get { return _parameterSet == uint.MaxValue; }
        }

        /// <summary>
        /// Gets the parameters that take pipeline input and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryParameters
        {
            get { return _pipelineableMandatoryParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> _pipelineableMandatoryParameters =
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by value, and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByValueParameters
        {
            get { return _pipelineableMandatoryByValueParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> _pipelineableMandatoryByValueParameters =
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by property name, and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByPropertyNameParameters
        {
            get { return _pipelineableMandatoryByPropertyNameParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> _pipelineableMandatoryByPropertyNameParameters =
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();


        /// <summary>
        /// Gets the parameters that do not take pipeline input and are mandatory in this parameter set
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> NonpipelineableMandatoryParameters
        {
            get { return _nonpipelineableMandatoryParameters; }
        }
        private Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> _nonpipelineableMandatoryParameters =
            new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();
    }
}

