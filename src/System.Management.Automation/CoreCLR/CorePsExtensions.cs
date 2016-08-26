#if CORECLR
/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace System.Management.Automation
{
    #region Reflection and Type related extensions

    #region Enums

    [Flags]
    internal enum MemberTypes
    {
        Constructor = 0x01,
        Event = 0x02,
        Field = 0x04,
        Method = 0x08,
        Property = 0x10,
        All = 0xbf,
    }

    #endregion Enums

    /// <summary>
    /// The type extension methods within this partial class are only used for CoreCLR powershell.
    /// 
    /// * If you want to add an extension method that will be used by both FullCLR and CoreCLR powershell, please 
    ///   add it to the 'PSTypeExtensions' partial class in 'ExtensionMethods.cs'.
    /// * If you want to add an extension method that will be used only by CoreCLR powershell, please add it here.
    /// </summary>
    internal static partial class PSTypeExtensions
    {
        #region Miscs

        internal static bool IsSubclassOf(this Type targetType, Type type)
        {
            return targetType.GetTypeInfo().IsSubclassOf(type);
        }

        #endregion Miscs

        #region Interface

        internal static Type GetInterface(this Type type, string name)
        {
            // The search is case-sensitive, as it's in the full CLR version
            return GetInterface(type, name, false);
        }

        internal static Type GetInterface(this Type type, string name, bool ignoreCase)
        {
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(
                       implementedInterface => String.Equals(name, implementedInterface.Name, stringComparison));
        }

        #endregion Interface

        #region Member

        internal static MemberInfo[] GetMember(this Type type, string name, MemberTypes memberType, BindingFlags bindingAttr)
        {
            if (bindingAttr == 0)
            {
                return new MemberInfo[0];
            }

            var members = new List<MemberInfo>();
            if ((memberType & MemberTypes.Field) != 0)
            {
                var fields = type.GetFields(name, bindingAttr);
                if (fields != null)
                {
                    for (int i = 0; i < fields.Length; i++)
                    {
                        members.Add(fields[i]);
                    }
                }
            }

            if ((memberType & MemberTypes.Property) != 0)
            {
                var properties = type.GetProperties(name, bindingAttr);
                if (properties != null)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        members.Add(properties[i]);
                    }
                }
            }

            return members.ToArray();
        }

        #endregion Member

        #region Field

        internal static FieldInfo[] GetFields(this Type type, string name, BindingFlags bindingFlags)
        {
            return GetFields(type, name, bindingFlags, false);
        }

        /// <summary>
        /// GetFields
        /// </summary>
        internal static FieldInfo[] GetFields(this Type type, string name, BindingFlags bindingFlags, bool isNameNull)
        {
            if (!isNameNull && (name == null || (name = name.Trim()) == ""))
            {
                throw new PSArgumentNullException("name");
            }

            if (bindingFlags == 0)
            {
                return null;
            }

            Type currentType = type;
            StringComparison strCompare = (bindingFlags & BindingFlags.IgnoreCase) != 0
                                              ? StringComparison.OrdinalIgnoreCase
                                              : StringComparison.Ordinal;
            var fields = new List<FieldInfo>();
            bool isInHierarchy = false;

            do
            {
                TypeInfo currentTypeInfo = currentType.GetTypeInfo();

                foreach (FieldInfo field in currentTypeInfo.DeclaredFields)
                {
                    if (!isNameNull && !String.Equals(name, field.Name, strCompare))
                    {
                        continue;
                    }

                    if (((bindingFlags & BindingFlags.Instance) != 0 && !field.IsStatic) ||
                        ((bindingFlags & BindingFlags.Static) != 0 && field.IsStatic))
                    {
                        if (isInHierarchy)
                        {
                            // Specify BindingFlags.FlattenHierarchy to include public and protected static members up the hierarchy; 
                            // private static members in inherited classes are not included
                            if (!field.IsPublic && !(field.IsFamily && field.IsStatic))
                            {
                                continue;
                            }
                        }

                        if ((bindingFlags & BindingFlags.Public) != 0 && field.IsPublic)
                        {
                            fields.Add(field);
                            continue;
                        }

                        if ((bindingFlags & BindingFlags.NonPublic) != 0 && !field.IsPublic)
                        {
                            fields.Add(field);
                        }
                    }
                }

                if ((bindingFlags & BindingFlags.FlattenHierarchy) != 0 && (bindingFlags & BindingFlags.DeclaredOnly) == 0)
                {
                    isInHierarchy = true;
                    currentType = currentTypeInfo.BaseType;
                }
                else
                {
                    currentType = null;
                }

            } while (currentType != null);

            return fields.ToArray();
        }

        #endregion Field

        #region Property

        internal static PropertyInfo[] GetProperties(this Type type, string name, BindingFlags bindingFlags)
        {
            return GetProperties(type, name, bindingFlags, false);
        }

        /// <summary>
        /// GetProperty
        /// </summary>
        internal static PropertyInfo[] GetProperties(this Type type, string name, BindingFlags bindingFlags, bool isNameNull)
        {
            if (!isNameNull && (name == null || (name = name.Trim()) == ""))
            {
                throw new PSArgumentNullException("name");
            }

            if (bindingFlags == 0)
            {
                return null;
            }

            Type currentType = type;
            StringComparison strCompare = (bindingFlags & BindingFlags.IgnoreCase) != 0
                                              ? StringComparison.OrdinalIgnoreCase
                                              : StringComparison.Ordinal;
            var properties = new List<PropertyInfo>();
            bool isInHierarchy = false;

            do
            {
                TypeInfo currentTypeInfo = currentType.GetTypeInfo();

                foreach (PropertyInfo property in currentTypeInfo.DeclaredProperties)
                {
                    if (!isNameNull && !String.Equals(name, property.Name, strCompare))
                    {
                        continue;
                    }

                    if (((bindingFlags & BindingFlags.Instance) != 0 && IsInstanceProperty(property)) ||
                        ((bindingFlags & BindingFlags.Static) != 0 && IsStaticProperty(property)))
                    {
                        if (isInHierarchy)
                        {
                            // Specify BindingFlags.FlattenHierarchy to include public and protected static members up the hierarchy; 
                            // private static members in inherited classes are not included
                            if (!IsPublicProperty(property) && !(IsProtectedProperty(property) && IsStaticProperty(property)))
                            {
                                continue;
                            }
                        }

                        if ((bindingFlags & BindingFlags.Public) != 0 && IsPublicProperty(property))
                        {
                            properties.Add(property);
                            continue;
                        }

                        if ((bindingFlags & BindingFlags.NonPublic) != 0 && IsNonPublicProperty(property))
                        {
                            properties.Add(property);
                            continue;
                        }
                    }
                }

                if ((bindingFlags & BindingFlags.FlattenHierarchy) != 0 && (bindingFlags & BindingFlags.DeclaredOnly) == 0)
                {
                    isInHierarchy = true;
                    currentType = currentTypeInfo.BaseType;
                }
                else
                {
                    currentType = null;
                }

            } while (currentType != null);

            return properties.ToArray();
        }

        #region Property Helper Methods

        private static bool IsInstanceProperty(PropertyInfo property)
        {
            if (property.GetMethod != null && property.GetMethod.IsStatic)
            {
                return false;
            }

            if (property.SetMethod != null && property.SetMethod.IsStatic)
            {
                return false;
            }

            if (property.GetMethod == null && property.SetMethod == null)
            {
                return false;
            }

            return true;
        }

        private static bool IsStaticProperty(PropertyInfo property)
        {
            if (property.GetMethod != null && !property.GetMethod.IsStatic)
            {
                return false;
            }

            if (property.SetMethod != null && !property.SetMethod.IsStatic)
            {
                return false;
            }

            if (property.GetMethod == null && property.SetMethod == null)
            {
                return false;
            }

            return true;
        }

        private static bool IsPublicProperty(PropertyInfo property)
        {
            if ((property.GetMethod != null && property.GetMethod.IsPublic) || (property.SetMethod != null && property.SetMethod.IsPublic))
            {
                return true;
            }

            return false;
        }

        private static bool IsNonPublicProperty(PropertyInfo property)
        {
            if (property.GetMethod == null && property.SetMethod == null)
            {
                return false;
            }

            if (property.GetMethod != null && property.GetMethod.IsPublic)
            {
                return false;
            }

            if (property.SetMethod != null && property.SetMethod.IsPublic)
            {
                return false;
            }

            return true;
        }

        private static bool IsProtectedProperty(PropertyInfo property)
        {
            if (property.GetMethod == null && property.SetMethod == null)
            {
                return false;
            }

            if (property.GetMethod != null && !property.GetMethod.IsFamily)
            {
                return false;
            }

            if (property.SetMethod != null && !property.SetMethod.IsFamily)
            {
                return false;
            }

            return true;
        }

        #endregion Property Helper Methods

        #endregion Property

        #region Constructor

        /// <summary>
        /// Search for a constructor that matches the specified bindingFlags and parameter types
        /// </summary>
        internal static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, string binderNotUsed, Type[] types, string modifiersNotUsed)
        {
            if (binderNotUsed != null || modifiersNotUsed != null)
            {
                throw new ArgumentException("Parameters 'binder' and 'modifier' should not be used");
            }

            if (types == null || types.Any(element => element == null))
            {
                throw new ArgumentNullException("types");
            }

            ConstructorInfo[] results = GetConstructors(type, bindingFlags, null);
            return results != null ? GetMatchingConstructor(results, types) : null;
        }

        internal static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, string binderNotUsed,
                                                       CallingConventions callConvention, Type[] types, string modifiersNotUsed)
        {
            if (binderNotUsed != null || modifiersNotUsed != null)
            {
                throw new ArgumentException("Parameters 'binder' and 'modifier' should not be used");
            }

            if (types == null || types.Any(element => element == null))
            {
                throw new ArgumentNullException("types");
            }

            ConstructorInfo[] results = GetConstructors(type, bindingFlags, callConvention);
            return results != null ? GetMatchingConstructor(results, types) : null;
        }

        /// <summary>
        /// Helper method - Get the matching constructor based on the parameter types
        /// </summary>
        private static ConstructorInfo GetMatchingConstructor(ConstructorInfo[] constructors, Type[] types)
        {
            // Compare the parameter types in two passes. 
            // The first pass is to check if the parameter types are exactly the same,
            // The second pass is to check if the parameter types is assignable from the given argument types.
            var matchConstructors = new List<ConstructorInfo>();
            bool inSecondPass = false;

            do
            {
                // Use 'for' loop to avoid construct new ArrayEnumerator object
                for (int constorIndex = 0; constorIndex < constructors.Length; constorIndex++)
                {
                    var constor = constructors[constorIndex];
                    ParameterInfo[] parameters = constor.GetParameters();
                    if (types.Length == parameters.Length)
                    {
                        bool success = true;
                        for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                        {
                            if (!IsParameterTypeMatching(parameters[typeIndex].ParameterType, types[typeIndex], inSecondPass))
                            {
                                success = false;
                                break;
                            }
                        }

                        if (success)
                        {
                            matchConstructors.Add(constor);
                        }
                    }
                }

                // Flip the 'inSecondPass' flag, so that we run another pass only if we're about 
                // to start the second pass and we didn't find anything from the first pass.
                inSecondPass = !inSecondPass;

            } while (matchConstructors.Count == 0 && inSecondPass);

            if (matchConstructors.Count > 1)
            {
                throw new AmbiguousMatchException();
            }

            return matchConstructors.Count == 1 ? matchConstructors[0] : null;
        }

        /// <summary>
        /// Constructors defined in the current type
        /// </summary>
        internal static ConstructorInfo[] GetConstructors(this Type type, BindingFlags bindingFlags, CallingConventions? callConvention)
        {
            if ((bindingFlags & BindingFlags.FlattenHierarchy) != 0)
            {
                throw new PSArgumentException("Invalid binding flags");
            }

            if (((bindingFlags & BindingFlags.Instance) != 0 && (bindingFlags & BindingFlags.Static) != 0) ||
                ((bindingFlags & BindingFlags.Instance) == 0 && (bindingFlags & BindingFlags.Static) == 0))
            {
                throw new PSArgumentException("Invalid binding flags");
            }

            // If bindingFlags is zero, return null. 
            if (bindingFlags == 0)
            {
                return null;
            }

            // If type is a generic parameter, return empty array
            if (type.IsGenericParameter)
            {
                return new ConstructorInfo[0];
            }

            var ctors = new List<ConstructorInfo>();

            foreach (ConstructorInfo ctor in type.GetTypeInfo().DeclaredConstructors)
            {
                /* CallingConventions is different on CoreCLR ...
                 * TODO -- find out how different and what problem it causes.
                if (callConvention.HasValue && ctor.CallingConvention != callConvention.Value)
                {
                    continue;
                }
                */

                if ((bindingFlags & BindingFlags.Instance) != 0 && ctor.IsStatic)
                {
                    continue;
                }

                if ((bindingFlags & BindingFlags.Static) != 0 && !ctor.IsStatic)
                {
                    continue;
                }

                if ((bindingFlags & BindingFlags.Public) != 0 && ctor.IsPublic)
                {
                    ctors.Add(ctor);
                    continue;
                }

                if ((bindingFlags & BindingFlags.NonPublic) != 0 && !ctor.IsPublic)
                {
                    ctors.Add(ctor);
                    continue;
                }
            }

            return ctors.ToArray();
        }

        #endregion Constructor

        #region Method

        internal static MethodInfo GetMethod(this Type targetType, string name, BindingFlags bindingFlags, string binderNotUsed, Type[] types, string modifierNotUsed)
        {
            if (binderNotUsed != null || modifierNotUsed != null)
            {
                throw new ArgumentException("Parameters 'binderNotUsed' and 'modifier_NotUsed' should not be used.");
            }

            if (types == null || types.Any(element => element == null))
            {
                throw new ArgumentNullException("types");
            }

            MethodInfo[] methods = GetMethods(targetType, name, bindingFlags);
            return methods != null ? GetMatchingMethod(methods, types) : null;
        }

        internal static MethodInfo GetMethod(this Type targetType, string name, BindingFlags bindingFlags, string binderNotUsed, CallingConventions callConvention, Type[] types, string modifierNotUsed)
        {
            if (binderNotUsed != null || modifierNotUsed != null)
            {
                throw new ArgumentException("Parameters 'binderNotUsed' and 'modifier_NotUsed' should not be used.");
            }

            if (types == null || types.Any(element => element == null))
            {
                throw new ArgumentNullException("types");
            }

            MethodInfo[] methods = GetMethods(targetType, name, bindingFlags, false, callConvention);
            return methods != null ? GetMatchingMethod(methods, types) : null;
        }

        // Helper method
        private static MethodInfo GetMatchingMethod(MethodInfo[] methods, Type[] types)
        {
            // Compare the parameter types in two passes. 
            // The first pass is to check if the parameter types are exactly the same,
            // The second pass is to check if the parameter types is assignable from the given argument types.
            var matchMethods = new List<MethodInfo>();
            bool inSecondPass = false;

            do
            {
                // Use for loop to avoid construct new ArrayEnumerator object
                for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    var method = methods[methodIndex];
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == types.Length)
                    {
                        bool success = true;
                        for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                        {
                            if (!IsParameterTypeMatching(parameters[typeIndex].ParameterType, types[typeIndex], inSecondPass))
                            {
                                success = false;
                                break;
                            }
                        }

                        if (success)
                        {
                            matchMethods.Add(method);
                        }
                    }
                }

                // Flip the 'inSecondPass' flag, so that we run another pass only if we're about 
                // to start the second pass and we didn't find anything from the first pass.
                inSecondPass = !inSecondPass;

            } while (matchMethods.Count == 0 && inSecondPass);

            if (matchMethods.Count > 1)
            {
                throw new AmbiguousMatchException();
            }

            return matchMethods.Count == 1 ? matchMethods[0] : null;
        }

        private static bool IsParameterTypeMatching(Type paramType, Type argType, bool inSecondPass)
        {
            return inSecondPass
                    ? paramType.IsAssignableFrom(argType)
                    : paramType == argType;
        }

        internal static MethodInfo[] GetMethods(this Type type, string name, BindingFlags bindingFlags)
        {
            return GetMethods(type, name, bindingFlags, false, null);
        }

        internal static MethodInfo[] GetMethods(this Type type, string name, BindingFlags bindingFlags, bool isNameNull, CallingConventions? callConvention)
        {
            if (!isNameNull && (name == null || (name = name.Trim()) == ""))
            {
                throw new ArgumentNullException("name");
            }

            if (bindingFlags == 0)
            {
                return null;
            }

            Type currentType = type;
            StringComparison strCompare = (bindingFlags & BindingFlags.IgnoreCase) != 0
                                            ? StringComparison.OrdinalIgnoreCase
                                            : StringComparison.Ordinal;
            var methods = new List<MethodInfo>();
            bool isInHierarchy = false;

            do
            {
                TypeInfo currentTypeInfo = currentType.GetTypeInfo();

                foreach (MethodInfo method in currentTypeInfo.DeclaredMethods)
                {
                    if (!isNameNull && !String.Equals(name, method.Name, strCompare))
                    {
                        continue;
                    }

                    if (callConvention.HasValue && method.CallingConvention != callConvention.Value)
                    {
                        continue;
                    }

                    if (((bindingFlags & BindingFlags.Instance) != 0 && !method.IsStatic) ||
                        ((bindingFlags & BindingFlags.Static) != 0 && method.IsStatic))
                    {
                        if (isInHierarchy)
                        {
                            // Specify BindingFlags.FlattenHierarchy to include public and protected static members up the hierarchy; 
                            // private static members in inherited classes are not included
                            if (!method.IsPublic && !(method.IsFamily && method.IsStatic))
                            {
                                continue;
                            }
                        }

                        if ((bindingFlags & BindingFlags.Public) != 0 && method.IsPublic)
                        {
                            methods.Add(method);
                            continue;
                        }

                        if ((bindingFlags & BindingFlags.NonPublic) != 0 && !method.IsPublic)
                        {
                            methods.Add(method);
                        }
                    }
                }

                if ((bindingFlags & BindingFlags.FlattenHierarchy) != 0 && (bindingFlags & BindingFlags.DeclaredOnly) == 0)
                {
                    isInHierarchy = true;
                    currentType = currentTypeInfo.BaseType;
                }
                else
                {
                    currentType = null;
                }

            } while (currentType != null);

            return methods.ToArray();
        }

        #endregion Method

        #region TypeCode

        private static readonly Dictionary<Type, TypeCode> s_typeCodeMap =
            new Dictionary<Type, TypeCode>()
                {
                    // 'DBNull = 2' is removed from 'System.TypeCode' in CoreCLR. We return
                    // '(TypeCode)2' for 'System.DBNull' to avoid any breaking changes.
                    {typeof(DBNull), (TypeCode)2},
                    {typeof(Boolean),TypeCode.Boolean},
                    {typeof(Char),TypeCode.Char},
                    {typeof(sbyte),TypeCode.SByte},
                    {typeof(byte), TypeCode.Byte},
                    {typeof(Int16),TypeCode.Int16},
                    {typeof(UInt16),TypeCode.UInt16},
                    {typeof(Int32),TypeCode.Int32},
                    {typeof(UInt32),TypeCode.UInt32},
                    {typeof(Int64),TypeCode.Int64},
                    {typeof(UInt64),TypeCode.UInt64},
                    {typeof(Single),TypeCode.Single},
                    {typeof(Double),TypeCode.Double},
                    {typeof(string),TypeCode.String},
                    {typeof(Decimal),TypeCode.Decimal},
                    {typeof(DateTime),TypeCode.DateTime},
                };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TypeCode GetTypeCodeInCoreClr(Type type)
        {
            if (type == null)
                return TypeCode.Empty;
            if (s_typeCodeMap.ContainsKey(type))
                return s_typeCodeMap[type];
            if (type.GetTypeInfo().IsEnum)
                return GetTypeCode(Enum.GetUnderlyingType(type));
            return TypeCode.Object;
        }

        #endregion TypeCode
    }

    #endregion Reflection and Type related extensions

    #region Environment Extensions

    // TODO:CORECLR - Environment Extensions need serious work to refine.
    internal enum EnvironmentVariableTarget
    {
        Process,
        User,
        Machine
    }

    internal static partial class Environment
    {
        #region Forward_To_System.Environment

        #region Properties

        public static int CurrentManagedThreadId
        {
            get
            {
                return System.Environment.CurrentManagedThreadId;
            }
        }

        public static bool HasShutdownStarted
        {
            get
            {
                return System.Environment.HasShutdownStarted;
            }
        }

        public static string NewLine
        {
            get
            {
                return System.Environment.NewLine;
            }
        }

        public static int ProcessorCount
        {
            get
            {
                return System.Environment.ProcessorCount;
            }
        }

        public static string StackTrace
        {
            get
            {
                return System.Environment.StackTrace;
            }
        }

        public static int TickCount
        {
            get
            {
                return System.Environment.TickCount;
            }
        }

        #endregion Properties

        #region Methods

        public static string ExpandEnvironmentVariables(string name)
        {
            return System.Environment.ExpandEnvironmentVariables(name);
        }

        public static void FailFast(string message)
        {
            System.Environment.FailFast(message);
        }

        public static void FailFast(string message, Exception exception)
        {
            System.Environment.FailFast(message, exception);
        }

        public static string GetEnvironmentVariable(string variable)
        {
            return System.Environment.GetEnvironmentVariable(variable);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            return System.Environment.GetEnvironmentVariables();
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            System.Environment.SetEnvironmentVariable(variable, value);
        }

        #endregion Methods

        #endregion Forward_To_System.Environment

        private const int MaxMachineNameLength = 256;
        private static string[] s_commandLineArgs = new string[0];

        public static string[] GetCommandLineArgs()
        {
            return s_commandLineArgs;
        }

        #region EnvironmentVariable_Extensions

        /// <summary>
        /// The code is mostly copied from the .NET implementation.
        /// The only difference is how resource string is retrieved.
        /// We use the same resource string as in .NET implementation.
        /// </summary>
        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                return GetEnvironmentVariables();
            }

