//---------------------------------------------------------------------
// <copyright file="Enums.cs" company="Microsoft Corporation">
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
    /// Specifies a return status value for custom actions.
    /// </summary>
    public enum ActionResult : int
    {
        /// <summary>Action completed successfully.</summary>
        Success = 0,

        /// <summary>Skip remaining actions, not an error.</summary>
        SkipRemainingActions = 259,

        /// <summary>User terminated prematurely.</summary>
        UserExit = 1602,

        /// <summary>Unrecoverable error or unhandled exception occurred.</summary>
        Failure = 1603,

        /// <summary>Action not executed.</summary>
        NotExecuted = 1626,
    }

    /// <summary>
    /// Specifies the open mode for a <see cref="Database"/>.
    /// </summary>
    public enum DatabaseOpenMode : int
    {
        /// <summary>Open a database read-only, no persistent changes.</summary>
        ReadOnly = 0,

        /// <summary>Open a database read/write in transaction mode.</summary>
        Transact = 1,

        /// <summary>Open a database direct read/write without transaction.</summary>
        Direct = 2,

        /// <summary>Create a new database, transact mode read/write.</summary>
        Create = 3,

        /// <summary>Create a new database, direct mode read/write.</summary>
        CreateDirect = 4,
    }

    /// <summary>
    /// Log modes available for <see cref="Installer.EnableLog(InstallLogModes,string)"/>
    /// and <see cref="Installer.SetExternalUI(ExternalUIHandler,InstallLogModes)"/>.
    /// </summary>
    [Flags]
    public enum InstallLogModes : int
    {
        /// <summary>Disable logging.</summary>
        None           = 0,

        /// <summary>Log out of memory or fatal exit information.</summary>
        FatalExit      = (1 << ((int) InstallMessage.FatalExit      >> 24)),

        /// <summary>Log error messages.</summary>
        Error          = (1 << ((int) InstallMessage.Error          >> 24)),

        /// <summary>Log warning messages.</summary>
        Warning        = (1 << ((int) InstallMessage.Warning        >> 24)),

        /// <summary>Log user requests.</summary>
        User           = (1 << ((int) InstallMessage.User           >> 24)),

        /// <summary>Log status messages that are not displayed.</summary>
        Info           = (1 << ((int) InstallMessage.Info           >> 24)),

        /// <summary>Log request to determine a valid source location.</summary>
        ResolveSource  = (1 << ((int) InstallMessage.ResolveSource  >> 24)),

        /// <summary>Log insufficient disk space error.</summary>
        OutOfDiskSpace = (1 << ((int) InstallMessage.OutOfDiskSpace >> 24)),

        /// <summary>Log the start of installation actions.</summary>
        ActionStart    = (1 << ((int) InstallMessage.ActionStart    >> 24)),

        /// <summary>Log the data record for installation actions.</summary>
        ActionData     = (1 << ((int) InstallMessage.ActionData     >> 24)),

        /// <summary>Log parameters for user-interface initialization.</summary>
        CommonData     = (1 << ((int) InstallMessage.CommonData     >> 24)),

        /// <summary>Log the property values at termination.</summary>
        PropertyDump   = (1 << ((int) InstallMessage.Progress       >> 24)), // log only

        /// <summary>
        /// Sends large amounts of information to log file not generally useful to users.
        /// May be used for support.
        /// </summary>
        Verbose        = (1 << ((int) InstallMessage.Initialize     >> 24)), // log only

        /// <summary>
        /// Log extra debugging information.
        /// </summary>
        ExtraDebug     = (1 << ((int) InstallMessage.Terminate      >> 24)), // log only

        /// <summary>
        /// Log only on error.
        /// </summary>
        LogOnlyOnError = (1 << ((int) InstallMessage.ShowDialog     >> 24)), // log only

        /// <summary>
        /// Log progress bar information. This message includes information on units so far and total number
        /// of units. See <see cref="Session.Message"/> for an explanation of the message format. This message
        /// is only sent to an external user interface and is not logged.
        /// </summary>
        Progress       = (1 << ((int) InstallMessage.Progress       >> 24)), // external handler only

        /// <summary>
        /// If this is not a quiet installation, then the basic UI has been initialized. If this is a full
        /// UI installation, the Full UI is not yet initialized. This message is only sent to an external
        /// user interface and is not logged.
        /// </summary>
        Initialize     = (1 << ((int) InstallMessage.Initialize     >> 24)), // external handler only

        /// <summary>
        /// If a full UI is being used, the full UI has ended. If this is not a quiet installation, the basic
        /// UI has not yet ended. This message is only sent to an external user interface and is not logged.
        /// </summary>
        Terminate      = (1 << ((int) InstallMessage.Terminate      >> 24)), // external handler only

        /// <summary>
        /// Sent prior to display of the Full UI dialog. This message is only sent to an external user
        /// interface and is not logged.
        /// </summary>
        ShowDialog     = (1 << ((int) InstallMessage.ShowDialog     >> 24)), // external handler only

        /// <summary>
        /// List of files in use that need to be replaced.
        /// </summary>
        FilesInUse     = (1 << ((int) InstallMessage.FilesInUse     >> 24)), // external handler only

        /// <summary>
        /// [MSI 4.0] List of apps that the user can request Restart Manager to shut down and restart.
        /// </summary>
        RMFilesInUse   = (1 << ((int) InstallMessage.RMFilesInUse   >> 24)), // external handler only
    }

    /// <summary>
    /// Type of message to be processed by <see cref="Session.Message"/>,
    /// <see cref="ExternalUIHandler"/>, or <see cref="ExternalUIRecordHandler"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum InstallMessage : int
    {
        /// <summary>Premature termination, possibly fatal OOM.</summary>
        FatalExit      = 0x00000000,

        /// <summary>Formatted error message.</summary>
        Error          = 0x01000000,

        /// <summary>Formatted warning message.</summary>
        Warning        = 0x02000000,

        /// <summary>User request message.</summary>
        User           = 0x03000000,

        /// <summary>Informative message for log.</summary>
        Info           = 0x04000000,

        /// <summary>List of files in use that need to be replaced.</summary>
        FilesInUse     = 0x05000000,

        /// <summary>Request to determine a valid source location.</summary>
        ResolveSource  = 0x06000000,

        /// <summary>Insufficient disk space message.</summary>
        OutOfDiskSpace = 0x07000000,

        /// <summary>Start of action: action name &amp; description.</summary>
        ActionStart    = 0x08000000,

        /// <summary>Formatted data associated with individual action item.</summary>
        ActionData     = 0x09000000,

        /// <summary>Progress gauge info: units so far, total.</summary>
        Progress       = 0x0A000000,

        /// <summary>Product info for dialog: language Id, dialog caption.</summary>
        CommonData     = 0x0B000000,

        /// <summary>Sent prior to UI initialization, no string data.</summary>
        Initialize     = 0x0C000000,

        /// <summary>Sent after UI termination, no string data.</summary>
        Terminate      = 0x0D000000,

        /// <summary>Sent prior to display or authored dialog or wizard.</summary>
        ShowDialog     = 0x0E000000,

        /// <summary>[MSI 4.0] List of apps that the user can request Restart Manager to shut down and restart.</summary>
        RMFilesInUse   = 0x19000000,

        /// <summary>[MSI 4.5] Sent prior to install of a product.</summary>
        InstallStart   = 0x1A000000,

        /// <summary>[MSI 4.5] Sent after install of a product.</summary>
        InstallEnd     = 0x1B000000,
    }

    /// <summary>
    /// Specifies the install mode for <see cref="Installer.ProvideComponent"/> or <see cref="Installer.ProvideQualifiedComponent"/>.
    /// </summary>
    public enum InstallMode : int
    {
        /// <summary>Provide the component only if the feature's installation state is <see cref="InstallState.Local"/>.</summary>
        NoSourceResolution = -3,

        /// <summary>Only check that the component is registered, without verifying that the key file of the component exists.</summary>
        NoDetection        = -2,

        /// <summary>Provide the component only if the feature exists.</summary>
        Existing           = -1,

        /// <summary>Provide the component and perform any installation necessary to provide the component.</summary>
        Default            = 0,
    }

    /// <summary>
    /// Specifies the run mode for <see cref="Session.GetMode"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum InstallRunMode : int
    {
        /// <summary>The administrative mode is installing, or the product is installing.</summary>
        Admin           = 0,

        /// <summary>The advertisements are installing or the product is installing or updating.</summary>
        Advertise       = 1,

        /// <summary>An existing installation is being modified or there is a new installation.</summary>
        Maintenance     = 2,

        /// <summary>Rollback is enabled.</summary>
        RollbackEnabled = 3,

        /// <summary>The log file is active. It was enabled prior to the installation session.</summary>
        LogEnabled      = 4,

        /// <summary>Execute operations are spooling or they are in the determination phase.</summary>
        Operations      = 5,

        /// <summary>A reboot is necessary after a successful installation (settable).</summary>
        RebootAtEnd     = 6,

        /// <summary>A reboot is necessary to continue the installation (settable).</summary>
        RebootNow       = 7,

        /// <summary>Files from cabinets and Media table files are installing.</summary>
        Cabinet         = 8,

        /// <summary>The source LongFileNames is suppressed through the PID_MSISOURCE summary property.</summary>
        SourceShortNames = 9,

        /// <summary>The target LongFileNames is suppressed through the SHORTFILENAMES property.</summary>
        TargetShortNames = 10,

        // <summary>Reserved for future use.</summary>
        //Reserved11      = 11,

        /// <summary>The operating system is Windows 95, Windows 98, or Windows ME.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "x")]
        Windows9x       = 12,

        /// <summary>The operating system supports demand installation.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Zaw")]
        ZawEnabled      = 13,

        // <summary>Reserved for future use.</summary>
        //Reserved14      = 14,

        // <summary>Reserved for future use.</summary>
        //Reserved15      = 15,

        /// <summary>A custom action called from install script execution.</summary>
        Scheduled       = 16,

        /// <summary>A custom action called from rollback execution script.</summary>
        Rollback        = 17,

        /// <summary>A custom action called from commit execution script.</summary>
        Commit          = 18,
    }

    /// <summary>
    /// Installed state of a Component or Feature.
    /// </summary>
    public enum InstallState : int
    {
        /// <summary>The component is disabled.</summary>
        NotUsed        = -7,

        /// <summary>The installation configuration data is corrupt.</summary>
        BadConfig      = -6,

        /// <summary>The installation is suspended or in progress.</summary>
        Incomplete     = -5,

        /// <summary>Component is set to run from source, but source is unavailable.</summary>
        SourceAbsent   = -4,

        /// <summary>The buffer overflow is returned.</summary>
        MoreData       = -3,

        /// <summary>An invalid parameter was passed to the function.</summary>
        InvalidArgument = -2,

        /// <summary>An unrecognized product or feature name was passed to the function.</summary>
        Unknown        = -1,

        /// <summary>The component is broken.</summary>
        Broken         = 0,

        /// <summary>The feature is advertised.</summary>
        Advertised     = 1,

        /// <summary>The component is being removed. In action state and not settable.</summary>
        Removed        = 1,

        /// <summary>The component is not installed, or action state is absent but clients remain.</summary>
        Absent         = 2,

        /// <summary>The component is installed on the local drive.</summary>
        Local          = 3,

        /// <summary>The component will run from the source, CD, or network.</summary>
        Source         = 4,

        /// <summary>The component will be installed in the default location: local or source.</summary>
        Default        = 5,
    }

    /// <summary>
    /// Specifies the type of installation for <see cref="Installer.ApplyPatch(string,string,InstallType,string)"/>.
    /// </summary>
    public enum InstallType : int
    {
        /// <summary>Searches system for products to patch.</summary>
        Default = 0,

        /// <summary>Indicates a administrative installation.</summary>
        NetworkImage = 1,

        /// <summary>Indicates a particular instance.</summary>
        SingleInstance = 2,
    }

    /// <summary>
    /// Level of the installation user interface, specified with
    /// <see cref="Installer.SetInternalUI(InstallUIOptions)"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    [Flags]
    public enum InstallUIOptions : int
    {
        /// <summary>Does not change UI level.</summary>
        NoChange     = 0,

        /// <summary>Uses Default UI level.</summary>
        Default      = 1,

        /// <summary>Silent installation.</summary>
        Silent       = 2,

        /// <summary>Simple progress and error handling.</summary>
        Basic        = 3,

        /// <summary>Authored UI, wizard dialogs suppressed.</summary>
        Reduced      = 4,

        /// <summary>Authored UI with wizards, progress, and errors.</summary>
        Full         = 5,

        /// <summary>
        /// When combined with the <see cref="Basic"/> value, the installer does not display
        /// the cancel button in the progress dialog.
        /// </summary>
        HideCancel   = 0x20,

        /// <summary>
        /// When combined with the <see cref="Basic"/> value, the installer displays progress
        /// dialog boxes but does not display any modal dialog boxes or error dialog boxes.
        /// </summary>
        ProgressOnly = 0x40,

        /// <summary>
        /// When combined with another value, the installer displays a modal dialog
        /// box at the end of a successful installation or if there has been an error.
        /// No dialog box is displayed if the user cancels.
        /// </summary>
        EndDialog    = 0x80,

        /// <summary>
        /// Forces display of the source resolution dialog even if the UI is otherwise silent.
        /// </summary>
        SourceResolutionOnly = 0x100,

        /// <summary>
        /// [MSI 5.0] Forces display of the UAC dialog even if the UI is otherwise silent.
        /// </summary>
        UacOnly = 0x200,
    }

    /// <summary>
    /// Specifies a return status value for message handlers.  These values are returned by
    /// <see cref="Session.Message"/>, <see cref="ExternalUIHandler"/>, and <see cref="IEmbeddedUI.ProcessMessage"/>.
    /// </summary>
    public enum MessageResult : int
    {
        /// <summary>An error was found in the message handler.</summary>
        Error  = -1,

        /// <summary>No action was taken.</summary>
        None   = 0,

        /// <summary>IDOK</summary>
        [SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
        OK     = 1,

        /// <summary>IDCANCEL</summary>
        Cancel = 2,

        /// <summary>IDABORT</summary>
        Abort  = 3,

        /// <summary>IDRETRY</summary>
        Retry  = 4,

        /// <summary>IDIGNORE</summary>
        Ignore = 5,

        /// <summary>IDYES</summary>
        Yes    = 6,

        /// <summary>IDNO</summary>
        No     = 7,
    }

    /// <summary>
    /// Specifies constants defining which buttons to display for a message. This can be cast to
    /// the MessageBoxButtons enum in System.Windows.Forms and System.Windows.
    /// </summary>
    public enum MessageButtons
    {
        /// <summary>
        /// The message contains an OK button.
        /// </summary>
        OK = 0,

        /// <summary>
        /// The message contains OK and Cancel buttons.
        /// </summary>
        OKCancel = 1,

        /// <summary>
        /// The message contains Abort, Retry, and Ignore buttons.
        /// </summary>
        AbortRetryIgnore = 2,

        /// <summary>
        /// The message contains Yes, No, and Cancel buttons.
        /// </summary>
        YesNoCancel = 3,

        /// <summary>
        /// The message contains Yes and No buttons.
        /// </summary>
        YesNo = 4,

        /// <summary>
        /// The message contains Retry and Cancel buttons.
        /// </summary>
        RetryCancel = 5,
    }

    /// <summary>
    /// Specifies constants defining which information to display. This can be cast to
    /// the MessageBoxIcon enum in System.Windows.Forms and System.Windows.
    /// </summary>
    public enum MessageIcon
    {
        /// <summary>
        /// The message contain no symbols.
        /// </summary>
        None = 0,

        /// <summary>
        /// The message contains a symbol consisting of white X in a circle with a red background.
        /// </summary>
        Error = 16,

        /// <summary>
        /// The message contains a symbol consisting of a white X in a circle with a red background.
        /// </summary>
        Hand = 16,

        /// <summary>
        /// The message contains a symbol consisting of white X in a circle with a red background.
        /// </summary>
        Stop = 16,

        /// <summary>
        /// The message contains a symbol consisting of a question mark in a circle.
        /// </summary>
        Question = 32,

        /// <summary>
        /// The message contains a symbol consisting of an exclamation point in a triangle with a yellow background.
        /// </summary>
        Exclamation = 48,

        /// <summary>
        /// The message contains a symbol consisting of an exclamation point in a triangle with a yellow background.
        /// </summary>
        Warning = 48,

        /// <summary>
        /// The message contains a symbol consisting of a lowercase letter i in a circle.
        /// </summary>
        Information = 64,

        /// <summary>
        /// The message contains a symbol consisting of a lowercase letter i in a circle.
        /// </summary>
        Asterisk = 64,
    }

    /// <summary>
    /// Specifies constants defining the default button on a message. This can be cast to
    /// the MessageBoxDefaultButton enum in System.Windows.Forms and System.Windows.
    /// </summary>
    public enum MessageDefaultButton
    {
        /// <summary>
        /// The first button on the message is the default button.
        /// </summary>
        Button1 = 0,

        /// <summary>
        /// The second button on the message is the default button.
        /// </summary>
        Button2 = 256,

        /// <summary>
        /// The third button on the message is the default button.
        /// </summary>
        Button3 = 512,
    }

    /// <summary>
    /// Additional styles for use with message boxes.
    /// </summary>
    [Flags]
    internal enum MessageBoxStyles
    {
        /// <summary>
        /// The message box is created with the WS_EX_TOPMOST window style.
        /// </summary>
        TopMost = 0x00040000,

        /// <summary>
        /// The caller is a service notifying the user of an event.
        /// The function displays a message box on the current active desktop, even if there is no user logged on to the computer.
        /// </summary>
        ServiceNotification = 0x00200000,
    }

    /// <summary>
    /// Specifies the different patch states for <see cref="PatchInstallation.GetPatches(string, string, string, UserContexts, PatchStates)"/>.
    /// </summary>
    [Flags]
    public enum PatchStates : int
    {
        /// <summary>Invalid value.</summary>
        None = 0,

        /// <summary>Patches applied to a product.</summary>
        Applied = 1,

        /// <summary>Patches that are superseded by other patches.</summary>
        Superseded = 2,

        /// <summary>Patches that are obsolesced by other patches.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Obsoleted")]
        Obsoleted = 4,

        /// <summary>Patches that are registered to a product but not applied.</summary>
        Registered = 8,

        /// <summary>All valid patch states.</summary>
        All = (Applied | Superseded | Obsoleted | Registered)
    }

    /// <summary>
    /// Specifies the reinstall mode for <see cref="Installer.ReinstallFeature"/> or <see cref="Installer.ReinstallProduct"/>.
    /// </summary>
    [Flags]
    public enum ReinstallModes : int
    {
        /// <summary>Reinstall only if file is missing.</summary>
        FileMissing      = 0x00000002,

        /// <summary>Reinstall if file is missing, or older version.</summary>
        FileOlderVersion = 0x00000004,

        /// <summary>Reinstall if file is missing, or equal or older version.</summary>
        FileEqualVersion = 0x00000008,

        /// <summary>Reinstall if file is missing, or not exact version.</summary>
        FileExact        = 0x00000010,

        /// <summary>Checksum executables, reinstall if missing or corrupt.</summary>
        FileVerify       = 0x00000020,

        /// <summary>Reinstall all files, regardless of version.</summary>
        FileReplace      = 0x00000040,

        /// <summary>Insure required machine reg entries.</summary>
        MachineData      = 0x00000080,

        /// <summary>Insure required user reg entries.</summary>
        UserData         = 0x00000100,

        /// <summary>Validate shortcuts items.</summary>
        Shortcut         = 0x00000200,

        /// <summary>Use re-cache source install package.</summary>
        Package          = 0x00000400,
    }

    /// <summary>
    /// Attributes for <see cref="Transaction"/> methods.
    /// </summary>
    [Flags]
    public enum TransactionAttributes : int
    {
        /// <summary>No attributes.</summary>
        None                   = 0x00000000,

        /// <summary>Request that the Windows Installer not shutdown the embedded UI until the transaction is complete.</summary>
        ChainEmbeddedUI        = 0x00000001,

        /// <summary>Request that the Windows Installer transfer the embedded UI from the original installation.</summary>
        JoinExistingEmbeddedUI = 0x00000002,
    }

    /// <summary>
    /// Transform error conditions available for <see cref="Database.CreateTransformSummaryInfo"/> or
    /// <see cref="Database.ApplyTransform(string,TransformErrors)"/>.
    /// </summary>
    [Flags]
    public enum TransformErrors : int
    {
        /// <summary>No error conditions.</summary>
        None             = 0x0000,

        /// <summary>Adding a row that already exists.</summary>
        AddExistingRow   = 0x0001,

        /// <summary>Deleting a row that doesn't exist.</summary>
        DelMissingRow    = 0x0002,

        /// <summary>Adding a table that already exists.</summary>
        AddExistingTable = 0x0004,

        /// <summary>Deleting a table that doesn't exist.</summary>
        DelMissingTable  = 0x0008,

        /// <summary>Updating a row that doesn't exist.</summary>
        UpdateMissingRow = 0x0010,

        /// <summary>Transform and database code pages do not match and neither code page is neutral.</summary>
        ChangeCodePage   = 0x0020,

        /// <summary>Create the temporary _TransformView table when applying the transform.</summary>
        ViewTransform    = 0x0100,
    }

    /// <summary>
    /// Transform validation flags available for <see cref="Database.CreateTransformSummaryInfo"/>.
    /// </summary>
    [Flags]
    public enum TransformValidations : int
    {
        /// <summary>Validate no properties.</summary>
        None                       = 0x0000,

        /// <summary>Default language must match base database.</summary>
        Language                   = 0x0001,

        /// <summary>Product must match base database.</summary>
        Product                    = 0x0002,

        /// <summary>Check major version only.</summary>
        MajorVersion               = 0x0008,

        /// <summary>Check major and minor versions only.</summary>
        MinorVersion               = 0x0010,

        /// <summary>Check major, minor, and update versions.</summary>
        UpdateVersion              = 0x0020,

        /// <summary>Installed version &lt; base version.</summary>
        NewLessBaseVersion         = 0x0040,

        /// <summary>Installed version &lt;= base version.</summary>
        NewLessEqualBaseVersion    = 0x0080,

        /// <summary>Installed version = base version.</summary>
        NewEqualBaseVersion        = 0x0100,

        /// <summary>Installed version &gt;= base version.</summary>
        NewGreaterEqualBaseVersion = 0x0200,

        /// <summary>Installed version &gt; base version.</summary>
        NewGreaterBaseVersion      = 0x0400,

        /// <summary>UpgradeCode must match base database.</summary>
        UpgradeCode                = 0x0800,
    }

    /// <summary>
    /// Specifies the installation context for <see cref="ProductInstallation"/>s,
    /// <see cref="PatchInstallation"/>es, and
    /// <see cref="Installer.DetermineApplicablePatches(string,string[],InapplicablePatchHandler,string,UserContexts)"/>
    /// </summary>
    [Flags]
    public enum UserContexts : int
    {
        /// <summary>Not installed.</summary>
        None           = 0,

        /// <summary>User managed install context.</summary>
        UserManaged    = 1,

        /// <summary>User non-managed context.</summary>
        UserUnmanaged  = 2,

        /// <summary>Per-machine context.</summary>
        Machine        = 4,

        /// <summary>All contexts, or all valid values.</summary>
        All            = (UserManaged | UserUnmanaged | Machine),

        /// <summary>All user-managed contexts.</summary>
        AllUserManaged = 8,
    }

    /// <summary>
    /// Defines the type of error encountered by the <see cref="View.Validate"/>, <see cref="View.ValidateNew"/>,
    /// or <see cref="View.ValidateFields"/> methods of the <see cref="View"/> class.
    /// </summary>
    public enum ValidationError : int
    {
        /*
        InvalidArg        = -3,
        MoreData          = -2,
        FunctionError     = -1,
        */

        /// <summary>No error.</summary>
        None              = 0,

        /// <summary>The new record duplicates primary keys of the existing record in a table.</summary>
        DuplicateKey      = 1,

        /// <summary>There are no null values allowed, or the column is about to be deleted but is referenced by another row.</summary>
        Required          = 2,

        /// <summary>The corresponding record in a foreign table was not found.</summary>
        BadLink           = 3,

        /// <summary>The data is greater than the maximum value allowed.</summary>
        Overflow          = 4,

        /// <summary>The data is less than the minimum value allowed.</summary>
        Underflow         = 5,

        /// <summary>The data is not a member of the values permitted in the set.</summary>
        NotInSet          = 6,

        /// <summary>An invalid version string was supplied.</summary>
        BadVersion        = 7,

        /// <summary>The case was invalid. The case must be all uppercase or all lowercase.</summary>
        BadCase           = 8,

        /// <summary>An invalid GUID was supplied.</summary>
        BadGuid           = 9,

        /// <summary>An invalid wildcard file name was supplied, or the use of wildcards was invalid.</summary>
        BadWildcard       = 10,

        /// <summary>An invalid identifier was supplied.</summary>
        BadIdentifier     = 11,

        /// <summary>Invalid language IDs were supplied.</summary>
        BadLanguage       = 12,

        /// <summary>An invalid file name was supplied.</summary>
        BadFileName       = 13,

        /// <summary>An invalid path was supplied.</summary>
        BadPath           = 14,

        /// <summary>An invalid conditional statement was supplied.</summary>
        BadCondition      = 15,

        /// <summary>An invalid format string was supplied.</summary>
        BadFormatted      = 16,

        /// <summary>An invalid template string was supplied.</summary>
        BadTemplate       = 17,

        /// <summary>An invalid string was supplied in the DefaultDir column of the Directory table.</summary>
        BadDefaultDir     = 18,

        /// <summary>An invalid registry path string was supplied.</summary>
        BadRegPath        = 19,

        /// <summary>An invalid string was supplied in the CustomSource column of the CustomAction table.</summary>
        BadCustomSource   = 20,

        /// <summary>An invalid property string was supplied.</summary>
        BadProperty       = 21,

        /// <summary>The _Validation table is missing a reference to a column.</summary>
        MissingData       = 22,

        /// <summary>The category column of the _Validation table for the column is invalid.</summary>
        BadCategory       = 23,

        /// <summary>The table in the Keytable column of the _Validation table was not found or loaded.</summary>
        BadKeyTable       = 24,

        /// <summary>The value in the MaxValue column of the _Validation table is less than the value in the MinValue column.</summary>
        BadMaxMinValues   = 25,

        /// <summary>An invalid cabinet name was supplied.</summary>
        BadCabinet        = 26,

        /// <summary>An invalid shortcut target name was supplied.</summary>
        BadShortcut       = 27,

        /// <summary>The string is too long for the length specified by the column definition.</summary>
        StringOverflow    = 28,

        /// <summary>An invalid localization attribute was supplied. (Primary keys cannot be localized.)</summary>
        BadLocalizeAttrib = 29
    }

    /// <summary>
    /// Specifies the modify mode for <see cref="View.Modify"/>.
    /// </summary>
    public enum ViewModifyMode : int
    {
        /// <summary>
        /// Refreshes the information in the supplied record without changing the position
        /// in the result set and without affecting subsequent fetch operations.
        /// </summary>
        Seek             = -1,

        /// <summary>Refreshes the data in a Record.</summary>
        Refresh          = 0,

        /// <summary>Inserts a Record into the view.</summary>
        Insert           = 1,

        /// <summary>Updates the View with new data from the Record.</summary>
        Update           = 2,

        /// <summary>Updates or inserts a Record into the View.</summary>
        Assign           = 3,

        /// <summary>Updates or deletes and inserts a Record into the View.</summary>
        Replace          = 4,

        /// <summary>Inserts or validates a record.</summary>
        Merge            = 5,

        /// <summary>Deletes a Record from the View.</summary>
        Delete           = 6,

        /// <summary>Inserts a Record into the View.  The inserted data is not persistent.</summary>
        InsertTemporary  = 7,

        /// <summary>Validates a record.</summary>
        Validate         = 8,

        /// <summary>Validates a new record.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        ValidateNew      = 9,

        /// <summary>Validates fields of a fetched or new record. Can validate one or more fields of an incomplete record.</summary>
        ValidateField    = 10,

        /// <summary>Validates a record that will be deleted later.</summary>
        ValidateDelete   = 11,
    }
}
