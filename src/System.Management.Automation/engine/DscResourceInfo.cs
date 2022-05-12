// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Enumerated values for DSC resource implementation type.
    /// </summary>
    public enum ImplementedAsType
    {
        /// <summary>
        /// DSC resource implementation type not known.
        /// </summary>
        None = 0,

        /// <summary>
        /// DSC resource is implemented using PowerShell module.
        /// </summary>
        PowerShell = 1,

        /// <summary>
        /// DSC resource is implemented using a CIM provider.
        /// </summary>
        Binary = 2,

        /// <summary>
        /// DSC resource is a composite and implemented using configuration keyword.
        /// </summary>
        Composite = 3
    }

    /// <summary>
    /// Contains a DSC resource information.
    /// </summary>
    public class DscResourceInfo
    {
        /// <summary>
        /// Initializes a new instance of the DscResourceInfo class.
        /// </summary>
        /// <param name="name">Name of the DscResource.</param>
        /// <param name="friendlyName">FriendlyName of the DscResource.</param>
        /// <param name="path">Path of the DscResource.</param>
        /// <param name="parentPath">ParentPath of the DscResource.</param>
        /// <param name="context">The execution context for the DscResource.</param>
        internal DscResourceInfo(string name, string friendlyName, string path, string parentPath, ExecutionContext context)
        {
            this.Name = name;
            this.FriendlyName = friendlyName;
            this.Path = path;
            this.ParentPath = parentPath;
            this.Properties = new ReadOnlyCollection<DscResourcePropertyInfo>(new List<DscResourcePropertyInfo>());
        }

        /// <summary>
        /// Name of the DSC Resource.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets resource type name.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Gets or sets friendly name defined for the resource.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets of the file which implements the resource. For the resources which are defined using
        /// MOF file, this will be path to a module which resides in the same folder where schema.mof file is present.
        /// For composite resources, this will be the module which implements the resource.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets parent folder, where the resource is defined
        /// It is the folder containing either the implementing module(=Path) or folder containing ".schema.mof".
        /// For native providers, Path will be null and only ParentPath will be present.
        /// </summary>
        public string ParentPath { get; set; }

        /// <summary>
        /// Gets or sets a value which indicate how DSC resource is implemented.
        /// </summary>
        public ImplementedAsType ImplementedAs { get; set; }

        /// <summary>
        /// Gets or sets company which owns this resource.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets properties of the resource.
        /// </summary>
        public ReadOnlyCollection<DscResourcePropertyInfo> Properties { get; private set; }

        /// <summary>
        /// Updates properties of the resource.
        /// </summary>
        /// <param name="properties">Updated properties.</param>
        public void UpdateProperties(IList<DscResourcePropertyInfo> properties)
        {
            if (properties != null)
                this.Properties = new ReadOnlyCollection<DscResourcePropertyInfo>(properties);
        }

        /// <summary>
        /// Module in which the DscResource is implemented in.
        /// </summary>
        public PSModuleInfo Module { get; internal set; }

        /// <summary>
        /// Gets the help file path for the cmdlet.
        /// </summary>
        public string HelpFile { get; internal set; } = string.Empty;

        // HelpFile
    }

    /// <summary>
    /// Contains a DSC resource property information.
    /// </summary>
    public sealed class DscResourcePropertyInfo
    {
        /// <summary>
        /// Initializes a new instance of the DscResourcePropertyInfo class.
        /// </summary>
        internal DscResourcePropertyInfo()
        {
            this.Values = new ReadOnlyCollection<string>(new List<string>());
        }

        /// <summary>
        /// Gets or sets name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets type of the property.
        /// </summary>
        public string PropertyType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the property is mandatory or not.
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// Gets Values for a resource property.
        /// </summary>
        public ReadOnlyCollection<string> Values { get; private set; }

        internal void UpdateValues(IList<string> values)
        {
            if (values != null)
                this.Values = new ReadOnlyCollection<string>(values);
        }
    }
}
