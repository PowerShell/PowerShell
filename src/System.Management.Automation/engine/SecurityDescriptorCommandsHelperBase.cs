// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Security;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Internal
{
    /// <summary>
    /// Defines the base class from which all Security Descriptor commands
    /// are derived.
    /// </summary>
    public abstract class SecurityDescriptorCommandsHelperBase : PSCmdlet
    {
        #region brokered properties

        /// <summary>
        /// Add brokered properties for easy access to important properties
        /// of security descriptor.
        /// </summary>
        internal static void AddBrokeredProperties(
            Collection<PSObject> results,
            bool audit,
            bool allCentralAccessPolicies)
        {
            foreach (PSObject result in results)
            {
                if (audit)
                {
                    // Audit
                    result.Properties.Add
                    (
                        new PSCodeProperty
                            (
                                "Audit",
                                typeof(SecurityDescriptorCommandsHelperBase).GetMethod("GetAudit")
                            )
                    );
                }
                // CentralAccessPolicyId retrieval does not require elevation, so we always add this property.
                result.Properties.Add
                (
                    new PSCodeProperty
                        (
                            "CentralAccessPolicyId",
                            typeof(SecurityDescriptorCommandsHelperBase).GetMethod("GetCentralAccessPolicyId")
                        )
                );
#if !CORECLR    // GetAllCentralAccessPolicies and GetCentralAccessPolicyName are not supported in OneCore powershell
                // because function 'LsaQueryCAPs' is not available in OneCoreUAP and NanoServer.
                if (allCentralAccessPolicies)
                {
                    // AllCentralAccessPolicies
                    result.Properties.Add
                    (
                        new PSCodeProperty
                            (
                                "AllCentralAccessPolicies",
                                typeof(SecurityDescriptorCommandsHelperBase).GetMethod("GetAllCentralAccessPolicies")
                            )
                    );
                }
                // CentralAccessPolicyName retrieval does not require elevation, so we always add this property.
                result.Properties.Add
                (
                    new PSCodeProperty
                        (
                            "CentralAccessPolicyName",
                            typeof(SecurityDescriptorCommandsHelperBase).GetMethod("GetCentralAccessPolicyName")
                        )
                );
#endif
            }
        }

        /// <summary>
        /// Gets the Path of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the path.
        /// </param>
        /// <returns>
        /// The path of the provided PSObject.
        /// </returns>
        public static string GetPath(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }
            else
            {
                // These are guaranteed to not be null
                // Get path
                return instance.Properties["PSPath"].Value.ToString()!;
            }
        }

        /// <summary>
        /// Gets the Owner of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the Owner.
        /// </param>
        /// <returns>
        /// The Owner of the provided PSObject.
        /// </returns>
        public static string? GetOwner(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            if (!(instance.BaseObject is ObjectSecurity sd))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            // Get owner
            try
            {
                IdentityReference? ir = sd.GetOwner(typeof(NTAccount));
                return ir?.ToString();
            }
            catch (IdentityNotMappedException)
            {
                // All Acl cmdlets returning SIDs will return a string
                // representation of the SID in all cases where the SID
                // cannot be mapped to a proper user or group name.
            }

            // We are here since we cannot get IdentityReference from sd..
            // So return sddl..
            return sd.GetSecurityDescriptorSddlForm(AccessControlSections.Owner);
        }

        /// <summary>
        /// Gets the Group of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the Group.
        /// </param>
        /// <returns>
        /// The Group of the provided PSObject.
        /// </returns>
        public static string? GetGroup(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            if (!(instance.BaseObject is ObjectSecurity sd))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            // Get Group
            try
            {
                IdentityReference? ir = sd.GetGroup(typeof(NTAccount));
                return ir?.ToString();
            }
            catch (IdentityNotMappedException)
            {
                // All Acl cmdlets returning SIDs will return a string
                // representation of the SID in all cases where the SID
                // cannot be mapped to a proper user or group name.
            }

            // We are here since we cannot get IdentityReference from sd..
            // So return sddl..
            return sd.GetSecurityDescriptorSddlForm(AccessControlSections.Group);
        }
        /// <summary>
        /// Gets the access rules of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the access rules.
        /// </param>
        /// <returns>
        /// The access rules of the provided PSObject.
        /// </returns>
        public static AuthorizationRuleCollection GetAccess(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            ObjectSecurity? sd = instance.BaseObject as ObjectSecurity;
            if (sd == null)
            {
                PSTraceSource.NewArgumentException(nameof(instance));
            }

            // Get DACL
            CommonObjectSecurity? cos = sd as CommonObjectSecurity;
            if (cos != null)
            {
                return cos.GetAccessRules(true, true, typeof(NTAccount));
            }
            else
            {
                DirectoryObjectSecurity? dos = sd as DirectoryObjectSecurity;
                Dbg.Diagnostics.Assert(dos != null, "Acl should be of type CommonObjectSecurity or DirectoryObjectSecurity");
                return dos.GetAccessRules(true, true, typeof(NTAccount));
            }
        }

        /// <summary>
        /// Gets the audit rules of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the audit rules.
        /// </param>
        /// <returns>
        /// The audit rules of the provided PSObject.
        /// </returns>
        public static AuthorizationRuleCollection GetAudit(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            ObjectSecurity? sd = instance.BaseObject as ObjectSecurity;
            if (sd == null)
            {
                PSTraceSource.NewArgumentException(nameof(instance));
            }

            CommonObjectSecurity? cos = sd as CommonObjectSecurity;
            if (cos != null)
            {
                return cos.GetAuditRules(true, true, typeof(NTAccount));
            }
            else
            {
                DirectoryObjectSecurity? dos = sd as DirectoryObjectSecurity;
                Dbg.Diagnostics.Assert(dos != null, "Acl should be of type CommonObjectSecurity or DirectoryObjectSecurity");
                return dos.GetAuditRules(true, true, typeof(NTAccount));
            }
        }

        /// <summary>
        /// Gets the central access policy ID of the provided PSObject.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the central access policy ID.
        /// </param>
        /// <returns>
        /// The central access policy ID of the provided PSObject.
        /// </returns>
        public static SecurityIdentifier? GetCentralAccessPolicyId(PSObject instance)
        {
            SessionState sessionState = new();
            string path = sessionState.Path.GetUnresolvedProviderPathFromPSPath(
                GetPath(instance));
            IntPtr pSd = IntPtr.Zero;

            try
            {
                // Get the file's SACL containing the CAPID ACE.
                uint rs = NativeMethods.GetNamedSecurityInfo(
                    path,
                    NativeMethods.SeObjectType.SE_FILE_OBJECT,
                    NativeMethods.SecurityInformation.SCOPE_SECURITY_INFORMATION,
                    out IntPtr pOwner,
                    out IntPtr pGroup,
                    out IntPtr pDacl,
                    out IntPtr pSacl,
                    out pSd);
                if (rs != NativeMethods.ERROR_SUCCESS)
                {
                    throw new Win32Exception((int)rs);
                }

                if (pSacl == IntPtr.Zero)
                {
                    return null;
                }

                NativeMethods.ACL sacl = Marshal.PtrToStructure<NativeMethods.ACL>(pSacl);
                if (sacl.AceCount == 0)
                {
                    return null;
                }

                // Extract the first CAPID from the SACL that does not have INHERIT_ONLY_ACE flag set.
                IntPtr pAce = pSacl + Marshal.SizeOf(new NativeMethods.ACL());
                for (ushort aceIdx = 0; aceIdx < sacl.AceCount; aceIdx++)
                {
                    NativeMethods.ACE_HEADER ace = Marshal.PtrToStructure<NativeMethods.ACE_HEADER>(pAce);
                    Dbg.Diagnostics.Assert(ace.AceType ==
                        NativeMethods.SYSTEM_SCOPED_POLICY_ID_ACE_TYPE,
                        "Unexpected ACE type: " + ace.AceType.ToString(CultureInfo.CurrentCulture));
                    if ((ace.AceFlags & NativeMethods.INHERIT_ONLY_ACE) == 0)
                    {
                        break;
                    }

                    pAce += ace.AceSize;
                }

                IntPtr pSid = pAce + Marshal.SizeOf(new NativeMethods.SYSTEM_AUDIT_ACE()) -
                    Marshal.SizeOf(new uint());
                bool ret = NativeMethods.IsValidSid(pSid);
                if (!ret)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return new SecurityIdentifier(pSid);
            }
            finally
            {
                NativeMethods.LocalFree(pSd);
            }
        }

#if !CORECLR
        /// <summary>
        /// Gets the central access policy name of the provided PSObject.
        /// </summary>
        /// <remarks>
        /// Function 'LsaQueryCAPs' is not available in OneCoreUAP and NanoServer.
        /// </remarks>
        /// <param name="instance">
        /// The PSObject for which to obtain the central access policy name.
        /// </param>
        /// <returns>
        /// The central access policy name of the provided PSObject.
        /// </returns>
        public static string GetCentralAccessPolicyName(PSObject instance)
        {
            SecurityIdentifier capId = GetCentralAccessPolicyId(instance);
            if (capId == null)
            {
                return null; // file does not have the scope ace
            }

            int capIdSize = capId.BinaryLength;
            byte[] capIdArray = new byte[capIdSize];
            capId.GetBinaryForm(capIdArray, 0);
            IntPtr caps = IntPtr.Zero;
            IntPtr pCapId = Marshal.AllocHGlobal(capIdSize);

            try
            {
                // Retrieve the CAP by CAPID.
                Marshal.Copy(capIdArray, 0, pCapId, capIdSize);
                IntPtr[] ppCapId = new IntPtr[1];
                ppCapId[0] = pCapId;
                uint rs = NativeMethods.LsaQueryCAPs(
                    ppCapId,
                    1,
                    out caps,
                    out uint capCount);
                if (rs != NativeMethods.STATUS_SUCCESS)
                {
                    throw new Win32Exception((int)rs);
                }

                if (capCount == 0 || caps == IntPtr.Zero)
                {
                    return null;
                }

                // Get the CAP name.
                NativeMethods.CENTRAL_ACCESS_POLICY cap = Marshal.PtrToStructure<NativeMethods.CENTRAL_ACCESS_POLICY>(caps);
                // LSA_UNICODE_STRING is composed of WCHARs, but its length is given in bytes.
                return Marshal.PtrToStringUni(cap.Name.Buffer, cap.Name.Length / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(pCapId);
                uint rs = NativeMethods.LsaFreeMemory(caps);
                Dbg.Diagnostics.Assert(rs == NativeMethods.STATUS_SUCCESS,
                    "LsaFreeMemory failed: " + rs.ToString(CultureInfo.CurrentCulture));
            }
        }

        /// <summary>
        /// Gets the names and IDs of all central access policies available on the machine.
        /// </summary>
        /// <remarks>
        /// Function 'LsaQueryCAPs' is not available in OneCoreUAP and NanoServer.
        /// </remarks>
        /// <param name="instance">
        /// The PSObject argument is ignored.
        /// </param>
        /// <returns>
        /// The names and IDs of all central access policies available on the machine.
        /// </returns>
        public static string[] GetAllCentralAccessPolicies(PSObject instance)
        {
            IntPtr caps = IntPtr.Zero;

            try
            {
                // Retrieve all CAPs.
                uint rs = NativeMethods.LsaQueryCAPs(
                    null,
                    0,
                    out caps,
                    out uint capCount);
                if (rs != NativeMethods.STATUS_SUCCESS)
                {
                    throw new Win32Exception((int)rs);
                }

                Dbg.Diagnostics.Assert(capCount < 0xFFFF,
                    "Too many central access policies");
                if (capCount == 0 || caps == IntPtr.Zero)
                {
                    return null;
                }

                // Add CAP names and IDs to a string array.
                string[] policies = new string[capCount];
                IntPtr capPtr = caps;
                for (uint capIdx = 0; capIdx < capCount; capIdx++)
                {
                    // Retrieve CAP name.
                    Dbg.Diagnostics.Assert(capPtr != IntPtr.Zero,
                        "Invalid central access policies array");
                    NativeMethods.CENTRAL_ACCESS_POLICY cap = Marshal.PtrToStructure<NativeMethods.CENTRAL_ACCESS_POLICY>(capPtr);
                    // LSA_UNICODE_STRING is composed of WCHARs, but its length is given in bytes.
                    policies[capIdx] = "\"" + Marshal.PtrToStringUni(
                        cap.Name.Buffer,
                        cap.Name.Length / 2) + "\"";

                    // Retrieve CAPID.
                    IntPtr pCapId = cap.CAPID;
                    Dbg.Diagnostics.Assert(pCapId != IntPtr.Zero,
                        "Invalid central access policies array");
                    bool ret = NativeMethods.IsValidSid(pCapId);
                    if (!ret)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    SecurityIdentifier sid = new SecurityIdentifier(pCapId);
                    policies[capIdx] += " (" + sid.ToString() + ")";

                    capPtr += Marshal.SizeOf(cap);
                }

                return policies;
            }
            finally
            {
                uint rs = NativeMethods.LsaFreeMemory(caps);
                Dbg.Diagnostics.Assert(rs == NativeMethods.STATUS_SUCCESS,
                    "LsaFreeMemory failed: " + rs.ToString(CultureInfo.CurrentCulture));
            }
        }
#endif

        /// <summary>
        /// Gets the security descriptor (in SDDL form) of the
        /// provided PSObject.  SDDL form is the Security Descriptor
        /// Definition Language.
        /// </summary>
        /// <param name="instance">
        /// The PSObject for which to obtain the security descriptor.
        /// </param>
        /// <returns>
        /// The security descriptor of the provided PSObject, in SDDL form.
        /// </returns>
        public static string GetSddl(PSObject instance)
        {
            if (instance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            if (!(instance.BaseObject is ObjectSecurity sd))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(instance));
            }

            string sddl = sd.GetSecurityDescriptorSddlForm(AccessControlSections.All);
            return sddl;
        }

        #endregion brokered properties
    }
}
