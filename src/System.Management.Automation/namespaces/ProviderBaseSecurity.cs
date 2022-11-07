// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.AccessControl;

namespace System.Management.Automation.Provider
{
    /// <summary>
    /// Defines the base class for all of the classes the provide implementations for a particular
    /// data store or item for the PowerShell core commands.
    /// </summary>
    public abstract partial class CmdletProvider
    {
        #region ISecurityDescriptorCmdletProvider method wrappers

        /// <summary>
        /// Internal wrapper for the GetSecurityDescriptor protected method. This method will
        /// only be called if the provider implements the ISecurityDescriptorCmdletProvider interface.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve the security descriptor from.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing. An instance of an object that represents the security descriptor
        /// for the item specified by the path should be written to the context.
        /// </returns>
        internal void GetSecurityDescriptor(
            string path,
            AccessControlSections sections,
            CmdletProviderContext context)
        {
            Context = context;

            ISecurityDescriptorCmdletProvider permissionProvider = this as ISecurityDescriptorCmdletProvider;

            //
            // if this is not supported, the fn will throw
            //
            CheckIfSecurityDescriptorInterfaceIsSupported(permissionProvider);

            // Call interface method

            permissionProvider.GetSecurityDescriptor(path, sections);
        }

        /// <summary>
        /// Internal wrapper for the SetSecurityDescriptor protected method. This method will
        /// only be called if the provider implements the ISecurityDescriptorCmdletProvider interface.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the new security descriptor on.
        /// </param>
        /// <param name="securityDescriptor">
        /// The new security descriptor for the item.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor object that was set should be written
        /// to the context.
        /// </returns>
        internal void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor,
            CmdletProviderContext context)
        {
            Context = context;

            ISecurityDescriptorCmdletProvider permissionProvider = this as ISecurityDescriptorCmdletProvider;

            //
            // if this is not supported, the fn will throw
            //
            CheckIfSecurityDescriptorInterfaceIsSupported(permissionProvider);

            // Call interface method

            permissionProvider.SetSecurityDescriptor(path, securityDescriptor);
        }

        private static void CheckIfSecurityDescriptorInterfaceIsSupported(ISecurityDescriptorCmdletProvider permissionProvider)
        {
            if (permissionProvider == null)
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        ProviderBaseSecurity.ISecurityDescriptorCmdletProvider_NotSupported);
            }
        }
        #endregion ISecurityDescriptorCmdletProvider method wrappers
    }
}
