/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    internal class PositionalCommandParameter
    {
        #region ctor

        /// <summary>
        /// Constructs a container for the merged parameter metadata and
        /// parameter set specific metadata for a positional parameter
        /// </summary>
        /// 
        internal PositionalCommandParameter(MergedCompiledCommandParameter parameter)
        {
            _parameter = parameter;
        }

        #endregion ctor

        internal MergedCompiledCommandParameter Parameter
        {
            get
            {
                return _parameter;
            }
        }
        private MergedCompiledCommandParameter _parameter;

        internal Collection<ParameterSetSpecificMetadata> ParameterSetData
        {
            get
            {
                return _parameterSetData;
            }
        }
        private Collection<ParameterSetSpecificMetadata> _parameterSetData = new Collection<ParameterSetSpecificMetadata>();
    }
}

