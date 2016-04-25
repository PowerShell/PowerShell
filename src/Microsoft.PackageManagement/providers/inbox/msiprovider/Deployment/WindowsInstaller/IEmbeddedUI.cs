//---------------------------------------------------------------------
// <copyright file="IEmbeddedUI.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// [MSI 4.5] Interface for an embedded external user interface for an installation.
    /// </summary>
    /// <remarks>
    /// Classes which implement this interface must have a public constructor that takes no parameters.
    /// </remarks>
    internal interface IEmbeddedUI
    {
        /// <summary>
        /// Initializes the embedded UI.
        /// </summary>
        /// <param name="session">Handle to the installer which can be used to get and set properties.
        /// The handle is only valid for the duration of this method call.</param>
        /// <param name="resourcePath">Path to the directory that contains all the files from the MsiEmbeddedUI table.</param>
        /// <param name="internalUILevel">On entry, contains the current UI level for the installation. After this
        /// method returns, the installer resets the UI level to the returned value of this parameter.</param>
        /// <returns>True if the embedded UI was successfully initialized; false if the installation
        /// should continue without the embedded UI.</returns>
        /// <exception cref="InstallCanceledException">The installation was canceled by the user.</exception>
        /// <exception cref="InstallerException">The embedded UI failed to initialize and
        /// causes the installation to fail.</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/initializeembeddedui.asp">InitializeEmbeddedUI</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        bool Initialize(Session session, string resourcePath, ref InstallUIOptions internalUILevel);

        /// <summary>
        /// Processes information and progress messages sent to the user interface.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="messageRecord">Record that contains message data.</param>
        /// <param name="buttons">Message buttons.</param>
        /// <param name="icon">Message box icon.</param>
        /// <param name="defaultButton">Message box default button.</param>
        /// <returns>Result of processing the message.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/embeddeduihandler.asp">EmbeddedUIHandler</a>
        /// </p></remarks>
        MessageResult ProcessMessage(
            InstallMessage messageType,
            Record messageRecord,
            MessageButtons buttons,
            MessageIcon icon,
            MessageDefaultButton defaultButton);

        /// <summary>
        /// Shuts down the embedded UI at the end of the installation.
        /// </summary>
        /// <remarks>
        /// If the installation was canceled during initialization, this method will not be called.
        /// If the installation was canceled or failed at any later point, this method will be called at the end.
        /// <p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/shutdownembeddedui.asp">ShutdownEmbeddedUI</a>
        /// </p></remarks>
        void Shutdown();
    }
}
