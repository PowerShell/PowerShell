// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Threading;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    /// <summary>
    /// Defines a drive that exposes a provider path to the user.
    /// </summary>
    /// <remarks>
    /// A cmdlet provider may want to derive from this class to provide their
    /// own public members or to cache information related to the drive. For instance,
    /// if a drive is a connection to a remote machine and making that connection
    /// is expensive, then the provider may want keep a handle to the connection as
    /// a member of their derived <see cref="PSDriveInfo"/> class and use it when
    /// the provider is invoked.
    /// </remarks>
    public class PSDriveInfo : IComparable
    {
        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// using "SessionState" as the category.
        /// This is the same category as the SessionState tracer class.
        /// </summary>
        [Dbg.TraceSourceAttribute(
             "PSDriveInfo",
             "The namespace navigation tracer")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("PSDriveInfo",
             "The namespace navigation tracer");

        /// <summary>
        /// Gets or sets the current working directory for the drive.
        /// </summary>
        public string CurrentLocation
        {
            get
            {
                return _currentWorkingDirectory;
            }

            set
            {
                _currentWorkingDirectory = value;
            }
        }

        /// <summary>
        /// The current working directory for the virtual drive
        /// as a relative path from Root.
        /// </summary>
        private string _currentWorkingDirectory;

        /// <summary>
        /// Gets the name of the drive.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// The name of the virtual drive.
        /// </summary>
        private string _name;

        /// <summary>
        /// Gets the name of the provider that root path
        /// of the drive represents.
        /// </summary>
        public ProviderInfo Provider
        {
            get
            {
                return _provider;
            }
        }

        /// <summary>
        /// The provider information for the provider that implements
        /// the functionality for the drive.
        /// </summary>
        private ProviderInfo _provider;

        /// <summary>
        /// Gets the root path of the drive.
        /// </summary>
        public string Root
        {
            get
            {
                return _root;
            }

            internal set
            {
                _root = value;
            }
        }

        /// <summary>
        /// Sets the root of the drive.
        /// </summary>
        /// <param name="path">
        /// The root path to set for the drive.
        /// </param>
        /// <remarks>
        /// This method can only be called during drive
        /// creation. A NotSupportedException if this method
        /// is called outside of drive creation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If this method gets called any other time except
        /// during drive creation.
        /// </exception>
        internal void SetRoot(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!DriveBeingCreated)
            {
                NotSupportedException e =
                    PSTraceSource.NewNotSupportedException();
                throw e;
            }

            _root = path;
        }

        /// <summary>
        /// The root of the virtual drive.
        /// </summary>
        private string _root;

        /// <summary>
        /// Gets or sets the description for the drive.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// When supported by provider this specifies a maximum drive size.
        /// </summary>
        public long? MaximumSize { get; internal set; }

        /// <summary>
        /// Gets the credential to use with the drive.
        /// </summary>
        public PSCredential Credential { get; } = PSCredential.Empty;

        /// <summary>
        /// Determines if the root of the drive can
        /// be modified during drive creation through
        /// the SetRoot method.
        /// </summary>
        /// <value>
        /// True if the drive is being created and the
        /// root can be modified through the SetRoot method.
        /// False otherwise.
        /// </value>
        internal bool DriveBeingCreated { get; set; }

        /// <summary>
        /// True if the drive was automounted by the system,
        /// false otherwise.
        /// </summary>
        /// <value></value>
        internal bool IsAutoMounted { get; set; }

        /// <summary>
        /// True if the drive was automounted by the system,
        /// and then manually removed by the user.
        /// </summary>
        internal bool IsAutoMountedManuallyRemoved { get; set; }

        /// <summary>
        /// Gets or sets the Persist Switch parameter.
        /// If this switch parameter is set then the created PSDrive
        /// would be persisted across PowerShell sessions.
        /// </summary>
        internal bool Persist { get; } = false;

        /// <summary>
        /// Get or sets the value indicating if the created drive is a network drive.
        /// </summary>
        internal bool IsNetworkDrive { get; set; } = false;

        /// <summary>
        /// Gets or sets the UNC path of the drive. This property would be populated only
        /// if the created PSDrive is targeting a network drive or else this property
        /// would be null.
        /// </summary>
        public string DisplayRoot { get; internal set; } = null;

        /// <summary>
        /// Gets or sets if the drive-root relative paths on this drive are separated by a
        /// colon or not.
        ///
        /// This is true for all PSDrives on all platforms, except for filesystems on
        /// non-Windows platforms.
        ///
        /// This is not a path separator in the sense of separating paths in a single
        /// string.
        ///
        /// The biggest difference in filesystem handling between PS internally, and Unix
        /// style systems is, that paths on Windows separate the drive letter from the
        /// actual path by a colon. The second difference is, that a path that starts with
        /// a \ or / on Windows is considered to be a relative path (drive-relative in
        /// that case) where a similar path on a Unix style filesystem would be
        /// root-relative, which is basically drive-relative for the filesystem, as there
        /// is only one filesystem drive.
        ///
        /// This property indicates, that a path can be checked for that drive-relativity
        /// by checking for a colon. The main reason for this can be seen in all the
        /// places that use this property, where PowerShell's code checks/splits/string
        /// manipulates paths according to the colon character. This happens in many
        /// places.
        ///
        /// The idea here was to introduce a property that allows a code to query if a
        /// PSDrive expects colon to be such a separator or not. I talked to Jim back then
        /// about the problem, and this seemed to be a reasonable solution, given that
        /// there is no other way to know for a PSDrive if paths can be qualified only in
        /// a certain windows way on all platforms, or need special treatment on platforms
        /// where colon does not exist as drive separator (regular filesystems on Unix
        /// platforms are the only exception).
        ///
        /// Globally this property can also be only true for one single PSDrive, because
        /// if there is no drive separator, there is also no drive, and because there is
        /// no drive there is no way to match against multiple such drives.
        ///
        /// Additional data:
        /// It seems that on single rooted filesystems, only the default
        /// drive of "/" needs to set this VolumeSeparatedByColon to false
        /// otherwise, creating new drives from the filesystem should actually
        /// have this set to true as all the drives will have <string>: except
        /// for "/"
        /// </summary>
        public bool VolumeSeparatedByColon { get; internal set; } = true;

        #region ctor

        /// <summary>
        /// Constructs a new instance of the PSDriveInfo using another PSDriveInfo
        /// as a template.
        /// </summary>
        /// <param name="driveInfo">
        /// An existing PSDriveInfo object that should be copied to this instance.
        /// </param>
        /// <remarks>
        /// A protected constructor that derived classes can call with an instance
        /// of this class. This allows for easy creation of derived PSDriveInfo objects
        /// which can be created in CmdletProvider's NewDrive method using the PSDriveInfo
        /// that is passed in.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="PSDriveInfo"/> is null.
        /// </exception>
        protected PSDriveInfo(PSDriveInfo driveInfo)
        {
            if (driveInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(driveInfo));
            }

            _name = driveInfo.Name;
            _provider = driveInfo.Provider;
            Credential = driveInfo.Credential;
            _currentWorkingDirectory = driveInfo.CurrentLocation;
            Description = driveInfo.Description;
            this.MaximumSize = driveInfo.MaximumSize;
            DriveBeingCreated = driveInfo.DriveBeingCreated;
            _hidden = driveInfo._hidden;
            IsAutoMounted = driveInfo.IsAutoMounted;
            _root = driveInfo._root;
            Persist = driveInfo.Persist;
            this.Trace();
        }

        /// <summary>
        /// Constructs a drive that maps a PowerShell Path in
        /// the shell to a Cmdlet Provider.
        /// </summary>
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential)
        {
            // Verify the parameters

            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            if (root == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(root));
            }

            // Copy the parameters to the local members

            _name = name;
            _provider = provider;
            _root = root;
            Description = description;

            if (credential != null)
            {
                Credential = credential;
            }

            // Set the current working directory to the empty
            // string since it is relative to the root.

            _currentWorkingDirectory = string.Empty;

            Dbg.Diagnostics.Assert(
                _currentWorkingDirectory != null,
                "The currentWorkingDirectory cannot be null");

            // Trace out the fields

            this.Trace();
        }

        /// <summary>
        /// Constructs a drive that maps a PowerShell Path in
        /// the shell to a Cmdlet Provider.
        /// </summary>
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <param name="displayRoot">
        /// The network path of the drive. This field would be populated only if PSDriveInfo
        /// is targeting the network drive or else this filed is null for local drives.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential, string displayRoot)
            : this(name, provider, root, description, credential)
        {
            DisplayRoot = displayRoot;
        }

        /// <summary>
        /// Constructs a drive that maps a PowerShell Path in
        /// the shell to a Cmdlet Provider.
        /// </summary>
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <param name="persist">
        /// It indicates if the created PSDrive would be
        /// persisted across PowerShell sessions.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential,
            bool persist)
            : this(name, provider, root, description, credential)
        {
            Persist = persist;
        }

        #endregion ctor

        /// <summary>
        /// Gets the name of the drive as a string.
        /// </summary>
        /// <returns>
        /// Returns a String that is that name of the drive.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Gets or sets the hidden property. The hidden property
        /// determines if the drive should be hidden from the user.
        /// </summary>
        /// <value>
        /// True if the drive should be hidden from the user, false
        /// otherwise.
        /// </value>
        internal bool Hidden
        {
            get
            {
                return _hidden;
            }

            set
            {
                _hidden = value;
            }
        }

        /// <summary>
        /// Determines if the drive should be hidden from the user.
        /// </summary>
        private bool _hidden;

        /// <summary>
        /// Sets the name of the drive to a new name.
        /// </summary>
        /// <param name="newName">
        /// The new name for the drive.
        /// </param>
        /// <remarks>
        /// This must be internal so that we allow the renaming of drives
        /// via the Core Command API but not through a reference to the
        /// drive object. More goes in to renaming a drive than just modifying
        /// the name in this class.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <paramref name="newName"/> is null or empty.
        /// </exception>
        internal void SetName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw PSTraceSource.NewArgumentException(nameof(newName));
            }

            _name = newName;
        }

        /// <summary>
        /// Sets the provider of the drive to a new provider.
        /// </summary>
        /// <param name="newProvider">
        /// The new provider for the drive.
        /// </param>
        /// <remarks>
        /// This must be internal so that we allow the renaming of providers.
        /// All drives must be associated with the new provider name and can
        /// be changed using the Core Command API but not through a reference to the
        /// drive object. More goes in to renaming a provider than just modifying
        /// the provider in this class.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="newProvider"/> is null.
        /// </exception>
        internal void SetProvider(ProviderInfo newProvider)
        {
            if (newProvider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(newProvider));
            }

            _provider = newProvider;
        }

        /// <summary>
        /// Traces the virtual drive.
        /// </summary>
        internal void Trace()
        {
            s_tracer.WriteLine(
                "A drive was found:");

            if (Name != null)
            {
                s_tracer.WriteLine(
                    "\tName: {0}",
                    Name);
            }

            if (Provider != null)
            {
                s_tracer.WriteLine(
                    "\tProvider: {0}",
                    Provider);
            }

            if (Root != null)
            {
                s_tracer.WriteLine(
                    "\tRoot: {0}",
                    Root);
            }

            if (CurrentLocation != null)
            {
                s_tracer.WriteLine(
                    "\tCWD: {0}",
                    CurrentLocation);
            }

            if (Description != null)
            {
                s_tracer.WriteLine(
                    "\tDescription: {0}",
                    Description);
            }
        }

        /// <summary>
        /// Compares this instance to the specified drive.
        /// </summary>
        /// <param name="drive">
        /// A PSDriveInfo object to compare.
        /// </param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and object specified.
        /// Return Value: Less than zero        Meaning: This instance is less than object.
        /// Return Value: Zero                  Meaning: This instance is equal to object.
        /// Return Value: Greater than zero     Meaning: This instance is greater than object or object is a null reference.
        /// </returns>
        public int CompareTo(PSDriveInfo drive)
        {
#pragma warning disable 56506

            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(drive));
            }

            return string.Compare(Name, drive.Name, StringComparison.OrdinalIgnoreCase);

