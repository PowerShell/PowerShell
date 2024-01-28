// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    /// <summary>
    /// This class holds the data for missing mandatory parameters for each parameter set as we
    /// are trying to process which parameter set to use based on the missing mandatory parameters.
    /// </summary>
    internal sealed class ParameterSetPromptingData
    {
        internal ParameterSetPromptingData(uint parameterSet, bool isDefaultSet)
        {
            ParameterSet = parameterSet;
            IsDefaultSet = isDefaultSet;
        }

        /// <summary>
        /// True if this parameter set represents the default parameter set.
        /// </summary>
        internal bool IsDefaultSet { get; }

        /// <summary>
        /// The parameter set this data represents.
        /// </summary>
        internal uint ParameterSet { get; } = 0;

        /// <summary>
        /// True if the parameter set represents parameters in all the parameter sets.
        /// </summary>
        internal bool IsAllSet
        {
            get { return ParameterSet == uint.MaxValue; }
        }

        /// <summary>
        /// Gets the parameters that take pipeline input and are mandatory in this parameter set.
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by value, and are mandatory in this parameter set.
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByValueParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that take pipeline input by property name, and are mandatory in this parameter set.
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> PipelineableMandatoryByPropertyNameParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();

        /// <summary>
        /// Gets the parameters that do not take pipeline input and are mandatory in this parameter set.
        /// </summary>
        internal Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata> NonpipelineableMandatoryParameters
        { get; } = new Dictionary<MergedCompiledCommandParameter, ParameterSetSpecificMetadata>();
    }
}
