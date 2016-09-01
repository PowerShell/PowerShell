/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

// for fxcop

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Management.Infrastructure;
using Microsoft.PowerShell.Commands;
#if !CORECLR
// System.DirectoryServices are not in CoreCLR
using System.DirectoryServices;
using System.Net.Mail;
#endif

namespace System.Management.Automation.Language
{
    internal static class TypeResolver
    {
        // our interface
        //    internal static Type ConvertTypeNameToType(TypeName typeName, out Exception exception)
        //        Used by TypeName.GetReflectionType and in TypeResolver
        //    internal static bool TryResolveType(string typeName, out Type type)
        //        Used in a bunch of places
        //    internal static Type ConvertStringToType(string typeName, out Exception exception)
        //        Used primarily in LanguagePrimitives.ConvertTo

        private static Type LookForTypeInSingleAssembly(Assembly assembly, string typename)
        {
            Type targetType = assembly.GetType(typename, false, true);
            if (targetType != null && IsPublic(targetType))
            {
                return targetType;
            }

            return null;
        }

        /// <summary>
        /// Inherited from InvalidCastException, because it happens in [string] -> [Type] conversion.
        /// </summary>
        internal class AmbiguousTypeException : InvalidCastException
        {
            public string[] Candidates { private set; get; }
            public TypeName TypeName { private set; get; }

            public AmbiguousTypeException(TypeName typeName, IEnumerable<string> candidates)
            {
                Candidates = candidates.ToArray();
                TypeName = typeName;
                Diagnostics.Assert(Candidates.Length > 1, "AmbiguousTypeException can be created only when there are more then 1 candidate.");
            }
        }

        private static Type LookForTypeInAssemblies(TypeName typeName, IEnumerable<Assembly> assemblies, TypeResolutionState typeResolutionState, bool reportAmbigousException, out Exception exception)
        {
            exception = null;
            string alternateNameToFind = typeResolutionState.GetAlternateTypeName(typeName.Name);
            Type foundType = null;
            Type foundType2 = null;
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type targetType = LookForTypeInSingleAssembly(assembly, typeName.Name); ;
                    if (targetType == null && alternateNameToFind != null)
                    {
                        targetType = LookForTypeInSingleAssembly(assembly, alternateNameToFind);
                    }

                    if (targetType != null)
                    {
                        if (!reportAmbigousException)
                        {
                            // accelerator  for the common case, when we are not interested  in ambiguity exception.
                            return targetType;
                        }

                        // .NET has forward notation for types, when they declared in one assembly and implemented in another one.
                        // We want to support both scenarios:
                        // 1) When we pass assembly with declared forwarded type (CoreCLR)
                        // 2) When we pass assembly with declared forwarded type and assembly with implemented forwarded type (FullCLR)
                        // In the case (2) we should not report duplicate, hence this check
                        if (foundType != targetType)
                        {
                            if (foundType != null)
                            {
                                foundType2 = targetType;
                                break;
                            }
                            else
                            {
                                foundType = targetType;
                            }
                        }
                    }
                }
                catch (Exception e) // Assembly.GetType might throw unadvertised exceptions
                {
                    CommandProcessorBase.CheckForSevereException(e);
                }
            }

            if (foundType2 != null)
            {
                exception = new AmbiguousTypeException(typeName, new String[] { foundType.AssemblyQualifiedName, foundType2.AssemblyQualifiedName });
                return null;
            }

