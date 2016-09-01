/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Management.Automation.Tracing;
    using System.Security.AccessControl;
    using System.Security.Principal;

    /// <summary>
    /// This class implements the encrypt and decrypt functionality.
    /// </summary>
    internal class InstanceStorePermission
    {
        internal static void SetDirectoryPermissions(string folderName)
        {
            string account = WindowsIdentity.GetCurrent().Name;
            RemoveInheritablePermissions(folderName);
            AddDirectorySecurity(folderName, account, FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
        }

        private static void AddDirectorySecurity(string folderName, string account, FileSystemRights rights, InheritanceFlags inheritance, PropagationFlags propagation, AccessControlType controlType) 
        { 
            DirectoryInfo info = new DirectoryInfo(folderName); 
            DirectorySecurity dSecurity = info.GetAccessControl(); 
            dSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, inheritance, propagation, controlType)); 
            info.SetAccessControl(dSecurity); 
        }

        private static void RemoveInheritablePermissions(string folderName)
        {
            DirectoryInfo info = new DirectoryInfo(folderName);
            DirectorySecurity dSecurity = info.GetAccessControl();
            const bool IsProtected = true;
            const bool PreserveInheritance = false;
            dSecurity.SetAccessRuleProtection(IsProtected, PreserveInheritance);
            info.SetAccessControl(dSecurity);
        }
    }
}
