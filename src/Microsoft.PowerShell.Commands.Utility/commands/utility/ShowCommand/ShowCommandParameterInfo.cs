// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    /// <summary>
    /// Implements a facade around ShowCommandParameterInfo and its deserialized counterpart.
    /// </summary>
    public class ShowCommandParameterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandParameterInfo"/> class
        /// with the specified <see cref="CommandParameterInfo"/>.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterInfo(CommandParameterInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
            this.IsMandatory = other.IsMandatory;
            this.ValueFromPipeline = other.ValueFromPipeline;
            this.ParameterType = new ShowCommandParameterType(other.ParameterType);
            this.Position = other.Position;

            var validateSetAttribute = other.Attributes.Where(static x => typeof(ValidateSetAttribute).IsAssignableFrom(x.GetType())).Cast<ValidateSetAttribute>().LastOrDefault();
            if (validateSetAttribute != null)
            {
                this.HasParameterSet = true;
                this.ValidParamSetValues = validateSetAttribute.ValidValues;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandParameterInfo"/> class.
        /// Creates an instance of the ShowCommandParameterInfo class based on a PSObject object.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
            this.IsMandatory = (bool)(other.Members["IsMandatory"].Value);
            this.ValueFromPipeline = (bool)(other.Members["ValueFromPipeline"].Value);
            this.HasParameterSet = (bool)(other.Members["HasParameterSet"].Value);
            this.ParameterType = new ShowCommandParameterType(other.Members["ParameterType"].Value as PSObject);
            this.Position = (int)(other.Members["Position"].Value);
            if (this.HasParameterSet)
            {
                this.ValidParamSetValues = ShowCommandCommandInfo.GetObjectEnumerable((other.Members["ValidParamSetValues"].Value as PSObject).BaseObject as System.Collections.ArrayList).Cast<string>().ToList();
            }
        }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <remarks>
        /// True if the parameter is dynamic, or false otherwise.
        /// </remarks>
        public bool IsMandatory { get; }

        /// <summary>
        /// Gets whether the parameter can take values from the incoming pipeline object.
        /// </summary>
        public bool ValueFromPipeline { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public ShowCommandParameterType ParameterType { get; }

        /// <summary>
        /// The possible values of this parameter.
        /// </summary>
        public IList<string> ValidParamSetValues { get; }

        /// <summary>
        /// Gets whether the parameter has a parameter set.
        /// </summary>
        public bool HasParameterSet { get; }

        /// <summary>
        /// Gets the position in which the parameter can be specified on the command line
        /// if not named. If the returned value is int.MinValue then the parameter must be named.
        /// </summary>
        public int Position { get; }
    }
}
