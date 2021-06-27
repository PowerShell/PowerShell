// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace System.Management.Automation
{
    /// <summary>
    /// The metadata associated with a parameter.
    /// </summary>
    internal class CompiledCommandParameter
    {
        #region ctor

        /// <summary>
        /// Constructs an instance of the CompiledCommandAttribute using the specified
        /// runtime-defined parameter.
        /// </summary>
        /// <param name="runtimeDefinedParameter">
        /// A runtime defined parameter that contains the definition of the parameter and its metadata.
        /// </param>
        /// <param name="processingDynamicParameters">
        /// True if dynamic parameters are being processed, or false otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="runtimeDefinedParameter"/> is null.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If the parameter has more than one <see cref="ParameterAttribute">ParameterAttribute</see>
        /// that defines the same parameter-set name.
        /// </exception>
        internal CompiledCommandParameter(RuntimeDefinedParameter runtimeDefinedParameter, bool processingDynamicParameters)
        {
            if (runtimeDefinedParameter == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runtimeDefinedParameter));
            }

            this.Name = runtimeDefinedParameter.Name;
            this.Type = runtimeDefinedParameter.ParameterType;
            this.IsDynamic = processingDynamicParameters;

            this.CollectionTypeInformation = new ParameterCollectionTypeInformation(runtimeDefinedParameter.ParameterType);

            this.CompiledAttributes = new Collection<Attribute>();

            this.ParameterSetData = new Dictionary<string, ParameterSetSpecificMetadata>(StringComparer.OrdinalIgnoreCase);

            Collection<ValidateArgumentsAttribute> validationAttributes = null;
            Collection<ArgumentTransformationAttribute> argTransformationAttributes = null;
            string[] aliases = null;

            // First, process attributes that aren't type conversions
            foreach (Attribute attribute in runtimeDefinedParameter.Attributes)
            {
                if (processingDynamicParameters)
                {
                    // When processing dynamic parameters, the attribute list may contain experimental attributes
                    // and disabled parameter attributes. We should ignore those attributes.
                    // When processing non-dynamic parameters, the experimental attributes and disabled parameter
                    // attributes have already been filtered out when constructing the RuntimeDefinedParameter.
                    if (attribute is ExperimentalAttribute || attribute is ParameterAttribute param && param.ToHide)
                    {
                        continue;
                    }
                }

                if (attribute is not ArgumentTypeConverterAttribute)
                {
                    ProcessAttribute(runtimeDefinedParameter.Name, attribute, ref validationAttributes, ref argTransformationAttributes, ref aliases);
                }
            }

            // If this is a PSCredential type and they haven't added any argument transformation attributes,
            // add one for credential transformation
            if ((this.Type == typeof(PSCredential)) && argTransformationAttributes == null)
            {
                ProcessAttribute(runtimeDefinedParameter.Name, new CredentialAttribute(), ref validationAttributes, ref argTransformationAttributes, ref aliases);
            }

            // Now process type converters
            foreach (var attribute in runtimeDefinedParameter.Attributes.OfType<ArgumentTypeConverterAttribute>())
            {
                ProcessAttribute(runtimeDefinedParameter.Name, attribute, ref validationAttributes, ref argTransformationAttributes, ref aliases);
            }

            this.ValidationAttributes = validationAttributes == null
                ? Array.Empty<ValidateArgumentsAttribute>()
                : validationAttributes.ToArray();
            this.ArgumentTransformationAttributes = argTransformationAttributes == null
                ? Array.Empty<ArgumentTransformationAttribute>()
                : argTransformationAttributes.ToArray();
            this.Aliases = aliases == null
                ? Array.Empty<string>()
                : aliases.ToArray();
        }

        /// <summary>
        /// Constructs an instance of the CompiledCommandAttribute using the reflection information retrieved
        /// from the enclosing bindable object type.
        /// </summary>
        /// <param name="member">
        /// The member information for the parameter
        /// </param>
        /// <param name="processingDynamicParameters">
        /// True if dynamic parameters are being processed, or false otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="member"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="member"/> is not a field or a property.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If the member has more than one <see cref="ParameterAttribute">ParameterAttribute</see>
        /// that defines the same parameter-set name.
        /// </exception>
        internal CompiledCommandParameter(MemberInfo member, bool processingDynamicParameters)
        {
            if (member == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(member));
            }

            this.Name = member.Name;
            this.DeclaringType = member.DeclaringType;
            this.IsDynamic = processingDynamicParameters;

            var propertyInfo = member as PropertyInfo;
            if (propertyInfo != null)
            {
                this.Type = propertyInfo.PropertyType;
            }
            else
            {
                var fieldInfo = member as FieldInfo;
                if (fieldInfo != null)
                {
                    this.Type = fieldInfo.FieldType;
                }
                else
                {
                    ArgumentException e =
                    PSTraceSource.NewArgumentException(
                        nameof(member),
                        DiscoveryExceptions.CompiledCommandParameterMemberMustBeFieldOrProperty);

                    throw e;
                }
            }

            this.CollectionTypeInformation = new ParameterCollectionTypeInformation(this.Type);
            this.CompiledAttributes = new Collection<Attribute>();
            this.ParameterSetData = new Dictionary<string, ParameterSetSpecificMetadata>(StringComparer.OrdinalIgnoreCase);

            // We do not want to get the inherited custom attributes, only the attributes exposed
            // directly on the member

            var memberAttributes = member.GetCustomAttributes(false);

            Collection<ValidateArgumentsAttribute> validationAttributes = null;
            Collection<ArgumentTransformationAttribute> argTransformationAttributes = null;
            string[] aliases = null;

            foreach (Attribute attr in memberAttributes)
            {
                switch (attr)
                {
                    case ExperimentalAttribute _:
                    case ParameterAttribute param when param.ToHide:
                        break;
                    default:
                        ProcessAttribute(member.Name, attr, ref validationAttributes, ref argTransformationAttributes, ref aliases);
                        break;
                }
            }

            this.ValidationAttributes = validationAttributes == null
                ? Array.Empty<ValidateArgumentsAttribute>()
                : validationAttributes.ToArray();
            this.ArgumentTransformationAttributes = argTransformationAttributes == null
                ? Array.Empty<ArgumentTransformationAttribute>()
                : argTransformationAttributes.ToArray();
            this.Aliases = aliases ?? Array.Empty<string>();
        }

        #endregion ctor

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// The PSTypeName from a PSTypeNameAttribute.
        /// </summary>
        internal string PSTypeName { get; private set; }

        /// <summary>
        /// Gets the Type information of the attribute.
        /// </summary>
        internal Type Type { get; }

        /// <summary>
        /// Gets the Type information of the attribute.
        /// </summary>
        internal Type DeclaringType { get; }

        /// <summary>
        /// Gets whether the parameter is a dynamic parameter or not.
        /// </summary>
        internal bool IsDynamic { get; }

        /// <summary>
        /// Gets the parameter collection type information.
        /// </summary>
        internal ParameterCollectionTypeInformation CollectionTypeInformation { get; }

        /// <summary>
        /// A collection of the attributes found on the member. The attributes have been compiled into
        /// a format that easier to digest by the metadata processor.
        /// </summary>
        internal Collection<Attribute> CompiledAttributes { get; }

        /// <summary>
        /// Gets the collection of data generation attributes on this parameter.
        /// </summary>
        internal ArgumentTransformationAttribute[] ArgumentTransformationAttributes { get; }

        /// <summary>
        /// Gets the collection of data validation attributes on this parameter.
        /// </summary>
        internal ValidateArgumentsAttribute[] ValidationAttributes { get; }

        /// <summary>
        /// Get and private set the obsolete attribute on this parameter.
        /// </summary>
        internal ObsoleteAttribute ObsoleteAttribute { get; private set; }

        /// <summary>
        /// If true, null can be bound to the parameter even if the parameter is mandatory.
        /// </summary>
        internal bool AllowsNullArgument { get; private set; }

        /// <summary>
        /// If true, null cannot be bound to the parameter (ValidateNotNull
        /// and/or ValidateNotNullOrEmpty has been specified).
        /// </summary>
        internal bool CannotBeNull { get; private set; }

        /// <summary>
        /// If true, an empty string can be bound to the string parameter
        /// even if the parameter is mandatory.
        /// </summary>
        internal bool AllowsEmptyStringArgument { get; private set; }

        /// <summary>
        /// If true, an empty collection can be bound to the collection/array parameter
        /// even if the parameter is mandatory.
        /// </summary>
        internal bool AllowsEmptyCollectionArgument { get; private set; }

        /// <summary>
        /// Gets or sets the value that tells whether this parameter
        /// is for the "all" parameter set.
        /// </summary>
        internal bool IsInAllSets { get; set; }

        /// <summary>
        /// Returns true if this parameter is ValueFromPipeline or ValueFromPipelineByPropertyName
        /// in one or more (but not necessarily all) parameter sets.
        /// </summary>
        internal bool IsPipelineParameterInSomeParameterSet { get; private set; }

        /// <summary>
        /// Returns true if this parameter is Mandatory in one or more (but not necessarily all) parameter sets.
        /// </summary>
        internal bool IsMandatoryInSomeParameterSet { get; private set; }

        /// <summary>
        /// Gets or sets the parameter set flags that map the parameter sets
        /// for this parameter to the parameter set names.
        /// </summary>
        /// <remarks>
        /// This is a bit-field that maps the parameter sets in this parameter
        /// to the parameter sets for the rest of the command.
        /// </remarks>
        internal uint ParameterSetFlags { get; set; }

        /// <summary>
        /// A delegate that can set the property.
        /// </summary>
        internal Action<object, object> Setter { get; set; }

        /// <summary>
        /// A dictionary of the parameter sets and the parameter set specific data for this parameter.
        /// </summary>
        internal Dictionary<string, ParameterSetSpecificMetadata> ParameterSetData { get; }

        /// <summary>
        /// The alias names for this parameter.
        /// </summary>
        internal string[] Aliases { get; }

        /// <summary>
        /// Determines if this parameter takes pipeline input for any of the specified
        /// parameter set flags.
        /// </summary>
        /// <param name="validParameterSetFlags">
        /// The flags for the parameter sets to check to see if the parameter takes
        /// pipeline input.
        /// </param>
        /// <returns>
        /// True if the parameter takes pipeline input in any of the specified parameter
        /// sets, or false otherwise.
        /// </returns>
        internal bool DoesParameterSetTakePipelineInput(uint validParameterSetFlags)
        {
            if (!IsPipelineParameterInSomeParameterSet)
            {
                return false;
            }

            // Loop through each parameter set the parameter is in to see if that parameter set is
            // still valid.  If so, and the parameter takes pipeline input in that parameter set,
            // then return true

            foreach (ParameterSetSpecificMetadata parameterSetData in ParameterSetData.Values)
            {
                if ((parameterSetData.IsInAllSets ||
                    (parameterSetData.ParameterSetFlag & validParameterSetFlags) != 0) &&
                    (parameterSetData.ValueFromPipeline ||
                     parameterSetData.ValueFromPipelineByPropertyName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the parameter set data for this parameter for the specified parameter set.
        /// </summary>
        /// <param name="parameterSetFlag">
        /// The parameter set to get the parameter set data for.
        /// </param>
        /// <returns>
        /// The parameter set specified data for the specified parameter set.
        /// </returns>
        internal ParameterSetSpecificMetadata GetParameterSetData(uint parameterSetFlag)
        {
            ParameterSetSpecificMetadata result = null;

            foreach (ParameterSetSpecificMetadata setData in ParameterSetData.Values)
            {
                // If the parameter is in all sets, then remember the data, but
                // try to find a more specific match

                if (setData.IsInAllSets)
                {
                    result = setData;
                }
                else
                {
                    if ((setData.ParameterSetFlag & parameterSetFlag) != 0)
                    {
                        result = setData;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the parameter set data for this parameter for the specified parameter sets.
        /// </summary>
        /// <param name="parameterSetFlags">
        /// The parameter sets to get the parameter set data for.
        /// </param>
        /// <returns>
        /// A collection for all parameter set specified data for the parameter sets specified by
        /// the <paramref name="parameterSetFlags"/>.
        /// </returns>
        internal IEnumerable<ParameterSetSpecificMetadata> GetMatchingParameterSetData(uint parameterSetFlags)
        {
            foreach (ParameterSetSpecificMetadata setData in ParameterSetData.Values)
            {
                // If the parameter is in all sets, then remember the data, but
                // try to find a more specific match

                if (setData.IsInAllSets)
                {
                    yield return setData;
                }
                else
                {
                    if ((setData.ParameterSetFlag & parameterSetFlags) != 0)
                    {
                        yield return setData;
                    }
                }
            }
        }

        #region helper methods

        /// <summary>
        /// Processes the Attribute metadata to generate a CompiledCommandAttribute.
        /// </summary>
        /// <exception cref="MetadataException">
        /// If the attribute is a parameter attribute and another parameter attribute
        /// has been processed with the same parameter-set name.
        /// </exception>
        private void ProcessAttribute(
            string memberName,
            Attribute attribute,
            ref Collection<ValidateArgumentsAttribute> validationAttributes,
            ref Collection<ArgumentTransformationAttribute> argTransformationAttributes,
            ref string[] aliases)
        {
            if (attribute == null)
                return;

            CompiledAttributes.Add(attribute);

            // Now process the attribute based on it's type
            if (attribute is ParameterAttribute paramAttr)
            {
                ProcessParameterAttribute(memberName, paramAttr);
                return;
            }

            ValidateArgumentsAttribute validateAttr = attribute as ValidateArgumentsAttribute;
            if (validateAttr != null)
            {
                if (validationAttributes == null)
                    validationAttributes = new Collection<ValidateArgumentsAttribute>();
                validationAttributes.Add(validateAttr);
                if ((attribute is ValidateNotNullAttribute) || (attribute is ValidateNotNullOrEmptyAttribute))
                {
                    this.CannotBeNull = true;
                }

                return;
            }

            AliasAttribute aliasAttr = attribute as AliasAttribute;
            if (aliasAttr != null)
            {
                if (aliases == null)
                {
                    aliases = aliasAttr.aliasNames;
                }
                else
                {
                    var prevAliasNames = aliases;
                    var newAliasNames = aliasAttr.aliasNames;
                    aliases = new string[prevAliasNames.Length + newAliasNames.Length];
                    Array.Copy(prevAliasNames, aliases, prevAliasNames.Length);
                    Array.Copy(newAliasNames, 0, aliases, prevAliasNames.Length, newAliasNames.Length);
                }

                return;
            }

            ArgumentTransformationAttribute argumentAttr = attribute as ArgumentTransformationAttribute;
            if (argumentAttr != null)
            {
                if (argTransformationAttributes == null)
                    argTransformationAttributes = new Collection<ArgumentTransformationAttribute>();
                argTransformationAttributes.Add(argumentAttr);
                return;
            }

            AllowNullAttribute allowNullAttribute = attribute as AllowNullAttribute;
            if (allowNullAttribute != null)
            {
                this.AllowsNullArgument = true;
                return;
            }

            AllowEmptyStringAttribute allowEmptyStringAttribute = attribute as AllowEmptyStringAttribute;
            if (allowEmptyStringAttribute != null)
            {
                this.AllowsEmptyStringArgument = true;
                return;
            }

            AllowEmptyCollectionAttribute allowEmptyCollectionAttribute = attribute as AllowEmptyCollectionAttribute;
            if (allowEmptyCollectionAttribute != null)
            {
                this.AllowsEmptyCollectionArgument = true;
                return;
            }

            ObsoleteAttribute obsoleteAttr = attribute as ObsoleteAttribute;
            if (obsoleteAttr != null)
            {
                ObsoleteAttribute = obsoleteAttr;
                return;
            }

            PSTypeNameAttribute psTypeNameAttribute = attribute as PSTypeNameAttribute;
            if (psTypeNameAttribute != null)
            {
                this.PSTypeName = psTypeNameAttribute.PSTypeName;
            }
        }

        /// <summary>
        /// Extracts the data from the ParameterAttribute and creates the member data as necessary.
        /// </summary>
        /// <param name="parameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="parameter">
        /// The instance of the ParameterAttribute to extract the data from.
        /// </param>
        /// <exception cref="MetadataException">
        /// If a parameter set name has already been declared on this parameter.
        /// </exception>
        private void ProcessParameterAttribute(
            string parameterName,
            ParameterAttribute parameter)
        {
            // If the parameter set name already exists on this parameter and the set name is the default parameter
            // set name, it is an error.

            if (ParameterSetData.ContainsKey(parameter.ParameterSetName))
            {
                MetadataException e =
                    new MetadataException(
                        "ParameterDeclaredInParameterSetMultipleTimes",
                        null,
                        DiscoveryExceptions.ParameterDeclaredInParameterSetMultipleTimes,
                        parameterName,
                        parameter.ParameterSetName);

                throw e;
            }

            if (parameter.ValueFromPipeline || parameter.ValueFromPipelineByPropertyName)
            {
                IsPipelineParameterInSomeParameterSet = true;
            }

            if (parameter.Mandatory)
            {
                IsMandatoryInSomeParameterSet = true;
            }

            // Construct an instance of the parameter set specific data
            ParameterSetSpecificMetadata parameterSetSpecificData = new ParameterSetSpecificMetadata(parameter);
            ParameterSetData.Add(parameter.ParameterSetName, parameterSetSpecificData);
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion helper methods
    }

    /// <summary>
    /// The types of collections that are supported as parameter types.
    /// </summary>
    internal enum ParameterCollectionType
    {
        NotCollection,
        IList,
        Array,
        ICollectionGeneric
    }

    /// <summary>
    /// Contains the collection type information for a parameter.
    /// </summary>
    internal class ParameterCollectionTypeInformation
    {
        /// <summary>
        /// Constructs a parameter collection type information object
        /// which exposes the specified Type's collection type in a
        /// simple way.
        /// </summary>
        /// <param name="type">
        /// The type to determine the collection information for.
        /// </param>
        internal ParameterCollectionTypeInformation(Type type)
        {
            ParameterCollectionType = ParameterCollectionType.NotCollection;
            Diagnostics.Assert(type != null, "Caller to verify type argument");

            // NTRAID#Windows OS Bugs-1009284-2004/05/11-JeffJon
            // What other collection types should be supported?

            // Look for array types

            // NTRAID#Windows Out of Band Releases-906820-2005/09/07
            // According to MSDN, IsSubclassOf returns false if the types are exactly equal.
            // Should this include ==?
            if (type.IsSubclassOf(typeof(Array)))
            {
                ParameterCollectionType = ParameterCollectionType.Array;
                ElementType = type.GetElementType();
                return;
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return;
            }

            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                return;
            }

            bool implementsIList = (type.GetInterface(nameof(IList)) != null);

            // Look for class Collection<T>.  Collection<T> implements IList, and also IList
            // is more efficient to bind than ICollection<T>.  This optimization
            // retrieves the element type so that we can coerce the elements.
            // Otherwise they must already be the right type.
            if (implementsIList && type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Collection<>)))
            {
                ParameterCollectionType = ParameterCollectionType.IList;
                // figure out elementType
                Type[] elementTypes = type.GetGenericArguments();
                Diagnostics.Assert(
                    elementTypes.Length == 1,
                    "Expected 1 generic argument, got " + elementTypes.Length);
                ElementType = elementTypes[0];
                return;
            }

            // Look for interface ICollection<T>.  Note that Collection<T>
            // does not implement ICollection<T>, and also, ICollection<T>
            // does not derive from IList.  The only way to add elements
            // to an ICollection<T> is via reflected calls to Add(T),
            // but the advantage over plain IList is that we can typecast the elements.
            Type interfaceICollection =
                Array.Find(interfaces, static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (interfaceICollection != null)
            {
                // We only deal with the first type for which ICollection<T> is implemented
                ParameterCollectionType = ParameterCollectionType.ICollectionGeneric;
                // figure out elementType
                Type[] elementTypes = interfaceICollection.GetGenericArguments();
                Diagnostics.Assert(
                    elementTypes.Length == 1,
                    "Expected 1 generic argument, got " + elementTypes.Length);
                ElementType = elementTypes[0];
                return;
            }

            // Look for IList
            if (implementsIList)
            {
                ParameterCollectionType = ParameterCollectionType.IList;
                // elementType remains null
                return;
            }
        }

        /// <summary>
        /// The collection type of the parameter.
        /// </summary>
        internal ParameterCollectionType ParameterCollectionType { get; }

        /// <summary>
        /// The type of the elements in the collection.
        /// </summary>
        internal Type ElementType { get; }
    }
}
