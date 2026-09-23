// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    /// <summary>
    /// Implements a facade around PSModuleInfo and its deserialized counterpart.
    /// </summary>
    public class ShowCommandModuleInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandModuleInfo"/> class
        /// with the specified <see cref="CommandInfo"/>.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandModuleInfo(PSModuleInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandModuleInfo"/> class
        /// with the specified <see cref="PSObject"/>.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandModuleInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
        }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        public string Name { get; }
    }
}
