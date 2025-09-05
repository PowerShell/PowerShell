// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Threading;
using System.Windows;

using Microsoft.Management.UI;
using Microsoft.PowerShell.Commands.ShowCommandInternal;

namespace Microsoft.PowerShell.Commands.Internal
{
    /// <summary>
    /// Implements the WPF window part of the ShowWindow option of get-help.
    /// </summary>
    internal static class HelpWindowHelper
    {
        /// <summary>
        /// Shows the help window.
        /// </summary>
        /// <param name="helpObj">Object with help information.</param>
        /// <param name="cmdlet">Cmdlet calling this method.</param>
        [SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Called from methods called using reflection")]
        private static void ShowHelpWindow(PSObject helpObj, PSCmdlet cmdlet)
        {
            Window ownerWindow = ShowCommandHelper.GetHostWindow(cmdlet);
            if (ownerWindow != null)
            {
                ownerWindow.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        (_) =>
                        {
                            HelpWindow helpWindow = new HelpWindow(helpObj);
                            helpWindow.Owner = ownerWindow;
                            helpWindow.Show();

                            helpWindow.Closed += new EventHandler((sender, e) => ownerWindow.Focus());
                        }),
                        string.Empty);
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