#if UNIX
            return null;
#else
            if( target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey environmentKey =
                       Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Session Manager\Environment", false))
                {
                    return GetRegistryKeyNameValuePairs(environmentKey);
                }
            }
            else // target == EnvironmentVariableTarget.User 
            {
                using (RegistryKey environmentKey =
                       Registry.CurrentUser.OpenSubKey("Environment", false))
                {
                    return GetRegistryKeyNameValuePairs(environmentKey);
                }
            }
#endif
        }

        /// <summary>
        /// The code is mostly copied from the .NET implementation.
        /// The only difference is how resource string is retrieved.
        /// We use the same resource string as in .NET implementation.
        /// </summary>
        internal static IDictionary GetRegistryKeyNameValuePairs(RegistryKey registryKey)
        {
            Hashtable table = new Hashtable(20);

            if (registryKey != null)
            {
                string[] names = registryKey.GetValueNames();
                foreach (string name in names)
                {
                    string value = registryKey.GetValue(name, "").ToString();
                    table.Add(name, value);
                }
            }
            return table;
        }

        /// <summary>
        /// The code is mostly copied from the .NET implementation.
        /// The only difference is how resource string is retrieved.
        /// We use the same resource string as in .NET implementation.
        /// </summary>
        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }

            if (target == EnvironmentVariableTarget.Process)
            {
                return System.Environment.GetEnvironmentVariable(variable);
            }

