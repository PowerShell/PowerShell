// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    internal class ParameterSetSpecificMetadata
    {
        #region ctor

        /// <summary>
        /// Constructs an instance of the ParameterSetSpecificMetadata using the instance of the attribute
        /// that is specified.
        /// </summary>
        /// <param name="attribute">
        /// The attribute to be compiled.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="attribute"/> is null.
        /// </exception>
        internal ParameterSetSpecificMetadata(ParameterAttribute attribute)
        {
            if (attribute == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(attribute));
            }

            _attribute = attribute;
            IsMandatory = attribute.Mandatory;
            Position = attribute.Position;
            ValueFromRemainingArguments = attribute.ValueFromRemainingArguments;
            this.valueFromPipeline = attribute.ValueFromPipeline;
            this.valueFromPipelineByPropertyName = attribute.ValueFromPipelineByPropertyName;
            HelpMessage = attribute.HelpMessage;
            HelpMessageBaseName = attribute.HelpMessageBaseName;
            HelpMessageResourceId = attribute.HelpMessageResourceId;
        }

        internal ParameterSetSpecificMetadata(
            bool isMandatory,
            int position,
            bool valueFromRemainingArguments,
            bool valueFromPipeline,
            bool valueFromPipelineByPropertyName,
            string helpMessageBaseName,
            string helpMessageResourceId,
            string helpMessage)
        {
            IsMandatory = isMandatory;
            Position = position;
            ValueFromRemainingArguments = valueFromRemainingArguments;
            this.valueFromPipeline = valueFromPipeline;
            this.valueFromPipelineByPropertyName = valueFromPipelineByPropertyName;
            HelpMessageBaseName = helpMessageBaseName;
            HelpMessageResourceId = helpMessageResourceId;
            HelpMessage = helpMessage;
        }

        #endregion ctor

        /// <summary>
        /// Returns true if the parameter is mandatory for this parameterset, false otherwise.
        /// </summary>
        /// <value></value>
        internal bool IsMandatory { get; }

        /// <summary>
        /// If the parameter is allowed to be positional for this parameter set, this returns
        /// the position it is allowed to be in. If it is not positional, this returns int.MinValue.
        /// </summary>
        /// <value></value>
        internal int Position { get; } = int.MinValue;

        /// <summary>
        /// Returns true if the parameter is positional for this parameter set, or false otherwise.
        /// </summary>
        internal bool IsPositional
        {
            get
            {
                return Position != int.MinValue;
            }
        }

        /// <summary>
        /// Returns true if this parameter takes all the remaining unbound arguments that were specified,
        /// or false otherwise.
        /// </summary>
        /// <value></value>
        internal bool ValueFromRemainingArguments { get; }

        internal bool valueFromPipeline;
        /// <summary>
        /// Specifies that this parameter can take values from the incoming pipeline object.
        /// </summary>
        internal bool ValueFromPipeline
        {
            get
            {
                return valueFromPipeline;
            }
        }

        internal bool valueFromPipelineByPropertyName;
        /// <summary>
        /// Specifies that this parameter can take values from a property un the incoming
        /// pipeline object with the same name as the parameter.
        /// </summary>
        internal bool ValueFromPipelineByPropertyName
        {
            get
            {
                return valueFromPipelineByPropertyName;
            }
        }

        /// <summary>
        /// A short description for this parameter, suitable for presentation as a tool tip.
        /// </summary>
        internal string HelpMessage { get; }

        /// <summary>
        /// The base name of the resource for a help message.
        /// </summary>
        internal string HelpMessageBaseName { get; }

        /// <summary>
        /// The Id of the resource for a help message.
        /// </summary>
        internal string HelpMessageResourceId { get; } = null;

        /// <summary>
        /// Gets or sets the value that tells whether this parameter set
        /// data is for the "all" parameter set.
        /// </summary>
        internal bool IsInAllSets { get; set; }

        /// <summary>
        /// Gets the parameter set flag that represents the parameter set
        /// that this data is valid for.
        /// </summary>
        internal uint ParameterSetFlag { get; set; }

        /// <summary>
        /// If HelpMessageBaseName and HelpMessageResourceId are set, the help info is
        /// loaded from the resource indicated by HelpMessageBaseName and HelpMessageResourceId.
        /// If that fails and HelpMessage is set, the help info is set to HelpMessage; otherwise,
        /// the exception that is thrown when loading the resource is thrown.
        /// If both HelpMessageBaseName and HelpMessageResourceId are not set, the help info is
        /// set to HelpMessage.
        /// </summary>
        /// <returns>
        /// Help info about the parameter
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If the value of the specified resource is not a string and
        ///     HelpMessage is not set.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If only one of HelpMessageBaseName and HelpMessageResourceId is set
        ///     OR if no usable resources have been found, and
        ///     there are no neutral culture resources and HelpMessage is not set.
        /// </exception>
        internal string GetHelpMessage(Cmdlet cmdlet)
        {
            string helpInfo = null;
            bool isHelpMsgSet = !string.IsNullOrEmpty(HelpMessage);
            bool isHelpMsgBaseNameSet = !string.IsNullOrEmpty(HelpMessageBaseName);
            bool isHelpMsgResIdSet = !string.IsNullOrEmpty(HelpMessageResourceId);

            if (isHelpMsgBaseNameSet ^ isHelpMsgResIdSet)
            {
                throw PSTraceSource.NewArgumentException(isHelpMsgBaseNameSet ? "HelpMessageResourceId" : "HelpMessageBaseName");
            }

            if (isHelpMsgBaseNameSet && isHelpMsgResIdSet)
            {
                try
                {
                    helpInfo = cmdlet.GetResourceString(HelpMessageBaseName, HelpMessageResourceId);
                }
                catch (ArgumentException)
                {
                    if (isHelpMsgSet)
                    {
                        helpInfo = HelpMessage;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (InvalidOperationException)
                {
                    if (isHelpMsgSet)
                    {
                        helpInfo = HelpMessage;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (isHelpMsgSet)
            {
                helpInfo = HelpMessage;
            }

            return helpInfo;
        }

        private readonly ParameterAttribute _attribute;
    }
}
