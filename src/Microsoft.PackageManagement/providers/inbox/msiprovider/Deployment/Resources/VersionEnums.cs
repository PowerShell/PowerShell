//---------------------------------------------------------------------
// <copyright file="VersionEnums.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.Resources
{
    // Silence warnings about doc-comments
    #pragma warning disable 1591
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Identifies build types of a versioned file.
    /// </summary>
    [Flags]
    public enum VersionBuildTypes : int
    {
        None         = 0x00,
        Debug        = 0x01,
        Prerelease   = 0x02,
        Patched      = 0x04,
        PrivateBuild = 0x08,
        InfoInferred = 0x10,
        SpecialBuild = 0x20,
    }

    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    internal enum VersionFileOS : int
    {
        Unknown       = 0x00000000,
        DOS           = 0x00010000,
        OS216         = 0x00020000,
        OS232         = 0x00030000,
        NT            = 0x00040000,
        WINCE         = 0x00050000,
        WINDOWS16     = 0x00000001,
        PM16          = 0x00000002,
        PM32          = 0x00000003,
        WINDOWS32     = 0x00000004,
        DOS_WINDOWS16 = 0x00010001,
        DOS_WINDOWS32 = 0x00010004,
        OS216_PM16    = 0x00020002,
        OS232_PM32    = 0x00030003,
        NT_WINDOWS32  = 0x00040004,
    }

    /// <summary>
    /// Identifies the type of a versioned file.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum VersionFileType : int
    {
        Unknown       = 0,
        Application   = 1,
        Dll           = 2,
        Driver        = 3,
        Font          = 4,
        VirtualDevice = 5,
        StaticLibrary = 7,
    }

    /// <summary>
    /// Identifies the sub-type of a versioned file.
    /// </summary>
    public enum VersionFileSubtype : int
    {
        Unknown                = 0,
        PrinterDriver          = 1,
        KeyboardDriver         = 2,
        LanguageDriver         = 3,
        DisplayDriver          = 4,
        MouseDriver            = 5,
        NetworkDriver          = 6,
        SystemDriver           = 7,
        InstallableDriver      = 8,
        SoundDriver            = 9,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Comm")]
        CommDriver             = 10,
        InputMethodDriver      = 11,
        VersionedPrinterDriver = 12,
        RasterFont             = 1,
        VectorFont             = 2,
        TrueTypeFont           = 3,
    }

    #pragma warning restore 1591
}
