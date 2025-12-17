// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Xml;

using System.Management.Automation.Internal;
using Microsoft.PowerShell;
using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation
{
    /// <summary>
    /// Base class for all Adapters
    /// This is the place to look every time you create a new Adapter. Consider if you
    /// should implement each of the virtual methods here.
    /// The base class deals with errors and performs additional operations before and after
    /// calling the derived virtual methods.
    /// </summary>
    internal abstract class Adapter
    {
        /// <summary>
        /// Tracer for this and derivative classes.
        /// </summary>
        [TraceSource("ETS", "Extended Type System")]
        protected static PSTraceSource tracer = PSTraceSource.GetTracer("ETS", "Extended Type System");
        #region virtual

        #region member

        internal virtual bool CanSiteBinderOptimize(MemberTypes typeToOperateOn) { return false; }

        protected static IEnumerable<string> GetDotNetTypeNameHierarchy(Type type)
        {
            for (; type != null; type = type.BaseType)
            {
                yield return type.FullName;
            }
        }

        protected static IEnumerable<string> GetDotNetTypeNameHierarchy(object obj)
        {
            return GetDotNetTypeNameHierarchy(obj.GetType());
        }

        /// <summary>
        /// Returns the TypeNameHierarchy out of an object.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        protected virtual IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            return GetDotNetTypeNameHierarchy(obj);
        }

        /// <summary>
        /// Returns the cached typename, if it can be cached, otherwise constructs a new typename.
        /// By default, we don't return interned values, adapters can override if they choose.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        protected virtual ConsolidatedString GetInternedTypeNameHierarchy(object obj)
        {
            return new ConsolidatedString(GetTypeNameHierarchy(obj));
        }

        /// <summary>
        /// Returns null if memberName is not a member in the adapter or
        /// the corresponding PSMemberInfo.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="memberName">Name of the member to be retrieved.</param>
        /// <returns>The PSMemberInfo corresponding to memberName from obj.</returns>
        protected abstract T GetMember<T>(object obj, string memberName) where T : PSMemberInfo;

        /// <summary>
        /// Returns the first PSMemberInfo whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="predicate">The predicate to find the matching member.</param>
        /// <returns>The PSMemberInfo corresponding to the predicate match.</returns>
        protected abstract T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo;

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
        protected abstract PSMemberInfoInternalCollection<T> GetMembers<T>(object obj) where T : PSMemberInfo;

        #endregion member

        #region property

        /// <summary>
        /// Returns the value from a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <returns>The value of the property.</returns>
        protected abstract object PropertyGet(PSProperty property);

        /// <summary>
        /// Sets the value of a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected abstract void PropertySet(PSProperty property, object setValue, bool convertIfPossible);

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected abstract bool PropertyIsSettable(PSProperty property);

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected abstract bool PropertyIsGettable(PSProperty property);

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous GetMember.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected abstract string PropertyType(PSProperty property, bool forDisplay);

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected abstract string PropertyToString(PSProperty property);

        /// <summary>
        /// Returns an array with the property attributes.
        /// </summary>
        /// <param name="property">Property we want the attributes from.</param>
        /// <returns>An array with the property attributes.</returns>
        protected abstract AttributeCollection PropertyAttributes(PSProperty property);

        #endregion property

        #region method

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="invocationConstraints">Invocation constraints.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected virtual object MethodInvoke(PSMethod method, PSMethodInvocationConstraints invocationConstraints, object[] arguments)
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
        protected abstract object MethodInvoke(PSMethod method, object[] arguments);

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="method">The return of GetMember.</param>
        /// <returns></returns>
        protected abstract Collection<string> MethodDefinitions(PSMethod method);

        /// <summary>
        /// Returns the string representation of the method in the object.
        /// </summary>
        /// <returns>The string representation of the method in the object.</returns>
        protected virtual string MethodToString(PSMethod method)
        {
            StringBuilder returnValue = new StringBuilder();
            Collection<string> definitions = MethodDefinitions(method);
            for (int i = 0; i < definitions.Count; i++)
            {
                returnValue.Append(definitions[i]);
                returnValue.Append(", ");
            }

            returnValue.Remove(returnValue.Length - 2, 2);
            return returnValue.ToString();
        }

        #endregion method

        #region parameterized property

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual string ParameterizedPropertyType(PSParameterizedProperty property)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual bool ParameterizedPropertyIsSettable(PSParameterizedProperty property)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual bool ParameterizedPropertyIsGettable(PSParameterizedProperty property)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="property">The return of GetMember.</param>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual Collection<string> ParameterizedPropertyDefinitions(PSParameterizedProperty property)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Called after a non null return from GetMember to get the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the property.</returns>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual object ParameterizedPropertyGet(PSParameterizedProperty property, object[] arguments)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Called after a non null return from GetMember to set the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="setValue">The value to set property with.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual void ParameterizedPropertySet(PSParameterizedProperty property, object setValue, object[] arguments)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        /// <remarks>
        /// It is not necessary for derived methods to override this.
        /// This method is called only if ParameterizedProperties are present.
        /// </remarks>
        protected virtual string ParameterizedPropertyToString(PSParameterizedProperty property)
        {
            Diagnostics.Assert(false, "adapter is not called for parameterized properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion parameterized property

        #endregion virtual

        #region base

        #region private

        private static Exception NewException(
            Exception e,
            string errorId,
            string targetErrorId,
            string resourceString,
            params object[] parameters)
        {
            object[] newParameters = new object[parameters.Length + 1];
            for (int i = 0; i < parameters.Length; i++)
            {
                newParameters[i + 1] = parameters[i];
            }

            Exception ex = e as TargetInvocationException;
            if (ex != null)
            {
                Exception inner = ex.InnerException ?? ex;
                newParameters[0] = inner.Message;
                return new ExtendedTypeSystemException(
                    targetErrorId,
                    inner,
                    resourceString,
                    newParameters);
            }

            newParameters[0] = e.Message;
            return new ExtendedTypeSystemException(
                errorId,
                e,
                resourceString,
                newParameters);
        }

        #endregion private

        #region member

        internal ConsolidatedString BaseGetTypeNameHierarchy(object obj)
        {
            try
            {
                return GetInternedTypeNameHierarchy(obj);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseGetTypeNameHierarchy",
                    "CatchFromBaseGetTypeNameHierarchyTI",
                    ExtendedTypeSystem.ExceptionRetrievingTypeNameHierarchy);
            }
        }

        internal T BaseGetMember<T>(object obj, string memberName) where T : PSMemberInfo
        {
            try
            {
                return this.GetMember<T>(obj, memberName);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseGetMember",
                    "CatchFromBaseGetMemberTI",
                    ExtendedTypeSystem.ExceptionGettingMember,
                    memberName);
            }
        }

        internal T BaseGetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            try
            {
                return this.GetFirstMemberOrDefault<T>(obj, predicate);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseGetMember",
                    "CatchFromBaseGetMemberTI",
                    ExtendedTypeSystem.ExceptionGettingMember,
                    nameof(predicate));
            }
        }

        internal PSMemberInfoInternalCollection<T> BaseGetMembers<T>(object obj) where T : PSMemberInfo
        {
            try
            {
                return this.GetMembers<T>(obj);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseGetMembers",
                    "CatchFromBaseGetMembersTI",
                    ExtendedTypeSystem.ExceptionGettingMembers);
            }
        }

        #endregion member

        #region property

        internal object BasePropertyGet(PSProperty property)
        {
            try
            {
                return PropertyGet(property);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new GetValueInvocationException(
                    "CatchFromBaseAdapterGetValueTI",
                    inner,
                    ExtendedTypeSystem.ExceptionWhenGetting,
                    property.Name, inner.Message);
            }
            catch (GetValueException) { throw; }
            catch (Exception e)
            {
                throw new GetValueInvocationException(
                    "CatchFromBaseAdapterGetValue",
                    e,
                    ExtendedTypeSystem.ExceptionWhenGetting,
                    property.Name, e.Message);
            }
        }

        internal void BasePropertySet(PSProperty property, object setValue, bool convert)
        {
            try
            {
                PropertySet(property, setValue, convert);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new SetValueInvocationException(
                    "CatchFromBaseAdapterSetValueTI",
                    inner,
                    ExtendedTypeSystem.ExceptionWhenSetting,
                    property.Name, inner.Message);
            }
            catch (SetValueException) { throw; }
            catch (Exception e)
            {
                throw new SetValueInvocationException(
                    "CatchFromBaseAdapterSetValue",
                    e,
                    ExtendedTypeSystem.ExceptionWhenSetting,
                    property.Name, e.Message);
            }
        }

        internal bool BasePropertyIsSettable(PSProperty property)
        {
            try
            {
                return this.PropertyIsSettable(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBasePropertyIsSettable",
                    "CatchFromBasePropertyIsSettableTI",
                    ExtendedTypeSystem.ExceptionRetrievingPropertyWriteState,
                    property.Name);
            }
        }

        internal bool BasePropertyIsGettable(PSProperty property)
        {
            try
            {
                return this.PropertyIsGettable(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBasePropertyIsGettable",
                    "CatchFromBasePropertyIsGettableTI",
                    ExtendedTypeSystem.ExceptionRetrievingPropertyReadState,
                    property.Name);
            }
        }

        internal string BasePropertyType(PSProperty property)
        {
            try
            {
                return this.PropertyType(property, forDisplay: false);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBasePropertyType",
                    "CatchFromBasePropertyTypeTI",
                    ExtendedTypeSystem.ExceptionRetrievingPropertyType,
                    property.Name);
            }
        }

        internal string BasePropertyToString(PSProperty property)
        {
            try
            {
                return this.PropertyToString(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBasePropertyToString",
                    "CatchFromBasePropertyToStringTI",
                    ExtendedTypeSystem.ExceptionRetrievingPropertyString,
                    property.Name);
            }
        }

        internal AttributeCollection BasePropertyAttributes(PSProperty property)
        {
            try
            {
                return this.PropertyAttributes(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBasePropertyAttributes",
                    "CatchFromBasePropertyAttributesTI",
                    ExtendedTypeSystem.ExceptionRetrievingPropertyAttributes,
                    property.Name);
            }
        }

        #endregion property

        #region method
        internal object BaseMethodInvoke(PSMethod method, PSMethodInvocationConstraints invocationConstraints, params object[] arguments)
        {
            try
            {
                return this.MethodInvoke(method, invocationConstraints, arguments);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new MethodInvocationException(
                    "CatchFromBaseAdapterMethodInvokeTI",
                    inner,
                    ExtendedTypeSystem.MethodInvocationException,
                    method.Name,
                    arguments.Length,
                    inner.Message);
            }
            catch (FlowControlException) { throw; }
            catch (ScriptCallDepthException) { throw; }
            catch (PipelineStoppedException) { throw; }
            catch (MethodException) { throw; }
            catch (Exception e)
            {
                if (method.baseObject is SteppablePipeline
                    && (method.Name.Equals("Begin", StringComparison.OrdinalIgnoreCase) ||
                        method.Name.Equals("Process", StringComparison.OrdinalIgnoreCase) ||
                        method.Name.Equals("End", StringComparison.OrdinalIgnoreCase)))
                {
                    throw;
                }

                throw new MethodInvocationException(
                    "CatchFromBaseAdapterMethodInvoke",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    method.Name,
                    arguments.Length,
                    e.Message);
            }
        }

        internal Collection<string> BaseMethodDefinitions(PSMethod method)
        {
            try
            {
                return this.MethodDefinitions(method);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseMethodDefinitions",
                    "CatchFromBaseMethodDefinitionsTI",
                    ExtendedTypeSystem.ExceptionRetrievingMethodDefinitions,
                    method.Name);
            }
        }

        internal string BaseMethodToString(PSMethod method)
        {
            try
            {
                return this.MethodToString(method);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseMethodToString",
                    "CatchFromBaseMethodToStringTI",
                    ExtendedTypeSystem.ExceptionRetrievingMethodString,
                    method.Name);
            }
        }
        #endregion method

        #region parameterized property
        internal string BaseParameterizedPropertyType(PSParameterizedProperty property)
        {
            try
            {
                return this.ParameterizedPropertyType(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseParameterizedPropertyType",
                    "CatchFromBaseParameterizedPropertyTypeTI",
                    ExtendedTypeSystem.ExceptionRetrievingParameterizedPropertytype,
                    property.Name);
            }
        }

        internal bool BaseParameterizedPropertyIsSettable(PSParameterizedProperty property)
        {
            try
            {
                return this.ParameterizedPropertyIsSettable(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseParameterizedPropertyIsSettable",
                    "CatchFromBaseParameterizedPropertyIsSettableTI",
                    ExtendedTypeSystem.ExceptionRetrievingParameterizedPropertyWriteState,
                    property.Name);
            }
        }

        internal bool BaseParameterizedPropertyIsGettable(PSParameterizedProperty property)
        {
            try
            {
                return this.ParameterizedPropertyIsGettable(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseParameterizedPropertyIsGettable",
                    "CatchFromBaseParameterizedPropertyIsGettableTI",
                    ExtendedTypeSystem.ExceptionRetrievingParameterizedPropertyReadState,
                    property.Name);
            }
        }

        internal Collection<string> BaseParameterizedPropertyDefinitions(PSParameterizedProperty property)
        {
            try
            {
                return this.ParameterizedPropertyDefinitions(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseParameterizedPropertyDefinitions",
                    "CatchFromBaseParameterizedPropertyDefinitionsTI",
                    ExtendedTypeSystem.ExceptionRetrievingParameterizedPropertyDefinitions,
                    property.Name);
            }
        }

        internal object BaseParameterizedPropertyGet(PSParameterizedProperty property, params object[] arguments)
        {
            try
            {
                return this.ParameterizedPropertyGet(property, arguments);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new GetValueInvocationException(
                    "CatchFromBaseAdapterParameterizedPropertyGetValueTI",
                    inner,
                    ExtendedTypeSystem.ExceptionWhenGetting,
                    property.Name,
                    inner.Message);
            }
            catch (GetValueException) { throw; }
            catch (Exception e)
            {
                throw new GetValueInvocationException(
                    "CatchFromBaseParameterizedPropertyAdapterGetValue",
                    e,
                    ExtendedTypeSystem.ExceptionWhenGetting,
                    property.Name,
                    e.Message);
            }
        }

        internal void BaseParameterizedPropertySet(PSParameterizedProperty property, object setValue, params object[] arguments)
        {
            try
            {
                this.ParameterizedPropertySet(property, setValue, arguments);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new SetValueInvocationException(
                    "CatchFromBaseAdapterParameterizedPropertySetValueTI",
                    inner,
                    ExtendedTypeSystem.ExceptionWhenSetting,
                    property.Name,
                    inner.Message);
            }
            catch (SetValueException) { throw; }
            catch (Exception e)
            {
                throw new SetValueInvocationException(
                    "CatchFromBaseAdapterParameterizedPropertySetValue",
                    e,
                    ExtendedTypeSystem.ExceptionWhenSetting,
                    property.Name,
                    e.Message);
            }
        }

        internal string BaseParameterizedPropertyToString(PSParameterizedProperty property)
        {
            try
            {
                return this.ParameterizedPropertyToString(property);
            }
            catch (ExtendedTypeSystemException) { throw; }
            catch (Exception e)
            {
                throw NewException(
                    e,
                    "CatchFromBaseParameterizedPropertyToString",
                    "CatchFromBaseParameterizedPropertyToStringTI",
                    ExtendedTypeSystem.ExceptionRetrievingParameterizedPropertyString,
                    property.Name);
            }
        }

        #endregion parameterized property

        #region Internal Helper Methods

        private static Type GetArgumentType(object argument, bool isByRefParameter)
        {
            if (argument == null)
            {
                return typeof(LanguagePrimitives.Null);
            }

            if (isByRefParameter && argument is PSReference psref)
            {
                return GetArgumentType(PSObject.Base(psref.Value), isByRefParameter: false);
            }

            return GetObjectType(argument, debase: false);
        }

        internal static ConversionRank GetArgumentConversionRank(object argument, Type parameterType, bool isByRef, bool allowCastingToByRefLikeType)
        {
            Type fromType = null;
            ConversionRank rank = ConversionRank.None;

            if (allowCastingToByRefLikeType && parameterType.IsByRefLike)
            {
                // When resolving best method for use in binders, we can accept implicit/explicit casting conversions to
                // a ByRef-like target type, because when generating IL from a call site with the binder, the IL includes
                // the casting operation. However, we don't accept such conversions when it's for invoking the method via
                // reflection, because reflection just doesn't support ByRef-like type.
                fromType = GetArgumentType(PSObject.Base(argument), isByRefParameter: false);
                if (fromType != typeof(LanguagePrimitives.Null))
                {
                    LanguagePrimitives.FigureCastConversion(fromType, parameterType, ref rank);
                }

                return rank;
            }

            fromType = GetArgumentType(argument, isByRef);
            rank = LanguagePrimitives.GetConversionRank(fromType, parameterType);

            if (rank == ConversionRank.None)
            {
                fromType = GetArgumentType(PSObject.Base(argument), isByRef);
                rank = LanguagePrimitives.GetConversionRank(fromType, parameterType);
            }

            return rank;
        }

        /// <summary>
        /// Compare the 2 methods, determining which method is better.
        /// </summary>
        /// <returns>1 if method1 is better, -1 if method2 is better, 0 otherwise.</returns>
        private static int CompareOverloadCandidates(OverloadCandidate candidate1, OverloadCandidate candidate2, object[] arguments)
        {
            Diagnostics.Assert(candidate1.ConversionRanks.Length == candidate2.ConversionRanks.Length,
                               "should have same number of conversions regardless of the number of parameters - default arguments are not included here");

            Type[] params1 = candidate1.ExpandedParameterTypes ?? candidate1.ParameterTypes;
            Type[] params2 = candidate2.ExpandedParameterTypes ?? candidate2.ParameterTypes;

            int betterCount = 0;
            int multiplier = candidate1.ConversionRanks.Length;
            for (int i = 0; i < candidate1.ConversionRanks.Length; ++i, --multiplier)
            {
                if (candidate1.ConversionRanks[i] < candidate2.ConversionRanks[i])
                {
                    betterCount -= multiplier;
                }
                else if (candidate1.ConversionRanks[i] > candidate2.ConversionRanks[i])
                {
                    betterCount += multiplier;
                }
                else if (candidate1.ConversionRanks[i] == ConversionRank.UnrelatedArrays)
                {
                    // If both are unrelated arrays, then use the element type conversions instead.
                    Type argElemType = EffectiveArgumentType(arguments[i]).GetElementType();
                    ConversionRank rank1 = LanguagePrimitives.GetConversionRank(argElemType, params1[i].GetElementType());
                    ConversionRank rank2 = LanguagePrimitives.GetConversionRank(argElemType, params2[i].GetElementType());
                    if (rank1 < rank2)
                    {
                        betterCount -= multiplier;
                    }
                    else if (rank1 > rank2)
                    {
                        betterCount += multiplier;
                    }
                }
            }

            if (betterCount == 0)
            {
                multiplier = candidate1.ConversionRanks.Length;
                for (int i = 0; i < candidate1.ConversionRanks.Length; ++i, multiplier = Math.Abs(multiplier) - 1)
                {
                    // The following rather tricky logic tries to pick the best method in 2 very different cases -
                    //   - Pick the most specific method when conversions aren't losing information
                    //   - Pick the most general method when conversions will lose information.
                    // Consider:
                    //    f(uint32), f(decimal), call with f([byte]$i)
                    //        in this case, we want to call f(uint32) because it is more specific
                    //        while not losing information
                    //    f(byte), f(int16), call with f([int]$i)
                    //        in this case, we want to call f(int16) because it is more general,
                    //        we know we could lose information with either call, but we will lose
                    //        less information calling f(int16).
                    ConversionRank rank1 = candidate1.ConversionRanks[i];
                    ConversionRank rank2 = candidate2.ConversionRanks[i];
                    if (rank1 < ConversionRank.NullToValue || rank2 < ConversionRank.NullToValue)
                    {
                        // The tie breaking rules here do not apply to conversions that are not
                        // numeric or inheritance related.
                        continue;
                    }

                    if ((rank1 >= ConversionRank.NumericImplicit) != (rank2 >= ConversionRank.NumericImplicit))
                    {
                        // Skip trying to break ties when argument conversions are not both implicit or both
                        // explicit.  If we have that situation, there are multiple arguments and an
                        // ambiguity is probably the best choice.
                        continue;
                    }

                    // We will now compare the parameter types, ignoring the actual argument type.  To choose
                    // the right method, we need to know if we want the "most specific" or the "most general".
                    // If we have implicit argument conversions, we'll want the most specific, so invert the multiplier.
                    if (rank1 >= ConversionRank.NumericImplicit)
                    {
                        multiplier = -multiplier;
                    }

                    // With a positive multiplier, we'll choose the "most general" type, and a negative
                    // multiplier will choose the "most specific".
                    rank1 = LanguagePrimitives.GetConversionRank(params1[i], params2[i]);
                    rank2 = LanguagePrimitives.GetConversionRank(params2[i], params1[i]);
                    if (rank1 < rank2)
                    {
                        betterCount += multiplier;
                    }
                    else if (rank1 > rank2)
                    {
                        betterCount -= multiplier;
                    }
                }
            }

            if (betterCount == 0)
            {
                // Check if parameters are the same.  If so, we have a few tiebreakering rules down below.
                for (int i = 0; i < candidate1.ConversionRanks.Length; ++i)
                {
                    if (params1[i] != params2[i])
                    {
                        return 0;
                    }
                }

                // Apply tie breaking rules, related to expanded parameters
                if (candidate1.ExpandedParameterTypes != null && candidate2.ExpandedParameterTypes != null)
                {
                    // Both are using expanded parameters.  The one with more parameters is better
                    return (candidate1.ParameterTypes.Length > candidate2.ParameterTypes.Length) ? 1 : -1;
                }
                else if (candidate1.ExpandedParameterTypes != null)
                {
                    return -1;
                }
                else if (candidate2.ExpandedParameterTypes != null)
                {
                    return 1;
                }

                // Apply tie breaking rules, related to specificity of parameters
                betterCount = CompareTypeSpecificity(candidate1, candidate2);
            }

            // The methods with fewer parameter wins
            // Need to revisit this if we support named arguments
            if (betterCount == 0)
            {
                if (candidate1.ParameterTypes.Length < candidate2.ParameterTypes.Length)
                {
                    return 1;
                }
                else if (candidate1.ParameterTypes.Length > candidate2.ParameterTypes.Length)
                {
                    return -1;
                }
            }

            return betterCount;
        }

        private static OverloadCandidate FindBestCandidate(List<OverloadCandidate> candidates, object[] arguments)
        {
            Dbg.Assert(candidates != null, "Caller should verify candidates != null");

            OverloadCandidate bestCandidateSoFar = null;
            bool multipleBestCandidates = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                OverloadCandidate currentCandidate = candidates[i];
                if (bestCandidateSoFar == null) // first iteration
                {
                    bestCandidateSoFar = currentCandidate;
                    continue;
                }

                int comparisonResult = CompareOverloadCandidates(bestCandidateSoFar, currentCandidate, arguments);
                if (comparisonResult == 0)
                {
                    multipleBestCandidates = true;
                }
                else if (comparisonResult < 0)
                {
                    bestCandidateSoFar = currentCandidate;
                    multipleBestCandidates = false;
                }
            }

            Dbg.Assert(
                !candidates.Any(otherCandidate => otherCandidate != bestCandidateSoFar && CompareOverloadCandidates(otherCandidate, bestCandidateSoFar, arguments) > 0),
                "No other candidates are better than bestCandidateSoFar");

            return multipleBestCandidates ? null : bestCandidateSoFar;
        }

        private static OverloadCandidate FindBestCandidate(List<OverloadCandidate> candidates, object[] arguments, PSMethodInvocationConstraints invocationConstraints)
        {
            List<OverloadCandidate> filteredCandidates = candidates.Where(candidate => IsInvocationConstraintSatisfied(candidate, invocationConstraints)).ToList();
            if (filteredCandidates.Count > 0)
            {
                candidates = filteredCandidates;
            }

            OverloadCandidate bestCandidate = FindBestCandidate(candidates, arguments);
            return bestCandidate;
        }

        private static int CompareTypeSpecificity(Type type1, Type type2)
        {
            if (type1.IsGenericParameter || type2.IsGenericParameter)
            {
                int result = 0;
                if (type1.IsGenericParameter)
                {
                    result -= 1;
                }

                if (type2.IsGenericParameter)
                {
                    result += 1;
                }

                return result;
            }

            if (type1.IsArray)
            {
                Dbg.Assert(type2.IsArray, "Caller should verify that both overload candidates have the same parameter types");
                Dbg.Assert(type1.GetArrayRank() == type2.GetArrayRank(), "Caller should verify that both overload candidates have the same parameter types");
                return CompareTypeSpecificity(type1.GetElementType(), type2.GetElementType());
            }

            if (type1.IsGenericType)
            {
                Dbg.Assert(type2.IsGenericType, "Caller should verify that both overload candidates have the same parameter types");
                Dbg.Assert(type1.GetGenericTypeDefinition() == type2.GetGenericTypeDefinition(), "Caller should verify that both overload candidates have the same parameter types");
                return CompareTypeSpecificity(type1.GetGenericArguments(), type2.GetGenericArguments());
            }

            return 0;
        }

        private static int CompareTypeSpecificity(Type[] params1, Type[] params2)
        {
            Dbg.Assert(params1.Length == params2.Length, "Caller should verify that both overload candidates have the same number of parameters");

            bool candidate1hasAtLeastOneMoreSpecificParameter = false;
            bool candidate2hasAtLeastOneMoreSpecificParameter = false;
            for (int i = 0; i < params1.Length; ++i)
            {
                int specificityComparison = CompareTypeSpecificity(params1[i], params2[i]);
                if (specificityComparison > 0)
                {
                    candidate1hasAtLeastOneMoreSpecificParameter = true;
                }
                else if (specificityComparison < 0)
                {
                    candidate2hasAtLeastOneMoreSpecificParameter = true;
                }

                if (candidate1hasAtLeastOneMoreSpecificParameter && candidate2hasAtLeastOneMoreSpecificParameter)
                {
                    break;
                }
            }

            if (candidate1hasAtLeastOneMoreSpecificParameter && !candidate2hasAtLeastOneMoreSpecificParameter)
            {
                return 1;
            }
            else if (candidate2hasAtLeastOneMoreSpecificParameter && !candidate1hasAtLeastOneMoreSpecificParameter)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns -1 if <paramref name="candidate1"/> is less specific than <paramref name="candidate2"/>
        /// (1 otherwise, or 0 if both are equally specific or non-comparable)
        /// </summary>
        private static int CompareTypeSpecificity(OverloadCandidate candidate1, OverloadCandidate candidate2)
        {
            if (!(candidate1.Method.isGeneric || candidate2.Method.isGeneric))
            {
                return 0;
            }

            Type[] params1 = GetGenericMethodDefinitionIfPossible(candidate1.Method.method).GetParameters().Select(static p => p.ParameterType).ToArray();
            Type[] params2 = GetGenericMethodDefinitionIfPossible(candidate2.Method.method).GetParameters().Select(static p => p.ParameterType).ToArray();
            return CompareTypeSpecificity(params1, params2);
        }

        private static MethodBase GetGenericMethodDefinitionIfPossible(MethodBase method)
        {
            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
            {
                MethodInfo methodInfo = method as MethodInfo;
                if (methodInfo != null)
                {
                    return methodInfo.GetGenericMethodDefinition();
                }
            }

            return method;
        }

#nullable enable
        [DebuggerDisplay("OverloadCandidate: {Method.methodDefinition}")]
        private sealed class OverloadCandidate
        {
            /// <summary>
            /// Gets the underlying method information for this overload.
            /// </summary>
            public MethodInformation Method { get; }

            /// <summary>
            /// Gets the types for each parameters for the provided method.
            /// </summary>
            public Type[] ParameterTypes { get; }

            /// <summary>
            /// If set this contains the expanded param arg types based on
            /// the caller supplied count.
            /// </summary>
            public Type[]? ExpandedParameterTypes { get; internal set; }

            /// <summary>
            /// The conversion ranks for the caller supplied arguments.
            /// </summary>
            public ConversionRank[] ConversionRanks { get; }

            /// <summary>
            /// Gets the index map that maps the parameter index to the caller
            /// supplied argument index. A value of null means no caller
            /// supplied value was present for the parameter of that index.
            /// </summary>
            public int?[] ArgumentMap { get; internal set; }

            public OverloadCandidate(MethodInformation method, int argumentCount)
            {
                Method = method;
                ParameterTypes = new Type[method.parameters.Length];
                for (int i = 0; i < method.parameters.Length; i++)
                {
                    ParameterTypes[i] = method.parameters[i].parameterType;
                }
                ConversionRanks = new ConversionRank[argumentCount];
                ArgumentMap = new int?[ParameterTypes.Length];
            }

            /// <summary>
            /// Update the internal state that tracks how many caller supplied
            /// extra params have been supplied as individual objects rather
            /// than as an array.
            /// </summary>
            /// <param name="count">
            /// The number of individual arguments supplied for the extra params arg.
            /// </param>
            /// <param name="elementType">
            /// The element type of the params argument.
            /// </param>
            public void SetExtraParamsCount(int count, Type elementType)
            {
                if (count == 0)
                {
                    // A count of zero is treated as the literal array type.
                    ExpandedParameterTypes = ParameterTypes;
                    return;
                }

                ExpandedParameterTypes = new Type[ParameterTypes.Length + count - 1];
                Array.Copy(ParameterTypes, ExpandedParameterTypes, ParameterTypes.Length - 1);
                ExpandedParameterTypes[ParameterTypes.Length - 1] = elementType;

                if (count > 1)
                {
                    // A count greater than one needs to extend the type array
                    // as well as the argument map.
                    int?[] originalMap = ArgumentMap;
                    ArgumentMap = new int?[ExpandedParameterTypes.Length];
                    Array.Copy(originalMap, ArgumentMap, originalMap.Length);
                    for (int i = originalMap.Length; i < ArgumentMap.Length; i++)
                    {
                        ExpandedParameterTypes[i] = elementType;
                        ArgumentMap[i] = i;
                    }
                }
            }
        }
#nullable disable

        private static bool IsInvocationTargetConstraintSatisfied(MethodInformation method, PSMethodInvocationConstraints invocationConstraints)
        {
            Dbg.Assert(method != null, "Caller should verify method != null");

            if (method.method == null)
            {
                return true; // do not apply methodTargetType constraint to non-.NET types (i.e. to COM or WMI types)
            }

            // An invocation constraint is only specified when there is an explicit cast on the target expression, so:
            //
            //    [IFoo]$x.Bar()
            //
            // will have [IFoo] as the method target type, but
            //
            //    $hash = @{}; $hash.Add(1,2)
            //
            // will have no method target type.

            var methodDeclaringType = method.method.DeclaringType;
            if (invocationConstraints == null || invocationConstraints.MethodTargetType == null)
            {
                // If no method target type is specified, we say the constraint is matched as long as the method is not an interface.
                // This behavior matches V2 - our candidate sets never included methods with declaring type as an interface in V2.

                return !methodDeclaringType.IsInterface;
            }

            var targetType = invocationConstraints.MethodTargetType;
            if (targetType.IsInterface)
            {
                // If targetType is an interface, types must match exactly.  This is how we can call method impls.
                // We also allow the method declaring type to be in a base interface.
                return methodDeclaringType == targetType || (methodDeclaringType.IsInterface && targetType.IsSubclassOf(methodDeclaringType));
            }

            if (methodDeclaringType.IsInterface)
            {
                // targetType is a class.  We don't try comparing with targetType because we'll end up with
                // an ambiguous set because what is effectively the same method may appear in our set multiple
                // times (once with the declaring type as the interface, and once as the actual class type.)
                return false;
            }

            // Dual-purpose of ([type]<expression>).method() syntax makes this code a little bit tricky to understand.
            // First purpose of this syntax is cast.
            // Second is a non-virtual super-class method call.
            //
            // Consider this code:
            //
            // ```
            // class B {
            //     [string]foo() {return 'B.foo'}
            //     [string]foo($a) {return 'B.foo'}
            // }
            //
            // class Q : B {
            //     [string]$Name
            //     Q([string]$name) {$this.name = $name}
            // }
            //
            // ([Q]'t').foo()
            // ```
            //
            // Here we are using [Q] just for the cast and we are expecting foo() to be resolved to a super-class method.
            // So methodDeclaringType is [B] and targetType is [Q]
            //
            // Now consider another code
            //
            // ```
            // ([object]"abc").ToString()
            // ```
            //
            // Here we are using [object] to specify that we want a super-class implementation of ToString(), so it should
            // return "System.String"
            // Here methodDeclaringType is [string] and targetType is [object]
            //
            // Notice: in one case targetType is a subclass of methodDeclaringType,
            // in another case it's the reverse.
            // Both of them are valid.
            //
            // Array is a special case.
            return targetType.IsAssignableFrom(methodDeclaringType)
                || methodDeclaringType.IsAssignableFrom(targetType)
                || (targetType.IsArray && methodDeclaringType == typeof(Array));
        }

        private static bool IsInvocationConstraintSatisfied(OverloadCandidate overloadCandidate, PSMethodInvocationConstraints invocationConstraints)
        {
            Dbg.Assert(overloadCandidate != null, "Caller should verify overloadCandidate != null");

            if (invocationConstraints == null)
            {
                return true;
            }

            if (invocationConstraints.ParameterTypes != null)
            {
                int parameterIndex = 0;
                foreach (Type parameterTypeConstraint in invocationConstraints.ParameterTypes)
                {
                    if (parameterTypeConstraint != null)
                    {
                        if (parameterIndex >= overloadCandidate.ParameterTypes.Length)
                        {
                            return false;
                        }

                        Type parameterType = overloadCandidate.ParameterTypes[parameterIndex];
                        if (parameterType != parameterTypeConstraint)
                        {
                            return false;
                        }
                    }

                    parameterIndex++;
                }
            }

            return true;
        }

        /// <summary>
        /// Return the best method out of overloaded methods.
        /// The best has the smallest type distance between the method's parameters and the given arguments.
        /// </summary>
        /// <param name="methods">Different overloads for a method.</param>
        /// <param name="invocationConstraints">Invocation constraints.</param>
        /// <param name="allowCastingToByRefLikeType">True if we accept implicit/explicit casting conversion to a ByRef-like parameter type for method resolution.</param>
        /// <param name="arguments">Arguments to check against the overloads.</param>
        /// <param name="errorId">If no best method, the error id to use in the error message.</param>
        /// <param name="errorMsg">If no best method, the error message (format string) to use in the error message.</param>
        /// <param name="expandParamsOnBest">True if the best method's last parameter is a params method.</param>
        /// <param name="callNonVirtually">True if best method should be called as non-virtual.</param>
        internal static MethodInformation FindBestMethod(
            MethodInformation[] methods,
            PSMethodInvocationConstraints invocationConstraints,
            bool allowCastingToByRefLikeType,
            object[] arguments,
            ref string errorId,
            ref string errorMsg,
            out bool expandParamsOnBest,
            out bool callNonVirtually)
        {
            return FindBestMethod(
                methods,
                invocationConstraints,
                allowCastingToByRefLikeType,
                arguments.Select(a => (string.Empty, a)).ToArray(),
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out callNonVirtually,
                out int?[] _);
        }

        /// <summary>
        /// Return the best method out of overloaded methods with explicit argument names.
        /// The best has the smallest type distance between the method's parameters and the given arguments.
        /// </summary>
        /// <param name="methods">Different overloads for a method.</param>
        /// <param name="invocationConstraints">Invocation constraints.</param>
        /// <param name="allowCastingToByRefLikeType">True if we accept implicit/explicit casting conversion to a ByRef-like parameter type for method resolution.</param>
        /// <param name="arguments">Arguments to check against the overloads. Each entry is a tuple of the argument name and value.</param>
        /// <param name="errorId">If no best method, the error id to use in the error message.</param>
        /// <param name="errorMsg">If no best method, the error message (format string) to use in the error message.</param>
        /// <param name="expandParamsOnBest">True if the best method's last parameter is a params method.</param>
        /// <param name="callNonVirtually">True if best method should be called as non-virtual.</param>
        /// <param name="argumentMap">
        /// Maps the index of the MethodInformation parameter to the index of the argument to use, value is null if the caller did not provide a value for that argument.
        /// </param>
        internal static MethodInformation FindBestMethod(
            MethodInformation[] methods,
            PSMethodInvocationConstraints invocationConstraints,
            bool allowCastingToByRefLikeType,
            (string, object)[] arguments,
            ref string errorId,
            ref string errorMsg,
            out bool expandParamsOnBest,
            out bool callNonVirtually,
            out int?[] argumentMap)
        {
            callNonVirtually = false;
            var methodInfo = FindBestMethodImpl(
                methods,
                invocationConstraints,
                allowCastingToByRefLikeType,
                arguments,
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out argumentMap);
            if (methodInfo == null)
            {
                return null;
            }

            // For PS classes we need to support base method call syntax:
            //
            // class BaseClass
            // {
            //    [int] foo() { return 1}
            // }
            // class DerivedClass : BaseClass
            // {
            //    [int] foo() { return 2 * ([BaseClass]$this).foo() }
            // }
            //
            // If we have such information in invocationConstraints then we should call method on the baseClass.
            if (invocationConstraints != null &&
                invocationConstraints.MethodTargetType != null &&
                methodInfo.method != null &&
                methodInfo.method.DeclaringType != null)
            {
                Type targetType = methodInfo.method.DeclaringType;
                if (targetType != invocationConstraints.MethodTargetType && targetType.IsSubclassOf(invocationConstraints.MethodTargetType))
                {
                    var parameterTypes = methodInfo.method.GetParameters().Select(static parameter => parameter.ParameterType).ToArray();
                    var targetTypeMethod = invocationConstraints.MethodTargetType.GetMethod(methodInfo.method.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);

                    if (targetTypeMethod != null && (targetTypeMethod.IsPublic || targetTypeMethod.IsFamily || targetTypeMethod.IsFamilyOrAssembly))
                    {
                        methodInfo = new MethodInformation(targetTypeMethod, 0);
                        callNonVirtually = true;
                    }
                }
            }

            return methodInfo;
        }

        private static Type[] ResolveGenericTypeParameters(object[] genericTypeParameters)
        {
            if (genericTypeParameters is null || genericTypeParameters.Length == 0)
            {
                return null;
            }

            Type[] genericParamTypes = new Type[genericTypeParameters.Length];
            for (int i = 0; i < genericTypeParameters.Length; i++)
            {
                genericParamTypes[i] = genericTypeParameters[i] switch
                {
                    Type paramType => paramType,
                    ITypeName paramTypeName => TypeOps.ResolveTypeName(paramTypeName, paramTypeName.Extent),
                    _ => throw new ArgumentException("Unexpected value"),
                };
            }

            return genericParamTypes;
        }

        private static MethodInformation FindBestMethodImpl(
            MethodInformation[] methods,
            PSMethodInvocationConstraints invocationConstraints,
            bool allowCastingToByRefLikeType,
            (string, object)[] arguments,
            ref string errorId,
            ref string errorMsg,
            out bool expandParamsOnBest,
            out int?[] argumentMap)
        {
            expandParamsOnBest = false;

            // Small optimization so we don't calculate type distances when there is only one method
            // We skip the optimization, if the method hasVarArgs, since in the case where arguments
            // and parameters are of the same size, we want to know if the last argument should
            // be turned into an array.
            // We also skip the optimization if the number of arguments and parameters is different
            // so we let the loop deal with possible optional parameters.
            if (methods.Length == 1
                && !methods[0].hasVarArgs
                // generic methods need to be double checked in a loop below - generic methods can be rejected if type inference fails
                && !methods[0].isGeneric
                && (methods[0].method is null || !methods[0].method.DeclaringType.IsGenericTypeDefinition)
                && methods[0].parameters.Length == arguments.Length
                && !HasNamedArgument(arguments))
            {
                argumentMap = new int?[arguments.Length];
                for (int i = 0; i < arguments.Length; i++)
                {
                    argumentMap[i] = i;
                }
                return methods[0];
            }

            Type[] genericParamTypes = ResolveGenericTypeParameters(invocationConstraints?.GenericTypeParameters);
            var candidates = new List<OverloadCandidate>();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInformation methodInfo = methods[i];

                if (methodInfo.method?.DeclaringType.IsGenericTypeDefinition == true
                    || (!methodInfo.isGeneric && genericParamTypes is not null))
                {
                    // If method is defined by an *open* generic type, or
                    // if generic parameters were provided and this method isn't generic, skip it.
                    continue;
                }

                if (methodInfo.isGeneric)
                {
                    if (genericParamTypes is not null)
                    {
                        try
                        {
                            // This cast is safe, because
                            // 1. Only ConstructorInfo and MethodInfo derive from MethodBase
                            // 2. ConstructorInfo.IsGenericMethod is always false
                            var originalMethod = (MethodInfo)methodInfo.method;
                            methodInfo = new MethodInformation(
                                originalMethod.MakeGenericMethod(genericParamTypes),
                                parametersToIgnore: 0);
                        }
                        catch (ArgumentException)
                        {
                            // Just skip this possibility if the generic type parameters can't be used to make
                            // a valid generic method here.
                            continue;
                        }
                    }
                    else
                    {
                        // Infer the generic method when generic parameter types are not specified.
                        Type[] argumentTypes = arguments.Select(a => EffectiveArgumentType(a.Item2)).ToArray();
                        Type[] paramConstraintTypes = invocationConstraints?.ParameterTypes;

                        if (paramConstraintTypes is not null)
                        {
                            for (int k = 0; k < paramConstraintTypes.Length; k++)
                            {
                                if (paramConstraintTypes[k] is not null)
                                {
                                    argumentTypes[k] = paramConstraintTypes[k];
                                }
                            }
                        }

                        methodInfo = TypeInference.Infer(methodInfo, argumentTypes);
                        if (methodInfo is null)
                        {
                            // Skip generic methods for which we cannot infer type arguments
                            continue;
                        }
                    }
                }

                if (!IsInvocationTargetConstraintSatisfied(methodInfo, invocationConstraints))
                {
                    continue;
                }

                if (TryProcessOverload(methodInfo, arguments, allowCastingToByRefLikeType, out OverloadCandidate candidate))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                if (methods.Length > 0 && methods.All(static m => m.method != null && m.method.DeclaringType.IsGenericTypeDefinition && m.method.IsStatic))
                {
                    errorId = "CannotInvokeStaticMethodOnUninstantiatedGenericType";
                    errorMsg = string.Format(
                        CultureInfo.InvariantCulture,
                        ExtendedTypeSystem.CannotInvokeStaticMethodOnUninstantiatedGenericType,
                        methods[0].method.DeclaringType.FullName);
                    argumentMap = null;
                    return null;
                }
                else if (genericParamTypes is not null)
                {
                    errorId = "MethodCountCouldNotFindBestGeneric";
                    errorMsg = string.Format(
                        ExtendedTypeSystem.MethodGenericArgumentCountException,
                        methods[0].method.Name,
                        genericParamTypes.Length,
                        arguments.Length);
                    argumentMap = null;
                    return null;
                }
                else
                {
                    errorId = "MethodCountCouldNotFindBest";
                    errorMsg = ExtendedTypeSystem.MethodArgumentCountException;
                    argumentMap = null;
                    return null;
                }
            }

            OverloadCandidate bestCandidate = candidates.Count == 1
                ? candidates[0]
                : FindBestCandidate(candidates, arguments.Select(a => a.Item2).ToArray(), invocationConstraints);
            if (bestCandidate != null)
            {
                expandParamsOnBest = bestCandidate.ExpandedParameterTypes != null;
                argumentMap = bestCandidate.ArgumentMap;
                return bestCandidate.Method;
            }

            errorId = "MethodCountCouldNotFindBest";
            errorMsg = ExtendedTypeSystem.MethodAmbiguousException;
            argumentMap = null;
            return null;
        }

