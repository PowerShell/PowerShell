//---------------------------------------------------------------------
// <copyright file="Installer.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Resources;
    using System.Text;

    /// <summary>
/// Receives an exception from
/// <see cref="Installer.DetermineApplicablePatches(string,string[],InapplicablePatchHandler,string,UserContexts)"/>
/// indicating the reason a particular patch is not applicable to a product.
/// </summary>
/// <param name="patch">MSP file path, XML file path, or XML blob that was passed to
/// <see cref="Installer.DetermineApplicablePatches(string,string[],InapplicablePatchHandler,string,UserContexts)"/></param>
/// <param name="exception">exception indicating the reason the patch is not applicable</param>
/// <remarks><p>
/// If <paramref name="exception"/> is an <see cref="InstallerException"/> or subclass, then
/// its <see cref="InstallerException.ErrorCode"/> and <see cref="InstallerException.Message"/>
/// properties will indicate a more specific reason the patch was not applicable.
/// </p><p>
/// The <paramref name="exception"/> could also be a FileNotFoundException if the
/// patch string was a file path.
/// </p></remarks>
public delegate void InapplicablePatchHandler(string patch, Exception exception);

/// <summary>
/// Provides static methods for installing and configuring products and patches.
/// </summary>
internal static partial class Installer
{
    private static bool rebootRequired;
    private static bool rebootInitiated;
    private static ResourceManager errorResources;

    /// <summary>
    /// Indicates whether a system reboot is required after running an installation or configuration operation.
    /// </summary>
    public static bool RebootRequired
    {
        get
        {
            return Installer.rebootRequired;
        }
    }

    /// <summary>
    /// Indicates whether a system reboot has been initiated after running an installation or configuration operation.
    /// </summary>
    public static bool RebootInitiated
    {
        get
        {
            return Installer.rebootInitiated;
        }
    }