#if UNIX
            return null;
#else
            if (target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey environmentKey =
                       Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Session Manager\Environment", false))
                {
                    if (environmentKey == null) { return null; }

                    string value = environmentKey.GetValue(variable) as string;
                    return value;
                }
            }
            else // target == EnvironmentVariableTarget.User
            {
                using (RegistryKey environmentKey = Registry.CurrentUser.OpenSubKey("Environment", false))
                {
                    if (environmentKey == null) { return null; }

                    string value = environmentKey.GetValue(variable) as string;
                    return value;
                }
            }
#endif
        }

        #endregion EnvironmentVariable_Extensions

        #region Property_Extensions

        /// <summary>
        /// UserDomainName
        /// </summary>
        public static string UserDomainName
        {
            get
            {
#if UNIX
                return Platform.NonWindowsGetDomainName();
#else
                return WinGetUserDomainName();
#endif
            }
        }

        /// <summary>
        /// UserName
        /// </summary>
        public static string UserName
        {
            get
            {
#if UNIX
                return Platform.Unix.UserName;
#else
                return WinGetUserName();
#endif
            }
        }

        /// <summary>
        /// MachineName
        /// </summary>
        public static string MachineName
        {
            get
            {
                return System.Environment.MachineName;
            }
        }

        /// <summary>
        /// OSVersion 
        /// </summary>
        public static OperatingSystem OSVersion
        {
            get
            {
                if (s_os == null)
                {
#if UNIX
                    // TODO:PSL use P/Invoke to provide proper version
                    // OSVersion will be back in CoreCLR 1.1
                    s_os = new Environment.OperatingSystem(new Version(1, 0, 0, 0), "");
#else
                    s_os = WinGetOSVersion();
#endif
                }
                return s_os;
            }
        }
        private static volatile OperatingSystem s_os;

        #endregion Property_Extensions

        #region SpecialFolder_Extensions

        /// <summary>
        /// The code is copied from the .NET implementation.
        /// </summary>
        public static string GetFolderPath(SpecialFolder folder)
        {
            return InternalGetFolderPath(folder);
        }

        /// <summary>
        /// The API set 'api-ms-win-shell-shellfolders-l1-1-0.dll' was removed from NanoServer, so we cannot depend on 'SHGetFolderPathW'
        /// to get the special folder paths. Instead, we need to rely on the basic environment variables to get the special folder paths.
        /// </summary>
        /// <returns>
        /// The path to the specified system special folder, if that folder physically exists on your computer.
        /// Otherwise, an empty string ("").
        /// </returns>
        private static string InternalGetFolderPath(SpecialFolder folder)
        {
            // The API 'SHGetFolderPath' is not available on OneCore, so we have to rely on environment variables
            string folderPath = null;

#if UNIX
            switch (folder)
            {
                case SpecialFolder.ProgramFiles:
                    folderPath = "/bin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderPath = "/usr/bin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case SpecialFolder.System:
                case SpecialFolder.SystemX86:
                    folderPath = "/sbin";
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case SpecialFolder.Personal:
                    folderPath = System.Environment.GetEnvironmentVariable("HOME");
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderPath = System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), ".config");
                    if (!System.IO.Directory.Exists(folderPath)) { System.IO.Directory.CreateDirectory(folderPath); }
                    break;
                default:
                    throw new NotSupportedException();
            }