#nullable enable
        private static bool HasNamedArgument((string?, object?)[] arguments)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!string.IsNullOrEmpty(arguments[i].Item1))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryProcessOverload(
            MethodInformation methodInfo,
            (string?, object?)[] arguments,
            bool allowCastingToByRefLikeType,
            [NotNullWhen(true)] out OverloadCandidate? selectedCandidate)
        {
            /*
            This code checks if an overload can work with the caller provided
            arguments. It follows these rules:

            + If unnamed, argument is set to the next method parameter.
                + If there are no remaining parameters the overload is not
                  valid.
            + If named, argument is set to the parameter for that name
              + If there is no match for the name or it has already been set
                positionally, the overload is not valid.
            + If the argument is mapped to a param array
                + If named, only that argument is set as the param value
                + If unnamed, the argument and subsequent unnamed arguments are
                  set as the param value.
            + For any remaining values
                + If optional, the overload is still valid
                + If param, the overload is still valid and param is an empty
                  array
                + Otherwise, the overload is not valid.

            A caveat is when encountering arg names that are not unique (case
            insensitive). Only the last parameter with that name can be set by
            name and only if the preceding ones have been set positionally.
            */
            selectedCandidate = null;

            ParameterInformation[] parameters = methodInfo.parameters;

            List<(string?, int)> parameterNames = new List<(string?, int)>(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterNames.Add((parameters[i].name, i));
            }

            OverloadCandidate candidate = new OverloadCandidate(methodInfo, arguments.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                (string? argName, object? argValue) = arguments[i];

                int paramIndex;
                if (string.IsNullOrEmpty(argName))
                {
                    // Argument had no name, get the first parameter if avail.
                    if (parameterNames.Count == 0)
                    {
                        // No params left.
                        return false;
                    }

                    (argName, paramIndex) = parameterNames[0];
                    parameterNames.RemoveAt(0);
                }
                else
                {
                    int selectedIndex = parameterNames.FindIndex(n => string.Equals(argName, n.Item1, StringComparison.OrdinalIgnoreCase));
                    if (selectedIndex == -1)
                    {
                        // Had a name but it didn't match any remaining parameters.
                        return false;
                    }

                    (argName, paramIndex) = parameterNames[selectedIndex];
                    parameterNames.RemoveAt(selectedIndex);
                }

                ParameterInformation paramInfo = parameters[paramIndex];
                candidate.ArgumentMap[paramIndex] = i;

                if (paramInfo.isParamArray)
                {
                    // First determine how many args there are for the param arg.
                    // This checks up to the end of the arguments or the next
                    // named argument.
                    int extraParamsCount = 1;
                    for (int j = i + 1; j < arguments.Length; j++)
                    {
                        string? nextArgName = arguments[j].Item1;
                        if (!string.IsNullOrEmpty(nextArgName))
                        {
                            // Next arg was named so not part of the params.
                            break;
                        }

                        extraParamsCount++;
                    }

                    Type? elementType = paramInfo.parameterType?.GetElementType();
                    if (extraParamsCount == 1)
                    {
                        // There is only one argument, check to see if it can
                        // be passed as the array value or a single element of
                        // the param type.
                        ConversionRank arrayConv = GetArgumentConversionRank(
                            argValue,
                            paramInfo.parameterType,
                            isByRef: false,
                            allowCastingToByRefLikeType: false);

                        ConversionRank elemConv = GetArgumentConversionRank(
                            argValue,
                            elementType,
                            isByRef: false,
                            allowCastingToByRefLikeType: false);

                        if (elemConv > arrayConv)
                        {
                            // If the argument is the array element type we
                            // mark the candidate as having an argument that
                            // needs to be expanded.
                            candidate.SetExtraParamsCount(1, elementType!);
                            candidate.ConversionRanks[i] = elemConv;
                        }
                        else
                        {
                            candidate.ConversionRanks[i] = arrayConv;
                        }

                        if (candidate.ConversionRanks[i] == ConversionRank.None)
                        {
                            // The array or element cannot be casted.
                            return false;
                        }
                    }
                    else
                    {
                        // There are multiple arguments which are checked by
                        // the argument element type.
                        for (int j = i; j < i + extraParamsCount; j++)
                        {
                            object? nextArgValue = arguments[j].Item2;

                            candidate.ConversionRanks[j] = GetArgumentConversionRank(
                                nextArgValue,
                                elementType,
                                isByRef: false,
                                allowCastingToByRefLikeType: false);

                            if (candidate.ConversionRanks[j] == ConversionRank.None)
                            {
                                // The value cannot be casted.
                                return false;
                            }
                        }
                        i += extraParamsCount;

                        candidate.SetExtraParamsCount(extraParamsCount, elementType!);
                    }
                }
                else
                {
                    candidate.ConversionRanks[i] = GetArgumentConversionRank(
                        argValue,
                        paramInfo.parameterType,
                        paramInfo.isByRef,
                        allowCastingToByRefLikeType);

                    if (candidate.ConversionRanks[i] == ConversionRank.None)
                    {
                        // The value cannot be casted.
                        return false;
                    }
                }
            }

            // Unmapped args only work if they are optional or param.
            foreach ((string? _, int paramIndex) in parameterNames)
            {
                ParameterInformation paramInfo = parameters[paramIndex];
                if (paramInfo.isParamArray)
                {
                    candidate.SetExtraParamsCount(0, paramInfo.parameterType.GetElementType()!);
                }
                else if (!paramInfo.isOptional)
                {
                    return false;
                }
            }

            selectedCandidate = candidate;
            return true;
        }
