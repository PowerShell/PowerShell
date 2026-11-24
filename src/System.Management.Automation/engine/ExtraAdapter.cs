// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.PowerShell;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    /// <summary>
    /// Deals with DirectoryEntry objects.
    /// </summary>
    internal class DirectoryEntryAdapter : DotNetAdapter
    {
        #region private data
        // DirectoryEntry(DE) adapter needs dotnet adapter as DE adapter
        // don't know the underlying native adsi object's method metadata.
        // In the MethodInvoke() call, this adapter first calls
        // native adsi object's method, if there is a failure it calls
        // dotnet method (if one available).
        // This ensures dotnet methods are available on the adapted object.
        private static readonly DotNetAdapter s_dotNetAdapter = new DotNetAdapter();
        #endregion

        #region member

        internal override bool CanSiteBinderOptimize(MemberTypes typeToOperateOn)
        {
            return false;
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
            PSProperty property;
            DirectoryEntry entry = (DirectoryEntry)obj;

            // This line must precede InvokeGet. See the comment below.
            PropertyValueCollection collection = entry.Properties[memberName];
            object valueToTake = collection;

#pragma warning disable 56500
            // Even for the cases where propertyName does not exist
            // entry.Properties[propertyName] still returns a PropertyValueCollection.
            // The non schema way to check for a non existing property is to call entry.InvokeGet
            // and catch an eventual exception.
            // Specifically for "LDAP://RootDse" there are some cases where calling
            // InvokeGet will throw COMException for existing properties like defaultNamingContext.
            // Having a call to entry.Properties[propertyName] fixes the RootDse problem.
            // Calling entry.RefreshCache() also fixes the RootDse problem.
            try
            {
                object invokeGetValue = entry.InvokeGet(memberName);
                // if entry.Properties[memberName] returns empty value and invokeGet non-empty
                // value..take invokeGet's value. This will fix bug Windows Bug 121188.
                if ((collection == null) || ((collection.Value == null) && (invokeGetValue != null)))
                {
                    valueToTake = invokeGetValue;
                }

                property = new PSProperty(collection.PropertyName, this, obj, valueToTake);
            }
            catch (Exception)
            {
                property = null;
            }
#pragma warning restore 56500

            if (valueToTake == null)
            {
                property = null;
            }

            if (typeof(T).IsAssignableFrom(typeof(PSProperty)) && property != null)
            {
                return property as T;
            }

            if (typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                if (property == null)
                {
                    #region Readme
                    // we are unable to find a native adsi object property.
                    // The next option is to find method. Unfortunately DirectoryEntry
                    // doesn't provide us a way to access underlying native object's method
                    // metadata.
                    // Adapter engine resolve's members in the following steps:
                    //  1. Extended members -> 2. Adapted members -> 3. Dotnet members
                    // We cannot say from DirectoryEntryAdapter if a method with name "memberName"
                    // is available. So check if a DotNet property with the same name is available
                    // If yes, return null from the adapted view and let adapter engine
                    // take care of DotNet member resolution. If not, assume memberName method
                    // is available on native adsi object.
                    // In case of collisions between Dotnet Property and adsi native object methods,
                    // Dotnet wins. Looking through IADs com interfaces there doesn't appear
                    // to be a collision like this.
                    // Powershell Parser will call only GetMember<PSMemberInfo>, so here
                    // we cannot distinguish if the caller is looking for a property or a
                    // method.
                    #endregion

                    if (base.GetDotNetProperty<T>(obj, memberName) == null)
                    {
                        return new PSMethod(memberName, this, obj, null) as T;
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
            DirectoryEntry entry = (DirectoryEntry)obj;

            PSMemberInfoInternalCollection<T> members = new PSMemberInfoInternalCollection<T>();

            if (entry.Properties == null || entry.Properties.PropertyNames == null)
            {
                return null;
            }

            int countOfProperties = 0;

#pragma warning disable 56500
            try
            {
                countOfProperties = entry.Properties.PropertyNames.Count;
            }
            catch (Exception) // swallow all non-severe exceptions
            {
            }
#pragma warning restore 56500

            if (countOfProperties > 0)
            {
                foreach (PropertyValueCollection property in entry.Properties)
                {
                    members.Add(new PSProperty(property.PropertyName, this, obj, property) as T);
                }
            }

            return members;
        }

        #endregion member

        #region property

        /// <summary>
        /// Returns the value from a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            return property.adapterData;
        }

        /// <summary>
        /// Sets the value of a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            PropertyValueCollection values = property.adapterData as PropertyValueCollection;

            if (values != null)
            {
                // This means GetMember returned PropertyValueCollection
                try
                {
                    values.Clear();
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    if (e.ErrorCode != unchecked((int)0x80004005) || (setValue == null))
                        // When clear is called, DirectoryEntry calls PutEx on AD object with Clear option and Null Value
                        // WinNT provider throws E_FAIL when null value is specified though actually ADS_PROPERTY_CLEAR option is used,
                        // we need to catch this exception here.
                        // But at the same time we don't want to catch the exception if user explicitly sets the value to null.
                        throw;
                }

                IEnumerable enumValues = LanguagePrimitives.GetEnumerable(setValue);

                if (enumValues == null)
                {
                    values.Add(setValue);
                }
                else
                {
                    foreach (object objValue in enumValues)
                    {
                        values.Add(objValue);
                    }
                }
            }
            else
            {
                // This means GetMember returned the value from InvokeGet..So set the value using InvokeSet.
                DirectoryEntry entry = (DirectoryEntry)property.baseObject;
                Diagnostics.Assert(entry != null, "Object should be of type DirectoryEntry in DirectoryEntry adapter.");

                List<object> setValues = new List<object>();
                IEnumerable enumValues = LanguagePrimitives.GetEnumerable(setValue);

                if (enumValues == null)
                {
                    setValues.Add(setValue);
                }
                else
                {
                    foreach (object objValue in enumValues)
                    {
                        setValues.Add(objValue);
                    }
                }

                entry.InvokeSet(property.name, setValues.ToArray());
            }

            return;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            return true;
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            return true;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous GetMember.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            object value = null;
            try
            {
                value = BasePropertyGet(property);
            }
            catch (GetValueException)
            {
            }

            var type = value == null ? typeof(object) : value.GetType();
            return forDisplay ? ToStringCodeMethods.Type(type) : type.FullName;
        }

        #endregion property

        #region method

        protected override object MethodInvoke(PSMethod method, PSMethodInvocationConstraints invocationConstraints, object[] arguments)
        {
            return this.MethodInvoke(method, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, object[] arguments)
        {
            ParameterInformation[] parameters = new ParameterInformation[arguments.Length];

            for (int i = 0; i < arguments.Length; i++)
            {
                parameters[i] = new ParameterInformation(string.Empty, typeof(object), false, null, false);
            }

            MethodInformation[] methodInformation = new MethodInformation[1];
            methodInformation[0] = new MethodInformation(false, false, parameters);

            object[] newArguments;
            GetBestMethodAndArguments(method.Name, methodInformation, arguments, out newArguments);

            DirectoryEntry entry = (DirectoryEntry)method.baseObject;

            // First try to invoke method on the native adsi object. If the method
            // call fails, try to invoke dotnet method with same name, if one available.
            // This will ensure dotnet methods are exposed for DE objects.
            // The problem is in GetMember<T>(), DE adapter cannot check if a requested
            // method is available as it doesn't have access to native adsi object's
            // method metadata. So GetMember<T> returns PSMethod assuming a method
            // is available. This behavior will never give a chance to dotnet adapter
            // to resolve method call. So the DE adapter owns calling dotnet method
            // if one available.
            Exception exception;
            try
            {
                return entry.Invoke(method.Name, newArguments);
            }
            catch (DirectoryServicesCOMException dse)
            {
                exception = dse;
            }
            catch (TargetInvocationException tie)
            {
                exception = tie;
            }
            catch (COMException ce)
            {
                exception = ce;
            }

            // this code is reached only on exception
            // check if there is a dotnet method, invoke the dotnet method if available
            PSMethod dotNetmethod = s_dotNetAdapter.GetDotNetMethod<PSMethod>(method.baseObject, method.name);
            if (dotNetmethod != null)
            {
                return dotNetmethod.Invoke(arguments);
            }
            // else
            throw exception;
        }

        /// <summary>
        /// Returns the string representation of the method in the object.
        /// </summary>
        /// <returns>The string representation of the method in the object.</returns>
        protected override string MethodToString(PSMethod method)
        {
            StringBuilder returnValue = new StringBuilder();
            foreach (string overload in MethodDefinitions(method))
            {
                returnValue.Append(overload);
                returnValue.Append(", ");
            }

            returnValue.Remove(returnValue.Length - 2, 2);
            return returnValue.ToString();
        }

        #endregion method
    }
}
