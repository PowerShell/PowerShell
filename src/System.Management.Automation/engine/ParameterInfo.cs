// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a cmdlet parameter for a particular parameter set.
    /// </summary>
    public class CommandParameterInfo
    {
        #region ctor

        /// <summary>
        /// Constructs the parameter info using the specified aliases, attributes, and
        /// parameter set metadata.
        /// </summary>
        /// <param name="parameter">
        /// The parameter metadata to retrieve the parameter information from.
        /// </param>
        /// <param name="parameterSetFlag">
        /// The parameter set flag to get the parameter information from.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameter"/> is null.
        /// </exception>
        internal CommandParameterInfo(
            CompiledCommandParameter parameter,
            uint parameterSetFlag)
        {
            if (parameter == null)
            {
                throw PSTraceSource.NewArgumentNullException("parameter");
            }

            Name = parameter.Name;
            ParameterType = parameter.Type;
            IsDynamic = parameter.IsDynamic;
            Aliases = new ReadOnlyCollection<string>(parameter.Aliases);

            SetAttributes(parameter.CompiledAttributes);
            SetParameterSetData(parameter.GetParameterSetData(parameterSetFlag));
        }

        #endregion ctor

        #region public members

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// Gets whether or not the parameter is a dynamic parameter.
        /// </summary>
        /// <remarks>
        /// True if the parameter is dynamic, or false otherwise.
        /// </remarks>
        public bool IsMandatory { get; private set; }

        /// <summary>
        /// Gets whether or not the parameter is mandatory.
        /// </summary>
        /// <remarks>
        /// True if the parameter is mandatory, or false otherwise.
        /// </remarks>
        public bool IsDynamic { get; }

        /// <summary>
        /// Gets the position in which the parameter can be specified on the command line
        /// if not named. If the returned value is int.MinValue then the parameter must be named.
        /// </summary>
        public int Position { get; private set; } = int.MinValue;

        /// <summary>
        /// Gets whether the parameter can take values from the incoming pipeline object.
        /// </summary>
        public bool ValueFromPipeline { get; private set; }

        /// <summary>
        /// Gets whether the parameter can take values from a property inn the incoming
        /// pipeline object with the same name as the parameter.
        /// </summary>
        public bool ValueFromPipelineByPropertyName { get; private set; }

        /// <summary>
        /// Gets whether the parameter will take any argument that isn't bound to another parameter.
        /// </summary>
        public bool ValueFromRemainingArguments { get; private set; }

        /// <summary>
        /// Gets the help message for this parameter.
        /// </summary>
        public string HelpMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the aliases by which this parameter can be referenced.
        /// </summary>
        public ReadOnlyCollection<string> Aliases { get; }

        /// <summary>
        /// Gets the attributes that are specified on the parameter.
        /// </summary>
        public ReadOnlyCollection<Attribute> Attributes { get; private set; }

        #endregion public members

        #region private members

        private void SetAttributes(IList<Attribute> attributeMetadata)
        {
            Diagnostics.Assert(
                attributeMetadata != null,
                "The compiled attribute collection should never be null");

            Collection<Attribute> processedAttributes = new Collection<Attribute>();

            foreach (var attribute in attributeMetadata)
            {
                processedAttributes.Add(attribute);
            }

            Attributes = new ReadOnlyCollection<Attribute>(processedAttributes);
        }

        private void SetParameterSetData(ParameterSetSpecificMetadata parameterMetadata)
        {
            IsMandatory = parameterMetadata.IsMandatory;
            Position = parameterMetadata.Position;
            ValueFromPipeline = parameterMetadata.valueFromPipeline;
            ValueFromPipelineByPropertyName = parameterMetadata.valueFromPipelineByPropertyName;
            ValueFromRemainingArguments = parameterMetadata.ValueFromRemainingArguments;
            HelpMessage = parameterMetadata.HelpMessage;
        }

        #endregion private members
    }
}

