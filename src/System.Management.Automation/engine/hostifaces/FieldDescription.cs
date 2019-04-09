// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using Dbg = System.Management.Automation.Diagnostics;

//using System.Runtime.Serialization;
//using System.ComponentModel;
//using System.Runtime.InteropServices;
//using System.Globalization;
//using System.Management.Automation;
//using System.Reflection;

namespace System.Management.Automation.Host
{
    /// <summary>
    /// Provides a description of a field for use by <see cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>.
    /// <!--Used by the Msh engine to describe cmdlet parameters.-->
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="System.Management.Automation.Host.FieldDescription"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>

    public class
    FieldDescription
    {
        /// <summary>
        /// Initializes a new instance of FieldDescription and defines the Name value.
        /// </summary>
        /// <param name="name">
        /// The name to identify this field description
        /// </param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// <paramref name="name"/> is null or empty.
        /// </exception>

        public
        FieldDescription(string name)
        {
            // the only required parameter is the name.

            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException("name", DescriptionsStrings.NullOrEmptyErrorTemplate, "name");
            }

            this.name = name;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Sets the ParameterTypeName, ParameterTypeFullName, and ParameterAssemblyFullName as a single operation.
        /// </summary>
        /// <param name="parameterType">
        /// The Type that sets the properties.
        /// </param>
        /// <exception cref="System.Management.Automation.PSArgumentNullException">
        /// If <paramref name="parameterType"/> is null.
        /// </exception>

        public
        void
        SetParameterType(System.Type parameterType)
        {
            if (parameterType == null)
            {
                throw PSTraceSource.NewArgumentNullException("parameterType");
            }

            SetParameterTypeName(parameterType.Name);
            SetParameterTypeFullName(parameterType.FullName);
            SetParameterAssemblyFullName(parameterType.AssemblyQualifiedName);
        }

        /// <summary>
        /// Gets the short name of the parameter's type.
        /// </summary>
        /// <value>
        /// The type name of the parameter
        /// </value>
        /// <remarks>
        /// If not already set by a call to <see cref="System.Management.Automation.Host.FieldDescription.SetParameterType"/>,
        /// <see cref="System.String"/> will be used as the type.
        /// <!--The value of ParameterTypeName is the string value returned.
        /// by System.Type.Name.-->
        /// </remarks>

        public
        string
        ParameterTypeName
        {
            get
            {
                if (string.IsNullOrEmpty(parameterTypeName))
                {
                    // the default if the type name is not specified is 'string'

                    SetParameterType(typeof(string));
                }

                return parameterTypeName;
            }
        }

        /// <summary>
        /// Gets the full string name of the parameter's type.
        /// </summary>
        /// <remarks>
        /// If not already set by a call to <see cref="System.Management.Automation.Host.FieldDescription.SetParameterType"/>,
        /// <see cref="System.String"/> will be used as the type.
        /// <!--The value of ParameterTypeName is the string value returned.
        /// by System.Type.Name.-->
        /// </remarks>

        public
        string
        ParameterTypeFullName
        {
            get
            {
                if (string.IsNullOrEmpty(parameterTypeFullName))
                {
                    // the default if the type name is not specified is 'string'

                    SetParameterType(typeof(string));
                }

                return parameterTypeFullName;
            }
        }

        /// <summary>
        /// Gets the full name of the assembly containing the type identified by ParameterTypeFullName or ParameterTypeName.
        /// </summary>
        /// <remarks>
        /// If the assembly is not currently loaded in the hosting application's AppDomain, the hosting application needs
        /// to load the containing assembly to access the type information. AssemblyName is used for this purpose.
        ///
        /// If not already set by a call to <see cref="System.Management.Automation.Host.FieldDescription.SetParameterType"/>,
        /// <see cref="System.String"/> will be used as the type.
        /// </remarks>

        public
        string
        ParameterAssemblyFullName
        {
            get
            {
                if (string.IsNullOrEmpty(parameterAssemblyFullName))
                {
                    // the default if the type name is not specified is 'string'

                    SetParameterType(typeof(string));
                }

                return parameterAssemblyFullName;
            }
        }

        /// <summary>
        /// A short, human-presentable message to describe and identify the field.  If supplied, a typical implementation of
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/> will use this value instead of
        /// the field name to identify the field to the user.
        /// </summary>
        /// <exception cref="System.Management.Automation.PSArgumentNullException">
        /// set to null.
        /// </exception>
        /// <remarks>
        /// Note that the special character &amp; (ampersand) may be embedded in the label string to identify the next
        /// character in the label as a "hot key" (aka "keyboard accelerator") that the
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/> implementation may use
        /// to allow the user to quickly set input focus to this field.  The implementation of
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/> is responsible for parsing
        /// the label string for this special character and rendering it accordingly.
        ///
        /// For example, a field named "SSN" might have "&amp;Social Security Number" as it's label.
        ///
        /// If no label is set, then the empty string is returned.
        /// </remarks>

        public
        string
        Label
        {
            get
            {
                Dbg.Assert(label != null, "label should not be null");

                return label;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                label = value;
            }
        }

        /// <summary>
        /// Gets and sets the help message for this field.
        /// </summary>
        /// <exception cref="System.Management.Automation.PSArgumentNullException">
        /// Set to null.
        /// </exception>
        /// <remarks>
        /// This should be a few sentences to describe the field, suitable for presentation as a tool tip.
        /// Avoid placing including formatting characters such as newline and tab.
        /// </remarks>

        public
        string
        HelpMessage
        {
            get
            {
                Dbg.Assert(helpMessage != null, "helpMessage should not be null");

                return helpMessage;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                helpMessage = value;
            }
        }

        /// <summary>
        /// Gets and sets whether a value must be supplied for this field.
        /// </summary>

        public
        bool
        IsMandatory
        {
            get
            {
                return isMandatory;
            }

            set
            {
                isMandatory = value;
            }
        }

        /// <summary>
        /// Gets and sets the default value, if any, for the implementation of <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        /// to pre-populate its UI with. This is a PSObject instance so that the value can be serialized, converted,
        /// manipulated like any pipeline object.
        /// </summary>
        ///<remarks>
        /// It is up to the implementer of <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/> to decide if it
        /// can make use of the object in its presentation of the fields prompt.
        ///
        ///</remarks>

        public
        PSObject
        DefaultValue
        {
            get
            {
                return defaultValue;
            }

            set
            {
                // null is allowed.

                defaultValue = value;
            }
        }

        /// <summary>
        /// Gets the Attribute classes that apply to the field. In the case that <seealso cref="System.Management.Automation.Host.PSHostUserInterface.Prompt"/>
        /// is being called from the MSH engine, this will contain the set of prompting attributes that are attached to a
        /// cmdlet parameter declaration.
        /// </summary>

        public
        Collection<Attribute>
        Attributes
        {
            get { return metadata ?? (metadata = new Collection<Attribute>()); }
        }

        /// <summary>
        /// For use by remoting serialization.
        /// </summary>
        /// <param name="nameOfType"></param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// If <paramref name="nameOfType"/> is null.
        /// </exception>

        internal
        void
        SetParameterTypeName(string nameOfType)
        {
            if (string.IsNullOrEmpty(nameOfType))
            {
                throw PSTraceSource.NewArgumentException("nameOfType", DescriptionsStrings.NullOrEmptyErrorTemplate, "nameOfType");
            }

            parameterTypeName = nameOfType;
        }

        /// <summary>
        /// For use by remoting serialization.
        /// </summary>
        /// <param name="fullNameOfType"></param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// If <paramref name="fullNameOfType"/> is null.
        /// </exception>

        internal
        void
        SetParameterTypeFullName(string fullNameOfType)
        {
            if (string.IsNullOrEmpty(fullNameOfType))
            {
                throw PSTraceSource.NewArgumentException("fullNameOfType", DescriptionsStrings.NullOrEmptyErrorTemplate, "fullNameOfType");
            }

            parameterTypeFullName = fullNameOfType;
        }

        /// <summary>
        /// For use by remoting serialization.
        /// </summary>
        /// <param name="fullNameOfAssembly"></param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// If <paramref name="fullNameOfAssembly"/> is null.
        /// </exception>

        internal
        void
        SetParameterAssemblyFullName(string fullNameOfAssembly)
        {
            if (string.IsNullOrEmpty(fullNameOfAssembly))
            {
                throw PSTraceSource.NewArgumentException("fullNameOfAssembly", DescriptionsStrings.NullOrEmptyErrorTemplate, "fullNameOfAssembly");
            }

            parameterAssemblyFullName = fullNameOfAssembly;
        }

        /// <summary>
        /// Indicates if this field description was
        /// modified by the remoting protocol layer.
        /// </summary>
        /// <remarks>Used by the console host to
        /// determine if this field description was
        /// modified by the remoting protocol layer
        /// and take appropriate actions</remarks>
        internal bool ModifiedByRemotingProtocol
        {
            get
            {
                return modifiedByRemotingProtocol;
            }

            set
            {
                modifiedByRemotingProtocol = value;
            }
        }

        /// <summary>
        /// Indicates if this field description
        /// is coming from a remote host.
        /// </summary>
        /// <remarks>Used by the console host to
        /// not cast strings to an arbitrary type,
        /// but let the server-side do the type conversion
        /// </remarks>
        internal bool IsFromRemoteHost
        {
            get
            {
                return isFromRemoteHost;
            }

            set
            {
                isFromRemoteHost = value;
            }
        }

        #region Helper
        #endregion Helper

        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private readonly string name = null;
        private string label = string.Empty;
        private string parameterTypeName = null;
        private string parameterTypeFullName = null;
        private string parameterAssemblyFullName = null;
        private string helpMessage = string.Empty;
        private bool isMandatory = true;

        private PSObject defaultValue = null;
        private Collection<Attribute> metadata = new Collection<Attribute>();
        private bool modifiedByRemotingProtocol = false;
        private bool isFromRemoteHost = false;

        #endregion
    }
}

