// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\System\Security\AccessControl\RegistrySecurity.cs.
// Namespace: System.Security.AccessControl
//
/*============================================================
**
**
**
** Purpose: Managed ACL wrapper for registry keys.
**
**
===========================================================*/

using System;
using System.Security.Permissions;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.Commands.Internal
{
    /// <summary>
    /// <para>Represents a set of access rights allowed or denied for a user or group. This class cannot be inherited.</para>
    /// </summary>
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistryAccessRule : AccessRule
    {
        // Constructor for creating access rules for registry objects

        /// <summary>
        /// <para>Initializes a new instance of the RegistryAccessRule class, specifying the user or group the rule applies to,
        /// the access rights, and whether the specified access rights are allowed or denied.</para>
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        /// </summary>
        internal TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, AccessControlType type)
            : this(identity, (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the RegistryAccessRule class, specifying the user or group the rule applies to,
        /// the access rights, and whether the specified access rights are allowed or denied.</para>
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        /// </summary>
        internal TransactedRegistryAccessRule(String identity, RegistryRights registryRights, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, InheritanceFlags.None, PropagationFlags.None, type)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the RegistryAccessRule class, specifying the user or group the rule applies to,
        /// the access rights, and whether the specified access rights are allowed or denied.</para>
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags flags specifying how access rights are inherited from other objects.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags flags specifying how access rights are propagated to other objects.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        /// </summary>
        public TransactedRegistryAccessRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the RegistryAccessRule class, specifying the user or group the rule applies to,
        /// the access rights, and whether the specified access rights are allowed or denied.</para>
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of Microsoft.Win32.RegistryRights values indicating the rights allowed or denied.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags flags specifying how access rights are inherited from other objects.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags flags specifying how access rights are propagated to other objects.</param>
        /// <param name="type">One of the AccessControlType values indicating whether the rights are allowed or denied.</param>
        /// </summary>
        internal TransactedRegistryAccessRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, type)
        {
        }

        //
        // Internal constructor to be called by public constructors
        // and the access rule factory methods of {File|Folder}Security
        //
        internal TransactedRegistryAccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : base(
                identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                type)
        {
        }

        /// <summary>
        /// <para>Gets the rights allowed or denied by the access rule.</para>
        /// </summary>
        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)base.AccessMask; }
        }
    }

    /// <summary>
    /// <para>Represents a set of access rights to be audited for a user or group. This class cannot be inherited.</para>
    /// </summary>
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistryAuditRule : AuditRule
    {
        /// <summary>
        /// <para>Initializes a new instance of the RegistryAuditRule class, specifying the user or group to audit, the rights to
        /// audit, whether to take inheritance into account, and whether to audit success, failure, or both.</para>
        /// <param name="identity">The user or group the rule applies to. Must be of type SecurityIdentifier or a type such as
        /// NTAccount that can be converted to type SecurityIdentifier.</param>
        /// <param name="registryRights">A bitwise combination of RegistryRights values specifying the kinds of access to audit.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying whether the audit rule applies to subkeys of the current key.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that affect the way an inherited audit rule is propagated to subkeys of the current key.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit success, failure, or both.</param>
        /// </summary>
        internal TransactedRegistryAuditRule(IdentityReference identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(identity, (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the RegistryAuditRule class, specifying the user or group to audit, the rights to
        /// audit, whether to take inheritance into account, and whether to audit success, failure, or both.</para>
        /// <param name="identity">The name of the user or group the rule applies to.</param>
        /// <param name="registryRights">A bitwise combination of RegistryRights values specifying the kinds of access to audit.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying whether the audit rule applies to subkeys of the current key.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that affect the way an inherited audit rule is propagated to subkeys of the current key.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit success, failure, or both.</param>
        /// </summary>
        internal TransactedRegistryAuditRule(string identity, RegistryRights registryRights, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : this(new NTAccount(identity), (int)registryRights, false, inheritanceFlags, propagationFlags, flags)
        {
        }

        internal TransactedRegistryAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, flags)
        {
        }

        /// <summary>
        /// <para>Gets the access rights affected by the audit rule.</para>
        /// </summary>
        public RegistryRights RegistryRights
        {
            get { return (RegistryRights)base.AccessMask; }
        }
    }

    /// <summary>
    /// <para>Represents the Windows access control security for a registry key. This class cannot be inherited.
    /// This class is specifically to be used with TransactedRegistryKey.</para>
    /// </summary>
    // Suppressed because these are needed to manipulate TransactedRegistryKey, which is written to the pipeline.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistrySecurity : NativeObjectSecurity
    {
        private const string resBaseName = "RegistryProviderStrings";

        /// <summary>
        /// <para>Initializes a new instance of the TransactedRegistrySecurity class with default values.</para>
        /// </summary>
        public TransactedRegistrySecurity()
            : base(true, ResourceType.RegistryKey)
        {
        }

        /*
        // The name of registry key must start with a predefined string,
        // like CLASSES_ROOT, CURRENT_USER, MACHINE, and USERS.  See
        // MSDN's help for SetNamedSecurityInfo for details.
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode=true)]
        internal TransactedRegistrySecurity(String name, AccessControlSections includeSections)
            : base(true, ResourceType.RegistryKey, HKeyNameToWindowsName(name), includeSections)
        {
            new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.View, name).Demand();
        }
        */

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        // Suppressed because the passed name and hkey won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        internal TransactedRegistrySecurity(SafeRegistryHandle hKey, string name, AccessControlSections includeSections)
            : base(true, ResourceType.RegistryKey, hKey, includeSections, _HandleErrorCode, null)
        {
            new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.View, name).Demand();
        }

        private static Exception _HandleErrorCode(int errorCode, string name, SafeHandle handle, object context)
        {
            System.Exception exception = null;

            switch (errorCode)
            {
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    exception = new IOException(RegistryProviderStrings.Arg_RegKeyNotFound);
                    break;

                case Win32Native.ERROR_INVALID_NAME:
                    exception = new ArgumentException(RegistryProviderStrings.Arg_RegInvalidKeyName);
                    break;

                case Win32Native.ERROR_INVALID_HANDLE:
                    exception = new ArgumentException(RegistryProviderStrings.AccessControl_InvalidHandle);
                    break;

                default:
                    break;
            }

            return exception;
        }

        /// <summary>
        /// <para>Creates a new access control rule for the specified user, with the specified access rights, access control, and flags.</para>
        /// <param name="identityReference">An IdentityReference that identifies the user or group the rule applies to.</param>
        /// <param name="accessMask">A bitwise combination of RegistryRights values specifying the access rights to allow or deny, cast to an integer.</param>
        /// <param name="isInherited">A Boolean value specifying whether the rule is inherited.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying how the rule is inherited by subkeys.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that modify the way the rule is inherited by subkeys. Meaningless if the value of inheritanceFlags is InheritanceFlags.None.</param>
        /// <param name="type">One of the AccessControlType values specifying whether the rights are allowed or denied.</param>
        /// <returns>A TransactedRegistryAccessRule object representing the specified rights for the specified user.</returns>
        /// </summary>
        public override AccessRule AccessRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AccessControlType type)
        {
            return new TransactedRegistryAccessRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, type);
        }

        /// <summary>
        /// <para>Creates a new audit rule, specifying the user the rule applies to, the access rights to audit, the inheritance and propagation of the
        /// rule, and the outcome that triggers the rule.</para>
        /// <param name="identityReference">An IdentityReference that identifies the user or group the rule applies to.</param>
        /// <param name="accessMask">A bitwise combination of RegistryRights values specifying the access rights to audit, cast to an integer.</param>
        /// <param name="isInherited">A Boolean value specifying whether the rule is inherited.</param>
        /// <param name="inheritanceFlags">A bitwise combination of InheritanceFlags values specifying how the rule is inherited by subkeys.</param>
        /// <param name="propagationFlags">A bitwise combination of PropagationFlags values that modify the way the rule is inherited by subkeys. Meaningless if the value of inheritanceFlags is InheritanceFlags.None.</param>
        /// <param name="flags">A bitwise combination of AuditFlags values specifying whether to audit successful access, failed access, or both.</param>
        /// <returns>A TransactedRegistryAuditRule object representing the specified audit rule for the specified user, with the specified flags.
        /// The return type of the method is the base class, AuditRule, but the return value can be cast safely to the derived class.</returns>
        /// </summary>
        public override AuditRule AuditRuleFactory(IdentityReference identityReference, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, AuditFlags flags)
        {
            return new TransactedRegistryAuditRule(identityReference, accessMask, isInherited, inheritanceFlags, propagationFlags, flags);
        }

        internal AccessControlSections GetAccessControlSectionsFromChanges()
        {
            AccessControlSections persistRules = AccessControlSections.None;
            if (AccessRulesModified)
                persistRules = AccessControlSections.Access;
            if (AuditRulesModified)
                persistRules |= AccessControlSections.Audit;
            if (OwnerModified)
                persistRules |= AccessControlSections.Owner;
            if (GroupModified)
                persistRules |= AccessControlSections.Group;
            return persistRules;
        }

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        // Suppressed because the passed keyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        internal void Persist(SafeRegistryHandle hKey, string keyName)
        {
            new RegistryPermission(RegistryPermissionAccess.NoAccess, AccessControlActions.Change, keyName).Demand();

            WriteLock();

            try
            {
                AccessControlSections persistRules = GetAccessControlSectionsFromChanges();
                if (persistRules == AccessControlSections.None)
                    return;  // Don't need to persist anything.

                base.Persist(hKey, persistRules);
                OwnerModified = GroupModified = AuditRulesModified = AccessRulesModified = false;
            }
            finally
            {
                WriteUnlock();
            }
        }

        /// <summary>
        /// <para>Searches for a matching access control with which the new rule can be merged. If none are found, adds the new rule.</para>
        /// <param name="rule">The access control rule to add.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAccessRule(TransactedRegistryAccessRule rule)
        {
            base.AddAccessRule(rule);
        }

        /// <summary>
        /// <para>Removes all access control rules with the same user and AccessControlType (allow or deny) as the specified rule, and then adds the specified rule.</para>
        /// <param name="rule">The TransactedRegistryAccessRule to add. The user and AccessControlType of this rule determine the rules to remove before this rule is added.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.SetAccessRule(rule);
        }

        /// <summary>
        /// <para>Removes all access control rules with the same user as the specified rule, regardless of AccessControlType, and then adds the specified rule.</para>
        /// <param name="rule">The TransactedRegistryAccessRule to add. The user specified by this rule determines the rules to remove before this rule is added.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void ResetAccessRule(TransactedRegistryAccessRule rule)
        {
            base.ResetAccessRule(rule);
        }

        /// <summary>
        /// <para>Searches for an access control rule with the same user and AccessControlType (allow or deny) as the specified access rule, and with compatible
        /// inheritance and propagation flags; if such a rule is found, the rights contained in the specified access rule are removed from it.</para>
        /// <param name="rule">A TransactedRegistryAccessRule that specifies the user and AccessControlType to search for, and a set of inheritance
        /// and propagation flags that a matching rule, if found, must be compatible with. Specifies the rights to remove from the compatible rule, if found.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAccessRule(TransactedRegistryAccessRule rule)
        {
            return base.RemoveAccessRule(rule);
        }

        /// <summary>
        /// <para>Searches for all access control rules with the same user and AccessControlType (allow or deny) as the specified rule and, if found, removes them.</para>
        /// <param name="rule">A TransactedRegistryAccessRule that specifies the user and AccessControlType to search for. Any rights, inheritance flags, or
        /// propagation flags specified by this rule are ignored.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleAll(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleAll(rule);
        }

        /// <summary>
        /// <para>Searches for an access control rule that exactly matches the specified rule and, if found, removes it.</para>
        /// <param name="rule">The TransactedRegistryAccessRule to remove.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAccessRuleSpecific(TransactedRegistryAccessRule rule)
        {
            base.RemoveAccessRuleSpecific(rule);
        }

        /// <summary>
        /// <para>Searches for an audit rule with which the new rule can be merged. If none are found, adds the new rule.</para>
        /// <param name="rule">The audit rule to add. The user specified by this rule determines the search.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void AddAuditRule(TransactedRegistryAuditRule rule)
        {
            base.AddAuditRule(rule);
        }

        /// <summary>
        /// <para>Removes all audit rules with the same user as the specified rule, regardless of the AuditFlags value, and then adds the specified rule.</para>
        /// <param name="rule">The TransactedRegistryAuditRule to add. The user specified by this rule determines the rules to remove before this rule is added.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void SetAuditRule(TransactedRegistryAuditRule rule)
        {
            base.SetAuditRule(rule);
        }

        /// <summary>
        /// <para>Searches for an audit control rule with the same user as the specified rule, and with compatible inheritance and propagation flags;
        /// if a compatible rule is found, the rights contained in the specified rule are removed from it.</para>
        /// <param name="rule">A TransactedRegistryAuditRule that specifies the user to search for, and a set of inheritance and propagation flags that
        /// a matching rule, if found, must be compatible with. Specifies the rights to remove from the compatible rule, if found.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool RemoveAuditRule(TransactedRegistryAuditRule rule)
        {
            return base.RemoveAuditRule(rule);
        }

        /// <summary>
        /// <para>Searches for all audit rules with the same user as the specified rule and, if found, removes them.</para>
        /// <param name="rule">A TransactedRegistryAuditRule that specifies the user to search for. Any rights, inheritance
        /// flags, or propagation flags specified by this rule are ignored.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleAll(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleAll(rule);
        }

        /// <summary>
        /// <para>Searches for an audit rule that exactly matches the specified rule and, if found, removes it.</para>
        /// <param name="rule">The TransactedRegistryAuditRule to be removed.</param>
        /// </summary>
        // Suppressed because we want to ensure TransactedRegistry* objects.
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void RemoveAuditRuleSpecific(TransactedRegistryAuditRule rule)
        {
            base.RemoveAuditRuleSpecific(rule);
        }

        /// <summary>
        /// <para>Gets the enumeration type that the TransactedRegistrySecurity class uses to represent access rights.</para>
        /// <returns>A Type object representing the RegistryRights enumeration.</returns>
        /// </summary>
        public override Type AccessRightType
        {
            get { return typeof(RegistryRights); }
        }

        /// <summary>
        /// <para>Gets the type that the TransactedRegistrySecurity class uses to represent access rules.</para>
        /// <returns>A Type object representing the TransactedRegistryAccessRule class.</returns>
        /// </summary>
        public override Type AccessRuleType
        {
            get { return typeof(TransactedRegistryAccessRule); }
        }

        /// <summary>
        /// <para>Gets the type that the TransactedRegistrySecurity class uses to represent audit rules.</para>
        /// <returns>A Type object representing the TransactedRegistryAuditRule class.</returns>
        /// </summary>
        public override Type AuditRuleType
        {
            get { return typeof(TransactedRegistryAuditRule); }
        }
    }
}
