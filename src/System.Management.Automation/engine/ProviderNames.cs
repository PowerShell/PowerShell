// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Defines the names of the internal providers.
    /// Derived classes exist for custom and single shells. In the single
    /// shell the provider name includes the PSSnapin name. In custom
    /// shells it does not.
    /// </summary>
    internal abstract class ProviderNames
    {
        /// <summary>
        /// Gets the name of the EnvironmentProvider.
        /// </summary>
        internal abstract string Environment { get; }

        /// <summary>
        /// Gets the name of the Certificate.
        /// </summary>
        internal abstract string Certificate { get; }

        /// <summary>
        /// Gets the name of the VariableProvider.
        /// </summary>
        internal abstract string Variable { get; }

        /// <summary>
        /// Gets the name of the AliasProvider.
        /// </summary>
        internal abstract string Alias { get; }

        /// <summary>
        /// Gets the name of the FunctionProvider.
        /// </summary>
        internal abstract string Function { get; }

        /// <summary>
        /// Gets the name of the FileSystemProvider.
        /// </summary>
        internal abstract string FileSystem { get; }

        /// <summary>
        /// Gets the name of the RegistryProvider.
        /// </summary>
        internal abstract string Registry { get; }
    }

    /// <summary>
    /// The provider names for the single shell.
    /// </summary>
    internal class SingleShellProviderNames : ProviderNames
    {
        /// <summary>
        /// Gets the name of the EnvironmentProvider.
        /// </summary>
        internal override string Environment
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Environment";
            }
        }

        /// <summary>
        /// Gets the name of the Certificate.
        /// </summary>
        internal override string Certificate
        {
            get
            {
                return "Microsoft.PowerShell.Security\\Certificate";
            }
        }

        /// <summary>
        /// Gets the name of the VariableProvider.
        /// </summary>
        internal override string Variable
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Variable";
            }
        }

        /// <summary>
        /// Gets the name of the AliasProvider.
        /// </summary>
        internal override string Alias
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Alias";
            }
        }

        /// <summary>
        /// Gets the name of the FunctionProvider.
        /// </summary>
        internal override string Function
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Function";
            }
        }

        /// <summary>
        /// Gets the name of the FileSystemProvider.
        /// </summary>
        internal override string FileSystem
        {
            get
            {
                return "Microsoft.PowerShell.Core\\FileSystem";
            }
        }

        /// <summary>
        /// Gets the name of the RegistryProvider.
        /// </summary>
        internal override string Registry
        {
            get
            {
                return "Microsoft.PowerShell.Core\\Registry";
            }
        }
    }
}

