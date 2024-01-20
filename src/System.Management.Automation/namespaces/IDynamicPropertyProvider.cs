// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
using Microsoft.Win32;

namespace System.Management.Automation.Provider
{
    #region IDynamicPropertyCmdletProvider
    /// <summary>
    /// An interface that can be implemented on a Cmdlet provider to expose the dynamic
    /// manipulation of properties.
    /// </summary>
    /// <remarks>
    /// An IDynamicPropertyCmdletProvider provider implements a set of methods that allows
    /// the use of a set of core commands against the data store that the provider
    /// gives access to. By implementing this interface users can take advantage
    /// the commands that expose the creation and deletion of properties on an item.
    ///     rename-itemproperty
    ///     remove-itemproperty
    ///     new-itemproperty
    ///     etc.
    /// This interface should only be implemented on derived classes of
    /// <see cref="CmdletProvider"/>, <see cref="ItemCmdletProvider"/>,
    /// <see cref="ContainerCmdletProvider"/>, or <see cref="NavigationCmdletProvider"/>.
    ///
    /// A Cmdlet provider should implemented this interface if items in the
    /// namespace have dynamic properties the provide wishes to expose.
    /// </remarks>
    public interface IDynamicPropertyCmdletProvider : IPropertyCmdletProvider
    {
        /// <summary>
        /// Creates a new property on the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which the new property should be created.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="propertyType">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <returns>
        /// Nothing.  The new property that was created should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to add properties to provider objects
        /// using the new-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not create new properties on objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void NewProperty(
            string path,
            string propertyName,
            RegistryValueKind propertyType,
            object? value);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// new-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="propertyType">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? NewPropertyDynamicParameters(
            string path,
            string propertyName,
            RegistryValueKind propertyType,
            object? value);

        /// <summary>
        /// Removes a property on the item specified by the path.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property to be removed.
        /// </param>
        /// <returns>
        /// Nothing.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to remove properties from provider objects
        /// using the remove-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not remove properties on objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void RemoveProperty(
            string path,
            string propertyName);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// remove-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be removed.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object RemovePropertyDynamicParameters(
            string path,
            string propertyName);

        /// <summary>
        /// Renames a property of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which to rename the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// Nothing.  The new property that was renamed should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to rename properties of provider objects
        /// using the rename-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not rename properties on objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void RenameProperty(
            string path,
            string sourceProperty,
            string destinationProperty);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// rename-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? RenamePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationProperty);

        /// <summary>
        /// Copies a property of the item at the specified path to a new property on the
        /// destination item.
        /// </summary>
        /// <param name="sourcePath">
        /// The path to the item on which to copy the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// Nothing.  The new property that was copied to should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to copy properties of provider objects
        /// using the copy-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not copy properties from or to objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void CopyProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// copy-itemproperty cmdlet.
        /// </summary>
        /// <param name="sourcePath">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? CopyPropertyDynamicParameters(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty);

        /// <summary>
        /// Moves a property on an item specified by the path.
        /// </summary>
        /// <param name="sourcePath">
        /// The path to the item on which to move the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to move.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to move to.
        /// </param>
        /// <returns>
        /// Nothing.  The new property that was created should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to move properties from one provider object
        /// to another using the move-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not move properties on or to objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void MoveProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// move-itemproperty cmdlet.
        /// </summary>
        /// <param name="sourcePath">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? MovePropertyDynamicParameters(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty);
    }

    #endregion IDynamicPropertyCmdletProvider
}
