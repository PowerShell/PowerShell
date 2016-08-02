/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

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
        /// parameter set metadata
        /// </summary>
        /// 
        /// <param name="parameter">
        /// The parameter metadata to retrieve the parameter information from.
        /// </param>
        /// 
        /// <param name="parameterSetFlag">
        /// The parameter set flag to get the parameter information from.
        /// </param>
        /// 
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameter"/> is null.
        /// </exception>
        /// 
        internal CommandParameterInfo(
            CompiledCommandParameter parameter,
            uint parameterSetFlag)
        {
            if (parameter == null)
            {
                throw PSTraceSource.NewArgumentNullException("parameter");
            }

            _name = parameter.Name;
            _parameterType = parameter.Type;
            _isDynamic = parameter.IsDynamic;
            _aliases = new ReadOnlyCollection<string>(parameter.Aliases);

            SetAttributes(parameter.CompiledAttributes);
            SetParameterSetData(parameter.GetParameterSetData(parameterSetFlag));
        }

        #endregion ctor

        #region public members

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private string _name = String.Empty;

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }
        }
        private Type _parameterType;

        /// <summary>
        /// Gets whether or not the parameter is a dynamic parameter.
        /// </summary>
        /// 
        /// <remarks>
        /// True if the parameter is dynamic, or false otherwise.
        /// </remarks>
        public bool IsMandatory
        {
            get
            {
                return _isMandatory;
            }
        }
        private bool _isMandatory;

        /// <summary>
        /// Gets whether or not the parameter is mandatory.
        /// </summary>
        /// 
        /// <remarks>
        /// True if the parameter is mandatory, or false otherwise.
        /// </remarks>
        public bool IsDynamic
        {
            get
            {
                return _isDynamic;
            }
        }
        private bool _isDynamic;

        /// <summary>
        /// Gets the position in which the parameter can be specified on the command line
        /// if not named. If the returned value is int.MinValue then the parameter must be named.
        /// </summary>
        public int Position
        {
            get
            {
                return _position;
            }
        }
        private int _position = int.MinValue;

        private bool _valueFromPipeline;
        /// <summary>
        /// Gets whether the parameter can take values from the incoming pipeline object.
        /// </summary>
        public bool ValueFromPipeline
        {
            get
            {
                return _valueFromPipeline;
            }
        }

        private bool _valueFromPipelineByPropertyName;
        /// <summary>
        /// Gets whether the parameter can take values from a property inn the incoming
        /// pipeline object with the same name as the parameter.
        /// </summary>
        public bool ValueFromPipelineByPropertyName
        {
            get
            {
                return _valueFromPipelineByPropertyName;
            }
        }

        /// <summary>
        /// Gets whether the parameter will take any argument that isn't bound to another parameter.
        /// </summary>
        public bool ValueFromRemainingArguments
        {
            get
            {
                return _valueFromRemainingArguments;
            }
        }
        private bool _valueFromRemainingArguments;

        /// <summary>
        /// Gets the help message for this parameter.
        /// </summary>
        public string HelpMessage
        {
            get
            {
                return _helpMessage;
            }
        }
        private string _helpMessage = String.Empty;

        /// <summary>
        /// Gets the aliases by which this parameter can be referenced.
        /// </summary>
        public ReadOnlyCollection<string> Aliases
        {
            get
            {
                return _aliases;
            }
        }
        private ReadOnlyCollection<string> _aliases;

        /// <summary>
        /// Gets the attributes that are specified on the parameter.
        /// </summary>
        public ReadOnlyCollection<Attribute> Attributes
        {
            get
            {
                return _attributes;
            }
        }
        private ReadOnlyCollection<Attribute> _attributes;

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

            _attributes = new ReadOnlyCollection<Attribute>(processedAttributes);
        }


        private void SetParameterSetData(ParameterSetSpecificMetadata parameterMetadata)
        {
            _isMandatory = parameterMetadata.IsMandatory;
            _position = parameterMetadata.Position;
            _valueFromPipeline = parameterMetadata.valueFromPipeline;
            _valueFromPipelineByPropertyName = parameterMetadata.valueFromPipelineByPropertyName;
            _valueFromRemainingArguments = parameterMetadata.ValueFromRemainingArguments;
            _helpMessage = parameterMetadata.HelpMessage;
        }

        #endregion private members
    } // class CommandParameterInfo
} // namespace System.Management.Automation