#else
            string systemRoot = null;
            string userProfile = null;

            switch (folder)
            {
                case SpecialFolder.ProgramFiles:
                    folderPath = System.Environment.GetEnvironmentVariable("ProgramFiles");
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case SpecialFolder.ProgramFilesX86:
                    folderPath = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                    if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    break;
                case SpecialFolder.System:
                    systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
                    if (systemRoot != null)
                    {
                        folderPath = System.IO.Path.Combine(systemRoot, "system32");
                        if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    }
                    break;
                case SpecialFolder.SystemX86:
                    systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
                    if (systemRoot != null)
                    {
                        folderPath = System.IO.Path.Combine(systemRoot, "SysWOW64");
                        if (!System.IO.Directory.Exists(folderPath)) { folderPath = null; }
                    }
                    break;
                case SpecialFolder.MyDocuments: // same as SpecialFolder.Personal
                    userProfile = System.Environment.GetEnvironmentVariable("USERPROFILE");
                    if (userProfile != null)
                    {
                        folderPath = System.IO.Path.Combine(userProfile, "Documents");
                        // CSS doesn't include a Documents directory for each user, so we create one if needed.
                        if (!System.IO.Directory.Exists(folderPath)) { System.IO.Directory.CreateDirectory(folderPath); }
                    }
                    break;
                case SpecialFolder.LocalApplicationData:
                    folderPath = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
                    // When powershell gets executed in SetupComplete.cmd during NanoSrever's first boot, 'LOCALAPPDATA' won't be set yet.
                    // In this case, we need to return an alternate path, so that module auto-loading can continue to work properly.
                    if (folderPath == null)
                    {
                        // It's guaranteed by NanoServer team that 'USERPROFILE' will be already set when SetupComplete.cmd runs.
                        // So we use the path '%USERPROFILE%\AppData\Local' as an alternative in this case, and also set the env
                        // variable %LOCALAPPDATA% to it, so that modules running in PS can depend on this env variable.
                        userProfile = System.Environment.GetEnvironmentVariable("USERPROFILE");
                        if (userProfile != null)
                        {
                            string alternatePath = System.IO.Path.Combine(userProfile, @"AppData\Local");
                            if (System.IO.Directory.Exists(alternatePath))
                            {
                                System.Environment.SetEnvironmentVariable("LOCALAPPDATA", alternatePath);
                                folderPath = alternatePath;
                            }
                        }
                    }
                    else if (!System.IO.Directory.Exists(folderPath))
                    {
                        folderPath = null;
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
#endif

            return folderPath ?? string.Empty;
        }

        #endregion SpecialFolder_Extensions

        #region WinPlatform_Specific_Methods
#if !UNIX
        /// <summary>
        /// Windows UserDomainName implementation
        /// </summary>
        private static string WinGetUserDomainName()
        {
            StringBuilder domainName = new StringBuilder(1024);
            uint domainNameLen = (uint)domainName.Capacity;

            byte ret = Win32Native.GetUserNameEx(Win32Native.NameSamCompatible, domainName, ref domainNameLen);
            if (ret == 1)
            {
                string samName = domainName.ToString();
                int index = samName.IndexOf('\\');
                if (index != -1)
                {
                    return samName.Substring(0, index);
                }
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(Win32Native.GetMessage(errorCode));
            }

            // Cannot use LookupAccountNameW to get DomainName because 'GetUserName' is not available in CSS and thus we cannot get the account.
            throw new InvalidOperationException(CoreClrStubResources.CannotGetDomainName);
        }

        /// <summary>
        /// Windows UserName implementation
        /// </summary>
        private static string WinGetUserName()
        {
            StringBuilder domainName = new StringBuilder(1024);
            uint domainNameLen = (uint)domainName.Capacity;

            byte ret = Win32Native.GetUserNameEx(Win32Native.NameSamCompatible, domainName, ref domainNameLen);
            if (ret == 1)
            {
                string samName = domainName.ToString();
                int index = samName.IndexOf('\\');
                if (index != -1)
                {
                    return samName.Substring(index + 1);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Windows OSVersion implementation
        /// </summary>
        private static OperatingSystem WinGetOSVersion()
        {
            Win32Native.OSVERSIONINFOEX osviex = new Win32Native.OSVERSIONINFOEX();
            osviex.OSVersionInfoSize = Marshal.SizeOf(osviex);
            if (!Win32Native.GetVersionEx(ref osviex))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode);
            }

            Version v = new Version(osviex.MajorVersion, osviex.MinorVersion, osviex.BuildNumber, (osviex.ServicePackMajor << 16) | osviex.ServicePackMinor);
            return new OperatingSystem(v, osviex.CSDVersion);
        }

        /// <summary>
        /// DllImport uses the ApiSet dll that is available on CSS, since this code
        /// will only be included when building targeting CoreCLR.
        /// </summary>
        private static class Win32Native
        {
            internal const int NameSamCompatible = 2;             // EXTENDED_NAME_FORMAT - NameSamCompatible

            private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

            [DllImport("SspiCli.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            // Win32 return type is BOOLEAN (which is 1 byte and not BOOL which is 4bytes)
            internal static extern byte GetUserNameEx(int format, [Out] StringBuilder domainName, ref uint domainNameLen);

            [DllImport(PinvokeDllNames.FormatMessageDllName, CharSet = CharSet.Unicode)]
            internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId,
                                                     int dwLanguageId, [Out]StringBuilder lpBuffer,
                                                     int nSize, IntPtr va_list_arguments);

            [DllImport(PinvokeDllNames.GetVersionExDllName, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern bool GetVersionEx(ref OSVERSIONINFOEX osVerEx);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct OSVERSIONINFOEX
            {
                // The OSVersionInfoSize field must be set to Marshal.SizeOf(this)
                public int OSVersionInfoSize;
                public int MajorVersion;
                public int MinorVersion;
                public int BuildNumber;
                public int PlatformId;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string CSDVersion;
                public ushort ServicePackMajor;
                public ushort ServicePackMinor;
                public short SuiteMask;
                public byte ProductType;
                public byte Reserved;
            }

            /// <summary>
            /// The code is mostly copied from the .NET implementation.
            /// The only difference is how resource string is retrieved.
            /// We use the same resource string as in .NET implementation.
            /// </summary>
            internal static string GetMessage(int errorCode)
            {
                StringBuilder sb = new StringBuilder(512);
                int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                    FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                    IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
                if (result != 0)
                {
                    return sb.ToString();
                }
                else
                {
                    return string.Format(CultureInfo.CurrentCulture, CoreClrStubResources.UnknownErrorNumber, errorCode);
                }
            }
        }
#endif
        #endregion WinPlatform_Specific_Methods

        #region NestedTypes

        // Porting note: MyDocuments does not exist on .NET Core, but Personal does, and
        // they both point to your "documents repository," which on linux, is just the
        // home directory.

        /// <summary>
        /// It only contains the values that get used in powershell
        /// </summary>
        internal enum SpecialFolder
        {
            Personal = 0x05,
            MyDocuments = 0x05,
            LocalApplicationData = 0x1c,
            ProgramFiles = 0x26,
            ProgramFilesX86 = 0x2a,
            System = 0x25,
            SystemX86 = 0x29,
        }

        /// <summary>
        /// It only contains the properties that get used in powershell
        /// </summary>
        internal sealed class OperatingSystem
        {
            private Version _version;
            private string _servicePack;
            private string _versionString;

            internal OperatingSystem(Version version, string servicePack)
            {
                if (version == null)
                    throw new ArgumentNullException("version");

                _version = version;
                _servicePack = servicePack;
            }

            /// <summary>
            /// OS version
            /// </summary>
            public Version Version
            {
                get { return _version; }
            }

            /// <summary>
            /// VersionString
            /// </summary>
            public string VersionString
            {
                get
                {
                    if (_versionString != null)
                    {
                        return _versionString;
                    }

                    // It's always 'VER_PLATFORM_WIN32_NT' for NanoServer and IoT
                    const string os = "Microsoft Windows NT ";
                    if (string.IsNullOrEmpty(_servicePack))
                    {
                        _versionString = os + _version.ToString();
                    }
                    else
                    {
                        _versionString = os + _version.ToString(3) + " " + _servicePack;
                    }

                    return _versionString;
                }
            }
        }

        #endregion NestedTypes
    }

    #endregion Environment Extensions

    #region Non-generic collection extensions

    /// <summary>
    /// Add the AttributeCollection type with stripped functionalities for powershell on CoreCLR.
    /// The Adapter type has a protected abstract method 'PropertyAttributes' that returns an AttributeCollection
    /// instance. Third party adapter may already implement this method and thus we cannot change the return
    /// type for full powershell code. Therefore, we add the AttributeCollection type with minimal functionalities
    /// so that the code also work with CoreCLR.
    /// </summary>
    public class AttributeCollection : ICollection, IEnumerable
    {
        /// <summary>
        /// AttributeCollection
        /// </summary>
        public static readonly AttributeCollection Empty = new AttributeCollection(null);

        #region Constructors

        /// <summary>
        /// AttributeCollection protected constructor
        /// </summary>
        protected AttributeCollection()
        {
        }

        /// <summary>
        /// AttributeCollection public constructor
        /// </summary>
        /// <param name="attributes"></param>
        public AttributeCollection(params Attribute[] attributes)
        {
            if (attributes == null)
            {
                attributes = new Attribute[0];
            }

            _attributes = attributes;
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] == null)
                {
                    throw new ArgumentNullException("attributes");
                }
            }
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Copies the collection to an array, starting at the specified index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Array array, int index)
        {
            Array.Copy(this.Attributes, 0, array, index, this.Attributes.Length);
        }

        /// <summary>
        /// Gets an enumerator for this collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return this.Attributes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion Methods

        #region Properties

        private readonly Attribute[] _attributes;
        /// <summary>
        /// Gets the attribute collection.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        protected virtual Attribute[] Attributes
        {
            get
            {
                return _attributes;
            }
        }

        /// <summary>
        /// Gets the number of attributes.
        /// </summary>
        public int Count
        {
            get
            {
                return this.Attributes.Length;
            }
        }

        /// <summary>
        /// Gets the attribute with the specified index number.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual Attribute this[int index]
        {
            get
            {
                return this.Attributes[index];
            }
        }

        int ICollection.Count
        {
            get
            {
                return this.Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return null;
            }
        }

        #endregion Properties
    }

    #endregion Non-generic collection extensions

    #region Misc extensions

    /// <summary>
    /// Add the Pointer type with stripped functionalities for PowerShell on CoreCLR.
    /// We need this type because if a method returns a pointer, we need to wrap it into an object.
    /// </summary>
    public sealed class Pointer
    {
        private unsafe void* _ptr;
        private Type _ptrType;

        private Pointer()
        {
        }

        #region Methods

        /// <summary>
        /// Boxes the supplied unmanaged memory pointer and the type associated with that pointer into a managed Pointer wrapper object. 
        /// The value and the type are saved so they can be accessed from the native code during an invocation.
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static unsafe object Box(void* ptr, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (!type.IsPointer)
            {
                throw new ArgumentException("Argument must be pointer", "ptr");
            }

            Pointer pointer = new Pointer();
            pointer._ptr = ptr;
            pointer._ptrType = type;
            return pointer;
        }

        /// <summary>
        /// Returns the stored pointer.
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static unsafe void* Unbox(object ptr)
        {
            var pointer = ptr as Pointer;
            if (pointer == null)
            {
                throw new ArgumentException("Argument must be pointer", "ptr");
            }
            return ((Pointer)ptr)._ptr;
        }

        #endregion Methods
    }

    internal static class ListExtensions
    {
        internal static void ForEach<T>(this List<T> list, Action<T> action)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            for (int i = 0; i < list.Count; i++)
            {
                action(list[i]);
            }
        }
    }

    internal static class X509StoreExtensions
    {
        /// <summary>
        /// X509Store.Close() is not in CoreCLR and it's supposed to be replaced by X509Store.Dispose().
        /// However, X509Store.Dispose() is not supported until .NET 4.6. So we have to have this extension
        /// method 'Close' for X509Store for OneCore powershell, so that it works for both Full/Core CLR.
        /// </summary>
        internal static void Close(this System.Security.Cryptography.X509Certificates.X509Store x509Store)
        {
            x509Store.Dispose();
        }
    }

    #endregion Misc extensions
}


namespace Microsoft.PowerShell.CoreCLR
{
    using System.IO;
    using System.Management.Automation;

    /// <summary>
    /// AssemblyExtensions
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Load an assembly given its file path.
        /// </summary>
        /// <param name="assemblyPath">The path of the file that contains the manifest of the assembly.</param>
        /// <returns>The loaded assembly.</returns>
        public static Assembly LoadFrom(string assemblyPath)
        {
            return ClrFacade.LoadFrom(assemblyPath);
        }

        /// <summary>
        /// Load an assembly given its byte stream
        /// </summary>
        /// <param name="assembly">The byte stream of assembly</param>
        /// <returns>The loaded assembly</returns>
        public static Assembly LoadFrom(Stream assembly)
        {
            return ClrFacade.LoadFrom(assembly);
        }
    }
}

#endif
