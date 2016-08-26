//---------------------------------------------------------------------
// <copyright file="SummaryInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Provides access to summary information of a Windows Installer database.
    /// </summary>
    internal class SummaryInfo : InstallerHandle
    {
        internal const int MAX_PROPERTIES = 20;

        /// <summary>
        /// Gets a SummaryInfo object that can be used to examine, update, and add
        /// properties to the summary information stream of a package or transform.
        /// </summary>
        /// <param name="packagePath">Path to the package (database) or transform</param>
        /// <param name="enableWrite">True to reserve resources for writing summary information properties.</param>
        /// <exception cref="FileNotFoundException">the package does not exist or could not be read</exception>
        /// <exception cref="InstallerException">the package is an invalid format</exception>
        /// <remarks><p>
        /// The SummaryInfo object should be <see cref="InstallerHandle.Close"/>d after use.
        /// It is best that the handle be closed manually as soon as it is no longer
        /// needed, as leaving lots of unused handles open can degrade performance.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetsummaryinformation.asp">MsiGetSummaryInformation</a>
        /// </p></remarks>
        public SummaryInfo(string packagePath, bool enableWrite)
            : base((IntPtr) SummaryInfo.OpenSummaryInfo(packagePath, enableWrite), true)
        {
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SummaryInfo(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        /// <summary>Gets or sets the Title summary information property.</summary>
        /// <remarks><p>
        /// The Title summary information property briefly describes the type of installer package. Phrases
        /// such as "Installation Database" or "Transform" or "Patch" may be used for this property.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Title
        {
            get { return this[2]; }
            set { this[2] = value; }
        }

        /// <summary>Gets or sets the Subject summary information property.</summary>
        /// <remarks><p>
        /// The Subject summary information property conveys to a file browser the product that can be installed using
        /// the logic and data in this installer database. For example, the value of the summary property for
        /// Microsoft Office 97 would be "Microsoft Office 97 Professional". This value is typically set from the
        /// installer property ProductName.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Subject
        {
            get { return this[3]; }
            set { this[3] = value; }
        }

        /// <summary>Gets or sets the Author summary information property.</summary>
        /// <remarks><p>
        /// The Author summary information property conveys to a file browser the manufacturer of the installation
        /// database. This value is typically set from the installer property Manufacturer.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Author
        {
            get { return this[4]; }
            set { this[4] = value; }
        }

        /// <summary>Gets or sets the Keywords summary information property.</summary>
        /// <remarks><p>
        /// The Keywords summary information property is used by file browsers to hold keywords that permit the
        /// database file to be found in a keyword search. The set of keywords typically includes "Installer" as
        /// well as product-specific keywords, and may be localized.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Keywords
        {
            get { return this[5]; }
            set { this[5] = value; }
        }

        /// <summary>Gets or sets the Comments summary information property.</summary>
        /// <remarks><p>
        /// The Comments summary information property conveys the general purpose of the installer database. By convention,
        /// the value for this summary property is set to the following:
        /// </p><p>
        /// "This installer database contains the logic and data required to install &lt;product name&gt;."
        /// </p><p>
        /// where &lt;product name&gt; is the name of the product being installed. In general the value for this summary
        /// property only changes in the product name, nothing else.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Comments
        {
            get { return this[6]; }
            set { this[6] = value; }
        }

        /// <summary>Gets or sets the Template summary information property.</summary>
        /// <remarks><p>
        /// The Template summary information property indicates the platform and language versions supported by the database.
        /// </p><p>
        /// The syntax of the Template Summary property information is:
        /// [platform property][,platform property][,...];[language id][,language id][,...]
        /// </p><p>
        /// For example, the following are all valid values for the Template Summary property:
        /// <list type="bullet">
        /// <item>Intel;1033</item>
        /// <item>Intel64;1033</item>
        /// <item>;1033</item>
        /// <item>;</item>
        /// <item>Intel ;1033,2046</item>
        /// <item>Intel64;1033,2046</item>
        /// <item>Intel;0</item>
        /// </list>
        /// </p><p>
        /// If this is a 64-bit Windows Installer, enter Intel64 in the Template summary information property. Note that an
        /// installation package cannot have both the Intel and Intel64 properties set.
        /// </p><p>
        /// If the current platform does not match one of the platforms specified then the installer will not process the
        /// package. Not specifying a platform implies that the package is platform-independent.
        /// </p><p>
        /// Entering 0 in the language ID field of the Template summary information property, or leaving this field empty,
        /// indicates that the package is language neutral.
        /// </p><p>
        /// There are variations of this property depending on whether it is in a source installer database or a transform.
        /// </p><p>
        /// Source Installer Database - Only one language can be specified in a source installer database. Merge Modules are
        /// the only packages that may have multiple languages. For more information, see Multiple Language Merge Modules.
        /// </p><p>
        /// Transform - In a transform file, only one language may be specified. The specified platform and language determine
        /// whether a transform can be applied to a particular database. The platform property and the language property can
        /// be left blank if no transform restriction relies on them to validate the transform.
        /// </p><p>
        /// This summary property is REQUIRED.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Template
        {
            get { return this[7]; }
            set { this[7] = value; }
        }

        /// <summary>Gets or sets the LastSavedBy summary information property.</summary>
        /// <remarks><p>
        /// The installer sets the Last Saved By summary information property to the value of the LogonUser property during
        /// an administrative installation. The installer never uses this property and a user never needs to modify it.
        /// Developers of a database editing tool may use this property to track the last person to modify the database.
        /// This property should be left set to null in a final shipping database.
        /// </p><p>
        /// In a transform, this summary property contains the platform and language ID(s) that a database should have
        /// after it has been transformed. The property specifies to what the Template should be set in the new database.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string LastSavedBy
        {
            get { return this[8]; }
            set { this[8] = value; }
        }

        /// <summary>Gets or sets the RevisionNumber summary information property.</summary>
        /// <remarks><p>
        /// The Revision Number summary information property contains the package code for the installer package. The
        /// package code is a unique identifier of the installer package.
        /// </p><p>
        /// The Revision Number summary information  property of a patch package specifies the GUID patch code for
        /// the patch. This is followed by a list of patch code GUIDs for obsolete patches that are removed when this
        /// patch is applied. The patch codes are concatenated with no delimiters separating GUIDs in the list.
        /// </p><p>
        /// The Revision Number summary information  property of a transform package lists the product code GUIDs
        /// and version of the new and original products and the upgrade code GUID. The list is separated with
        /// semicolons as follows.
        /// </p><p>
        /// Original-Product-Code Original-Product-Version ; New-Product Code New-Product-Version; Upgrade-Code
        /// </p><p>
        /// This summary property is REQUIRED.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string RevisionNumber
        {
            get { return this[9]; }
            set { this[9] = value; }
        }

        /// <summary>Gets or sets the CreatingApp summary information property.</summary>
        /// <remarks><p>
        /// The CreatingApp summary information property conveys which application created the installer database.
        /// In general the value for this summary property is the name of the software used to author this database.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string CreatingApp
        {
            get { return this[18]; }
            set { this[18] = value; }
        }

        /// <summary>Gets or sets the LastPrintTime summary information property.</summary>
        /// <remarks><p>
        /// The LastPrintTime summary information property can be set to the date and time during an administrative
        /// installation to record when the administrative image was created. For non-administrative installations
        /// this property is the same as the CreateTime summary information property.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime LastPrintTime
        {
            get { return (DateTime) this[11, typeof(DateTime)]; }
            set { this[11, typeof(DateTime)] = value; }
        }

        /// <summary>Gets or sets the CreateTime summary information property.</summary>
        /// <remarks><p>
        /// The CreateTime summary information property conveys when the installer database was created.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime CreateTime
        {
            get { return (DateTime) this[12, typeof(DateTime)]; }
            set { this[12, typeof(DateTime)] = value; }
        }

        /// <summary>Gets or sets the LastSaveTime summary information property.</summary>
        /// <remarks><p>
        /// The LastSaveTime summary information property conveys when the last time the installer database was
        /// modified. Each time a user changes an installation the value for this summary property is updated to
        /// the current system time/date at the time the installer database was saved. Initially the value for
        /// this summary property is set to null to indicate that no changes have yet been made.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime LastSaveTime
        {
            get { return (DateTime) this[13, typeof(DateTime)]; }
            set { this[13, typeof(DateTime)] = value; }
        }

        /// <summary>Gets or sets the CodePage summary information property.</summary>
        /// <remarks><p>
        /// The Codepage summary information property is the numeric value of the ANSI code page used for any
        /// strings that are stored in the summary information. Note that this is not the same code page for
        /// strings in the installation database. The Codepage summary information property is used to translate
        /// the strings in the summary information into Unicode when calling the Unicode API functions. The
        /// Codepage summary information property must be set before any string properties are set in the
        /// summary information.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public short CodePage
        {
            get { return (short) this[1, typeof(short)]; }
            set { this[1, typeof(short)] = value; }
        }

        /// <summary>Gets or sets the PageCount summary information property.</summary>
        /// <remarks><p>
        /// For an installation package, the PageCount summary information property contains the minimum
        /// installer version required. For Windows Installer version 1.0, this property must be set to the
        /// integer 100. For 64-bit Windows Installer Packages, this property must be set to the integer 200.
        /// </p><p>
        /// For a transform package, the PageCount summary information property contains minimum installer
        /// version required to process the transform. Set to the greater of the two PageCount summary information
        /// property values belonging to the databases used to generate the transform.
        /// </p><p>
        /// The PageCount summary information property is set to null in patch packages.
        /// </p><p>
        /// This summary property is REQUIRED.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int PageCount
        {
            get { return (int) this[14, typeof(int)]; }
            set { this[14, typeof(int)] = value; }
        }

        /// <summary>Gets or sets the WordCount summary information property.</summary>
        /// <remarks><p>
        /// The WordCount summary information property indicates the type of source file image. If this property is
        /// not present, it defaults to 0. Note that this property is stored in place of the standard Count property.
        /// </p><p>
        /// This property is a bit field. New bits may be added in the future. At present the following bits are
        /// available:
        /// <list type="bullet">
        /// <item>Bit 0: 0 = long file names, 1 = short file names</item>
        /// <item>Bit 1: 0 = source is uncompressed, 1 = source is compressed</item>
        /// <item>Bit 2: 0 = source is original media, 1 = source is administrative installation</item>
        /// <item>[MSI 4.0] Bit 3: 0 = elevated privileges can be required to install, 1 = elevated privileges are not required to install</item>
        /// </list>
        /// </p><p>
        /// These are combined to give the WordCount summary information property one of the following values
        /// indicating a type of source file image:
        /// <list type="bullet">
        /// <item>0 - Original source using long file names. Matches tree in Directory table.</item>
        /// <item>1 - Original source using short file names. Matches tree in Directory table.</item>
        /// <item>2 - Compressed source files using long file names. Matches cabinets and files in the Media table.</item>
        /// <item>3 - Compressed source files using short file names. Matches cabinets and files in the Media table.</item>
        /// <item>4 - Administrative image using long file names. Matches tree in Directory table.</item>
        /// <item>5 - Administrative image using short file names. Matches tree in Directory table.</item>
        /// </list>
        /// </p><p>
        /// Note that if the package is marked as compressed (bit 1 is set), the installer only installs files
        /// located at the root of the source. In this case, even files marked as uncompressed in the File table must
        /// be located at the root to be installed. To specify a source image that has both a cabinet file (compressed
        /// files) and uncompressed files that match the tree in the Directory table, mark the package as uncompressed
        /// by leaving bit 1 unset (value=0) in the WordCount summary information property and set
        /// <see cref="FileAttributes.Compressed"/> (value=16384) in the Attributes column of the File table
        /// for each file in the cabinet.
        /// </p><p>
        /// For a patch package, the WordCount summary information property specifies the patch engine that was used
        /// to create the patch files. The default value is 1 and indicates that MSPATCH was used to create the patch
        /// A value of "2" means that the patch is using smaller, optimized, files available only with Windows Installer
        /// version 1.2 or later. A patch with a WordCount of "2" fails immediately if used with a Windows Installer
        /// version earlier than 1.2. A patch with a WordCount of "3" fails immediately if used with a Windows Installer
        /// version earlier than 2.0.
        /// </p><p>
        /// This summary property is REQUIRED.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int WordCount
        {
            get { return (int) this[15, typeof(int)]; }
            set { this[15, typeof(int)] = value; }
        }

        /// <summary>Gets or sets the CharacterCount summary information property.</summary>
        /// <remarks><p>
        /// The CharacterCount summary information property is only used in transforms. This part of the summary
        /// information stream is divided into two 16-bit words. The upper word contains the transform validation
        /// flags. The lower word contains the transform error condition flags.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int CharacterCount
        {
            get { return (int) this[16, typeof(int)]; }
            set { this[16, typeof(int)] = value; }
        }

        /// <summary>Gets or sets the Security summary information property.</summary>
        /// <remarks><p>
        /// The Security summary information property conveys whether the package should be opened as read-only. The database
        /// editing tool should not modify a read-only enforced database and should issue a warning at attempts to modify a
        /// read-only recommended database. The following values of this property are applicable to Windows Installer files:
        /// <list type="bullet">
        /// <item>0 - no restriction</item>
        /// <item>2 - read only recommended</item>
        /// <item>4 - read only enforced</item>
        /// </list>
        /// </p><p>
        /// This property should be set to read-only recommended (2) for an installation database and to read-only
        /// enforced (4) for a transform or patch.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfogetproperty.asp">MsiSummaryInfoGetProperty</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfosetproperty.asp">MsiSummaryInfoSetProperty</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Security
        {
            get { return (int) this[19, typeof(int)]; }
            set { this[19, typeof(int)] = value; }
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private object this[uint property, Type type]
        {
            get
            {
                uint dataType;
                StringBuilder stringValue = new StringBuilder("");
                uint bufSize = 0;
                int intValue;
                long timeValue = 0;

                uint ret = RemotableNativeMethods.MsiSummaryInfoGetProperty(
                    (int) this.Handle,
                    property,
                    out dataType,
                    out intValue,
                    ref timeValue,
                    stringValue,
                    ref bufSize);
                if (ret != 0 && dataType != (uint) VarEnum.VT_LPSTR)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                switch ((VarEnum) dataType)
                {
                    case VarEnum.VT_EMPTY:
                    {
                        if (type == typeof(DateTime))
                        {
                            return DateTime.MinValue;
                        }
                        else if (type == typeof(string))
                        {
                            return String.Empty;
                        }
                        else if (type == typeof(short))
                        {
                            return (short) 0;
                        }
                        else
                        {
                            return (int) 0;
                        }
                    }

                    case VarEnum.VT_LPSTR:
                    {
                        if (ret == (uint) NativeMethods.Error.MORE_DATA)
                        {
                            stringValue.Capacity = (int) ++bufSize;
                            ret = RemotableNativeMethods.MsiSummaryInfoGetProperty(
                                (int) this.Handle,
                                property,
                                out dataType,
                                out intValue,
                                ref timeValue,
                                stringValue,
                                ref bufSize);
                        }
                        if (ret != 0)
                        {
                            throw InstallerException.ExceptionFromReturnCode(ret);
                        }
                        return stringValue.ToString();
                    }

                    case VarEnum.VT_I2:
                    case VarEnum.VT_I4:
                    {
                        if (type == typeof(string))
                        {
                            return intValue.ToString(CultureInfo.InvariantCulture);
                        }
                        else if (type == typeof(short))
                        {
                            return (short) intValue;
                        }
                        else
                        {
                            return intValue;
                        }
                    }

                    case VarEnum.VT_FILETIME:
                    {
                        if (type == typeof(string))
                        {
                            return DateTime.FromFileTime(timeValue).ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            return DateTime.FromFileTime(timeValue);
                        }
                    }

                    default:
                    {
                        throw new InstallerException();
                    }
                }
            }

            set
            {
                uint dataType = (uint) VarEnum.VT_NULL;
                string stringValue = "";
                int intValue = 0;
                long timeValue = 0;

                if (type == typeof(short))
                {
                    dataType = (uint) VarEnum.VT_I2;
                    intValue = (int)(short) value;  // Double cast because value is a *boxed* short.
                }
                else if (type == typeof(int))
                {
                    dataType = (uint) VarEnum.VT_I4;
                    intValue = (int) value;
                }
                else if (type == typeof(string))
                {
                    dataType = (uint) VarEnum.VT_LPSTR;
                    stringValue = (string) value;
                }
                else // (type == typeof(DateTime))
                {
                    dataType = (uint) VarEnum.VT_FILETIME;
                    timeValue = ((DateTime) value).ToFileTime();
                }

                uint ret = NativeMethods.MsiSummaryInfoSetProperty(
                    (int) this.Handle,
                    property,
                    dataType,
                    intValue,
                    ref timeValue,
                    stringValue);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private string this[uint property]
        {
            get { return (string) this[property, typeof(string)]; }
            set { this[property, typeof(string)] = value; }
        }

        /// <summary>
        /// Formats and writes the previously stored properties into the standard summary information stream.
        /// </summary>
        /// <exception cref="InstallerException">The stream cannot be successfully written.</exception>
        /// <remarks><p>
        /// This method may only be called once after all the property values have been set. Properties may
        /// still be read after the stream is written.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisummaryinfopersist.asp">MsiSummaryInfoPersist</a>
        /// </p></remarks>
        public void Persist()
        {
            uint ret = NativeMethods.MsiSummaryInfoPersist((int) this.Handle);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        private static int OpenSummaryInfo(string packagePath, bool enableWrite)
        {
            int summaryInfoHandle;
            int maxProperties = !enableWrite ? 0 : SummaryInfo.MAX_PROPERTIES;
            uint ret = RemotableNativeMethods.MsiGetSummaryInformation(
                0,
                packagePath,
                (uint) maxProperties,
                out summaryInfoHandle);
            if (ret != 0)
            {
                if (ret == (uint) NativeMethods.Error.FILE_NOT_FOUND ||
                    ret == (uint) NativeMethods.Error.ACCESS_DENIED)
                {
                    throw new FileNotFoundException(null, packagePath);
                }
                else
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
            }
            return summaryInfoHandle;
        }
    }
}