#nullable disable

        internal static Type EffectiveArgumentType(object arg)
        {
            arg = PSObject.Base(arg);
            if (arg is null)
            {
                return typeof(LanguagePrimitives.Null);
            }

            if (arg is object[] array && array.Length > 0)
            {
                Type firstType = GetObjectType(array[0], debase: true);
                if (firstType is not null)
                {
                    bool allSameType = true;
                    for (int j = 1; j < array.Length; ++j)
                    {
                        if (firstType != GetObjectType(array[j], debase: true))
                        {
                            allSameType = false;
                            break;
                        }
                    }

                    if (allSameType)
                    {
                        return firstType.MakeArrayType();
                    }
                }
            }

            return GetObjectType(arg, debase: false);
        }

        internal static Type GetObjectType(object obj, bool debase)
        {
            if (debase)
            {
                obj = PSObject.Base(obj);
            }

            return obj == NullString.Value ? typeof(string) : obj?.GetType();
        }

        internal static void SetReferences(object[] arguments, MethodInformation methodInformation, object[] originalArguments)
        {
            using (PSObject.MemberResolution.TraceScope("Checking for possible references."))
            {
                ParameterInformation[] parameters = methodInformation.parameters;
                for (int i = 0; (i < originalArguments.Length) && (i < parameters.Length) && (i < arguments.Length); i++)
                {
                    object originalArgument = originalArguments[i];
                    PSReference originalArgumentReference = originalArgument as PSReference;
                    // It still might be an PSObject wrapping an PSReference
                    if (originalArgumentReference == null)
                    {
                        if (originalArgument is not PSObject originalArgumentObj)
                        {
                            continue;
                        }

                        originalArgumentReference = originalArgumentObj.BaseObject as PSReference;
                        if (originalArgumentReference == null)
                        {
                            continue;
                        }
                    }

                    ParameterInformation parameter = parameters[i];
                    if (!parameter.isByRef)
                    {
                        continue;
                    }

                    object argument = arguments[i];
                    PSObject.MemberResolution.WriteLine("Argument '{0}' was a reference so it will be set to \"{1}\".", i + 1, argument);
                    originalArgumentReference.Value = argument;
                }
            }
        }

        internal static MethodInformation GetBestMethodAndArguments(
            string methodName,
            MethodInformation[] methods,
            object[] arguments,
            out object[] newArguments)
        {
            return GetBestMethodAndArguments(methodName, methods, null, arguments, out newArguments);
        }

        internal static MethodInformation GetBestMethodAndArguments(
            string methodName,
            MethodInformation[] methods,
            PSMethodInvocationConstraints invocationConstraints,
            object[] arguments,
            out object[] newArguments)
        {
            bool expandParamsOnBest;
            bool callNonVirtually;
            string errorId = null;
            string errorMsg = null;

            MethodInformation bestMethod = FindBestMethod(
                methods,
                invocationConstraints,
                allowCastingToByRefLikeType: false,
                arguments,
                ref errorId,
                ref errorMsg,
                out expandParamsOnBest,
                out callNonVirtually);

            if (bestMethod == null)
            {
                throw new MethodException(errorId, null, errorMsg, methodName, arguments.Length);
            }

            newArguments = GetMethodArgumentsBase(methodName, bestMethod.parameters, arguments, expandParamsOnBest);
            return bestMethod;
        }

        /// <summary>
        /// Called in GetBestMethodAndArguments after a call to FindBestMethod to perform the
        /// type conversion, copying(varArg) and optional value setting of the final arguments.
        /// </summary>
        internal static object[] GetMethodArgumentsBase(string methodName,
            ParameterInformation[] parameters, object[] arguments,
            bool expandParamsOnBest)
        {
            int parametersLength = parameters.Length;
            if (parametersLength == 0)
            {
                return Array.Empty<object>();
            }

            object[] retValue = new object[parametersLength];
            for (int i = 0; i < parametersLength - 1; i++)
            {
                ParameterInformation parameter = parameters[i];
                SetNewArgument(methodName, arguments, retValue, parameter, i);
            }

            ParameterInformation lastParameter = parameters[parametersLength - 1];
            if (!expandParamsOnBest)
            {
                SetNewArgument(methodName, arguments, retValue, lastParameter, parametersLength - 1);
                return retValue;
            }

            // From this point on, we are dealing with VarArgs (Params)

            // If we have no arguments left, we use an appropriate empty array for the last parameter
            if (arguments.Length < parametersLength)
            {
                retValue[parametersLength - 1] = Array.CreateInstance(lastParameter.parameterType.GetElementType(), new int[] { 0 });
                return retValue;
            }

            // We are going to put all the remaining arguments into an array
            // and convert them to the proper type, if necessary to be the
            // one argument for this last parameter
            int remainingArgumentCount = arguments.Length - parametersLength + 1;
            if (remainingArgumentCount == 1 && arguments[arguments.Length - 1] == null)
            {
                // Don't turn a single null argument into an array of 1 element, just pass null.
                retValue[parametersLength - 1] = null;
            }
            else
            {
                object[] remainingArguments = new object[remainingArgumentCount];
                Type paramsElementType = lastParameter.parameterType.GetElementType();
                for (int j = 0; j < remainingArgumentCount; j++)
                {
                    int argumentIndex = j + parametersLength - 1;
                    try
                    {
                        remainingArguments[j] = MethodArgumentConvertTo(arguments[argumentIndex], false, argumentIndex,
                            paramsElementType, CultureInfo.InvariantCulture);
                    }
                    catch (InvalidCastException e)
                    {
                        // NTRAID#Windows Out Of Band Releases-924162-2005/11/17-JonN
                        throw new MethodException(
                            "MethodArgumentConversionInvalidCastArgument",
                            e,
                            ExtendedTypeSystem.MethodArgumentConversionException,
                            argumentIndex, arguments[argumentIndex], methodName, paramsElementType, e.Message);
                    }
                }

                try
                {
                    retValue[parametersLength - 1] = MethodArgumentConvertTo(remainingArguments,
                        lastParameter.isByRef, parametersLength - 1, lastParameter.parameterType,
                        CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException e)
                {
                    // NTRAID#Windows Out Of Band Releases-924162-2005/11/17-JonN
                    throw new MethodException(
                        "MethodArgumentConversionParamsConversion",
                        e,
                        ExtendedTypeSystem.MethodArgumentConversionException,
                        parametersLength - 1, remainingArguments, methodName, lastParameter.parameterType, e.Message);
                }
            }

            return retValue;
        }

        /// <summary>
        /// Auxiliary method in MethodInvoke to set newArguments[index] with the proper value.
        /// </summary>
        /// <param name="methodName">Used for the MethodException that might be thrown.</param>
        /// <param name="arguments">The complete array of arguments.</param>
        /// <param name="newArguments">The complete array of new arguments.</param>
        /// <param name="parameter">The parameter to use.</param>
        /// <param name="index">The index in newArguments to set.</param>
        internal static void SetNewArgument(string methodName, object[] arguments,
            object[] newArguments, ParameterInformation parameter, int index)
        {
            if (arguments.Length > index)
            {
                try
                {
                    newArguments[index] = MethodArgumentConvertTo(arguments[index], parameter.isByRef, index,
                        parameter.parameterType, CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException e)
                {
                    // NTRAID#Windows Out Of Band Releases-924162-2005/11/17-JonN
                    throw new MethodException(
                        "MethodArgumentConversionInvalidCastArgument",
                        e,
                        ExtendedTypeSystem.MethodArgumentConversionException,
                        index, arguments[index], methodName, parameter.parameterType, e.Message);
                }
            }
            else
            {
                Diagnostics.Assert(parameter.isOptional, "FindBestMethod would not return this method if there is no corresponding argument for a non optional parameter");
                newArguments[index] = parameter.defaultValue;
            }
        }

        internal static object MethodArgumentConvertTo(object valueToConvert,
            bool isParameterByRef, int parameterIndex, Type resultType,
            IFormatProvider formatProvider)
        {
            using (PSObject.MemberResolution.TraceScope("Method argument conversion."))
            {
                if (resultType == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(resultType));
                }

                bool isArgumentByRef;
                valueToConvert = UnReference(valueToConvert, out isArgumentByRef);
                if (isParameterByRef && !isArgumentByRef)
                {
                    throw new MethodException("NonRefArgumentToRefParameterMsg", null,
                        ExtendedTypeSystem.NonRefArgumentToRefParameter, parameterIndex + 1, typeof(PSReference).FullName, "[ref]");
                }

                if (isArgumentByRef && !isParameterByRef)
                {
                    throw new MethodException("RefArgumentToNonRefParameterMsg", null,
                        ExtendedTypeSystem.RefArgumentToNonRefParameter, parameterIndex + 1, typeof(PSReference).FullName, "[ref]");
                }

                return PropertySetAndMethodArgumentConvertTo(valueToConvert, resultType, formatProvider);
            }
        }

        internal static object UnReference(object obj, out bool isArgumentByRef)
        {
            isArgumentByRef = false;
            PSReference reference = obj as PSReference;
            if (reference != null)
            {
                PSObject.MemberResolution.WriteLine("Parameter was a reference.");
                isArgumentByRef = true;
                return reference.Value;
            }

            PSObject mshObj = obj as PSObject;
            if (mshObj != null)
            {
                reference = mshObj.BaseObject as PSReference;
            }

            if (reference != null)
            {
                PSObject.MemberResolution.WriteLine("Parameter was an PSObject containing a reference.");
                isArgumentByRef = true;
                return reference.Value;
            }

            return obj;
        }

        internal static object PropertySetAndMethodArgumentConvertTo(object valueToConvert,
            Type resultType, IFormatProvider formatProvider)
        {
            using (PSObject.MemberResolution.TraceScope("Converting parameter \"{0}\" to \"{1}\".", valueToConvert, resultType))
            {
                if (resultType == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(resultType));
                }

                PSObject mshObj = valueToConvert as PSObject;
                if (mshObj != null)
                {
                    if (resultType == typeof(object))
                    {
                        PSObject.MemberResolution.WriteLine("Parameter was an PSObject and will be converted to System.Object.");
                        // we use PSObject.Base so we don't return
                        // PSCustomObject
                        return PSObject.Base(mshObj);
                    }
                }

                return LanguagePrimitives.ConvertTo(valueToConvert, resultType, formatProvider);
            }
        }

        internal static void DoBoxingIfNecessary(ILGenerator generator, Type type)
        {
            if (type.IsByRef)
            {
                // We can't use a byref like we would use System.Object (the CLR will
                // crash if we attempt to do so.)  There isn't much anyone could do
                // with a byref in PowerShell anyway, so just load the object and
                // return that instead.
                type = type.GetElementType();
                if (type.IsPrimitive)
                {
                    if (type == typeof(byte)) { generator.Emit(OpCodes.Ldind_U1); }
                    else if (type == typeof(ushort)) { generator.Emit(OpCodes.Ldind_U2); }
                    else if (type == typeof(uint)) { generator.Emit(OpCodes.Ldind_U4); }
                    else if (type == typeof(sbyte)) { generator.Emit(OpCodes.Ldind_I8); }
                    else if (type == typeof(short)) { generator.Emit(OpCodes.Ldind_I2); }
                    else if (type == typeof(int)) { generator.Emit(OpCodes.Ldind_I4); }
                    else if (type == typeof(long)) { generator.Emit(OpCodes.Ldind_I8); }
                    else if (type == typeof(float)) { generator.Emit(OpCodes.Ldind_R4); }
                    else if (type == typeof(double)) { generator.Emit(OpCodes.Ldind_R8); }
                }
                else if (type.IsValueType)
                {
                    generator.Emit(OpCodes.Ldobj, type);
                }
                else
                {
                    generator.Emit(OpCodes.Ldind_Ref);
                }
            }
            else if (type.IsPointer)
            {
                // Pointers are similar to a byref.  Here we mimic what C# would do
                // when assigning a pointer to an object.  This might not be useful
                // to PowerShell script, but if we did nothing, the CLR would crash
                // our process.
                MethodInfo boxMethod = typeof(Pointer).GetMethod("Box");
                MethodInfo typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                generator.Emit(OpCodes.Ldtoken, type);
                generator.Emit(OpCodes.Call, typeFromHandle);
                generator.Emit(OpCodes.Call, boxMethod);
            }

            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        #endregion

        #endregion base
    }

    /// <summary>
    /// The abstract cache entry type.
    /// All specific cache entry types should derive from it.
    /// </summary>
    internal abstract class CacheEntry
    {
        /// <summary>
        /// Gets the boolean value to indicate if the member is hidden.
        /// </summary>
        /// <remarks>
        /// Currently, we only check the 'HiddenAttribute' declared for properties and methods,
        /// because it can be done for them through the 'hidden' keyword in PowerShell Class.
        ///
        /// We can't currently write a parameterized property in a PowerShell class so it's not too important
        /// to check for the 'HiddenAttribute' for parameterized properties. But if someone added the attribute
        /// to their C#, it'd be good to set this property correctly.
        /// </remarks>
        internal virtual bool IsHidden => false;
    }

    /// <summary>
    /// Ordered and case insensitive hashtable.
    /// </summary>
    internal class CacheTable
    {
        /// <summary>
        /// An object collection is used to help make populating method cache table more efficient
        /// <see cref="DotNetAdapter.PopulateMethodReflectionTable(Type, CacheTable, BindingFlags)"/>.
        /// </summary>
        internal Collection<object> memberCollection;
        private readonly Dictionary<string, int> _indexes;

        internal CacheTable()
        {
            memberCollection = new Collection<object>();
            _indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        internal void Add(string name, object member)
        {
            _indexes[name] = memberCollection.Count;
            memberCollection.Add(member);
        }

        internal object this[string name]
        {
            get
            {
                int indexObj;
                if (!_indexes.TryGetValue(name, out indexObj))
                {
                    return null;
                }

                return memberCollection[indexObj];
            }
        }

        /// <summary>
        /// Get the first non-hidden member that satisfies the predicate.
        /// </summary>
        /// <remarks>
        /// Hidden members are not returned for any fuzzy searches (searching by 'match' or enumerating a collection).
        /// A hidden member is returned only if the member name is explicitly looked for.
        /// </remarks>
        internal object GetFirstOrDefault(MemberNamePredicate predicate)
        {
            foreach (var entry in _indexes)
            {
                if (predicate(entry.Key))
                {
                    object member = memberCollection[entry.Value];
                    if (member is CacheEntry cacheEntry && cacheEntry.IsHidden)
                    {
                        continue;
                    }

                    return member;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Stores method related information.
    /// This structure should be used whenever a new type is adapted.
    /// For example, ManagementObjectAdapter uses this structure to store
    /// WMI method information.
    /// </summary>
    [DebuggerDisplay("MethodInformation: {methodDefinition}")]
    internal class MethodInformation
    {
        internal MethodBase method;
        private string _cachedMethodDefinition;

        internal string methodDefinition
        {
            get
            {
                if (_cachedMethodDefinition == null)
                {
                    var name = method is ConstructorInfo ? "new" : method.Name;
                    var methodDefn = DotNetAdapter.GetMethodInfoOverloadDefinition(name, method, method.GetParameters().Length - parameters.Length);
                    Interlocked.CompareExchange(ref _cachedMethodDefinition, methodDefn, null);
                }

                return _cachedMethodDefinition;
            }
        }

        internal ParameterInformation[] parameters;
        internal bool hasVarArgs;
        internal bool hasOptional;
        internal bool isGeneric;

        private bool _useReflection;

        private delegate object MethodInvoker(object target, object[] arguments);

        private MethodInvoker _methodInvoker;

        /// <summary>
        /// This constructor supports .net methods.
        /// </summary>
        internal MethodInformation(MethodBase method, int parametersToIgnore)
        {
            this.method = method;
            this.isGeneric = method.IsGenericMethod;
            ParameterInfo[] methodParameters = method.GetParameters();
            int parametersLength = methodParameters.Length - parametersToIgnore;
            this.parameters = new ParameterInformation[parametersLength];

            for (int i = 0; i < parametersLength; i++)
            {
                this.parameters[i] = new ParameterInformation(methodParameters[i]);
                if (methodParameters[i].IsOptional)
                {
                    hasOptional = true;
                }
            }

            this.hasVarArgs = false;
            if (parametersLength > 0)
            {
                ParameterInfo lastParameter = methodParameters[parametersLength - 1];

                if (lastParameter.ParameterType.IsArray)
                {
                    // The extension method 'CustomAttributeExtensions.GetCustomAttributes(ParameterInfo, Type, Boolean)' has inconsistent
                    // behavior on its return value in both FullCLR and CoreCLR. According to MSDN, if the attribute cannot be found, it
                    // should return an empty collection. However, it returns null in some rare cases [when the parameter isn't backed by
                    // actual metadata].
                    // This inconsistent behavior affects OneCore powershell because we are using the extension method here when compiling
                    // against CoreCLR. So we need to add a null check until this is fixed in CLR.
                    var paramArrayAttrs = lastParameter.GetCustomAttributes(typeof(ParamArrayAttribute), false);
                    if (paramArrayAttrs != null && paramArrayAttrs.Length > 0)
                    {
                        this.hasVarArgs = true;
                        this.parameters[parametersLength - 1].isParamArray = true;
                    }
                }
            }
        }

        internal MethodInformation(bool hasvarargs, bool hasoptional, ParameterInformation[] arguments)
        {
            hasVarArgs = hasvarargs;
            hasOptional = hasoptional;
            parameters = arguments;
        }

        internal object Invoke(object target, object[] arguments)
        {
            // There may be parameters of ByRef-like types, but they will be taken care of
            // when we resolve overloads to find the best methods -- proper exception will
            // be thrown when converting arguments to the ByRef-like parameter types.
            //
            // So when reaching here, we only care about (1) if the method return type is
            // BeRef-like; (2) if it's a constructor of a ByRef-like type.

            if (method is ConstructorInfo ctor)
            {
                if (ctor.DeclaringType.IsByRefLike)
                {
                    throw new MethodException(
                        nameof(ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType),
                        innerException: null,
                        ExtendedTypeSystem.CannotInstantiateBoxedByRefLikeType,
                        ctor.DeclaringType);
                }

                return ctor.Invoke(arguments);
            }

            var methodInfo = (MethodInfo)method;
            if (methodInfo.ReturnType.IsByRefLike)
            {
                throw new MethodException(
                    nameof(ExtendedTypeSystem.CannotCallMethodWithByRefLikeReturnType),
                    innerException: null,
                    ExtendedTypeSystem.CannotCallMethodWithByRefLikeReturnType,
                    methodInfo.Name,
                    methodInfo.ReturnType);
            }

            if (target is PSObject)
            {
                if (!method.DeclaringType.IsAssignableFrom(target.GetType()))
                {
                    target = PSObject.Base(target);
                }
            }

            if (!_useReflection)
            {
                _methodInvoker ??= GetMethodInvoker(methodInfo);

                if (_methodInvoker != null)
                {
                    return _methodInvoker(target, arguments);
                }
            }

            return method.Invoke(target, arguments);
        }

        private static readonly OpCode[] s_ldc = new OpCode[] {
            OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8
        };

        private static void EmitLdc(ILGenerator emitter, int c)
        {
            if (c < s_ldc.Length)
            {
                emitter.Emit(s_ldc[c]);
            }
            else
            {
                emitter.Emit(OpCodes.Ldc_I4, c);
            }
        }

        private static bool CompareMethodParameters(MethodBase method1, MethodBase method2)
        {
            ParameterInfo[] params1 = method1.GetParameters();
            ParameterInfo[] params2 = method2.GetParameters();

            if (params1.Length != params2.Length)
            {
                return false;
            }

            for (int i = 0; i < params1.Length; ++i)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type FindInterfaceForMethod(MethodInfo method, out MethodInfo methodToCall)
        {
            methodToCall = null;

            Type valuetype = method.DeclaringType;

            Diagnostics.Assert(valuetype.IsValueType, "This code only works with valuetypes");

            Type[] interfaces = valuetype.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                Type type = interfaces[i];
                MethodInfo methodInfo = type.GetMethod(method.Name, BindingFlags.Instance);
                if (methodInfo != null && CompareMethodParameters(methodInfo, method))
                {
                    methodToCall = methodInfo;
                    return type;
                }
            }

            // TODO: method impls (not especially important because I don't think they can be called in script.

            return null;
        }

        [SuppressMessage("NullPtr", "#pw26500", Justification = "This is a false positive. Original warning was on the deference of 'locals' on line 1863: emitter.Emit(OpCodes.Ldloca, locals[cLocal])")]
        private MethodInvoker GetMethodInvoker(MethodInfo method)
        {
            Type type;
            bool valueTypeInstanceMethod = false;
            bool anyOutOrRefParameters = false;
            bool mustStoreRetVal = false;
            MethodInfo methodToCall = method;
            int cLocal = 0;
            int c;

            DynamicMethod dynamicMethod = new DynamicMethod(method.Name, typeof(object),
                new Type[] { typeof(object), typeof(object[]) }, typeof(Adapter).Module, true);

            ILGenerator emitter = dynamicMethod.GetILGenerator();
            ParameterInfo[] parameters = method.GetParameters();

            int localCount = 0;
            if (!method.IsStatic && method.DeclaringType.IsValueType)
            {
                if (!method.IsVirtual)
                {
                    // We need a local to unbox the instance argument into
                    valueTypeInstanceMethod = true;
                    localCount += 1;
                }
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                // We need locals for any out/ref parameters.  We could get
                // away with avoiding a local if the parameter was 'object',
                // but that optimization is not implemented.
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    anyOutOrRefParameters = true;
                    localCount += 1;
                }
            }

            LocalBuilder[] locals = null;
            Type returnType = method.ReturnType;
            if (localCount > 0)
            {
                if (anyOutOrRefParameters && returnType != typeof(void))
                {
                    // If there are any ref/out parameters, we set them after the
                    // call.  We can't leave the return value on the stack (it fails
                    // verification), so we must create a local to hold the return value.
                    localCount += 1;
                    mustStoreRetVal = true;
                }

                locals = new LocalBuilder[localCount];

                cLocal = 0;
                if (valueTypeInstanceMethod)
                {
                    // Unbox the instance parameter into a local.
                    type = method.DeclaringType;
                    locals[cLocal] = emitter.DeclareLocal(type);
                    emitter.Emit(OpCodes.Ldarg_0);
                    emitter.Emit(OpCodes.Unbox_Any, type);
                    emitter.Emit(OpCodes.Stloc, locals[cLocal]);
                    cLocal += 1;
                }

                // Copy all arguments that are being passed as out/ref parameters into
                // locals.
                for (c = 0; c < parameters.Length; ++c)
                {
                    type = parameters[c].ParameterType;
                    if (parameters[c].IsOut || type.IsByRef)
                    {
                        if (type.IsByRef)
                        {
                            type = type.GetElementType();
                        }

                        locals[cLocal] = emitter.DeclareLocal(type);

                        emitter.Emit(OpCodes.Ldarg_1);
                        EmitLdc(emitter, c);
                        emitter.Emit(OpCodes.Ldelem_Ref);
                        if (type.IsValueType)
                        {
                            emitter.Emit(OpCodes.Unbox_Any, type);
                        }
                        else if (type != typeof(object))
                        {
                            emitter.Emit(OpCodes.Castclass, type);
                        }

                        emitter.Emit(OpCodes.Stloc, locals[cLocal]);

                        cLocal += 1;
                    }
                }

                if (mustStoreRetVal)
                {
                    locals[cLocal] = emitter.DeclareLocal(returnType);
                }
            }

            cLocal = 0;
            if (!method.IsStatic)
            {
                // Load the "instance" argument.
                if (method.DeclaringType.IsValueType)
                {
                    if (method.IsVirtual)
                    {
                        type = FindInterfaceForMethod(method, out methodToCall);
                        if (type == null)
                        {
                            _useReflection = true;
                            return null;
                        }

                        emitter.Emit(OpCodes.Ldarg_0);
                        emitter.Emit(OpCodes.Castclass, type);
                    }
                    else
                    {
                        emitter.Emit(OpCodes.Ldloca, locals[cLocal]);
                        cLocal += 1;
                    }
                }
                else
                {
                    emitter.Emit(OpCodes.Ldarg_0);
                }
            }

            for (c = 0; c < parameters.Length; c++)
            {
                type = parameters[c].ParameterType;
                if (type.IsByRef)
                {
                    emitter.Emit(OpCodes.Ldloca, locals[cLocal]);
                    cLocal += 1;
                }
                else if (parameters[c].IsOut)
                {
                    emitter.Emit(OpCodes.Ldloc, locals[cLocal]);
                    cLocal += 1;
                }
                else
                {
                    emitter.Emit(OpCodes.Ldarg_1);
                    EmitLdc(emitter, c);
                    emitter.Emit(OpCodes.Ldelem_Ref);

                    // Unbox value types since our args array is full of objects
                    if (type.IsValueType)
                    {
                        emitter.Emit(OpCodes.Unbox_Any, type);
                    }
                    // For reference types, cast from object
                    else if (type != typeof(object))
                    {
                        emitter.Emit(OpCodes.Castclass, type);
                    }
                }
            }

            emitter.Emit(method.IsStatic ? OpCodes.Call : OpCodes.Callvirt, methodToCall);

            if (mustStoreRetVal)
            {
                emitter.Emit(OpCodes.Stloc, locals[locals.Length - 1]);
            }

            // Handle the ref/out arguments by copying the locals
            // back into the original args array
            if (anyOutOrRefParameters)
            {
                cLocal = valueTypeInstanceMethod ? 1 : 0;
                for (c = 0; c < parameters.Length; c++)
                {
                    type = parameters[c].ParameterType;
                    if (!parameters[c].IsOut && !type.IsByRef)
                    {
                        continue;
                    }

                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    emitter.Emit(OpCodes.Ldarg_1);
                    EmitLdc(emitter, c);
                    emitter.Emit(OpCodes.Ldloc, locals[cLocal]);

                    // Again, box value types since the args array holds objects
                    if (type.IsValueType)
                    {
                        emitter.Emit(OpCodes.Box, type);
                    }

                    emitter.Emit(OpCodes.Stelem_Ref);
                    cLocal += 1;
                }
            }

            // We must return something, so return null for void methods
            if (returnType == typeof(void))
            {
                emitter.Emit(OpCodes.Ldnull);
            }
            else
            {
                if (mustStoreRetVal)
                {
                    // Return value was stored in a local, load it before return
                    emitter.Emit(OpCodes.Ldloc, locals[locals.Length - 1]);
                }

                Adapter.DoBoxingIfNecessary(emitter, returnType);
            }

            emitter.Emit(OpCodes.Ret);

            return (MethodInvoker)dynamicMethod.CreateDelegate(typeof(MethodInvoker));
        }
    }

    /// <summary>
    /// Stores parameter related information.
    /// This structure should be used whenever a new type is adapted.
    /// For example, ManagementObjectAdapter uses this structure to store
    /// method parameter information.
    /// </summary>
    internal class ParameterInformation
    {
        internal readonly string name;
        internal Type parameterType;
        internal object defaultValue;
        internal bool isOptional;
        internal bool isByRef;
        internal bool isParamArray;

        internal ParameterInformation(System.Reflection.ParameterInfo parameter)
        {
            this.name = parameter.Name;
            this.isOptional = parameter.IsOptional;
            this.defaultValue = parameter.DefaultValue;
            this.parameterType = parameter.ParameterType;
            if (this.parameterType.IsByRef)
            {
                this.isByRef = true;
                this.parameterType = this.parameterType.GetElementType();
            }
            else
            {
                this.isByRef = false;
            }
        }

        internal ParameterInformation(string name, Type parameterType, bool isOptional, object defaultValue, bool isByRef)
        {
            this.name = name;
            this.parameterType = parameterType;
            this.isOptional = isOptional;
            this.defaultValue = defaultValue;
            this.isByRef = isByRef;
        }
    }

    /// <summary>
    /// This is the adapter used for all objects that don't match the appropriate types for other adapters.
    /// It uses reflection to retrieve property information.
    /// </summary>
    internal class DotNetAdapter : Adapter
    {
        #region auxiliary methods and classes

        private const BindingFlags instanceBindingFlags = (BindingFlags.FlattenHierarchy | BindingFlags.Public |
                                                              BindingFlags.IgnoreCase | BindingFlags.Instance);

        private const BindingFlags staticBindingFlags = (BindingFlags.FlattenHierarchy | BindingFlags.Public |
                                                              BindingFlags.IgnoreCase | BindingFlags.Static);

        private readonly bool _isStatic;

        internal DotNetAdapter() { }

        internal DotNetAdapter(bool isStatic)
        {
            _isStatic = isStatic;
        }

        // This static is thread safe based on the lock in GetInstancePropertyReflectionTable
        /// <summary>
        /// CLR reflection property cache for instance properties.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_instancePropertyCacheTable = new Dictionary<Type, CacheTable>();

        // This static is thread safe based on the lock in GetStaticPropertyReflectionTable
        /// <summary>
        /// CLR reflection property cache for static properties.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_staticPropertyCacheTable = new Dictionary<Type, CacheTable>();

        // This static is thread safe based on the lock in GetInstanceMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for instance methods.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_instanceMethodCacheTable = new Dictionary<Type, CacheTable>();

        // This static is thread safe based on the lock in GetStaticMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for static methods.
        /// </summary>
        private static readonly Dictionary<Type, CacheTable> s_staticMethodCacheTable = new Dictionary<Type, CacheTable>();

        // This static is thread safe based on the lock in GetInstanceMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for instance events.
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, EventCacheEntry>> s_instanceEventCacheTable
            = new Dictionary<Type, Dictionary<string, EventCacheEntry>>();

        // This static is thread safe based on the lock in GetStaticMethodReflectionTable
        /// <summary>
        /// CLR reflection method cache for static events.
        /// </summary>
        private static readonly Dictionary<Type, Dictionary<string, EventCacheEntry>> s_staticEventCacheTable
            = new Dictionary<Type, Dictionary<string, EventCacheEntry>>();

        internal class MethodCacheEntry : CacheEntry
        {
            internal readonly MethodInformation[] methodInformationStructures;
            /// <summary>
            /// Cache delegate to the ctor of PSMethod&lt;&gt; with a template parameter derived from the methodInformationStructures.
            /// </summary>
            internal Func<string, DotNetAdapter, object, DotNetAdapter.MethodCacheEntry, bool, bool, PSMethod> PSMethodCtor;

            internal MethodCacheEntry(IList<MethodBase> methods)
            {
                methodInformationStructures = DotNetAdapter.GetMethodInformationArray(methods);
            }

            internal MethodInformation this[int i]
            {
                get
                {
                    return methodInformationStructures[i];
                }
            }

            private bool? _isHidden;

            internal override bool IsHidden
            {
                get
                {
                    if (_isHidden == null)
                    {
                        bool hasHiddenAttribute = false;
                        foreach (var method in methodInformationStructures)
                        {
                            if (method.method.GetCustomAttributes(typeof(HiddenAttribute), inherit: false).Length != 0)
                            {
                                hasHiddenAttribute = true;
                                break;
                            }
                        }

                        _isHidden = hasHiddenAttribute;
                    }

                    return _isHidden.Value;
                }
            }
        }

        internal class EventCacheEntry : CacheEntry
        {
            internal EventInfo[] events;

            internal EventCacheEntry(EventInfo[] events)
            {
                this.events = events;
            }
        }

        internal class ParameterizedPropertyCacheEntry : CacheEntry
        {
            internal MethodInformation[] getterInformation;
            internal MethodInformation[] setterInformation;
            internal string propertyName;
            internal bool readOnly;
            internal bool writeOnly;
            internal Type propertyType;
            // propertyDefinition is used as a string representation of the property
            internal string[] propertyDefinition;

            internal ParameterizedPropertyCacheEntry(List<PropertyInfo> properties)
            {
                PropertyInfo firstProperty = properties[0];
                this.propertyName = firstProperty.Name;
                this.propertyType = firstProperty.PropertyType;
                var getterList = new List<MethodInfo>();
                var setterList = new List<MethodInfo>();
                var definitionArray = new List<string>();

                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyInfo property = properties[i];
                    // Properties can have different return types. If they do
                    // we pretend it is System.Object
                    if (property.PropertyType != this.propertyType)
                    {
                        this.propertyType = typeof(object);
                    }

                    // Get the public getter
                    MethodInfo propertyGetter = property.GetGetMethod();
                    StringBuilder definition = new StringBuilder();
                    StringBuilder extraDefinition = new StringBuilder();
                    if (propertyGetter != null)
                    {
                        extraDefinition.Append("get;");
                        definition.Append(DotNetAdapter.GetMethodInfoOverloadDefinition(this.propertyName, propertyGetter, 0));
                        getterList.Add(propertyGetter);
                    }

                    // Get the public setter
                    MethodInfo propertySetter = property.GetSetMethod();
                    if (propertySetter != null)
                    {
                        extraDefinition.Append("set;");
                        if (definition.Length == 0)
                        {
                            definition.Append(DotNetAdapter.GetMethodInfoOverloadDefinition(this.propertyName, propertySetter, 1));
                        }

                        setterList.Add(propertySetter);
                    }

                    definition.Append(" {");
                    definition.Append(extraDefinition);
                    definition.Append('}');
                    definitionArray.Add(definition.ToString());
                }

                propertyDefinition = definitionArray.ToArray();

                this.writeOnly = getterList.Count == 0;
                this.readOnly = setterList.Count == 0;

                this.getterInformation = new MethodInformation[getterList.Count];
                for (int i = 0; i < getterList.Count; i++)
                {
                    this.getterInformation[i] = new MethodInformation(getterList[i], 0);
                }

                this.setterInformation = new MethodInformation[setterList.Count];
                for (int i = 0; i < setterList.Count; i++)
                {
                    this.setterInformation[i] = new MethodInformation(setterList[i], 1);
                }
            }
        }

        internal class PropertyCacheEntry : CacheEntry
        {
            internal delegate object GetterDelegate(object instance);

            internal delegate void SetterDelegate(object instance, object setValue);

            internal PropertyCacheEntry(PropertyInfo property)
            {
                this.member = property;
                this.propertyType = property.PropertyType;
                // Generating code for fields/properties in ValueTypes is complex and will probably
                // require different delegates
                // The same is true for generics, COM Types.
                Type declaringType = property.DeclaringType;

                if (declaringType.IsValueType ||
                    propertyType.IsGenericType ||
                    declaringType.IsGenericType ||
                    declaringType.IsCOMObject ||
                    propertyType.IsCOMObject)
                {
                    this.readOnly = property.GetSetMethod() == null;
                    this.writeOnly = property.GetGetMethod() == null;
                    this.useReflection = true;
                    return;
                }

                // Get the public or protected getter
                MethodInfo propertyGetter = property.GetGetMethod(true);
                if (propertyGetter != null && (propertyGetter.IsPublic || propertyGetter.IsFamily || propertyGetter.IsFamilyOrAssembly))
                {
                    this.isStatic = propertyGetter.IsStatic;
                    // Delegate is initialized later to avoid jit if it's not called
                }
                else
                {
                    this.writeOnly = true;
                }

                // Get the public or protected setter
                MethodInfo propertySetter = property.GetSetMethod(true);
                if (propertySetter != null && (propertySetter.IsPublic || propertySetter.IsFamily || propertySetter.IsFamilyOrAssembly))
                {
                    this.isStatic = propertySetter.IsStatic;
                }
                else
                {
                    this.readOnly = true;
                }
            }

            internal PropertyCacheEntry(FieldInfo field)
            {
                this.member = field;
                this.isStatic = field.IsStatic;
                this.propertyType = field.FieldType;

                // const fields have no setter and we are getting them with GetValue instead of
                // using generated code. Init fields are only settable during initialization
                // then cannot be set afterwards..
                if (field.IsLiteral || field.IsInitOnly)
                {
                    this.readOnly = true;
                }
            }

            private void InitGetter()
            {
                if (writeOnly || useReflection)
                {
                    return;
                }

                var parameter = Expression.Parameter(typeof(object));
                Expression instance = null;

                var field = member as FieldInfo;
                if (field != null)
                {
                    var declaringType = field.DeclaringType;
                    if (!field.IsStatic)
                    {
                        if (declaringType.IsValueType)
                        {
                            // I'm not sure we can get here with a Nullable, but if so,
                            // we must use the Value property, see PSGetMemberBinder.GetTargetValue.
                            instance = Nullable.GetUnderlyingType(declaringType) != null
                                ? (Expression)Expression.Property(parameter, "Value")
                                : Expression.Unbox(parameter, declaringType);
                        }
                        else
                        {
                            instance = parameter.Cast(declaringType);
                        }
                    }

                    Expression getterExpr;

                    if (declaringType.IsGenericTypeDefinition)
                    {
                        Expression innerException = Expression.New(CachedReflectionInfo.GetValueException_ctor,
                            Expression.Constant("PropertyGetException"),
                            Expression.Constant(null, typeof(Exception)),
                            Expression.Constant(ParserStrings.PropertyInGenericType),
                            Expression.NewArrayInit(typeof(object), Expression.Constant(field.Name)));
                        getterExpr = Compiler.ThrowRuntimeErrorWithInnerException("PropertyGetException",
                                                                          Expression.Constant(ParserStrings.PropertyInGenericType),
                                                                          innerException, typeof(object), Expression.Constant(field.Name));
                    }
                    else
                    {
                        getterExpr = Expression.Field(instance, field).Cast(typeof(object));
                    }

                    _getterDelegate = Expression.Lambda<GetterDelegate>(getterExpr, parameter).Compile();
                    return;
                }

                var property = (PropertyInfo)member;
                var propertyGetter = property.GetGetMethod(true);

                instance = this.isStatic ? null : parameter.Cast(propertyGetter.DeclaringType);
                _getterDelegate = Expression.Lambda<GetterDelegate>(
                    Expression.Property(instance, property).Cast(typeof(object)), parameter).Compile();
            }

            private void InitSetter()
            {
                if (readOnly || useReflection)
                {
                    return;
                }

                var parameter = Expression.Parameter(typeof(object));
                var value = Expression.Parameter(typeof(object));
                Expression instance = null;

                var field = member as FieldInfo;
                if (field != null)
                {
                    var declaringType = field.DeclaringType;
                    if (!field.IsStatic)
                    {
                        if (declaringType.IsValueType)
                        {
                            // I'm not sure we can get here with a Nullable, but if so,
                            // we must use the Value property, see PSGetMemberBinder.GetTargetValue.
                            instance = Nullable.GetUnderlyingType(declaringType) != null
                                ? (Expression)Expression.Property(parameter, "Value")
                                : Expression.Unbox(parameter, declaringType);
                        }
                        else
                        {
                            instance = parameter.Cast(declaringType);
                        }
                    }

                    Expression setterExpr;
                    string errMessage = null;
                    Type errType = field.FieldType;
                    if (declaringType.IsGenericTypeDefinition)
                    {
                        errMessage = ParserStrings.PropertyInGenericType;
                        if (errType.ContainsGenericParameters)
                        {
                            errType = typeof(object);
                        }
                    }
                    else if (readOnly)
                    {
                        errMessage = ParserStrings.PropertyIsReadOnly;
                    }

                    if (errMessage != null)
                    {
                        Expression innerException = Expression.New(CachedReflectionInfo.SetValueException_ctor,
                            Expression.Constant("PropertyAssignmentException"),
                            Expression.Constant(null, typeof(Exception)),
                            Expression.Constant(errMessage),
                            Expression.NewArrayInit(typeof(object), Expression.Constant(field.Name)));
                        setterExpr = Compiler.ThrowRuntimeErrorWithInnerException("PropertyAssignmentException",
                                                                                  Expression.Constant(errMessage),
                                                                                  innerException, errType, Expression.Constant(field.Name));
                    }
                    else
                    {
                        setterExpr = Expression.Assign(Expression.Field(instance, field), Expression.Convert(value, field.FieldType));
                    }

                    _setterDelegate = Expression.Lambda<SetterDelegate>(setterExpr, parameter, value).Compile();
                    return;
                }

                var property = (PropertyInfo)member;
                MethodInfo propertySetter = property.GetSetMethod(true);

                instance = this.isStatic ? null : parameter.Cast(propertySetter.DeclaringType);
                _setterDelegate =
                    Expression.Lambda<SetterDelegate>(
                        Expression.Assign(Expression.Property(instance, property),
                            Expression.Convert(value, property.PropertyType)), parameter, value).Compile();
            }

            internal MemberInfo member;

            internal GetterDelegate getterDelegate
            {
                get
                {
                    if (_getterDelegate == null)
                    {
                        InitGetter();
                    }

                    return _getterDelegate;
                }
            }

            private GetterDelegate _getterDelegate;

            internal SetterDelegate setterDelegate
            {
                get
                {
                    if (_setterDelegate == null)
                    {
                        InitSetter();
                    }

                    return _setterDelegate;
                }
            }

            private SetterDelegate _setterDelegate;

            internal bool useReflection;
            internal bool readOnly;
            internal bool writeOnly;
            internal bool isStatic;
            internal Type propertyType;

            private bool? _isHidden;

            internal override bool IsHidden
            {
                get
                {
                    _isHidden ??= member.GetCustomAttributes(typeof(HiddenAttribute), inherit: false).Length != 0;

                    return _isHidden.Value;
                }
            }

            private AttributeCollection _attributes;

            internal AttributeCollection Attributes
            {
                get
                {
                    if (_attributes == null)
                    {
                        // Since AttributeCollection can only be constructed with an Attribute[], one is built.
                        var objAttributes = this.member.GetCustomAttributes(true);
                        _attributes = new AttributeCollection(objAttributes.Cast<Attribute>().ToArray());
                    }

                    return _attributes;
                }
            }
        }

        /// <summary>
        /// Compare the signatures of the methods, returning true if the methods have
        /// the same signature.
        /// </summary>
        private static bool SameSignature(MethodBase method1, MethodBase method2)
        {
            if (method1.GetGenericArguments().Length != method2.GetGenericArguments().Length)
            {
                return false;
            }

            ParameterInfo[] parameters1 = method1.GetParameters();
            ParameterInfo[] parameters2 = method2.GetParameters();
            if (parameters1.Length != parameters2.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters1.Length; ++i)
            {
                if (parameters1[i].ParameterType != parameters2[i].ParameterType
                    || parameters1[i].IsOut != parameters2[i].IsOut
                    || parameters1[i].IsOptional != parameters2[i].IsOptional)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds an overload to a list of MethodInfo.  Before adding to the list, the
        /// list is searched to make sure we don't end up with 2 functions with the
        /// same signature.  This can happen when there is a newslot method.
        /// </summary>
        private static void AddOverload(List<MethodBase> previousMethodEntry, MethodInfo method)
        {
            bool add = true;

            for (int i = 0; i < previousMethodEntry.Count; i++)
            {
                if (SameSignature(previousMethodEntry[i], method))
                {
                    add = false;
                    break;
                }
            }

            if (add)
            {
                previousMethodEntry.Add(method);
            }
        }

        private static void PopulateMethodReflectionTable(Type type, MethodInfo[] methods, CacheTable typeMethods)
        {
            foreach (MethodInfo method in methods)
            {
                if (method.DeclaringType == type)
                {
                    string methodName = method.Name;
                    var previousMethodEntry = (List<MethodBase>)typeMethods[methodName];
                    if (previousMethodEntry == null)
                    {
                        var methodEntry = new List<MethodBase> { method };
                        typeMethods.Add(methodName, methodEntry);
                    }
                    else
                    {
                        AddOverload(previousMethodEntry, method);
                    }
                }
            }

            if (type.BaseType != null)
            {
                PopulateMethodReflectionTable(type.BaseType, methods, typeMethods);
            }
        }

        private static void PopulateMethodReflectionTable(ConstructorInfo[] ctors, CacheTable typeMethods)
        {
            foreach (ConstructorInfo ctor in ctors)
            {
                var previousMethodEntry = (List<MethodBase>)typeMethods["new"];
                if (previousMethodEntry == null)
                {
                    var methodEntry = new List<MethodBase>();
                    methodEntry.Add(ctor);
                    typeMethods.Add("new", methodEntry);
                }
                else
                {
                    previousMethodEntry.Add(ctor);
                }
            }
        }

        /// <summary>
        /// Called from GetMethodReflectionTable within a lock to fill the
        /// method cache table.
        /// </summary>
        /// <param name="type">Type to get methods from.</param>
        /// <param name="typeMethods">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        private static void PopulateMethodReflectionTable(Type type, CacheTable typeMethods, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);

            MethodInfo[] methods = type.GetMethods(bindingFlags);
            PopulateMethodReflectionTable(type, methods, typeMethods);

            Type[] interfaces = type.GetInterfaces();
            foreach (Type interfaceType in interfaces)
            {
                if (!TypeResolver.IsPublic(interfaceType))
                {
                    continue;
                }

                if (interfaceType.IsGenericType && type.IsArray)
                {
                    // A bit of background: Array doesn't directly support any generic interface at all. Instead, a stub class
                    // named 'SZArrayHelper' provides these generic interfaces at runtime for zero-based one-dimension arrays.
                    // This is why '[object[]].GetInterfaceMap([ICollection[object]])' throws 'ArgumentException'.
                    // (see https://stackoverflow.com/a/31883327)
                    //
                    // We had always been skipping generic interfaces for array types because 'GetInterfaceMap' doesn't work
                    // for it. Today, even though we don't use 'GetInterfaceMap' anymore, the same code is kept here because
                    // methods from generic interfaces of an array type could cause ambiguity in method overloads resolution.
                    // For example, "$objs = @(1,2,3,4); $objs.Contains(1)" would fail because there would be 2 overloads of
                    // the 'Contains' methods which are equally good matches for the call.
                    //    bool IList.Contains(System.Object value)
                    //    bool ICollection[Object].Contains(System.Object item)
                    continue;
                }

                methods = interfaceType.GetMethods(bindingFlags);

                foreach (MethodInfo interfaceMethod in methods)
                {
                    if (isStatic && interfaceMethod.IsVirtual)
                    {
                        // Ignore static virtual/abstract methods on an interface because:
                        //  1. if it's implicitly implemented, which will be mostly the case, then the corresponding
                        //     methods were already retrieved from the 'type.GetMethods' step above;
                        //  2. if it's explicitly implemented, we cannot call 'Invoke(null, args)' on the static method,
                        //     but have to use 'type.GetInterfaceMap(interfaceType)' to get the corresponding target
                        //     methods, and call 'Invoke(null, args)' on them. The target methods will be non-public
                        //     in this case, which we always ignore.
                        //  3. The recommendation from .NET team is to ignore the static virtuals on interfaces,
                        //     especially given that the APIs may change in .NET 7.
                        continue;
                    }

                    var previousMethodEntry = (List<MethodBase>)typeMethods[interfaceMethod.Name];
                    if (previousMethodEntry == null)
                    {
                        var methodEntry = new List<MethodBase> { interfaceMethod };
                        typeMethods.Add(interfaceMethod.Name, methodEntry);
                    }
                    else
                    {
                        if (!previousMethodEntry.Contains(interfaceMethod))
                        {
                            previousMethodEntry.Add(interfaceMethod);
                        }
                    }
                }
            }

            if ((bindingFlags & BindingFlags.Static) != 0 && TypeResolver.IsPublic(type))
            {
                // We don't add constructors if there was a static method named new.
                // We don't add constructors if the target type is not public, because it's useless to an internal type.
                var previousMethodEntry = (List<MethodBase>)typeMethods["new"];
                if (previousMethodEntry == null)
                {
                    var ctorBindingFlags = bindingFlags & ~(BindingFlags.FlattenHierarchy | BindingFlags.Static);
                    ctorBindingFlags |= BindingFlags.Instance;
                    var ctorInfos = type.GetConstructors(ctorBindingFlags);
                    PopulateMethodReflectionTable(ctorInfos, typeMethods);
                }
            }

            for (int i = 0; i < typeMethods.memberCollection.Count; i++)
            {
                typeMethods.memberCollection[i] =
                    new MethodCacheEntry((List<MethodBase>)typeMethods.memberCollection[i]);
            }
        }

        /// <summary>
        /// Called from GetEventReflectionTable within a lock to fill the
        /// event cache table.
        /// </summary>
        /// <param name="type">Type to get events from.</param>
        /// <param name="typeEvents">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        private static void PopulateEventReflectionTable(Type type, Dictionary<string, EventCacheEntry> typeEvents, BindingFlags bindingFlags)
        {
            EventInfo[] events = type.GetEvents(bindingFlags);
            var tempTable = new Dictionary<string, List<EventInfo>>(StringComparer.OrdinalIgnoreCase);

            foreach (EventInfo typeEvent in events)
            {
                string eventName = typeEvent.Name;
                if (!tempTable.TryGetValue(eventName, out List<EventInfo> entryList))
                {
                    entryList = new List<EventInfo>();
                    tempTable.Add(eventName, entryList);
                }

                entryList.Add(typeEvent);
            }

            foreach (KeyValuePair<string, List<EventInfo>> entry in tempTable)
            {
                typeEvents.Add(entry.Key, new EventCacheEntry(entry.Value.ToArray()));
            }
        }

        /// <summary>
        /// This method is necessary because an overridden property in a specific class derived from a generic one will
        /// appear twice. The second time, it should be ignored.
        /// </summary>
        private static bool PropertyAlreadyPresent(List<PropertyInfo> previousProperties, PropertyInfo property)
        {
            // The loop below
            bool returnValue = false;
            ParameterInfo[] propertyParameters = property.GetIndexParameters();
            int propertyIndexLength = propertyParameters.Length;

            foreach (PropertyInfo previousProperty in previousProperties)
            {
                ParameterInfo[] previousParameters = previousProperty.GetIndexParameters();
                if (previousParameters.Length == propertyIndexLength)
                {
                    bool parametersAreSame = true;
                    for (int parameterIndex = 0; parameterIndex < previousParameters.Length; parameterIndex++)
                    {
                        ParameterInfo previousParameter = previousParameters[parameterIndex];
                        ParameterInfo propertyParameter = propertyParameters[parameterIndex];
                        if (previousParameter.ParameterType != propertyParameter.ParameterType)
                        {
                            parametersAreSame = false;
                            break;
                        }
                    }

                    if (parametersAreSame)
                    {
                        returnValue = true;
                        break;
                    }
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Called from GetPropertyReflectionTable within a lock to fill the
        /// property cache table.
        /// </summary>
        /// <param name="type">Type to get properties from.</param>
        /// <param name="typeProperties">Table to be filled.</param>
        /// <param name="bindingFlags">BindingFlags to use.</param>
        private static void PopulatePropertyReflectionTable(Type type, CacheTable typeProperties, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            var tempTable = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase);

            PropertyInfo[] properties = type.GetProperties(bindingFlags);
            foreach (PropertyInfo property in properties)
            {
                PopulateSingleProperty(type, property, tempTable, property.Name);
            }

            Type[] interfaces = type.GetInterfaces();
            foreach (Type interfaceType in interfaces)
            {
                if (!TypeResolver.IsPublic(interfaceType))
                {
                    continue;
                }

                properties = interfaceType.GetProperties(bindingFlags);
                foreach (PropertyInfo property in properties)
                {
                    if (isStatic &&
                        (property.GetMethod?.IsVirtual == true || property.SetMethod?.IsVirtual == true))
                    {
                        // Ignore static virtual/abstract properties on an interface because:
                        //  1. if it's implicitly implemented, which will be mostly the case, then the corresponding
                        //     properties were already retrieved from the 'type.GetProperties' step above;
                        //  2. if it's explicitly implemented, we cannot call 'GetValue(null)' on the static property,
                        //     but have to use 'type.GetInterfaceMap(interfaceType)' to get the corresponding target
                        //     get/set accessor methods, and call 'Invoke(null, args)' on them. The target methods will
                        //     be non-public in this case, which we always ignore.
                        //  3. The recommendation from .NET team is to ignore the static virtuals on interfaces,
                        //     especially given that the APIs may change in .NET 7.
                        continue;
                    }

                    PopulateSingleProperty(type, property, tempTable, property.Name);
                }
            }

            foreach (KeyValuePair<string, List<PropertyInfo>> entry in tempTable)
            {
                List<PropertyInfo> propertiesList = entry.Value;
                PropertyInfo firstProperty = propertiesList[0];
                if ((propertiesList.Count > 1) || (firstProperty.GetIndexParameters().Length != 0))
                {
                    typeProperties.Add(entry.Key, new ParameterizedPropertyCacheEntry(propertiesList));
                }
                else
                {
                    typeProperties.Add(entry.Key, new PropertyCacheEntry(firstProperty));
                }
            }

            FieldInfo[] fields = type.GetFields(bindingFlags);
            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;
                var previousMember = (PropertyCacheEntry)typeProperties[fieldName];
                if (previousMember == null)
                {
                    typeProperties.Add(fieldName, new PropertyCacheEntry(field));
                }
                else if (!string.Equals(previousMember.member.Name, fieldName))
                {
                    // A property/field declared with 'new' in a derived class might appear twice, and it's OK to ignore
                    // the second property/field in that case.
                    // However, if the names of two properties/fields are different only in letter casing, then it's not
                    // CLS complaint and we throw an exception.
                    throw new ExtendedTypeSystemException(
                        "NotACLSComplaintField",
                        innerException: null,
                        ExtendedTypeSystem.NotAClsCompliantFieldProperty,
                        fieldName,
                        type.FullName,
                        previousMember.member.Name);
                }
            }
        }

        private static void PopulateSingleProperty(Type type, PropertyInfo property, Dictionary<string, List<PropertyInfo>> tempTable, string propertyName)
        {
            List<PropertyInfo> previousPropertyEntry;
            if (!tempTable.TryGetValue(propertyName, out previousPropertyEntry))
            {
                previousPropertyEntry = new List<PropertyInfo> { property };
                tempTable.Add(propertyName, previousPropertyEntry);
            }
            else
            {
                var firstProperty = previousPropertyEntry[0];
                if (!string.Equals(property.Name, firstProperty.Name, StringComparison.Ordinal))
                {
                    throw new ExtendedTypeSystemException("NotACLSComplaintProperty", null,
                                                          ExtendedTypeSystem.NotAClsCompliantFieldProperty, property.Name, type.FullName, firstProperty.Name);
                }

                if (PropertyAlreadyPresent(previousPropertyEntry, property))
                {
                    return;
                }

                previousPropertyEntry.Add(property);
            }
        }

        /// <summary>
        /// Called from GetProperty and GetProperties to populate the
        /// typeTable with all public properties and fields
        /// of type.
        /// </summary>
        /// <param name="type">Type to load properties for.</param>
        private static CacheTable GetStaticPropertyReflectionTable(Type type)
        {
            lock (s_staticPropertyCacheTable)
            {
                CacheTable typeTable = null;
                if (s_staticPropertyCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulatePropertyReflectionTable(type, typeTable, staticBindingFlags);
                s_staticPropertyCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for static methods.
        /// </summary>
        /// <param name="type">Type to load methods for.</param>
        private static CacheTable GetStaticMethodReflectionTable(Type type)
        {
            lock (s_staticMethodCacheTable)
            {
                CacheTable typeTable = null;
                if (s_staticMethodCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulateMethodReflectionTable(type, typeTable, staticBindingFlags);
                s_staticMethodCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for static events.
        /// </summary>
        /// <param name="type">Type containing properties to load in typeTable.</param>
        private static Dictionary<string, EventCacheEntry> GetStaticEventReflectionTable(Type type)
        {
            lock (s_staticEventCacheTable)
            {
                Dictionary<string, EventCacheEntry> typeTable;
                if (s_staticEventCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new Dictionary<string, EventCacheEntry>();
                PopulateEventReflectionTable(type, typeTable, staticBindingFlags);
                s_staticEventCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        /// <summary>
        /// Called from GetProperty and GetProperties to populate the
        /// typeTable with all public properties and fields
        /// of type.
        /// </summary>
        /// <param name="type">Type with properties to load in typeTable.</param>
        private static CacheTable GetInstancePropertyReflectionTable(Type type)
        {
            lock (s_instancePropertyCacheTable)
            {
                CacheTable typeTable = null;
                if (s_instancePropertyCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulatePropertyReflectionTable(type, typeTable, instanceBindingFlags);
                s_instancePropertyCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        /// <summary>
        /// Retrieves the table for instance methods.
        /// </summary>
        /// <param name="type">Type with methods to load in typeTable.</param>
        private static CacheTable GetInstanceMethodReflectionTable(Type type)
        {
            lock (s_instanceMethodCacheTable)
            {
                CacheTable typeTable = null;
                if (s_instanceMethodCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new CacheTable();
                PopulateMethodReflectionTable(type, typeTable, instanceBindingFlags);
                s_instanceMethodCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        internal IEnumerable<object> GetPropertiesAndMethods(Type type, bool @static)
        {
            CacheTable propertyTable = @static
                ? GetStaticPropertyReflectionTable(type)
                : GetInstancePropertyReflectionTable(type);
            for (int i = 0; i < propertyTable.memberCollection.Count; i++)
            {
                var propertyCacheEntry = propertyTable.memberCollection[i] as PropertyCacheEntry;
                if (propertyCacheEntry != null)
                    yield return propertyCacheEntry.member;
            }

            CacheTable methodTable = @static
                ? GetStaticMethodReflectionTable(type)
                : GetInstanceMethodReflectionTable(type);
            for (int i = 0; i < methodTable.memberCollection.Count; i++)
            {
                var method = methodTable.memberCollection[i] as MethodCacheEntry;
                if (method != null && !method[0].method.IsSpecialName)
                {
                    yield return method;
                }
            }
        }

        /// <summary>
        /// Retrieves the table for instance events.
        /// </summary>
        /// <param name="type">Type containing methods to load in typeTable.</param>
        private static Dictionary<string, EventCacheEntry> GetInstanceEventReflectionTable(Type type)
        {
            lock (s_instanceEventCacheTable)
            {
                Dictionary<string, EventCacheEntry> typeTable;
                if (s_instanceEventCacheTable.TryGetValue(type, out typeTable))
                {
                    return typeTable;
                }

                typeTable = new Dictionary<string, EventCacheEntry>(StringComparer.OrdinalIgnoreCase);
                PopulateEventReflectionTable(type, typeTable, instanceBindingFlags);
                s_instanceEventCacheTable[type] = typeTable;
                return typeTable;
            }
        }

        /// <summary>
        /// Returns true if a parameterized property should be in a PSMemberInfoCollection of type t.
        /// </summary>
        /// <param name="t">Type of a PSMemberInfoCollection like the type of T in PSMemberInfoCollection of T.</param>
        /// <returns>True if a parameterized property should be in a collection.</returns>
        /// <remarks>
        /// Usually typeof(T).IsAssignableFrom(typeof(PSParameterizedProperty)) would work like it does
        /// for PSMethod and PSProperty, but since PSParameterizedProperty derives from PSMethodInfo and
        /// since we don't want to have ParameterizedProperties in PSMemberInfoCollection of PSMethodInfo
        /// we need this method.
        /// </remarks>
        internal static bool IsTypeParameterizedProperty(Type t)
        {
            return t == typeof(PSMemberInfo) || t == typeof(PSParameterizedProperty);
        }

        private T GetDotNetPropertyImpl<T>(object obj, string propertyName, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = IsTypeParameterizedProperty(typeof(T));
            if (!lookingForProperties && !lookingForParameterizedProperties)
            {
                return null;
            }

            CacheTable typeTable = _isStatic
                                       ? GetStaticPropertyReflectionTable((Type)obj)
                                       : GetInstancePropertyReflectionTable(obj.GetType());

            object entry = predicate != null
                               ? typeTable.GetFirstOrDefault(predicate)
                               : typeTable[propertyName];
            switch (entry)
            {
                case null:
                    return null;
                case PropertyCacheEntry cacheEntry when lookingForProperties:
                    return new PSProperty(cacheEntry.member.Name, this, obj, cacheEntry) { IsHidden = cacheEntry.IsHidden } as T;
                case ParameterizedPropertyCacheEntry paramCacheEntry when lookingForParameterizedProperties:

                    // TODO: check for HiddenAttribute
                    // We can't currently write a parameterized property in a PowerShell class so this isn't too important,
                    // but if someone added the attribute to their C#, it'd be good to set isHidden correctly here.
                    return new PSParameterizedProperty(paramCacheEntry.propertyName, this, obj, paramCacheEntry) as T;
                default: return null;
            }
        }

        private T GetDotNetMethodImpl<T>(object obj, string methodName, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return null;
            }

            CacheTable typeTable = _isStatic
                                       ? GetStaticMethodReflectionTable((Type)obj)
                                       : GetInstanceMethodReflectionTable(obj.GetType());

            var methods = predicate != null
                              ? (MethodCacheEntry)typeTable.GetFirstOrDefault(predicate)
                              : (MethodCacheEntry)typeTable[methodName];

            if (methods == null)
            {
                return null;
            }

            var isCtor = methods[0].method is ConstructorInfo;
            bool isSpecial = !isCtor && methods[0].method.IsSpecialName;

            return PSMethod.Create(methods[0].method.Name, this, obj, methods, isSpecial, methods.IsHidden) as T;
        }

        internal T GetDotNetProperty<T>(object obj, string propertyName) where T : PSMemberInfo
        {
            return GetDotNetPropertyImpl<T>(obj, propertyName, predicate: null);
        }

        internal T GetDotNetMethod<T>(object obj, string methodName) where T : PSMemberInfo
        {
            return GetDotNetMethodImpl<T>(obj, methodName, predicate: null);
        }

        protected T GetFirstDotNetPropertyOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            return GetDotNetPropertyImpl<T>(obj, propertyName: null, predicate);
        }

        protected T GetFirstDotNetMethodOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            return GetDotNetMethodImpl<T>(obj, methodName: null, predicate);
        }

        protected T GetFirstDotNetEventOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSEvent)))
            {
                return null;
            }

            var table = _isStatic
                ? GetStaticEventReflectionTable((Type)obj)
                : GetInstanceEventReflectionTable(obj.GetType());

            foreach (var psEvent in table.Values)
            {
                if (predicate(psEvent.events[0].Name))
                {
                    return new PSEvent(psEvent.events[0]) as T;
                }
            }

            return null;
        }

        protected T GetFirstDynamicMemberOrDefault<T>(object obj, MemberNamePredicate predicate) where T : PSMemberInfo
        {
            var idmop = obj as IDynamicMetaObjectProvider;
            if (idmop == null || obj is PSObject)
            {
                return null;
            }

            if (!typeof(T).IsAssignableFrom(typeof(PSDynamicMember)))
            {
                return null;
            }

            foreach (var name in idmop.GetMetaObject(Expression.Variable(idmop.GetType())).GetDynamicMemberNames())
            {
                if (predicate(name))
                {
                    return new PSDynamicMember(name) as T;
                }
            }

            return null;
        }

        internal void AddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members, bool ignoreDuplicates) where T : PSMemberInfo
        {
            bool lookingForProperties = typeof(T).IsAssignableFrom(typeof(PSProperty));
            bool lookingForParameterizedProperties = IsTypeParameterizedProperty(typeof(T));
            if (!lookingForProperties && !lookingForParameterizedProperties)
            {
                return;
            }

            CacheTable table = _isStatic
                ? GetStaticPropertyReflectionTable((Type)obj)
                : GetInstancePropertyReflectionTable(obj.GetType());

            for (int i = 0; i < table.memberCollection.Count; i++)
            {
                if (table.memberCollection[i] is PropertyCacheEntry propertyEntry)
                {
                    if (lookingForProperties)
                    {
                        if (!ignoreDuplicates || (members[propertyEntry.member.Name] == null))
                        {
                            members.Add(
                                new PSProperty(
                                    name: propertyEntry.member.Name,
                                    adapter: this,
                                    baseObject: obj,
                                    adapterData: propertyEntry)
                                { IsHidden = propertyEntry.IsHidden } as T);
                        }
                    }
                }
                else if (lookingForParameterizedProperties)
                {
                    var parameterizedPropertyEntry = (ParameterizedPropertyCacheEntry)table.memberCollection[i];
                    if (!ignoreDuplicates || (members[parameterizedPropertyEntry.propertyName] == null))
                    {
                        // TODO: check for HiddenAttribute
                        // We can't currently write a parameterized property in a PowerShell class so this isn't too important,
                        // but if someone added the attribute to their C#, it'd be good to set isHidden correctly here.
                        members.Add(new PSParameterizedProperty(parameterizedPropertyEntry.propertyName,
                            this, obj, parameterizedPropertyEntry) as T);
                    }
                }
            }
        }

        internal void AddAllMethods<T>(object obj, PSMemberInfoInternalCollection<T> members, bool ignoreDuplicates) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                return;
            }

            CacheTable table = _isStatic
                ? GetStaticMethodReflectionTable((Type)obj)
                : GetInstanceMethodReflectionTable(obj.GetType());

            for (int i = 0; i < table.memberCollection.Count; i++)
            {
                var method = (MethodCacheEntry)table.memberCollection[i];
                var isCtor = method[0].method is ConstructorInfo;
                var name = isCtor ? "new" : method[0].method.Name;

                if (!ignoreDuplicates || (members[name] == null))
                {
                    bool isSpecial = !isCtor && method[0].method.IsSpecialName;
                    members.Add(PSMethod.Create(name, this, obj, method, isSpecial, method.IsHidden) as T);
                }
            }
        }

        internal void AddAllEvents<T>(object obj, PSMemberInfoInternalCollection<T> members, bool ignoreDuplicates) where T : PSMemberInfo
        {
            if (!typeof(T).IsAssignableFrom(typeof(PSEvent)))
            {
                return;
            }

            var table = _isStatic
                ? GetStaticEventReflectionTable((Type)obj)
                : GetInstanceEventReflectionTable(obj.GetType());

            foreach (var psEvent in table.Values)
            {
                if (!ignoreDuplicates || (members[psEvent.events[0].Name] == null))
                {
                    members.Add(new PSEvent(psEvent.events[0]) as T);
                }
            }
        }

        internal void AddAllDynamicMembers<T>(object obj, PSMemberInfoInternalCollection<T> members, bool ignoreDuplicates) where T : PSMemberInfo
        {
            var idmop = obj as IDynamicMetaObjectProvider;
            if (idmop == null || obj is PSObject)
            {
                return;
            }

            if (!typeof(T).IsAssignableFrom(typeof(PSDynamicMember)))
            {
                return;
            }

            foreach (var name in idmop.GetMetaObject(Expression.Variable(idmop.GetType())).GetDynamicMemberNames())
            {
                members.Add(new PSDynamicMember(name) as T);
            }
        }

        private static bool PropertyIsStatic(PSProperty property)
        {
            if (property.adapterData is not PropertyCacheEntry entry)
            {
                return false;
            }

            return entry.isStatic;
        }

        /// <summary>
        /// Get the string representation of the default value of passed-in parameter.
        /// </summary>
        /// <param name="parameterInfo">ParameterInfo containing the parameter's default value.</param>
        /// <returns>String representation of the parameter's default value.</returns>
        private static string GetDefaultValueStringRepresentation(ParameterInfo parameterInfo)
        {
            var parameterType = parameterInfo.ParameterType;
            var parameterDefaultValue = parameterInfo.DefaultValue;

            if (parameterDefaultValue == null)
            {
                return (parameterType.IsValueType || parameterType.IsGenericMethodParameter)
                    ? "default"
                    : "null";
            }

            if (parameterType.IsEnum)
            {
                return string.Create(CultureInfo.InvariantCulture, $"{parameterType}.{parameterDefaultValue}");
            }

            return (parameterDefaultValue is string)
                ? string.Create(CultureInfo.InvariantCulture, $"\"{parameterDefaultValue}\"")
                : parameterDefaultValue.ToString();
        }

        #endregion auxiliary methods and classes

        #region virtual

        #region member

        internal override bool CanSiteBinderOptimize(MemberTypes typeToOperateOn) { return true; }

        private static readonly ConcurrentDictionary<Type, ConsolidatedString> s_typeToTypeNameDictionary =
            new ConcurrentDictionary<Type, ConsolidatedString>();

        internal static ConsolidatedString GetInternedTypeNameHierarchy(Type type)
        {
            return s_typeToTypeNameDictionary.GetOrAdd(type,
                                                     t => new ConsolidatedString(GetDotNetTypeNameHierarchy(t), interned: true));
        }

        protected override ConsolidatedString GetInternedTypeNameHierarchy(object obj)
        {
            return GetInternedTypeNameHierarchy(obj.GetType());
        }

        /// <summary>
        /// Get the .NET member based on the given member name.
        /// </summary>
        /// <remarks>
        /// Dynamic members of an object that implements IDynamicMetaObjectProvider are not included because
        ///   1. Dynamic members cannot be invoked via reflection;
        ///   2. Access to dynamic members is handled by the DLR for free.
        /// </remarks>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="memberName">Name of the member to be retrieved.</param>
        /// <returns>
        /// The PSMemberInfo corresponding to memberName from obj,
        /// or null if the given member name is not a member in the adapter.
        /// </returns>
        protected override T GetMember<T>(object obj, string memberName)
        {
            T returnValue = GetDotNetProperty<T>(obj, memberName);
            if (returnValue != null) return returnValue;
            return GetDotNetMethod<T>(obj, memberName);
        }

        /// <summary>
        /// Get the first .NET member whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// </summary>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            return GetFirstDotNetPropertyOrDefault<T>(obj, predicate) ?? GetFirstDotNetMethodOrDefault<T>(obj, predicate) ??
                   GetFirstDotNetEventOrDefault<T>(obj, predicate) ?? GetFirstDynamicMemberOrDefault<T>(obj, predicate);
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
        /// <remarks>
        /// Dynamic members of an object that implements IDynamicMetaObjectProvider are included because
        /// we want to view the dynamic members via 'Get-Member' and be able to auto-complete those members.
        /// </remarks>
        /// <param name="obj">Object to get all the member information from.</param>
        /// <returns>All members in obj.</returns>
        protected override PSMemberInfoInternalCollection<T> GetMembers<T>(object obj)
        {
            PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();
            AddAllProperties<T>(obj, returnValue, false);
            AddAllMethods<T>(obj, returnValue, false);
            AddAllEvents<T>(obj, returnValue, false);
            AddAllDynamicMembers(obj, returnValue, false);

            return returnValue;
        }

        #endregion member

        #region property

        /// <summary>
        /// Returns an array with the property attributes.
        /// </summary>
        /// <param name="property">Property we want the attributes from.</param>
        /// <returns>An array with the property attributes.</returns>
        protected override AttributeCollection PropertyAttributes(PSProperty property)
        {
            PropertyCacheEntry adapterData = (PropertyCacheEntry)property.adapterData;
            return adapterData.Attributes;
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected override string PropertyToString(PSProperty property)
        {
            StringBuilder returnValue = new StringBuilder();
            if (PropertyIsStatic(property))
            {
                returnValue.Append("static ");
            }

            returnValue.Append(PropertyType(property, forDisplay: true));
            returnValue.Append(' ');
            returnValue.Append(property.Name);
            returnValue.Append(" {");
            if (PropertyIsGettable(property))
            {
                returnValue.Append("get;");
            }

            if (PropertyIsSettable(property))
            {
                returnValue.Append("set;");
            }

            returnValue.Append('}');
            return returnValue.ToString();
        }

        /// <summary>
        /// Returns the value from a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            PropertyCacheEntry adapterData = (PropertyCacheEntry)property.adapterData;

            if (adapterData.propertyType.IsByRefLike)
            {
                throw new GetValueException(
                    nameof(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField),
                    innerException: null,
                    ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField,
                    adapterData.member.Name,
                    adapterData.propertyType);
            }

            PropertyInfo propertyInfo = adapterData.member as PropertyInfo;
            if (propertyInfo != null)
            {
                if (adapterData.writeOnly)
                {
                    throw new GetValueException(
                        nameof(ExtendedTypeSystem.WriteOnlyProperty),
                        innerException: null,
                        ExtendedTypeSystem.WriteOnlyProperty,
                        propertyInfo.Name);
                }

                if (adapterData.useReflection)
                {
                    return propertyInfo.GetValue(property.baseObject, null);
                }
                else
                {
                    return adapterData.getterDelegate(property.baseObject);
                }
            }

            FieldInfo field = adapterData.member as FieldInfo;
            if (adapterData.useReflection)
            {
                return field?.GetValue(property.baseObject);
            }
            else
            {
                return adapterData.getterDelegate(property.baseObject);
            }
        }

        /// <summary>
        /// Sets the value of a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            PropertyCacheEntry adapterData = (PropertyCacheEntry)property.adapterData;

            if (adapterData.readOnly)
            {
                throw new SetValueException(
                    nameof(ExtendedTypeSystem.ReadOnlyProperty),
                    innerException: null,
                    ExtendedTypeSystem.ReadOnlyProperty,
                    adapterData.member.Name);
            }

            if (adapterData.propertyType.IsByRefLike)
            {
                throw new SetValueException(
                    nameof(ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField),
                    innerException: null,
                    ExtendedTypeSystem.CannotAccessByRefLikePropertyOrField,
                    adapterData.member.Name,
                    adapterData.propertyType);
            }

            PropertyInfo propertyInfo = adapterData.member as PropertyInfo;
            if (propertyInfo != null)
            {
                if (convertIfPossible)
                {
                    setValue = PropertySetAndMethodArgumentConvertTo(setValue, propertyInfo.PropertyType, CultureInfo.InvariantCulture);
                }

                if (adapterData.useReflection)
                {
                    propertyInfo.SetValue(property.baseObject, setValue, null);
                }
                else
                {
                    adapterData.setterDelegate(property.baseObject, setValue);
                }

                return;
            }

            FieldInfo field = adapterData.member as FieldInfo;
            if (convertIfPossible)
            {
                setValue = PropertySetAndMethodArgumentConvertTo(setValue, field.FieldType, CultureInfo.InvariantCulture);
            }

            if (adapterData.useReflection)
            {
                field.SetValue(property.baseObject, setValue);
            }
            else
            {
                adapterData.setterDelegate(property.baseObject, setValue);
            }
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            return !((PropertyCacheEntry)property.adapterData).readOnly;
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            return !((PropertyCacheEntry)property.adapterData).writeOnly;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous GetMember.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            var propertyType = ((PropertyCacheEntry)property.adapterData).propertyType;
            return forDisplay ? ToStringCodeMethods.Type(propertyType) : propertyType.FullName;
        }

        #endregion property

        #region method

        #region auxiliary to method calling

        /// <summary>
        /// Calls constructor using the arguments and catching the appropriate exception.
        /// </summary>
        /// <param name="arguments">Final arguments to the constructor.</param>
        /// <returns>The return of the constructor.</returns>
        /// <param name="methodInformation">Information about the method to call. Used for setting references.</param>
        /// <param name="originalArguments">Original arguments in the method call. Used for setting references.</param>
        /// <exception cref="MethodInvocationException">If the constructor throws an exception.</exception>
        internal static object AuxiliaryConstructorInvoke(MethodInformation methodInformation, object[] arguments, object[] originalArguments)
        {
            object returnValue;
#pragma warning disable 56500
            try
            {
                returnValue = methodInformation.Invoke(target: null, arguments);
            }
            catch (TargetInvocationException ex)
            {
                Exception inner = ex.InnerException ?? ex;
                throw new MethodInvocationException(
                    "DotNetconstructorTargetInvocation",
                    inner,
                    ExtendedTypeSystem.MethodInvocationException,
                    ".ctor", arguments.Length, inner.Message);
            }
            catch (Exception e)
            {
                throw new MethodInvocationException(
                    "DotNetconstructorException",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    ".ctor", arguments.Length, e.Message);
            }

            SetReferences(arguments, methodInformation, originalArguments);
            return returnValue;
#pragma warning restore 56500
        }

        /// <summary>
        /// Calls method on target using the arguments and catching the appropriate exception.
        /// </summary>
        /// <param name="target">Object we want to call the method on.</param>
        /// <param name="arguments">Final arguments to the method.</param>
        /// <param name="methodInformation">Information about the method to call. Used for setting references.</param>
        /// <param name="originalArguments">Original arguments in the method call. Used for setting references.</param>
        /// <returns>The return of the method.</returns>
        /// <exception cref="MethodInvocationException">If the method throws an exception.</exception>
        internal static object AuxiliaryMethodInvoke(object target, object[] arguments, MethodInformation methodInformation, object[] originalArguments)
        {
            object result;

#pragma warning disable 56500
            try
            {
                // call the method and return the result unless the return type is void in which
                // case we'll return AutomationNull.Value
                result = methodInformation.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex)
            {
                // Special handling to allow methods to throw flowcontrol exceptions
                // Needed for ExitNestedPrompt exception.
                if (ex.InnerException is FlowControlException || ex.InnerException is ScriptCallDepthException)
                    throw ex.InnerException;
                // Win7:138054 - When wrapping cmdlets, we want the original exception to be raised,
                // not the wrapped exception that occurs from invoking a steppable pipeline method.
                if (ex.InnerException is ParameterBindingException)
                    throw ex.InnerException;

                Exception inner = ex.InnerException ?? ex;

                throw new MethodInvocationException(
                    "DotNetMethodTargetInvocation",
                    inner,
                    ExtendedTypeSystem.MethodInvocationException,
                    methodInformation.method.Name, arguments.Length, inner.Message);
            }
            //
            // Note that FlowControlException, ScriptCallDepthException and ParameterBindingException will be wrapped in
            // a TargetInvocationException only when the invocation uses reflection so we need to bubble them up here as well.
            //
            catch (ParameterBindingException) { throw; }
            catch (FlowControlException) { throw; }
            catch (ScriptCallDepthException) { throw; }
            catch (PipelineStoppedException) { throw; }
            catch (Exception e)
            {
                if (methodInformation.method.DeclaringType == typeof(SteppablePipeline) &&
                    (methodInformation.method.Name.Equals("Begin") ||
                     methodInformation.method.Name.Equals("Process") ||
                     methodInformation.method.Name.Equals("End")))
                {
                    // Don't wrap exceptions that happen when calling methods on SteppablePipeline
                    // that are only used for proxy commands.
                    throw;
                }

                throw new MethodInvocationException(
                    "DotNetMethodException",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    methodInformation.method.Name, arguments.Length, e.Message);
            }
#pragma warning restore 56500

            SetReferences(arguments, methodInformation, originalArguments);
            MethodInfo methodInfo = methodInformation.method as MethodInfo;
            if (methodInfo != null && methodInfo.ReturnType != typeof(void))
                return result;
            return AutomationNull.Value;
        }

        /// <summary>
        /// Converts a MethodBase[] into a MethodInformation[]
        /// </summary>
        /// <param name="methods">The methods to be converted.</param>
        /// <returns>The MethodInformation[] corresponding to methods.</returns>
        internal static MethodInformation[] GetMethodInformationArray(IList<MethodBase> methods)
        {
            var returnValue = new MethodInformation[methods.Count];
            for (int i = 0; i < methods.Count; i++)
            {
                returnValue[i] = new MethodInformation(methods[i], 0);
            }

            return returnValue;
        }

        /// <summary>
        /// Calls the method best suited to the arguments on target.
        /// </summary>
        /// <param name="methodName">Used for error messages.</param>
        /// <param name="target">Object to call the method on.</param>
        /// <param name="methodInformation">Method information corresponding to methods.</param>
        /// <param name="invocationConstraints">Invocation constraints.</param>
        /// <param name="arguments">Arguments of the call.</param>
        /// <returns>The return of the method.</returns>
        /// <exception cref="MethodInvocationException">If the method throws an exception.</exception>
        /// <exception cref="MethodException">If we could not find a method for the given arguments.</exception>
        internal static object MethodInvokeDotNet(
            string methodName,
            object target,
            MethodInformation[] methodInformation,
            PSMethodInvocationConstraints invocationConstraints,
            object[] arguments)
        {
            object[] newArguments;
            MethodInformation bestMethod = GetBestMethodAndArguments(methodName, methodInformation, invocationConstraints, arguments, out newArguments);
            if (bestMethod.method is ConstructorInfo)
            {
                return InvokeResolvedConstructor(bestMethod, newArguments, arguments);
            }

            string methodDefinition = bestMethod.methodDefinition;
            ScriptTrace.Trace(1, "TraceMethodCall", ParserStrings.TraceMethodCall, methodDefinition);
            PSObject.MemberResolution.WriteLine("Calling Method: {0}", methodDefinition);
            return AuxiliaryMethodInvoke(target, newArguments, bestMethod, arguments);
        }

        /// <summary>
        /// Calls the method best suited to the arguments on target.
        /// </summary>
        /// <param name="type">The type being constructed, used for diagnostics and caching.</param>
        /// <param name="constructors">All overloads for the constructors.</param>
        /// <param name="arguments">Arguments of the call.</param>
        /// <returns>The return of the method.</returns>
        /// <exception cref="MethodInvocationException">If the method throws an exception.</exception>
        /// <exception cref="MethodException">If we could not find a method for the given arguments.</exception>
        internal static object ConstructorInvokeDotNet(Type type, ConstructorInfo[] constructors, object[] arguments)
        {
            var newConstructors = GetMethodInformationArray(constructors);
            object[] newArguments;
            MethodInformation bestMethod = GetBestMethodAndArguments(type.Name, newConstructors, arguments, out newArguments);
            return InvokeResolvedConstructor(bestMethod, newArguments, arguments);
        }

        private static object InvokeResolvedConstructor(MethodInformation bestMethod, object[] newArguments, object[] arguments)
        {
            if ((PSObject.MemberResolution.Options & PSTraceSourceOptions.WriteLine) != 0)
            {
                PSObject.MemberResolution.WriteLine("Calling Constructor: {0}", DotNetAdapter.GetMethodInfoOverloadDefinition(null,
                    bestMethod.method, 0));
            }

            return AuxiliaryConstructorInvoke(bestMethod, newArguments, arguments);
        }

        /// <summary>
        /// This is a flavor of MethodInvokeDotNet to deal with a peculiarity of property setters:
        /// The setValue is always the last parameter. This enables a parameter after a varargs or optional
        /// parameters and GetBestMethodAndArguments is not prepared for that.
        /// This method disregards the last parameter in its call to GetBestMethodAndArguments used in this case
        /// more for its "Arguments" side than for its "BestMethod" side, since there is only one method.
        /// </summary>
        internal static void ParameterizedPropertyInvokeSet(string propertyName, object target, object valuetoSet, MethodInformation[] methodInformation, object[] arguments)
        {
            // bestMethodIndex is ignored since we know we have only 1 method. GetBestMethodAndArguments
            // is still useful to deal with optional and varargs parameters and to perform the type conversions
            // of all parameters but the last one
            object[] newArguments;
            MethodInformation bestMethod = GetBestMethodAndArguments(propertyName, methodInformation, arguments, out newArguments);
            PSObject.MemberResolution.WriteLine("Calling Set Method: {0}", bestMethod.methodDefinition);
            ParameterInfo[] bestMethodParameters = bestMethod.method.GetParameters();
            Type propertyType = bestMethodParameters[bestMethodParameters.Length - 1].ParameterType;

            // we have to convert the last parameter (valuetoSet) manually since it has been
            // disregarded in GetBestMethodAndArguments.
            object lastArgument;
            try
            {
                lastArgument = PropertySetAndMethodArgumentConvertTo(valuetoSet, propertyType, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException e)
            {
                // NTRAID#Windows Out Of Band Releases-924162-2005/11/17-JonN
                throw new MethodException(
                    "PropertySetterConversionInvalidCastArgument",
                    e,
                    ExtendedTypeSystem.MethodArgumentConversionException,
                    arguments.Length - 1, valuetoSet, propertyName, propertyType, e.Message);
            }

            // and we also have to rebuild the argument array to include the last parameter
            object[] finalArguments = new object[newArguments.Length + 1];
            for (int i = 0; i < newArguments.Length; i++)
            {
                finalArguments[i] = newArguments[i];
            }

            finalArguments[newArguments.Length] = lastArgument;

            AuxiliaryMethodInvoke(target, finalArguments, bestMethod, arguments);
        }

        internal static string GetMethodInfoOverloadDefinition(string memberName, MethodBase methodEntry, int parametersToIgnore)
        {
            StringBuilder builder = new StringBuilder();
            if (methodEntry.IsStatic)
            {
                builder.Append("static ");
            }

            MethodInfo method = methodEntry as MethodInfo;
            if (method != null)
            {
                builder.Append(ToStringCodeMethods.Type(method.ReturnType));
                builder.Append(' ');
            }
            else
            {
                ConstructorInfo ctorInfo = methodEntry as ConstructorInfo;
                if (ctorInfo != null)
                {
                    builder.Append(ToStringCodeMethods.Type(ctorInfo.DeclaringType));
                    builder.Append(' ');
                }
            }

            if (methodEntry.DeclaringType.IsInterface)
            {
                builder.Append(ToStringCodeMethods.Type(methodEntry.DeclaringType, dropNamespaces: true));
                builder.Append('.');
            }

            builder.Append(memberName ?? methodEntry.Name);
            if (methodEntry.IsGenericMethodDefinition || methodEntry.IsGenericMethod)
            {
                builder.Append('[');

                Type[] genericArgs = methodEntry.GetGenericArguments();
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    if (i > 0) { builder.Append(", "); }

                    builder.Append(ToStringCodeMethods.Type(genericArgs[i]));
                }

                builder.Append(']');
            }

            builder.Append('(');
            System.Reflection.ParameterInfo[] parameters = methodEntry.GetParameters();
            int parametersLength = parameters.Length - parametersToIgnore;
            if (parametersLength > 0)
            {
                for (int i = 0; i < parametersLength; i++)
                {
                    System.Reflection.ParameterInfo parameter = parameters[i];
                    var parameterType = parameter.ParameterType;
                    if (parameterType.IsByRef)
                    {
                        builder.Append("[ref] ");
                        parameterType = parameterType.GetElementType();
                    }

                    if (parameterType.IsArray && (i == parametersLength - 1))
                    {
                        // The extension method 'CustomAttributeExtensions.GetCustomAttributes(ParameterInfo, Type, Boolean)' has inconsistent
                        // behavior on its return value in both FullCLR and CoreCLR. According to MSDN, if the attribute cannot be found, it
                        // should return an empty collection. However, it returns null in some rare cases [when the parameter isn't backed by
                        // actual metadata].
                        // This inconsistent behavior affects OneCore powershell because we are using the extension method here when compiling
                        // against CoreCLR. So we need to add a null check until this is fixed in CLR.
                        var paramArrayAttrs = parameter.GetCustomAttributes(typeof(ParamArrayAttribute), false);
                        if (paramArrayAttrs != null && paramArrayAttrs.Length > 0)
                            builder.Append("Params ");
                    }

                    builder.Append(ToStringCodeMethods.Type(parameterType));
                    builder.Append(' ');
                    builder.Append(parameter.Name);

                    if (parameter.HasDefaultValue)
                    {
                        builder.Append(" = ");
                        builder.Append(GetDefaultValueStringRepresentation(parameter));
                    }

                    builder.Append(", ");
                }

                builder.Remove(builder.Length - 2, 2);
            }

            builder.Append(')');

            return builder.ToString();
        }

        #endregion auxiliary to method calling

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, object[] arguments)
        {
            return this.MethodInvoke(method, null, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="invocationConstraints">Invocation constraints.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, PSMethodInvocationConstraints invocationConstraints, object[] arguments)
        {
            MethodCacheEntry methodEntry = (MethodCacheEntry)method.adapterData;
            return DotNetAdapter.MethodInvokeDotNet(
                method.Name,
                method.baseObject,
                methodEntry.methodInformationStructures,
                invocationConstraints,
                arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="method">The return of GetMember.</param>
        /// <returns></returns>
        protected override Collection<string> MethodDefinitions(PSMethod method)
        {
            MethodCacheEntry methodEntry = (MethodCacheEntry)method.adapterData;
            IList<string> uniqueValues = methodEntry
                .methodInformationStructures
                .Select(static m => m.methodDefinition)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            return new Collection<string>(uniqueValues);
        }

        #endregion method

        #region parameterized property

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected override string ParameterizedPropertyType(PSParameterizedProperty property)
        {
            var adapterData = (ParameterizedPropertyCacheEntry)property.adapterData;
            return adapterData.propertyType.FullName;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool ParameterizedPropertyIsSettable(PSParameterizedProperty property)
        {
            return !((ParameterizedPropertyCacheEntry)property.adapterData).readOnly;
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool ParameterizedPropertyIsGettable(PSParameterizedProperty property)
        {
            return !((ParameterizedPropertyCacheEntry)property.adapterData).writeOnly;
        }

        /// <summary>
        /// Called after a non null return from GetMember to get the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the property.</returns>
        protected override object ParameterizedPropertyGet(PSParameterizedProperty property, object[] arguments)
        {
            var adapterData = (ParameterizedPropertyCacheEntry)property.adapterData;
            return DotNetAdapter.MethodInvokeDotNet(property.Name, property.baseObject,
                adapterData.getterInformation, null, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to set the property value.
        /// </summary>
        /// <param name="property">The non empty return from GetMember.</param>
        /// <param name="setValue">The value to set property with.</param>
        /// <param name="arguments">The arguments to use.</param>
        protected override void ParameterizedPropertySet(PSParameterizedProperty property, object setValue, object[] arguments)
        {
            var adapterData = (ParameterizedPropertyCacheEntry)property.adapterData;
            ParameterizedPropertyInvokeSet(adapterData.propertyName, property.baseObject, setValue,
                adapterData.setterInformation, arguments);
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        protected override Collection<string> ParameterizedPropertyDefinitions(PSParameterizedProperty property)
        {
            var adapterData = (ParameterizedPropertyCacheEntry)property.adapterData;
            var returnValue = new Collection<string>();
            for (int i = 0; i < adapterData.propertyDefinition.Length; i++)
            {
                returnValue.Add(adapterData.propertyDefinition[i]);
            }

            return returnValue;
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected override string ParameterizedPropertyToString(PSParameterizedProperty property)
        {
            StringBuilder returnValue = new StringBuilder();
            Collection<string> definitions = ParameterizedPropertyDefinitions(property);
            for (int i = 0; i < definitions.Count; i++)
            {
                returnValue.Append(definitions[i]);
                returnValue.Append(", ");
            }

            returnValue.Remove(returnValue.Length - 2, 2);
            return returnValue.ToString();
        }

        #endregion parameterized property

        #endregion virtual
    }

    #region DotNetAdapterWithOnlyPropertyLookup

    /// <summary>
    /// This is used by PSObject to support dotnet member lookup for the adapted
    /// objects.
    /// </summary>
    /// <remarks>
    /// This class is created to avoid cluttering DotNetAdapter with if () { } blocks .
    /// </remarks>
    internal class BaseDotNetAdapterForAdaptedObjects : DotNetAdapter
    {
        /// <summary>
        /// Return a collection representing the <paramref name="obj"/> object's
        /// members as returned by CLR reflection.
        /// </summary>
        protected override PSMemberInfoInternalCollection<T> GetMembers<T>(object obj)
        {
            PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();
            AddAllProperties<T>(obj, returnValue, true);
            AddAllMethods<T>(obj, returnValue, true);
            AddAllEvents<T>(obj, returnValue, true);

            return returnValue;
        }

        /// <summary>
        /// Returns a member representing the <paramref name="obj"/> as given by CLR reflection.
        /// </summary>
        protected override T GetMember<T>(object obj, string memberName)
        {
            PSProperty property = base.GetDotNetProperty<PSProperty>(obj, memberName);
            if (typeof(T).IsAssignableFrom(typeof(PSProperty)) && (property != null))
            {
                return property as T;
            }

            // In order to not break v1..base dotnet adapter should not return methods
            // when accessed with T as PSMethod.. accessing method with PSMemberInfo
            // is ok as property always gets precedence over methods and duplicates
            // are ignored.
            if (typeof(T) == typeof(PSMemberInfo))
            {
                T returnValue = base.GetDotNetMethod<T>(obj, memberName);
                // We only return a method if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (returnValue != null && property == null)
                {
                    return returnValue;
                }
            }

            if (IsTypeParameterizedProperty(typeof(T)))
            {
                PSParameterizedProperty parameterizedProperty = base.GetDotNetProperty<PSParameterizedProperty>(obj, memberName);
                // We only return a parameterized property if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (parameterizedProperty != null && property == null)
                {
                    return parameterizedProperty as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first reflection member whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            PSProperty property = base.GetFirstDotNetPropertyOrDefault<PSProperty>(obj, predicate);
            if (typeof(T).IsAssignableFrom(typeof(PSProperty)) && property != null)
            {
                return property as T;
            }

            // In order to not break v1..base dotnet adapter should not return methods
            // when accessed with T as PSMethod.. accessing method with PSMemberInfo
            // is ok as property always gets precedence over methods and duplicates
            // are ignored.
            if (typeof(T) == typeof(PSMemberInfo))
            {
                T returnValue = base.GetFirstDotNetMethodOrDefault<T>(obj, predicate);

                // We only return a method if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (returnValue != null && property == null)
                {
                    return returnValue;
                }
            }

            if (IsTypeParameterizedProperty(typeof(T)))
            {
                var parameterizedProperty = base.GetFirstDotNetPropertyOrDefault<PSParameterizedProperty>(obj, predicate);

                // We only return a parameterized property if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (parameterizedProperty != null && property == null)
                {
                    return parameterizedProperty as T;
                }
            }

            return null;
        }
    }

    #endregion

#if !UNIX
    /// <summary>
    /// Used only to add a COM style type name to a COM interop .NET type.
    /// </summary>
    internal class DotNetAdapterWithComTypeName : DotNetAdapter
    {
        private readonly ComTypeInfo _comTypeInfo;

        internal DotNetAdapterWithComTypeName(ComTypeInfo comTypeInfo)
        {
            _comTypeInfo = comTypeInfo;
        }

        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName.Equals("System.__ComObject"))
                {
                    yield return ComAdapter.GetComTypeName(_comTypeInfo.Clsid);
                }

                yield return type.FullName;
            }
        }

        protected override ConsolidatedString GetInternedTypeNameHierarchy(object obj)
        {
            return new ConsolidatedString(GetTypeNameHierarchy(obj), interned: true);
        }
    }
#endif

    /// <summary>
    /// Adapter used for GetMember and GetMembers only.
    /// All other methods will not be called.
    /// </summary>
    internal abstract class MemberRedirectionAdapter : Adapter
    {
        #region virtual

        #region property specific

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
        /// Returns the value from a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Sets the value of a property coming from a previous call to GetMember.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to GetMember.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns true if the property is gettable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is gettable.</returns>
        protected override bool PropertyIsGettable(PSProperty property)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property's value.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous GetMember.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the member.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Returns the string representation of the property in the object.
        /// </summary>
        /// <param name="property">Property obtained in a previous GetMember.</param>
        /// <returns>The string representation of the property in the object.</returns>
        protected override string PropertyToString(PSProperty property)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for properties");
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion property specific

        #region method specific

        /// <summary>
        /// Called after a non null return from GetMember to try to call
        /// the method with the arguments.
        /// </summary>
        /// <param name="method">The non empty return from GetMethods.</param>
        /// <param name="arguments">The arguments to use.</param>
        /// <returns>The return value for the method.</returns>
        protected override object MethodInvoke(PSMethod method, object[] arguments)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for methods");
            throw PSTraceSource.NewNotSupportedException();
        }

        /// <summary>
        /// Called after a non null return from GetMember to return the overloads.
        /// </summary>
        /// <param name="method">The return of GetMember.</param>
        /// <returns></returns>
        protected override Collection<string> MethodDefinitions(PSMethod method)
        {
            Diagnostics.Assert(false, "redirection adapter is not called for methods");
            throw PSTraceSource.NewNotSupportedException();
        }

        #endregion method specific

        #endregion virtual
    }
    /// <summary>
    /// Adapter for properties in the inside PSObject if it has a null BaseObject.
    /// </summary>
    internal class PSObjectAdapter : MemberRedirectionAdapter
    {
        #region virtual

        /// <summary>
        /// Returns the TypeNameHierarchy out of an object.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            return ((PSObject)obj).InternalTypeNames;
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
            return ((PSObject)obj).Members[memberName] as T;
        }

        /// <summary>
        /// Returns the first PSMemberInfo whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            return ((PSObject)obj).GetFirstPropertyOrDefault(predicate) as T;
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
            var returnValue = new PSMemberInfoInternalCollection<T>();
            PSObject mshObj = (PSObject)obj;
            foreach (PSMemberInfo member in mshObj.Members)
            {
                T memberAsT = member as T;
                if (memberAsT != null)
                {
                    returnValue.Add(memberAsT);
                }
            }

            return returnValue;
        }

        #endregion virtual
    }
    /// <summary>
    /// Adapter for properties inside a member set.
    /// </summary>
    internal class PSMemberSetAdapter : MemberRedirectionAdapter
    {
        #region virtual

        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            // Make sure PSMemberSet adapter shows PSMemberSet as the typename.
            // This is because PSInternalMemberSet internal class derives from
            // PSMemberSet to support delay loading PSBase, PSObject etc. We
            // should not show internal type members to the users. Also this
            // might break type files shipped in v1.
            yield return typeof(PSMemberSet).FullName;
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
            return ((PSMemberSet)obj).Members[memberName] as T;
        }

        /// <summary>
        /// Returns the first PSMemberInfo whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            foreach (var member in ((PSMemberSet)obj).Members)
            {
                if (predicate(member.Name))
                {
                    return member as T;
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
            var returnValue = new PSMemberInfoInternalCollection<T>();
            foreach (PSMemberInfo member in ((PSMemberSet)obj).Members)
            {
                T memberAsT = member as T;
                if (memberAsT != null)
                {
                    returnValue.Add(memberAsT);
                }
            }

            return returnValue;
        }

        #endregion virtual
    }
    /// <summary>
    /// Base class for all adapters that adapt only properties and retain
    /// .NET methods.
    /// </summary>
    internal abstract class PropertyOnlyAdapter : DotNetAdapter
    {
        /// <summary>
        /// For a PropertyOnlyAdapter, the property may come from various sources,
        /// but methods, including parameterized properties, still come from DotNetAdapter.
        /// So, the binder can optimize on method calls for objects that map to a
        /// custom PropertyOnlyAdapter.
        /// </summary>
        internal override bool CanSiteBinderOptimize(MemberTypes typeToOperateOn)
        {
            switch (typeToOperateOn)
            {
                case MemberTypes.Property:
                    return false;
                case MemberTypes.Method:
                    return true;
                default:
                    throw new InvalidOperationException("Should be unreachable. Update code if other member types need to be handled here.");
            }
        }

        protected override ConsolidatedString GetInternedTypeNameHierarchy(object obj)
        {
            return new ConsolidatedString(GetTypeNameHierarchy(obj), interned: true);
        }

        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSProperty from.</param>
        /// <param name="propertyName">Name of the property to be retrieved.</param>
        /// <returns>The PSProperty corresponding to propertyName from obj.</returns>
        protected abstract PSProperty DoGetProperty(object obj, string propertyName);

        /// <summary>
        /// Returns the first PSProperty whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSProperty from.</param>
        /// <param name="predicate">The predicate to find the matching member.</param>
        /// <returns>The first PSProperty whose name matches the <paramref name="predicate"/>.</returns>
        protected abstract PSProperty DoGetFirstPropertyOrDefault(object obj, MemberNamePredicate predicate);

        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        /// <param name="obj">Object to get all the property information from.</param>
        /// <param name="members">Collection where the properties will be added.</param>
        protected abstract void DoAddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members) where T : PSMemberInfo;

        /// <summary>
        /// Returns null if memberName is not a member in the adapter or
        /// the corresponding PSMemberInfo.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="memberName">Name of the member to be retrieved.</param>
        /// <returns>The PSMemberInfo corresponding to memberName from obj.</returns>
        protected override T GetMember<T>(object obj, string memberName)
        {
            PSProperty property = DoGetProperty(obj, memberName);

            if (typeof(T).IsAssignableFrom(typeof(PSProperty)) && property != null)
            {
                return property as T;
            }

            if (typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                T returnValue = base.GetDotNetMethod<T>(obj, memberName);
                // We only return a method if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (returnValue != null && property == null)
                {
                    return returnValue;
                }
            }

            if (IsTypeParameterizedProperty(typeof(T)))
            {
                PSParameterizedProperty parameterizedProperty = base.GetDotNetProperty<PSParameterizedProperty>(obj, memberName);
                // We only return a parameterized property if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (parameterizedProperty != null && property == null)
                {
                    return parameterizedProperty as T;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first PSMemberInfo whose name matches the specified <see cref="MemberNamePredicate"/>.
        /// Otherwise, return null.
        /// </summary>
        /// <typeparam name="T">A subtype of <see cref="PSMemberInfo"/>.</typeparam>
        /// <param name="obj">Object to retrieve the PSMemberInfo from.</param>
        /// <param name="predicate">A name matching predicate.</param>
        /// <returns>The PSMemberInfo corresponding to the predicate match.</returns>
        protected override T GetFirstMemberOrDefault<T>(object obj, MemberNamePredicate predicate)
        {
            PSProperty property = DoGetFirstPropertyOrDefault(obj, predicate);

            if (typeof(T).IsAssignableFrom(typeof(PSProperty)))
            {
                return property as T;
            }

            if (typeof(T).IsAssignableFrom(typeof(PSMethod)))
            {
                T returnValue = base.GetFirstDotNetMethodOrDefault<T>(obj, predicate);

                // We only return a method if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (returnValue != null && property == null)
                {
                    return returnValue;
                }
            }

            if (IsTypeParameterizedProperty(typeof(T)))
            {
                var parameterizedProperty = base.GetFirstDotNetPropertyOrDefault<PSParameterizedProperty>(obj, predicate);

                // We only return a parameterized property if there is no property by the same name
                // to match the behavior we have in GetMembers
                if (parameterizedProperty != null && property == null)
                {
                    return parameterizedProperty as T;
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
            var returnValue = new PSMemberInfoInternalCollection<T>();
            if (typeof(T).IsAssignableFrom(typeof(PSProperty)))
            {
                DoAddAllProperties<T>(obj, returnValue);
            }

            base.AddAllMethods(obj, returnValue, true);
            if (IsTypeParameterizedProperty(typeof(T)))
            {
                var parameterizedProperties = new PSMemberInfoInternalCollection<PSParameterizedProperty>();
                base.AddAllProperties(obj, parameterizedProperties, true);
                foreach (PSParameterizedProperty parameterizedProperty in parameterizedProperties)
                {
                    try
                    {
                        returnValue.Add(parameterizedProperty as T);
                    }
                    catch (ExtendedTypeSystemException)
                    {
                        // ignore duplicates: the adapted properties will take precedence
                    }
                }
            }

            return returnValue;
        }
    }

    /// <summary>
    /// Deals with XmlNode objects.
    /// </summary>
    internal class XmlNodeAdapter : PropertyOnlyAdapter
    {
        #region virtual
        /// <summary>
        /// Returns the TypeNameHierarchy out of an object.
        /// </summary>
        /// <param name="obj">Object to get the TypeNameHierarchy from.</param>
        protected override IEnumerable<string> GetTypeNameHierarchy(object obj)
        {
            XmlNode node = (XmlNode)obj;
            string nodeNamespace = node.NamespaceURI;
            IEnumerable<string> baseTypeNames = GetDotNetTypeNameHierarchy(obj);
            if (string.IsNullOrEmpty(nodeNamespace))
            {
                foreach (string baseType in baseTypeNames)
                {
                    yield return baseType;
                }
            }
            else
            {
                StringBuilder firstType = null;
                foreach (string baseType in baseTypeNames)
                {
                    if (firstType == null)
                    {
                        firstType = new StringBuilder(baseType);
                        firstType.Append('#');
                        firstType.Append(node.NamespaceURI);
                        firstType.Append('#');
                        firstType.Append(node.LocalName);
                        yield return firstType.ToString();
                    }

                    yield return baseType;
                }
            }
        }

        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        /// <param name="obj">Object to get all the property information from.</param>
        /// <param name="members">Collection where the members will be added.</param>
        protected override void DoAddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members)
        {
            XmlNode node = (XmlNode)obj;

            Dictionary<string, List<XmlNode>> nodeArrays = new Dictionary<string, List<XmlNode>>(StringComparer.OrdinalIgnoreCase);

            if (node.Attributes != null)
            {
                foreach (XmlNode attribute in node.Attributes)
                {
                    List<XmlNode> nodeList;
                    if (!nodeArrays.TryGetValue(attribute.LocalName, out nodeList))
                    {
                        nodeList = new List<XmlNode>();
                        nodeArrays[attribute.LocalName] = nodeList;
                    }

                    nodeList.Add(attribute);
                }
            }

            if (node.ChildNodes != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    // Win8: 437544 ignore whitespace
                    if (childNode is XmlWhitespace)
                    {
                        continue;
                    }

                    List<XmlNode> nodeList;
                    if (!nodeArrays.TryGetValue(childNode.LocalName, out nodeList))
                    {
                        nodeList = new List<XmlNode>();
                        nodeArrays[childNode.LocalName] = nodeList;
                    }

                    nodeList.Add(childNode);
                }
            }

            foreach (KeyValuePair<string, List<XmlNode>> nodeArrayEntry in nodeArrays)
            {
                members.Add(new PSProperty(nodeArrayEntry.Key, this, obj, nodeArrayEntry.Value.ToArray()) as T);
            }
        }
        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSProperty from.</param>
        /// <param name="propertyName">Name of the property to be retrieved.</param>
        /// <returns>The PSProperty corresponding to propertyName from obj.</returns>
        protected override PSProperty DoGetProperty(object obj, string propertyName)
        {
            XmlNode[] nodes = FindNodes(obj, propertyName, StringComparison.OrdinalIgnoreCase);
            if (nodes.Length == 0)
            {
                return null;
            }

            return new PSProperty(nodes[0].LocalName, this, obj, nodes);
        }

        protected override PSProperty DoGetFirstPropertyOrDefault(object obj, MemberNamePredicate predicate)
        {
            XmlNode node = FindNode(obj, predicate);
            return node == null ? null : new PSProperty(node.LocalName, this, obj, node);
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            XmlNode[] nodes = (XmlNode[])property.adapterData;
            Diagnostics.Assert(nodes.Length != 0, "DoGetProperty would not return an empty array, it would return null instead");
            if (nodes.Length != 1)
            {
                return false;
            }

            XmlNode node = nodes[0];
            if (node is XmlText)
            {
                return true;
            }

            if (node is XmlAttribute)
            {
                return true;
            }

            XmlAttributeCollection nodeAttributes = node.Attributes;
            if ((nodeAttributes != null) && (nodeAttributes.Count != 0))
            {
                return false;
            }

            XmlNodeList nodeChildren = node.ChildNodes;
            if ((nodeChildren == null) || (nodeChildren.Count == 0))
            {
                return true;
            }

            if ((nodeChildren.Count == 1) && (nodeChildren[0].NodeType == XmlNodeType.Text))
            {
                return true;
            }

            return false;
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

        private static object GetNodeObject(XmlNode node)
        {
            XmlText text = node as XmlText;
            if (text != null)
            {
                return text.InnerText;
            }

            XmlAttributeCollection nodeAttributes = node.Attributes;

            // A node with attributes should not be simplified
            if ((nodeAttributes != null) && (nodeAttributes.Count != 0))
            {
                return node;
            }

            // If node does not have any children, return the innertext of the node
            if (!node.HasChildNodes)
            {
                return node.InnerText;
            }

            XmlNodeList nodeChildren = node.ChildNodes;
            // nodeChildren will not be null as we already verified that the node has children.
            if ((nodeChildren.Count == 1) && (nodeChildren[0].NodeType == XmlNodeType.Text))
            {
                return node.InnerText;
            }

            XmlAttribute attribute = node as XmlAttribute;
            if (attribute != null)
            {
                return attribute.Value;
            }

            return node;
        }

        /// <summary>
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            XmlNode[] nodes = (XmlNode[])property.adapterData;

            if (nodes.Length == 1)
            {
                return GetNodeObject(nodes[0]);
            }

            object[] returnValue = new object[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                returnValue[i] = GetNodeObject(nodes[i]);
            }

            return returnValue;
        }
        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            // XML is always a string so implicitly convert to string
            string valueString = LanguagePrimitives.ConvertTo<string>(setValue);
            XmlNode[] nodes = (XmlNode[])property.adapterData;
            Diagnostics.Assert(nodes.Length != 0, "DoGetProperty would not return an empty array, it would return null instead");
            if (nodes.Length > 1)
            {
                throw new SetValueException("XmlNodeSetRestrictionsMoreThanOneNode",
                    null,
                    ExtendedTypeSystem.XmlNodeSetShouldBeAString,
                    property.Name);
            }

            XmlNode node = nodes[0];
            XmlText text = node as XmlText;
            if (text != null)
            {
                text.InnerText = valueString;
                return;
            }

            XmlAttributeCollection nodeAttributes = node.Attributes;
            // A node with attributes cannot be set
            if ((nodeAttributes != null) && (nodeAttributes.Count != 0))
            {
                throw new SetValueException("XmlNodeSetRestrictionsNodeWithAttributes",
                    null,
                    ExtendedTypeSystem.XmlNodeSetShouldBeAString,
                    property.Name);
            }

            XmlNodeList nodeChildren = node.ChildNodes;
            if (nodeChildren == null || nodeChildren.Count == 0)
            {
                node.InnerText = valueString;
                return;
            }

            if ((nodeChildren.Count == 1) && (nodeChildren[0].NodeType == XmlNodeType.Text))
            {
                node.InnerText = valueString;
                return;
            }

            XmlAttribute attribute = node as XmlAttribute;
            if (attribute != null)
            {
                attribute.Value = valueString;
                return;
            }

            throw new SetValueException("XmlNodeSetRestrictionsUnknownNodeType",
                null,
                ExtendedTypeSystem.XmlNodeSetShouldBeAString,
                property.Name);
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous DoGetProperty.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the property.</returns>
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
        #endregion virtual

        /// <summary>
        /// Auxiliary in GetProperty to perform case sensitive and case insensitive searches
        /// in the child nodes.
        /// </summary>
        /// <param name="obj">XmlNode to extract property from.</param>
        /// <param name="propertyName">Property to look for.</param>
        /// <param name="comparisonType">Type pf comparison to perform.</param>
        /// <returns>The corresponding XmlNode or null if not present.</returns>
        private static XmlNode[] FindNodes(object obj, string propertyName, StringComparison comparisonType)
        {
            List<XmlNode> retValue = new List<XmlNode>();
            XmlNode node = (XmlNode)obj;

            if (node.Attributes != null)
            {
                foreach (XmlNode attribute in node.Attributes)
                {
                    if (attribute.LocalName.Equals(propertyName, comparisonType))
                    {
                        retValue.Add(attribute);
                    }
                }
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode is XmlWhitespace)
                {
                    // Win8: 437544 ignore whitespace
                    continue;
                }

                if (childNode.LocalName.Equals(propertyName, comparisonType))
                {
                    retValue.Add(childNode);
                }
            }

            return retValue.ToArray();
        }

        private static XmlNode FindNode(object obj, MemberNamePredicate predicate)
        {
            var node = (XmlNode)obj;

            if (node.Attributes != null)
            {
                foreach (XmlNode attribute in node.Attributes)
                {
                    if (predicate(attribute.LocalName))
                    {
                        return attribute;
                    }
                }
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode is XmlWhitespace)
                {
                    // Win8: 437544 ignore whitespace
                    continue;
                }

                if (predicate(childNode.LocalName))
                {
                    return childNode;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Deals with DataRow objects.
    /// </summary>
    internal class DataRowAdapter : PropertyOnlyAdapter
    {
        #region virtual

        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        /// <param name="obj">Object to get all the property information from.</param>
        /// <param name="members">Collection where the members will be added.</param>
        protected override void DoAddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members)
        {
            DataRow dataRow = (DataRow)obj;
            if (dataRow.Table == null || dataRow.Table.Columns == null)
            {
                return;
            }

            foreach (DataColumn property in dataRow.Table.Columns)
            {
                members.Add(new PSProperty(property.ColumnName, this, obj, property.ColumnName) as T);
            }

            return;
        }
        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSProperty from.</param>
        /// <param name="propertyName">Name of the property to be retrieved.</param>
        /// <returns>The PSProperty corresponding to propertyName from obj.</returns>
        protected override PSProperty DoGetProperty(object obj, string propertyName)
        {
            DataRow dataRow = (DataRow)obj;

            if (!dataRow.Table.Columns.Contains(propertyName))
            {
                return null;
            }

            string columnName = dataRow.Table.Columns[propertyName].ColumnName;
            return new PSProperty(columnName, this, obj, columnName);
        }

        protected override PSProperty DoGetFirstPropertyOrDefault(object obj, MemberNamePredicate predicate)
        {
            DataRow dataRow = (DataRow)obj;

            foreach (DataColumn property in dataRow.Table.Columns)
            {
                if (predicate(property.ColumnName))
                {
                    return new PSProperty(property.ColumnName, this, obj, property.ColumnName);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous DoGetProperty.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the property.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            string columnName = (string)property.adapterData;
            DataRow dataRow = (DataRow)property.baseObject;
            var dataType = dataRow.Table.Columns[columnName].DataType;
            return forDisplay ? ToStringCodeMethods.Type(dataType) : dataType.FullName;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            string columnName = (string)property.adapterData;
            DataRow dataRow = (DataRow)property.baseObject;
            return !dataRow.Table.Columns[columnName].ReadOnly;
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
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            DataRow dataRow = (DataRow)property.baseObject;
            return dataRow[(string)property.adapterData];
        }
        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            DataRow dataRow = (DataRow)property.baseObject;
            dataRow[(string)property.adapterData] = setValue;
            return;
        }
        #endregion virtual
    }
    /// <summary>
    /// Deals with DataRowView objects.
    /// </summary>
    internal class DataRowViewAdapter : PropertyOnlyAdapter
    {
        #region virtual
        /// <summary>
        /// Retrieves all the properties available in the object.
        /// </summary>
        /// <param name="obj">Object to get all the property information from.</param>
        /// <param name="members">Collection where the members will be added.</param>
        protected override void DoAddAllProperties<T>(object obj, PSMemberInfoInternalCollection<T> members)
        {
            DataRowView dataRowView = (DataRowView)obj;
            if (dataRowView.Row == null || dataRowView.Row.Table == null || dataRowView.Row.Table.Columns == null)
            {
                return;
            }

            foreach (DataColumn property in dataRowView.Row.Table.Columns)
            {
                members.Add(new PSProperty(property.ColumnName, this, obj, property.ColumnName) as T);
            }

            return;
        }
        /// <summary>
        /// Returns null if propertyName is not a property in the adapter or
        /// the corresponding PSProperty with its adapterData set to information
        /// to be used when retrieving the property.
        /// </summary>
        /// <param name="obj">Object to retrieve the PSProperty from.</param>
        /// <param name="propertyName">Name of the property to be retrieved.</param>
        /// <returns>The PSProperty corresponding to propertyName from obj.</returns>
        protected override PSProperty DoGetProperty(object obj, string propertyName)
        {
            DataRowView dataRowView = (DataRowView)obj;

            if (!dataRowView.Row.Table.Columns.Contains(propertyName))
            {
                return null;
            }

            string columnName = dataRowView.Row.Table.Columns[propertyName].ColumnName;
            return new PSProperty(columnName, this, obj, columnName);
        }

        protected override PSProperty DoGetFirstPropertyOrDefault(object obj, MemberNamePredicate predicate)
        {
            DataRowView dataRowView = (DataRowView)obj;

            foreach (DataColumn column in dataRowView.Row.Table.Columns)
            {
                string columnName = column.ColumnName;
                if (predicate(columnName))
                {
                    return new PSProperty(columnName, this, obj, columnName);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the name of the type corresponding to the property.
        /// </summary>
        /// <param name="property">PSProperty obtained in a previous DoGetProperty.</param>
        /// <param name="forDisplay">True if the result is for display purposes only.</param>
        /// <returns>The name of the type corresponding to the property.</returns>
        protected override string PropertyType(PSProperty property, bool forDisplay)
        {
            string columnName = (string)property.adapterData;
            DataRowView dataRowView = (DataRowView)property.baseObject;
            var dataType = dataRowView.Row.Table.Columns[columnName].DataType;
            return forDisplay ? ToStringCodeMethods.Type(dataType) : dataType.FullName;
        }

        /// <summary>
        /// Returns true if the property is settable.
        /// </summary>
        /// <param name="property">Property to check.</param>
        /// <returns>True if the property is settable.</returns>
        protected override bool PropertyIsSettable(PSProperty property)
        {
            string columnName = (string)property.adapterData;
            DataRowView dataRowView = (DataRowView)property.baseObject;
            return !dataRowView.Row.Table.Columns[columnName].ReadOnly;
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
        /// Returns the value from a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <returns>The value of the property.</returns>
        protected override object PropertyGet(PSProperty property)
        {
            DataRowView dataRowView = (DataRowView)property.baseObject;
            return dataRowView[(string)property.adapterData];
        }
        /// <summary>
        /// Sets the value of a property coming from a previous call to DoGetProperty.
        /// </summary>
        /// <param name="property">PSProperty coming from a previous call to DoGetProperty.</param>
        /// <param name="setValue">Value to set the property with.</param>
        /// <param name="convertIfPossible">Instructs the adapter to convert before setting, if the adapter supports conversion.</param>
        protected override void PropertySet(PSProperty property, object setValue, bool convertIfPossible)
        {
            DataRowView dataRowView = (DataRowView)property.baseObject;
            dataRowView[(string)property.adapterData] = setValue;
            return;
        }
        #endregion virtual
    }

    internal class TypeInference
    {
        [TraceSource("ETS", "Extended Type System")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("ETS", "Extended Type System");

        internal static MethodInformation Infer(MethodInformation genericMethod, Type[] argumentTypes)
        {
            Dbg.Assert(genericMethod != null, "Caller should verify that genericMethod != null");
            Dbg.Assert(argumentTypes != null, "Caller should verify that arguments != null");

            // the cast is safe, because
            // 1) only ConstructorInfo and MethodInfo derive from MethodBase
            // 2) ConstructorInfo.IsGenericMethod is always false
            MethodInfo originalMethod = (MethodInfo)genericMethod.method;
            MethodInfo inferredMethod = TypeInference.Infer(originalMethod, argumentTypes, genericMethod.hasVarArgs);

            if (inferredMethod != null)
            {
                return new MethodInformation(inferredMethod, 0);
            }
            else
            {
                return null;
            }
        }

        private static MethodInfo Infer(MethodInfo genericMethod, Type[] typesOfMethodArguments, bool hasVarArgs)
        {
            Dbg.Assert(genericMethod != null, "Caller should verify that genericMethod != null");
            Dbg.Assert(typesOfMethodArguments != null, "Caller should verify that arguments != null");

            if (!genericMethod.ContainsGenericParameters)
            {
                return genericMethod;
            }

            Type[] typeParameters = genericMethod.GetGenericArguments();
            Type[] typesOfMethodParameters = genericMethod.GetParameters().Select(static p => p.ParameterType).ToArray();

            MethodInfo inferredMethod = Infer(genericMethod, typeParameters, typesOfMethodParameters, typesOfMethodArguments);

            // normal inference failed, perhaps instead of inferring for
            //   M<T1,T2,T3>(T1, T2, ..., params T3 [])
            // we can try to infer for this signature instead
            //   M<T1,T2,T3>)(T1, T2, ..., T3, T3, T3, T3)
            // where T3 is repeated appropriate number of times depending on the number of actual method arguments.
            if (inferredMethod == null &&
                hasVarArgs &&
                typesOfMethodArguments.Length >= (typesOfMethodParameters.Length - 1))
            {
                IEnumerable<Type> typeOfRegularParameters = typesOfMethodParameters.Take(typesOfMethodParameters.Length - 1);
                IEnumerable<Type> multipliedVarArgsElementType = Enumerable.Repeat(
                    typesOfMethodParameters[typesOfMethodParameters.Length - 1].GetElementType(),
                    typesOfMethodArguments.Length - typesOfMethodParameters.Length + 1);

                inferredMethod = Infer(
                    genericMethod,
                    typeParameters,
                    typeOfRegularParameters.Concat(multipliedVarArgsElementType),
                    typesOfMethodArguments);
            }

            return inferredMethod;
        }

        private static MethodInfo Infer(MethodInfo genericMethod, ICollection<Type> typeParameters, IEnumerable<Type> typesOfMethodParameters, IEnumerable<Type> typesOfMethodArguments)
        {
            Dbg.Assert(genericMethod != null, "Caller should verify that genericMethod != null");
            Dbg.Assert(typeParameters != null, "Caller should verify that typeParameters != null");
            Dbg.Assert(typesOfMethodParameters != null, "Caller should verify that typesOfMethodParameters != null");
            Dbg.Assert(typesOfMethodArguments != null, "Caller should verify that typesOfMethodArguments != null");

            using (s_tracer.TraceScope("Inferring type parameters for the following method: {0}", genericMethod))
            {
                if ((s_tracer.Options & PSTraceSourceOptions.WriteLine) == PSTraceSourceOptions.WriteLine)
                {
                    s_tracer.WriteLine(
                        "Types of method arguments: {0}",
                        string.Join(", ", typesOfMethodArguments.Select(static t => t.ToString()).ToArray()));
                }

                var typeInference = new TypeInference(typeParameters);
                if (!typeInference.UnifyMultipleTerms(typesOfMethodParameters, typesOfMethodArguments))
                {
                    return null;
                }

                IEnumerable<Type> inferredTypeParameters = typeParameters.Select(typeInference.GetInferredType);
                if (inferredTypeParameters.Any(static inferredType => inferredType == null))
                {
                    return null;
                }

                try
                {
                    MethodInfo instantiatedMethod = genericMethod.MakeGenericMethod(inferredTypeParameters.ToArray());
                    s_tracer.WriteLine("Inference successful: {0}", instantiatedMethod);
                    return instantiatedMethod;
                }
                catch (ArgumentException e)
                {
                    // Inference failure - most likely due to generic constraints being violated (i.e. where T: IEnumerable)
                    s_tracer.WriteLine("Inference failure: {0}", e.Message);
                    return null;
                }
            }
        }

        private readonly HashSet<Type>[] _typeParameterIndexToSetOfInferenceCandidates;

#if DEBUG
        private readonly HashSet<Type> _typeParametersOfTheMethod;
#endif

        internal TypeInference(ICollection<Type> typeParameters)
        {
#if DEBUG
            Dbg.Assert(typeParameters != null, "Caller should verify that typeParameters != null");
            Dbg.Assert(
                typeParameters.All(t => t.IsGenericParameter),
                "Caller should verify that typeParameters are really generic type parameters");
#endif
            _typeParameterIndexToSetOfInferenceCandidates = new HashSet<Type>[typeParameters.Count];
#if DEBUG
            List<int> listOfTypeParameterPositions = typeParameters.Select(static t => t.GenericParameterPosition).ToList();
            listOfTypeParameterPositions.Sort();
            Dbg.Assert(
                listOfTypeParameterPositions.Count == listOfTypeParameterPositions.Distinct().Count(),
                "No type parameters should occupy the same position");
            Dbg.Assert(
                listOfTypeParameterPositions.All(p => p >= 0),
                "Type parameter positions should be between 0 and #ofParams");
            Dbg.Assert(
                listOfTypeParameterPositions.All(p => p < _typeParameterIndexToSetOfInferenceCandidates.Length),
                "Type parameter positions should be between 0 and #ofParams");

            _typeParametersOfTheMethod = new HashSet<Type>();
            foreach (Type t in typeParameters)
            {
                _typeParametersOfTheMethod.Add(t);
            }
#endif
        }

        internal Type GetInferredType(Type typeParameter)
        {
#if DEBUG
            Dbg.Assert(typeParameter != null, "Caller should verify typeParameter != null");
            Dbg.Assert(
                _typeParametersOfTheMethod.Contains(typeParameter),
                "Caller should verify that typeParameter is actually a generic type parameter of the method");
#endif

            ICollection<Type> inferenceCandidates =
                _typeParameterIndexToSetOfInferenceCandidates[typeParameter.GenericParameterPosition];

            if ((inferenceCandidates != null) && (inferenceCandidates.Any(static t => t == typeof(LanguagePrimitives.Null))))
            {
                Type firstValueType = inferenceCandidates.FirstOrDefault(static t => t.IsValueType);
                if (firstValueType != null)
                {
                    s_tracer.WriteLine("Cannot reconcile null and {0} (a value type)", firstValueType);
                    inferenceCandidates = null;
                    _typeParameterIndexToSetOfInferenceCandidates[typeParameter.GenericParameterPosition] = null;
                }
                else
                {
                    inferenceCandidates = inferenceCandidates.Where(static t => t != typeof(LanguagePrimitives.Null)).ToList();
                    if (inferenceCandidates.Count == 0)
                    {
                        inferenceCandidates = null;
                        _typeParameterIndexToSetOfInferenceCandidates[typeParameter.GenericParameterPosition] = null;
                    }
                }
            }

            if ((inferenceCandidates != null) && (inferenceCandidates.Count > 1))
            {
                // "base class" assignability-wise (to account for interfaces)
                Type commonBaseClass = inferenceCandidates.FirstOrDefault(
                    potentiallyCommonBaseClass =>
                        inferenceCandidates.All(
                            otherCandidate =>
                                otherCandidate == potentiallyCommonBaseClass ||
                                potentiallyCommonBaseClass.IsAssignableFrom(otherCandidate)));

                if (commonBaseClass != null)
                {
                    inferenceCandidates.Clear();
                    inferenceCandidates.Add(commonBaseClass);
                }
                else
                {
                    s_tracer.WriteLine("Multiple unreconcilable inferences for type parameter {0}", typeParameter);
                    inferenceCandidates = null;
                    _typeParameterIndexToSetOfInferenceCandidates[typeParameter.GenericParameterPosition] = null;
                }
            }

            if (inferenceCandidates == null)
            {
                s_tracer.WriteLine("Couldn't infer type parameter {0}", typeParameter);
                return null;
            }
            else
            {
                Dbg.Assert(inferenceCandidates.Count == 1, "inferenceCandidates should contain exactly 1 element at this point");
                return inferenceCandidates.Single();
            }
        }

        internal bool UnifyMultipleTerms(IEnumerable<Type> parameterTypes, IEnumerable<Type> argumentTypes)
        {
            List<Type> leftList = parameterTypes.ToList();
            List<Type> rightList = argumentTypes.ToList();

            if (leftList.Count != rightList.Count)
            {
                s_tracer.WriteLine("Mismatch in number of parameters and arguments");
                return false;
            }

            for (int i = 0; i < leftList.Count; i++)
            {
                if (!this.Unify(leftList[i], rightList[i]))
                {
                    s_tracer.WriteLine("Couldn't unify {0} with {1}", leftList[i], rightList[i]);
                    return false;
                }
            }

            return true;
        }

        private bool Unify(Type parameterType, Type argumentType)
        {
            if (!parameterType.ContainsGenericParameters)
            {
                return true;
            }

            if (parameterType.IsGenericParameter)
            {
#if DEBUG
                Dbg.Assert(
                    _typeParametersOfTheMethod.Contains(parameterType),
                    "Only uninstantiated generic type parameters encountered in real life, should be the ones coming from the method");
#endif

                HashSet<Type> inferenceCandidates = _typeParameterIndexToSetOfInferenceCandidates[parameterType.GenericParameterPosition];
                if (inferenceCandidates == null)
                {
                    inferenceCandidates = new HashSet<Type>();
                    _typeParameterIndexToSetOfInferenceCandidates[parameterType.GenericParameterPosition] = inferenceCandidates;
                }

                inferenceCandidates.Add(argumentType);
                s_tracer.WriteLine("Inferred {0} => {1}", parameterType, argumentType);
                return true;
            }

            if (parameterType.IsArray)
            {
                if (argumentType == typeof(LanguagePrimitives.Null))
                {
                    return true;
                }

                if (argumentType.IsArray && parameterType.GetArrayRank() == argumentType.GetArrayRank())
                {
                    return this.Unify(parameterType.GetElementType(), argumentType.GetElementType());
                }

                s_tracer.WriteLine("Couldn't unify array {0} with {1}", parameterType, argumentType);
                return false;
            }

            if (parameterType.IsByRef)
            {
                if (argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(PSReference<>))
                {
                    Type referencedType = argumentType.GetGenericArguments()[0];
                    if (referencedType == typeof(LanguagePrimitives.Null))
                    {
                        return true;
                    }
                    else
                    {
                        return this.Unify(
                            parameterType.GetElementType(),
                            referencedType);
                    }
                }
                else
                {
                    s_tracer.WriteLine("Couldn't unify reference type {0} with {1}", parameterType, argumentType);
                    return false;
                }
            }

            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (argumentType == typeof(LanguagePrimitives.Null))
                {
                    return true;
                }

                return this.Unify(parameterType.GetGenericArguments()[0], argumentType);
            }

            if (parameterType.IsGenericType)
            {
                if (argumentType == typeof(LanguagePrimitives.Null))
                {
                    return true;
                }

                return this.UnifyConstructedType(parameterType, argumentType);
            }

            Dbg.Assert(false, "Unrecognized kind of type");
            s_tracer.WriteLine("Unrecognized kind of type: {0}", parameterType);
            return false;
        }

        private bool UnifyConstructedType(Type parameterType, Type argumentType)
        {
            Dbg.Assert(parameterType.IsGenericType, "Caller should verify parameterType.IsGenericType before calling this method");

            if (IsEqualGenericTypeDefinition(parameterType, argumentType))
            {
                IEnumerable<Type> typeParametersOfParameterType = parameterType.GetGenericArguments();
                IEnumerable<Type> typeArgumentsOfArgumentType = argumentType.GetGenericArguments();
                return this.UnifyMultipleTerms(typeParametersOfParameterType, typeArgumentsOfArgumentType);
            }

            Type[] interfaces = argumentType.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (IsEqualGenericTypeDefinition(parameterType, interfaces[i]))
                {
                    return UnifyConstructedType(parameterType, interfaces[i]);
                }
            }

            Type baseType = argumentType.BaseType;
            while (baseType != null)
            {
                if (IsEqualGenericTypeDefinition(parameterType, baseType))
                {
                    return UnifyConstructedType(parameterType, baseType);
                }

                baseType = baseType.BaseType;
            }

            s_tracer.WriteLine("Attempt to unify different constructed types: {0} and {1}", parameterType, argumentType);
            return false;
        }

        private static bool IsEqualGenericTypeDefinition(Type parameterType, Type argumentType)
        {
            Dbg.Assert(parameterType.IsGenericType, "Caller should verify parameterType.IsGenericType before calling this method");

            if (!argumentType.IsGenericType)
            {
                return false;
            }

            return parameterType.GetGenericTypeDefinition() == argumentType.GetGenericTypeDefinition();
        }
    }
}
