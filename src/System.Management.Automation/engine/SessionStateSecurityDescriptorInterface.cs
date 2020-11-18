// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Provider;
using System.Security.AccessControl;

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region private methods

        /// <summary>
        /// Gets an instance of an ISecurityDescriptorCmdletProvider given the provider ID.
        /// </summary>
        /// <param name="providerInstance">
        /// An instance of a CmdletProvider.
        /// </param>
        /// <returns>
        /// An instance of a ISecurityDescriptorCmdletProvider for the specified provider ID.
        /// </returns>
        /// <throws>
        /// ArgumentNullException if providerId is null.
        /// NotSupportedException if the providerId is not for a provider
        /// that is derived from ISecurityDescriptorCmdletProvider.
        /// </throws>
        internal static ISecurityDescriptorCmdletProvider GetPermissionProviderInstance(CmdletProvider providerInstance)
        {
            if (providerInstance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInstance));
            }

            if (!(providerInstance is ISecurityDescriptorCmdletProvider permissionCmdletProvider))
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        ProviderBaseSecurity.ISecurityDescriptorCmdletProvider_NotSupported);
            }

            return permissionCmdletProvider;
        }

        #endregion private methods

        #region GetSecurityDescriptor

        /// <summary>
        /// Gets the security descriptor from the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve the security descriptor from.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <returns>
        /// The security descriptor for the item at the specified path.
        /// </returns>
        internal Collection<PSObject> GetSecurityDescriptor(string path,
                                                             AccessControlSections sections)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            GetSecurityDescriptor(path, sections, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> contextResults = context.GetAccumulatedObjects() ?? new Collection<PSObject>();

            return contextResults;
        }

        /// <summary>
        /// Gets the security descriptor from the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve the security descriptor from.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor for the item at the specified path is
        /// written to the context.
        /// </returns>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void GetSecurityDescriptor(
            string path,
            AccessControlSections sections,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            ProviderInfo provider = null;

            CmdletProvider providerInstance = null;
            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    false,
                    context,
                    out provider,
                    out providerInstance);

            foreach (string providerPath in providerPaths)
            {
                GetSecurityDescriptor(providerInstance, providerPath, sections, context);
            }
        }

        private void GetSecurityDescriptor(
            CmdletProvider providerInstance,
            string path,
            AccessControlSections sections,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            // This just verifies that the provider supports the interface.

            GetPermissionProviderInstance(providerInstance);

            try
            {
                providerInstance.GetSecurityDescriptor(path, sections, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "GetSecurityDescriptorProviderException",
                    SessionStateStrings.GetSecurityDescriptorProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        #endregion GetSecurityDescriptor

        #region SetSecurityDescriptor

        /// <summary>
        /// Sets the security descriptor on the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the security descriptor on.
        /// </param>
        /// <param name="securityDescriptor">
        /// The security descriptor to set on the item at the specified path.
        /// </param>
        /// <returns>
        /// The security descriptor that was set on the item at the specified path.
        /// </returns>
        internal Collection<PSObject> SetSecurityDescriptor(string path, ObjectSecurity securityDescriptor)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (securityDescriptor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            SetSecurityDescriptor(path, securityDescriptor, context);

            context.ThrowFirstErrorOrDoNothing();

            // Return an empty array instead of null
            Collection<PSObject> contextResults = context.GetAccumulatedObjects() ?? new Collection<PSObject>();

            return contextResults;
        }

        /// <summary>
        /// Sets the security descriptor on the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the security descriptor on.
        /// </param>
        /// <param name="securityDescriptor">
        /// The security descriptor to set on the item at the specified path.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor that was set on the item at the specified path
        /// is written to the context.
        /// </returns>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (securityDescriptor == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(securityDescriptor));
            }

            ProviderInfo provider = null;

            CmdletProvider providerInstance = null;
            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    false,
                    context,
                    out provider,
                    out providerInstance);

            foreach (string providerPath in providerPaths)
            {
                SetSecurityDescriptor(
                    providerInstance,
                    providerPath,
                    securityDescriptor,
                    context);
            }
        }

        private void SetSecurityDescriptor(
            CmdletProvider providerInstance,
            string path,
            ObjectSecurity securityDescriptor,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Diagnostics.Assert(
                securityDescriptor != null,
                "Caller should validate securityDescriptor before calling this method");

            Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            // This just verifies that the provider supports the interface.

            GetPermissionProviderInstance(providerInstance);

            try
            {
                providerInstance.SetSecurityDescriptor(path, securityDescriptor, context);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (PrivilegeNotHeldException e)
            {
                //
                // thrown if one tries to set SACL and does not have
                // SeSecurityPrivilege
                //
                context.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
            }
            catch (UnauthorizedAccessException e)
            {
                //
                // thrown if
                // -- owner or pri. group are invalid OR
                // -- marta returns ERROR_ACCESS_DENIED
                //
                context.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.PermissionDenied, path));
            }
            catch (NotSupportedException e)
            {
                //
                // thrown if path points to an item that does not
                // support access control.
                //
                // for example, FAT or FAT32 file in case of file system provider
                //
                context.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidOperation, path));
            }
            catch (SystemException e)
            {
                //
                // thrown if the CLR gets back unexpected error
                // from OS security or marta
                //
                context.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidOperation, path));
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "SetSecurityDescriptorProviderException",
                    SessionStateStrings.SetSecurityDescriptorProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        #endregion SetSecurityDescriptor

        #region NewSecurityDescriptor

        /// <summary>
        /// Gets the security descriptor from the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve the security descriptor from.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor for the item at the specified path is
        /// written to the context.
        /// </returns>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections sections)
        {
            ObjectSecurity sd = null;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            ProviderInfo provider = null;

            CmdletProvider providerInstance = null;
            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    false,
                    out provider,
                    out providerInstance);

            //
            // path must resolve to exact 1 item,
            // any other case is an error
            //
            if (providerPaths.Count == 1)
            {
                sd = NewSecurityDescriptorFromPath(providerInstance,
                                                   providerPaths[0],
                                                   sections);
            }
            else
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            return sd;
        }

        private ObjectSecurity NewSecurityDescriptorFromPath(
            CmdletProvider providerInstance,
            string path,
            AccessControlSections sections)
        {
            ObjectSecurity sd = null;

            // All parameters should have been validated by caller
            Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Diagnostics.Assert(
                ExecutionContext != null,
                "Caller should validate context before calling this method");

            // This just verifies that the provider supports the interface.

            ISecurityDescriptorCmdletProvider sdProvider =
                GetPermissionProviderInstance(providerInstance);

            try
            {
                sd = sdProvider.NewSecurityDescriptorFromPath(path,
                                                              sections);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "NewSecurityDescriptorProviderException",
                    SessionStateStrings.GetSecurityDescriptorProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return sd;
        }

        /// <summary>
        /// Gets the security descriptor from the specified item.
        /// </summary>
        /// <param name="type">
        /// The type of the item which corresponds to the security
        /// descriptor that we want to create.
        /// </param>
        /// <param name="providerId">
        /// The name of the provider.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor for the item at the specified type is
        /// written to the context.
        /// </returns>
        internal ObjectSecurity NewSecurityDescriptorOfType(
            string providerId,
            string type,
            AccessControlSections sections)
        {
            CmdletProvider providerInstance = GetProviderInstance(providerId);
            return NewSecurityDescriptorOfType(providerInstance, type, sections);
        }

        /// <summary>
        /// Gets the security descriptor from the specified item.
        /// </summary>
        /// <param name="type">
        /// The type of the item which corresponds to the security
        /// descriptor that we want to create.
        /// </param>
        /// <param name="providerInstance">
        /// The type of the item which corresponds to the security
        /// descriptor that we want to create.
        /// </param>
        /// <param name="sections">
        /// Specifies the parts of a security descriptor to retrieve.
        /// </param>
        /// <returns>
        /// Nothing. The security descriptor for the item at the specified type is
        /// written to the context.
        /// </returns>
        internal ObjectSecurity NewSecurityDescriptorOfType(
            CmdletProvider providerInstance,
            string type,
            AccessControlSections sections)
        {
            ObjectSecurity sd = null;

            if (type == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(type));
            }

            if (providerInstance == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(providerInstance));
            }

            // This just verifies that the provider supports the interface.

            ISecurityDescriptorCmdletProvider sdProvider =
                GetPermissionProviderInstance(providerInstance);

            try
            {
                sd = sdProvider.NewSecurityDescriptorOfType(type,
                                                            sections);
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "NewSecurityDescriptorProviderException",
                    SessionStateStrings.GetSecurityDescriptorProviderException,
                    providerInstance.ProviderInfo,
                    type,
                    e);
            }

            return sd;
        }

        #endregion NewSecurityDescriptor
    }
}
