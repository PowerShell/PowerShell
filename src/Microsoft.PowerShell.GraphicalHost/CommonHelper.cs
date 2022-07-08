//-----------------------------------------------------------------------
// <copyright file="CommonHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Implements HelpWindow.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI
{
    using System.Windows;

    /// <summary>
    /// Utilities in common in this assembly
    /// </summary>
    internal static class CommonHelper
    {
        /// <summary>
        /// Restore the values from the settings to the actual window position, size and state.
        /// </summary>
        /// <param name="target">the window we are setting position and size of</param>
        /// <param name="userSettingTop">the value for top from the user settings</param>
        /// <param name="userSettingLeft">the value for left from the user settings</param>
        /// <param name="userSettingWidth">the value for width from the user settings</param>
        /// <param name="userSettingHeight">the value for height from the user settings</param>
        /// <param name="defaultWidth">the with used if <paramref name="userSettingWidth"/> is not valid</param>
        /// <param name="defaultHeight">the height used if <paramref name="userSettingHeight"/> is not valid</param>
        /// <param name="userSettingMaximized">true if the window is maximized in the user setting</param>
        internal static void SetStartingPositionAndSize(Window target, double userSettingTop, double userSettingLeft, double userSettingWidth, double userSettingHeight, double defaultWidth, double defaultHeight, bool userSettingMaximized)
        {
            bool leftInvalid = userSettingLeft < System.Windows.SystemParameters.VirtualScreenLeft ||
                            userSettingWidth > System.Windows.SystemParameters.VirtualScreenLeft +
                            System.Windows.SystemParameters.VirtualScreenWidth;

            bool topInvalid = userSettingTop < System.Windows.SystemParameters.VirtualScreenTop ||
                userSettingTop > System.Windows.SystemParameters.VirtualScreenTop +
                            System.Windows.SystemParameters.VirtualScreenHeight;

            bool widthInvalid = userSettingWidth < 0 ||
                userSettingWidth > System.Windows.SystemParameters.VirtualScreenWidth;

            bool heightInvalid = userSettingHeight < 0 ||
                userSettingHeight > System.Windows.SystemParameters.VirtualScreenHeight;

            if (leftInvalid || topInvalid)
            {
                target.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
            else
            {
                target.Left = userSettingLeft;
                target.Top = userSettingTop;
            }

            // If any saved coordinate is invalid, we set the window to the default position
            if (widthInvalid || heightInvalid)
            {
                target.Width = defaultWidth;
                target.Height = defaultHeight;
            }
            else
            {
                target.Width = userSettingWidth;
                target.Height = userSettingHeight;
            }

            if (userSettingMaximized)
            {
                target.WindowState = WindowState.Maximized;
            }
        }
    }
}
