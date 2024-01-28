// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Interpreter;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;

using Microsoft.PowerShell;
using TypeTable = System.Management.Automation.Runspaces.TypeTable;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56503

namespace System.Management.Automation
{
    #region PSMemberInfo

    /// <summary>
    /// Enumerates all possible types of members.
    /// </summary>
    [TypeConverterAttribute(typeof(LanguagePrimitives.EnumMultipleTypeConverter))]
    [FlagsAttribute]
    public enum PSMemberTypes
    {
        /// <summary>
        /// An alias to another member.
        /// </summary>
        AliasProperty = 1,

        /// <summary>
        /// A property defined as a reference to a method.
        /// </summary>
        CodeProperty = 2,

        /// <summary>
        /// A property from the BaseObject.
        /// </summary>
        Property = 4,

        /// <summary>
        /// A property defined by a Name-Value pair.
        /// </summary>
        NoteProperty = 8,

        /// <summary>
        /// A property defined by script language.
        /// </summary>
        ScriptProperty = 16,

        /// <summary>
        /// A set of properties.
        /// </summary>
        PropertySet = 32,

        /// <summary>
        /// A method from the BaseObject.
        /// </summary>
        Method = 64,

        /// <summary>
        /// A method defined as a reference to another method.
        /// </summary>
        CodeMethod = 128,

        /// <summary>
        /// A method defined as a script.
        /// </summary>
        ScriptMethod = 256,

        /// <summary>
        /// A member that acts like a Property that takes parameters. This is not consider to be a property or a method.
        /// </summary>
        ParameterizedProperty = 512,

        /// <summary>
        /// A set of members.
        /// </summary>
        MemberSet = 1024,

        /// <summary>
        /// All events.
        /// </summary>
        Event = 2048,

        /// <summary>
        /// All dynamic members (where PowerShell cannot know the type of the member)
        /// </summary>
        Dynamic = 4096,

        /// <summary>
        /// Members that are inferred by type inference for PSObject and hashtable.
        /// </summary>
        InferredProperty = 8192,
        /// <summary>
        /// All property member types.
        /// </summary>
        Properties = AliasProperty | CodeProperty | Property | NoteProperty | ScriptProperty | InferredProperty,

        /// <summary>
        /// All method member types.
        /// </summary>
        Methods = CodeMethod | Method | ScriptMethod,

        /// <summary>
        /// All member types.
        /// </summary>
        All = Properties | Methods | Event | PropertySet | MemberSet | ParameterizedProperty | Dynamic
    }

    /// <summary>
    /// Enumerator for all possible views available on a PSObject.
    /// </summary>
    [TypeConverterAttribute(typeof(LanguagePrimitives.EnumMultipleTypeConverter))]
    [FlagsAttribute]
    public enum PSMemberViewTypes
    {
        /// <summary>
        /// Extended methods / properties.
        /// </summary>
        Extended = 1,

        /// <summary>
        /// Adapted methods / properties.
        /// </summary>
        Adapted = 2,

        /// <summary>
        /// Base methods / properties.
        /// </summary>
        Base = 4,

        /// <summary>
        /// All methods / properties.
        /// </summary>
        All = Extended | Adapted | Base
    }

    /// <summary>
    /// Match options.
    /// </summary>
    [FlagsAttribute]
    internal enum MshMemberMatchOptions
    {
        /// <summary>
        /// No options.
        /// </summary>
        None = 0,

        /// <summary>
        /// Hidden members should be displayed.
        /// </summary>
        IncludeHidden = 1,

        /// <summary>
        /// Only include members with <see cref="PSMemberInfo.ShouldSerialize"/> property set to <see langword="true"/>
        /// </summary>
        OnlySerializable = 2
    }

    /// <summary>
    /// Serves as the base class for all members of an PSObject.
    /// </summary>
    public abstract class PSMemberInfo
    {
        internal object instance;
        internal string name;

        internal bool ShouldSerialize { get; set; }

        internal virtual void ReplicateInstance(object particularInstance)
        {
            this.instance = particularInstance;
        }

        internal void SetValueNoConversion(object setValue)
        {
            if (this is not PSProperty thisAsProperty)
            {
                this.Value = setValue;
                return;
            }

            thisAsProperty.SetAdaptedValue(setValue, false);
        }

        /// <summary>
        /// Initializes a new instance of an PSMemberInfo derived class.
        /// </summary>
        protected PSMemberInfo()
        {
            ShouldSerialize = true;
            IsInstance = true;
        }

        internal void CloneBaseProperties(PSMemberInfo destiny)
        {
            destiny.name = name;
            destiny.IsHidden = IsHidden;
            destiny.IsReservedMember = IsReservedMember;
            destiny.IsInstance = IsInstance;
            destiny.instance = instance;
            destiny.ShouldSerialize = ShouldSerialize;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public abstract PSMemberTypes MemberType { get; }

        /// <summary>
        /// Gets the member name.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Allows a derived class to set the member name...
        /// </summary>
        /// <param name="name"></param>
        protected void SetMemberName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
        }

        /// <summary>
        /// True if this is one of the reserved members.
        /// </summary>
        internal bool IsReservedMember { get; set; }

        /// <summary>
        /// True if the member should be hidden when searching with PSMemberInfoInternalCollection's Match
        /// or enumerating a collection.
        /// This should not be settable as it would make the count of hidden properties in
        /// PSMemberInfoInternalCollection invalid.
        /// For now, we are carefully setting this.isHidden before adding
        /// the members toPSObjectMembersetCollection. In the future, we might need overload for all
        /// PSMemberInfo constructors to take isHidden.
        /// </summary>
        internal bool IsHidden { get; set; }

        /// <summary>
        /// True if this member has been added to the instance as opposed to
        /// coming from the adapter or from type data.
        /// </summary>
        public bool IsInstance { get; internal set; }

        /// <summary>
        /// Gets and Sets the value of this member.
        /// </summary>
        /// <exception cref="GetValueException">When getting the value of a property throws an exception.
        /// This exception is also thrown if the property is an <see cref="PSScriptProperty"/> and there
        /// is no Runspace to run the script.</exception>
        /// <exception cref="SetValueException">When setting the value of a property throws an exception.
        /// This exception is also thrown if the property is an <see cref="PSScriptProperty"/> and there
        /// is no Runspace to run the script.</exception>
        /// <exception cref="ExtendedTypeSystemException">When some problem other then getting/setting the value happened.</exception>
        public abstract object Value { get; set; }

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">When there was a problem getting the property.</exception>
        public abstract string TypeNameOfValue { get; }

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public abstract PSMemberInfo Copy();

        internal bool MatchesOptions(MshMemberMatchOptions options)
        {
            if (this.IsHidden && ((options & MshMemberMatchOptions.IncludeHidden) == 0))
            {
                return false;
            }

            if (!this.ShouldSerialize && ((options & MshMemberMatchOptions.OnlySerializable) != 0))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Serves as a base class for all members that behave like properties.
    /// </summary>
    public abstract class PSPropertyInfo : PSMemberInfo
    {
        /// <summary>
        /// Initializes a new instance of an PSPropertyInfo derived class.
        /// </summary>
        protected PSPropertyInfo()
        {
        }

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        public abstract bool IsSettable { get; }

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        public abstract bool IsGettable { get; }

        internal Exception NewSetValueException(Exception e, string errorId)
        {
            return new SetValueInvocationException(errorId,
                e,
                ExtendedTypeSystem.ExceptionWhenSetting,
                this.Name, e.Message);
        }

        internal Exception NewGetValueException(Exception e, string errorId)
        {
            return new GetValueInvocationException(errorId,
                e,
                ExtendedTypeSystem.ExceptionWhenGetting,
                this.Name, e.Message);
        }
    }

    /// <summary>
    /// Serves as an alias to another member.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSAliasProperty"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSAliasProperty : PSPropertyInfo
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(this.Name);
            returnValue.Append(" = ");
            if (ConversionType != null)
            {
                returnValue.Append('(');
                returnValue.Append(ConversionType);
                returnValue.Append(')');
            }

            returnValue.Append(ReferencedMemberName);
            return returnValue.ToString();
        }

        /// <summary>
        /// Initializes a new instance of PSAliasProperty setting the name of the alias
        /// and the name of the member this alias refers to.
        /// </summary>
        /// <param name="name">Name of the alias.</param>
        /// <param name="referencedMemberName">Name of the member this alias refers to.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSAliasProperty(string name, string referencedMemberName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (string.IsNullOrEmpty(referencedMemberName))
            {
                throw PSTraceSource.NewArgumentException(nameof(referencedMemberName));
            }

            ReferencedMemberName = referencedMemberName;
        }

        /// <summary>
        /// Initializes a new instance of PSAliasProperty setting the name of the alias,
        /// the name of the member this alias refers to and the type to convert the referenced
        /// member's value.
        /// </summary>
        /// <param name="name">Name of the alias.</param>
        /// <param name="referencedMemberName">Name of the member this alias refers to.</param>
        /// <param name="conversionType">The type to convert the referenced member's value.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSAliasProperty(string name, string referencedMemberName, Type conversionType)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (string.IsNullOrEmpty(referencedMemberName))
            {
                throw PSTraceSource.NewArgumentException(nameof(referencedMemberName));
            }