            return foundType;
        }

        /// <summary>
        /// A type IsPublic if IsPublic or (IsNestedPublic and is nested in public type(s))
        /// </summary>
        internal static bool IsPublic(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            return IsPublic(typeInfo);
        }

        /// <summary>
        /// A typeInfo is public if IsPublic or (IsNestedPublic and is nested in public type(s))
        /// </summary>
        internal static bool IsPublic(TypeInfo typeInfo)
        {
            if (typeInfo.IsPublic)
            {
                return true;
            }
            if (!typeInfo.IsNestedPublic)
            {
                return false;
            }
            Type type;
            while ((type = typeInfo.DeclaringType) != null)
            {
                typeInfo = type.GetTypeInfo();
                if (!(typeInfo.IsPublic || typeInfo.IsNestedPublic))
                {
                    return false;
                }
            }
            return true;
        }

        private static Type ResolveTypeNameWorker(TypeName typeName, SessionStateScope currentScope, IEnumerable<Assembly> loadedAssemblies, TypeResolutionState typeResolutionState, bool reportAmbigousException, out Exception exception)
        {
            Type result;
            exception = null;
            while (currentScope != null)
            {
                result = currentScope.LookupType(typeName.Name);
                if (result != null)
                {
                    return result;
                }
                currentScope = currentScope.Parent;
            }

            if (TypeAccelerators.builtinTypeAccelerators.TryGetValue(typeName.Name, out result))
            {
                return result;
            }

            exception = null;
            result = LookForTypeInAssemblies(typeName, loadedAssemblies, typeResolutionState, reportAmbigousException, out exception);
            if (exception != null)
            {
                // skip the rest of lookups, if exception reported.
                return result;
            }

            if (result == null)
            {
                lock (TypeAccelerators.userTypeAccelerators)
                {
                    TypeAccelerators.userTypeAccelerators.TryGetValue(typeName.Name, out result);
                }
            }

            return result;
        }

        internal static Type ResolveAssemblyQualifiedTypeName(TypeName typeName, out Exception exception)
        {
            // If an assembly name was specified, we let Type.GetType deal with loading the assembly
            // and resolving the type.
            exception = null;
            try
            {
                // We shouldn't really bother looking for the type in System namespace, but
                // we've always done that.  We explicitly are not supporting arbitrary
                // 'using namespace' here because there is little value, if you need the assembly
                // qualifier, it's best to just fully specify the type.
                var result = Type.GetType(typeName.FullName, false, true) ??
                             Type.GetType("System." + typeName.FullName, false, true);

                if (result != null && IsPublic(result))
                {
                    return result;
                }
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);
                exception = e;
            }

            return null;
        }

        internal static Type ResolveTypeNameWithContext(TypeName typeName, out Exception exception, Assembly[] assemblies, TypeResolutionState typeResolutionState)
        {
            ExecutionContext context = null;

            exception = null;

            if (typeResolutionState == null)
            {
                // Usings from script scope (and if no script scope, fall back to default 'using namespace system')
                context = LocalPipeline.GetExecutionContextFromTLS();
                typeResolutionState = TypeResolutionState.GetDefaultUsingState(context);
            }

            // We can do the cache lookup only if we don't define type in the current scope (cache would be invalid in this case).
            var result = typeResolutionState.ContainsTypeDefined(typeName.Name)
                ? null
                : TypeCache.Lookup(typeName, typeResolutionState);
            if (result != null)
            {
                return result;
            }

            if (typeName.AssemblyName != null)
            {
                result = ResolveAssemblyQualifiedTypeName(typeName, out exception);
                TypeCache.Add(typeName, typeResolutionState, result);
                return result;
            }

            // Simple typename (no generics, no arrays, no assembly name)
            // We use the following search order, using the specified name (assumed to be fully namespace qualified):
            //
            //     * Search scope table (includes 'using type x = ...' aliases)
            //     * Built in type accelerators (implicit 'using type x = ...' aliases that are effectively in global scope
            //     * typeResolutionState.assemblies, which contains:
            //          - Assemblies with PS types, added by 'using module' 
            //          - Assemblies added by 'using assembly'.
            //          For this case, we REPORT ambiguity, since user explicitly specifies the set of assemblies.                        
            //     * All other loaded assemblies (excluding dynamic assemblies created for PS defined types).
            //          IGNORE ambiguity. It mimics PS v4. There are two reasons:
            //          1) If we report ambiguity, we need to fix our caching logic accordingly.
            //             Consider this code
            //                  Add-Type 'public class Q {}' # ok
            //                  Add-Type 'public class Q { }' # get error about the same name
            //                  [Q] # we would get error about ambiguous type, because we added assembly with duplicated type
            //                      # before we can report TYPE_ALREADY_EXISTS error.
            //      
            //                  Add-Type 'public class Q2 {}' # ok
            //                  [Q2] # caching Q2 type
            //                  Add-Type 'public class Q2 { }' # get error about the same name
            //                  [Q2] # we don't get an error about ambiguous type, because it's cached already
            //          2) NuGet (VS Package Management console) uses MEF extensibility model. 
            //             Different assemblies includes same interface (i.e. NuGet.VisualStudio.IVsPackageInstallerServices), 
            //             where they include only methods that they are interested in the interface declaration (result interfaces are different!).
            //             Then, at runtime VS provides an instance. Everything work as far as instance has compatible API.
            //             So [NuGet.VisualStudio.IVsPackageInstallerServices] can be resolved to several different assemblies and it's ok.
            //     * User defined type accelerators (rare - interface was never public)
            //
            // If nothing is found, we search again, this time applying any 'using namespace ...' declarations including the implicit 'using namespace System'. 
            // We must search all using aliases and REPORT an error if there is an ambiguity.

            var assemList = assemblies ?? ClrFacade.GetAssemblies(typeResolutionState, typeName);

            if (context == null)
            {
                context = LocalPipeline.GetExecutionContextFromTLS();
            }
            SessionStateScope currentScope = null;
            if (context != null)
            {
                currentScope = context.EngineSessionState.CurrentScope;
            }

            // If this is TypeDefinition we should not cache anything in TypeCache.
            if (typeName._typeDefinitionAst != null)
            {
                return typeName._typeDefinitionAst.Type;
            }

            result = ResolveTypeNameWorker(typeName, currentScope, typeResolutionState.assemblies, typeResolutionState, /* reportAmbigousException */ true, out exception);
            if (exception == null && result == null)
            {
                result = ResolveTypeNameWorker(typeName, currentScope, assemList, typeResolutionState, /* reportAmbigousException */ false, out exception);
            }

            if (result != null)
            {
                TypeCache.Add(typeName, typeResolutionState, result);
                return result;
            }

            if (exception == null)
            {
                foreach (var ns in typeResolutionState.namespaces)
                {
                    var newTypeNameToSearch = ns + "." + typeName.Name;
                    newTypeNameToSearch = typeResolutionState.GetAlternateTypeName(newTypeNameToSearch) ??
                                          newTypeNameToSearch;
                    var newTypeName = new TypeName(typeName.Extent, newTypeNameToSearch);
#if CORECLR
                    if (assemblies == null)
                    {
                        // We called 'ClrFacade.GetAssemblies' to get assemblies. That means the assemblies to search from 
                        // are not pre-defined, and thus we have to refetch assembly again based on the new type name.
                        assemList = ClrFacade.GetAssemblies(typeResolutionState, newTypeName);
                    }
#endif
                    var newResult = ResolveTypeNameWorker(newTypeName, currentScope, typeResolutionState.assemblies, typeResolutionState, /* reportAmbigousException */ true, out exception);
                    if (exception == null && newResult == null)
                    {
                        newResult = ResolveTypeNameWorker(newTypeName, currentScope, assemList, typeResolutionState, /* reportAmbigousException */ false, out exception);
                    }

                    if (exception != null)
                    {
                        break;
                    }

                    if (newResult != null)
                    {
                        if (result == null)
                        {
                            result = newResult;
                        }
                        else
                        {
                            exception = new AmbiguousTypeException(typeName, new string[] { result.FullName, newResult.FullName });
                            result = null;
                            break;
                        }
                    }
                }
            }

            if (exception != null)
            {
                // AmbiguousTypeException is for internal representation only.
                var ambiguousException = exception as AmbiguousTypeException;
                if (ambiguousException != null)
                {
                    exception = new PSInvalidCastException("AmbiguousTypeReference", exception,
                    ParserStrings.AmbiguousTypeReference, ambiguousException.TypeName.Name,
                                    ambiguousException.Candidates[0], ambiguousException.Candidates[1]);
                }
            }

            if (result != null)
            {
                TypeCache.Add(typeName, typeResolutionState, result);
            }
            return result;
        }

        internal static Type ResolveTypeName(TypeName typeName, out Exception exception)
        {
            return ResolveTypeNameWithContext(typeName, out exception, null, null);
        }

        internal static bool TryResolveType(string typeName, out Type type)
        {
            Exception exception;
            type = ResolveType(typeName, out exception);
            return (type != null);
        }

        internal static Type ResolveITypeName(ITypeName iTypeName, out Exception exception)
        {
            exception = null;
            var typeName = iTypeName as TypeName;
            if (typeName == null)
            {
                // The type is something more complicated - generic or array.
                try
                {
                    return iTypeName.GetReflectionType();
                }
                catch (Exception e)
                {
                    CommandProcessorBase.CheckForSevereException(e);
                    exception = e;
                    return null;
                }
            }

            return ResolveTypeName(typeName, out exception);
        }

        /// <summary>
        /// This routine converts a string into a Type object using the msh rules.
        /// </summary>
        /// <param name="strTypeName">A string representing the name of the type to convert</param>
        /// <param name="exception">The exception, if one happened, trying to find the type</param>
        /// <returns>A type if the conversion was successful, null otherwise.</returns>
        internal static Type ResolveType(string strTypeName, out Exception exception)
        {
            exception = null;
            if (String.IsNullOrWhiteSpace(strTypeName))
            {
                return null;
            }

            var iTypeName = Parser.ScanType(strTypeName, ignoreErrors: false);
            if (iTypeName == null)
            {
                return null;
            }

            return ResolveITypeName(iTypeName, out exception);
        }
    }

    /// <summary>
    /// The idea behind this class is: I should be able to re-use expensive 
    /// type resolution operation result in the same context.
    /// Hence, this class is a key for TypeCache dictionary.
    /// 
    /// Every SessionStateScope has TypeResolutionState. 
    /// typesDefined contains PowerShell types names defined in the current scope and all scopes above.
    /// Same for namespaces.
    /// 
    /// If TypeResolutionState doesn't add anything new compare to it's parent, we represent it as null.
    /// So, when we do lookup, we need to find first non-null TypeResolutionState.
    /// </summary>
    internal class TypeResolutionState
    {
        internal static readonly string[] systemNamespace = { "System" };
        internal static readonly Assembly[] emptyAssemblies = Utils.EmptyArray<Assembly>();
        internal static readonly TypeResolutionState UsingSystem = new TypeResolutionState();

        internal readonly string[] namespaces;
        internal readonly Assembly[] assemblies;
        private readonly HashSet<string> _typesDefined;
        internal readonly int genericArgumentCount;
        internal readonly bool attribute;

        private TypeResolutionState()
            : this(systemNamespace, emptyAssemblies)
        {
        }

        /// <summary>
        /// TypeResolutionState can be shared and that's why it should be represented as an immutable object.
        /// So, we use this API to alternate TypeResolutionState, but instead of mutating existing one, we clone it.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        internal TypeResolutionState CloneWithAddTypesDefined(IEnumerable<string> types)
        {
            var newTypesDefined = new HashSet<string>(_typesDefined, StringComparer.OrdinalIgnoreCase);
            foreach (var type in types)
            {
                newTypesDefined.Add(type);
            }

            return new TypeResolutionState(this, newTypesDefined);
        }

        internal bool ContainsTypeDefined(string type)
        {
            return _typesDefined.Contains(type);
        }

        internal TypeResolutionState(string[] namespaces, Assembly[] assemblies)
        {
            this.namespaces = namespaces ?? systemNamespace;
            this.assemblies = assemblies ?? emptyAssemblies;
            _typesDefined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        internal TypeResolutionState(TypeResolutionState other, int genericArgumentCount, bool attribute)
        {
            this.namespaces = other.namespaces;
            this.assemblies = other.assemblies;
            _typesDefined = other._typesDefined;
            this.genericArgumentCount = genericArgumentCount;
            this.attribute = attribute;
        }

        private TypeResolutionState(TypeResolutionState other, HashSet<string> typesDefined)
        {
            this.namespaces = other.namespaces;
            this.assemblies = other.assemblies;
            _typesDefined = typesDefined;
            this.genericArgumentCount = other.genericArgumentCount;
            this.attribute = other.attribute;
        }

        internal static TypeResolutionState GetDefaultUsingState(ExecutionContext context)
        {
            if (context == null)
            {
                context = LocalPipeline.GetExecutionContextFromTLS();
            }

            if (context != null)
            {
                return context.EngineSessionState.CurrentScope.TypeResolutionState;
            }

            return TypeResolutionState.UsingSystem;
        }

        internal string GetAlternateTypeName(string typeName)
        {
            string alternateName = null;
            if (genericArgumentCount > 0 && typeName.IndexOf('`') < 0)
            {
                alternateName = typeName + "`" + genericArgumentCount;
            }
            else if (attribute && !typeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase))
            {
                alternateName = typeName + "Attribute";
            }
            return alternateName;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var other = obj as TypeResolutionState;

            if (other == null)
                return false;

            if (this.attribute != other.attribute)
                return false;

            if (this.genericArgumentCount != other.genericArgumentCount)
                return false;

            if (this.namespaces.Length != other.namespaces.Length)
                return false;

            if (this.assemblies.Length != other.assemblies.Length)
                return false;

            for (int i = 0; i < namespaces.Length; i++)
            {
                if (!this.namespaces[i].Equals(other.namespaces[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            for (int i = 0; i < assemblies.Length; i++)
            {
                if (!this.assemblies[i].Equals(other.assemblies[i]))
                    return false;
            }

            if (_typesDefined.Count != other._typesDefined.Count)
                return false;

            return _typesDefined.SetEquals(other._typesDefined);
        }

        public override int GetHashCode()
        {
            var stringComparer = StringComparer.OrdinalIgnoreCase;
            int result = Utils.CombineHashCodes(genericArgumentCount.GetHashCode(), attribute.GetHashCode());
            for (int i = 0; i < namespaces.Length; i++)
            {
                result = Utils.CombineHashCodes(result, stringComparer.GetHashCode(namespaces[i]));
            }
            for (int i = 0; i < assemblies.Length; i++)
            {
                result = Utils.CombineHashCodes(result, this.assemblies[i].GetHashCode());
            }
            foreach (var t in _typesDefined)
            {
                result = Utils.CombineHashCodes(result, t.GetHashCode());
            }
            return result;
        }
    }

    internal class TypeCache
    {
        private class KeyComparer : IEqualityComparer<Tuple<ITypeName, TypeResolutionState>>
        {
            public bool Equals(Tuple<ITypeName, TypeResolutionState> x,
                               Tuple<ITypeName, TypeResolutionState> y)
            {
                return x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2);
            }

            public int GetHashCode(Tuple<ITypeName, TypeResolutionState> obj)
            {
                return obj.GetHashCode();
            }
        }

        private static readonly ConcurrentDictionary<Tuple<ITypeName, TypeResolutionState>, Type> s_cache = new ConcurrentDictionary<Tuple<ITypeName, TypeResolutionState>, Type>(new KeyComparer());

        internal static Type Lookup(ITypeName typeName, TypeResolutionState typeResolutionState)
        {
            Type result;
            s_cache.TryGetValue(Tuple.Create(typeName, typeResolutionState), out result);
            return result;
        }

        internal static void Add(ITypeName typeName, TypeResolutionState typeResolutionState, Type type)
        {
            s_cache.GetOrAdd(Tuple.Create(typeName, typeResolutionState), type);
        }
    }
}

namespace System.Management.Automation
{
    /// <summary>
    /// A class to the core types in PowerShell.
    /// </summary>
    internal static class CoreTypes
    {
        // A list of the core PowerShell types, and their accelerator.
        //
        // These types are frequently used in scripting, and for parameter validation in scripts.
        //
        // When in ConstrainedLanguage mode, all operations on these types are fully supported unlike all other
        // types that are assumed to have dangerous constructors or methods.
        //
        // Do not add types to this pool unless they can be safely exposed to an attacker and will not
        // expose the ability to corrupt or escape PowerShell's environment. The following operations must
        // be safe: type conversion, all constructors, all methods (instance and static), and
        // and properties (instance and static).
        internal static Lazy<Dictionary<Type, string[]>> Items = new Lazy<Dictionary<Type, string[]>>(
            () =>
                new Dictionary<Type, string[]>
                {
                    { typeof(AliasAttribute),                              new[] { "Alias" } },
                    { typeof(AllowEmptyCollectionAttribute),               new[] { "AllowEmptyCollection" } },
                    { typeof(AllowEmptyStringAttribute),                   new[] { "AllowEmptyString" } },
                    { typeof(AllowNullAttribute),                          new[] { "AllowNull" } },
                    { typeof(ArgumentCompleterAttribute),                  new[] { "ArgumentCompleter" } },
                    { typeof(Array),                                       new[] { "array" } },
                    { typeof(bool),                                        new[] { "bool" } },
                    { typeof(byte),                                        new[] { "byte" } },
                    { typeof(char),                                        new[] { "char" } },
                    { typeof(CmdletBindingAttribute),                      new[] { "CmdletBinding" } },
                    { typeof(DateTime),                                    new[] { "datetime" } },
                    { typeof(decimal),                                     new[] { "decimal" } },
                    { typeof(double),                                      new[] { "double" } },
                    { typeof(DscResourceAttribute),                        new[] { "DscResource"} },
                    { typeof(float),                                       new[] { "float", "single" } },
                    { typeof(Guid),                                        new[] { "guid" } },
                    { typeof(Hashtable),                                   new[] { "hashtable" } },
                    { typeof(int),                                         new[] { "int", "int32" } },
                    { typeof(Int16),                                       new[] { "int16" } },
                    { typeof(long),                                        new[] { "long", "int64" } },
                    { typeof(CimInstance),                                 new[] { "ciminstance" } },
                    { typeof(CimClass),                                    new[] { "cimclass" } },
                    { typeof(Microsoft.Management.Infrastructure.CimType), new[] { "cimtype" } },
                    { typeof(CimConverter),                                new[] { "cimconverter" } },
                    { typeof(ModuleSpecification),                         null },
                    { typeof(IPEndPoint),                                  new[] { "IPEndpoint" } },
                    { typeof(NullString),                                  new[] { "NullString" } },
                    { typeof(OutputTypeAttribute),                         new[] { "OutputType" } },
                    { typeof(Object[]),                                    null },
                    { typeof(ObjectSecurity),                              new[] { "ObjectSecurity" } },
                    { typeof(ParameterAttribute),                          new[] { "Parameter" } },
                    { typeof(PhysicalAddress),                             new[] { "PhysicalAddress" } },
                    { typeof(PSCredential),                                new[] { "pscredential" } },
                    { typeof(PSDefaultValueAttribute),                     new[] { "PSDefaultValue" } },
                    { typeof(PSListModifier),                              new[] { "pslistmodifier" } },
                    { typeof(PSObject),                                    new[] { "psobject", "pscustomobject" } },
                    { typeof(PSPrimitiveDictionary),                       new[] { "psprimitivedictionary" } },
                    { typeof(PSReference),                                 new[] { "ref" } },
                    { typeof(PSTypeNameAttribute),                         new[] { "PSTypeNameAttribute" } },
                    { typeof(Regex),                                       new[] { "regex" } },
                    { typeof(DscPropertyAttribute),                        new[] { "DscProperty" } },
                    { typeof(SByte),                                       new[] { "sbyte" } },
                    { typeof(string),                                      new[] { "string" } },
                    { typeof(SupportsWildcardsAttribute),                  new[] { "SupportsWildcards" } },
                    { typeof(SwitchParameter),                             new[] { "switch" } },
                    { typeof(CultureInfo),                                 new[] { "cultureinfo" } },
                    { typeof(BigInteger),                                  new[] { "bigint" } },
                    { typeof(SecureString),                                new[] { "securestring" } },
                    { typeof(TimeSpan),                                    new[] { "timespan" } },
                    { typeof(UInt16),                                      new[] { "uint16" } },
                    { typeof(UInt32),                                      new[] { "uint32" } },
                    { typeof(UInt64),                                      new[] { "uint64" } },
                    { typeof(Uri),                                         new[] { "uri" } },
                    { typeof(ValidateCountAttribute),                      new[] { "ValidateCount" } },
                    { typeof(ValidateDriveAttribute),                      new[] { "ValidateDrive" } },
                    { typeof(ValidateLengthAttribute),                     new[] { "ValidateLength" } },
                    { typeof(ValidateNotNullAttribute),                    new[] { "ValidateNotNull" } },
                    { typeof(ValidateNotNullOrEmptyAttribute),             new[] { "ValidateNotNullOrEmpty" } },
                    { typeof(ValidatePatternAttribute),                    new[] { "ValidatePattern" } },
                    { typeof(ValidateRangeAttribute),                      new[] { "ValidateRange" } },
                    { typeof(ValidateScriptAttribute),                     new[] { "ValidateScript" } },
                    { typeof(ValidateSetAttribute),                        new[] { "ValidateSet" } },
                    { typeof(ValidateUserDriveAttribute),                  new[] { "ValidateUserDrive"} },
                    { typeof(Version),                                     new[] { "version" } },
                    { typeof(void),                                        new[] { "void" } },
                    { typeof(IPAddress),                                   new[] { "ipaddress" } },
                    { typeof(DscLocalConfigurationManagerAttribute),       new[] {"DscLocalConfigurationManager"}},
                    { typeof(WildcardPattern),                             new[] { "WildcardPattern" } },
                    { typeof(X509Certificate),                             new[] { "X509Certificate" } },
                    { typeof(X500DistinguishedName),                       new[] { "X500DistinguishedName" } },
                    { typeof(XmlDocument),                                 new[] { "xml" } },
                    { typeof(CimSession),                                  new[] { "CimSession" } },
#if !CORECLR
                    // Following types not in CoreCLR
                    { typeof(DirectoryEntry),                              new[] { "adsi" } },
                    { typeof(DirectorySearcher),                           new[] { "adsisearcher" } },
                    { typeof(ManagementClass),                             new[] { "wmiclass" } },
                    { typeof(ManagementObject),                            new[] { "wmi" } },
                    { typeof(ManagementObjectSearcher),                    new[] { "wmisearcher" } },
                    { typeof(MailAddress),                                 new[] { "mailaddress" } }
#endif
                }
            );

        internal static bool Contains(Type inputType)
        {
            if (Items.Value.ContainsKey(inputType))
            {
                return true;
            }
            var inputTypeInfo = inputType.GetTypeInfo();
            if (inputTypeInfo.IsEnum)
            {
                return true;
            }
            if (inputTypeInfo.IsGenericType)
            {
                var genericTypeDefinition = inputTypeInfo.GetGenericTypeDefinition();
                return genericTypeDefinition == typeof(Nullable<>) || genericTypeDefinition == typeof(FlagsExpression<>);
            }
            return (inputType.IsArray && Contains(inputType.GetElementType()));
        }
    }

    /// <summary>
    /// A class to view and modify the type accelerators used by the PowerShell engine.  Builtin
    /// type accelerators are read only, but user defined type accelerators may be added.
    /// </summary>
    internal static class TypeAccelerators
    {
        // builtins are not exposed publicly in a direct manner so they can't be changed at all
        internal static Dictionary<string, Type> builtinTypeAccelerators = new Dictionary<string, Type>(64, StringComparer.OrdinalIgnoreCase);

        // users can add to user added accelerators (but not currently remove any.)  Keeping a separate
        // list allows us to add removing in the future w/o worrying about breaking the builtins.
        internal static Dictionary<string, Type> userTypeAccelerators = new Dictionary<string, Type>(64, StringComparer.OrdinalIgnoreCase);

        // We expose this one publicly for programmatic access to our type accelerator table, but it is
        // otherwise unused (so changes to this dictionary don't affect internals.)
        private static Dictionary<string, Type> s_allTypeAccelerators;

        static TypeAccelerators()
        {
            // Add all the core types
            foreach (KeyValuePair<Type, string[]> coreType in CoreTypes.Items.Value)
            {
                if (coreType.Value != null)
                {
                    foreach (string accelerator in coreType.Value)
                    {
                        builtinTypeAccelerators.Add(accelerator, coreType.Key);
                    }
                }
            }

            // Add additional utility types that are useful as type accelerators, but aren't
            // fundamentally "core language", or may be unsafe to expose to untrusted input.
            builtinTypeAccelerators.Add("scriptblock", typeof(ScriptBlock));
            builtinTypeAccelerators.Add("psvariable", typeof(PSVariable));
            builtinTypeAccelerators.Add("type", typeof(Type));
            builtinTypeAccelerators.Add("psmoduleinfo", typeof(PSModuleInfo));
            builtinTypeAccelerators.Add("powershell", typeof(PowerShell));
            builtinTypeAccelerators.Add("runspacefactory", typeof(RunspaceFactory));
            builtinTypeAccelerators.Add("runspace", typeof(Runspace));
            builtinTypeAccelerators.Add("initialsessionstate", typeof(InitialSessionState));
            builtinTypeAccelerators.Add("psscriptmethod", typeof(PSScriptMethod));
            builtinTypeAccelerators.Add("psscriptproperty", typeof(PSScriptProperty));
            builtinTypeAccelerators.Add("psnoteproperty", typeof(PSNoteProperty));
            builtinTypeAccelerators.Add("psaliasproperty", typeof(PSAliasProperty));
            builtinTypeAccelerators.Add("psvariableproperty", typeof(PSVariableProperty));
        }

        internal static string FindBuiltinAccelerator(Type type)
        {
            foreach (KeyValuePair<string, Type> entry in builtinTypeAccelerators)
            {
                if (entry.Value == type)
                {
                    return entry.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a type accelerator.
        /// </summary>
        /// <param name="typeName">The type accelerator name.</param>
        /// <param name="type">The type of the type accelerator.</param>
        public static void Add(string typeName, Type type)
        {
            userTypeAccelerators[typeName] = type;
            if (s_allTypeAccelerators != null)
            {
                s_allTypeAccelerators[typeName] = type;
            }
        }

        /// <summary>
        /// Remove a type accelerator.
        /// </summary>
        /// <returns>True if the accelerator was removed, false otherwise.</returns>
        /// <param name="typeName">The accelerator to remove.</param>
        public static bool Remove(string typeName)
        {
            userTypeAccelerators.Remove(typeName);
            if (s_allTypeAccelerators != null)
            {
                s_allTypeAccelerators.Remove(typeName);
            }
            return true;
        }

        /// <summary>
        /// This property is useful to tools that need to know what
        /// type accelerators are available (e.g. to allow for autocompletion.)
        /// </summary>
        /// <remarks>
        /// The returned dictionary should be treated as read only.  Changes made
        /// to the dictionary will not affect PowerShell scripts in any way.  Use
        /// <see cref="TypeAccelerators.Add"/> and
        /// <see cref="TypeAccelerators.Remove"/> to
        /// affect the type resolution in PowerShell scripts.
        /// </remarks>
        public static Dictionary<string, Type> Get
        {
            get
            {
                if (s_allTypeAccelerators == null)
                {
                    s_allTypeAccelerators = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                    FillCache(s_allTypeAccelerators);
                }

                return s_allTypeAccelerators;
            }
        }

        internal static void FillCache(Dictionary<string, Type> cache)
        {
            foreach (KeyValuePair<string, Type> val in builtinTypeAccelerators)
            {
                cache.Add(val.Key, val.Value);
            }
            foreach (KeyValuePair<string, Type> val in userTypeAccelerators)
            {
                cache.Add(val.Key, val.Value);
            }
        }
    }
}
