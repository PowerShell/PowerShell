// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command that appends the specified content to the item at the specified path.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Content", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113278")]
    public class AddContentCommand : WriteContentCommandBase
    {
        #region protected members

        /// <summary>
        /// Seeks to the end of the writer stream in each of the writers in the
        /// content holders.
        /// </summary>
        /// <param name="contentHolders">
        /// The content holders that contain the writers to be moved.
        /// </param>
        /// <exception cref="ProviderInvocationException">
        /// If calling Seek on the content writer throws an exception.
        /// </exception>
        internal override void SeekContentPosition(List<ContentHolder> contentHolders)
        {
            foreach (ContentHolder holder in contentHolders)
            {
                if (holder.Writer != null)
                {
                    try
                    {
                        holder.Writer.Seek(0, System.IO.SeekOrigin.End);
                    }
                    catch (Exception e) // Catch-all OK, 3rd party callout
                    {
                        ProviderInvocationException providerException =
                            new ProviderInvocationException(
                                "ProviderSeekError",
                                SessionStateStrings.ProviderSeekError,
                                holder.PathInfo.Provider,
                                holder.PathInfo.Path,
                                e);

                        // Log a provider health event

                        MshLog.LogProviderHealthEvent(
                            this.Context,
                            holder.PathInfo.Provider.Name,
                            providerException,
                            Severity.Warning);

                        throw providerException;
                    }
                }
            }
        }

        /// <summary>
        /// Makes the call to ShouldProcess with appropriate action and target strings.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which the content will be added.
        /// </param>
        /// <returns>
        /// True if the action should continue or false otherwise.
        /// </returns>
        internal override bool CallShouldProcess(string path)
        {
            string action = NavigationResources.AddContentAction;

            string target = StringUtil.Format(NavigationResources.AddContentTarget, path);

            return ShouldProcess(target, action);
        }

        #endregion protected members
    }
}

