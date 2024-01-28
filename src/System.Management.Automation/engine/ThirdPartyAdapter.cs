// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Management.Automation
{
    /// <summary>
    /// Internal wrapper for third-party adapters (PSPropertyAdapter)
    /// </summary>
    internal sealed class ThirdPartyAdapter : PropertyOnlyAdapter
    {
        internal ThirdPartyAdapter(Type adaptedType, PSPropertyAdapter externalAdapter)
        {
            AdaptedType = adaptedType;
            _externalAdapter = externalAdapter;
        }

        /// <summary>
        /// The type this instance is adapting.
        /// </summary>
        internal Type AdaptedType { get; }

        /// <summary>
        /// The type of the external adapter.
        /// </summary>
        internal Type ExternalAdapterType
        {
            get
            {
                return _externalAdapter.GetType();
            }
        }

        /// <summary>
        /// Returns the TypeNameHierarchy out of an object.
        /// </summary>
        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            Collection<string> typeNameHierarchy = null;

            try
            {
                typeNameHierarchy = _externalAdapter.GetTypeNameHierarchy(obj);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.GetTypeNameHierarchyError",
                    exception,
                    ExtendedTypeSystem.GetTypeNameHierarchyError, obj.ToString());
            }

            if (typeNameHierarchy == null)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.NullReturnValueError",
                    null,
                    ExtendedTypeSystem.NullReturnValueError, "PSPropertyAdapter.GetTypeNameHierarchy");
            }

            return typeNameHierarchy;
        }

        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        protected override void DoAddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members)
        {
            Collection<PSAdaptedProperty> properties = null;

            try
            {
                properties = _externalAdapter.GetProperties(obj);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.GetProperties",
                    exception,
                    ExtendedTypeSystem.GetProperties, obj.ToString());
            }

            if (properties == null)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.NullReturnValueError",
                    null,
                    ExtendedTypeSystem.NullReturnValueError, "PSPropertyAdapter.GetProperties");
            }

            foreach (PSAdaptedProperty property in properties)
            {
                InitializeProperty(property, obj);

                members.Add(property as T);
            }
        }

        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        protected override PSProperty DoGetProperty(object obj, string propertyName)
        {
            PSAdaptedProperty property = null;

            try
            {
                property = _externalAdapter.GetProperty(obj, propertyName);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.GetProperty",
                    exception,
                    ExtendedTypeSystem.GetProperty, propertyName, obj.ToString());
            }

            if (property != null)
            {
                InitializeProperty(property, obj);
            }

            return property;
        }

        protected override PSProperty DoGetFirstPropertyOrDefault(object obj, MemberNamePredicate predicate)
        {
            PSAdaptedProperty property = null;

            try
            {
                property = _externalAdapter.GetFirstPropertyOrDefault(obj, predicate);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.GetProperty",
                    exception,
                    ExtendedTypeSystem.GetProperty, nameof(predicate), obj.ToString());
            }

            if (property != null)
            {
                InitializeProperty(property, obj);
            }

            return property;
        }

        /// <summary>
        /// Ensures that the adapter and base object are set in the given PSAdaptedProperty.
        /// </summary>
        private void InitializeProperty(PSAdaptedProperty property, object baseObject)
        {
            if (property.adapter == null)
            {
                property.adapter = this;
                property.baseObject = baseObject;
            }
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            PSAdaptedProperty adaptedProperty = property as PSAdaptedProperty;

            Diagnostics.Assert(adaptedProperty != null, "ThirdPartyAdapter should only receive PSAdaptedProperties");

            try
            {
                return _externalAdapter.IsSettable(adaptedProperty);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.PropertyIsSettableError",
                    exception,
                    ExtendedTypeSystem.PropertyIsSettableError, property.Name);
            }
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            PSAdaptedProperty adaptedProperty = property as PSAdaptedProperty;

            Diagnostics.Assert(adaptedProperty != null, "ThirdPartyAdapter should only receive PSAdaptedProperties");

            try
            {
                return _externalAdapter.IsGettable(adaptedProperty);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.PropertyIsGettableError",
                    exception,
                    ExtendedTypeSystem.PropertyIsGettableError, property.Name);
            }
        }

        /// <summary>
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        protected override object PropertyGet(PSProperty property)
        {
            PSAdaptedProperty adaptedProperty = property as PSAdaptedProperty;

            Diagnostics.Assert(adaptedProperty != null, "ThirdPartyAdapter should only receive PSAdaptedProperties");

            try
            {
                return _externalAdapter.GetPropertyValue(adaptedProperty);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.PropertyGetError",
                    exception,
                    ExtendedTypeSystem.PropertyGetError, property.Name);
            }
        }

        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// </summary>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            PSAdaptedProperty adaptedProperty = property as PSAdaptedProperty;

            Diagnostics.Assert(adaptedProperty != null, "ThirdPartyAdapter should only receive PSAdaptedProperties");

            try
            {
                _externalAdapter.SetPropertyValue(adaptedProperty, setValue);
            }
            catch (SetValueException) { throw; }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.PropertySetError",
                    exception,
                    ExtendedTypeSystem.PropertySetError, property.Name);
            }
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            PSAdaptedProperty adaptedProperty = property as PSAdaptedProperty;

            Diagnostics.Assert(adaptedProperty != null, "ThirdPartyAdapter should only receive PSAdaptedProperties");

            string propertyTypeName = null;

            try
            {
                propertyTypeName = _externalAdapter.GetPropertyTypeName(adaptedProperty);
            }
            catch (Exception exception)
            {
                throw new ExtendedTypeSystemException(
                    "PSPropertyAdapter.PropertyTypeError",
                    exception,
                    ExtendedTypeSystem.PropertyTypeError, property.Name);
            }

            return propertyTypeName ?? "System.Object";
        }

        private readonly PSPropertyAdapter _externalAdapter;
    }

    /// <summary>
    /// User-defined property adapter.
    /// </summary>
    /// <remarks>
    /// This class is used to expose a simplified version of the type adapter API
    /// </remarks>
    public abstract class PSPropertyAdapter
    {
        /// <summary>
        /// Returns the type hierarchy for the given object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object")]
        public virtual Collection<string> GetTypeNameHierarchy(object baseObject)
        {
            ArgumentNullException.ThrowIfNull(baseObject);

            Collection<string> types = new Collection<string>();

            for (Type type = baseObject.GetType(); type != null; type = type.BaseType)
            {
                types.Add(type.FullName);
            }

            return types;
        }

        /// <summary>
        /// Returns a list of the adapted properties.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object")]
        public abstract Collection<PSAdaptedProperty> GetProperties(object baseObject);

        /// <summary>
        /// Returns a specific property, or null if the base object does not contain the given property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object")]
        public abstract PSAdaptedProperty GetProperty(object baseObject, string propertyName);

        /// <summary>
        /// Returns true if the given property is settable.
        /// </summary>
        public abstract bool IsSettable(PSAdaptedProperty adaptedProperty);

        /// <summary>
        /// Returns true if the given property is gettable.
        /// </summary>
        public abstract bool IsGettable(PSAdaptedProperty adaptedProperty);

        /// <summary>
        /// Returns the value of a given property.
        /// </summary>
        public abstract object GetPropertyValue(PSAdaptedProperty adaptedProperty);

        /// <summary>
        /// Sets the value of a given property.
        /// </summary>
        public abstract void SetPropertyValue(PSAdaptedProperty adaptedProperty, object value);

        /// <summary>
        /// Returns the type for a given property.
        /// </summary>
        public abstract string GetPropertyTypeName(PSAdaptedProperty adaptedProperty);

        /// <summary>
        /// Returns a property if it's name matches the specified <see cref="MemberNamePredicate"/>, otherwise null.
        /// </summary>
        /// <returns>An adapted property if the predicate matches, or <see langword="null"/>.</returns>
        public virtual PSAdaptedProperty GetFirstPropertyOrDefault(object baseObject, MemberNamePredicate predicate)
        {
            foreach (var property in GetProperties(baseObject))
            {
                if (predicate(property.Name))
                {
                    return property;
                }
            }

            return null;
        }
    }
}
