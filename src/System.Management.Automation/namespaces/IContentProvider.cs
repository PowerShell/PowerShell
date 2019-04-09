// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider
{
    #region IContentCmdletProvider

    /// <summary>
    /// An interface that can be implemented on a Cmdlet provider to expose an item's
    /// content.
    /// </summary>
    /// <remarks>
    /// An IContentCmdletProvider provider implements a set of methods that allows
    /// the use of a set of core commands against the data store that the provider
    /// gives access to. By implementing this interface users can take advantage
    /// the commands that expose the contents of an item.
    ///     get-content
    ///     set-content
    ///     clear-content
    ///
    /// This interface should only be implemented on derived classes of
    /// <see cref="CmdletProvider"/>, <see cref="ItemCmdletProvider"/>,
    /// <see cref="ContainerCmdletProvider"/>, or <see cref="NavigationCmdletProvider"/>.
    ///
    /// A namespace provider should implemented this interface if items in the
    /// namespace have content the provide wishes to expose.
    /// </remarks>
    public interface IContentCmdletProvider
    {
        /// <summary>
        /// Gets the content reader for the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to get the content reader for.
        /// </param>
        /// <remarks>
        /// Overrides of this method should return an <see cref="System.Management.Automation.Provider.IContentReader"/>
        /// for the item specified by the path.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not return a content reader for objects
        /// that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        IContentReader GetContentReader(string path);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// get-content cmdlet.
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
        object GetContentReaderDynamicParameters(string path);

        /// <summary>
        /// Gets the content writer for the item at the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item to get the content writer for.
        /// </param>
        /// <returns>
        /// An IContentWriter for the item at the specified path.
        /// </returns>
        /// <remarks>
        /// Overrides of this method should return an <see cref="System.Management.Automation.Provider.IContentWriter"/>
        /// for the item specified by the path.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not return a content writer for objects
        /// that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        IContentWriter GetContentWriter(string path);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// set-content and add-content cmdlet.
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
        object GetContentWriterDynamicParameters(string path);

        /// <summary>
        /// Clears the content from the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item to clear the content from.
        /// </param>
        /// <remarks>
        /// Overrides of this method should remove any content from the object but
        /// not remove (delete) the object itself.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not clear or write objects that are generally hidden from
        /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
        /// the path represents an item that is hidden from the user and Force is set to false.
        /// </remarks>
        void ClearContent(string path);

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to the
        /// clear-content cmdlet.
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
        object ClearContentDynamicParameters(string path);
    }

    #endregion IContentCmdletProvider
}

