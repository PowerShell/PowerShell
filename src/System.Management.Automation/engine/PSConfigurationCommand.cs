// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Configuration;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Represents the configuration file location for a PowerShell configuration scope.
    /// </summary>
    public sealed class PowerShellConfigurationInfo
    {
        /// <summary>
        /// Gets the configuration scope.
        /// </summary>
        public ConfigScope Scope { get; internal set; }

        /// <summary>
        /// Gets the file path of the configuration file.
        /// </summary>
        public string Path { get; internal set; }
    }

    /// <summary>
    /// Implements the Get-PowerShellConfiguration cmdlet.
    /// Returns the configuration file paths for AllUsers and/or CurrentUser scope.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "PowerShellConfiguration", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2296500")]
    [OutputType(typeof(PowerShellConfigurationInfo))]
    public sealed class GetPowerShellConfigurationCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the configuration scope to retrieve.
        /// When not specified, both AllUsers and CurrentUser configurations are returned.
        /// </summary>
        [Parameter(Position = 0)]
        public ConfigScope? Scope { get; set; }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (Scope.HasValue)
            {
                WriteObject(BuildConfigInfo(Scope.Value));
            }
            else
            {
                WriteObject(BuildConfigInfo(ConfigScope.AllUsers));
                WriteObject(BuildConfigInfo(ConfigScope.CurrentUser));
            }
        }

        private static PowerShellConfigurationInfo BuildConfigInfo(ConfigScope scope)
        {
            PowerShellConfig config = PowerShellConfig.Instance;

            return new PowerShellConfigurationInfo
            {
                Scope = scope,
                Path = scope == ConfigScope.AllUsers ? config.AllUsersConfigFilePath : config.CurrentUserConfigFilePath,
            };
        }
    }
}
