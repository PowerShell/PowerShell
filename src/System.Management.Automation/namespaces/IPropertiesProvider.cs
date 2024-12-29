// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

#nullable enable
namespace System.Management.Automation.Provider
{
    #region IPropertyCmdletProvider

    /// <summary>
    /// An interface that can be implemented by a Cmdlet provider to expose properties of an item.
    /// </summary>
    /// <remarks>
    /// An IPropertyCmdletProvider provider implements a set of methods that allows
    /// the use of a set of core commands against the data store that the provider
    /// gives access to. By implementing this interface users can take advantage
    /// the commands that expose the contents of an item.
    ///     get-itemproperty
    ///     set-itemproperty
    ///     etc.
    /// This interface should only be implemented on derived classes of
    /// <see cref="CmdletProvider"/>, <see cref="ItemCmdletProvider"/>,
    /// <see cref="ContainerCmdletProvider"/>, or <see cref="NavigationCmdletProvider"/>.
    ///
    /// A namespace provider should implemented this interface if items in the
    /// namespace have properties the provide wishes to expose.
    /// </remarks>
    public interface IPropertyCmdletProvider
    {
        /// <summary>
        /// Gets the properties of the item specified by the path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <returns>
        /// Nothing.  The property that was retrieved should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to add properties to provider objects
        /// using the get-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not retrieve properties from objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        ///
        /// An <see cref="System.Management.Automation.PSObject"/> can be used as a property bag for the
        /// properties that need to be returned if the <paramref name="providerSpecificPickList"/> contains
        /// multiple properties to write.
        /// </remarks>
        void GetProperty(
            string path,
            Collection<string>? providerSpecificPickList);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? GetPropertyDynamicParameters(
            string path,
            Collection<string>? providerSpecificPickList);

        /// <summary>
        /// Sets the specified properties of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <returns>
        /// Nothing.  The property that was set should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to set the value of provider object properties
        /// using the set-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not retrieve properties from objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        ///
        /// An <see cref="System.Management.Automation.PSObject"/> can be used as a property bag for the
        /// properties that need to be returned if the <paramref name="providerSpecificPickList"/> contains
        /// multiple properties to write.
        /// <paramref name="propertyValue"/> is a property bag containing the properties that should be set.
        /// See <see cref="System.Management.Automation.PSObject"/> for more information.
        /// </remarks>
        void SetProperty(
            string path,
            PSObject propertyValue);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue);

        /// <summary>
        /// Clears a property of the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which to clear the property.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
        /// </param>
        /// <returns>
        /// Nothing.  The property that was cleared should be passed to the WritePropertyObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to clear the value of provider object properties
        /// using the clear-itemproperty cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not clear properties from objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        ///
        /// An <see cref="System.Management.Automation.PSObject"/> can be used as a property bag for the
        /// properties that need to be returned if the <paramref name="providerSpecificPickList"/> contains
        /// multiple properties to write.
        /// </remarks>
        void ClearProperty(
            string path,
            Collection<string> propertyToClear);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// clear-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        object? ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear);
    }

    #endregion IPropertyCmdletProvider
}