    /// <summary>
    /// Enables the installer's internal user interface. Then this user interface is used
    /// for all subsequent calls to user-interface-generating installer functions in this process.
    /// </summary>
    /// <param name="uiOptions">Specifies the level of complexity of the user interface</param>
    /// <param name="windowHandle">Handle to a window, which becomes the owner of any user interface created.
    /// A pointer to the previous owner of the user interface is returned.</param>
    /// <returns>The previous user interface level</returns>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetinternalui.asp">MsiSetInternalUI</a>
    /// </p></remarks>
    [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
    public static InstallUIOptions SetInternalUI(InstallUIOptions uiOptions, ref IntPtr windowHandle)
    {
        return (InstallUIOptions) NativeMethods.MsiSetInternalUI((uint) uiOptions, ref windowHandle);
    }

    /// <summary>
    /// Enables the installer's internal user interface. Then this user interface is used
    /// for all subsequent calls to user-interface-generating installer functions in this process.
    /// The owner of the user interface does not change.
    /// </summary>
    /// <param name="uiOptions">Specifies the level of complexity of the user interface</param>
    /// <returns>The previous user interface level</returns>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetinternalui.asp">MsiSetInternalUI</a>
    /// </p></remarks>
    public static InstallUIOptions SetInternalUI(InstallUIOptions uiOptions)
    {
        return (InstallUIOptions) NativeMethods.MsiSetInternalUI((uint) uiOptions, IntPtr.Zero);
    }

    /// <summary>
    /// Enables logging of the selected message type for all subsequent install sessions in
    /// the current process space.
    /// </summary>
    /// <param name="logModes">One or more mode flags specifying the type of messages to log</param>
    /// <param name="logFile">Full path to the log file.  A null path disables logging,
    /// in which case the logModes parameter is ignored.</param>
    /// <exception cref="ArgumentException">an invalid log mode was specified</exception>
    /// <remarks>This method takes effect on any new installation processes.  Calling this
    /// method from within a custom action will not start logging for that installation.</remarks>
    public static void EnableLog(InstallLogModes logModes, string logFile)
    {
        Installer.EnableLog(logModes, logFile, false, true);
    }

    /// <summary>
    /// Enables logging of the selected message type for all subsequent install sessions in
    /// the current process space.
    /// </summary>
    /// <param name="logModes">One or more mode flags specifying the type of messages to log</param>
    /// <param name="logFile">Full path to the log file.  A null path disables logging,
    /// in which case the logModes parameter is ignored.</param>
    /// <param name="append">If true, the log lines will be appended to any existing file content.
    /// If false, the log file will be truncated if it exists.  The default is false.</param>
    /// <param name="flushEveryLine">If true, the log will be flushed after every line.
    /// If false, the log will be flushed every 20 lines.  The default is true.</param>
    /// <exception cref="ArgumentException">an invalid log mode was specified</exception>
    /// <remarks><p>
    /// This method takes effect on any new installation processes.  Calling this
    /// method from within a custom action will not start logging for that installation.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienablelog.asp">MsiEnableLog</a>
    /// </p></remarks>
    public static void EnableLog(InstallLogModes logModes, string logFile, bool append, bool flushEveryLine)
    {
        uint ret = NativeMethods.MsiEnableLog((uint) logModes, logFile, (append ? (uint) 1 : 0) + (flushEveryLine ? (uint) 2 : 0));
        if (ret != 0 && ret != (uint) NativeMethods.Error.FILE_INVALID)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    /// <summary>
    /// increments the usage count for a particular feature and returns the installation state for
    /// that feature. This method should be used to indicate an application's intent to use a feature.
    /// </summary>
    /// <param name="productCode">The product code of the product.</param>
    /// <param name="feature">The feature to be used.</param>
    /// <param name="installMode">Must have the value <see cref="InstallMode.NoDetection"/>.</param>
    /// <returns>The installed state of the feature.</returns>
    /// <remarks><p>
    /// The UseFeature method should only be used on features known to be published. The application
    /// should determine the status of the feature by calling either the FeatureState method or
    /// Features method.
    /// </p><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiusefeature.asp">MsiUseFeature</a>,
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiusefeatureex.asp">MsiUseFeatureEx</a>
    /// </p></remarks>
    public static InstallState UseFeature(string productCode, string feature, InstallMode installMode)
    {
        int installState = NativeMethods.MsiUseFeatureEx(productCode, feature, unchecked ((uint) installMode), 0);
        return (InstallState) installState;
    }

    /// <summary>
    /// Opens an installer package for use with functions that access the product database and install engine,
    /// returning an Session object.
    /// </summary>
    /// <param name="packagePath">Path to the package</param>
    /// <param name="ignoreMachineState">Specifies whether or not the create a Session object that ignores the
    /// computer state and that is incapable of changing the current computer state. A value of false yields
    /// the normal behavior.  A value of true creates a "safe" Session object that cannot change of the current
    /// machine state.</param>
    /// <returns>A Session object allowing access to the product database and install engine</returns>
    /// <exception cref="InstallerException">The product could not be opened</exception>
    /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
    /// <remarks><p>
    /// Note that only one Session object can be opened by a single process. OpenPackage cannot be used in a
    /// custom action because the active installation is the only session allowed.
    /// </p><p>
    /// A "safe" Session object ignores the current computer state when opening the package and prevents
    /// changes to the current computer state.
    /// </p><p>
    /// The Session object should be <see cref="InstallerHandle.Close"/>d after use.
    /// It is best that the handle be closed manually as soon as it is no longer
    /// needed, as leaving lots of unused handles open can degrade performance.
    /// </p><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopenpackage.asp">MsiOpenPackage</a>,
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopenpackageex.asp">MsiOpenPackageEx</a>
    /// </p></remarks>
    public static Session OpenPackage(string packagePath, bool ignoreMachineState)
    {
        int sessionHandle;
        uint ret = NativeMethods.MsiOpenPackageEx(packagePath, ignoreMachineState ? (uint) 1 : 0, out sessionHandle);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
        return new Session((IntPtr) sessionHandle, true);
    }

    /// <summary>
    /// Opens an installer package for use with functions that access the product database and install engine,
    /// returning an Session object.
    /// </summary>
    /// <param name="database">Database used to create the session</param>
    /// <param name="ignoreMachineState">Specifies whether or not the create a Session object that ignores the
    /// computer state and that is incapable of changing the current computer state. A value of false yields
    /// the normal behavior.  A value of true creates a "safe" Session object that cannot change of the current
    /// machine state.</param>
    /// <returns>A Session object allowing access to the product database and install engine</returns>
    /// <exception cref="InstallerException">The product could not be opened</exception>
    /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
    /// <remarks><p>
    /// Note that only one Session object can be opened by a single process. OpenPackage cannot be used in a
    /// custom action because the active installation is the only session allowed.
    /// </p><p>
    /// A "safe" Session object ignores the current computer state when opening the package and prevents
    /// changes to the current computer state.
    /// </p><p>
    /// The Session object should be <see cref="InstallerHandle.Close"/>d after use.
    /// It is best that the handle be closed manually as soon as it is no longer
    /// needed, as leaving lots of unused handles open can degrade performance.
    /// </p><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopenpackage.asp">MsiOpenPackage</a>,
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopenpackageex.asp">MsiOpenPackageEx</a>
    /// </p></remarks>
    [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
    public static Session OpenPackage(Database database, bool ignoreMachineState)
    {
        if (database == null)
        {
            throw new ArgumentNullException("database");
        }

        return Installer.OpenPackage(
            String.Format(CultureInfo.InvariantCulture, "#{0}", database.Handle),
            ignoreMachineState);
    }

    /// <summary>
    /// Opens an installer package for an installed product using the product code.
    /// </summary>
    /// <param name="productCode">Product code of the installed product</param>
    /// <returns>A Session object allowing access to the product database and install engine,
    /// or null if the specified product is not installed.</returns>
    /// <exception cref="ArgumentException">An unknown product was requested</exception>
    /// <exception cref="InstallerException">The product could not be opened</exception>
    /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
    /// <remarks><p>
    /// Note that only one Session object can be opened by a single process. OpenProduct cannot be
    /// used in a custom action because the active installation is the only session allowed.
    /// </p><p>
    /// The Session object should be <see cref="InstallerHandle.Close"/>d after use.
    /// It is best that the handle be closed manually as soon as it is no longer
    /// needed, as leaving lots of unused handles open can degrade performance.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiopenproduct.asp">MsiOpenProduct</a>
    /// </p></remarks>
    public static Session OpenProduct(string productCode)
    {
        int sessionHandle;
        uint ret = NativeMethods.MsiOpenProduct(productCode, out sessionHandle);
        if (ret != 0)
        {
            if (ret == (uint) NativeMethods.Error.UNKNOWN_PRODUCT)
            {
                return null;
            }
            else
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }
        return new Session((IntPtr) sessionHandle, true);
    }

    /// <summary>
    /// Gets the full component path, performing any necessary installation. This method prompts for source if
    /// necessary and increments the usage count for the feature.
    /// </summary>
    /// <param name="product">Product code for the product that contains the feature with the necessary component</param>
    /// <param name="feature">Feature ID of the feature with the necessary component</param>
    /// <param name="component">Component code of the necessary component</param>
    /// <param name="installMode">Installation mode; this can also include bits from <see cref="ReinstallModes"/></param>
    /// <returns>Path to the component</returns>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprovidecomponent.asp">MsiProvideComponent</a>
    /// </p></remarks>
    public static string ProvideComponent(string product, string feature, string component, InstallMode installMode)
    {
        StringBuilder pathBuf = new StringBuilder(512);
        uint pathBufSize = (uint) pathBuf.Capacity;
        uint ret = NativeMethods.MsiProvideComponent(product, feature, component, unchecked((uint)installMode), pathBuf, ref pathBufSize);
        if (ret == (uint) NativeMethods.Error.MORE_DATA)
        {
            pathBuf.Capacity = (int) ++pathBufSize;
            ret = NativeMethods.MsiProvideComponent(product, feature, component, unchecked((uint)installMode), pathBuf, ref pathBufSize);
        }

        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
        return pathBuf.ToString();
    }

    /// <summary>
    /// Gets the full component path for a qualified component that is published by a product and
    /// performs any necessary installation. This method prompts for source if necessary and increments
    /// the usage count for the feature.
    /// </summary>
    /// <param name="component">Specifies the component ID for the requested component. This may not be the
    /// GUID for the component itself but rather a server that provides the correct functionality, as in the
    /// ComponentId column of the PublishComponent table.</param>
    /// <param name="qualifier">Specifies a qualifier into a list of advertising components (from PublishComponent Table).</param>
    /// <param name="installMode">Installation mode; this can also include bits from <see cref="ReinstallModes"/></param>
    /// <param name="product">Optional; specifies the product to match that has published the qualified component.</param>
    /// <returns>Path to the component</returns>
    /// <remarks><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprovidequalifiedcomponent.asp">MsiProvideQualifiedComponent</a>
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprovidequalifiedcomponentex.asp">MsiProvideQualifiedComponentEx</a>
    /// </p></remarks>
    public static string ProvideQualifiedComponent(string component, string qualifier, InstallMode installMode, string product)
    {
        StringBuilder pathBuf = new StringBuilder(512);
        uint pathBufSize = (uint) pathBuf.Capacity;
        uint ret = NativeMethods.MsiProvideQualifiedComponentEx(component, qualifier, unchecked((uint)installMode), product, 0, 0, pathBuf, ref pathBufSize);
        if (ret == (uint) NativeMethods.Error.MORE_DATA)
        {
            pathBuf.Capacity = (int) ++pathBufSize;
            ret = NativeMethods.MsiProvideQualifiedComponentEx(component, qualifier, unchecked((uint)installMode), product, 0, 0, pathBuf, ref pathBufSize);
        }

        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
        return pathBuf.ToString();
    }

    /// <summary>
    /// Gets the full path to a Windows Installer component containing an assembly. This method prompts for a source and
    /// increments the usage count for the feature.
    /// </summary>
    /// <param name="assemblyName">Assembly name</param>
    /// <param name="appContext">Set to null for global assemblies. For private assemblies, set to the full path of the
    /// application configuration file (.cfg file) or executable file (.exe) of the application to which the assembly
    /// has been made private.</param>
    /// <param name="installMode">Installation mode; this can also include bits from <see cref="ReinstallModes"/></param>
    /// <param name="isWin32Assembly">True if this is a Win32 assembly, false if it is a .NET assembly</param>
    /// <returns>Path to the assembly</returns>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiprovideassembly.asp">MsiProvideAssembly</a>
    /// </p></remarks>
    public static string ProvideAssembly(string assemblyName, string appContext, InstallMode installMode, bool isWin32Assembly)
    {
        StringBuilder pathBuf = new StringBuilder(512);
        uint pathBufSize = (uint) pathBuf.Capacity;
        uint ret = NativeMethods.MsiProvideAssembly(assemblyName, appContext, unchecked ((uint) installMode), (isWin32Assembly ? (uint) 1 : 0), pathBuf, ref pathBufSize);
        if (ret == (uint) NativeMethods.Error.MORE_DATA)
        {
            pathBuf.Capacity = (int) ++pathBufSize;
            ret = NativeMethods.MsiProvideAssembly(assemblyName, appContext, unchecked ((uint) installMode), (isWin32Assembly ? (uint) 1 : 0), pathBuf, ref pathBufSize);
        }

        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
        return pathBuf.ToString();
    }

    /// <summary>
    /// Installs files that are unexpectedly missing.
    /// </summary>
    /// <param name="product">Product code for the product that owns the component to be installed</param>
    /// <param name="component">Component to be installed</param>
    /// <param name="installState">Specifies the way the component should be installed.</param>
    /// <exception cref="InstallCanceledException">the user exited the installation</exception>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiinstallmissingcomponent.asp">MsiInstallMissingComponent</a>
    /// </p></remarks>
    public static void InstallMissingComponent(string product, string component, InstallState installState)
    {
        uint ret = NativeMethods.MsiInstallMissingComponent(product, component, (int) installState);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    /// <summary>
    /// Installs files that are unexpectedly missing.
    /// </summary>
    /// <param name="product">Product code for the product that owns the file to be installed</param>
    /// <param name="file">File to be installed</param>
    /// <exception cref="InstallCanceledException">the user exited the installation</exception>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiinstallmissingfile.asp">MsiInstallMissingFile</a>
    /// </p></remarks>
    public static void InstallMissingFile(string product, string file)
    {
        uint ret = NativeMethods.MsiInstallMissingFile(product, file);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    /// <summary>
    /// Reinstalls a feature.
    /// </summary>
    /// <param name="product">Product code for the product containing the feature to be reinstalled</param>
    /// <param name="feature">Feature to be reinstalled</param>
    /// <param name="reinstallModes">Reinstall modes</param>
    /// <exception cref="InstallCanceledException">the user exited the installation</exception>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msireinstallfeature.asp">MsiReinstallFeature</a>
    /// </p></remarks>
    public static void ReinstallFeature(string product, string feature, ReinstallModes reinstallModes)
    {
        uint ret = NativeMethods.MsiReinstallFeature(product, feature, (uint) reinstallModes);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    /// <summary>
    /// Reinstalls a product.
    /// </summary>
    /// <param name="product">Product code for the product to be reinstalled</param>
    /// <param name="reinstallModes">Reinstall modes</param>
    /// <exception cref="InstallCanceledException">the user exited the installation</exception>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msireinstallproduct.asp">MsiReinstallProduct</a>
    /// </p></remarks>
    public static void ReinstallProduct(string product, ReinstallModes reinstallModes)
    {
        uint ret = NativeMethods.MsiReinstallProduct(product, (uint) reinstallModes);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    /// <summary>
    /// Opens an installer package and initializes an install session.
    /// </summary>
    /// <param name="packagePath">path to the patch package</param>
    /// <param name="commandLine">command line property settings</param>
    /// <exception cref="InstallerException">There was an error installing the product</exception>
    /// <remarks><p>
    /// To completely remove a product, set REMOVE=ALL in <paramRef name="commandLine"/>.
    /// </p><p>
    /// This method displays the user interface with the current settings and
    /// log mode. You can change user interface settings with the <see cref="SetInternalUI(InstallUIOptions)"/>
    /// and <see cref="SetExternalUI(ExternalUIHandler,InstallLogModes)"/> functions. You can set the log mode with the
    /// <see cref="EnableLog(InstallLogModes,string)"/> function.
    /// </p><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiinstallproduct.asp">MsiInstallProduct</a>
    /// </p></remarks>
    public static void InstallProduct(string packagePath, string commandLine)
    {
        uint ret = NativeMethods.MsiInstallProduct(packagePath, commandLine);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// Installs or uninstalls a product.
    /// </summary>
    /// <param name="productCode">Product code of the product to be configured.</param>
    /// <param name="installLevel">Specifies the default installation configuration of the
    /// product. The <paramref name="installLevel"/> parameter is ignored and all features
    /// are installed if the <paramref name="installState"/> parameter is set to any other
    /// value than <see cref="InstallState.Default"/>. This parameter must be either 0
    /// (install using authored feature levels), 65535 (install all features), or a value
    /// between 0 and 65535 to install a subset of available features.																																											   </param>
    /// <param name="installState">Specifies the installation state for the product.</param>
    /// <param name="commandLine">Specifies the command line property settings. This should
    /// be a list of the format Property=Setting Property=Setting.</param>
    /// <exception cref="InstallerException">There was an error configuring the product</exception>
    /// <remarks><p>
    /// This method displays the user interface with the current settings and
    /// log mode. You can change user interface settings with the <see cref="SetInternalUI(InstallUIOptions)"/>
    /// and <see cref="SetExternalUI(ExternalUIHandler,InstallLogModes)"/> functions. You can set the log mode with the
    /// <see cref="EnableLog(InstallLogModes,string)"/> function.
    /// </p><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiconfigureproduct.asp">MsiConfigureProduct</a>,
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiconfigureproductex.asp">MsiConfigureProductEx</a>
    /// </p></remarks>
    public static void ConfigureProduct(string productCode, int installLevel, InstallState installState, string commandLine)
    {
        uint ret = NativeMethods.MsiConfigureProductEx(productCode, installLevel, (int) installState, commandLine);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// Configures the installed state for a product feature.
    /// </summary>
    /// <param name="productCode">Product code of the product to be configured.</param>
    /// <param name="feature">Specifies the feature ID for the feature to be configured.</param>
    /// <param name="installState">Specifies the installation state for the feature.</param>
    /// <exception cref="InstallerException">There was an error configuring the feature</exception>
    /// <remarks><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiconfigurefeature.asp">MsiConfigureFeature</a>
    /// </p></remarks>
    public static void ConfigureFeature(string productCode, string feature, InstallState installState)
    {
        uint ret = NativeMethods.MsiConfigureFeature(productCode, feature, (int) installState);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// For each product listed by the patch package as eligible to receive the patch, ApplyPatch invokes
    /// an installation and sets the PATCH property to the path of the patch package.
    /// </summary>
    /// <param name="patchPackage">path to the patch package</param>
    /// <param name="commandLine">optional command line property settings</param>
    /// <exception cref="InstallerException">There was an error applying the patch</exception>
    /// <remarks><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiapplypatch.asp">MsiApplyPatch</a>
    /// </p></remarks>
    public static void ApplyPatch(string patchPackage, string commandLine)
    {
        Installer.ApplyPatch(patchPackage, null, InstallType.Default, commandLine);
    }

    /// <summary>
    /// For each product listed by the patch package as eligible to receive the patch, ApplyPatch invokes
    /// an installation and sets the PATCH property to the path of the patch package.
    /// </summary>
    /// <param name="patchPackage">path to the patch package</param>
    /// <param name="installPackage">path to the product to be patched, if installType
    /// is set to <see cref="InstallType.NetworkImage"/></param>
    /// <param name="installType">type of installation to patch</param>
    /// <param name="commandLine">optional command line property settings</param>
    /// <exception cref="InstallerException">There was an error applying the patch</exception>
    /// <remarks><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiapplypatch.asp">MsiApplyPatch</a>
    /// </p></remarks>
    public static void ApplyPatch(string patchPackage, string installPackage, InstallType installType, string commandLine)
    {
        uint ret = NativeMethods.MsiApplyPatch(patchPackage, installPackage, (int) installType, commandLine);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// Removes one or more patches from a single product. To remove a patch from
    /// multiple products, RemovePatches must be called for each product.
    /// </summary>
    /// <param name="patches">List of patches to remove. Each patch can be specified by the GUID
    /// of the patch or the full path to the patch package.</param>
    /// <param name="productCode">The ProductCode (GUID) of the product from which the patches
    /// are removed.  This parameter cannot be null.</param>
    /// <param name="commandLine">optional command line property settings</param>
    /// <exception cref="InstallerException">There was an error removing the patches</exception>
    /// <remarks><p>
    /// The <see cref="RebootRequired"/> and <see cref="RebootInitiated"/> properties should be
    /// tested after calling this method.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiremovepatches.asp">MsiRemovePatches</a>
    /// </p></remarks>
    public static void RemovePatches(IList<string> patches, string productCode, string commandLine)
    {
        if (patches == null || patches.Count == 0)
        {
            throw new ArgumentNullException("patches");
        }

        if (productCode == null)
        {
            throw new ArgumentNullException("productCode");
        }

        StringBuilder patchList = new StringBuilder();
        foreach (string patch in patches)
        {
            if (patch != null)
            {
                if (patchList.Length != 0)
                {
                    patchList.Append(';');
                }

                patchList.Append(patch);
            }
        }

        if (patchList.Length == 0)
        {
            throw new ArgumentNullException("patches");
        }

        uint ret = NativeMethods.MsiRemovePatches(patchList.ToString(), productCode, (int) InstallType.SingleInstance, commandLine);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// Determines which patches apply to a specified product MSI and in what sequence.
    /// </summary>
    /// <param name="productPackage">Full path to an MSI file that is the target product
    /// for the set of patches.</param>
    /// <param name="patches">An array of strings specifying the patches to be checked.  Each item
    /// may be the path to an MSP file, the path an XML file, or just an XML blob.</param>
    /// <param name="errorHandler">Callback to be invoked for each inapplicable patch, reporting the
    /// reason the patch is not applicable.  This value may be left null if that information is not
    /// desired.</param>
    /// <returns>An array of selected patch strings from <paramref name="patches"/>, indicating
    /// the set of applicable patches.  The items are re-ordered to be in the best sequence.</returns>
    /// <remarks><p>
    /// If an item in <paramref name="patches"/> is a file path but does not end in .MSP or .XML,
    /// it is assumed to be an MSP file.
    /// </p><p>
    /// As this overload uses InstallContext.None, it does not consider the current state of
    /// the system.
    /// </p><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidetermineapplicablepatches.asp">MsiDetermineApplicablePatches</a>
    /// </p></remarks>
    public static IList<string> DetermineApplicablePatches(
        string productPackage,
        string[] patches,
        InapplicablePatchHandler errorHandler)
    {
        return DetermineApplicablePatches(productPackage, patches, errorHandler, null, UserContexts.None);
    }

    /// <summary>
    /// Determines which patches apply to a specified product and in what sequence.  If
    /// the product is installed, this method accounts for patches that have already been applied to
    /// the product and accounts for obsolete and superceded patches.
    /// </summary>
    /// <param name="product">The product that is the target for the set of patches.  This may be
    /// either a ProductCode (GUID) of a product that is currently installed, or the path to a an
    /// MSI file.</param>
    /// <param name="patches">An array of strings specifying the patches to be checked.  Each item
    /// may be the path to an MSP file, the path an XML file, or just an XML blob.</param>
    /// <param name="errorHandler">Callback to be invoked for each inapplicable patch, reporting the
    /// reason the patch is not applicable.  This value may be left null if that information is not
    /// desired.</param>
    /// <param name="userSid">Specifies a security identifier (SID) of a user. This parameter restricts
    /// the context of enumeration for this user account. This parameter cannot be the special SID
    /// strings s-1-1-0 (everyone) or s-1-5-18 (local system). If <paramref name="context"/> is set to
    /// <see cref="UserContexts.None"/> or <see cref="UserContexts.Machine"/>, then
    /// <paramref name="userSid"/> must be null. For the current user context, <paramref name="userSid"/>
    /// can be null and <paramref name="context"/> can be set to <see cref="UserContexts.UserManaged"/>
    /// or <see cref="UserContexts.UserUnmanaged"/>.</param>
    /// <param name="context">Restricts the enumeration to per-user-unmanaged, per-user-managed,
    /// or per-machine context, or (if referring to an MSI) to no system context at all.  This
    /// parameter can be <see cref="UserContexts.Machine"/>, <see cref="UserContexts.UserManaged"/>,
    /// <see cref="UserContexts.UserUnmanaged"/>, or <see cref="UserContexts.None"/>.</param>
    /// <returns>An array of selected patch strings from <paramref name="patches"/>, indicating
    /// the set of applicable patches.  The items are re-ordered to be in the best sequence.</returns>
    /// <remarks><p>
    /// If an item in <paramref name="patches"/> is a file path but does not end in .MSP or .XML,
    /// it is assumed to be an MSP file.
    /// </p><p>
    /// Passing an InstallContext of None only analyzes the MSI file; it does not consider the
    /// current state of the system. You cannot use InstallContext.None with a ProductCode GUID.
    /// </p><p>
    /// Win32 MSI APIs:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidetermineapplicablepatches.asp">MsiDetermineApplicablePatches</a>
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msideterminepatchsequence.asp">MsiDeterminePatchSequence</a>
    /// </p></remarks>
    public static IList<string> DetermineApplicablePatches(
        string product,
        string[] patches,
        InapplicablePatchHandler errorHandler,
        string userSid,
        UserContexts context)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentNullException("product");
        }

        if (patches == null)
        {
            throw new ArgumentNullException("patches");
        }

        NativeMethods.MsiPatchSequenceData[] sequenceData = new NativeMethods.MsiPatchSequenceData[patches.Length];
        for (int i = 0; i < patches.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(patches[i]))
            {
                throw new ArgumentNullException("patches[" + i + "]");
            }

            sequenceData[i].szPatchData = patches[i];
            sequenceData[i].ePatchDataType = GetPatchStringDataType(patches[i]);
            sequenceData[i].dwOrder = -1;
            sequenceData[i].dwStatus = 0;
        }

        uint ret;
        if (context == UserContexts.None)
        {
            ret = NativeMethods.MsiDetermineApplicablePatches(product, (uint) sequenceData.Length, sequenceData);
        }
        else
        {
            ret = NativeMethods.MsiDeterminePatchSequence(product, userSid, context, (uint) sequenceData.Length, sequenceData);
        }

        if (errorHandler != null)
        {
            for (int i = 0; i < sequenceData.Length; i++)
            {
                if (sequenceData[i].dwOrder < 0 && sequenceData[i].dwStatus != 0)
                {
                    errorHandler(sequenceData[i].szPatchData, InstallerException.ExceptionFromReturnCode(sequenceData[i].dwStatus));
                }
            }
        }

        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }

        IList<string> patchSeq = new List<string>(patches.Length);
        for (int i = 0; i < sequenceData.Length; i++)
        {
            for (int j = 0; j < sequenceData.Length; j++)
            {
                if (sequenceData[j].dwOrder == i)
                {
                    patchSeq.Add(sequenceData[j].szPatchData);
                }
            }
        }
        return patchSeq;
    }

    /// <summary>
    /// Applies one or more patches to products that are eligible to receive the patch.
    /// For each product listed by the patch package as eligible to receive the patch, ApplyPatch invokes
    /// an installation and sets the PATCH property to the path of the patch package.
    /// </summary>
    /// <param name="patchPackages">The set of patch packages to be applied.
    /// Each item is the full path to an MSP file.</param>
    /// <param name="productCode">Provides the ProductCode of the product being patched. If this parameter
    /// is null, the patches are applied to all products that are eligible to receive these patches.</param>
    /// <param name="commandLine">optional command line property settings</param>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiapplymultiplepatches.asp">MsiApplyMultiplePatches</a>
    /// </p></remarks>
    public static void ApplyMultiplePatches(
        IList<string> patchPackages, string productCode, string commandLine)
    {
        if (patchPackages == null || patchPackages.Count == 0)
        {
            throw new ArgumentNullException("patchPackages");
        }

        StringBuilder patchList = new StringBuilder();
        foreach (string patch in patchPackages)
        {
            if (patch != null)
            {
                if (patchList.Length != 0)
                {
                    patchList.Append(';');
                }

                patchList.Append(patch);
            }
        }

        if (patchList.Length == 0)
        {
            throw new ArgumentNullException("patchPackages");
        }

        uint ret = NativeMethods.MsiApplyMultiplePatches(patchList.ToString(), productCode, commandLine);
        Installer.CheckInstallResult(ret);
    }

    /// <summary>
    /// Extracts information from a patch that can be used to determine whether the patch
    /// applies on a target system. The method returns an XML string that can be provided to
    /// <see cref="DetermineApplicablePatches(string,string[],InapplicablePatchHandler,string,UserContexts)"/>
    /// instead of the full patch file.
    /// </summary>
    /// <param name="patchPath">Full path to the patch being queried.</param>
    /// <returns>XML string containing patch data.</returns>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiextractpatchxmldata.asp">MsiExtractPatchXMLData</a>
    /// </p></remarks>
    public static string ExtractPatchXmlData(string patchPath)
    {
        StringBuilder buf = new StringBuilder("");
        uint bufSize = 0;
        uint ret = NativeMethods.MsiExtractPatchXMLData(patchPath, 0, buf, ref bufSize);
        if (ret == (uint) NativeMethods.Error.MORE_DATA)
        {
            buf.Capacity = (int) ++bufSize;
            ret = NativeMethods.MsiExtractPatchXMLData(patchPath, 0, buf, ref bufSize);
        }

        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
        return buf.ToString();
    }

    /// <summary>
    /// [MSI 3.1] Migrates a user's application configuration data to a new SID.
    /// </summary>
    /// <param name="oldSid">Previous user SID that data is to be migrated from</param>
    /// <param name="newSid">New user SID that data is to be migrated to</param>
    /// <remarks><p>
    /// Win32 MSI API:
    /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msinotifysidchange.asp">MsiNotifySidChange</a>
    /// </p></remarks>
    public static void NotifySidChange(string oldSid, string newSid)
    {
        uint ret = NativeMethods.MsiNotifySidChange(oldSid, newSid);
        if (ret != 0)
        {
            throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    private static void CheckInstallResult(uint ret)
    {
        switch (ret)
        {
            case (uint) NativeMethods.Error.SUCCESS: break;
            case (uint) NativeMethods.Error.SUCCESS_REBOOT_REQUIRED: Installer.rebootRequired = true; break;
            case (uint) NativeMethods.Error.SUCCESS_REBOOT_INITIATED: Installer.rebootInitiated = true; break;
            default: throw InstallerException.ExceptionFromReturnCode(ret);
        }
    }

    private static int GetPatchStringDataType(string patchData)
    {
        if (patchData.IndexOf("<", StringComparison.Ordinal) >= 0 &&
            patchData.IndexOf(">", StringComparison.Ordinal) >= 0)
        {
            return 2; // XML blob
        }
        else if (String.Compare(Path.GetExtension(patchData), ".xml",
            StringComparison.OrdinalIgnoreCase) == 0)
        {
            return 1; // XML file path
        }
        else
        {
            return 0; // MSP file path
        }
    }
}
}
