// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that appends the specified content to the item at the specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "Content", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096807")]
    public class ClearContentCommand : ContentCommandBase
    {
        #region Command code

        /// <summary>
        /// Clears the contents from the item at the specified path.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Default to the CmdletProviderContext that will direct output to
            // the pipeline.

            CmdletProviderContext currentCommandContext = CmdletProviderContext;
            currentCommandContext.PassThru = false;

            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Content.Clear(path, currentCommandContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                }
            }
        }
        #endregion Command code

        /// <summary>
        /// Determines if the provider for the specified path supports ShouldProcess.
        /// </summary>
        /// <value></value>
        protected override bool ProviderSupportsShouldProcess
        {
            get
            {
                return base.DoesProviderSupportShouldProcess(base.Path);
            }
        }

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                // Go ahead an let any exceptions terminate the pipeline.
                return InvokeProvider.Content.ClearContentDynamicParameters(Path[0], context);
            }

            return InvokeProvider.Content.ClearContentDynamicParameters(".", context);
        }
    }
}
