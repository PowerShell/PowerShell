//-----------------------------------------------------------------------
// <copyright file="HelpWindowHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Implements HelpWindowHelper
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.PowerShell.Commands.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using System.Threading;
    using System.Windows;
    using Microsoft.Management.UI;
    using Microsoft.PowerShell.Commands.ShowCommandInternal;

    /// <summary>
    /// Implements the WPF window part of the the ShowWindow option of get-help
    /// </summary>
    internal static class HelpWindowHelper
    {
        /// <summary>
        /// Shows the help window
        /// </summary>
        /// <param name="helpObj">object with help information</param>
        /// <param name="cmdlet">cmdlet calling this method</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from methods called using reflection")]
        private static void ShowHelpWindow(PSObject helpObj, PSCmdlet cmdlet)
        {
            Window ownerWindow = ShowCommandHelper.GetHostWindow(cmdlet);
            if (ownerWindow != null)
            {
                ownerWindow.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        delegate(object ignored)
                        {
                            HelpWindow helpWindow = new HelpWindow(helpObj);
                            helpWindow.Owner = ownerWindow;
                            helpWindow.Show();

                            helpWindow.Closed += new EventHandler(delegate(object sender, EventArgs e) { ownerWindow.Focus(); });
                        }),
                        String.Empty);
                return;
            }

            Thread guiThread = new Thread(
            (ThreadStart)delegate
            {
                HelpWindow helpWindow = new HelpWindow(helpObj);
                helpWindow.ShowDialog();
            });
            guiThread.SetApartmentState(ApartmentState.STA);
            guiThread.Start();
        }
    }
}
