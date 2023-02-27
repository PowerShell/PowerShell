// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Xml;

namespace Microsoft.WSMan.Management
{
    /// <summary>
    /// Class that queries the server and gets current configurations.
    /// Also provides a generic way to update the configurations.
    /// </summary>
    internal class CurrentConfigurations
    {
        /// <summary>
        /// Prefix used to add NameSpace of root element to namespace manager.
        /// </summary>
        public const string DefaultNameSpacePrefix = "defaultNameSpace";

        /// <summary>
        /// This holds the current configurations XML.
        /// </summary>
        private readonly XmlDocument rootDocument;

        /// <summary>
        /// Holds the reference to the current document element.
        /// </summary>
        private XmlElement documentElement;

        /// <summary>
        /// Holds the Namespace Manager to use for XPATH queries.
        /// </summary>
        private XmlNamespaceManager nameSpaceManger;

        /// <summary>
        /// Session of the WsMan sserver.
        /// </summary>
        private readonly IWSManSession serverSession;

        /// <summary>
        /// Gets the server session associated with the configuration.
        /// </summary>
        public IWSManSession ServerSession
        {
            get { return serverSession; }
        }

        /// <summary>
        /// Gets the current configuration XML.
        /// </summary>
        public XmlDocument RootDocument
        {
            get { return this.rootDocument; }
        }

        /// <summary>
        /// Gets the current configuration on the given server and for given URI.
        /// This issues a GET request to the server.
        /// </summary>
        /// <param name="serverSession">Current server session.</param>
        public CurrentConfigurations(IWSManSession serverSession)
        {
            ArgumentNullException.ThrowIfNull(serverSession);

            this.rootDocument = new XmlDocument();
            this.serverSession = serverSession;
        }

        /// <summary>
        /// Refresh the CurrentConfiguration. This method calls GET operation for the given
        /// URI on the server and update the current configuration. It also initialize some
        /// of required class members.
        /// </summary>
        /// <param name="responseOfGet">Plugin configuration.</param>
        /// <returns>False, if operation failed.</returns>
        public bool RefreshCurrentConfiguration(string responseOfGet)
        {
            ArgumentException.ThrowIfNullOrEmpty(responseOfGet);

            this.rootDocument.LoadXml(responseOfGet);
            this.documentElement = this.rootDocument.DocumentElement;

            this.nameSpaceManger = new XmlNamespaceManager(this.rootDocument.NameTable);
            this.nameSpaceManger.AddNamespace(CurrentConfigurations.DefaultNameSpacePrefix, this.documentElement.NamespaceURI);

            return string.IsNullOrEmpty(this.serverSession.Error);
        }

        /// <summary>
        /// Update the server with updated XML.
        /// Issues a PUT request with the ResourceUri provided.
        /// </summary>
        /// <param name="resourceUri">Resource URI to use.</param>
        /// <returns>False, if operation is not successful.</returns>
        public void PutConfigurationOnServer(string resourceUri)
        {
            ArgumentException.ThrowIfNullOrEmpty(resourceUri);

            this.serverSession.Put(resourceUri, this.rootDocument.InnerXml, 0);
        }

        /// <summary>
        /// This method will remove the configuration from the XML.
        /// Currently the method will only remove the attributes. But it is extensible enough to support
        /// Node removals in future.
        /// </summary>
        /// <param name="pathToNodeFromRoot">Path with namespace to the node from Root element. Must not end with '/'.</param>
        public void RemoveOneConfiguration(string pathToNodeFromRoot)
        {
            ArgumentNullException.ThrowIfNull(pathToNodeFromRoot);

            XmlNode nodeToRemove =
                this.documentElement.SelectSingleNode(
                    pathToNodeFromRoot,
                    this.nameSpaceManger);

            if (nodeToRemove != null)
            {
                if (nodeToRemove is XmlAttribute)
                {
                    RemoveAttribute(nodeToRemove as XmlAttribute);
                }
            }
            else
            {
                throw new ArgumentException("Node is not present in the XML, Please give valid XPath", nameof(pathToNodeFromRoot));
            }
        }

        /// <summary>
        /// Create or Update the value of the configuration on the given Node. Currently this
        /// method is supported for updating attributes, but can be easily updated for nodes.
        /// Caller should call this method to add a new attribute to the Node.
        /// </summary>
        /// <param name="pathToNodeFromRoot">Path with namespace to the node from Root element. Must not end with '/'.</param>
        /// <param name="configurationName">Name of the configuration with name space to update or create.</param>
        /// <param name="configurationValue">Value of the configurations.</param>
        public void UpdateOneConfiguration(string pathToNodeFromRoot, string configurationName, string configurationValue)
        {
            ArgumentNullException.ThrowIfNull(pathToNodeFromRoot);
            ArgumentException.ThrowIfNullOrEmpty(configurationName);
            ArgumentNullException.ThrowIfNull(configurationValue);

            XmlNode nodeToUpdate =
                this.documentElement.SelectSingleNode(
                    pathToNodeFromRoot,
                    this.nameSpaceManger);

            if (nodeToUpdate != null)
            {
                foreach (XmlAttribute attribute in nodeToUpdate.Attributes)
                {
                    if (attribute.Name.Equals(configurationName, StringComparison.OrdinalIgnoreCase))
                    {
                        attribute.Value = configurationValue;
                        return;
                    }
                }

                XmlNode attr = this.rootDocument.CreateNode(XmlNodeType.Attribute, configurationName, string.Empty);
                attr.Value = configurationValue;

                nodeToUpdate.Attributes.SetNamedItem(attr);
            }
        }

        /// <summary>
        /// Gets the value of the configuration on the given Node or attribute.
        /// </summary>
        /// <param name="pathFromRoot">Path with namespace to the node from Root element.</param>
        /// <returns>Value of the Node, or Null if no node present.</returns>
        public string GetOneConfiguration(string pathFromRoot)
        {
            ArgumentNullException.ThrowIfNull(pathFromRoot);

            XmlNode requiredNode =
                this.documentElement.SelectSingleNode(
                    pathFromRoot,
                    this.nameSpaceManger);

            if (requiredNode != null)
            {
                return requiredNode.Value;
            }

            return null;
        }

        /// <summary>
        /// Removes the attribute from OwnerNode.
        /// </summary>
        /// <param name="attributeToRemove">Attribute to Remove.</param>
        private static void RemoveAttribute(XmlAttribute attributeToRemove)
        {
            XmlElement ownerElement = attributeToRemove.OwnerElement;
            ownerElement.RemoveAttribute(attributeToRemove.Name);
        }
    }
}
