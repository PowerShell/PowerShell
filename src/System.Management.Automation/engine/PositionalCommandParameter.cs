/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

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
            this.parameter = parameter;
        }

        #endregion ctor

        internal MergedCompiledCommandParameter Parameter
        {
            get
            {
                return parameter;
            }
        }
        private MergedCompiledCommandParameter parameter;

        internal Collection<ParameterSetSpecificMetadata> ParameterSetData
        {
            get
            {
                return parameterSetData;
            }
        }
        private Collection<ParameterSetSpecificMetadata> parameterSetData = new Collection<ParameterSetSpecificMetadata>();

    }
}

