// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
#if !UNIX

using System;
using System.Buffers;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace Microsoft.PowerShell.Commands;

/// <summary>
/// The CertificateProvider ISecurityDescriptorCmdletProvider
/// implementation.
/// </summary>
public sealed partial class CertificateProvider : ISecurityDescriptorCmdletProvider
{
#pragma warning disable SA1310 // Reflects native C constant name.
    private const int OWNER_SECURITY_INFORMATION = 0x00000001;
    private const int GROUP_SECURITY_INFORMATION = 0x00000002;
    private const int DACL_SECURITY_INFORMATION = 0x00000004;
    private const int SACL_SECURITY_INFORMATION = 0x00000008;
#pragma warning restore SA1310

    private delegate int GetKeySecurityDescriptorDelegate(
        SafeHandle key,
        int sections,
        Span<byte> data,
        out int dataSize);

    private delegate int SetKeySecurityDescriptorDelegate(
        SafeHandle key,
        int sections,
        ReadOnlySpan<byte> data);

    /// <summary>
    /// Gets the SecurityDescriptor at the specified path, including only the specified
    /// AccessControlSections.
    /// </summary>
    /// <param name="path">
    /// The path of the item to retrieve. It may be a drive or provider-qualified path and may include.
    /// glob characters.
    /// </param>
    /// <param name="includeSections">
    /// The sections of the security descriptor to include.
    /// </param>
    public void GetSecurityDescriptor(
        string? path,
        AccessControlSections includeSections)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw PSTraceSource.NewArgumentNullException(nameof(path));
        }

        if ((includeSections & ~AccessControlSections.All) != 0)
        {
            throw PSTraceSource.NewArgumentException(nameof(includeSections));
        }

        path = NormalizePath(path);
        object item = this.GetItemAtPath(path, false, out var _);
        if (!(item is X509Certificate2 cert))
        {
            throw PSTraceSource.NewArgumentException(
                nameof(path),
                CertificateProviderStrings.CannotGetAclWrongPathType,
                path);
        }

        using var keyHandle = GetCertificateKeyHandle(cert);
        try
        {
            CertificateKeySecurity sd = GetKeySecurity(keyHandle, includeSections);
            WriteSecurityDescriptorObject(sd, path);
        }
        catch (Win32Exception e)
        {
            string msg = string.Format(CertificateProviderStrings.GetKeySDFailure, e.Message);
            ErrorRecord err = new ErrorRecord(
                e,
                "GetCertKeyDescriptorWin32Failure",
                ErrorCategory.NotSpecified,
                path)
            {
                ErrorDetails = new ErrorDetails(msg),
            };
            throw new CertificateProviderWrappedErrorRecord(err);
        }
    }

    /// <summary>
    /// Creates a new empty security descriptor of the same type as
    /// the item specified by the path.
    /// </summary>
    /// <param name="path">
    /// Path of the item to use to determine the type of resulting
    /// SecurityDescriptor.
    /// </param>
    /// <param name="includeSections">
    /// The sections of the security descriptor to create.
    /// </param>
    /// <returns>
    /// A new ObjectSecurity object of the same type as
    /// the item specified by the path.
    /// </returns>
    public ObjectSecurity NewSecurityDescriptorFromPath(
        string? path,
        AccessControlSections includeSections)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw PSTraceSource.NewArgumentNullException(nameof(path));
        }

        if ((includeSections & ~AccessControlSections.All) != 0)
        {
            throw PSTraceSource.NewArgumentException(nameof(includeSections));
        }

        return new CertificateKeySecurity();
    }

    /// <summary>
    /// Creates a new empty security descriptor of the specified type.
    /// </summary>
    /// <param name="type">
    /// The type of Security Descriptor to create. The only valid type is
    /// "key".
    /// </param>
    /// <param name="includeSections">
    /// The sections of the security descriptor to create.
    /// </param>
    /// <returns>
    /// A new ObjectSecurity object of the specified type.
    /// </returns>
    public ObjectSecurity NewSecurityDescriptorOfType(
        string type,
        AccessControlSections includeSections)
    {
        if (type.ToLowerInvariant() != "key")
        {
            throw PSTraceSource.NewArgumentException(
                nameof(type),
                CertificateProviderStrings.CannotGetAclWrongItemType,
                type);
        }

        if ((includeSections & ~AccessControlSections.All) != 0)
        {
            throw PSTraceSource.NewArgumentException(nameof(includeSections));
        }

        return new CertificateKeySecurity();
    }

    /// <summary>
    /// Sets the SecurityDescriptor at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path of the item to set the security descriptor on.
    /// It may be a drive or provider-qualified path and may include.
    /// glob characters.
    /// </param>
    /// <param name="securityDescriptor">
    /// The new security descriptor for the item.
    /// </param>
    public void SetSecurityDescriptor(
        string? path,
        ObjectSecurity? securityDescriptor)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw PSTraceSource.NewArgumentNullException(nameof(path));
        }

        if (securityDescriptor is null)
        {
            throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
        }

        path = NormalizePath(path);
        object item = this.GetItemAtPath(path, false, out var _);
        if (!(item is X509Certificate2 cert))
        {
            throw PSTraceSource.NewArgumentException(
                nameof(path),
                CertificateProviderStrings.CannotSetAclWrongPathType,
                path);
        }

        using var keyHandle = GetCertificateKeyHandle(cert);
        try
        {
            try
            {
                // First try to set with all as we don't know what the caller
                // has changed on the SD.
                SetKeySecurity(keyHandle, securityDescriptor, AccessControlSections.All);
            }
            catch (Win32Exception e) when (
                e.NativeErrorCode == Interop.Windows.ERROR_PRIVILEGE_NOT_HELD ||
                e.NativeErrorCode == Interop.Windows.ERROR_INVALID_SECURITY_DESCR ||
                e.NativeErrorCode == Interop.Windows.ERROR_INVALID_PARAMETER)
            {
                /*
                Failed to set all sections of the SD, this fallback tries to
                determine what sections can be removed in the set operation.
                We expect the following error codes:

                    ERROR_PRIVILEGE_NOT_HELD: Setting the SACL requires
                    SeSecurityPrivilege to be enabled. We can avoid that
                    if the SD has no SACL entries.

                    ERROR_INVALID_SECURITY_DESCR: Tries to set an SD with a
                    null owner or group (CNG based keys). The filter will
                    handle this.

                    ERROR_INVALID_PARAMETER: Same as the above but for
                    CryptoAPI based keys.
                */
                CertificateKeySecurity currentSD = GetKeySecurity(
                    keyHandle,
                    AccessControlSections.Owner | AccessControlSections.Group | AccessControlSections.Access);
                AccessControlSections sections = GetLimitedSections(securityDescriptor, currentSD);
                if (sections == AccessControlSections.All)
                {
                    throw;
                }

                SetKeySecurity(keyHandle, securityDescriptor, sections);
            }
        }
        catch (Win32Exception e)
        {
            string msg = string.Format(CertificateProviderStrings.SetKeySDFailure, e.Message);

            ErrorRecord err = new ErrorRecord(
                e,
                "SetCertKeyDescriptorWin32Failure",
                ErrorCategory.NotSpecified,
                null)
            {
                ErrorDetails = new ErrorDetails(msg),
            };
            throw new CertificateProviderWrappedErrorRecord(err);
        }
    }

    /// <summary>
    /// Gets the certificate key handle.
    /// </summary>
    /// <param name="cert">The certificate to get the handle for.</param>
    /// <returns>The certificate key handle.</returns>
    private static Interop.Windows.SafeCryptoPrivateKeyHandle GetCertificateKeyHandle(X509Certificate2 cert)
    {
        // Try to enable SeBackupPrivilege which can allow the caller to get
        // the key handle even if the SD doesn't give it explicit rights.
        var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
        bool backupEnabled = PlatformInvokes.EnableTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);
        try
        {
            if (Interop.Windows.CryptAcquireCertificatePrivateKey(
                cert.Handle,
                Interop.Windows.CRYPT_ACQUIRE_SILENT_FLAG | Interop.Windows.CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG,
                nint.Zero,
                out var keyHandle,
                out int _))
            {
                return keyHandle;
            }

            Win32Exception exp = new Win32Exception();

            // NTE_BAD_KEYSET is returned when the user does not have
            // permissions to access the key SD.
            ErrorRecord err;
            if (exp.NativeErrorCode == Interop.Windows.ERROR_ACCESS_DENIED || exp.NativeErrorCode == Interop.Windows.NTE_BAD_KEYSET)
            {
                string errMsg = string.Format(CertificateProviderStrings.GetKeyHandleAuthFailure, exp.Message);
                err = new ErrorRecord(
                    exp,
                    "GetCertificateKeyHandleAccessDenied",
                    ErrorCategory.PermissionDenied,
                    cert)
                {
                    ErrorDetails = new ErrorDetails(errMsg),
                };
            }
            else if (exp.NativeErrorCode == Interop.Windows.CRYPT_E_NO_KEY_PROPERTY)
            {
                string errMsg = string.Format(CertificateProviderStrings.GetKeyHandleMissingFailure, exp.Message);
                err = new ErrorRecord(
                    exp,
                    "GetCertificateKeyHandleNoKey",
                    ErrorCategory.ObjectNotFound,
                    cert)
                {
                    ErrorDetails = new ErrorDetails(errMsg),
                };
            }
            else
            {
                err = new ErrorRecord(
                    exp,
                    "GetCertificateKeyHandleNativeError",
                    ErrorCategory.NotSpecified,
                    cert);
            }

            throw new CertificateProviderWrappedErrorRecord(err);
        }
        finally
        {
            if (backupEnabled)
            {
                PlatformInvokes.RestoreTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);
            }
        }
    }

    /// <summary>
    /// Gets the SD for the provided key.
    /// </summary>
    /// <param name="key">The certificate key handle to get the SD for.</param>
    /// <param name="sections">SD sections to retrieve.</param>
    /// <returns>The CertificateKeySecurity object.</returns>
    /// /// <exception cref="Win32Exception">The Win32 native error on failure.</exception>
    private static CertificateKeySecurity GetKeySecurity(
        Interop.Windows.SafeCryptoPrivateKeyHandle key,
        AccessControlSections sections)
    {
        int sectionsFlag = GetAccessControlSectionsValue(sections);

        GetKeySecurityDescriptorDelegate getFunc = key.IsNCryptKey
            ? GetNCryptKeySecurityDescriptor
            : GetCryptoAPIKeySecurityDescriptor;

        CertificateKeySecurity sd = new();
        int errCode = getFunc(key, sectionsFlag, null, out int sdSize);
        if (errCode == Interop.Windows.ERROR_INSUFFICIENT_BUFFER)
        {
            using var pool = MemoryPool<byte>.Shared.Rent(sdSize);
            Span<byte> descriptorBuffer = pool.Memory.Span;
            errCode = getFunc(key, sectionsFlag, descriptorBuffer, out sdSize);
            sd.SetSecurityDescriptorBinaryForm(descriptorBuffer[..sdSize].ToArray());
        }

        if (errCode != Interop.Windows.ERROR_SUCCESS)
        {
            throw new Win32Exception(errCode);
        }

        return sd;
    }

    /// <summary>
    /// Sets the SD for the provided key.
    /// </summary>
    /// <param name="key">The certificate key handle to set the SD for.</param>
    /// <param name="securityDescriptor">The ObjectSecurity descriptor to set.</param>
    /// <param name="sections">The sections of the SD to set.</param>
    /// <exception cref="Win32Exception">The Win32 native error on failure.</exception>
    private static void SetKeySecurity(
        Interop.Windows.SafeCryptoPrivateKeyHandle key,
        ObjectSecurity securityDescriptor,
        AccessControlSections sections)
    {
        int sectionsFlag = GetAccessControlSectionsValue(sections);
        byte[] descriptorBytes = securityDescriptor.GetSecurityDescriptorBinaryForm();

        /*
        None of the APIs enable these privileges, as the API in PowerShell
        currently cannot adjust multiple privileges we need to do it one at a
        time. We require these privileges for:

            SeRestorePrivilege - Needed to set if we have no WritePermissions
                rights or changing the owner to a SID that is not part of the
                user's groups with SE_GROUP_ENABLED (whoami /groups).
            SeTakeOwnershipPrivilege - Needed to overwrite the owner if we
                do not have WritePermissions.
        */
        var previousRestoreState = new PlatformInvokes.TOKEN_PRIVILEGE();
        bool revertRestore = false;
        var previousOwnershipState = new PlatformInvokes.TOKEN_PRIVILEGE();
        bool revertOwnership = false;
        try
        {
            revertRestore = PlatformInvokes.EnableTokenPrivilege("SeRestorePrivilege", ref previousRestoreState);
            revertOwnership = PlatformInvokes.EnableTokenPrivilege("SeTakeOwnershipPrivilege", ref previousOwnershipState);

            int errCode = key.IsNCryptKey
                        ? SetNCryptKeySecurityDescriptor(key, sectionsFlag, descriptorBytes)
                        : SetCryptoAPIKeySecurityDescriptor(key, sectionsFlag, descriptorBytes);
            if (errCode != Interop.Windows.ERROR_SUCCESS)
            {
                throw new Win32Exception(errCode);
            }
        }
        finally
        {
            if (revertRestore)
            {
                PlatformInvokes.RestoreTokenPrivilege("SeRestorePrivilege", ref previousRestoreState);
            }

            if (revertOwnership)
            {
                PlatformInvokes.RestoreTokenPrivilege("SeTakeOwnershipPrivilege", ref previousOwnershipState);
            }
        }
    }

    private static AccessControlSections GetLimitedSections(
        ObjectSecurity newSD,
        ObjectSecurity existingSD)
    {
        AccessControlSections sections = AccessControlSections.All;
        Type accountType = typeof(SecurityIdentifier);

        SecurityIdentifier? newOwner = (SecurityIdentifier?)newSD.GetOwner(accountType);
        SecurityIdentifier? existingOwner = (SecurityIdentifier?)existingSD.GetOwner(accountType);
        if (newOwner == null || newOwner == existingOwner)
        {
            sections &= ~AccessControlSections.Owner;
        }

        SecurityIdentifier? newGroup = (SecurityIdentifier?)newSD.GetGroup(accountType);
        SecurityIdentifier? existingGroup = (SecurityIdentifier?)existingSD.GetGroup(accountType);
        if (newGroup == null || newGroup == existingGroup)
        {
            sections &= ~AccessControlSections.Group;
        }

        // We cannot distinguish between clearing the audit rules and wanting
        // to skip changing them. To replicate the FileSystem behaviour we
        // unset the Audit section if the rules are empty.
        if (newSD is CertificateKeySecurity certSD && certSD.GetAuditRules(true, true, accountType).Count == 0)
        {
            sections &= ~AccessControlSections.Audit;
        }

        return sections;
    }

    private static int GetAccessControlSectionsValue(AccessControlSections sections)
    {
        int sectionsFlag = 0;
        if (sections.HasFlag(AccessControlSections.Owner))
        {
            sectionsFlag |= OWNER_SECURITY_INFORMATION;
        }

        if (sections.HasFlag(AccessControlSections.Group))
        {
            sectionsFlag |= GROUP_SECURITY_INFORMATION;
        }

        if (sections.HasFlag(AccessControlSections.Access))
        {
            sectionsFlag |= DACL_SECURITY_INFORMATION;
        }

        if (sections.HasFlag(AccessControlSections.Audit))
        {
            sectionsFlag |= SACL_SECURITY_INFORMATION;
        }

        return sectionsFlag;
    }

    private static int GetNCryptKeySecurityDescriptor(
        SafeHandle key,
        int sections,
        Span<byte> data,
        out int dataSize)
    {
        bool checkingSize = data.Length == 0;

        int errCode = Interop.Windows.NCryptGetProperty(
            key,
            Interop.Windows.NCRYPT_SECURITY_DESCR_PROPERTY,
            data,
            data.Length,
            out dataSize,
            sections | Interop.Windows.CRYPT_ACQUIRE_SILENT_FLAG);

        // Treat both Get functions the same as other normal Win32 APIs. I
        // have seen this method set the error to ERROR_SUCCESS as well as
        // ERROR_INSUFFICIENT_BUFFER so this ensures consistent behaviour.
        if (checkingSize && errCode == 0)
        {
            errCode = Interop.Windows.ERROR_INSUFFICIENT_BUFFER;
        }

        return errCode;
    }

    private static int GetCryptoAPIKeySecurityDescriptor(
        SafeHandle key,
        int sections,
        Span<byte> data,
        out int dataSize)
    {
        int dataCount = data.Length;
        bool checkingSize = dataCount == 0;

        // CryptGetProvParam does not automatically enable SeSecurityPrivilege
        // required to get the SACL so we do so ourselves if possible.
        var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
        bool privEnabled = false;
        if ((sections & SACL_SECURITY_INFORMATION) == SACL_SECURITY_INFORMATION)
        {
            privEnabled = PlatformInvokes.EnableTokenPrivilege("SeSecurityPrivilege", ref currentPrivilegeState);
        }

        bool res;
        int errCode;
        try
        {
            res = Interop.Windows.CryptGetProvParam(
                key,
                Interop.Windows.PP_KEYSET_SEC_DESCR,
                data,
                ref dataCount,
                sections);
            errCode = Marshal.GetLastPInvokeError();
        }
        finally
        {
            if (privEnabled)
            {
                PlatformInvokes.RestoreTokenPrivilege("SeSecurityPrivilege", ref currentPrivilegeState);
            }
        }

        /*
        CryptGetProvParam returns true with ERROR_SUCCESS when dataSize was set
        to 0. To replicate the behaviour of NCryptGetProperty and other Win32
        APIs we return ERROR_INSUFFICIENT_BUFFER in that example. The function
        is also buggy in that it calls GetNamedSecurityInfoW but doesn't pass
        back any error codes from that function if it failed. The best we can
        do is when dataSize stays at 0 we change the error code to a failure
        manually. We don't know the reason why but it still fails.

        Examples of when it could happen is if trying to retrieve the SACL but
        the SeSecurityPrivilege isn't held.
        */
        if (checkingSize)
        {
            res = false;

            if (dataCount != 0)
            {
                errCode = Interop.Windows.ERROR_INSUFFICIENT_BUFFER;
            }
            else if (dataCount == 0 && errCode == 0)
            {
                // Shown as 'Unknown error (0xffffffff)' in Win32Exception.
                errCode = -1;
            }
        }

        dataSize = dataCount;
        return res ? Interop.Windows.ERROR_SUCCESS : errCode;
    }

    private static int SetNCryptKeySecurityDescriptor(
        SafeHandle key,
        int sections,
        ReadOnlySpan<byte> data)
    {
        return Interop.Windows.NCryptSetProperty(
            key,
            Interop.Windows.NCRYPT_SECURITY_DESCR_PROPERTY,
            data,
            data.Length,
            sections | Interop.Windows.CRYPT_ACQUIRE_SILENT_FLAG);
    }

    private static int SetCryptoAPIKeySecurityDescriptor(
        SafeHandle key,
        int sections,
        ReadOnlySpan<byte> data)
    {
        // CryptSetProvParam does not automatically enable SeSecurityPrivilege
        // required to set the SACL so we do so ourselves if possible.
        var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
        bool privEnabled = false;
        if ((sections & SACL_SECURITY_INFORMATION) == SACL_SECURITY_INFORMATION)
        {
            privEnabled = PlatformInvokes.EnableTokenPrivilege("SeSecurityPrivilege", ref currentPrivilegeState);
        }

        bool res;
        int errCode;
        try
        {
            res = Interop.Windows.CryptSetProvParam(
                key,
                Interop.Windows.PP_KEYSET_SEC_DESCR,
                data,
                sections);
            errCode = Marshal.GetLastPInvokeError();
        }
        finally
        {
            if (privEnabled)
            {
                PlatformInvokes.RestoreTokenPrivilege("SeSecurityPrivilege", ref currentPrivilegeState);
            }
        }

        return res ? Interop.Windows.ERROR_SUCCESS : errCode;
    }
}

#endif
