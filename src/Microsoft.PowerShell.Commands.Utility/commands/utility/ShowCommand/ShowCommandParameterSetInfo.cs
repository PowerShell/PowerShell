// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    /// <summary>
    /// Implements a facade around CommandParameterSetInfo and its deserialized counterpart.
    /// </summary>
    public class ShowCommandParameterSetInfo
    {
        /// <summary>
        /// Creates an instance of the ShowCommandParameterSetInfo class based on a CommandParameterSetInfo object.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterSetInfo(CommandParameterSetInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            this.Name = other.Name;
            this.IsDefault = other.IsDefault;
            this.Parameters = other.Parameters.Select(x => new ShowCommandParameterInfo(x)).ToArray();
        }

        /// <summary>
        /// Creates an instance of the ShowCommandParameterSetInfo class based on a PSObject object.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterSetInfo(PSObject other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            this.Name = other.Members["Name"].Value as string;
            this.IsDefault = (bool)(other.Members["IsDefault"].Value);
            var parameters = (other.Members["Parameters"].Value as PSObject).BaseObject as System.Collections.ArrayList;
            this.Parameters = ShowCommandCommandInfo.GetObjectEnumerable(parameters).Cast<PSObject>().Select(x => new ShowCommandParameterInfo(x)).ToArray();
        }

        /// <summary>
        /// Gets the name of the parameter set.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets whether the parameter set is the default parameter set.
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// Gets the parameter information for the parameters in this parameter set.
        /// </summary>
        public ICollection<ShowCommandParameterInfo> Parameters { get; private set; }
    }
}
