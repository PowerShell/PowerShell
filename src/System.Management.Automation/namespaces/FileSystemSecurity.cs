// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using System.Security.AccessControl;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The FileSystemProvider provides stateless namespace navigation
    /// of the file system.
    /// </summary>
    public sealed partial class FileSystemProvider : NavigationCmdletProvider, IContentCmdletProvider, IPropertyCmdletProvider, ISecurityDescriptorCmdletProvider
    {
        #region ISecurityDescriptorCmdletProvider members

        /// <summary>
        /// Gets the SecurityDescriptor at the specified path, including only the specified
        /// AccessControlSections.
        /// </summary>
        /// <param name="path">
        /// The path of the item to retrieve. It may be a drive or provider-qualified path and may include.
        /// glob characters.
        /// </param>
        /// <param name="sections">
        /// The sections of the security descriptor to include.
        /// </param>
        /// <returns>
        /// Nothing. An object that represents the security descriptor for the item
        /// specified by path is written to the context's pipeline.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        ///     path doesn't exist
        ///     sections is not valid.
        /// </exception>
        public void GetSecurityDescriptor(string path,
                                          AccessControlSections sections)
        {
            ObjectSecurity sd = null;
            path = NormalizePath(path);

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if ((sections & ~AccessControlSections.All) != 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(sections));
            }

            var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
            try
            {
                PlatformInvokes.EnableTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);

                if (Directory.Exists(path))
                {
                    sd = new DirectorySecurity(path, sections);
                }
                else
                {
                    sd = new FileSecurity(path, sections);
                }
            }
            catch (System.Security.SecurityException e)
            {
                WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
            }
            finally
            {
                PlatformInvokes.RestoreTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);
            }

            WriteSecurityDescriptorObject(sd, path);
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
        /// <exception cref="System.ArgumentException">
        ///     path is null or empty.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        ///     securitydescriptor is null.
        /// </exception>
        public void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            path = NormalizePath(path);

            if (securityDescriptor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                ThrowTerminatingError(CreateErrorRecord(path,
                                                        "SetSecurityDescriptor_FileNotFound"));
            }

            FileSystemSecurity sd = securityDescriptor as FileSystemSecurity;

            if (sd == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(securityDescriptor));
            }
            else
            {
                // This algorithm works around the following security descriptor complexities:
                //
                //     - In order to copy an ACL between files, you need to use the
                //       binary form, and transfer all sections. If you don't use the binary form,
                //       then the FileSystem only applies changes that have happened to that specific
                //       ACL object -- which will not be present if you are just stamping a specific
                //       ACL on a lot of files.
                //     - Copying a full ACL means copying its Audit section, which normal users
                //       don't have access to.
                //
                // In order to make this cmdlet support regular users modifying their own files,
                // the solution is to:
                //
                //     - First attempt to copy the entire security descriptor as we did in V1.
                //       This ensures backward compatibility for administrator scripts that currently
                //       work.
                //     - If the attempt fails due to a PrivilegeNotHeld exception, try again with
                //       an estimate of the minimum required subset. This is an estimate, since the
                //       ACL object doesn't tell you exactly what's changed.
                //           - If their ACL doesn't include any audit rules, don't try to set the
                //             audit section. If it does contain Audit rules, continue to try and
                //             set the section, so they get an appropriate error message.
                //           - If their ACL has the same Owner / Group as the destination file,
                //             also don't try to set those sections.
                //       If they added audit rules, or made changes to the Owner / Group, they will
                //       still get an error message.
                //
                // We can't roll the two steps into one, as the second step can't handle the
                // situation where an admin wants to _clear_ the audit entries. It would be nice to
                // detect a difference in audit entries (like we do with Owner and Group,) but
                // retrieving the Audit entries requires SeSecurityPrivilege as well.

                try
                {
                    // Try to set the entire security descriptor
                    SetSecurityDescriptor(path, sd, AccessControlSections.All);
                }
                catch (PrivilegeNotHeldException)
                {
                    // Get the security descriptor of the destination path
                    ObjectSecurity existingDescriptor = new FileInfo(path).GetAccessControl();
                    // Use SecurityIdentifier to avoid having the below comparison steps
                    // fail when dealing with an untranslatable SID in the SD
                    Type identityType = typeof(System.Security.Principal.SecurityIdentifier);

                    AccessControlSections sections = AccessControlSections.All;

                    // If they didn't modify any audit information, don't try to set
                    // the audit section.
                    int auditRuleCount = sd.GetAuditRules(true, true, identityType).Count;
                    if ((auditRuleCount == 0) &&
                        (sd.AreAuditRulesProtected == existingDescriptor.AreAccessRulesProtected))
                    {
                        sections &= ~AccessControlSections.Audit;
                    }

                    // If they didn't modify the owner, don't try to set that section.
                    if (sd.GetOwner(identityType) == existingDescriptor.GetOwner(identityType))
                    {
                        sections &= ~AccessControlSections.Owner;
                    }

                    // If they didn't modify the group, don't try to set that section.
                    if (sd.GetGroup(identityType) == existingDescriptor.GetGroup(identityType))
                    {
                        sections &= ~AccessControlSections.Group;
                    }

                    // Try to set the security descriptor again, this time with a reduced set
                    // of sections.
                    SetSecurityDescriptor(path, sd, sections);
                }
            }
        }

        private void SetSecurityDescriptor(string path, ObjectSecurity sd, AccessControlSections sections)
        {
            var currentPrivilegeState = new PlatformInvokes.TOKEN_PRIVILEGE();
            byte[] securityDescriptorBinary = null;

            try
            {
                // Get the binary form of the descriptor.
                PlatformInvokes.EnableTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);
                securityDescriptorBinary = sd.GetSecurityDescriptorBinaryForm();
            }
            finally
            {
                PlatformInvokes.RestoreTokenPrivilege("SeBackupPrivilege", ref currentPrivilegeState);
            }

            try
            {
                PlatformInvokes.EnableTokenPrivilege("SeRestorePrivilege", ref currentPrivilegeState);

                // Transfer it to the new file / directory.
                // We keep these two code branches so that we can have more
                // granular information when we output the object type via
                // WriteSecurityDescriptorObject.
                if (Directory.Exists(path))
                {
                    DirectorySecurity newDescriptor = new DirectorySecurity();
                    newDescriptor.SetSecurityDescriptorBinaryForm(securityDescriptorBinary, sections);
                    new DirectoryInfo(path).SetAccessControl(newDescriptor);
                    WriteSecurityDescriptorObject(newDescriptor, path);
                }
                else
                {
                    FileSecurity newDescriptor = new FileSecurity();
                    newDescriptor.SetSecurityDescriptorBinaryForm(securityDescriptorBinary, sections);
                    new FileInfo(path).SetAccessControl(newDescriptor);
                    WriteSecurityDescriptorObject(newDescriptor, path);
                }
            }
            finally
            {
                PlatformInvokes.RestoreTokenPrivilege("SeRestorePrivilege", ref currentPrivilegeState);
            }
        }

        /// <summary>
        /// Creates a new empty security descriptor of the same type as
        /// the item specified by the path.  If "path" points to a file system directory,
        /// then the descriptor returned will be of type DirectorySecurity.
        /// </summary>
        /// <param name="path">
        /// Path of the item to use to determine the type of resulting
        /// SecurityDescriptor.
        /// </param>
        /// <param name="sections">
        /// The sections of the security descriptor to create.
        /// </param>
        /// <returns>
        /// A new ObjectSecurity object of the same type as
        /// the item specified by the path.
        /// </returns>
        public ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections sections)
        {
            ItemType itemType = ItemType.Unknown;

            if (IsItemContainer(path))
            {
                itemType = ItemType.Directory;
            }
            else
            {
                itemType = ItemType.File;
            }

            return NewSecurityDescriptor(itemType);
        }

        /// <summary>
        /// Creates a new empty security descriptor of the specified type.
        /// </summary>
        /// <param name="type">
        /// The type of Security Descriptor to create. Valid types are
        /// "file", "directory," and "container."
        /// </param>
        /// <param name="sections">
        /// The sections of the security descriptor to create.
        /// </param>
        /// <returns>
        /// A new ObjectSecurity object of the specified type.
        /// </returns>
        public ObjectSecurity NewSecurityDescriptorOfType(
            string type,
            AccessControlSections sections)
        {
            ItemType itemType = ItemType.Unknown;

            itemType = GetItemType(type);
            return NewSecurityDescriptor(itemType);
        }

        private static ObjectSecurity NewSecurityDescriptor(
            ItemType itemType)
        {
            ObjectSecurity sd = null;

            switch (itemType)
            {
                case ItemType.File:
                    sd = new FileSecurity();
                    break;

                case ItemType.Directory:
                    sd = new DirectorySecurity();
                    break;
            }

            return sd;
        }

        private static ErrorRecord CreateErrorRecord(string path,
                                                     string errorId)
        {
            string message = null;

            message = StringUtil.Format(FileSystemProviderStrings.FileNotFound, path);

            ErrorRecord er =
                new ErrorRecord(new FileNotFoundException(message),
                                errorId,
                                ErrorCategory.ObjectNotFound,
                                null);

            return er;
        }

        #endregion ISecurityDescriptorCmdletProvider members
    }
}
