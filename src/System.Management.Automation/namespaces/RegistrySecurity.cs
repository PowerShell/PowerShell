// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security.AccessControl;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Provider that provides access to Registry through cmdlets. This provider
    /// implements <see cref="System.Management.Automation.Provider.NavigationCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.IPropertyCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.IDynamicPropertyCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider"/>
    /// interfaces.
    /// </summary>
    public sealed partial class RegistryProvider :
        NavigationCmdletProvider,
        IPropertyCmdletProvider,
        IDynamicPropertyCmdletProvider,
        ISecurityDescriptorCmdletProvider
    {
        #region ISecurityDescriptorCmdletProvider members

        /// <summary>
        /// Gets the security descriptor for the item specified by <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <returns>
        /// Nothing. An object that represents the security descriptor for the item
        /// specified by path is written to the WriteSecurityDescriptorObject method.
        /// </returns>
        public void GetSecurityDescriptor(string path,
                                          AccessControlSections sections)
        {
            ObjectSecurity sd = null;
            IRegistryWrapper key = null;

            // Validate input first.
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if ((sections & ~AccessControlSections.All) != 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(sections));
            }

            path = NormalizePath(path);

            key = GetRegkeyForPathWriteIfError(path, false);

            if (key != null)
            {
                try
                {
                    sd = key.GetAccessControl(sections);
                }
                catch (System.Security.SecurityException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }

                WriteSecurityDescriptorObject(sd, path);
            }
        }

        /// <summary>
        /// Sets the security descriptor for the item specified by <paramref name="path"/>
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the security descriptor on.
        /// </param>
        /// <param name="securityDescriptor">
        /// The new security descriptor for the item.
        /// </param>
        public void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor)
        {
            IRegistryWrapper key = null;

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (securityDescriptor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
            }

            path = NormalizePath(path);

            ObjectSecurity sd;
            if (TransactionAvailable())
            {
                sd = securityDescriptor as TransactedRegistrySecurity;

                if (sd == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(securityDescriptor));
                }
            }
            else
            {
                sd = securityDescriptor as RegistrySecurity;

                if (sd == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(securityDescriptor));
                }
            }

            key = GetRegkeyForPathWriteIfError(path, true);

            if (key != null)
            {
                //
                // the caller already checks for the following exceptions:
                // -- UnauthorizedAccessException
                // -- PrivilegeNotHeldException
                // -- NotSupportedException
                // -- SystemException
                //
                try
                {
                    key.SetAccessControl(sd);
                }
                catch (System.Security.SecurityException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }
                catch (System.UnauthorizedAccessException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }

                WriteSecurityDescriptorObject(sd, path);
            }
        }

        /// <summary>
        /// Creates a new empty security descriptor.
        /// </summary>
        /// <param name="path">
        /// The path to the item whose type is to be used when
        /// creating a new descriptor.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to create.
        /// </param>
        /// <returns>
        /// An instance of <see cref="System.Security.AccessControl.ObjectSecurity"/> object.
        /// </returns>
        /// <remarks><paramref name="path"/> and <paramref name="sections"/> are not used by this method.</remarks>
        public ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections sections)
        {
            if (TransactionAvailable())
            {
                return new TransactedRegistrySecurity();
            }
            else
            {
                return new RegistrySecurity(); // sections);
            }
        }

        /// <summary>
        /// Creates a new empty security descriptor.
        /// </summary>
        /// <param name="type">
        /// The type of item associated with this security descriptor
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to create.
        /// </param>
        /// <returns>
        /// An instance of <see cref="System.Security.AccessControl.ObjectSecurity"/> object.
        /// </returns>
        public ObjectSecurity NewSecurityDescriptorOfType(
            string type,
            AccessControlSections sections)
        {
            if (TransactionAvailable())
            {
                return new TransactedRegistrySecurity();
            }
            else
            {
                return new RegistrySecurity(); // sections);
            }
        }

        #endregion ISecurityDescriptorCmdletProvider members
    }
}
#endif // !UNIX
