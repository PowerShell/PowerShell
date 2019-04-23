// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using Microsoft.PowerShell;

namespace System.Management.Automation
{
    /// <summary>
    /// Implements Adapter for the COM objects.
    /// </summary>
    internal class ComAdapter : Adapter
    {
        private readonly ComTypeInfo _comTypeInfo;

        /// <summary>
        ///   Constructor for the ComAdapter.
        /// </summary>
        /// <param name="typeinfo">Typeinfo for the com object we are adapting.</param>
        internal ComAdapter(ComTypeInfo typeinfo)
        {
            Diagnostics.Assert(typeinfo != null, "Caller to verify typeinfo is not null.");
            _comTypeInfo = typeinfo;
        }

        internal static string GetComTypeName(string clsid)
        {
            StringBuilder firstType = new StringBuilder("System.__ComObject");
            firstType.Append("#{");
            firstType.Append(clsid);
            firstType.Append("}");
            return firstType.ToString();
        }

        /// <summary>
        /// Returns the TypeNameHierarchy out of an object.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            yield return GetComTypeName(_comTypeInfo.Clsid);
            foreach (string baseType in GetDotNetTypeNameHierarchy(obj))
            {
                yield return baseType;
            }
        }

        /// <summary>
        /// Returns null if memberName is not a member in the adapter or
        /// the corresponding PSMemberInfo.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="memberName">Name of the member to be retrieved.</param>
        /// <returns>The PSMemberInfo corresponding to memberName from obj.</returns>
        protected override T GetMember<T>(object obj, string memberName)
        {
            ComProperty prop;
            if (_comTypeInfo.Properties.TryGetValue(memberName, out prop))
            {
                if (prop.IsParameterized)
                {
                    if (typeof(T).IsAssignableFrom(typeof(PSParameterizedProperty)))
                    {
                        return new PSParameterizedProperty(prop.Name, this, obj, prop) as T;
                    }
                }
                else if (typeof(T).IsAssignableFrom(typeof(PSProperty)))
                {
                    return new PSProperty(prop.Name, this, obj, prop) as T;
                }
            }

            ComMethod method;
            if (typeof(T).IsAssignableFrom(typeof(PSMethod)) &&
                (_comTypeInfo != null) && (_comTypeInfo.Methods.TryGetValue(memberName, out method)))
            {
                PSMethod mshMethod = new PSMethod(method.Name, this, obj, method);
                return mshMethod as T;
            }

            return null;
        }

        /// <summary>
        /// Returns the first PSMemberInfo whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// </summary>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = typeof(T).IsAssignableFrom(typeof(PSParameterizedProperty));
            if (lookingForProperties || lookingForParameterizedProperties)
            {
                foreach (ComProperty prop in _comTypeInfo.Properties.Values)
                {
                    if (prop.IsParameterized
                        && lookingForParameterizedProperties
                        && predicate(prop.Name))
                    {
                        return new PSParameterizedProperty(prop.Name, this, obj, prop) as T;
                    }

                    if (lookingForProperties && predicate(prop.Name))
                    {
                        return new PSProperty(prop.Name, this, obj, prop) as T;
                    }
                }
            }

            bool lookingForMethods = typeof(T).IsAssignableFrom(typeof(PSMethod));

