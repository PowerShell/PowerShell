//---------------------------------------------------------------------
// <copyright file="ColumnEnums.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    // Enumerations are in alphabetical order.

    /// <summary>
    /// Available values for the Attributes column of the Component table.
    /// </summary>
    [Flags]
    public enum ComponentAttributes : int
    {
        /// <summary>
        /// Local only - Component cannot be run from source.
        /// </summary>
        /// <remarks><p>
        /// Set this value for all components belonging to a feature to prevent the feature from being run-from-network or
        /// run-from-source. Note that if a feature has no components, the feature always shows run-from-source and
        /// run-from-my-computer as valid options.
        /// </p></remarks>
        None              = 0x0000,

        /// <summary>
        /// Component can only be run from source.
        /// </summary>
        /// <remarks><p>
        /// Set this bit for all components belonging to a feature to prevent the feature from being run-from-my-computer.
        /// Note that if a feature has no components, the feature always shows run-from-source and run-from-my-computer
        /// as valid options.
        /// </p></remarks>
        SourceOnly        = 0x0001,

        /// <summary>
        /// Component can run locally or from source.
        /// </summary>
        Optional          = 0x0002,

        /// <summary>
        /// If this bit is set, the value in the KeyPath column is used as a key into the Registry table.
        /// </summary>
        /// <remarks><p>
        /// If the Value field of the corresponding record in the Registry table is null, the Name field in that record
        /// must not contain "+", "-", or "*". For more information, see the description of the Name field in Registry
        /// table.
        /// <p>Setting this bit is recommended for registry entries written to the HKCU hive. This ensures the installer
        /// writes the necessary HKCU registry entries when there are multiple users on the same machine.</p>
        /// </p></remarks>
        RegistryKeyPath   = 0x0004,

        /// <summary>
        /// If this bit is set, the installer increments the reference count in the shared DLL registry of the component's
        /// key file. If this bit is not set, the installer increments the reference count only if the reference count
        /// already exists.
        /// </summary>
        SharedDllRefCount = 0x0008,

        /// <summary>
        /// If this bit is set, the installer does not remove the component during an uninstall. The installer registers
        /// an extra system client for the component in the Windows Installer registry settings.
        /// </summary>
        Permanent         = 0x0010,

        /// <summary>
        /// If this bit is set, the value in the KeyPath column is a key into the ODBCDataSource table.
        /// </summary>
        OdbcDataSource    = 0x0020,

        /// <summary>
        /// If this bit is set, the installer reevaluates the value of the statement in the Condition column upon a reinstall.
        /// If the value was previously False and has changed to true, the installer installs the component. If the value
        /// was previously true and has changed to false, the installer removes the component even if the component has
        /// other products as clients.
        /// </summary>
        Transitive        = 0x0040,

        /// <summary>
        /// If this bit is set, the installer does not install or reinstall the component if a key path file or a key path
        /// registry entry for the component already exists. The application does register itself as a client of the component.
        /// </summary>
        /// <remarks><p>
        /// Use this flag only for components that are being registered by the Registry table. Do not use this flag for
        /// components registered by the AppId, Class, Extension, ProgId, MIME, and Verb tables.
        /// </p></remarks>
        NeverOverwrite    = 0x0080,

        /// <summary>
        /// Set this bit to mark this as a 64-bit component. This attribute facilitates the installation of packages that
        /// include both 32-bit and 64-bit components. If this bit is not set, the component is registered as a 32-bit component.
        /// </summary>
        /// <remarks><p>
        /// If this is a 64-bit component replacing a 32-bit component, set this bit and assign a new GUID in the
        /// ComponentId column.
        /// </p></remarks>
        SixtyFourBit      = 0x0100,

        /// <summary>
        /// Set this bit to disable registry reflection on all existing and new registry keys affected by this component.
        /// </summary>
        /// <remarks><p>
        /// If this bit is set, the Windows Installer calls the RegDisableReflectionKey on each key being accessed by the component.
        /// This bit is available with Windows Installer version 4.0 and is ignored on 32-bit systems.
        /// </p></remarks>
        DisableRegistryReflection = 0x0200,

        /// <summary>
        /// [MSI 4.5] Set this bit for a component in a patch package to prevent leaving orphan components on the computer.
        /// </summary>
        /// <remarks><p>
        /// If a subsequent patch is installed, marked with the SupersedeEarlier flag in its MsiPatchSequence
        /// table to supersede the first patch, Windows Installer 4.5 can unregister and uninstall components marked with the
        /// UninstallOnSupersedence value. If the component is not marked with this bit, installation of a superseding patch can leave
        /// behind an unused component on the computer.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Supersedence")]
        UninstallOnSupersedence = 0x0400,

        /// <summary>
        /// [MSI 4.5] If a component is marked with this attribute value in at least one package installed on the system,
        /// the installer treats the component as marked in all packages. If a package that shares the marked component
        /// is uninstalled, Windows Installer 4.5 can continue to share the highest version of the component on the system,
        /// even if that highest version was installed by the package that is being uninstalled.
        /// </summary>
        Shared = 0x0800,
    }

    /// <summary>
    /// Defines flags for the Attributes column of the Control table.
    /// </summary>
    [Flags]
    public enum ControlAttributes : int
    {
        /// <summary>If this bit is set, the control is visible on the dialog box.</summary>
        Visible           = 0x00000001,

        /// <summary>specifies if the given control is enabled or disabled. Most controls appear gray when disabled.</summary>
        Enabled           = 0x00000002,

        /// <summary>If this bit is set, the control is displayed with a sunken, three dimensional look.</summary>
        Sunken            = 0x00000004,

        /// <summary>The Indirect control attribute specifies whether the value displayed or changed by this control is referenced indirectly.</summary>
        Indirect          = 0x00000008,

        /// <summary>If this bit is set on a control, the associated property specified in the Property column of the Control table is an integer.</summary>
        Integer           = 0x00000010,

        /// <summary>If this bit is set the text in the control is displayed in a right-to-left reading order.</summary>
        RightToLeftReadingOrder = 0x00000020,

        /// <summary>If this style bit is set, text in the control is aligned to the right.</summary>
        RightAligned      = 0x00000040,

        /// <summary>If this bit is set, the scroll bar is located on the left side of the control, otherwise it is on the right.</summary>
        LeftScroll        = 0x00000080,

        /// <summary>This is a combination of the RightToLeftReadingOrder, RightAligned, and LeftScroll attributes.</summary>
        Bidirectional     = RightToLeftReadingOrder | RightAligned | LeftScroll,

        /// <summary>If this bit is set on a text control, the control is displayed transparently with the background showing through the control where there are no characters.</summary>
        Transparent       = 0x00010000,

        /// <summary>If this bit is set on a text control, the occurrence of the character "&amp;" in a text string is displayed as itself.</summary>
        NoPrefix          = 0x00020000,

        /// <summary>If this bit is set the text in the control is displayed on a single line.</summary>
        NoWrap            = 0x00040000,

        /// <summary>If this bit is set for a text control, the control will automatically attempt to format the displayed text as a number representing a count of bytes.</summary>
        FormatSize        = 0x00080000,

        /// <summary>If this bit is set, fonts are created using the user's default UI code page. Otherwise it is created using the database code page.</summary>
        UsersLanguage     = 0x00100000,

        /// <summary>If this bit is set on an Edit control, the installer creates a multiple line edit control with a vertical scroll bar.</summary>
        Multiline         = 0x00010000,

        /// <summary>This attribute creates an edit control for entering passwords. The control displays each character as an asterisk (*) as they are typed into the control.</summary>
        PasswordInput     = 0x00200000,

        /// <summary>If this bit is set on a ProgressBar control, the bar is drawn as a series of small rectangles in Microsoft Windows 95-style. Otherwise it is drawn as a single continuous rectangle.</summary>
        Progress95        = 0x00010000,

        /// <summary>If this bit is set, the control shows removable volumes.</summary>
        RemovableVolume   = 0x00010000,

        /// <summary>If this bit is set, the control shows fixed internal hard drives.</summary>
        FixedVolume       = 0x00020000,

        /// <summary>If this bit is set, the control shows remote volumes.</summary>
        RemoteVolume      = 0x00040000,

        /// <summary>If this bit is set, the control shows CD-ROM volumes.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cdrom")]
        CdromVolume       = 0x00080000,

        /// <summary>If this bit is set, the control shows RAM disk volumes.</summary>
        RamDiskVolume     = 0x00100000,

        /// <summary>If this bit is set, the control shows floppy volumes.</summary>
        FloppyVolume      = 0x00200000,

        /// <summary>Specifies whether or not the rollback backup files are included in the costs displayed by the VolumeCostList control.</summary>
        ShowRollbackCost  = 0x00400000,

        /// <summary>If this bit is set, the items listed in the control are displayed in a specified order. Otherwise, items are displayed in alphabetical order.</summary>
        Sorted            = 0x00010000,

        /// <summary>If this bit is set on a combo box, the edit field is replaced by a static text field. This prevents a user from entering a new value and requires the user to choose only one of the predefined values.</summary>
        ComboList         = 0x00020000,

        //ImageHandle       = 0x00010000,

        /// <summary>If this bit is set on a check box or a radio button group, the button is drawn with the appearance of a push button, but its logic stays the same.</summary>
        PushLike          = 0x00020000,

        /// <summary>If this bit is set, the text in the control is replaced by a bitmap image. The Text column in the Control table is a foreign key into the Binary table.</summary>
        Bitmap            = 0x00040000,

        /// <summary>If this bit is set, text is replaced by an icon image and the Text column in the Control table is a foreign key into the Binary table.</summary>
        Icon              = 0x00080000,

        /// <summary>If this bit is set, the picture is cropped or centered in the control without changing its shape or size.</summary>
        FixedSize         = 0x00100000,

        /// <summary>Specifies which size of the icon image to load. If none of the bits are set, the first image is loaded.</summary>
        IconSize16        = 0x00200000,

        /// <summary>Specifies which size of the icon image to load. If none of the bits are set, the first image is loaded.</summary>
        IconSize32        = 0x00400000,

        /// <summary>Specifies which size of the icon image to load. If none of the bits are set, the first image is loaded.</summary>
        IconSize48        = 0x00600000,

        /// <summary>If this bit is set, and the installation is not yet running with elevated privileges, the control is created with a UAC icon.</summary>
        ElevationShield   = 0x00800000,

        /// <summary>If this bit is set, the RadioButtonGroup has text and a border displayed around it.</summary>
        HasBorder         = 0x01000000,
    }

    /// <summary>
    /// Defines flags for the Type column of the CustomAction table.
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags")]
    [Flags]
    public enum CustomActionTypes : int
    {
        /// <summary>Unspecified custom action type.</summary>
        None               = 0x0000,

        /// <summary>Target = entry point name</summary>
        Dll                = 0x0001,

        /// <summary>Target = command line args</summary>
        Exe                = 0x0002,

        /// <summary>Target = text string to be formatted and set into property</summary>
        TextData           = 0x0003,

        /// <summary>Target = entry point name, null if none to call</summary>
        JScript            = 0x0005,

        /// <summary>Target = entry point name, null if none to call</summary>
        VBScript           = 0x0006,

        /// <summary>Target = property list for nested engine initialization</summary>
        Install            = 0x0007,

        /// <summary>Source = File.File, file part of installation</summary>
        SourceFile         = 0x0010,

        /// <summary>Source = Directory.Directory, folder containing existing file</summary>
        Directory          = 0x0020,

        /// <summary>Source = Property.Property, full path to executable</summary>
        Property           = 0x0030,

        /// <summary>Ignore action return status, continue running</summary>
        Continue           = 0x0040,

        /// <summary>Run asynchronously</summary>
        Async              = 0x0080,

        /// <summary>Skip if UI sequence already run</summary>
        FirstSequence      = 0x0100,

        /// <summary>Skip if UI sequence already run in same process</summary>
        OncePerProcess     = 0x0200,

        /// <summary>Run on client only if UI already run on client</summary>
        ClientRepeat       = 0x0300,

        /// <summary>Queue for execution within script</summary>
        InScript           = 0x0400,

        /// <summary>In conjunction with InScript: queue in Rollback script</summary>
        Rollback           = 0x0100,

        /// <summary>In conjunction with InScript: run Commit ops from script on success</summary>
        Commit             = 0x0200,

        /// <summary>No impersonation, run in system context</summary>
        NoImpersonate      = 0x0800,

        /// <summary>Impersonate for per-machine installs on TS machines</summary>
        TSAware            = 0x4000,

        /// <summary>Script requires 64bit process</summary>
        SixtyFourBitScript = 0x1000,

        /// <summary>Don't record the contents of the Target field in the log file</summary>
        HideTarget         = 0x2000,

        /// <summary>The custom action runs only when a patch is being uninstalled</summary>
        PatchUninstall     = 0x8000,
    }

    /// <summary>
    /// Defines flags for the Attributes column of the Dialog table.
    /// </summary>
    [Flags]
    public enum DialogAttributes : int
    {
        /// <summary>If this bit is set, the dialog is originally created as visible, otherwise it is hidden.</summary>
        Visible          = 0x00000001,

        /// <summary>If this bit is set, the dialog box is modal, other dialogs of the same application cannot be put on top of it, and the dialog keeps the control while it is running.</summary>
        Modal            = 0x00000002,

        /// <summary>If this bit is set, the dialog box can be minimized. This bit is ignored for modal dialog boxes, which cannot be minimized.</summary>
        Minimize         = 0x00000004,

        /// <summary>If this style bit is set, the dialog box will stop all other applications and no other applications can take the focus.</summary>
        SysModal         = 0x00000008,

        /// <summary>If this bit is set, the other dialogs stay alive when this dialog box is created.</summary>
        KeepModeless     = 0x00000010,

        /// <summary>If this bit is set, the dialog box periodically calls the installer. If the property changes, it notifies the controls on the dialog.</summary>
        TrackDiskSpace   = 0x00000020,

        /// <summary>If this bit is set, the pictures on the dialog box are created with the custom palette (one per dialog received from the first control created).</summary>
        UseCustomPalette = 0x00000040,

        /// <summary>If this style bit is set the text in the dialog box is displayed in right-to-left-reading order.</summary>
        RightToLeftReadingOrder  = 0x00000080,

        /// <summary>If this style bit is set, the text is aligned on the right side of the dialog box.</summary>
        RightAligned     = 0x00000100,

        /// <summary>If this style bit is set, the scroll bar is located on the left side of the dialog box.</summary>
        LeftScroll       = 0x00000200,

        /// <summary>This is a combination of the RightToLeftReadingOrder, RightAligned, and the LeftScroll dialog style bits.</summary>
        Bidirectional    = RightToLeftReadingOrder | RightAligned | LeftScroll,

        /// <summary>If this bit is set, the dialog box is an error dialog.</summary>
        Error            = 0x00010000,
    }

    /// <summary>
    /// Available values for the Attributes column of the Feature table.
    /// </summary>
    [Flags]
    public enum FeatureAttributes : int
    {
        /// <summary>
        /// Favor local - Components of this feature that are not marked for installation from source are installed locally.
        /// </summary>
        /// <remarks><p>
        /// A component shared by two or more features, some of which are set to FavorLocal and some to FavorSource,
        /// is installed locally. Components marked <see cref="ComponentAttributes.SourceOnly"/> in the Component
        /// table are always run from the source CD/server. The bits FavorLocal and FavorSource work with features not
        /// listed by the ADVERTISE property.
        /// </p></remarks>
        None                   = 0x0000,

        /// <summary>
        /// Components of this feature not marked for local installation are installed to run from the source
        /// CD-ROM or server.
        /// </summary>
        /// <remarks><p>
        /// A component shared by two or more features, some of which are set to FavorLocal and some to FavorSource,
        /// is installed to run locally. Components marked <see cref="ComponentAttributes.None"/> (local-only) in the
        /// Component table are always installed locally. The bits FavorLocal and FavorSource work with features
        /// not listed by the ADVERTISE property.
        /// </p></remarks>
        FavorSource            = 0x0001,

        /// <summary>
        /// Set this attribute and the state of the feature is the same as the state of the feature's parent.
        /// You cannot use this option if the feature is located at the root of a feature tree.
        /// </summary>
        /// <remarks><p>
        /// Omit this attribute and the feature state is determined according to DisallowAdvertise and
        /// FavorLocal and FavorSource.
        /// <p>To guarantee that the child feature's state always follows the state of its parent, even when the
        /// child and parent are initially set to absent in the SelectionTree control, you must include both
        /// FollowParent and UIDisallowAbsent in the attributes of the child feature.</p>
        /// <p>Note that if you set FollowParent without setting UIDisallowAbsent, the installer cannot force
        /// the child feature out of the absent state. In this case, the child feature matches the parent's
        /// installation state only if the child is set to something other than absent.</p>
        /// <p>Set FollowParent and UIDisallowAbsent to ensure a child feature follows the state of the parent feature.</p>
        /// </p></remarks>
        FollowParent           = 0x0002,

        /// <summary>
        /// Set this attribute and the feature state is Advertise.
        /// </summary>
        /// <remarks><p>
        /// If the feature is listed by the ADDDEFAULT property this bit is ignored and the feature state is determined
        /// according to FavorLocal and FavorSource.
        /// <p>Omit this attribute and the feature state is determined according to DisallowAdvertise and FavorLocal
        /// and FavorSource.</p>
        /// </p></remarks>
        FavorAdvertise         = 0x0004,

        /// <summary>
        /// Set this attribute to prevent the feature from being advertised.
        /// </summary>
        /// <remarks><p>
        /// Note that this bit works only with features that are listed by the ADVERTISE property.
        /// <p>Set this attribute and if the listed feature is not a parent or child, the feature is installed according to
        /// FavorLocal and FavorSource.</p>
        /// <p>Set this attribute for the parent of a listed feature and the parent is installed.</p>
        /// <p>Set this attribute for the child of a listed feature and the state of the child is Absent.</p>
        /// <p>Omit this attribute and if the listed feature is not a parent or child, the feature state is Advertise.</p>
        /// <p>Omit this attribute and if the listed feature is a parent or child, the state of both features is Advertise.</p>
        /// </p></remarks>
        DisallowAdvertise      = 0x0008,

        /// <summary>
        /// Set this attribute and the user interface does not display an option to change the feature state
        /// to Absent. Setting this attribute forces the feature to the installation state, whether or not the
        /// feature is visible in the UI.
        /// </summary>
        /// <remarks><p>
        /// Omit this attribute and the user interface displays an option to change the feature state to Absent.
        /// <p>Set FollowParent and UIDisallowAbsent to ensure a child feature follows the state of the parent feature.</p>
        /// <p>Setting this attribute not only affects the UI, but also forces the feature to the install state whether
        /// the feature is visible in the UI or not.</p>
        /// </p></remarks>
        UIDisallowAbsent       = 0x0010,

        /// <summary>
        /// Set this attribute and advertising is disabled for the feature if the operating system shell does not
        /// support Windows Installer descriptors.
        /// </summary>
        NoUnsupportedAdvertise = 0x0020,
    }

    /// <summary>
    /// Available values for the Attributes column of the File table.
    /// </summary>
    [Flags]
    public enum FileAttributes : int
    {
        /// <summary>No attributes.</summary>
        None          = 0x0000,

        /// <summary>Read-only.</summary>
        ReadOnly      = 0x0001,

        /// <summary>Hidden.</summary>
        Hidden        = 0x0002,

        /// <summary>System.</summary>
        System        = 0x0004,

        /// <summary>The file is vital for the proper operation of the component to which it belongs.</summary>
        Vital         = 0x0200,

        /// <summary>The file contains a valid checksum. A checksum is required to repair a file that has become corrupted.</summary>
        Checksum      = 0x0400,

        /// <summary>This bit must only be added by a patch and if the file is being added by the patch.</summary>
        PatchAdded    = 0x1000,

        /// <summary>
        /// The file's source type is uncompressed. If set, ignore the WordCount summary information property. If neither
        /// Noncompressed nor Compressed are set, the compression state of the file is specified by the WordCount summary
        /// information property. Do not set both Noncompressed and Compressed.
        /// </summary>
        NonCompressed = 0x2000,

        /// <summary>
        /// The file's source type is compressed. If set, ignore the WordCount summary information property. If neither
        /// Noncompressed or Compressed are set, the compression state of the file is specified by the WordCount summary
        /// information property. Do not set both Noncompressed and Compressed.
        /// </summary>
        Compressed    = 0x4000,
    }

    /// <summary>
    /// Defines values for the Action column of the IniFile and RemoveIniFile tables.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ini")]
    public enum IniFileAction : int
    {
        /// <summary>Creates or updates a .ini entry.</summary>
        AddLine    = 0,

        /// <summary>Creates a .ini entry only if the entry does not already exist.</summary>
        CreateLine = 1,

        /// <summary>Deletes .ini entry.</summary>
        RemoveLine = 2,

        /// <summary>Creates a new entry or appends a new comma-separated value to an existing entry.</summary>
        AddTag     = 3,

        /// <summary>Deletes a tag from a .ini entry.</summary>
        RemoveTag  = 4,
    }

    /// <summary>
    /// Defines values for the Type column of the CompLocator, IniLocator, and RegLocator tables.
    /// </summary>
    [Flags]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum LocatorTypes : int
    {
        /// <summary>Key path is a directory.</summary>
        Directory    = 0x00000000,

        /// <summary>Key path is a file name.</summary>
        FileName     = 0x00000001,

        /// <summary>Key path is a registry value.</summary>
        RawValue     = 0x00000002,

        /// <summary>Set this bit to have the installer search the 64-bit portion of the registry.</summary>
        SixtyFourBit = 0x00000010,
    }

    /// <summary>
    /// Defines values for the Root column of the Registry, RemoveRegistry, and RegLocator tables.
    /// </summary>
    public enum RegistryRoot : int
    {
        /// <summary>HKEY_CURRENT_USER for a per-user installation,
        /// or HKEY_LOCAL_MACHINE for a per-machine installation.</summary>
        UserOrMachine = -1,

        /// <summary>HKEY_CLASSES_ROOT</summary>
        ClassesRoot   = 0,

        /// <summary>HKEY_CURRENT_USER</summary>
        CurrentUser   = 1,

        /// <summary>HKEY_LOCAL_MACHINE</summary>
        LocalMachine  = 2,

        /// <summary>HKEY_USERS</summary>
        Users         = 3,
    }

    /// <summary>
    /// Defines values for the InstallMode column of the RemoveFile table.
    /// </summary>
    [Flags]
    public enum RemoveFileModes : int
    {
        /// <summary>Never remove.</summary>
        None      = 0,

        /// <summary>Remove when the associated component is being installed (install state = local or source).</summary>
        OnInstall = 1,

        /// <summary>Remove when the associated component is being removed (install state = absent).</summary>
        OnRemove  = 2,
    }

    /// <summary>
    /// Defines values for the ServiceType, StartType, and ErrorControl columns of the ServiceInstall table.
    /// </summary>
    [Flags]
    public enum ServiceAttributes : int
    {
        /// <summary>No flags.</summary>
        None = 0,

        /// <summary>A Win32 service that runs its own process.</summary>
        OwnProcess        = 0x0010,

        /// <summary>A Win32 service that shares a process.</summary>
        ShareProcess      = 0x0020,

        /// <summary>A Win32 service that interacts with the desktop.
        /// This value cannot be used alone and must be added to either
        /// <see cref="OwnProcess"/> or <see cref="ShareProcess"/>.</summary>
        Interactive       = 0x0100,

        /// <summary>Service starts during startup of the system.</summary>
        AutoStart         = 0x0002,

        /// <summary>Service starts when the service control manager calls the StartService function.</summary>
        DemandStart       = 0x0003,

        /// <summary>Specifies a service that can no longer be started.</summary>
        Disabled          = 0x0004,

        /// <summary>Logs the error, displays a message box and continues the startup operation.</summary>
        ErrorMessage      = 0x0001,

        /// <summary>Logs the error if it is possible and the system is restarted with the last configuration
        /// known to be good. If the last-known-good configuration is being started, the startup operation fails.</summary>
        ErrorCritical     = 0x0003,

        /// <summary>When combined with other error flags, specifies that the overall install should fail if
        /// the service cannot be installed into the system.</summary>
        ErrorControlVital = 0x8000,
    }

    /// <summary>
    /// Defines values for the Event column of the ServiceControl table.
    /// </summary>
    [Flags]
    public enum ServiceControlEvents : int
    {
        /// <summary>No control events.</summary>
        None            = 0x0000,

        /// <summary>During an install, starts the service during the StartServices action.</summary>
        Start           = 0x0001,

        /// <summary>During an install, stops the service during the StopServices action.</summary>
        Stop            = 0x0002,

        /// <summary>During an install, deletes the service during the DeleteServices action.</summary>
        Delete          = 0x0008,

        /// <summary>During an uninstall, starts the service during the StartServices action.</summary>
        UninstallStart  = 0x0010,

        /// <summary>During an uninstall, stops the service during the StopServices action.</summary>
        UninstallStop   = 0x0020,

        /// <summary>During an uninstall, deletes the service during the DeleteServices action.</summary>
        UninstallDelete = 0x0080,
    }

    /// <summary>
    /// Defines values for the StyleBits column of the TextStyle table.
    /// </summary>
    [Flags]
    public enum TextStyles : int
    {
        /// <summary>Bold</summary>
        Bold      = 0x0001,

        /// <summary>Italic</summary>
        Italic    = 0x0002,

        /// <summary>Underline</summary>
        Underline = 0x0004,

        /// <summary>Strike out</summary>
        Strike    = 0x0008,
    }

    /// <summary>
    /// Defines values for the Attributes column of the Upgrade table.
    /// </summary>
    [Flags]
    public enum UpgradeAttributes : int
    {
        /// <summary>Migrates feature states by enabling the logic in the MigrateFeatureStates action.</summary>
        MigrateFeatures     = 0x0001,

        /// <summary>Detects products and applications but does not remove.</summary>
        OnlyDetect          = 0x0002,

        /// <summary>Continues installation upon failure to remove a product or application.</summary>
        IgnoreRemoveFailure = 0x0004,

        /// <summary>Detects the range of versions including the value in VersionMin.</summary>
        VersionMinInclusive = 0x0100,

        /// <summary>Detects the range of versions including the value in VersionMax.</summary>
        VersionMaxInclusive = 0x0200,

        /// <summary>Detects all languages, excluding the languages listed in the Language column.</summary>
        LanguagesExclusive  = 0x0400,
    }
}
