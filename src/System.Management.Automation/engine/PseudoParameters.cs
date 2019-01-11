// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Represents a parameter declaration that can be constructed at runtime.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="RuntimeDefinedParameterDictionary"/>
    /// should be returned to cmdlet implementations of
    /// <see cref="IDynamicParameters.GetDynamicParameters"/>.
    ///
    /// It is permitted to subclass <see cref="RuntimeDefinedParameter"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    /// <seealso cref="RuntimeDefinedParameterDictionary"/>
    /// <seealso cref="IDynamicParameters"/>
    /// <seealso cref="IDynamicParameters.GetDynamicParameters"/>
    public class RuntimeDefinedParameter
    {
        /// <summary>
        /// Constructs a runtime-defined parameter instance.
        /// </summary>
        public RuntimeDefinedParameter()
        {
        }

        /// <summary>
        /// Constructs a new instance of a runtime-defined parameter using the specified parameters.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter. This cannot be null or empty.
        /// </param>
        /// <param name="parameterType">
        /// The type of the parameter value. Arguments will be coerced to this type before binding.
        /// This parameter cannot be null.
        /// </param>
        /// <param name="attributes">
        /// Any parameter attributes that should be on the parameter. This can be any of the
        /// parameter attributes including but not limited to Validate*Attribute, ExpandWildcardAttribute, etc.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameterType"/> is null.
        /// </exception>
        public RuntimeDefinedParameter(string name, Type parameterType, Collection<Attribute> attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException("name");
            }

            if (parameterType == null)
            {
                throw PSTraceSource.NewArgumentNullException("parameterType");
            }

            _name = name;
            _parameterType = parameterType;

            if (attributes != null)
            {
                Attributes = attributes;
            }
        }

        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If <paramref name="value"/> is null or empty on set.
        /// </exception>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("name");
                }

                _name = value;
            }
        }

        private string _name = string.Empty;

        /// <summary>
        /// Gets or sets the type of the parameter.
        /// </summary>
        /// <remarks>
        /// Arguments will be coerced to this type before being bound.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="value"/> is null.
        /// </exception>
        public Type ParameterType
        {
            get
            {
                return _parameterType;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                _parameterType = value;
            }
        }

        private Type _parameterType;

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        /// <remarks>
        /// If the value is set prior to parameter binding, the value will be
        /// reset before each pipeline object is processed.
        /// </remarks>
        public object Value
        {
            get
            {
                return _value;
            }

            set
            {
                this.IsSet = true;
                _value = value;
            }
        }

        private object _value;

        /// <summary>
        /// Gets or sets whether this parameter value has been set.
        /// </summary>
        public bool IsSet { get; set; }

        /// <summary>
        /// Gets or sets the attribute collection that describes the parameter.
        /// </summary>
        /// <remarks>
        /// This can be any attribute that can be applied to a normal parameter.
        /// </remarks>
        public Collection<Attribute> Attributes { get; } = new Collection<Attribute>();

        /// <summary>
        /// Check if the parameter is disabled due to the associated experimental feature.
        /// </summary>
        internal bool IsDisabled()
        {
            bool hasParameterAttribute = false;
            bool hasEnabledParamAttribute = false;
            bool hasSeenExpAttribute = false;

            foreach (Attribute attr in Attributes)
            {
                if (!hasSeenExpAttribute && attr is ExperimentalAttribute expAttribute)
                {
                    if (expAttribute.ToHide) { return true; }

                    hasSeenExpAttribute = true;
                }
                else if (attr is ParameterAttribute paramAttribute)
                {
                    hasParameterAttribute = true;
                    if (paramAttribute.ToHide) { continue; }

                    hasEnabledParamAttribute = true;
                }
            }

            // If one or more parameter attributes are declared but none is enabled,
            // then we consider the parameter is disabled.
            return hasParameterAttribute && !hasEnabledParamAttribute;
        }
    }

    /// <summary>
    /// Represents a collection of runtime-defined parameters that are keyed based on the name
    /// of the parameter.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="RuntimeDefinedParameterDictionary"/>
    /// should be returned to cmdlet implementations of
    /// <see cref="IDynamicParameters.GetDynamicParameters"/>.
    ///
    /// It is permitted to subclass <see cref="RuntimeDefinedParameterDictionary"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    /// <seealso cref="RuntimeDefinedParameter"/>
    /// <seealso cref="IDynamicParameters"/>
    /// <seealso cref="IDynamicParameters.GetDynamicParameters"/>
    [Serializable]
    public class RuntimeDefinedParameterDictionary : Dictionary<string, RuntimeDefinedParameter>
    {
        /// <summary>
        /// Constructs a new instance of a runtime-defined parameter dictionary.
        /// </summary>
        public RuntimeDefinedParameterDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Gets or sets the help file that documents these parameters.
        /// </summary>
        public string HelpFile
        {
            get { return _helpFile; }

            set { _helpFile = string.IsNullOrEmpty(value) ? string.Empty : value; }
        }

        private string _helpFile = string.Empty;

        /// <summary>
        /// Gets or sets private data associated with the runtime-defined parameters.
        /// </summary>
        public object Data { get; set; }

        internal static RuntimeDefinedParameter[] EmptyParameterArray = new RuntimeDefinedParameter[0];
    }
}
