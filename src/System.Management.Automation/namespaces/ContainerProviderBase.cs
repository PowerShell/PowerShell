// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region ContainerCmdletProvider

    /// <summary>
    /// The base class for Cmdlet providers that expose a single level of items.
    /// </summary>
    /// <remarks>
    /// The ContainerCmdletProvider class is base class that a provider derives from
    /// to implement methods that allow
    /// the use of a set of core commands against the objects that the provider
    /// gives access to. By deriving from this class users can take advantage of
    /// all the features of the <see cref="ItemCmdletProvider"/> as well as
    /// globbing and the following commands when targeting this provider:
    ///     get-childitem
    ///     rename-item
    ///     new-item
    ///     remove-item
    ///     set-location
    ///     push-location
    ///     pop-location
    ///     get-location -stack
    /// </remarks>
    public abstract class ContainerCmdletProvider : ItemCmdletProvider
    {
        #region Internal methods

        /// <summary>
        /// Internal wrapper for the GetChildItems protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path (or name in a flat namespace) to the item from which to retrieve the children.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be retrieved, false if only a single
        /// level of children should be retrieved. This parameter should only be true for
        /// the NavigationCmdletProvider derived class.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all children should be written to the Write*Object or
        /// Write*Objects method.
        /// </returns>
        internal void GetChildItems(
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            GetChildItems(path, recurse, depth);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the get-childitem cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be retrieved, false if only a single
        /// level of children should be retrieved. This parameter should only be true for
        /// the NavigationCmdletProvider derived class.
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
        internal object GetChildItemsDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return GetChildItemsDynamicParameters(path, recurse);
        }

        /// <summary>
        /// Internal wrapper for the GetChildNames protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item from which to retrieve the child names.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all names should be written to the Write*Object or
        /// Write*Objects method.
        /// </returns>
        /// <remarks>
        /// The child names are the leaf portion of the path. Example, for the file system
        /// the name for the path c:\windows\system32\foo.dll would be foo.dll or for
        /// the directory c:\windows\system32 would be system32. For Active Directory the
        /// child names would be RDN values of the child objects of the container.
        /// </remarks>
        internal void GetChildNames(
            string path,
            ReturnContainers returnContainers,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            GetChildNames(path, returnContainers);
        }

        /// <summary>
        /// Gets a new provider-specific path and filter (if any) that corresponds to the given
        /// path.
        /// </summary>
        /// <param name="path">
        /// The path to the item. Unlike most other provider APIs, this path is likely to
        /// contain PowerShell wildcards.
        /// </param>
        /// <param name="filter">
        /// The provider-specific filter currently applied.
        /// </param>
        /// <param name="updatedPath">
        /// The new path to the item.
        /// </param>
        /// <param name="updatedFilter">
        /// The new filter.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// True if the path or filter were altered. False otherwise.
        /// </returns>
        /// <remarks>
        /// Providers override this method if they support a native filtering syntax that
        /// can offer performance improvements over wildcard matching done by the PowerShell
        /// engine.
        /// If the provider can handle a portion (or all) of the PowerShell wildcard with
        /// semantics equivalent to the PowerShell wildcard, it may adjust the path to exclude
        /// the PowerShell wildcard.
        /// If the provider can augment the PowerShell wildcard with an approximate filter (but
        /// not replace it entirely,) it may simply return a filter without modifying the path.
        /// In this situation, PowerShell's wildcarding will still be applied to a smaller result
        /// set, resulting in improved performance.
        ///
        /// The default implementation of this method leaves both Path and Filter unmodified.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        internal virtual bool ConvertPath(
            string path,
            string filter,
            ref string updatedPath,
            ref string updatedFilter,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method
            return ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the get-childitem -name cmdlet.
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
        internal object GetChildNamesDynamicParameters(
            string path,
            CmdletProviderContext context)
        {
            Context = context;
            return GetChildNamesDynamicParameters(path);
        }

        /// <summary>
        /// Internal wrapper for the RenameItem protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all renamed items should be written to the Write*Object or
        /// Write*Objects.
        /// </returns>
        internal void RenameItem(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            RenameItem(path, newName);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the rename-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object RenameItemDynamicParameters(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            Context = context;
            return RenameItemDynamicParameters(path, newName);
        }

        /// <summary>
        /// Internal wrapper for the New protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to create.
        /// </param>
        /// <param name="type">
        /// The provider defined type of the item to create.
        /// </param>
        /// <param name="newItemValue">
        /// This is a provider specific type that the provider can use to create a new
        /// instance of an item at the specified path.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all new items should be written to the Write*Object or
        /// Write*Objects.
        /// </returns>
        internal void NewItem(
            string path,
            string type,
            object newItemValue,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            NewItem(path, type, newItemValue);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the new-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="type">
        /// The provider defined type of the item to create.
        /// </param>
        /// <param name="newItemValue">
        /// This is a provider specific type that the provider can use to create a new
        /// instance of an item at the specified path.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object NewItemDynamicParameters(
            string path,
            string type,
            object newItemValue,
            CmdletProviderContext context)
        {
            Context = context;
            return NewItemDynamicParameters(path, type, newItemValue);
        }

        /// <summary>
        /// Internal wrapper for the Remove protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to remove.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be removed, false if only a single
        /// level of children should be removed. This parameter should only be true for
        /// NavigationCmdletProvider and its derived classes.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        internal void RemoveItem(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            RemoveItem(path, recurse);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the remove-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be removed, false if only a single
        /// level of children should be removed. This parameter should only be true for
        /// NavigationCmdletProvider and its derived classes.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object RemoveItemDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return RemoveItemDynamicParameters(path, recurse);
        }

        /// <summary>
        /// Internal wrapper for the HasChildItems protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it has children.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// True if the item has children, false otherwise.
        /// </returns>
        /// <remarks>
        /// For implementers of ContainerCmdletProvider classes and those derived from it,
        /// if a null or empty path is passed,
        /// the provider should consider any items in the data store to be children
        /// and return true.
        /// </remarks>
        internal bool HasChildItems(string path, CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return HasChildItems(path);
        }

        /// <summary>
        /// Internal wrapper for the Copy protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path of the item to copy.
        /// </param>
        /// <param name="copyPath">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing. All objects that are copied should be written to the Write*Object or
        /// Write*Objects methods.
        /// </returns>
        internal void CopyItem(
            string path,
            string copyPath,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            CopyItem(path, copyPath, recurse);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the copy-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="destination">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse,
            CmdletProviderContext context)
        {
            Context = context;
            return CopyItemDynamicParameters(path, destination, recurse);
        }

        #endregion Internal members

        #region Protected methods

        /// <summary>
        /// Gets the children of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path (or name in a flat namespace) to the item from which to retrieve the children.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be retrieved, false if only a single
        /// level of children should be retrieved. This parameter should only be true for
        /// the NavigationCmdletProvider derived class.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user access to the provider objects using
        /// the get-childitem cmdlets.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not write objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
        ///
        /// The provider implementation is responsible for preventing infinite recursion when there are
        /// circular links and the like. An appropriate terminating exception should be thrown if this
        /// situation occurs.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void GetChildItems(
            string path,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gets the children of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path (or name in a flat namespace) to the item from which to retrieve the children.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be retrieved, false if only a single
        /// level of children should be retrieved. This parameter should only be true for
        /// the NavigationCmdletProvider derived class.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user access to the provider objects using
        /// the get-childitem cmdlets.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not write objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
        ///
        /// The provider implementation is responsible for preventing infinite recursion when there are
        /// circular links and the like. An appropriate terminating exception should be thrown if this
        /// situation occurs.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void GetChildItems(
            string path,
            bool recurse,
            uint depth)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                if (depth == uint.MaxValue)
                {
                    this.GetChildItems(path, recurse);
                }
                else
                {
                    throw
                        PSTraceSource.NewNotSupportedException(
                            SessionStateStrings.CmdletProvider_NotSupportedRecursionDepth);
                }
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the get-childitem cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be retrieved, false if only a single
        /// level of children should be retrieved. This parameter should only be true for
        /// the NavigationCmdletProvider derived class.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Gets names of the children of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item from which to retrieve the child names.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user access to the provider objects using
        /// the get-childitem  -name cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class. The exception to this
        /// is if <paramref name="returnAllContainers"/> is true, then any child name for a container should
        /// be returned even if it doesn't match the Filter, Include, or Exclude.
        ///
        /// By default overrides of this method should not write the names of objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
        ///
        /// The provider implementation is responsible for preventing infinite recursion when there are
        /// circular links and the like. An appropriate terminating exception should be thrown if this
        /// situation occurs.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void GetChildNames(
            string path,
            ReturnContainers returnContainers)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gets a new provider-specific path and filter (if any) that corresponds to the given
        /// path.
        /// </summary>
        /// <param name="path">
        /// The path to the item. Unlike most other provider APIs, this path is likely to
        /// contain PowerShell wildcards.
        /// </param>
        /// <param name="filter">
        /// The provider-specific filter currently applied.
        /// </param>
        /// <param name="updatedPath">
        /// The new path to the item.
        /// </param>
        /// <param name="updatedFilter">
        /// The new filter.
        /// </param>
        /// <returns>
        /// True if the path or filter were altered. False otherwise.
        /// </returns>
        /// <remarks>
        /// Providers override this method if they support a native filtering syntax that
        /// can offer performance improvements over wildcard matching done by the PowerShell
        /// engine.
        /// If the provider can handle a portion (or all) of the PowerShell wildcard with
        /// semantics equivalent to the PowerShell wildcard, it may adjust the path to exclude
        /// the PowerShell wildcard.
        /// If the provider can augment the PowerShell wildcard with an approximate filter (but
        /// not replace it entirely,) it may simply return a filter without modifying the path.
        /// In this situation, PowerShell's wildcarding will still be applied to a smaller result
        /// set, resulting in improved performance.
        ///
        /// The default implementation of this method leaves both Path and Filter unmodified.
        ///
        /// PowerShell wildcarding semantics are handled by the System.Management.Automation.Wildcardpattern
        /// class.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "3#")]
        protected virtual bool ConvertPath(
            string path,
            string filter,
            ref string updatedPath,
            ref string updatedFilter)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return false;
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the get-childitem -name cmdlet.
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
        protected virtual object GetChildNamesDynamicParameters(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Renames the item at the specified path to the new name provided.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <returns>
        /// Nothing is returned, but the renamed items should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to rename provider objects using
        /// the rename-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not allow renaming objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not allow renaming of a hidden or system file unless the Force property is set to true.
        ///
        /// This method is intended for the modification of the item's name only and not for Move operations.
        /// An error should be written to <see cref="CmdletProvider.WriteError"/> if the <paramref name="newName"/>
        /// parameter contains path separators or would cause the item to change its parent location.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void RenameItem(
            string path,
            string newName)
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
        /// the rename-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object RenameItemDynamicParameters(string path, string newName)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to create.
        /// </param>
        /// <param name="itemTypeName">
        /// The provider defined type for the object to create.
        /// </param>
        /// <param name="newItemValue">
        /// This is a provider specific type that the provider can use to create a new
        /// instance of an item at the specified path.
        /// </param>
        /// <returns>
        /// Nothing is returned, but the renamed items should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to create new provider objects using
        /// the new-item cmdlet.
        ///
        /// The <paramref name="itemTypeName"/> parameter is a provider specific string that the user specifies to tell
        /// the provider what type of object to create.  For instance, in the FileSystem provider the <paramref name="type"/>
        /// parameter can take a value of "file" or "directory". The comparison of this string should be
        /// case-insensitive and you should also allow for least ambiguous matches. So if the provider allows
        /// for the types "file" and "directory", only the first letter is required to disambiguate.
        /// If <paramref name="itemTypeName"/> refers to a type the provider cannot create, the provider should produce
        /// an <see cref="ArgumentException"/> with a message indicating the types the provider can create.
        ///
        /// The <paramref name="newItemValue"/> parameter can be any type of object that the provider can use
        /// to create the item. It is recommended that the provider accept at a minimum strings, and an instance
        /// of the type of object that would be returned from GetItem() for this path. <see cref="LanguagePrimitives.ConvertTo(object, System.Type)"/>
        /// can be used to convert some types to the desired type.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void NewItem(
            string path,
            string itemTypeName,
            object newItemValue)
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
        /// the new-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="itemTypeName">
        /// The provider defined type of the item to create.
        /// </param>
        /// <param name="newItemValue">
        /// This is a provider specific type that the provider can use to create a new
        /// instance of an item at the specified path.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object NewItemDynamicParameters(
            string path,
            string itemTypeName,
            object newItemValue)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Removes (deletes) the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to remove.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be removed, false if only a single
        /// level of children should be removed. This parameter should only be true for
        /// NavigationCmdletProvider and its derived classes.
        /// </param>
        /// <returns>
        /// Nothing should be returned or written from this method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to allow the user the ability to remove provider objects using
        /// the remove-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not remove objects that are generally hidden from
        /// the user unless the Force property is set to true. For instance, the FileSystem provider should
        /// not remove a hidden or system file unless the Force property is set to true.
        ///
        /// The provider implementation is responsible for preventing infinite recursion when there are
        /// circular links and the like. An appropriate terminating exception should be thrown if this
        /// situation occurs.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void RemoveItem(
            string path,
            bool recurse)
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
        /// the remove-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="recurse">
        /// True if all children in a subtree should be removed, false if only a single
        /// level of children should be removed. This parameter should only be true for
        /// NavigationCmdletProvider and its derived classes.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object RemoveItemDynamicParameters(
            string path,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if the item at the specified path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it has children.
        /// </param>
        /// <returns>
        /// True if the item has children, false otherwise.
        /// </returns>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the provider infrastructure the ability to determine
        /// if a particular provider object has children without having to retrieve all the child items.
        ///
        /// For implementers of <see cref="ContainerCmdletProvider"/> classes and those derived from it,
        /// if a null or empty path is passed,
        /// the provider should consider any items in the data store to be children
        /// and return true.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual bool HasChildItems(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Copies an item at the specified path to an item at the <paramref name="copyPath"/>.
        /// </summary>
        /// <param name="path">
        /// The path of the item to copy.
        /// </param>
        /// <param name="copyPath">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all the objects that were copied should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to copy provider objects using
        /// the copy-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path and items being copied
        /// meets those requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not copy objects over existing items unless the Force
        /// property is set to true. For instance, the FileSystem provider should not copy c:\temp\foo.txt over
        /// c:\bar.txt if c:\bar.txt already exists unless the Force parameter is true.
        ///
        /// If <paramref name="copyPath"/> exists and is a container then Force isn't required and <paramref name="path"/>
        /// should be copied into the <paramref name="copyPath"/> container as a child.
        ///
        /// If <paramref name="recurse"/> is true, the provider implementation is responsible for
        /// preventing infinite recursion when there are circular links and the like. An appropriate
        /// terminating exception should be thrown if this situation occurs.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void CopyItem(
            string path,
            string copyPath,
            bool recurse)
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
        /// the copy-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="destination">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        #endregion Protected members
    }

    #endregion ContainerCmdletProvider
}