            if (lookingForMethods)
            {
                foreach (ComMethod method in _comTypeInfo.Methods.Values)
                {
                    if (predicate(method.Name))
                    {
                        var mshMethod = new PSMethod(method.Name, this, obj, method);
                        return mshMethod as T;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves all the members available in the object.
        /// The adapter implementation is encouraged to cache all properties/methods available
        /// in the first call to GetMember and GetMembers so that subsequent
        /// calls can use the cache.
        /// In the case of the .NET adapter that would be a cache from the .NET type to
        /// the public properties and fields available in that type.
        /// In the case of the DirectoryEntry adapter, this could be a cache of the objectClass
        /// to the properties available in it.
        /// </summary>
        /// <param name="obj">Object to get all the member information from.</param>
        /// <returns>All members in obj.</returns>
        protected override PSMemberInfoInternalCollection<T> GetMembers<T>(object obj)
        {
            PSMemberInfoInternalCollection<T> collection = new PSMemberInfoInternalCollection<T>();

            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = typeof(T).IsAssignableFrom(typeof(PSParameterizedProperty));
            if (lookingForProperties || lookingForParameterizedProperties)
            {
                foreach (ComProperty prop in _comTypeInfo.Properties.Values)
                {
                    if (prop.IsParameterized)
                    {
                        if (lookingForParameterizedProperties)
                        {
                            collection.Add(new PSParameterizedProperty(prop.Name, this, obj, prop) as T);
                        }
                    }
                    else if (lookingForProperties)
                    {
                        collection.Add(new PSProperty(prop.Name, this, obj, prop) as T);
                    }
                }
            }

            bool lookingForMethods = typeof(T).IsAssignableFrom(typeof(PSMethod));

            if (lookingForMethods)
            {
                foreach (ComMethod method in _comTypeInfo.Methods.Values)
                {
                    if (collection[method.Name] == null)
                    {
                        PSMethod mshmethod = new PSMethod(method.Name, this, obj, method);
                        collection.Add(mshmethod as T);
                    }
                }
            }

            return collection;
        }

        /// <summary>
        /// Returns an array with the property attributes.
        /// </summary>
        /// <param name="property">Property we want the attributes from.</param>
        /// <returns>An array with the property attributes.</returns>
        protected override AttributeCollection PropertyAttributes(PSProperty property)
        {
            return new AttributeCollection();
        }

        /// <summary>
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.GetValue(property.baseObject);
        }

        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <param name="setValue">Value to set the property with.</param>
        ///  <param name="convertIfPossible">instructs the adapter to convert before setting, if the adapter supports conversion</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            prop.SetValue(property.baseObject, setValue);
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.IsSettable;
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.IsGettable;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous DoGetProperty.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the property.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return forDisplay ? ToStringCodeMethods.Type(prop.Type) : prop.Type.FullName;
        }

        /// <summary>
        /// Get the property signature.
        /// </summary>
        /// <param name="property">Property object whose signature we want.</param>
        /// <returns>String representing the signature of the property.</returns>
        protected override string PropertyToString(PSProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.ToString();
        }

        #region Methods

        /// <summary>
        /// Called after a non null return from GetMethodData to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, object[] arguments)
        {
            ComMethod commethod = (ComMethod)method.adapterData;
            return commethod.InvokeMethod(method, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMethodData to return the overloads.
        /// </summary>
        /// <param name="method">The return of GetMethodData.</param>
        /// <returns></returns>
        protected override Collection<string> MethodDefinitions(PSMethod method)
        {
            ComMethod commethod = (ComMethod)method.adapterData;
            return commethod.MethodDefinitions();
        }
        #endregion

        #region parameterized property

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected override string ParameterizedPropertyType(PSParameterizedProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.Type.FullName;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool ParameterizedPropertyIsSettable(PSParameterizedProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.IsSettable;
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool ParameterizedPropertyIsGettable(PSParameterizedProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.IsGettable;
        }

        /// <summary>
        /// Called after a non null return from GetMember to get the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the property.</returns>
        protected override object ParameterizedPropertyGet(PSParameterizedProperty property, object[] arguments)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.GetValue(property.baseObject, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to set the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="setValue">The value to set property with.</param>
        /// <param name="arguments">The arguments to use.</param>
        protected override void ParameterizedPropertySet(PSParameterizedProperty property, object setValue, object[] arguments)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            prop.SetValue(property.baseObject, setValue, arguments);
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected override string ParameterizedPropertyToString(PSParameterizedProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            return prop.ToString();
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="property">The return of GetMember.</param>
        protected override Collection<string> ParameterizedPropertyDefinitions(PSParameterizedProperty property)
        {
            ComProperty prop = (ComProperty)property.adapterData;
            Collection<string> returnValue = new Collection<string> { prop.GetDefinition() };
            return returnValue;
        }

        #endregion parameterized property
    }
}