#pragma warning restore 56506
        }

        /// <summary>
        /// Compares this instance to the specified object. The object must be a PSDriveInfo.
        /// </summary>
        /// <param name="obj">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// A signed number indicating the relative values of this
        /// instance and object specified.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="obj"/> is not a PSDriveInfo instance.
        /// </exception>
        public int CompareTo(object obj)
        {
            PSDriveInfo drive = obj as PSDriveInfo;

            if (drive == null)
            {
                ArgumentException e =
                    PSTraceSource.NewArgumentException(
                        nameof(obj),
                        SessionStateStrings.OnlyAbleToComparePSDriveInfo);
                throw e;
            }

            return (CompareTo(drive));
        }

        /// <summary>
        /// Compares this instance to the specified object.
        /// </summary>
        /// <param name="obj">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// True if the drive names are equal, false otherwise.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is PSDriveInfo)
            {
                return CompareTo(obj) == 0;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compares this instance to the specified object.
        /// </summary>
        /// <param name="drive">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// True if the drive names are equal, false otherwise.
        /// </returns>
        public bool Equals(PSDriveInfo drive)
        {
            return CompareTo(drive) == 0;
        }

        /// <summary>
        /// Equality operator for the drive determines if the drives
        /// are equal by having the same name.
        /// </summary>
        /// <param name="drive1">
        /// The first object to compare to the second.
        /// </param>
        /// <param name="drive2">
        /// The second object to compare to the first.
        /// </param>
        /// <returns>
        /// True if the objects are PSDriveInfo objects and have the same name,
        /// false otherwise.
        /// </returns>
        public static bool operator ==(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if ((drive1Object == null) == (drive2Object == null))
            {
                if (drive1Object != null)
                {
                    return drive1.Equals(drive2);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Inequality operator for the drive determines if the drives
        /// are not equal by using the drive name.
        /// </summary>
        /// <param name="drive1">
        /// The first object to compare to the second.
        /// </param>
        /// <param name="drive2">
        /// The second object to compare to the first.
        /// </param>
        /// <returns>
        /// True if the PSDriveInfo objects do not have the same name,
        /// false otherwise.
        /// </returns>
        public static bool operator !=(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            return !(drive1 == drive2);
        }

        /// <summary>
        /// Compares the specified drives to determine if drive1 is less than
        /// drive2.
        /// </summary>
        /// <param name="drive1">
        /// The drive to determine if it is less than the other drive.
        /// </param>
        /// <param name="drive2">
        /// The drive to compare drive1 against.
        /// </param>
        /// <returns>
        /// True if the lexical comparison of drive1's name is less than drive2's name.
        /// </returns>
        public static bool operator <(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if (drive1Object == null)
            {
                return (drive2Object != null);
            }
            else
            {
                if (drive2Object == null)
                {
                    // Since drive1 is not null and drive2 is, drive1 is greater than drive2
                    return false;
                }
                else
                {
                    // Since drive1 and drive2 are not null use the CompareTo

                    return drive1.CompareTo(drive2) < 0;
                }
            }
        }

        /// <summary>
        /// Compares the specified drives to determine if drive1 is greater than
        /// drive2.
        /// </summary>
        /// <param name="drive1">
        /// The drive to determine if it is greater than the other drive.
        /// </param>
        /// <param name="drive2">
        /// The drive to compare drive1 against.
        /// </param>
        /// <returns>
        /// True if the lexical comparison of drive1's name is greater than drive2's name.
        /// </returns>
        public static bool operator >(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if ((drive1Object == null))
            {
                // Since both drives are null, they are equal
                // Since drive1 is null it is less than drive2 which is not null
                return false;
            }
            else
            {
                if (drive2Object == null)
                {
                    // Since drive1 is not null and drive2 is, drive1 is greater than drive2
                    return true;
                }
                else
                {
                    // Since drive1 and drive2 are not null use the CompareTo

                    return drive1.CompareTo(drive2) > 0;
                }
            }
        }

        /// <summary>
        /// Gets the hash code for this instance.
        /// </summary>
        /// <returns>The result of base.GetHashCode().</returns>
        /// <!-- Override the base GetHashCode because the compiler complains
        /// if you don't when you implement operator== and operator!= -->
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private PSNoteProperty _noteProperty;

        internal PSNoteProperty GetNotePropertyForProviderCmdlets(string name)
        {
            if (_noteProperty == null)
            {
                Interlocked.CompareExchange(ref _noteProperty,
                                            new PSNoteProperty(name, this), null);
            }

            return _noteProperty;
        }
    }
}
