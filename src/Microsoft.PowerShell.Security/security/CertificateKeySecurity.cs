// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX

using System;
using System.Security.AccessControl;

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// Specifies the certificate key access and auditing rights.
/// </summary>
[Flags]
public enum CertificateKeyRights
{
    /// <summary>
    /// Read the key data.
    /// </summary>
    ReadData = 0x00000001,

    /// <summary>
    /// Specifies the right to append data.
    /// </summary>
    AppendData = 0x00000004,

    /// <summary>
    ///  Read extended attributes of the key.
    /// </summary>
    ReadExtendedAttributes = 0x00000008,

    /// <summary>
    /// Write extended attributes of the key.
    /// </summary>
    WriteExtendedAttributes = 0x00000010,

    /// <summary>
    /// Read attributes of the key.
    /// </summary>
    ReadAttributes = 0x00000080,

    /// <summary>
    /// Write attributes of the key.
    /// </summary>
    WriteAttributes = 0x00000100,

    /// <summary>
    ///  Delete the key.
    /// </summary>
    Delete = 0x00010000,

    /// <summary>
    ///  Read permissions for the key.
    /// </summary>
    ReadPermissions = 0x00020000,

    /// <summary>
    ///  Change permissions for the key.
    /// </summary>
    ChangePermissions = 0x00040000,

    /// <summary>
    /// Take ownership of the key.
    /// </summary>
    TakeOwnership = 0x00080000,

    /// <summary>
    ///  Use the key for synchronization.
    /// </summary>
    Synchronize = 0x00100000,

    /// <summary>
    /// Read access to the key. This represents the Read right as shown in the
    /// security dialogue GUI.
    /// </summary>
    Read = Synchronize | ReadPermissions | ReadAttributes |
        ReadExtendedAttributes | ReadData,

    /// <summary>
    /// Full control of the key. This represents the Full Control right as
    /// shown in the security dialogue GUI.
    /// </summary>
    FullControl = Synchronize | TakeOwnership | ChangePermissions |
        ReadPermissions | Delete | WriteAttributes | ReadAttributes |
        WriteExtendedAttributes | ReadExtendedAttributes | AppendData |
        ReadData | 0x62, // extra mask fills in missing rights for FC.

    /// <summary>
    /// A combination of GenericRead and GenericWrite.
    /// </summary>
    GenericAll = 0x10000000,

    /// <summary>
    /// Not used.
    /// </summary>
    GenericExecute = 0x20000000,

    /// <summary>
    ///  Write the key data, extended attributes of the key, attributes
    ///  of the key, and permissions for the key.
    /// </summary>
    GenericWrite = 0x40000000,

    /// <summary>
    /// Read the key data, extended attributes of the key, attributes of
    /// the key, and permissions for the key.
    /// </summary>
    GenericRead = unchecked((int)0x80000000),
}

/// <summary>
/// The CertificateKeySecurity ObjectSecurity implementation.
/// </summary>
public sealed class CertificateKeySecurity : ObjectSecurity<CertificateKeyRights>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateKeySecurity"/> class.
    /// </summary>
    public CertificateKeySecurity()
        : base(false, ResourceType.Unknown)
    {
    }
}

#endif
