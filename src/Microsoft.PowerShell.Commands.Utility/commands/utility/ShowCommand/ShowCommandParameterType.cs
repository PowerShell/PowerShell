// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    /// <summary>
    /// Implements a facade around ShowCommandParameterInfo and its deserialized counterpart.
    /// </summary>
    public class ShowCommandParameterType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandParameterType"/> class
        /// with the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterType(Type other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.FullName = other.FullName;
            if (other.IsEnum)
            {
                this.EnumValues = new ArrayList(Enum.GetValues(other));
            }

            if (other.IsArray)
            {
                this.ElementType = new ShowCommandParameterType(other.GetElementType());
            }

            object[] attributes = other.GetCustomAttributes(typeof(FlagsAttribute), true);
            this.HasFlagAttribute = attributes.Length != 0;
            this.ImplementsDictionary = typeof(IDictionary).IsAssignableFrom(other);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowCommandParameterType"/> class
        /// with the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandParameterType(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.IsEnum = (bool)(other.Members["IsEnum"].Value);
            this.FullName = other.Members["FullName"].Value as string;
            this.IsArray = (bool)(other.Members["IsArray"].Value);
            this.HasFlagAttribute = (bool)(other.Members["HasFlagAttribute"].Value);
            this.ImplementsDictionary = (bool)(other.Members["ImplementsDictionary"].Value);

            if (this.IsArray)
            {
                this.ElementType = new ShowCommandParameterType(other.Members["ElementType"].Value as PSObject);
            }

            if (this.IsEnum)
            {
                this.EnumValues = (other.Members["EnumValues"].Value as PSObject).BaseObject as ArrayList;
            }
        }

        /// <summary>
        /// The full name of the outermost type.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Whether or not this type is an enum.
        /// </summary>
        public bool IsEnum { get; }

        /// <summary>
        /// Whether or not this type is an dictionary.
        /// </summary>
        public bool ImplementsDictionary { get; }

        /// <summary>
        /// Whether or not this enum has a flag attribute.
        /// </summary>
        public bool HasFlagAttribute { get; }

        /// <summary>
        /// Whether or not this type is an array type.
        /// </summary>
        public bool IsArray { get; }

        /// <summary>
        /// Gets the inner type, if this corresponds to an array type.
        /// </summary>
        public ShowCommandParameterType ElementType { get; }

        /// <summary>
        /// Whether or not this type is a string.
        /// </summary>
        public bool IsString
        {
            get
            {
                return string.Equals(this.FullName, "System.String", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Whether or not this type is an script block.
        /// </summary>
        public bool IsScriptBlock
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.ScriptBlock", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Whether or not this type is a bool.
        /// </summary>
        public bool IsBoolean
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.ScriptBlock", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Whether or not this type is a switch parameter.
        /// </summary>
        public bool IsSwitch
        {
            get
            {
                return string.Equals(this.FullName, "System.Management.Automation.SwitchParameter", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// If this is an enum value, return the list of potential values.
        /// </summary>
        public ArrayList EnumValues { get; }
    }
}