            ReferencedMemberName = referencedMemberName;
            // conversionType is optional and can be null
            ConversionType = conversionType;
        }

        /// <summary>
        /// Gets the name of the member this alias refers to.
        /// </summary>
        public string ReferencedMemberName { get; }

        /// <summary>
        /// Gets the member this alias refers to.
        /// </summary>
        internal PSMemberInfo ReferencedMember => this.LookupMember(ReferencedMemberName);

        /// <summary>
        /// Gets the type to convert the referenced member's value. It might be
        /// null when no conversion is done.
        /// </summary>
        public Type ConversionType { get; private set; }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSAliasProperty alias = new PSAliasProperty(name, ReferencedMemberName) { ConversionType = ConversionType };
            CloneBaseProperties(alias);
            return alias;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.AliasProperty;

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">
        /// When
        ///     the alias has not been added to an PSObject or
        ///     the alias has a cycle or
        ///     an aliased member is not present
        /// </exception>
        public override string TypeNameOfValue
        {
            get
            {
                if (ConversionType != null)
                {
                    return ConversionType.FullName;
                }

                return this.ReferencedMember.TypeNameOfValue;
            }
        }

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">
        /// When
        ///     the alias has not been added to an PSObject or
        ///     the alias has a cycle or
        ///     an aliased member is not present
        /// </exception>
        public override bool IsSettable
        {
            get
            {
                if (this.ReferencedMember is PSPropertyInfo memberProperty)
                {
                    return memberProperty.IsSettable;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When
        ///         the alias has not been added to an PSObject or
        ///         the alias has a cycle or
        ///         an aliased member is not present
        /// </exception>
        public override bool IsGettable
        {
            get
            {
                if (this.ReferencedMember is PSPropertyInfo memberProperty)
                {
                    return memberProperty.IsGettable;
                }

                return false;
            }
        }

        private PSMemberInfo LookupMember(string name)
        {
            LookupMember(name, new HashSet<string>(StringComparer.OrdinalIgnoreCase), out PSMemberInfo returnValue, out bool hasCycle);
            if (hasCycle)
            {
                throw new
                    ExtendedTypeSystemException(
                        "CycleInAliasLookup",
                        null,
                        ExtendedTypeSystem.CycleInAlias,
                        this.Name);
            }

            return returnValue;
        }

        private void LookupMember(string name, HashSet<string> visitedAliases, out PSMemberInfo returnedMember, out bool hasCycle)
        {
            returnedMember = null;
            if (this.instance == null)
            {
                throw new ExtendedTypeSystemException("AliasLookupMemberOutsidePSObject",
                    null,
                    ExtendedTypeSystem.AccessMemberOutsidePSObject,
                    name);
            }

            PSMemberInfo member = PSObject.AsPSObject(this.instance).Properties[name];
            if (member == null)
            {
                throw new ExtendedTypeSystemException(
                    "AliasLookupMemberNotPresent",
                    null,
                    ExtendedTypeSystem.MemberNotPresent,
                    name);
            }

            if (member is not PSAliasProperty aliasMember)
            {
                hasCycle = false;
                returnedMember = member;
                return;
            }

            if (visitedAliases.Contains(name))
            {
                hasCycle = true;
                return;
            }

            visitedAliases.Add(name);
            LookupMember(aliasMember.ReferencedMemberName, visitedAliases, out returnedMember, out hasCycle);
        }

        /// <summary>
        /// Gets and Sets the value of this member.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">
        /// When
        ///     the alias has not been added to an PSObject or
        ///     the alias has a cycle or
        ///     an aliased member is not present
        /// </exception>
        /// <exception cref="GetValueException">When getting the value of a property throws an exception.</exception>
        /// <exception cref="SetValueException">When setting the value of a property throws an exception.</exception>
        public override object Value
        {
            get
            {
                object returnValue = this.ReferencedMember.Value;
                if (ConversionType != null)
                {
                    returnValue = LanguagePrimitives.ConvertTo(returnValue, ConversionType, CultureInfo.InvariantCulture);
                }

                return returnValue;
            }

            set => this.ReferencedMember.Value = value;
        }

        #endregion virtual implementation
    }

    /// <summary>
    /// Serves as a property implemented with references to methods for getter and setter.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSCodeProperty"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSCodeProperty : PSPropertyInfo
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(this.TypeNameOfValue);
            returnValue.Append(' ');
            returnValue.Append(this.Name);
            returnValue.Append('{');
            if (this.IsGettable)
            {
                returnValue.Append("get=");
                returnValue.Append(GetterCodeReference.Name);
                returnValue.Append(';');
            }

            if (this.IsSettable)
            {
                returnValue.Append("set=");
                returnValue.Append(SetterCodeReference.Name);
                returnValue.Append(';');
            }

            returnValue.Append('}');
            return returnValue.ToString();
        }

        /// <summary>
        /// Called from TypeTableUpdate before SetSetterFromTypeTable is called.
        /// </summary>
        internal void SetGetterFromTypeTable(Type type, string methodName)
        {
            MethodInfo methodAsMember = null;

            try
            {
                methodAsMember = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                // Ignore the AmbiguousMatchException.
                // We will generate error below if we cannot find exactly one match method.
            }

            if (methodAsMember == null)
            {
                throw new ExtendedTypeSystemException(
                    "GetterFormatFromTypeTable",
                    null,
                    ExtendedTypeSystem.CodePropertyGetterFormat);
            }

            SetGetter(methodAsMember);
        }

        /// <summary>
        /// Called from TypeTableUpdate after SetGetterFromTypeTable is called.
        /// </summary>
        internal void SetSetterFromTypeTable(Type type, string methodName)
        {
            MethodInfo methodAsMember = null;

            try
            {
                methodAsMember = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                // Ignore the AmbiguousMatchException.
                // We will generate error below if we cannot find exactly one match method.
            }

            if (methodAsMember == null)
            {
                throw new ExtendedTypeSystemException(
                    "SetterFormatFromTypeTable",
                    null,
                    ExtendedTypeSystem.CodePropertySetterFormat);
            }

            SetSetter(methodAsMember, GetterCodeReference);
        }

        /// <summary>
        /// Used from TypeTable with the internal constructor.
        /// </summary>
        internal void SetGetter(MethodInfo methodForGet)
        {
            if (methodForGet == null)
            {
                GetterCodeReference = null;
                return;
            }

            if (!CheckGetterMethodInfo(methodForGet))
            {
                throw new ExtendedTypeSystemException(
                    "GetterFormat",
                    null,
                    ExtendedTypeSystem.CodePropertyGetterFormat);
            }

            GetterCodeReference = methodForGet;
        }

        internal static bool CheckGetterMethodInfo(MethodInfo methodForGet)
        {
            ParameterInfo[] parameters = methodForGet.GetParameters();
            return methodForGet.IsPublic
                   && methodForGet.IsStatic
                   && methodForGet.ReturnType != typeof(void)
                   && parameters.Length == 1
                   && parameters[0].ParameterType == typeof(PSObject);
        }

        /// <summary>
        /// Used from TypeTable with the internal constructor.
        /// </summary>
        private void SetSetter(MethodInfo methodForSet, MethodInfo methodForGet)
        {
            if (methodForSet == null)
            {
                if (methodForGet == null)
                {
                    throw new ExtendedTypeSystemException(
                        "SetterAndGetterNullFormat",
                        null,
                        ExtendedTypeSystem.CodePropertyGetterAndSetterNull);
                }

                SetterCodeReference = null;
                return;
            }

            if (!CheckSetterMethodInfo(methodForSet, methodForGet))
            {
                throw new ExtendedTypeSystemException(
                    "SetterFormat",
                    null,
                    ExtendedTypeSystem.CodePropertySetterFormat);
            }

            SetterCodeReference = methodForSet;
        }

        internal static bool CheckSetterMethodInfo(MethodInfo methodForSet, MethodInfo methodForGet)
        {
            ParameterInfo[] parameters = methodForSet.GetParameters();
            return methodForSet.IsPublic
                   && methodForSet.IsStatic
                   && methodForSet.ReturnType == typeof(void)
                   && parameters.Length == 2
                   && parameters[0].ParameterType == typeof(PSObject)
                   && (methodForGet == null || methodForGet.ReturnType == parameters[1].ParameterType);
        }

        /// <summary>
        /// Used from TypeTable to delay setting getter and setter.
        /// </summary>
        internal PSCodeProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the PSCodeProperty class as a read only property.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="getterCodeReference">This should be a public static non void method taking one PSObject parameter.</param>
        /// <exception cref="ArgumentException">If name is null or empty or getterCodeReference is null.</exception>
        /// <exception cref="ExtendedTypeSystemException">If getterCodeReference doesn't have the right format.</exception>
        public PSCodeProperty(string name, MethodInfo getterCodeReference)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (getterCodeReference == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(getterCodeReference));
            }

            SetGetter(getterCodeReference);
        }

        /// <summary>
        /// Initializes a new instance of the PSCodeProperty class. Setter or getter can be null, but both cannot be null.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="getterCodeReference">This should be a public static non void method taking one PSObject parameter.</param>
        /// <param name="setterCodeReference">This should be a public static void method taking 2 parameters, where the first is an PSObject.</param>
        /// <exception cref="ArgumentException">When methodForGet and methodForSet are null.</exception>
        /// <exception cref="ExtendedTypeSystemException">
        /// if:
        ///     - getterCodeReference doesn't have the right format,
        ///     - setterCodeReference doesn't have the right format,
        ///     - both getterCodeReference and setterCodeReference are null.
        /// </exception>
        public PSCodeProperty(string name, MethodInfo getterCodeReference, MethodInfo setterCodeReference)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (getterCodeReference == null && setterCodeReference == null)
            {
                throw PSTraceSource.NewArgumentNullException("getterCodeReference setterCodeReference");
            }

            SetGetter(getterCodeReference);
            SetSetter(setterCodeReference, getterCodeReference);
        }

        /// <summary>
        /// Gets the method used for the properties' getter. It might be null.
        /// </summary>
        public MethodInfo GetterCodeReference { get; private set; }

        /// <summary>
        /// Gets the method used for the properties' setter. It might be null.
        /// </summary>
        public MethodInfo SetterCodeReference { get; private set; }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSCodeProperty property = new PSCodeProperty(name, GetterCodeReference, SetterCodeReference);
            CloneBaseProperties(property);
            return property;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.CodeProperty;

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        public override bool IsSettable => this.SetterCodeReference != null;

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        public override bool IsGettable => GetterCodeReference != null;

        /// <summary>
        /// Gets and Sets the value of this member.
        /// </summary>
        /// <exception cref="GetValueException">When getting and there is no getter or when the getter throws an exception.</exception>
        /// <exception cref="SetValueException">When setting and there is no setter or when the setter throws an exception.</exception>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "<Pending>")]
        public override object Value
        {
            get
            {
                if (GetterCodeReference == null)
                {
                    throw new GetValueException(
                        "GetWithoutGetterFromCodePropertyValue",
                        null,
                        ExtendedTypeSystem.GetWithoutGetterException,
                        this.Name);
                }

                try
                {
                    return GetterCodeReference.Invoke(null, new object[] { this.instance });
                }
                catch (TargetInvocationException ex)
                {
                    Exception inner = ex.InnerException ?? ex;
                    throw new GetValueInvocationException(
                        "CatchFromCodePropertyGetTI",
                        inner,
                        ExtendedTypeSystem.ExceptionWhenGetting,
                        this.name,
                        inner.Message);
                }
                catch (Exception e)
                {
                    if (e is GetValueException)
                    {
                        throw;
                    }

                    throw new GetValueInvocationException(
                        "CatchFromCodePropertyGet",
                        e,
                        ExtendedTypeSystem.ExceptionWhenGetting,
                        this.name,
                        e.Message);
                }
            }

            set
            {
                if (SetterCodeReference == null)
                {
                    throw new SetValueException(
                        "SetWithoutSetterFromCodeProperty",
                        null,
                        ExtendedTypeSystem.SetWithoutSetterException,
                        this.Name);
                }

                try
                {
                    SetterCodeReference.Invoke(null, new object[] { this.instance, value });
                }
                catch (TargetInvocationException ex)
                {
                    Exception inner = ex.InnerException ?? ex;
                    throw new SetValueInvocationException(
                        "CatchFromCodePropertySetTI",
                        inner,
                        ExtendedTypeSystem.ExceptionWhenSetting,
                        this.name,
                        inner.Message);
                }
                catch (Exception e)
                {
                    if (e is SetValueException)
                    {
                        throw;
                    }

                    throw new SetValueInvocationException(
                        "CatchFromCodePropertySet",
                        e,
                        ExtendedTypeSystem.ExceptionWhenSetting,
                        this.name,
                        e.Message);
                }
            }
        }

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        /// <exception cref="GetValueException">If there is no property getter.</exception>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "<Pending>")]
        public override string TypeNameOfValue
        {
            get
            {
                if (GetterCodeReference == null)
                {
                    throw new GetValueException(
                        "GetWithoutGetterFromCodePropertyTypeOfValue",
                        null,
                        ExtendedTypeSystem.GetWithoutGetterException,
                        this.Name);
                }

                return GetterCodeReference.ReturnType.FullName;
            }
        }

        #endregion virtual implementation
    }

    /// <summary>
    /// Type used to capture the properties inferred from Hashtable and PSObject.
    /// </summary>
    internal sealed class PSInferredProperty : PSPropertyInfo
    {
        public PSInferredProperty(string name, PSTypeName typeName)
        {
            this.name = name;
            TypeName = typeName;
        }

        internal PSTypeName TypeName { get; }

        public override PSMemberTypes MemberType => PSMemberTypes.InferredProperty;

        public override object Value { get; set; }

        public override string TypeNameOfValue => TypeName.Name;

        public override PSMemberInfo Copy() => new PSInferredProperty(Name, TypeName);

        public override bool IsSettable => false;

        public override bool IsGettable => false;

        public override string ToString() => $"{ToStringCodeMethods.Type(TypeName.Type)} {Name}";
    }

    /// <summary>
    /// Used to access the adapted or base properties from the BaseObject.
    /// </summary>
    public class PSProperty : PSPropertyInfo
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            if (this.isDeserialized)
            {
                StringBuilder returnValue = new StringBuilder();
                returnValue.Append(this.TypeNameOfValue);
                returnValue.Append(" {get;set;}");
                return returnValue.ToString();
            }

            Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");
            return adapter.BasePropertyToString(this);
        }

        /// <summary>
        /// Used by the adapters to keep intermediate data used between DoGetProperty and
        /// DoGetValue or DoSetValue.
        /// </summary>
        internal string typeOfValue;

        internal object serializedValue;
        internal bool isDeserialized;

        /// <summary>
        /// This will be either instance.adapter or instance.clrAdapter.
        /// </summary>
        internal Adapter adapter;

        internal object adapterData;
        internal object baseObject;

        /// <summary>
        /// Constructs a property from a serialized value.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="serializedValue">Value of the property.</param>
        internal PSProperty(string name, object serializedValue)
        {
            this.isDeserialized = true;
            this.serializedValue = serializedValue;
            this.name = name;
        }

        /// <summary>
        /// Constructs this property.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="adapter">Adapter used in DoGetProperty.</param>
        /// <param name="baseObject">Object passed to DoGetProperty.</param>
        /// <param name="adapterData">Adapter specific data.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSProperty(string name, Adapter adapter, object baseObject, object adapterData)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            this.adapter = adapter;
            this.adapterData = adapterData;
            this.baseObject = baseObject;
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSProperty property = new PSProperty(this.name, this.adapter, this.baseObject, this.adapterData);
            CloneBaseProperties(property);
            property.typeOfValue = this.typeOfValue;
            property.serializedValue = this.serializedValue;
            property.isDeserialized = this.isDeserialized;
            return property;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.Property;

        private object GetAdaptedValue()
        {
            if (this.isDeserialized)
            {
                return serializedValue;
            }

            Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");

            object o = adapter.BasePropertyGet(this);
            return o;
        }

        internal void SetAdaptedValue(object setValue, bool shouldConvert)
        {
            if (this.isDeserialized)
            {
                serializedValue = setValue;
                return;
            }

            Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");
            adapter.BasePropertySet(this, setValue, shouldConvert);
        }

        /// <summary>
        /// Gets or sets the value of this property.
        /// </summary>
        /// <exception cref="GetValueException">When getting the value of a property throws an exception.</exception>
        /// <exception cref="SetValueException">When setting the value of a property throws an exception.</exception>
        public override object Value
        {
            get => GetAdaptedValue();
            set => SetAdaptedValue(value, true);
        }

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        public override bool IsSettable
        {
            get
            {
                if (this.isDeserialized)
                {
                    return true;
                }

                Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");
                return adapter.BasePropertyIsSettable(this);
            }
        }

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        public override bool IsGettable
        {
            get
            {
                if (this.isDeserialized)
                {
                    return true;
                }

                Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");
                return adapter.BasePropertyIsGettable(this);
            }
        }

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        public override string TypeNameOfValue
        {
            get
            {
                if (this.isDeserialized)
                {
                    if (serializedValue == null)
                    {
                        return string.Empty;
                    }

                    if (serializedValue is PSObject serializedValueAsPSObject)
                    {
                        var typeNames = serializedValueAsPSObject.InternalTypeNames;
                        if ((typeNames != null) && (typeNames.Count >= 1))
                        {
                            // type name at 0-th index is the most specific type (i.e. deserialized.system.io.directoryinfo)
                            // type names at other indices are less specific (i.e. deserialized.system.object)
                            return typeNames[0];
                        }
                    }

                    return serializedValue.GetType().FullName;
                }

                Diagnostics.Assert((this.baseObject != null) && (this.adapter != null), "if it is deserialized, it should have all these properties set");
                return adapter.BasePropertyType(this);
            }
        }

        #endregion virtual implementation
    }

    /// <summary>
    /// A property created by a user-defined PSPropertyAdapter.
    /// </summary>
    public class PSAdaptedProperty : PSProperty
    {
        /// <summary>
        /// Creates a property for the given base object.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="tag">An adapter can use this object to keep any arbitrary data it needs.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSAdaptedProperty(string name, object tag)
            : base(name, null, null, tag)
        {
            //
            // Note that the constructor sets the adapter and base object to null; the ThirdPartyAdapter managing this property must set these values
            //
        }

        internal PSAdaptedProperty(string name, Adapter adapter, object baseObject, object adapterData)
            : base(name, adapter, baseObject, adapterData)
        {
        }

        /// <summary>
        /// Copy an adapted property.
        /// </summary>
        public override PSMemberInfo Copy()
        {
            PSAdaptedProperty property = new PSAdaptedProperty(this.name, this.adapter, this.baseObject, this.adapterData);
            CloneBaseProperties(property);
            property.typeOfValue = this.typeOfValue;
            property.serializedValue = this.serializedValue;
            property.isDeserialized = this.isDeserialized;
            return property;
        }

        /// <summary>
        /// Gets the object the property belongs to.
        /// </summary>
        public object BaseObject => this.baseObject;

        /// <summary>
        /// Gets the data attached to this property.
        /// </summary>
        public object Tag => this.adapterData;
    }

    /// <summary>
    /// Serves as a property that is a simple name-value pair.
    /// </summary>
    public class PSNoteProperty : PSPropertyInfo
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();

            returnValue.Append(GetDisplayTypeNameOfValue(this.Value));
            returnValue.Append(' ');
            returnValue.Append(this.Name);
            returnValue.Append('=');
            returnValue.Append(this.noteValue == null ? "null" : this.noteValue.ToString());
            return returnValue.ToString();
        }

        internal object noteValue;

        /// <summary>
        /// Initializes a new instance of the PSNoteProperty class.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        /// <exception cref="ArgumentException">For an empty or null name.</exception>
        public PSNoteProperty(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            // value can be null
            this.noteValue = value;
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSNoteProperty property = new PSNoteProperty(this.name, this.noteValue);
            CloneBaseProperties(property);
            return property;
        }

        /// <summary>
        /// Gets PSMemberTypes.NoteProperty.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;

        /// <summary>
        /// Gets true since the value of an PSNoteProperty can always be set.
        /// </summary>
        public override bool IsSettable => this.IsInstance;

        /// <summary>
        /// Gets true since the value of an PSNoteProperty can always be obtained.
        /// </summary>
        public override bool IsGettable => true;

        /// <summary>
        /// Gets or sets the value of this property.
        /// </summary>
        public override object Value
        {
            get => this.noteValue;
            set
            {
                if (!this.IsInstance)
                {
                    throw new SetValueException("ChangeValueOfStaticNote",
                        null,
                        ExtendedTypeSystem.ChangeStaticMember,
                        this.Name);
                }

                this.noteValue = value;
            }
        }

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        public override string TypeNameOfValue
        {
            get
            {
                object val = this.Value;

                if (val == null)
                {
                    return typeof(object).FullName;
                }

                if (val is PSObject valAsPSObject)
                {
                    var typeNames = valAsPSObject.InternalTypeNames;
                    if ((typeNames != null) && (typeNames.Count >= 1))
                    {
                        // type name at 0-th index is the most specific type (i.e. system.string)
                        // type names at other indices are less specific (i.e. system.object)
                        return typeNames[0];
                    }
                }

                return val.GetType().FullName;
            }
        }

        #endregion virtual implementation

        internal static string GetDisplayTypeNameOfValue(object val)
        {
            string displayTypeName = null;

            if (val is PSObject valAsPSObject)
            {
                var typeNames = valAsPSObject.InternalTypeNames;
                if ((typeNames != null) && (typeNames.Count >= 1))
                {
                    displayTypeName = typeNames[0];
                }
            }

            if (string.IsNullOrEmpty(displayTypeName))
            {
                displayTypeName = val == null
                    ? "object"
                    : ToStringCodeMethods.Type(val.GetType(), dropNamespaces: true);
            }

            return displayTypeName;
        }
    }

    /// <summary>
    /// Serves as a property that is a simple name-value pair.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSNoteProperty"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSVariableProperty : PSNoteProperty
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(GetDisplayTypeNameOfValue(_variable.Value));
            returnValue.Append(' ');
            returnValue.Append(_variable.Name);
            returnValue.Append('=');
            returnValue.Append(_variable.Value ?? "null");
            return returnValue.ToString();
        }

        internal PSVariable _variable;

        /// <summary>
        /// Initializes a new instance of the PSVariableProperty class. This is
        /// a subclass of the NoteProperty that wraps a variable instead of a simple value.
        /// </summary>
        /// <param name="variable">The variable to wrap.</param>
        /// <exception cref="ArgumentException">For an empty or null name.</exception>
        public PSVariableProperty(PSVariable variable)
            : base(variable?.Name, null)
        {
            _variable = variable ?? throw PSTraceSource.NewArgumentException(nameof(variable));
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo,
        /// Note that it returns another reference to the variable, not a reference
        /// to a new variable...
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSNoteProperty property = new PSVariableProperty(_variable);
            CloneBaseProperties(property);
            return property;
        }

        /// <summary>
        /// Gets PSMemberTypes.NoteProperty.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.NoteProperty;

        /// <summary>
        /// True if the underlying variable is settable...
        /// </summary>
        public override bool IsSettable => (_variable.Options & (ScopedItemOptions.Constant | ScopedItemOptions.ReadOnly)) == ScopedItemOptions.None;

        /// <summary>
        /// Gets true since the value of an PSNoteProperty can always be obtained.
        /// </summary>
        public override bool IsGettable => true;

        /// <summary>
        /// Gets or sets the value of this property.
        /// </summary>
        public override object Value
        {
            get => _variable.Value;
            set
            {
                if (!this.IsInstance)
                {
                    throw new SetValueException("ChangeValueOfStaticNote",
                        null,
                        ExtendedTypeSystem.ChangeStaticMember,
                        this.Name);
                }

                _variable.Value = value;
            }
        }

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        public override string TypeNameOfValue
        {
            get
            {
                object val = _variable.Value;

                if (val == null)
                {
                    return typeof(object).FullName;
                }

                if (val is PSObject valAsPSObject)
                {
                    var typeNames = valAsPSObject.InternalTypeNames;
                    if ((typeNames != null) && (typeNames.Count >= 1))
                    {
                        // type name at 0-th index is the most specific type (i.e. system.string)
                        // type names at other indices are less specific (i.e. system.object)
                        return typeNames[0];
                    }
                }

                return val.GetType().FullName;
            }
        }

        #endregion virtual implementation
    }

    /// <summary>
    /// Serves as a property implemented with getter and setter scripts.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSScriptProperty"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSScriptProperty : PSPropertyInfo
    {
        /// <summary>
        /// Returns the string representation of this property.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(this.TypeNameOfValue);
            returnValue.Append(' ');
            returnValue.Append(this.Name);
            returnValue.Append(" {");
            if (this.IsGettable)
            {
                returnValue.Append("get=");
                returnValue.Append(this.GetterScript.ToString());
                returnValue.Append(';');
            }

            if (this.IsSettable)
            {
                returnValue.Append("set=");
                returnValue.Append(this.SetterScript.ToString());
                returnValue.Append(';');
            }

            returnValue.Append('}');
            return returnValue.ToString();
        }

        private readonly PSLanguageMode? _languageMode;
        private readonly string _getterScriptText;
        private ScriptBlock _getterScript;

        private readonly string _setterScriptText;
        private ScriptBlock _setterScript;
        private bool _shouldCloneOnAccess;

        /// <summary>
        /// Gets the script used for the property getter. It might be null.
        /// </summary>
        public ScriptBlock GetterScript
        {
            get
            {
                // If we don't have a script block for the getter, see if we
                // have the text for it (to support delayed script compilation).
                if ((_getterScript == null) && (_getterScriptText != null))
                {
                    _getterScript = ScriptBlock.Create(_getterScriptText);

                    if (_languageMode.HasValue)
                    {
                        _getterScript.LanguageMode = _languageMode;
                    }

                    _getterScript.DebuggerStepThrough = true;
                }

                if (_getterScript == null)
                {
                    return null;
                }

                if (_shouldCloneOnAccess)
                {
                    // returning a clone as TypeTable might be shared between multiple
                    // runspaces and ScriptBlock is not shareable. We decided to
                    // Clone as needed instead of Cloning whenever a shared TypeTable is
                    // attached to a Runspace to save on Memory.
                    ScriptBlock newGetterScript = _getterScript.Clone();
                    newGetterScript.LanguageMode = _getterScript.LanguageMode;
                    return newGetterScript;
                }
                else
                {
                    return _getterScript;
                }
            }
        }

        /// <summary>
        /// Gets the script used for the property setter. It might be null.
        /// </summary>
        public ScriptBlock SetterScript
        {
            get
            {
                // If we don't have a script block for the setter, see if we
                // have the text for it (to support delayed script compilation).
                if ((_setterScript == null) && (_setterScriptText != null))
                {
                    _setterScript = ScriptBlock.Create(_setterScriptText);

                    if (_languageMode.HasValue)
                    {
                        _setterScript.LanguageMode = _languageMode;
                    }

                    _setterScript.DebuggerStepThrough = true;
                }

                if (_setterScript == null)
                {
                    return null;
                }

                if (_shouldCloneOnAccess)
                {
                    // returning a clone as TypeTable might be shared between multiple
                    // runspaces and ScriptBlock is not shareable. We decided to
                    // Clone as needed instead of Cloning whenever a shared TypeTable is
                    // attached to a Runspace to save on Memory.
                    ScriptBlock newSetterScript = _setterScript.Clone();
                    newSetterScript.LanguageMode = _setterScript.LanguageMode;
                    return newSetterScript;
                }
                else
                {
                    return _setterScript;
                }
            }
        }

        /// <summary>
        /// Initializes an instance of the PSScriptProperty class as a read only property.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="getterScript">Script to be used for the property getter. $this will be this PSObject.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSScriptProperty(string name, ScriptBlock getterScript)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;

            _getterScript = getterScript ?? throw PSTraceSource.NewArgumentNullException(nameof(getterScript));
        }

        /// <summary>
        /// Initializes an instance of the PSScriptProperty class as a read only
        /// property. getterScript or setterScript can be null, but not both.
        /// </summary>
        /// <param name="name">Name of this property.</param>
        /// <param name="getterScript">Script to be used for the property getter. $this will be this PSObject.</param>
        /// <param name="setterScript">Script to be used for the property setter. $this will be this PSObject and $args(1) will be the value to set.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSScriptProperty(string name, ScriptBlock getterScript, ScriptBlock setterScript)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (getterScript == null && setterScript == null)
            {
                // we only do not allow both getterScript and setterScript to be null
                throw PSTraceSource.NewArgumentException("getterScript setterScript");
            }

            if (getterScript != null)
            {
                getterScript.DebuggerStepThrough = true;
            }

            if (setterScript != null)
            {
                setterScript.DebuggerStepThrough = true;
            }

            _getterScript = getterScript;
            _setterScript = setterScript;
        }

        /// <summary>
        /// Initializes an instance of the PSScriptProperty class as a read only
        /// property, using the text of the properties to support lazy initialization.
        /// </summary>
        /// <param name="name">Name of this property.</param>
        /// <param name="getterScript">Script to be used for the property getter. $this will be this PSObject.</param>
        /// <param name="setterScript">Script to be used for the property setter. $this will be this PSObject and $args(1) will be the value to set.</param>
        /// <param name="languageMode">Language mode to be used during script block evaluation.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSScriptProperty(string name, string getterScript, string setterScript, PSLanguageMode? languageMode)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (getterScript == null && setterScript == null)
            {
                // we only do not allow both getterScript and setterScript to be null
                throw PSTraceSource.NewArgumentException("getterScript setterScript");
            }

            _getterScriptText = getterScript;
            _setterScriptText = setterScript;
            _languageMode = languageMode;
        }

        internal PSScriptProperty(string name, ScriptBlock getterScript, ScriptBlock setterScript, bool shouldCloneOnAccess)
            : this(name, getterScript, setterScript)
        {
            _shouldCloneOnAccess = shouldCloneOnAccess;
        }

        internal PSScriptProperty(string name, string getterScript, string setterScript, PSLanguageMode? languageMode, bool shouldCloneOnAccess)
            : this(name, getterScript, setterScript, languageMode)
        {
            _shouldCloneOnAccess = shouldCloneOnAccess;
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            var property = new PSScriptProperty(name, this.GetterScript, this.SetterScript) { _shouldCloneOnAccess = _shouldCloneOnAccess };
            CloneBaseProperties(property);
            return property;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.ScriptProperty;

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        public override bool IsSettable => this._setterScript != null || this._setterScriptText != null;

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        public override bool IsGettable => this._getterScript != null || this._getterScriptText != null;

        /// <summary>
        /// Gets and Sets the value of this property.
        /// </summary>
        /// <exception cref="GetValueException">When getting and there is no getter,
        /// when the getter throws an exception or when there is no Runspace to run the script.
        /// </exception>
        /// <exception cref="SetValueException">When setting and there is no setter,
        /// when the setter throws an exception or when there is no Runspace to run the script.</exception>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "<Pending>")]
        public override object Value
        {
            get
            {
                if (this.GetterScript == null)
                {
                    throw new GetValueException("GetWithoutGetterFromScriptPropertyValue",
                        null,
                        ExtendedTypeSystem.GetWithoutGetterException,
                        this.Name);
                }

                return InvokeGetter(this.instance);
            }

            set
            {
                if (this.SetterScript == null)
                {
                    throw new SetValueException("SetWithoutSetterFromScriptProperty",
                        null,
                        ExtendedTypeSystem.SetWithoutSetterException,
                        this.Name);
                }

                InvokeSetter(this.instance, value);
            }
        }

        internal object InvokeSetter(object scriptThis, object value)
        {
            try
            {
                SetterScript.DoInvokeReturnAsIs(
                    useLocalScope: true,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                    dollarUnder: AutomationNull.Value,
                    input: AutomationNull.Value,
                    scriptThis: scriptThis,
                    args: new[] { value });
                return value;
            }
            catch (RuntimeException e)
            {
                throw NewSetValueException(e, "ScriptSetValueRuntimeException");
            }
            catch (TerminateException)
            {
                // The debugger is terminating the execution; let the exception bubble up
                throw;
            }
            catch (FlowControlException e)
            {
                throw NewSetValueException(e, "ScriptSetValueFlowControlException");
            }
            catch (PSInvalidOperationException e)
            {
                throw NewSetValueException(e, "ScriptSetValueInvalidOperationException");
            }
        }

        internal object InvokeGetter(object scriptThis)
        {
            try
            {
                return GetterScript.DoInvokeReturnAsIs(
                    useLocalScope: true,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.SwallowErrors,
                    dollarUnder: AutomationNull.Value,
                    input: AutomationNull.Value,
                    scriptThis: scriptThis,
                    args: Array.Empty<object>());
            }
            catch (RuntimeException e)
            {
                throw NewGetValueException(e, "ScriptGetValueRuntimeException");
            }
            catch (TerminateException)
            {
                // The debugger is terminating the execution; let the exception bubble up
                throw;
            }
            catch (FlowControlException e)
            {
                throw NewGetValueException(e, "ScriptGetValueFlowControlException");
            }
            catch (PSInvalidOperationException e)
            {
                throw NewGetValueException(e, "ScriptgetValueInvalidOperationException");
            }
        }

        /// <summary>
        /// Gets the type of the value for this member. Currently this always returns typeof(object).FullName.
        /// </summary>
        public override string TypeNameOfValue
        {
            get
            {
                if ((this.GetterScript != null) &&
                    (this.GetterScript.OutputType.Count > 0))
                {
                    return this.GetterScript.OutputType[0].Name;
                }
                else
                {
                    return typeof(object).FullName;
                }
            }
        }

        #endregion virtual implementation
    }

    internal sealed class PSMethodInvocationConstraints
    {
        internal PSMethodInvocationConstraints(
            Type methodTargetType,
            Type[] parameterTypes)
            : this(methodTargetType, parameterTypes, genericTypeParameters: null)
        {
        }

        internal PSMethodInvocationConstraints(
            Type methodTargetType,
            Type[] parameterTypes,
            object[] genericTypeParameters)
        {
            MethodTargetType = methodTargetType;
            ParameterTypes = parameterTypes;
            GenericTypeParameters = genericTypeParameters;
        }

        /// <remarks>
        /// If <see langword="null"/> then there are no constraints
        /// </remarks>
        public Type MethodTargetType { get; }

        /// <remarks>
        /// If <see langword="null"/> then there are no constraints
        /// </remarks>
        public Type[] ParameterTypes { get; }

        /// <summary>
        /// Gets the generic type parameters for the method invocation.
        /// </summary>
        public object[] GenericTypeParameters { get; }

        internal static bool EqualsForCollection<T>(ICollection<T> xs, ICollection<T> ys)
        {
            if (xs == null)
            {
                return ys == null;
            }

            if (ys == null)
            {
                return false;
            }

            if (xs.Count != ys.Count)
            {
                return false;
            }

            return xs.SequenceEqual(ys);
        }

        public bool Equals(PSMethodInvocationConstraints other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other.MethodTargetType != this.MethodTargetType)
            {
                return false;
            }

            if (!EqualsForCollection(ParameterTypes, other.ParameterTypes))
            {
                return false;
            }

            if (!EqualsForCollection(GenericTypeParameters, other.GenericTypeParameters))
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(PSMethodInvocationConstraints))
            {
                return false;
            }

            return Equals((PSMethodInvocationConstraints)obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(MethodTargetType, ParameterTypes, GenericTypeParameters);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string separator = string.Empty;
            if (MethodTargetType is not null)
            {
                sb.Append("this: ");
                sb.Append(ToStringCodeMethods.Type(MethodTargetType, dropNamespaces: true));
                separator = " ";
            }

            if (GenericTypeParameters is not null)
            {
                sb.Append(separator);
                sb.Append("genericTypeParams: ");

                separator = string.Empty;
                foreach (object parameter in GenericTypeParameters)
                {
                    sb.Append(separator);

                    switch (parameter)
                    {
                        case Type paramType:
                            sb.Append(ToStringCodeMethods.Type(paramType, dropNamespaces: true));
                            break;
                        case ITypeName paramTypeName:
                            sb.Append(paramTypeName.ToString());
                            break;
                        default:
                            throw new ArgumentException("Unexpected value");
                    }

                    separator = ", ";
                }

                separator = " ";
            }

            if (ParameterTypes is not null)
            {
                sb.Append(separator);
                sb.Append("args: ");
                separator = string.Empty;
                foreach (var p in ParameterTypes)
                {
                    sb.Append(separator);
                    sb.Append(ToStringCodeMethods.Type(p, dropNamespaces: true));
                    separator = ", ";
                }
            }

            if (sb.Length == 0)
            {
                sb.Append("<empty>");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Serves as a base class for all members that behave like methods.
    /// </summary>
    public abstract class PSMethodInfo : PSMemberInfo
    {
        /// <summary>
        /// Initializes a new instance of a class derived from PSMethodInfo.
        /// </summary>
        protected PSMethodInfo()
        {
        }

        /// <summary>
        /// Invokes the appropriate method overload for the given arguments and returns its result.
        /// </summary>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="MethodException">For problems finding an appropriate method for the arguments.</exception>
        /// <exception cref="MethodInvocationException">For exceptions invoking the method.
        /// This exception is also thrown for an <see cref="PSScriptMethod"/> when there is no Runspace to run the script.</exception>
        public abstract object Invoke(params object[] arguments);

        /// <summary>
        /// Gets a list of all the overloads for this method.
        /// </summary>
        public abstract Collection<string> OverloadDefinitions { get; }

        #region virtual implementation

        /// <summary>
        /// Gets the value of this member. The getter returns the PSMethodInfo itself.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">When setting the member.</exception>
        /// <remarks>
        /// This is not the returned value of the method even for Methods with no arguments.
        /// The getter returns this (the PSMethodInfo itself). The setter is not supported.
        /// </remarks>
        public sealed override object Value
        {
            get => this;
            set => throw new ExtendedTypeSystemException("CannotChangePSMethodInfoValue",
                null,
                ExtendedTypeSystem.CannotSetValueForMemberType,
                this.GetType().FullName);
        }

        #endregion virtual implementation
    }

    /// <summary>
    /// Serves as a method implemented with a reference to another method.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSCodeMethod"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSCodeMethod : PSMethodInfo
    {
        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            foreach (string overload in OverloadDefinitions)
            {
                returnValue.Append(overload);
                returnValue.Append(", ");
            }

            returnValue.Remove(returnValue.Length - 2, 2);
            return returnValue.ToString();
        }

        private MethodInformation[] _codeReferenceMethodInformation;

        internal static bool CheckMethodInfo(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.IsStatic
                   && method.IsPublic
                   && parameters.Length != 0
                   && parameters[0].ParameterType == typeof(PSObject);
        }

        internal void SetCodeReference(Type type, string methodName)
        {
            MethodInfo methodAsMember = null;

            try
            {
                methodAsMember = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
            }
            catch (AmbiguousMatchException)
            {
                // Ignore the AmbiguousMatchException.
                // We will generate error below if we cannot find exactly one match method.
            }

            if (methodAsMember == null)
            {
                throw new ExtendedTypeSystemException("WrongMethodFormatFromTypeTable", null,
                    ExtendedTypeSystem.CodeMethodMethodFormat);
            }

            CodeReference = methodAsMember;
            if (!CheckMethodInfo(CodeReference))
            {
                throw new ExtendedTypeSystemException("WrongMethodFormat", null, ExtendedTypeSystem.CodeMethodMethodFormat);
            }
        }

        /// <summary>
        /// Used from TypeTable.
        /// </summary>
        internal PSCodeMethod(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the PSCodeMethod class.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="codeReference">This should be a public static method where the first parameter is an PSObject.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        /// <exception cref="ExtendedTypeSystemException">If the codeReference does not have the right format.</exception>
        public PSCodeMethod(string name, MethodInfo codeReference)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (codeReference == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(codeReference));
            }

            if (!CheckMethodInfo(codeReference))
            {
                throw new ExtendedTypeSystemException("WrongMethodFormat", null, ExtendedTypeSystem.CodeMethodMethodFormat);
            }

            this.name = name;
            CodeReference = codeReference;
        }

        /// <summary>
        /// Gets the method referenced by this PSCodeMethod.
        /// </summary>
        public MethodInfo CodeReference { get; private set; }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSCodeMethod member = new PSCodeMethod(name, CodeReference);
            CloneBaseProperties(member);
            return member;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.CodeMethod;

        /// <summary>
        /// Invokes CodeReference method and returns its results.
        /// </summary>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="MethodException">
        ///     When
        ///         could CodeReference cannot match the given argument count or
        ///         could not convert an argument to the type required
        /// </exception>
        /// <exception cref="MethodInvocationException">For exceptions invoking the CodeReference.</exception>
        public override object Invoke(params object[] arguments)
        {
            if (arguments == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arguments));
            }

            object[] newArguments = new object[arguments.Length + 1];
            newArguments[0] = this.instance;
            for (int i = 0; i < arguments.Length; i++)
            {
                newArguments[i + 1] = arguments[i];
            }

            _codeReferenceMethodInformation ??= DotNetAdapter.GetMethodInformationArray(new[] { CodeReference });

            Adapter.GetBestMethodAndArguments(CodeReference.Name, _codeReferenceMethodInformation, newArguments, out object[] convertedArguments);

            return DotNetAdapter.AuxiliaryMethodInvoke(null, convertedArguments, _codeReferenceMethodInformation[0], newArguments);
        }

        /// <summary>
        /// Gets the definition for CodeReference.
        /// </summary>
        public override Collection<string> OverloadDefinitions => new Collection<string>
        {
            DotNetAdapter.GetMethodInfoOverloadDefinition(null, CodeReference, 0)
        };

        /// <summary>
        /// Gets the type of the value for this member. Currently this always returns typeof(PSCodeMethod).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(PSCodeMethod).FullName;

        #endregion virtual implementation
    }

    /// <summary>
    /// Serves as a method implemented with a script.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSScriptMethod"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSScriptMethod : PSMethodInfo
    {
        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(this.TypeNameOfValue);
            returnValue.Append(' ');
            returnValue.Append(this.Name);
            returnValue.Append("();");
            return returnValue.ToString();
        }

        private readonly ScriptBlock _script;
        private bool _shouldCloneOnAccess;

        /// <summary>
        /// Gets the script implementing this PSScriptMethod.
        /// </summary>
        public ScriptBlock Script
        {
            get
            {
                if (_shouldCloneOnAccess)
                {
                    // returning a clone as TypeTable might be shared between multiple
                    // runspaces and ScriptBlock is not shareable. We decided to
                    // Clone as needed instead of Cloning whenever a shared TypeTable is
                    // attached to a Runspace to save on Memory.
                    ScriptBlock newScript = _script.Clone();
                    newScript.LanguageMode = _script.LanguageMode;

                    return newScript;
                }
                else
                {
                    return _script;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of PSScriptMethod.
        /// </summary>
        /// <param name="name">Name of the method.</param>
        /// <param name="script">Script to be used when calling the method.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSScriptMethod(string name, ScriptBlock script)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;

            _script = script ?? throw PSTraceSource.NewArgumentNullException(nameof(script));
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="script"></param>
        /// <param name="shouldCloneOnAccess">
        /// Used by TypeTable.
        /// TypeTable might be shared between multiple runspaces and
        /// ScriptBlock is not shareable. We decided to Clone as needed
        /// instead of Cloning whenever a shared TypeTable is attached
        /// to a Runspace to save on Memory.
        /// </param>
        internal PSScriptMethod(string name, ScriptBlock script, bool shouldCloneOnAccess)
            : this(name, script)
        {
            _shouldCloneOnAccess = shouldCloneOnAccess;
        }

        #region virtual implementation

        /// <summary>
        /// Invokes Script method and returns its results.
        /// </summary>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="MethodInvocationException">For exceptions invoking the Script or if there is no Runspace to run the script.</exception>
        public override object Invoke(params object[] arguments)
        {
            if (arguments == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arguments));
            }

            return InvokeScript(Name, _script, this.instance, arguments);
        }

        internal static object InvokeScript(string methodName, ScriptBlock script, object @this, object[] arguments)
        {
            try
            {
                return script.DoInvokeReturnAsIs(
                    useLocalScope: true,
                    errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                    dollarUnder: AutomationNull.Value,
                    input: AutomationNull.Value,
                    scriptThis: @this,
                    args: arguments);
            }
            catch (RuntimeException e)
            {
                throw new MethodInvocationException(
                    "ScriptMethodRuntimeException",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    methodName, arguments.Length, e.Message);
            }
            catch (TerminateException)
            {
                // The debugger is terminating the execution; let the exception bubble up
                throw;
            }
            catch (FlowControlException e)
            {
                throw new MethodInvocationException(
                    "ScriptMethodFlowControlException",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    methodName, arguments.Length, e.Message);
            }
            catch (PSInvalidOperationException e)
            {
                throw new MethodInvocationException(
                    "ScriptMethodInvalidOperationException",
                    e,
                    ExtendedTypeSystem.MethodInvocationException,
                    methodName, arguments.Length, e.Message);
            }
        }

        /// <summary>
        /// Gets a list of all the overloads for this method.
        /// </summary>
        public override Collection<string> OverloadDefinitions
        {
            get
            {
                Collection<string> retValue = new Collection<string> { this.ToString() };
                return retValue;
            }
        }

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            var method = new PSScriptMethod(this.name, _script) { _shouldCloneOnAccess = _shouldCloneOnAccess };
            CloneBaseProperties(method);
            return method;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.ScriptMethod;

        /// <summary>
        /// Gets the type of the value for this member. Currently this always returns typeof(object).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(object).FullName;

        #endregion virtual implementation
    }

    /// <summary>
    /// Used to access the adapted or base methods from the BaseObject.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSMethod"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSMethod : PSMethodInfo
    {
        internal override void ReplicateInstance(object particularInstance)
        {
            base.ReplicateInstance(particularInstance);
            baseObject = particularInstance;
        }

        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            return _adapter.BaseMethodToString(this);
        }

        internal object adapterData;
        internal Adapter _adapter;
        internal object baseObject;

        /// <summary>
        /// Constructs this method.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="adapter">Adapter to be used invoking.</param>
        /// <param name="baseObject">BaseObject for the methods.</param>
        /// <param name="adapterData">AdapterData from adapter.GetMethodData.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSMethod(string name, Adapter adapter, object baseObject, object adapterData)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            this.adapterData = adapterData;
            this._adapter = adapter;
            this.baseObject = baseObject;
        }

        /// <summary>
        /// Constructs a PSMethod.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="adapter">Adapter to be used invoking.</param>
        /// <param name="baseObject">BaseObject for the methods.</param>
        /// <param name="adapterData">AdapterData from adapter.GetMethodData.</param>
        /// <param name="isSpecial">True if this member is a special member, false otherwise.</param>
        /// <param name="isHidden">True if this member is hidden, false otherwise.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSMethod(string name, Adapter adapter, object baseObject, object adapterData, bool isSpecial, bool isHidden)
            : this(name, adapter, baseObject, adapterData)
        {
            this.IsSpecial = isSpecial;
            this.IsHidden = isHidden;
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSMethod member = new PSMethod(this.name, _adapter, this.baseObject, this.adapterData, this.IsSpecial, this.IsHidden);
            CloneBaseProperties(member);
            return member;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.Method;

        /// <summary>
        /// Invokes the appropriate method overload for the given arguments and returns its result.
        /// </summary>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="MethodException">For problems finding an appropriate method for the arguments.</exception>
        /// <exception cref="MethodInvocationException">For exceptions invoking the method.</exception>
        public override object Invoke(params object[] arguments)
        {
            return this.Invoke(null, arguments);
        }

        /// <summary>
        /// Invokes the appropriate method overload for the given arguments and returns its result.
        /// </summary>
        /// <param name="invocationConstraints">Constraints.</param>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="MethodException">For problems finding an appropriate method for the arguments.</exception>
        /// <exception cref="MethodInvocationException">For exceptions invoking the method.</exception>
        internal object Invoke(PSMethodInvocationConstraints invocationConstraints, params object[] arguments)
        {
            if (arguments == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arguments));
            }

            return _adapter.BaseMethodInvoke(this, invocationConstraints, arguments);
        }

        /// <summary>
        /// Gets a list of all the overloads for this method.
        /// </summary>
        public override Collection<string> OverloadDefinitions => _adapter.BaseMethodDefinitions(this);

        /// <summary>
        /// Gets the type of the value for this member. This always returns typeof(PSMethod).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(PSMethod).FullName;

        #endregion virtual implementation

        /// <summary>
        /// True if the method is a special method like GET/SET property accessor methods.
        /// </summary>
        internal bool IsSpecial { get; }

        internal static PSMethod Create(string name, DotNetAdapter dotNetInstanceAdapter, object baseObject, DotNetAdapter.MethodCacheEntry method)
        {
            return Create(name, dotNetInstanceAdapter, baseObject, method, false, false);
        }

        internal static PSMethod Create(string name, DotNetAdapter dotNetInstanceAdapter, object baseObject, DotNetAdapter.MethodCacheEntry method, bool isSpecial, bool isHidden)
        {
            if (method[0].method is ConstructorInfo)
            {
                // Constructor cannot be converted to a delegate, so just return a simple PSMethod instance
                return new PSMethod(name, dotNetInstanceAdapter, baseObject, method, isSpecial, isHidden);
            }

            method.PSMethodCtor ??= CreatePSMethodConstructor(method.methodInformationStructures);

            return method.PSMethodCtor.Invoke(name, dotNetInstanceAdapter, baseObject, method, isSpecial, isHidden);
        }

        private static Type GetMethodGroupType(MethodInfo methodInfo)
        {
            if (methodInfo.DeclaringType.IsGenericTypeDefinition)
            {
                // If the method is from a generic type definition, consider it not convertible.
                return typeof(Func<PSNonBindableType>);
            }

            if (methodInfo.IsGenericMethodDefinition)
            {
                // For a generic method, it's possible to infer the generic parameters based on the target delegate.
                // However, we don't yet handle generic methods in PSMethod-to-Delegate conversion, so for now, we
                // don't produce the metadata type that represents the signature of a generic method.
                //
                // Say one day we want to support generic method in PSMethod-to-Delegate conversion and need to produce
                // the metadata type, we should use the generic parameter types from the MethodInfo directly to construct
                // the Func<> metadata type. See the concept shown in the following scripts:
                //    $class = "public class Zoo { public static T GetName<T>(int index, T input) { return default(T); } }"
                //    Add-Type -TypeDefinition $class
                //    $method = [Zoo].GetMethod("GetName")
                //    $allTypes = $method.GetParameters().ParameterType + $method.ReturnType
                //    $metadataType = [Func`3].MakeGenericType($allTypes)
                // In this way, '$metadataType.ContainsGenericParameters' returns 'True', indicating it represents a generic method.
                // And also, given a generic argument type from `$metadataType.GetGenericArguments()`, it's easy to tell if it's a
                // generic parameter (for example, 'T') based on the property 'IsGenericParameter'.
                // Moreover, it's also easy to get constraints of the generic parameter, via 'GetGenericParameterConstraints()'
                // and 'GenericParameterAttributes'.
                return typeof(Func<PSNonBindableType>);
            }

            var parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length > 16)
            {
                // Too many parameters, an unlikely scenario.
                return typeof(Func<PSNonBindableType>);
            }

            try
            {
                var methodTypes = new Type[parameterInfos.Length + 1];
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];
                    Type parameterType = parameterInfo.ParameterType;
                    methodTypes[i] = GetPSMethodProjectedType(parameterType, parameterInfo.IsOut);
                }

                methodTypes[parameterInfos.Length] = GetPSMethodProjectedType(methodInfo.ReturnType);

                return DelegateHelpers.MakeDelegate(methodTypes);
            }
            catch (Exception)
            {
                return typeof(Func<PSNonBindableType>);
            }
        }

        private static Type GetPSMethodProjectedType(Type type, bool isOut = false)
        {
            if (type == typeof(void))
            {
                return typeof(VOID);
            }

            if (type == typeof(TypedReference))
            {
                return typeof(PSTypedReference);
            }

            if (type.IsByRef)
            {
                var elementType = GetPSMethodProjectedType(type.GetElementType());
                type = isOut ? typeof(PSOutParameter<>).MakeGenericType(elementType)
                             : typeof(PSReference<>).MakeGenericType(elementType);
            }
            else if (type.IsPointer)
            {
                var elementType = GetPSMethodProjectedType(type.GetElementType());
                type = typeof(PSPointer<>).MakeGenericType(elementType);
            }

            return type;
        }

        private static Func<string, DotNetAdapter, object, object, bool, bool, PSMethod> CreatePSMethodConstructor(MethodInformation[] methods)
        {
            // Produce the PSMethod creator for MethodInfo objects
            var types = new Type[methods.Length];
            for (int i = 0; i < methods.Length; i++)
            {
                types[i] = GetMethodGroupType((MethodInfo)methods[i].method);
            }

            var methodGroupType = CreateMethodGroup(types, 0, types.Length);
            Type psMethodType = typeof(PSMethod<>).MakeGenericType(methodGroupType);
            var delegateType = typeof(Func<string, DotNetAdapter, object, object, bool, bool, PSMethod>);
            return (Func<string, DotNetAdapter, object, object, bool, bool, PSMethod>)Delegate.CreateDelegate(delegateType,
                psMethodType.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static));
        }

        private static Type CreateMethodGroup(Type[] sourceTypes, int start, int count)
        {
            var types = sourceTypes;
            if (count != sourceTypes.Length)
            {
                types = new Type[count];
                Array.Copy(sourceTypes, start, types, 0, count);
            }

            switch (count)
            {
                case 1: return typeof(MethodGroup<>).MakeGenericType(types);
                case 2: return typeof(MethodGroup<,>).MakeGenericType(types);
                case 3: return typeof(MethodGroup<,>).MakeGenericType(types[0], CreateMethodGroup(types, 1, 2));
                case 4: return typeof(MethodGroup<,,,>).MakeGenericType(types);
                case int i when i < 8: return typeof(MethodGroup<,,,>).MakeGenericType(types[0], types[1], types[2], CreateMethodGroup(types, 3, i - 3));
                case 8: return typeof(MethodGroup<,,,,,,,>).MakeGenericType(types);
                case int i when i < 16:
                    return typeof(MethodGroup<,,,,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], types[4], types[5], types[6], CreateMethodGroup(types, 7, i - 7));
                case 16: return typeof(MethodGroup<,,,,,,,,,,,,,,,>).MakeGenericType(types);
                case int i when i < 32:
                    return typeof(MethodGroup<,,,,,,,,,,,,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], types[4], types[5], types[6], types[7], types[8], types[9], types[10],
                        types[11], types[12], types[13], types[14], CreateMethodGroup(types, 15, i - 15));
                case 32: return typeof(MethodGroup<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types);
                default:
                    return typeof(MethodGroup<,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,>).MakeGenericType(types[0], types[1], types[2], types[3], types[4], types[5], types[6], types[7], types[8],
                        types[9], types[10], types[11], types[12], types[13], types[14], types[15], types[16], types[17], types[18], types[19], types[20], types[21], types[22], types[23],
                        types[24], types[25], types[26], types[27], types[28], types[29], types[30], CreateMethodGroup(sourceTypes, start + 31, count - 31));
            }
        }
    }

    internal abstract class PSNonBindableType
    {
    }

    internal sealed class VOID
    {
    }

    internal sealed class PSOutParameter<T>
    {
    }

    internal struct PSPointer<T>
    {
    }

    internal struct PSTypedReference
    {
    }

    internal abstract class MethodGroup
    {
    }

    internal sealed class MethodGroup<T1> : MethodGroup
    {
    }

    internal sealed class MethodGroup<T1, T2> : MethodGroup
    {
    }

    internal sealed class MethodGroup<T1, T2, T3, T4> : MethodGroup
    {
    }

    internal sealed class MethodGroup<T1, T2, T3, T4, T5, T6, T7, T8> : MethodGroup
    {
    }

    internal sealed class MethodGroup<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : MethodGroup
    {
    }

    internal sealed class MethodGroup<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31,
        T32> : MethodGroup
    {
    }

    internal struct PSMethodSignatureEnumerator : IEnumerator<Type>
    {
        private int _currentIndex;
        private readonly Type _t;

        internal PSMethodSignatureEnumerator(Type t)
        {
            Diagnostics.Assert(t.IsSubclassOf(typeof(PSMethod)), "Must be a PSMethod<MethodGroup<>>");
            _t = t.GenericTypeArguments[0];
            Current = null;
            _currentIndex = -1;
        }

        public bool MoveNext()
        {
            _currentIndex++;
            return MoveNext(_t, _currentIndex);
        }

        private bool MoveNext(Type type, int index)
        {
            var genericTypeArguments = type.GenericTypeArguments;
            var length = genericTypeArguments.Length;
            if (index < length - 1)
            {
                Current = genericTypeArguments[index];
                return true;
            }

            var t = genericTypeArguments[length - 1];
            if (t.IsSubclassOf(typeof(MethodGroup)))
            {
                var remaining = index - (length - 1);
                return MoveNext(t, remaining);
            }

            if (index >= length)
            {
                Current = null;
                return false;
            }

            Current = t;
            return true;
        }

        public void Reset()
        {
            _currentIndex = -1;
            Current = null;
        }

        public Type Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    internal sealed class PSMethod<T> : PSMethod
    {
        public override PSMemberInfo Copy()
        {
            PSMethod member = new PSMethod<T>(this.name, this._adapter, this.baseObject, this.adapterData, this.IsSpecial, this.IsHidden);
            CloneBaseProperties(member);
            return member;
        }

        internal PSMethod(string name, Adapter adapter, object baseObject, object adapterData)
            : base(name, adapter, baseObject, adapterData)
        {
        }

        internal PSMethod(string name, Adapter adapter, object baseObject, object adapterData, bool isSpecial, bool isHidden)
            : base(name, adapter, baseObject, adapterData, isSpecial, isHidden)
        {
        }

        /// <summary>
        /// Helper factory function since we cannot bind a delegate to a ConstructorInfo.
        /// </summary>
        internal static PSMethod<T> Create(string name, Adapter adapter, object baseObject, object adapterData, bool isSpecial, bool isHidden)
        {
            return new PSMethod<T>(name, adapter, baseObject, adapterData, isSpecial, isHidden);
        }
    }

    /// <summary>
    /// Used to access parameterized properties from the BaseObject.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSParameterizedProperty"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSParameterizedProperty : PSMethodInfo
    {
        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            Diagnostics.Assert((this.baseObject != null) && (this.adapter != null) && (this.adapterData != null), "it should have all these properties set");
            return this.adapter.BaseParameterizedPropertyToString(this);
        }

        internal Adapter adapter;
        internal object adapterData;
        internal object baseObject;

        /// <summary>
        /// Constructs this parameterized property.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="adapter">Adapter used in DoGetMethod.</param>
        /// <param name="baseObject">Object passed to DoGetMethod.</param>
        /// <param name="adapterData">Adapter specific data.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSParameterizedProperty(string name, Adapter adapter, object baseObject, object adapterData)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            this.adapter = adapter;
            this.adapterData = adapterData;
            this.baseObject = baseObject;
        }

        internal PSParameterizedProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
        }

        /// <summary>
        /// Gets true if this property can be set.
        /// </summary>
        public bool IsSettable => adapter.BaseParameterizedPropertyIsSettable(this);

        /// <summary>
        /// Gets true if this property can be read.
        /// </summary>
        public bool IsGettable => adapter.BaseParameterizedPropertyIsGettable(this);

        #region virtual implementation

        /// <summary>
        /// Invokes the getter method and returns its result.
        /// </summary>
        /// <param name="arguments">Arguments to the method.</param>
        /// <returns>Return value from the method.</returns>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="GetValueException">When getting the value of a property throws an exception.</exception>
        public override object Invoke(params object[] arguments)
        {
            if (arguments == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arguments));
            }

            return this.adapter.BaseParameterizedPropertyGet(this, arguments);
        }

        /// <summary>
        /// Invokes the setter method.
        /// </summary>
        /// <param name="valueToSet">Value to set this property with.</param>
        /// <param name="arguments">Arguments to the method.</param>
        /// <exception cref="ArgumentException">If arguments is null.</exception>
        /// <exception cref="SetValueException">When setting the value of a property throws an exception.</exception>
        public void InvokeSet(object valueToSet, params object[] arguments)
        {
            if (arguments == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(arguments));
            }

            this.adapter.BaseParameterizedPropertySet(this, valueToSet, arguments);
        }

        /// <summary>
        /// Returns a collection of the definitions for this property.
        /// </summary>
        public override Collection<string> OverloadDefinitions => adapter.BaseParameterizedPropertyDefinitions(this);

        /// <summary>
        /// Gets the type of the value for this member.
        /// </summary>
        public override string TypeNameOfValue => adapter.BaseParameterizedPropertyType(this);

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSParameterizedProperty property = new PSParameterizedProperty(this.name, this.adapter, this.baseObject, this.adapterData);
            CloneBaseProperties(property);
            return property;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.ParameterizedProperty;

        #endregion virtual implementation
    }

    /// <summary>
    /// Serves as a set of members.
    /// </summary>
    public class PSMemberSet : PSMemberInfo
    {
        internal override void ReplicateInstance(object particularInstance)
        {
            base.ReplicateInstance(particularInstance);
            foreach (var member in Members)
            {
                member.ReplicateInstance(particularInstance);
            }
        }

        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(" {");

            foreach (PSMemberInfo member in this.Members)
            {
                returnValue.Append(member.Name);
                returnValue.Append(", ");
            }

            if (returnValue.Length > 2)
            {
                returnValue.Remove(returnValue.Length - 2, 2);
            }

            returnValue.Insert(0, this.Name);
            returnValue.Append('}');
            return returnValue.ToString();
        }

        private readonly PSMemberInfoIntegratingCollection<PSMemberInfo> _members;
        private readonly PSMemberInfoIntegratingCollection<PSPropertyInfo> _properties;
        private readonly PSMemberInfoIntegratingCollection<PSMethodInfo> _methods;
        internal PSMemberInfoInternalCollection<PSMemberInfo> internalMembers;
        private readonly PSObject _constructorPSObject;

        private static readonly Collection<CollectionEntry<PSMemberInfo>> s_emptyMemberCollection = new Collection<CollectionEntry<PSMemberInfo>>();
        private static readonly Collection<CollectionEntry<PSMethodInfo>> s_emptyMethodCollection = new Collection<CollectionEntry<PSMethodInfo>>();
        private static readonly Collection<CollectionEntry<PSPropertyInfo>> s_emptyPropertyCollection = new Collection<CollectionEntry<PSPropertyInfo>>();

        /// <summary>
        /// Initializes a new instance of PSMemberSet with no initial members.
        /// </summary>
        /// <param name="name">Name for the member set.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSMemberSet(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            this.internalMembers = new PSMemberInfoInternalCollection<PSMemberInfo>();
            _members = new PSMemberInfoIntegratingCollection<PSMemberInfo>(this, s_emptyMemberCollection);
            _properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_emptyPropertyCollection);
            _methods = new PSMemberInfoIntegratingCollection<PSMethodInfo>(this, s_emptyMethodCollection);
        }

        /// <summary>
        /// Initializes a new instance of PSMemberSet with all the initial members in <paramref name="members"/>
        /// </summary>
        /// <param name="name">Name for the member set.</param>
        /// <param name="members">Members in the member set.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSMemberSet(string name, IEnumerable<PSMemberInfo> members)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (members == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(members));
            }

            this.internalMembers = new PSMemberInfoInternalCollection<PSMemberInfo>();
            foreach (PSMemberInfo member in members)
            {
                if (member == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(members));
                }

                this.internalMembers.Add(member.Copy());
            }

            _members = new PSMemberInfoIntegratingCollection<PSMemberInfo>(this, s_emptyMemberCollection);
            _properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_emptyPropertyCollection);
            _methods = new PSMemberInfoIntegratingCollection<PSMethodInfo>(this, s_emptyMethodCollection);
        }

        /// <summary>
        /// Initializes a new instance of PSMemberSet with all the initial members in <paramref name="members"/>.
        /// This constructor is supposed to be used in TypeTable to reuse the passed-in member collection.
        /// Null-argument check is skipped here, so callers need to check arguments before passing in.
        /// </summary>
        /// <param name="name">Name for the member set.</param>
        /// <param name="members">Members in the member set.</param>
        internal PSMemberSet(string name, PSMemberInfoInternalCollection<PSMemberInfo> members)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(name), "Caller needs to guarantee not null or empty.");
            Diagnostics.Assert(members != null, "Caller needs to guarantee not null.");

            this.name = name;
            this.internalMembers = members;

            _members = new PSMemberInfoIntegratingCollection<PSMemberInfo>(this, s_emptyMemberCollection);
            _properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_emptyPropertyCollection);
            _methods = new PSMemberInfoIntegratingCollection<PSMethodInfo>(this, s_emptyMethodCollection);
        }

        private static readonly Collection<CollectionEntry<PSMemberInfo>> s_typeMemberCollection = GetTypeMemberCollection();
        private static readonly Collection<CollectionEntry<PSMethodInfo>> s_typeMethodCollection = GetTypeMethodCollection();
        private static readonly Collection<CollectionEntry<PSPropertyInfo>> s_typePropertyCollection = GetTypePropertyCollection();

        private static Collection<CollectionEntry<PSMemberInfo>> GetTypeMemberCollection()
        {
            Collection<CollectionEntry<PSMemberInfo>> returnValue = new Collection<CollectionEntry<PSMemberInfo>>();
            returnValue.Add(new CollectionEntry<PSMemberInfo>(
                PSObject.TypeTableGetMembersDelegate<PSMemberInfo>,
                PSObject.TypeTableGetMemberDelegate<PSMemberInfo>,
                PSObject.TypeTableGetFirstMemberOrDefaultDelegate<PSMemberInfo>,
                true, true, "type table members"));
            return returnValue;
        }

        private static Collection<CollectionEntry<PSMethodInfo>> GetTypeMethodCollection()
        {
            Collection<CollectionEntry<PSMethodInfo>> returnValue = new Collection<CollectionEntry<PSMethodInfo>>();
            returnValue.Add(new CollectionEntry<PSMethodInfo>(
                PSObject.TypeTableGetMembersDelegate<PSMethodInfo>,
                PSObject.TypeTableGetMemberDelegate<PSMethodInfo>,
                PSObject.TypeTableGetFirstMemberOrDefaultDelegate<PSMethodInfo>,
                true, true, "type table members"));
            return returnValue;
        }

        private static Collection<CollectionEntry<PSPropertyInfo>> GetTypePropertyCollection()
        {
            Collection<CollectionEntry<PSPropertyInfo>> returnValue = new Collection<CollectionEntry<PSPropertyInfo>>();
            returnValue.Add(new CollectionEntry<PSPropertyInfo>(
                PSObject.TypeTableGetMembersDelegate<PSPropertyInfo>,
                PSObject.TypeTableGetMemberDelegate<PSPropertyInfo>,
                PSObject.TypeTableGetFirstMemberOrDefaultDelegate<PSPropertyInfo>,
                true, true, "type table members"));
            return returnValue;
        }

        /// <summary>
        /// Used to create the Extended MemberSet.
        /// </summary>
        /// <param name="name">Name of the memberSet.</param>
        /// <param name="mshObject">Object associated with this memberset.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSMemberSet(string name, PSObject mshObject)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (mshObject == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(mshObject));
            }

            _constructorPSObject = mshObject;
            this.internalMembers = mshObject.InstanceMembers;
            _members = new PSMemberInfoIntegratingCollection<PSMemberInfo>(this, s_typeMemberCollection);
            _properties = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_typePropertyCollection);
            _methods = new PSMemberInfoIntegratingCollection<PSMethodInfo>(this, s_typeMethodCollection);
        }

        internal bool inheritMembers = true;

        /// <summary>
        /// Gets a flag indicating whether the memberset will inherit members of the memberset
        /// of the same name in the "parent" class.
        /// </summary>
        public bool InheritMembers => this.inheritMembers;

        /// <summary>
        /// Gets the internal member collection.
        /// </summary>
        internal virtual PSMemberInfoInternalCollection<PSMemberInfo> InternalMembers => this.internalMembers;

        /// <summary>
        /// Gets the member collection.
        /// </summary>
        public PSMemberInfoCollection<PSMemberInfo> Members => _members;

        /// <summary>
        /// Gets the Property collection, or the members that are actually properties.
        /// </summary>
        public PSMemberInfoCollection<PSPropertyInfo> Properties => _properties;

        /// <summary>
        /// Gets the Method collection, or the members that are actually methods.
        /// </summary>
        public PSMemberInfoCollection<PSMethodInfo> Methods => _methods;

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            if (_constructorPSObject == null)
            {
                PSMemberSet memberSet = new PSMemberSet(name);
                foreach (PSMemberInfo member in this.Members)
                {
                    memberSet.Members.Add(member);
                }

                CloneBaseProperties(memberSet);
                return memberSet;
            }
            else
            {
                return new PSMemberSet(name, _constructorPSObject);
            }
        }

        /// <summary>
        /// Gets the member type. For PSMemberSet the member type is PSMemberTypes.MemberSet.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.MemberSet;

        /// <summary>
        /// Gets the value of this member. The getter returns the PSMemberSet itself.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">When trying to set the property.</exception>
        public override object Value
        {
            get => this;
            set => throw new ExtendedTypeSystemException("CannotChangePSMemberSetValue", null,
                ExtendedTypeSystem.CannotSetValueForMemberType, this.GetType().FullName);
        }

        /// <summary>
        /// Gets the type of the value for this member. This returns typeof(PSMemberSet).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(PSMemberSet).FullName;

        #endregion virtual implementation
    }

    /// <summary>
    /// This MemberSet is used internally to represent the memberset for properties
    /// PSObject, PSBase, PSAdapted members of a PSObject. Having a specialized
    /// memberset enables delay loading the members for these members. This saves
    /// time loading the members of a PSObject.
    /// </summary>
    /// <remarks>
    /// This is added to improve hosting PowerShell's PSObjects in a ASP.Net GridView
    /// Control
    /// </remarks>
    internal sealed class PSInternalMemberSet : PSMemberSet
    {
        private readonly object _syncObject = new object();
        private readonly PSObject _psObject;

        #region Constructor

        /// <summary>
        /// Constructs the specialized member set.
        /// </summary>
        /// <param name="propertyName">
        /// Should be one of PSObject, PSBase, PSAdapted
        /// </param>
        /// <param name="psObject">
        /// original PSObject to use to generate members
        /// </param>
        internal PSInternalMemberSet(string propertyName, PSObject psObject)
            : base(propertyName)
        {
            this.internalMembers = null;
            _psObject = psObject;
        }

        #endregion

        #region virtual overrides

        /// <summary>
        /// Generates the members when needed.
        /// </summary>
        internal override PSMemberInfoInternalCollection<PSMemberInfo> InternalMembers
        {
            get
            {
                // do not cache "psadapted"
                if (name.Equals(PSObject.AdaptedMemberSetName, StringComparison.OrdinalIgnoreCase))
                {
                    return GetInternalMembersFromAdapted();
                }

                // cache "psbase" and "psobject"
                if (internalMembers == null)
                {
                    lock (_syncObject)
                    {
                        if (internalMembers == null)
                        {
                            internalMembers = new PSMemberInfoInternalCollection<PSMemberInfo>();

                            switch (name.ToLowerInvariant())
                            {
                                case PSObject.BaseObjectMemberSetName:
                                    GenerateInternalMembersFromBase();
                                    break;
                                case PSObject.PSObjectMemberSetName:
                                    GenerateInternalMembersFromPSObject();
                                    break;
                                default:
                                    Diagnostics.Assert(false,
                                        string.Create(CultureInfo.InvariantCulture, $"PSInternalMemberSet cannot process {name}"));
                                    break;
                            }
                        }
                    }
                }

                return internalMembers;
            }
        }

        #endregion

        #region Private Methods

        private void GenerateInternalMembersFromBase()
        {
            if (_psObject.IsDeserialized)
            {
                if (_psObject.ClrMembers != null)
                {
                    foreach (PSMemberInfo member in _psObject.ClrMembers)
                    {
                        internalMembers.Add(member.Copy());
                    }
                }
            }
            else
            {
                foreach (PSMemberInfo member in
                    PSObject.DotNetInstanceAdapter.BaseGetMembers<PSMemberInfo>(_psObject.ImmediateBaseObject))
                {
                    internalMembers.Add(member.Copy());
                }
            }
        }

        private PSMemberInfoInternalCollection<PSMemberInfo> GetInternalMembersFromAdapted()
        {
            PSMemberInfoInternalCollection<PSMemberInfo> retVal = new PSMemberInfoInternalCollection<PSMemberInfo>();

            if (_psObject.IsDeserialized)
            {
                if (_psObject.AdaptedMembers != null)
                {
                    foreach (PSMemberInfo member in _psObject.AdaptedMembers)
                    {
                        retVal.Add(member.Copy());
                    }
                }
            }
            else
            {
                foreach (PSMemberInfo member in _psObject.InternalAdapter.BaseGetMembers<PSMemberInfo>(
                    _psObject.ImmediateBaseObject))
                {
                    retVal.Add(member.Copy());
                }
            }

            return retVal;
        }

        private void GenerateInternalMembersFromPSObject()
        {
            PSMemberInfoCollection<PSMemberInfo> members = PSObject.DotNetInstanceAdapter.BaseGetMembers<PSMemberInfo>(
                _psObject);
            foreach (PSMemberInfo member in members)
            {
                internalMembers.Add(member.Copy());
            }
        }

        #endregion
    }

    /// <summary>
    /// Serves as a list of property names.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSPropertySet"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSPropertySet : PSMemberInfo
    {
        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.Append(this.Name);
            returnValue.Append(" {");
            if (ReferencedPropertyNames.Count != 0)
            {
                foreach (string property in ReferencedPropertyNames)
                {
                    returnValue.Append(property);
                    returnValue.Append(", ");
                }

                returnValue.Remove(returnValue.Length - 2, 2);
            }

            returnValue.Append('}');
            return returnValue.ToString();
        }

        /// <summary>
        /// Initializes a new instance of PSPropertySet with a name and list of property names.
        /// </summary>
        /// <param name="name">Name of the set.</param>
        /// <param name="referencedPropertyNames">Name of the properties in the set.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public PSPropertySet(string name, IEnumerable<string> referencedPropertyNames)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            this.name = name;
            if (referencedPropertyNames == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(referencedPropertyNames));
            }

            ReferencedPropertyNames = new Collection<string>();
            foreach (string referencedPropertyName in referencedPropertyNames)
            {
                if (string.IsNullOrEmpty(referencedPropertyName))
                {
                    throw PSTraceSource.NewArgumentException(nameof(referencedPropertyNames));
                }

                ReferencedPropertyNames.Add(referencedPropertyName);
            }
        }

        /// <summary>
        /// Initializes a new instance of PSPropertySet with a name and list of property names.
        /// This constructor is supposed to be used in TypeTable to reuse the passed-in property name list.
        /// Null-argument check is skipped here, so callers need to check arguments before passing in.
        /// </summary>
        /// <param name="name">Name of the set.</param>
        /// <param name="referencedPropertyNameList">Name of the properties in the set.</param>
        internal PSPropertySet(string name, List<string> referencedPropertyNameList)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(name), "Caller needs to guarantee not null or empty.");
            Diagnostics.Assert(referencedPropertyNameList != null, "Caller needs to guarantee not null.");

            // We use the constructor 'public Collection(IList<T> list)' to create the collection,
            // so that the passed-in list is directly used as the backing store of the collection.
            this.name = name;
            ReferencedPropertyNames = new Collection<string>(referencedPropertyNameList);
        }

        /// <summary>
        /// Gets the property names in this property set.
        /// </summary>
        public Collection<string> ReferencedPropertyNames { get; }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSPropertySet member = new PSPropertySet(name, ReferencedPropertyNames);
            CloneBaseProperties(member);
            return member;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.PropertySet;

        /// <summary>
        /// Gets the PSPropertySet itself.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">When setting the member.</exception>
        public override object Value
        {
            get => this;
            set => throw new ExtendedTypeSystemException("CannotChangePSPropertySetValue", null,
                ExtendedTypeSystem.CannotSetValueForMemberType, this.GetType().FullName);
        }

        /// <summary>
        /// Gets the type of the value for this member. This returns typeof(PSPropertySet).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(PSPropertySet).FullName;

        #endregion virtual implementation
    }

    /// <summary>
    /// Used to access the adapted or base events from the BaseObject.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSMethod"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class PSEvent : PSMemberInfo
    {
        /// <summary>
        /// Returns the string representation of this member.
        /// </summary>
        /// <returns>This property as a string.</returns>
        public override string ToString()
        {
            StringBuilder eventDefinition = new StringBuilder();
            eventDefinition.Append(this.baseEvent.ToString());

            eventDefinition.Append('(');

            int loopCounter = 0;
            foreach (ParameterInfo parameter in baseEvent.EventHandlerType.GetMethod("Invoke").GetParameters())
            {
                if (loopCounter > 0)
                    eventDefinition.Append(", ");

                eventDefinition.Append(parameter.ParameterType.ToString());

                loopCounter++;
            }

            eventDefinition.Append(')');

            return eventDefinition.ToString();
        }

        internal EventInfo baseEvent;

        /// <summary>
        /// Constructs this event.
        /// </summary>
        /// <param name="baseEvent">The actual event.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal PSEvent(EventInfo baseEvent)
        {
            this.baseEvent = baseEvent;
            this.name = baseEvent.Name;
        }

        #region virtual implementation

        /// <summary>
        /// Returns a new PSMemberInfo that is a copy of this PSMemberInfo.
        /// </summary>
        /// <returns>A new PSMemberInfo that is a copy of this PSMemberInfo.</returns>
        public override PSMemberInfo Copy()
        {
            PSEvent member = new PSEvent(this.baseEvent);
            CloneBaseProperties(member);
            return member;
        }

        /// <summary>
        /// Gets the member type.
        /// </summary>
        public override PSMemberTypes MemberType => PSMemberTypes.Event;

        /// <summary>
        /// Gets the value of this member. The getter returns the
        /// actual .NET event that this type wraps.
        /// </summary>
        /// <exception cref="ExtendedTypeSystemException">When setting the member.</exception>
        public sealed override object Value
        {
            get => baseEvent;
            set => throw new ExtendedTypeSystemException("CannotChangePSEventInfoValue", null,
                ExtendedTypeSystem.CannotSetValueForMemberType, this.GetType().FullName);
        }

        /// <summary>
        /// Gets the type of the value for this member. This always returns typeof(PSMethod).FullName.
        /// </summary>
        public override string TypeNameOfValue => typeof(PSEvent).FullName;

        #endregion virtual implementation
    }

    /// <summary>
    /// A dynamic member.
    /// </summary>
    public class PSDynamicMember : PSMemberInfo
    {
        internal PSDynamicMember(string name)
        {
            this.name = name;
        }

        /// <summary/>
        public override string ToString()
        {
            return "dynamic " + Name;
        }

        /// <summary/>
        public override PSMemberTypes MemberType => PSMemberTypes.Dynamic;

        /// <summary/>
        public override object Value
        {
            get => throw PSTraceSource.NewInvalidOperationException();
            set => throw PSTraceSource.NewInvalidOperationException();
        }

        /// <summary/>
        public override string TypeNameOfValue => "dynamic";

        /// <summary/>
        public override PSMemberInfo Copy()
        {
            return new PSDynamicMember(Name);
        }
    }

    #endregion PSMemberInfo

    #region Member collection classes and its auxiliary classes

    /// <summary>
    /// /// This class is used in PSMemberInfoInternalCollection and ReadOnlyPSMemberInfoCollection.
    /// </summary>
    internal static class MemberMatch
    {
        internal static WildcardPattern GetNamePattern(string name)
        {
            if (name != null && WildcardPattern.ContainsWildcardCharacters(name))
            {
                return WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
            }

            return null;
        }

        /// <summary>
        /// Returns all members in memberList matching name and memberTypes.
        /// </summary>
        /// <param name="memberList">Members to look for member with the correct types and name.</param>
        /// <param name="name">Name of the members to look for. The name might contain globbing characters.</param>
        /// <param name="nameMatch">WildcardPattern out of name.</param>
        /// <param name="memberTypes">Type of members we want to retrieve.</param>
        /// <returns>A collection of members of the right types and name extracted from memberList.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal static PSMemberInfoInternalCollection<T> Match<T>(PSMemberInfoInternalCollection<T> memberList, string name, WildcardPattern nameMatch, PSMemberTypes memberTypes)
            where T : PSMemberInfo
        {
            PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();
            if (memberList == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(memberList));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (nameMatch == null)
            {
                T member = memberList[name];
                if (member != null && (member.MemberType & memberTypes) != 0)
                {
                    returnValue.Add(member);
                }

                return returnValue;
            }

            foreach (T member in memberList)
            {
                if (nameMatch.IsMatch(member.Name) && ((member.MemberType & memberTypes) != 0))
                {
                    returnValue.Add(member);
                }
            }

            return returnValue;
        }
    }

    /// <summary>
    /// A Predicate that determine if a member name matches a criterion.
    /// </summary>
    /// <param name="memberName"></param>
    /// <returns><see langword="true"/> if the <paramref name="memberName"/> matches the predicate, otherwise <see langword="false"/>.</returns>
    public delegate bool MemberNamePredicate(string memberName);

    /// <summary>
    /// Serves as the collection of members in an PSObject or MemberSet.
    /// </summary>
    public abstract class PSMemberInfoCollection<T> : IEnumerable<T> where T : PSMemberInfo
    {
        #region ctor

        /// <summary>
        /// Initializes a new instance of an PSMemberInfoCollection derived class.
        /// </summary>
        protected PSMemberInfoCollection()
        {
        }

        #endregion ctor

        #region abstract

        /// <summary>
        /// Adds a member to this collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When:
        ///         adding a member to an PSMemberSet from the type configuration file or
        ///         adding a member with a reserved member name or
        ///         trying to add a member with a type not compatible with this collection or
        ///         a member by this name is already present
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract void Add(T member);

        /// <summary>
        /// Adds a member to this collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <param name="preValidated">flag to indicate that validation has already been done
        ///     on this new member.  Use only when you can guarantee that the input will not
        ///     cause any of the errors normally caught by this method.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When:
        ///         adding a member to an PSMemberSet from the type configuration file or
        ///         adding a member with a reserved member name or
        ///         trying to add a member with a type not compatible with this collection or
        ///         a member by this name is already present
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract void Add(T member, bool preValidated);

        /// <summary>
        /// Removes a member from this collection.
        /// </summary>
        /// <param name="name">Name of the member to be removed.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When:
        ///         removing a member from an PSMemberSet from the type configuration file
        ///         removing a member with a reserved member name or
        ///         trying to remove a member with a type not compatible with this collection
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract void Remove(string name);

        /// <summary>
        /// Gets the member in this collection matching name. If the member does not exist, null is returned.
        /// </summary>
        /// <param name="name">Name of the member to look for.</param>
        /// <returns>The member matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract T this[string name] { get; }

        #endregion abstract

        #region Match

        /// <summary>
        /// Returns all members in the collection matching name.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <returns>All members in the collection matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract ReadOnlyPSMemberInfoCollection<T> Match(string name);

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public abstract ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes);

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <param name="matchOptions">Match options.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal abstract ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes, MshMemberMatchOptions matchOptions);

        #endregion Match

        internal static bool IsReservedName(string name)
        {
            return (string.Equals(name, PSObject.BaseObjectMemberSetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, PSObject.AdaptedMemberSetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, PSObject.ExtendedMemberSetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, PSObject.PSObjectMemberSetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, PSObject.PSTypeNames, StringComparison.OrdinalIgnoreCase));
        }

        #region IEnumerable

        /// <summary>
        /// Gets the general enumerator for this collection.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the specific enumerator for this collection.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        public abstract IEnumerator<T> GetEnumerator();

        #endregion IEnumerable

        internal abstract T FirstOrDefault(MemberNamePredicate predicate);
    }

    /// <summary>
    /// Serves as a read only collection of members.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="ReadOnlyPSMemberInfoCollection&lt;T&gt;"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    public class ReadOnlyPSMemberInfoCollection<T> : IEnumerable<T> where T : PSMemberInfo
    {
        private readonly PSMemberInfoInternalCollection<T> _members;

        /// <summary>
        /// Initializes a new instance of ReadOnlyPSMemberInfoCollection with the given members.
        /// </summary>
        /// <param name="members"></param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal ReadOnlyPSMemberInfoCollection(PSMemberInfoInternalCollection<T> members)
        {
            if (members == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(members));
            }

            _members = members;
        }

        /// <summary>
        /// Return the member in this collection matching name. If the member does not exist, null is returned.
        /// </summary>
        /// <param name="name">Name of the member to look for.</param>
        /// <returns>The member matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public T this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw PSTraceSource.NewArgumentException(nameof(name));
                }

                return _members[name];
            }
        }

        /// <summary>
        /// Returns all members in the collection matching name.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <returns>All members in the collection matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public ReadOnlyPSMemberInfoCollection<T> Match(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return _members.Match(name);
        }

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return _members.Match(name, memberTypes);
        }

        /// <summary>
        /// Gets the general enumerator for this collection.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the specific enumerator for this collection.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        /// <summary>
        /// Gets the number of elements in this collection.
        /// </summary>
        public int Count => _members.Count;

        /// <summary>
        /// Returns the 0 based member identified by index.
        /// </summary>
        /// <param name="index">Index of the member to retrieve.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public T this[int index] => _members[index];
    }

    /// <summary>
    /// Collection of members.
    /// </summary>
    internal sealed class PSMemberInfoInternalCollection<T> : PSMemberInfoCollection<T>, IEnumerable<T> where T : PSMemberInfo
    {
        private OrderedDictionary _members;
        private int _countHidden;

        /// <summary>
        /// Gets the OrderedDictionary for holding all members.
        /// We use this property to delay initializing _members until we absolutely need to.
        /// </summary>
        private OrderedDictionary Members
        {
            get
            {
                if (_members == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _members, new OrderedDictionary(StringComparer.OrdinalIgnoreCase), null);
                }

                return _members;
            }
        }

        /// <summary>
        /// Constructs this collection.
        /// </summary>
        internal PSMemberInfoInternalCollection()
        {
        }

        /// <summary>
        /// Constructs this collection with an initial capacity.
        /// </summary>
        internal PSMemberInfoInternalCollection(int capacity)
        {
            _members = new OrderedDictionary(capacity, StringComparer.OrdinalIgnoreCase);
        }

        private void Replace(T oldMember, T newMember)
        {
            Members[newMember.Name] = newMember;
            if (oldMember.IsHidden)
            {
                _countHidden--;
            }

            if (newMember.IsHidden)
            {
                _countHidden++;
            }
        }

        /// <summary>
        /// Adds a member to the collection by replacing the one with the same name.
        /// </summary>
        /// <param name="newMember"></param>
        internal void Replace(T newMember)
        {
            Diagnostics.Assert(newMember != null, "called from internal code that checks for new member not null");

            // Save to a local variable to reduce property access.
            var members = Members;
            lock (members)
            {
                var oldMember = members[newMember.Name] as T;
                Diagnostics.Assert(oldMember != null, "internal code checks member already exists");
                Replace(oldMember, newMember);
            }
        }

        /// <summary>
        /// Adds a member to this collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <exception cref="ExtendedTypeSystemException">When a member by this name is already present.</exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Add(T member)
        {
            Add(member, false);
        }

        /// <summary>
        /// Adds a member to this collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <param name="preValidated">flag to indicate that validation has already been done
        ///     on this new member.  Use only when you can guarantee that the input will not
        ///     cause any of the errors normally caught by this method.</param>
        /// <exception cref="ExtendedTypeSystemException">When a member by this name is already present.</exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Add(T member, bool preValidated)
        {
            if (member == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(member));
            }

            // Save to a local variable to reduce property access.
            var members = Members;
            lock (members)
            {
                if (members[member.Name] is T existingMember)
                {
                    Replace(existingMember, member);
                }
                else
                {
                    members[member.Name] = member;
                    if (member.IsHidden)
                    {
                        _countHidden++;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a member from this collection.
        /// </summary>
        /// <param name="name">Name of the member to be removed.</param>
        /// <exception cref="ExtendedTypeSystemException">When removing a member with a reserved member name.</exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (IsReservedName(name))
            {
                throw new ExtendedTypeSystemException("PSMemberInfoInternalCollectionRemoveReservedName",
                    null,
                    ExtendedTypeSystem.ReservedMemberName,
                    name);
            }

            if (_members == null)
            {
                return;
            }

            lock (_members)
            {
                if (_members[name] is PSMemberInfo member)
                {
                    if (member.IsHidden)
                    {
                        _countHidden--;
                    }

                    _members.Remove(name);
                }
            }
        }

        /// <summary>
        /// Returns the member in this collection matching name.
        /// </summary>
        /// <param name="name">Name of the member to look for.</param>
        /// <returns>The member matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override T this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw PSTraceSource.NewArgumentException(nameof(name));
                }

                if (_members == null)
                {
                    return null;
                }

                lock (_members)
                {
                    return _members[name] as T;
                }
            }
        }

        /// <summary>
        /// Returns all members in the collection matching name.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <returns>All members in the collection matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override ReadOnlyPSMemberInfoCollection<T> Match(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return Match(name, PSMemberTypes.All, MshMemberMatchOptions.None);
        }

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return Match(name, memberTypes, MshMemberMatchOptions.None);
        }

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <param name="matchOptions">Match options.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal override ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes, MshMemberMatchOptions matchOptions)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            PSMemberInfoInternalCollection<T> internalMembers = GetInternalMembers(matchOptions);
            return new ReadOnlyPSMemberInfoCollection<T>(MemberMatch.Match(internalMembers, name, MemberMatch.GetNamePattern(name), memberTypes));
        }

        private PSMemberInfoInternalCollection<T> GetInternalMembers(MshMemberMatchOptions matchOptions)
        {
            PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();

            if (_members == null)
            {
                return returnValue;
            }

            lock (_members)
            {
                foreach (T member in _members.Values.OfType<T>())
                {
                    if (member.MatchesOptions(matchOptions))
                    {
                        returnValue.Add(member);
                    }
                }
            }

            return returnValue;
        }

        /// <summary>
        /// The number of elements in this collection.
        /// </summary>
        internal int Count
        {
            get
            {
                if (_members == null)
                {
                    return 0;
                }

                lock (_members)
                {
                    return _members.Count;
                }
            }
        }

        /// <summary>
        /// The number of elements in this collection not marked as Hidden.
        /// </summary>
        internal int VisibleCount
        {
            get
            {
                if (_members == null)
                {
                    return 0;
                }

                lock (_members)
                {
                    return _members.Count - _countHidden;
                }
            }
        }

        /// <summary>
        /// Returns the 0 based member identified by index.
        /// </summary>
        /// <param name="index">Index of the member to retrieve.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal T this[int index]
        {
            get
            {
                if (_members == null)
                {
                    return null;
                }

                lock (_members)
                {
                    return _members[index] as T;
                }
            }
        }

        /// <summary>
        /// Gets the specific enumerator for this collection.
        /// This virtual works around the difficulty of implementing
        /// interfaces virtually.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        public override IEnumerator<T> GetEnumerator()
        {
            if (_members == null)
            {
                return Enumerable.Empty<T>().GetEnumerator();
            }

            lock (_members)
            {
                // Copy the members to a list so that iteration can be performed without holding a lock.
                return _members.Values.OfType<T>().ToList().GetEnumerator();
            }
        }

        /// <summary>
        /// Returns the first member that matches the specified <see cref="MemberNamePredicate"/>.
        /// </summary>
        internal override T FirstOrDefault(MemberNamePredicate predicate)
        {
            lock (_members)
            {
                foreach (DictionaryEntry entry in _members)
                {
                    if (predicate((string)entry.Key))
                    {
                        return entry.Value as T;
                    }
                }
            }

            return null;
        }
    }

    #region CollectionEntry

    internal sealed class CollectionEntry<T> where T : PSMemberInfo
    {
        internal delegate PSMemberInfoInternalCollection<T> GetMembersDelegate(PSObject obj);

        internal delegate T GetMemberDelegate(PSObject obj, string name);

        internal delegate T GetFirstOrDefaultDelegate(PSObject obj, MemberNamePredicate predicate);

        internal CollectionEntry(
            GetMembersDelegate getMembers,
            GetMemberDelegate getMember,
            GetFirstOrDefaultDelegate getFirstOrDefault,
            bool shouldReplicateWhenReturning,
            bool shouldCloneWhenReturning,
            string collectionNameForTracing)
        {
            GetMembers = getMembers;
            GetMember = getMember;
            GetFirstOrDefault = getFirstOrDefault;
            _shouldReplicateWhenReturning = shouldReplicateWhenReturning;
            _shouldCloneWhenReturning = shouldCloneWhenReturning;
            CollectionNameForTracing = collectionNameForTracing;
        }

        internal GetMembersDelegate GetMembers { get; }

        internal GetMemberDelegate GetMember { get; }

        internal GetFirstOrDefaultDelegate GetFirstOrDefault { get; }

        internal string CollectionNameForTracing { get; }

        private readonly bool _shouldReplicateWhenReturning;

        private readonly bool _shouldCloneWhenReturning;

        internal T CloneOrReplicateObject(object owner, T member)
        {
            if (_shouldCloneWhenReturning)
            {
                member = (T)member.Copy();
            }

            if (_shouldReplicateWhenReturning)
            {
                member.ReplicateInstance(owner);
            }

            return member;
        }
    }

    #endregion CollectionEntry

    internal static class ReservedNameMembers
    {
        private static object GenerateMemberSet(string name, object obj)
        {
            PSObject mshOwner = PSObject.AsPSObject(obj);
            var memberSet = mshOwner.InstanceMembers[name];
            if (memberSet == null)
            {
                memberSet = new PSInternalMemberSet(name, mshOwner)
                {
                    ShouldSerialize = false,
                    IsHidden = true,
                    IsReservedMember = true
                };
                mshOwner.InstanceMembers.Add(memberSet);
                memberSet.instance = mshOwner;
            }

            return memberSet;
        }

        internal static object GeneratePSBaseMemberSet(object obj)
        {
            return GenerateMemberSet(PSObject.BaseObjectMemberSetName, obj);
        }

        internal static object GeneratePSAdaptedMemberSet(object obj)
        {
            return GenerateMemberSet(PSObject.AdaptedMemberSetName, obj);
        }

        internal static object GeneratePSObjectMemberSet(object obj)
        {
            return GenerateMemberSet(PSObject.PSObjectMemberSetName, obj);
        }

        internal static object GeneratePSExtendedMemberSet(object obj)
        {
            PSObject mshOwner = PSObject.AsPSObject(obj);
            var memberSet = mshOwner.InstanceMembers[PSObject.ExtendedMemberSetName];
            if (memberSet == null)
            {
                memberSet = new PSMemberSet(PSObject.ExtendedMemberSetName, mshOwner)
                {
                    ShouldSerialize = false,
                    IsHidden = true,
                    IsReservedMember = true
                };
                memberSet.ReplicateInstance(mshOwner);
                memberSet.instance = mshOwner;
                mshOwner.InstanceMembers.Add(memberSet);
            }

            return memberSet;
        }

        // This is the implementation of the PSTypeNames CodeProperty.
        public static Collection<string> PSTypeNames(PSObject o)
        {
            return o.TypeNames;
        }

        internal static void GeneratePSTypeNames(object obj)
        {
            PSObject mshOwner = PSObject.AsPSObject(obj);
            if (mshOwner.InstanceMembers[PSObject.PSTypeNames] != null)
            {
                // PSTypeNames member set is already generated..just return.
                return;
            }

            PSCodeProperty codeProperty = new PSCodeProperty(PSObject.PSTypeNames, CachedReflectionInfo.ReservedNameMembers_PSTypeNames)
            {
                ShouldSerialize = false,
                instance = mshOwner,
                IsHidden = true,
                IsReservedMember = true
            };
            mshOwner.InstanceMembers.Add(codeProperty);
        }
    }

    internal sealed class PSMemberInfoIntegratingCollection<T> : PSMemberInfoCollection<T>, IEnumerable<T> where T : PSMemberInfo
    {
        #region reserved names

        private void GenerateAllReservedMembers()
        {
            if (!_mshOwner.HasGeneratedReservedMembers)
            {
                _mshOwner.HasGeneratedReservedMembers = true;
                ReservedNameMembers.GeneratePSExtendedMemberSet(_mshOwner);
                ReservedNameMembers.GeneratePSBaseMemberSet(_mshOwner);
                ReservedNameMembers.GeneratePSObjectMemberSet(_mshOwner);
                ReservedNameMembers.GeneratePSAdaptedMemberSet(_mshOwner);
                ReservedNameMembers.GeneratePSTypeNames(_mshOwner);
            }
        }

        #endregion reserved names

        #region Constructor, fields and properties

        internal Collection<CollectionEntry<T>> Collections { get; }

        private readonly PSObject _mshOwner;
        private readonly PSMemberSet _memberSetOwner;

        internal PSMemberInfoIntegratingCollection(object owner, Collection<CollectionEntry<T>> collections)
        {
            if (owner == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(owner));
            }

            _mshOwner = owner as PSObject;
            _memberSetOwner = owner as PSMemberSet;
            if (_mshOwner == null && _memberSetOwner == null)
            {
                throw PSTraceSource.NewArgumentException(nameof(owner));
            }

            if (collections == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(collections));
            }

            Collections = collections;
        }

        #endregion Constructor, fields and properties

        #region overrides

        /// <summary>
        /// Adds member to the collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When
        ///         member is an PSProperty or PSMethod
        ///         adding a member to a MemberSet with a reserved name
        ///         adding a member with a reserved member name or
        ///         adding a member with a type not compatible with this collection
        ///         a member with this name already exists
        ///         trying to add a member to a static memberset
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Add(T member)
        {
            Add(member, false);
        }

        /// <summary>
        /// Adds member to the collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <param name="preValidated">flag to indicate that validation has already been done
        ///     on this new member.  Use only when you can guarantee that the input will not
        ///     cause any of the errors normally caught by this method.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When
        ///         member is an PSProperty or PSMethod
        ///         adding a member to a MemberSet with a reserved name
        ///         adding a member with a reserved member name or
        ///         adding a member with a type not compatible with this collection
        ///         a member with this name already exists
        ///         trying to add a member to a static memberset
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Add(T member, bool preValidated)
        {
            if (member == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(member));
            }

            if (!preValidated)
            {
                if (member.MemberType == PSMemberTypes.Property || member.MemberType == PSMemberTypes.Method)
                {
                    throw new ExtendedTypeSystemException(
                        "CannotAddMethodOrProperty",
                        null,
                        ExtendedTypeSystem.CannotAddPropertyOrMethod);
                }

                if (_memberSetOwner != null && _memberSetOwner.IsReservedMember)
                {
                    throw new ExtendedTypeSystemException("CannotAddToReservedNameMemberset",
                        null,
                        ExtendedTypeSystem.CannotChangeReservedMember,
                        _memberSetOwner.Name);
                }
            }

            AddToReservedMemberSet(member, preValidated);
        }

        /// <summary>
        /// Auxiliary to add members from types.xml.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="preValidated"></param>
        internal void AddToReservedMemberSet(T member, bool preValidated)
        {
            if (!preValidated)
            {
                if (_memberSetOwner != null && !_memberSetOwner.IsInstance)
                {
                    throw new ExtendedTypeSystemException("RemoveMemberFromStaticMemberSet",
                        null,
                        ExtendedTypeSystem.ChangeStaticMember,
                        member.Name);
                }
            }

            AddToTypesXmlCache(member, preValidated);
        }

        /// <summary>
        /// Adds member to the collection.
        /// </summary>
        /// <param name="member">Member to be added.</param>
        /// <param name="preValidated">flag to indicate that validation has already been done
        ///    on this new member.  Use only when you can guarantee that the input will not
        ///    cause any of the errors normally caught by this method.</param>
        /// <exception cref="ExtendedTypeSystemException">
        ///     When
        ///         adding a member with a reserved member name or
        ///         adding a member with a type not compatible with this collection
        ///         a member with this name already exists
        ///         trying to add a member to a static memberset
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal void AddToTypesXmlCache(T member, bool preValidated)
        {
            if (member == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(member));
            }

            if (!preValidated)
            {
                if (IsReservedName(member.Name))
                {
                    throw new ExtendedTypeSystemException("PSObjectMembersMembersAddReservedName",
                        null,
                        ExtendedTypeSystem.ReservedMemberName,
                        member.Name);
                }
            }

            PSMemberInfo memberToBeAdded = member.Copy();

            if (_mshOwner != null)
            {
                if (!preValidated)
                {
                    TypeTable typeTable = _mshOwner.GetTypeTable();
                    if (typeTable != null)
                    {
                        var typesXmlMembers = typeTable.GetMembers(_mshOwner.InternalTypeNames);
                        var typesXmlMember = typesXmlMembers[member.Name];
                        if (typesXmlMember is T)
                        {
                            throw new ExtendedTypeSystemException(
                                "AlreadyPresentInTypesXml",
                                null,
                                ExtendedTypeSystem.MemberAlreadyPresentFromTypesXml,
                                member.Name);
                        }
                    }
                }

                memberToBeAdded.ReplicateInstance(_mshOwner);
                _mshOwner.InstanceMembers.Add(memberToBeAdded, preValidated);

                // All member binders may need to invalidate dynamic sites, and now must generate
                // different binding restrictions (specifically, must check for an instance member
                // before considering the type table or adapted members.)
                PSGetMemberBinder.SetHasInstanceMember(memberToBeAdded.Name);
                PSVariableAssignmentBinder.NoteTypeHasInstanceMemberOrTypeName(PSObject.Base(_mshOwner).GetType());

                return;
            }

            _memberSetOwner.InternalMembers.Add(memberToBeAdded, preValidated);
        }

        /// <summary>
        /// Removes the member named name from the collection.
        /// </summary>
        /// <param name="name">Name of the member to be removed.</param>
        /// <exception cref="ExtendedTypeSystemException">
        /// When trying to remove a member with a type not compatible with this collection
        /// When trying to remove a member from a static memberset
        /// When trying to remove a member from a MemberSet with a reserved name
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override void Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            if (_mshOwner != null)
            {
                _mshOwner.InstanceMembers.Remove(name);
                return;
            }

            if (!_memberSetOwner.IsInstance)
            {
                throw new ExtendedTypeSystemException("AddMemberToStaticMemberSet",
                    null,
                    ExtendedTypeSystem.ChangeStaticMember,
                    name);
            }

            if (IsReservedName(_memberSetOwner.Name))
            {
                throw new ExtendedTypeSystemException("CannotRemoveFromReservedNameMemberset",
                    null,
                    ExtendedTypeSystem.CannotChangeReservedMember,
                    _memberSetOwner.Name);
            }

            _memberSetOwner.InternalMembers.Remove(name);
        }

        /// <summary>
        /// Method which checks if the <paramref name="name"/> is reserved and if so
        /// it will ensure that the particular reserved member is loaded into the
        /// objects member collection.
        ///
        /// Caller should ensure that name is not null or empty.
        /// </summary>
        /// <param name="name">
        /// Name of the member to check and load (if needed).
        /// </param>
        private void EnsureReservedMemberIsLoaded(string name)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(name),
                "Name cannot be null or empty");

            // Length >= psbase (shortest special member)
            if (name.Length >= 6 && (name[0] == 'p' || name[0] == 'P') && (name[1] == 's' || name[1] == 'S'))
            {
                switch (name.ToLowerInvariant())
                {
                    case PSObject.BaseObjectMemberSetName:
                        ReservedNameMembers.GeneratePSBaseMemberSet(_mshOwner);
                        break;
                    case PSObject.AdaptedMemberSetName:
                        ReservedNameMembers.GeneratePSAdaptedMemberSet(_mshOwner);
                        break;
                    case PSObject.ExtendedMemberSetName:
                        ReservedNameMembers.GeneratePSExtendedMemberSet(_mshOwner);
                        break;
                    case PSObject.PSObjectMemberSetName:
                        ReservedNameMembers.GeneratePSObjectMemberSet(_mshOwner);
                        break;
                    case PSObject.PSTypeNames:
                        ReservedNameMembers.GeneratePSTypeNames(_mshOwner);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Returns the name corresponding to name or null if it is not present.
        /// </summary>
        /// <param name="name">Name of the member to return.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override T this[string name]
        {
            get
            {
                using (PSObject.MemberResolution.TraceScope("Lookup"))
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        throw PSTraceSource.NewArgumentException(nameof(name));
                    }

                    PSMemberInfo member;
                    object delegateOwner;
                    if (_mshOwner != null)
                    {
                        // this will check if name is a reserved name like PSBase, PSTypeNames
                        // if it is a reserved name, ensures the value is loaded.
                        EnsureReservedMemberIsLoaded(name);
                        delegateOwner = _mshOwner;
                        PSMemberInfoInternalCollection<PSMemberInfo> instanceMembers;
                        if (PSObject.HasInstanceMembers(_mshOwner, out instanceMembers))
                        {
                            member = instanceMembers[name];
                            if (member is T memberAsT)
                            {
                                PSObject.MemberResolution.WriteLine("Found PSObject instance member: {0}.", name);
                                return memberAsT;
                            }
                        }
                    }
                    else
                    {
                        member = _memberSetOwner.InternalMembers[name];
                        delegateOwner = _memberSetOwner.instance;
                        if (member is T memberAsT)
                        {
                            // In membersets we cannot replicate the instance when adding
                            // since the memberset might not yet have an associated PSObject.
                            // We replicate the instance when returning the members of the memberset.
                            PSObject.MemberResolution.WriteLine("Found PSMemberSet member: {0}.", name);
                            member.ReplicateInstance(delegateOwner);
                            return memberAsT;
                        }
                    }

                    if (delegateOwner == null)
                        return null;

                    delegateOwner = PSObject.AsPSObject(delegateOwner);
                    foreach (CollectionEntry<T> collection in Collections)
                    {
                        Diagnostics.Assert(delegateOwner != null, "all integrating collections with non empty collections have an associated PSObject");
                        T memberAsT = collection.GetMember((PSObject)delegateOwner, name);
                        if (memberAsT != null)
                        {
                            return collection.CloneOrReplicateObject(delegateOwner, memberAsT);
                        }
                    }

                    return null;
                }
            }
        }

        private PSMemberInfoInternalCollection<T> GetIntegratedMembers(MshMemberMatchOptions matchOptions)
        {
            using (PSObject.MemberResolution.TraceScope("Generating the total list of members"))
            {
                PSMemberInfoInternalCollection<T> returnValue = new PSMemberInfoInternalCollection<T>();
                object delegateOwner;
                if (_mshOwner != null)
                {
                    delegateOwner = _mshOwner;
                    foreach (PSMemberInfo member in _mshOwner.InstanceMembers)
                    {
                        if (member.MatchesOptions(matchOptions))
                        {
                            if (member is T memberAsT)
                            {
                                returnValue.Add(memberAsT);
                            }
                        }
                    }
                }
                else
                {
                    delegateOwner = _memberSetOwner.instance;
                    foreach (PSMemberInfo member in _memberSetOwner.InternalMembers)
                    {
                        if (member.MatchesOptions(matchOptions))
                        {
                            if (member is T memberAsT)
                            {
                                member.ReplicateInstance(delegateOwner);
                                returnValue.Add(memberAsT);
                            }
                        }
                    }
                }

                if (delegateOwner == null)
                    return returnValue;

                delegateOwner = PSObject.AsPSObject(delegateOwner);
                foreach (CollectionEntry<T> collection in Collections)
                {
                    PSMemberInfoInternalCollection<T> members = collection.GetMembers((PSObject)delegateOwner);
                    foreach (T member in members)
                    {
                        PSMemberInfo previousMember = returnValue[member.Name];
                        if (previousMember != null)
                        {
                            PSObject.MemberResolution.WriteLine("Member \"{0}\" of type \"{1}\" has been ignored because a member with the same name and type \"{2}\" is already present.",
                                member.Name, member.MemberType, previousMember.MemberType);
                            continue;
                        }

                        if (!member.MatchesOptions(matchOptions))
                        {
                            PSObject.MemberResolution.WriteLine("Skipping hidden member \"{0}\".", member.Name);
                            continue;
                        }

                        T memberToAdd = collection.CloneOrReplicateObject(delegateOwner, member);
                        returnValue.Add(memberToAdd);
                    }
                }

                return returnValue;
            }
        }

        /// <summary>
        /// Returns all members in the collection matching name.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <returns>All members in the collection matching name.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override ReadOnlyPSMemberInfoCollection<T> Match(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return Match(name, PSMemberTypes.All, MshMemberMatchOptions.None);
        }

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public override ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            return Match(name, memberTypes, MshMemberMatchOptions.None);
        }

        /// <summary>
        /// Returns all members in the collection matching name and types.
        /// </summary>
        /// <param name="name">Name of the members to be return. May contain wildcard characters.</param>
        /// <param name="memberTypes">Type of the members to be searched.</param>
        /// <param name="matchOptions">Search options.</param>
        /// <returns>All members in the collection matching name and types.</returns>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal override ReadOnlyPSMemberInfoCollection<T> Match(string name, PSMemberTypes memberTypes, MshMemberMatchOptions matchOptions)
        {
            using (PSObject.MemberResolution.TraceScope("Matching \"{0}\"", name))
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw PSTraceSource.NewArgumentException(nameof(name));
                }

                if (_mshOwner != null)
                {
                    GenerateAllReservedMembers();
                }

                WildcardPattern nameMatch = MemberMatch.GetNamePattern(name);
                PSMemberInfoInternalCollection<T> allMembers = GetIntegratedMembers(matchOptions);
                ReadOnlyPSMemberInfoCollection<T> returnValue = new ReadOnlyPSMemberInfoCollection<T>(MemberMatch.Match(allMembers, name, nameMatch, memberTypes));
                PSObject.MemberResolution.WriteLine("{0} total matches.", returnValue.Count);
                return returnValue;
            }
        }

        /// <summary>
        /// Gets the specific enumerator for this collection.
        /// This virtual works around the difficulty of implementing
        /// interfaces virtually.
        /// </summary>
        /// <returns>The enumerator for this collection.</returns>
        public override IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        internal override T FirstOrDefault(MemberNamePredicate predicate)
        {
            object delegateOwner;
            if (_mshOwner != null)
            {
                delegateOwner = _mshOwner;
                foreach (PSMemberInfo member in _mshOwner.InstanceMembers)
                {
                    if (member is T memberAsT && predicate(memberAsT.Name))
                    {
                        return memberAsT;
                    }
                }
            }
            else
            {
                delegateOwner = _memberSetOwner.instance;
                foreach (PSMemberInfo member in _memberSetOwner.InternalMembers)
                {
                    if (member is T memberAsT && predicate(memberAsT.Name))
                    {
                        memberAsT.ReplicateInstance(delegateOwner);
                        return memberAsT;
                    }
                }
            }

            if (delegateOwner == null)
            {
                return null;
            }

            var ownerAsPSObj = PSObject.AsPSObject(delegateOwner);
            for (int i = 0; i < Collections.Count; i++)
            {
                var collectionEntry = Collections[i];
                var member = collectionEntry.GetFirstOrDefault(ownerAsPSObj, predicate);
                if (member != null)
                {
                    return collectionEntry.CloneOrReplicateObject(ownerAsPSObj, member);
                }
            }

            return null;
        }

        #endregion overrides

        /// <summary>
        /// Enumerable for this class.
        /// </summary>
        internal struct Enumerator : IEnumerator<T>
        {
            private T _current;
            private int _currentIndex;
            private readonly PSMemberInfoInternalCollection<T> _allMembers;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> class to enumerate over members.
            /// </summary>
            /// <param name="integratingCollection">Members we are enumerating.</param>
            internal Enumerator(PSMemberInfoIntegratingCollection<T> integratingCollection)
            {
                using (PSObject.MemberResolution.TraceScope("Enumeration Start"))
                {
                    _currentIndex = -1;
                    _current = null;
                    _allMembers = integratingCollection.GetIntegratedMembers(MshMemberMatchOptions.None);
                    if (integratingCollection._mshOwner != null)
                    {
                        integratingCollection.GenerateAllReservedMembers();
                        PSObject.MemberResolution.WriteLine("Enumerating PSObject with type \"{0}\".", integratingCollection._mshOwner.ImmediateBaseObject.GetType().FullName);
                        PSObject.MemberResolution.WriteLine("PSObject instance members: {0}", _allMembers.VisibleCount);
                    }
                    else
                    {
                        PSObject.MemberResolution.WriteLine("Enumerating PSMemberSet \"{0}\".", integratingCollection._memberSetOwner.Name);
                        PSObject.MemberResolution.WriteLine("MemberSet instance members: {0}", _allMembers.VisibleCount);
                    }
                }
            }

            /// <summary>
            /// Moves to the next element in the enumeration.
            /// </summary>
            /// <returns>
            /// If there are no more elements to enumerate, returns false.
            /// Returns true otherwise.
            /// </returns>
            public bool MoveNext()
            {
                _currentIndex++;

                T member = null;
                while (_currentIndex < _allMembers.Count)
                {
                    member = _allMembers[_currentIndex];
                    if (!member.IsHidden)
                    {
                        break;
                    }

                    _currentIndex++;
                }

                if (_currentIndex < _allMembers.Count)
                {
                    _current = member;
                    return true;
                }

                _current = null;
                return false;
            }

            /// <summary>
            /// Gets the current PSMemberInfo in the enumeration.
            /// </summary>
            /// <exception cref="ArgumentException">For invalid arguments.</exception>
            T IEnumerator<T>.Current
            {
                get
                {
                    if (_currentIndex == -1)
                    {
                        throw PSTraceSource.NewInvalidOperationException();
                    }

                    return _current;
                }
            }

            object IEnumerator.Current => ((IEnumerator<T>)this).Current;

            void IEnumerator.Reset()
            {
                _currentIndex = -1;
                _current = null;
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            public void Dispose()
            {
            }
        }
    }

    #endregion Member collection classes and its auxiliary classes
}

#pragma warning restore 56503
