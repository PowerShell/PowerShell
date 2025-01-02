// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region ItemCmdletProvider

    /// <summary>
    /// The base class for Cmdlet providers that expose an item as a PowerShell path.
    /// </summary>
    /// <remarks>
    /// The ItemCmdletProvider class is a base class that a provider derives from to
    /// inherit a set of methods that allows the PowerShell engine
    /// to provide a core set of commands for getting and setting of data on one or
    /// more items. A provider should derive from this class if they want
    /// to take advantage of the item core commands that are
    /// already implemented by the engine. This allows users to have common
    /// commands and semantics across multiple providers.
    /// </remarks>
    public abstract class ItemCmdletProvider : DriveCmdletProvider
    {
        #region internal methods

        /// <summary>
        /// Internal wrapper for the GetItem protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteObject method.
        /// </returns>
        internal void GetItem(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            GetItem(path);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the get-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        internal object GetItemDynamicParameters(string path, CmdletProviderContext context)
        {
            Context = context;
            return GetItemDynamicParameters(path);
        }

        /// <summary>
        /// Internal wrapper for the SetItem protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set.
        /// </param>
        /// <param name="value">
        /// The value of the item specified by the path.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// The item that was set at the specified path.
        /// </returns>
        internal void SetItem(
            string path,
            object value,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.SetItem");

            Context = context;

            // Call virtual method

            SetItem(path, value);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the set-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="value">
        /// The value of the item specified by the path.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        internal object SetItemDynamicParameters(
            string path,
            object value,
            CmdletProviderContext context)
        {
            Context = context;
            return SetItemDynamicParameters(path, value);
        }

        /// <summary>
        /// Internal wrapper for the ClearItem protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to clear.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        internal void ClearItem(
            string path,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.ClearItem");

            Context = context;

            // Call virtual method

            ClearItem(path);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the clear-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        internal object ClearItemDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return ClearItemDynamicParameters(path);
        }

        /// <summary>
        /// Internal wrapper for the InvokeDefaultAction protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to perform the default action on.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        internal void InvokeDefaultAction(
            string path,
            CmdletProviderContext context)
        {
            providerBaseTracer.WriteLine("ItemCmdletProvider.InvokeDefaultAction");

            Context = context;

            // Call virtual method

            InvokeDefaultAction(path);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the invoke-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        internal object InvokeDefaultActionDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return InvokeDefaultActionDynamicParameters(path);
        }

        /// <summary>
        /// Internal wrapper for the Exists protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it exists.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// True if the item exists, false otherwise.
        /// </returns>
        internal bool ItemExists(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            bool itemExists = false;
            try
            {
                // Some providers don't expect non-valid path elements, and instead
                // throw an exception here.
                itemExists = ItemExists(path);
            }
            catch (Exception)
            {
            }

            return itemExists;
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the test-path cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        internal object ItemExistsDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return ItemExistsDynamicParameters(path);
        }

        /// <summary>
        /// Internal wrapper for the IsValidPath protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to check for validity.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// True if the path is syntactically and semantically valid for the provider, or
        /// false otherwise.
        /// </returns>
        /// <remarks>
        /// This test should not verify the existence of the item at the path. It should
        /// only perform syntactic and semantic validation of the path.  For instance, for
        /// the file system provider, that path should be canonicalized, syntactically verified,
        /// and ensure that the path does not refer to a device.
        /// </remarks>
        internal bool IsValidPath(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return IsValidPath(path);
        }

        /// <summary>
        /// Internal wrapper for the ExpandPath protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set. Only called for providers that declare
        /// the ExpandWildcards capability.
        /// </summary>
        /// <param name="path">
        /// The path to expand. Expansion must be consistent with the wildcarding
        /// rules of PowerShell's WildcardPattern class.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// A list of provider paths that this path expands to. They must all exist.
        /// </returns>
        internal string[] ExpandPath(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            return ExpandPath(path);
        }

        #endregion internal methods

        #region Protected methods

        /// <summary>
        /// Gets the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user access to the provider objects using
        /// the get-item and get-childitem cmdlets.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not write objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void GetItem(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the get-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object GetItemDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the item specified by the path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set.
        /// </param>
        /// <param name="value">
        /// The value of the item specified by the path.
        /// </param>
        /// <returns>
        /// Nothing.  The item that was set should be passed to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to modify provider objects using
        /// the set-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not set or write objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void SetItem(
            string path,
            object value)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the set-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="value">
        /// The value of the item specified by the path.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object SetItemDynamicParameters(string path, object value)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the item specified by the path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to clear.
        /// </param>
        /// <returns>
        /// Nothing.  The item that was cleared should be passed to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to clear provider objects using
        /// the clear-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not clear or write objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void ClearItem(
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the clear-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object ClearItemDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Invokes the default action on the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to perform the default action on.
        /// </param>
        /// <returns>
        /// Nothing.  The item that was set should be passed to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// The default implementation does nothing.
        ///
        /// Providers override this method to give the user the ability to invoke provider objects using
        /// the invoke-item cmdlet. Think of the invocation as a double click in the Windows Shell. This
        /// method provides a default action based on the path that was passed.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not invoke objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        protected virtual void InvokeDefaultAction(
            string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the invoke-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object InvokeDefaultActionDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if an item exists at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it exists.
        /// </param>
        /// <returns>
        /// True if the item exists, false otherwise.
        /// </returns>
        /// <returns>
        /// Nothing.  The item that was set should be passed to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to check for the existence of provider objects using
        /// the set-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// The implementation of this method should take into account any form of access to the object that may
        /// make it visible to the user.  For instance, if a user has write access to a file in the file system
        /// provider bug not read access, the file still exists and the method should return true.  Sometimes this
        /// may require checking the parent to see if the child can be enumerated.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual bool ItemExists(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the test-path cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object ItemExistsDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Providers must override this method to verify the syntax and semantics
        /// of their paths.
        /// </summary>
        /// <param name="path">
        /// The path to check for validity.
        /// </param>
        /// <returns>
        /// True if the path is syntactically and semantically valid for the provider, or
        /// false otherwise.
        /// </returns>
        /// <remarks>
        /// This test should not verify the existence of the item at the path. It should
        /// only perform syntactic and semantic validation of the path.  For instance, for
        /// the file system provider, that path should be canonicalized, syntactically verified,
        /// and ensure that the path does not refer to a device.
        /// </remarks>
        protected abstract bool IsValidPath(string path);

        /// <summary>
        /// Expand a provider path that contains wildcards to a list of provider
        /// paths that the path represents.Only called for providers that declare
        /// the ExpandWildcards capability.
        /// </summary>
        /// <param name="path">
        /// The path to expand. Expansion must be consistent with the wildcarding
        /// rules of PowerShell's WildcardPattern class.
        /// </param>
        /// <returns>
        /// A list of provider paths that this path expands to. They must all exist.
        /// </returns>
        protected virtual string[] ExpandPath(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return new string[] { path };
            }
        }

        #endregion Protected methods
    }

    #endregion ItemCmdletProvider
}
