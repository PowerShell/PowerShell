using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using Microsoft.PowerShell.DesiredStateConfiguration.Internal;
using System.Reflection;

namespace System.Management.Automation
{
    #region Keyword Command Class

    /// <summary>
    /// An abstract class to extend in order to write a keyword specification. Give this class the KeywordAttribute
    /// in order to use it as a PowerShell DynamicKeyword specification. Any properties with
    /// the KeywordParameterAttribute will be parameters, while properties with the KeywordPropertyAttribute will
    /// be keyword properties.
    /// </summary>
    public abstract class Keyword : InternalCommand
    {
        /// <summary>
        /// Constructs a keyword with null runtime delegates. This is intended to be overridden.
        /// </summary>
        protected Keyword()
        {
        }

        /// <summary>
        /// Specifies the call to run on a DynamicKeyword data object before the AST node
        /// containing that keyword is parsed
        /// </summary>
        public Func<DynamicKeyword, ParseError[]> PreParse { get; set; }

        /// <summary>
        /// Specifies the call to run on a DynamicKeyword statement AST immediately after that AST node
        /// has been parsed
        /// </summary>
        public Func<DynamicKeywordStatementAst, ParseError[]> PostParse { get; set; }

        /// <summary>
        /// Specifies the call to run on a DynamicKeyword statement AST node at semantic check time
        /// </summary>
        public Func<DynamicKeywordStatementAst, ParseError[]> SemanticCheck { get; set; }
    }

    #endregion /* Keyword Command Class */

    #region Dynamic Keyword Specification Attributes

    /// <summary>
    /// Specifies that a class describes a DynamicKeyword for PowerShell
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class KeywordAttribute : ParsingBaseAttribute
    {
        public const DynamicKeywordNameMode DefaultNameMode = DynamicKeywordNameMode.NoName;
        public const DynamicKeywordBodyMode DefaultBodyMode = DynamicKeywordBodyMode.Command;

        /// <summary>
        /// Construct a KeywordAttribute with default values
        /// </summary>
        public KeywordAttribute()
        {
        }

        /// <summary>
        /// Specifies whether a DynamicKeyword has a name, and if so what kind
        /// </summary>
        public DynamicKeywordNameMode Name { get; set; } = DefaultNameMode;

        /// <summary>
        /// Specifies the body type of the DynamicKeyword
        /// </summary>
        public DynamicKeywordBodyMode Body { get; set; } = DefaultBodyMode;
    }

    /// <summary>
    /// Attribute defining a class property as a PowerShell dynamic keyword parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class KeywordParameterAttribute : ParsingBaseAttribute
    {
        public const bool NotMandatory = false;

        /// <summary>
        /// Construct a KeywordParameterAttribute with default values
        /// </summary>
        public KeywordParameterAttribute()
        {
        }

        /// <summary>
        /// Specifies whether this parameter is mandatory
        /// </summary>
        public bool Mandatory { get; set; } = NotMandatory;
    }

    /// <summary>
    /// Attribute defining a class property as a PowerShell dynamic keyword property (for hashtable keywords)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class KeywordPropertyAttribute : ParsingBaseAttribute
    {
        public const bool NotMandatory = false;

        /// <summary>
        /// Construct a KeywordPropertyAttribute with default values
        /// </summary>
        public KeywordPropertyAttribute()
        {
        }

        /// <summary>
        /// Specifies whether this property is mandatory
        /// </summary>
        public bool Mandatory { get; set; } = NotMandatory;
    }

    #endregion
}

namespace System.Management.Automation.Language
{
    #region Dynamic Keyword Parser Datastructures

    /// <summary>
    /// Defines the name modes for a dynamic keyword. A name expression may be required, optional or not permitted.
    /// </summary>
    public enum DynamicKeywordNameMode
    {
        /// <summary>
        /// This keyword does not take a name value
        /// </summary>
        NoName = 0,
        /// <summary>
        /// Name must be present and simple non-empty bare word
        /// </summary>
        SimpleNameRequired = 1,
        /// <summary>
        /// Name must be present but can also be an expression
        /// </summary>
        NameRequired = 2,
        /// <summary>
        /// Name may be optionally present, but if it is present, it must be a non-empty bare word.
        /// </summary>
        SimpleOptionalName = 3,
        /// <summary>
        /// Name may be optionally present, expression or bare word
        /// </summary>
        OptionalName = 4,
    };

    /// <summary>
    /// Defines the body mode for a dynamic keyword. It can be a scriptblock, hashtable or command which means no body
    /// </summary>
    public enum DynamicKeywordBodyMode
    {
        /// <summary>
        /// The keyword act like a command
        /// </summary>
        Command = 0,
        /// <summary>
        /// The keyword has a scriptblock body
        /// </summary>
        ScriptBlock = 1,
        /// <summary>
        /// The keyword has hashtable body
        /// </summary>
        Hashtable = 2,
    }

    /// <summary>
    /// Defines the schema/behaviour for a dynamic keyword.
    /// a constrained
    /// </summary>
    public class DynamicKeyword
    {
        #region static properties/functions

        /// <summary>
        /// Defines a dictionary of dynamic keywords, stored in thread-local storage.
        /// </summary>
        private static Dictionary<string, DynamicKeyword> DynamicKeywords
        {
            get
            {
                return t_dynamicKeywords ??
                       (t_dynamicKeywords = new Dictionary<string, DynamicKeyword>(StringComparer.OrdinalIgnoreCase));
            }
        }

        [ThreadStatic]
        private static Dictionary<string, DynamicKeyword> t_dynamicKeywords;

        /// <summary>
        /// stack of DynamicKeywords Cache
        /// </summary>
        ///
        private static Stack<Dictionary<string, DynamicKeyword>> DynamicKeywordsStack
        {
            get
            {
                return t_dynamicKeywordsStack ??
                       (t_dynamicKeywordsStack = new Stack<Dictionary<string, DynamicKeyword>>());
            }
        }
        [ThreadStatic]
        private static Stack<Dictionary<string, DynamicKeyword>> t_dynamicKeywordsStack;

        /// <summary>
        /// Reset the keyword table to a new empty collection.
        /// </summary>
        public static void Reset()
        {
            t_dynamicKeywords = new Dictionary<string, DynamicKeyword>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Push current dynamicKeywords cache into stack
        /// </summary>
        public static void Push()
        {
            DynamicKeywordsStack.Push(t_dynamicKeywords);
            Reset();
        }

        /// <summary>
        /// Pop up previous  dynamicKeywords cache
        /// </summary>
        public static void Pop()
        {
            t_dynamicKeywords = DynamicKeywordsStack.Pop();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DynamicKeyword GetKeyword(string name)
        {
            DynamicKeyword keywordToReturn;
            DynamicKeyword.DynamicKeywords.TryGetValue(name, out keywordToReturn);
            return keywordToReturn;
        }

        /// <summary>
        /// Returns a copied list of all of the existing dynamic keyword definitions.
        /// </summary>
        /// <returns></returns>
        public static List<DynamicKeyword> GetKeyword()
        {
            return new List<DynamicKeyword>(DynamicKeyword.DynamicKeywords.Values);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ContainsKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException("name");
                throw e;
            }

            return DynamicKeyword.DynamicKeywords.ContainsKey(name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="keywordToAdd"></param>
        public static void AddKeyword(DynamicKeyword keywordToAdd)
        {
            if (keywordToAdd == null)
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException("keywordToAdd");
                throw e;
            }

            // Allow overwriting of the existing entries
            string name = keywordToAdd.Keyword;
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException("keywordToAdd.Keyword");
            }

            DynamicKeyword.DynamicKeywords.Remove(name);
            DynamicKeyword.DynamicKeywords.Add(name, keywordToAdd);
        }

        /// <summary>
        /// Remove a single entry from the dynamic keyword collection
        /// and clean up any associated data.
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException("name");
                throw e;
            }
            DynamicKeyword.DynamicKeywords.Remove(name);
        }

        /// <summary>
        /// Check if it is a hidden keyword
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool IsHiddenKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException("name");
                throw e;
            }

            return s_hiddenDynamicKeywords.Contains(name);
        }

        /// <summary>
        /// A set of dynamic keywords that are not supposed to be used in script directly.
        /// They are for internal use only.
        /// </summary>
        private static readonly HashSet<string> s_hiddenDynamicKeywords =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MSFT_Credential" };

        #endregion

        /// <summary>
        /// Duplicates the DynamicKeyword
        /// </summary>
        /// <returns>A copy of the DynamicKeyword</returns>
        public DynamicKeyword Copy()
        {
            DynamicKeyword keyword = new DynamicKeyword()
            {
                ImplementingModule = this.ImplementingModule,
                ImplementingModuleVersion = this.ImplementingModuleVersion,
                ImplementingModuleInfo = this.ImplementingModuleInfo,
                Keyword = this.Keyword,
                ResourceName = this.ResourceName,
                BodyMode = this.BodyMode,
                DirectCall = this.DirectCall,
                NameMode = this.NameMode,
                MetaStatement = this.MetaStatement,
                IsReservedKeyword = this.IsReservedKeyword,
                HasReservedProperties = this.HasReservedProperties,
                PreParse = this.PreParse,
                PostParse = this.PostParse,
                SemanticCheck = this.SemanticCheck,
                IsNested = this.IsNested,
            };
            foreach (KeyValuePair<string, DynamicKeywordProperty> entry in this.Properties)
            {
                keyword.Properties.Add(entry.Key, entry.Value);
            }
            foreach (KeyValuePair<string, DynamicKeywordParameter> entry in this.Parameters)
            {
                keyword.Parameters.Add(entry.Key, entry.Value);
            }
            return keyword;
        }

        /// <summary>
        /// The name of the module that implements the function corresponding to this keyword.
        /// </summary>
        public string ImplementingModule { get; set; }

        /// <summary>
        /// The version of the module that implements the function corresponding to this keyword.
        /// </summary>
        public Version ImplementingModuleVersion { get; set; }

        /// <summary>
        /// The info object representing all the necessary data on the module this keyword was loaded from
        /// </summary>
        public PSModuleInfo ImplementingModuleInfo { get; set; }

        /// <summary>
        /// The keyword string
        /// If an alias qualifier exist, use alias
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// The keyword resource name string
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Set to true if we should be looking for a scriptblock instead of a hashtable
        /// </summary>
        public DynamicKeywordBodyMode BodyMode { get; set; }

        /// <summary>
        /// If true, then don't use the marshalled call. Just
        /// rewrite the node as a simple direct function call.
        /// If NameMode is other than NoName, then the name of the instance
        /// will be passed as the parameter -InstanceName.
        ///
        /// </summary>
        public bool DirectCall { get; set; }

        /// <summary>
        /// This allows you to specify if the keyword takes a name argument and if so, what form that takes.
        /// </summary>
        public DynamicKeywordNameMode NameMode { get; set; }

        /// <summary>
        /// Indicate that the nothing should be added to the AST for this
        /// dynamic keyword.
        /// </summary>
        public bool MetaStatement { get; set; }

        /// <summary>
        /// Indicate that the keyword is reserved for future use by powershell
        /// </summary>
        public bool IsReservedKeyword { get; set; }

        /// <summary>
        /// Contains the list of properties that are reserved for future use
        /// </summary>
        public bool HasReservedProperties { get; set; }

        /// <summary>
        /// True if the keyword belongs in the scope of another keyword. False otherwise.
        /// </summary>
        public bool IsNested { get; set; }

        /// <summary>
        /// A list of the properties allowed for this constuctor
        /// </summary>
        public Dictionary<string, DynamicKeywordProperty> Properties
        {
            get
            {
                return _properties ??
                       (_properties = new Dictionary<string, DynamicKeywordProperty>(StringComparer.OrdinalIgnoreCase));
            }
        }
        private Dictionary<string, DynamicKeywordProperty> _properties;

        /// <summary>
        /// A list of the parameters allowed for this constuctor.
        /// </summary>
        public Dictionary<string, DynamicKeywordParameter> Parameters
        {
            get
            {
                return _parameters ??
                       (_parameters = new Dictionary<string, DynamicKeywordParameter>(StringComparer.OrdinalIgnoreCase));
            }
        }
        private Dictionary<string, DynamicKeywordParameter> _parameters;

        /// <summary>
        /// Keywords that are defined only in the scope of this keyword
        /// </summary>
        public Dictionary<string, DynamicKeyword> InnerKeywords
        {
            get
            {
                return _innerKeywords ??
                    (_innerKeywords = new Dictionary<string, DynamicKeyword>());
            }
        }
        private Dictionary<string, DynamicKeyword> _innerKeywords;

        /// <summary>
        /// A custom function that gets executed at parsing time before parsing dynamickeyword block
        /// The delegate has one parameter: DynamicKeyword
        /// </summary>
        public Func<DynamicKeyword, ParseError[]> PreParse { get; set; }

        /// <summary>
        /// A custom function that gets executed at parsing time after parsing dynamickeyword block
        /// </summary>
        public Func<DynamicKeywordStatementAst, ParseError[]> PostParse { get; set; }

        /// <summary>
        /// A custom function that checks semantic for the given <see cref="DynamicKeywordStatementAst"/>
        /// </summary>
        public Func<DynamicKeywordStatementAst, ParseError[]> SemanticCheck { get; set; }
    }


    internal static class DynamicKeywordExtension
    {
        internal static bool IsMetaDSCResource(this DynamicKeyword keyword)
        {
            string implementingModule = keyword.ImplementingModule;
            if (implementingModule != null)
            {
                return implementingModule.Equals(DscClassCache.DefaultModuleInfoForMetaConfigResource.Item1, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        internal static bool IsCompatibleWithConfigurationType(this DynamicKeyword keyword, ConfigurationType ConfigurationType)
        {
            return ((ConfigurationType == ConfigurationType.Meta && keyword.IsMetaDSCResource()) ||
                    (ConfigurationType != ConfigurationType.Meta && !keyword.IsMetaDSCResource()));
        }

        private static Dictionary<String, List<String>> s_excludeKeywords = new Dictionary<String, List<String>>(StringComparer.OrdinalIgnoreCase)
        {
            {@"Node", new List<String> {@"Node"}},
        };

        /// <summary>
        /// Get allowed keyword list for a given keyword
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="allowedKeywords"></param>
        /// <returns>NULL if no keyword allowed for a given <see cref="DynamicKeyword"/></returns>
        internal static IEnumerable<DynamicKeyword> GetAllowedKeywords(this DynamicKeyword keyword, IEnumerable<DynamicKeyword> allowedKeywords)
        {
            string keywordName = keyword.Keyword;
            if (String.Compare(keywordName, @"Node", StringComparison.OrdinalIgnoreCase) == 0)
            {
                List<string> excludeKeywords;
                if (s_excludeKeywords.TryGetValue(keywordName, out excludeKeywords))
                {
                    return allowedKeywords.Where(k => !excludeKeywords.Contains(k.Keyword));
                }
                else
                    return allowedKeywords;
            }
            return null;
        }
    }

    /// <summary>
    /// Metadata about a member property for a dynamic keyword.
    /// </summary>
    public class DynamicKeywordProperty
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The required type of the property
        /// </summary>
        public string TypeConstraint { get; set; }

        /// <summary>
        /// Any attributes that the property has
        /// </summary>
        public List<string> Attributes
        {
            get { return _attributes ?? (_attributes = new List<string>()); }
        }
        private List<string> _attributes;

        /// <summary>
        /// List of strings that may be used as values for this property.
        /// </summary>
        public List<string> Values
        {
            get { return _values ?? (_values = new List<string>()); }
        }
        private List<string> _values;

        /// <summary>
        /// Mapping the descriptive values to the actual values
        /// </summary>
        public Dictionary<string, string> ValueMap
        {
            get { return _valueMap ?? (_valueMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)); }
        }
        private Dictionary<string, string> _valueMap;

        /// <summary>
        /// Indicates that this property is mandatory and must be present.
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        /// Indicates that this property is a key.
        /// </summary>
        public bool IsKey { get; set; }

        /// <summary>
        /// Indicates a range constraint on the property value
        /// </summary>
        public Tuple<int, int> Range { get; set; }
    }

    /// <summary>
    /// Metadata about a parameter for a dynamic keyword. Adds one
    /// new property to the base classL Switch for switch parameters
    /// (THere is no such thing as a switch property...)
    /// </summary>
    public class DynamicKeywordParameter : DynamicKeywordProperty
    {
        /// <summary>
        /// Type if this is a switch parameter and takes no argument
        /// </summary>
        public bool Switch { get; set; }
    }

    #endregion /* Dynamic Keyword Parser Datastructures */

    #region Dynamic Keyword Metadata Reader

    internal class DynamicKeywordDllModuleMetadataReader
    {
        private readonly PSModuleInfo _moduleInfo;

        private MetadataReader _metadataReader;
        private Stack<Dictionary<string, List<string>>> _enumDefStack;
        private Stack<HashSet<string>> _keywordDefinitionStack;

        // Type providers to resolve metadata into strings for method/constructor signatures.
        // Strings are used when a user has defined a type, since using Type would require loading the assembly,
        // which is what the metadata reader is trying to avoid
        private ITypeProvider<string, IGenericContext<string>> _namingTypeProvider;
        private IGenericContext<string> _currentNamingGenericContext;

        // Type provider to resolve data from types that are known because they belong to the PowerShell assemblies
        private ITypeProvider<Type, IGenericContext<Type>> _knownTypeTypeProvider;
        
        /// <summary>
        /// Construct a new DLL reader from a loaded module, whose information is
        /// described by a PSModuleInfo object
        /// </summary>
        /// <param name="moduleInfo">the information object describing the module to read Dynamic Keyword specifications from</param>
        public DynamicKeywordDllModuleMetadataReader(PSModuleInfo moduleInfo)
        {
            _moduleInfo = moduleInfo;
            _knownTypeTypeProvider = new KnownTypeTypeProvider();
            _namingTypeProvider = new NamingTypeProvider();
        }

        /// <summary>
        /// Parse all globally defined keywords in the keyword specification by recursive descent
        /// </summary>
        /// <returns>An enumerable of top level keywords that have been parsed</returns>
        public IEnumerable<DynamicKeyword> ReadDynamicKeywordSpecificationModule()
        {
            var globalDynamicKeywords = new List<DynamicKeyword>();

            try
            {
                using (FileStream stream = File.OpenRead(_moduleInfo.Path))
                using (var peReader = new PEReader(stream))
                {
                    if (!peReader.HasMetadata)
                    {
                        return null;
                    }

                    _metadataReader = peReader.GetMetadataReader();
                    _enumDefStack = new Stack<Dictionary<string, List<string>>>();
                    _keywordDefinitionStack = new Stack<HashSet<string>>();

                    globalDynamicKeywords = ReadGlobalDynamicKeywords().ToList();
                }
            }
            catch (BadImageFormatException)
            {
                // If the DLL is somehow invalid, we just ignore it
                return null;
            }

            if (globalDynamicKeywords.Count == 0)
            {
                return null;
            }

            return globalDynamicKeywords;
        }

        /// <summary>
        /// Read in enum type names and values from a list of enum type defintions -- this assumes
        /// <paramref name="enumDefinitions"/> only contains TypeDefinitions representing enum types
        /// </summary>
        /// <param name="enumDefinitions"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> ReadEnumDefinitions(IEnumerable<TypeDefinition> enumDefinitions)
        {
            var enumDefs = new Dictionary<string, List<string>>();
            foreach (var typeDef in enumDefinitions)
            {
                switch (typeDef.BaseType.Kind)
                {
                    case HandleKind.TypeReference:
                        var baseType = _metadataReader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
                        var enumFields = new List<string>();
                        foreach (FieldDefinitionHandle enumFieldHandle in typeDef.GetFields())
                        {
                            FieldDefinition field = _metadataReader.GetFieldDefinition(enumFieldHandle);
                            string fieldName = _metadataReader.GetString(field.Name);
                            // Ensure the value we add is not the special enum value
                            if (fieldName != "value__")
                            {
                                enumFields.Add(fieldName);
                            }
                        }
                        enumDefs.Add(_metadataReader.GetString(typeDef.Name), enumFields);
                        break;
                }
            }
            return enumDefs;
        }

        /// <summary>
        /// Read in all keywords defined in the module, by starting with top-level keywords and continuing by recursive descent
        /// </summary>
        /// <returns>an enumeration of all keywords defined in the module</returns>
        private IEnumerable<DynamicKeyword> ReadGlobalDynamicKeywords()
        {
            // Go through the module and find all the top level type definitions
            var topLevelEnums = new List<TypeDefinition>();
            var topLevelClasses = new List<TypeDefinition>();
            foreach (var typeDefHandle in _metadataReader.TypeDefinitions)
            {
                TypeDefinition typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
                if (typeDef.GetDeclaringType().IsNil)
                {
                    if (IsEnum(typeDef))
                    {
                        topLevelEnums.Add(typeDef);
                        continue;
                    }

                    // Assume other types are keyword definitions for now -- we'll check this below
                    topLevelClasses.Add(typeDef);
                }
            }

            // Get all enums defined at this level in the scope
            _enumDefStack.Push(ReadEnumDefinitions(topLevelEnums));

            // Set up the keyword scoping stack to check for duplicate names
            _keywordDefinitionStack.Push(new HashSet<string>());

            // Read in all the keywords
            foreach (var typeDefHandle in _metadataReader.TypeDefinitions)
            {
                var typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
                var declaringType = typeDef.GetDeclaringType(); // Make sure this keyword is not nested (declared as an inner class)
                CustomAttribute? keywordAttribute = null;
                if (declaringType.IsNil && IsKeywordSpecification(typeDef, ref keywordAttribute))
                {
                    DynamicKeyword keyword = ReadKeywordSpecification(typeDef, keywordAttribute.Value);
                    yield return keyword;
                }
            }
            yield break;
        }

        /// <summary>
        /// Read in a DynamicKeyword object from a dll specification, using the type definition and KeywordAttribute metadata on the class
        /// </summary>
        /// <param name="typeDef">the type definition defining the keyword</param>
        /// <param name="keywordAttribute">the attribute decorating the class specifying its properties</param>
        /// <param name="isNested">whether the keyword is nested inside another keyword or not</param>
        /// <returns></returns>
        private DynamicKeyword ReadKeywordSpecification(TypeDefinition typeDef, CustomAttribute keywordAttribute, bool isNested = false)
        {
            string keywordName = _metadataReader.GetString(typeDef.Name);

            // Make sure keywords by the same name are not already defined in enclosing scopes -- C# only prevents direct ancestors
            foreach (var enclosingScope in _keywordDefinitionStack)
            {
                if (enclosingScope.Contains(keywordName))
                {
                    var msg = String.Format("The keyword '{0}' is already defined in an enclosing scope", keywordName);
                    throw new RuntimeException(msg);
                }
            }

            // Now register the keyword name in the scope
            _keywordDefinitionStack.Peek().Add(keywordName);

            // Set the keyword properties -- note the defaults are set here, since reading metadata does not execute default attribute setters
            DynamicKeywordNameMode nameMode = KeywordAttribute.DefaultNameMode;
            DynamicKeywordBodyMode bodyMode = KeywordAttribute.DefaultBodyMode;
            ReadKeywordAttributeParameters(keywordAttribute, ref nameMode, ref bodyMode);

            // Read in enum definitions in the local scope
            _enumDefStack.Push(ReadEnumDefinitions(typeDef.GetNestedTypes().Select(tdHandle => _metadataReader.GetTypeDefinition(tdHandle)).Where(t => IsEnum(t))));

            // Set the current generic context
            IEnumerable<string> genericTypeParameterNames = typeDef.GetGenericParameters()
                .Select(t => _metadataReader.GetString(_metadataReader.GetGenericParameter(t).Name));

            _currentNamingGenericContext = new NamingGenericTypeContext(genericTypeParameterNames, ImmutableArray<string>.Empty);

            // Read in all parameters and properties
            var keywordParameters = new List<DynamicKeywordParameter>();
            var keywordProperties = new List<DynamicKeywordProperty>();
            foreach (var propertyHandle in typeDef.GetProperties())
            {
                var propertyDef = _metadataReader.GetPropertyDefinition(propertyHandle);
                foreach (var attributeHandle in propertyDef.GetCustomAttributes())
                {
                    var keywordMemberAttribute = _metadataReader.GetCustomAttribute(attributeHandle);

                    if (IsKeywordParameterAttribute(keywordMemberAttribute))
                    {
                        // Hashtable-bodied keyword cannot take parameters
                        if (bodyMode == DynamicKeywordBodyMode.Hashtable)
                        {
                            var msg = String.Format("Keyword '{0}' has a Hashtable body, but must use another body mode to take parameters", keywordName);
                        }
                        keywordParameters.Add(ReadParameterSpecification(propertyDef, keywordMemberAttribute));
                    }

                    if (IsKeywordPropertyAttribute(keywordMemberAttribute))
                    {
                        // Only Hashtable-bodied keywords can have properties
                        if (bodyMode != DynamicKeywordBodyMode.Hashtable)
                        {
                            var msg = String.Format("Keyword '{0}' has body mode '{1}', but must have a Hashtable body mode to have properties assigned", keywordName, bodyMode);
                            throw new RuntimeException(msg);
                        }
                        keywordProperties.Add(ReadPropertySpecifiction(propertyDef, keywordMemberAttribute));
                    }
                }
            }

            // Read in all nested keywords
            _keywordDefinitionStack.Push(new HashSet<string>());
            var innerKeywords = new List<DynamicKeyword>();
            foreach (var innerTypeDefHandle in typeDef.GetNestedTypes())
            {
                if (bodyMode == DynamicKeywordBodyMode.Command)
                {
                    var msg = String.Format("Keyword '{0}' is a command-bodied keyword, and cannot contain other keywords", keywordName);
                }

                var innerTypeDef = _metadataReader.GetTypeDefinition(innerTypeDefHandle);
                CustomAttribute? innerKeywordAttribute = null; 
                if (IsKeywordSpecification(innerTypeDef, ref innerKeywordAttribute))
                {
                    innerKeywords.Add(ReadKeywordSpecification(innerTypeDef, innerKeywordAttribute.Value, isNested: true));
                }
            }

            // Leave the definition scope of this keyword
            _keywordDefinitionStack.Pop();
            _enumDefStack.Pop();

            // Finally, construct the keyword -- this is designed so the keyword can be constructed as readonly
            var keyword = new DynamicKeyword()
            {
                Keyword = keywordName,
                NameMode = nameMode,
                BodyMode = bodyMode,
                ImplementingModule = _moduleInfo.Name,
                ImplementingModuleVersion = _moduleInfo.Version,
                ImplementingModuleInfo = _moduleInfo,
                IsReservedKeyword = false,
                HasReservedProperties = false,
                IsNested = isNested,
            };
            foreach (var parameter in keywordParameters)
            {
                keyword.Parameters.Add(parameter.Name, parameter);
            }
            foreach (var property in keywordProperties)
            {
                keyword.Properties.Add(property.Name, property);
            }
            foreach (var innerKeyword in innerKeywords)
            {
                keyword.InnerKeywords.Add(innerKeyword.Keyword, innerKeyword);
            }
            return keyword;
        }

        /// <summary>
        /// Read the parameters in a KeywordAttribute declaration, to pass on the the DynamicKeyword object that is created
        /// </summary>
        /// <param name="keywordAttribute">the KeywordAttribute metadata</param>
        /// <param name="nameMode">the name mode of the keyword</param>
        /// <param name="bodyMode">the body mode of the keyword</param>
        private void ReadKeywordAttributeParameters(CustomAttribute keywordAttribute, ref DynamicKeywordNameMode nameMode, ref DynamicKeywordBodyMode bodyMode)
        {
            CustomAttributeValue<Type> keywordValue = keywordAttribute.DecodeValue(_knownTypeTypeProvider);

            foreach (var attributeParameter in keywordValue.NamedArguments)
            {
                switch (attributeParameter.Name)
                {
                    case nameof(KeywordAttribute.Name):
                        nameMode = (DynamicKeywordNameMode)attributeParameter.Value;
                        break;

                    case nameof(KeywordAttribute.Body):
                            bodyMode = (DynamicKeywordBodyMode)attributeParameter.Value;
                            break;
                    }
                }
        }

        /// <summary>
        /// Read in a DynamicKeywordParameter object from dll metadata, using the C# property definition and its parameter attribute
        /// </summary>
        /// <param name="propertyDef"></param>
        /// <param name="keywordParameterAttribute"></param>
        /// <returns></returns>
        private DynamicKeywordParameter ReadParameterSpecification(PropertyDefinition propertyDef, CustomAttribute keywordParameterAttribute)
        {
            string parameterName = _metadataReader.GetString(propertyDef.Name);
            string parameterType = propertyDef.DecodeSignature(_namingTypeProvider, _currentNamingGenericContext).ReturnType;

            // Read properties set in the attribute
            bool mandatory = KeywordParameterAttribute.NotMandatory;
            CustomAttributeValue<Type> parameterAttribute = keywordParameterAttribute.DecodeValue(_knownTypeTypeProvider);
            foreach (var parameterParameter in parameterAttribute.NamedArguments)
            {
                switch (parameterParameter.Name)
                {
                    case nameof(KeywordParameterAttribute.Mandatory):
                        mandatory = (bool)parameterParameter.Value;
                        break;
                }
            }

            var keywordParameter = new DynamicKeywordParameter()
            {
                Name = parameterName,
                TypeConstraint = parameterType,
                Mandatory = mandatory,
                Switch = parameterType == nameof(SwitchParameter),
            };
            // If the parameter has an enum type, set the values it can take
            TrySetMemberEnumType(keywordParameter);
            return keywordParameter;
        }

        /// <summary>
        /// Read in a DynamicKeywordProperty object from dll metadata, using the C# property definition and its property attribute
        /// </summary>
        /// <param name="propertyDef">the property being read in as a dynamic keyword property</param>
        /// <param name="keywordPropertyAttribute">the attribute on the property defining it as a dynamic keyword property</param>
        /// <returns>a fully formed dynamic keyword property</returns>
        private DynamicKeywordProperty ReadPropertySpecifiction(PropertyDefinition propertyDef, CustomAttribute keywordPropertyAttribute)
        {
            string propertyName = _metadataReader.GetString(propertyDef.Name);
            string propertyType = propertyDef.DecodeSignature(_namingTypeProvider, _currentNamingGenericContext).ReturnType;

            // Read in properties set in the attribute
            bool mandatory = KeywordPropertyAttribute.NotMandatory;
            CustomAttributeValue<Type> propertyAttribute = keywordPropertyAttribute.DecodeValue(_knownTypeTypeProvider);
            foreach (var propertyParameter in propertyAttribute.NamedArguments)
            {
                switch (propertyParameter.Name)
                {
                    case nameof(KeywordPropertyAttribute.Mandatory):
                        mandatory = (bool)propertyParameter.Value;
                        break;
                }
            }

            var keywordProperty = new DynamicKeywordProperty()
            {
                Name = propertyName,
                TypeConstraint = propertyType,
                Mandatory = mandatory,
            };
            // If the property has an enum type, set its possible values
            TrySetMemberEnumType(keywordProperty);
            return keywordProperty;
        }

        /// <summary>
        /// Checks whether a type definition represents a keyword specification, and if so sets the
        /// <paramref name="keywordAttribute"/> parameter to the custom attribute on the type definition
        /// representing the KeywordAttribute
        /// </summary>
        /// <param name="typeDef">the type definition metadata to check</param>
        /// <param name="keywordAttribute">the custom attribute representing the KeywordAttribute</param>
        /// <returns>
        /// true if the type definition represents a keyword specification (and sets <paramref name="keywordAttribute"/>),
        /// otherwise false (and does not set <paramref name="keywordAttribute"/>)
        /// </returns>
        private bool IsKeywordSpecification(TypeDefinition typeDef, ref CustomAttribute? keywordAttribute)
        {
            // First, check the type definition inherits from Keyword
            EntityHandle baseTypeHandle = typeDef.BaseType;
            if (baseTypeHandle.IsNil)
            {
                return false;
            }
            switch (baseTypeHandle.Kind)
            {
                case HandleKind.TypeReference:
                    TypeReference typeRef = _metadataReader.GetTypeReference((TypeReferenceHandle)baseTypeHandle);
                    if (_metadataReader.GetString(typeRef.Name) != nameof(Keyword))
                    {
                        return false;
                    }
                    if (_metadataReader.GetString(typeRef.Namespace) != typeof(Keyword).Namespace)
                    {
                        return false;
                    }
                    break;

                default:
                    return false;
            }

            // Now make sure it carries the KeywordAttribute attribute
            foreach (CustomAttributeHandle attrHandle in typeDef.GetCustomAttributes())
            {
                CustomAttribute customAttribute = _metadataReader.GetCustomAttribute(attrHandle);
                if (IsKeywordAttribute(customAttribute))
                {
                    keywordAttribute = customAttribute;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check that the given attribute metadata represents a KeywordAttribute
        /// </summary>
        /// <param name="attribute">the attribute metadata to check</param>
        /// <returns>true if the attribute represents a KeywordAttribute, false otherwise</returns>
        private bool IsKeywordAttribute(CustomAttribute attribute)
        {
            return IsAttributeOfType(attribute, typeof(KeywordAttribute));
        }

        /// <summary>
        /// Check that the given attribute metadata represents a KeywordParameterAttribute
        /// </summary>
        /// <param name="attribute">the attribute metadata to check</param>
        /// <returns>true if the attribute rerpresents a KeywordParameterAttribute, false otherwise</returns>
        private bool IsKeywordParameterAttribute(CustomAttribute attribute)
        {
            return IsAttributeOfType(attribute, typeof(KeywordParameterAttribute));
        }

        /// <summary>
        /// Check that the given attribute metadata represents a KeywordPropertyAttribute
        /// </summary>
        /// <param name="attribute">the attribute metadata to check</param>
        /// <returns>true if the attribute represents a KeywordPropertyAttribute, false otherwise</returns>
        private bool IsKeywordPropertyAttribute(CustomAttribute attribute)
        {
            return IsAttributeOfType(attribute, typeof(KeywordPropertyAttribute));
        }

        /// <summary>
        /// Checks whether a metadata-read custom attribute is of a known type. This will not load the
        /// type of the attribute, but instead checks the name and namespace of the known type against
        /// the metadata of the read type
        /// </summary>
        /// <param name="attribute">the custom attribute whose type to check</param>
        /// <param name="type">the type that the attribute is checked against</param>
        /// <returns>true if the attribute is of the given type, false otherwise</returns>
        private bool IsAttributeOfType(CustomAttribute attribute, Type type)
        {
            switch (attribute.Constructor.Kind)
            {
                case HandleKind.MethodDefinition:
                    // BUG: System.Reflection.Metadata does not present the Parent of a MethodDefinition, so we cannot check.
                    // However, this only applies to attributes defined in the same file -- so we are safe unless there
                    // there is a custom attribute inheriting from one we are looking for. We could seal the Keyword attribute classes to prevent this.
                    return false;

                case HandleKind.MemberReference:
                    MemberReference member = _metadataReader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                    StringHandle typeName;
                    StringHandle typeNamespace;
                    switch (member.Parent.Kind)
                    {
                        case HandleKind.TypeReference:
                            TypeReference typeRef = _metadataReader.GetTypeReference((TypeReferenceHandle)member.Parent);
                            typeName = typeRef.Name;
                            typeNamespace = typeRef.Namespace;
                            break;

                        case HandleKind.TypeDefinition:
                            TypeDefinition typeDef = _metadataReader.GetTypeDefinition((TypeDefinitionHandle)member.Parent);
                            typeName = typeDef.Name;
                            typeNamespace = typeDef.Namespace;
                            break;

                        default:
                            return false;
                    }
                    return _metadataReader.GetString(typeName) == type.Name && _metadataReader.GetString(typeNamespace) == type.Namespace;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Look through all the enums defined so far up the scope stack for an enum with the given name, and return
        /// all the values defined by that enum if it exists
        /// </summary>
        /// <param name="enumTypeName">the name of the enum to look for in the dynamic keyword specification</param>
        /// <returns>IEnumerable of enum values, or null if no such enum is found</returns>
        private IEnumerable<string> GetEnumValues(string enumTypeName)
        {
            foreach (var enumScope in _enumDefStack)
            {
                if (enumScope.ContainsKey(enumTypeName))
                {
                    return enumScope[enumTypeName];
                }
            }

            return null;
        }

        /// <summary>
        /// Use the TypeConstraint property on a DynamicKeywordProperty to try and
        /// set the possible values for its type
        /// </summary>
        /// <param name="keywordProperty">the property to set the enumerated types on</param>
        /// <returns>true if the values were successfully set, false otherwise</returns>
        private bool TrySetMemberEnumType(DynamicKeywordProperty keywordProperty)
        {
            if (String.IsNullOrEmpty(keywordProperty.TypeConstraint))
            {
                return false;
            }

            IEnumerable<string> values = GetEnumValues(keywordProperty.TypeConstraint);
            if (values == null)
            {
                return false;
            }

            keywordProperty.Values.AddRange(values);
            return true;
        }

        /// <summary>
        /// Check whether a type definition describes an enum type or not
        /// </summary>
        /// <param name="typeDef">the type definition to check</param>
        /// <returns>true if the type definition inherits from enum, false otherwise</returns>
        private bool IsEnum(TypeDefinition typeDef)
        {
            var baseType = _metadataReader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
            return String.Join(".", _metadataReader.GetString(baseType.Namespace), _metadataReader.GetString(baseType.Name)) == nameof(System.Enum);
        }
    }

    #region Type Providers

    /// <summary>
    /// Provides type representations of metadata types
    /// </summary>
    /// <typeparam name="TType">the type to express type representations with</typeparam>
    /// <typeparam name="TGenericContext">the type that interprets generic types encountered in metadata</typeparam>
    internal interface ITypeProvider<TType, TGenericContext> : ISignatureTypeProvider<TType, TGenericContext>, ICustomAttributeTypeProvider<TType>
        where TGenericContext : IGenericContext<TType>
    {
    }

    /// <summary>
    /// Interprets generic types encountered in metadata
    /// </summary>
    /// <typeparam name="TType">the type with which to represent type metadata interpreted by the context</typeparam>
    internal interface IGenericContext<TType>
    {
        ImmutableArray<TType> TypeParameters { get; }
        ImmutableArray<TType> MethodParameters { get; }
    }

    /// <summary>
    /// A generic context that provides string representations of generic types read in as metadata
    /// </summary>
    internal struct NamingGenericTypeContext : IGenericContext<string>
    {
        private ImmutableArray<string> _typeParameters;
        private ImmutableArray<string> _methodParameters;

        /// <summary>
        /// Construct a new generic context from lists of known generic type parameters and method parameters
        /// </summary>
        /// <param name="typeParameters">generic type parameters defined in this context</param>
        /// <param name="methodParameters">generic method type parameters defined in this context</param>
        public NamingGenericTypeContext(IEnumerable<string> typeParameters, IEnumerable<string> methodParameters)
        {
            _typeParameters = typeParameters.ToImmutableArray();
            _methodParameters = methodParameters.ToImmutableArray();
        }

        /// <summary>
        /// Generic type parameters defined in this context
        /// </summary>
        public ImmutableArray<string> TypeParameters
        {
            get
            {
                return _typeParameters;
            }
        }

        /// <summary>
        /// Generic method type parameters defined in this context
        /// </summary>
        public ImmutableArray<string> MethodParameters
        {
            get
            {
                return _methodParameters;
            }
        }
    }

    /// <summary>
    /// A metadata type interpreter that renders metadata types as strings
    /// </summary>
    internal class NamingTypeProvider : ITypeProvider<string, IGenericContext<string>>
    {
        /// <summary>
        /// Get a string representation of an array of a given type and shape
        /// </summary>
        /// <param name="elementType">the string representation of the base type of the array</param>
        /// <param name="shape">the shape of the array</param>
        /// <returns>a string representing the type of the array</returns>
        public string GetArrayType(string elementType, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(elementType);
            builder.Append('[');

            for (int i = 0; i < shape.Rank; i++)
            {
                int lowerBound = 0;

                if (i < shape.LowerBounds.Length)
                {
                    lowerBound = shape.LowerBounds[i];
                    builder.Append(lowerBound);
                }

                builder.Append("...");

                if (i < shape.Sizes.Length)
                {
                    builder.Append(lowerBound + shape.Sizes[i] - 1);
                }

                if (i < shape.Rank - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');
            return builder.ToString();
        }

        /// <summary>
        /// Get a string representing a reference to the given type
        /// </summary>
        /// <param name="elementType">the base type of the referenced element</param>
        /// <returns>the string representation of a reference to the element type</returns>
        public string GetByReferenceType(string elementType)
        {
            return elementType + "&";
        }

        /// <summary>
        /// Get a string representing of a function pointer to a method with the given signature
        /// </summary>
        /// <param name="signature">the signature of the method being pointed to</param>
        /// <returns>the string representation of a function pointer to a method of the given signature</returns>
        public string GetFunctionPointerType(MethodSignature<string> signature)
        {
            ImmutableArray<string> parameterTypes = signature.ParameterTypes;

            int requiredParameterCount = signature.RequiredParameterCount;

            var builder = new StringBuilder();
            builder.Append("method ");
            builder.Append(signature.ReturnType);
            builder.Append(" *(");

            int i;
            for (i = 0; i < requiredParameterCount; i++)
            {
                builder.Append(parameterTypes[i]);
                if (i < parameterTypes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            if (i < parameterTypes.Length)
            {
                builder.Append("..., ");
                for (; i < parameterTypes.Length; i++)
                {
                    builder.Append(parameterTypes[i]);
                    if (i < parameterTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.Append(')');
            return builder.ToString();
        }

        /// <summary>
        /// Get a string representing an instantiation of a given generic type
        /// </summary>
        /// <param name="genericType">the representation of the generic type being instantiated</param>
        /// <param name="typeArguments">string representations of the type parameters of the generic type</param>
        /// <returns>the string representing an instantiation of the given generic type with the type arugments given</returns>
        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return genericType + "<" + String.Join(",", typeArguments) + ">";
        }

        /// <summary>
        /// Get a string representing an instantiation of a given generic method type parameter
        /// </summary>
        /// <param name="genericContext">the generic type context providing typing to the method signature type parameters</param>
        /// <param name="index">the index of the type parameter to represent</param>
        /// <returns>the string representation of the method type parameter given by the index</returns>
        public string GetGenericMethodParameter(IGenericContext<string> genericContext, int index)
        {
            return "!!" + genericContext.MethodParameters[index];
        }

        /// <summary>
        /// Get a string representing an instantiation of a given generic type parameter
        /// </summary>
        /// <param name="genericContext">the generic context in which this generic type is being instantiated</param>
        /// <param name="index">the index of the type parameter</param>
        /// <returns>the string representation of the generic type parameter at the given index</returns>
        public string GetGenericTypeParameter(IGenericContext<string> genericContext, int index)
        {
            return "!" + genericContext.TypeParameters[index];
        }

        /// <summary>
        /// Get a string representation of a modified type -- this is highly unlikely to be used
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="unmodifiedType"></param>
        /// <param name="isRequired"></param>
        /// <returns></returns>
        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
        {
            return unmodifiedType + (isRequired ? " modreq(" : " modopt(") + modifier + ")";
        }

        /// <summary>
        /// Get a string representation of a pinned type -- this is highly unlikely to be used
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>

        public string GetPinnedType(string elementType)
        {
            return elementType + " pinned";
        }

        /// <summary>
        /// Get a string representing a pointer type
        /// </summary>
        /// <param name="elementType">string representing the base type being pointed to</param>
        /// <returns>the string representation of a pointer to the given type</returns>
        public string GetPointerType(string elementType)
        {
            return elementType + "*";
        }

        /// <summary>
        /// Get a string representing a primitive dotnet type
        /// </summary>
        /// <param name="typeCode">the metadata type code representing the given primitive type</param>
        /// <returns>the short form C# string representation of the given primitive type</returns>
        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    return "bool";

                case PrimitiveTypeCode.Byte:
                    return "byte";

                case PrimitiveTypeCode.Char:
                    return "char";

                case PrimitiveTypeCode.Double:
                    return "double";

                case PrimitiveTypeCode.Int16:
                    return "short";

                case PrimitiveTypeCode.Int32:
                    return "int";

                case PrimitiveTypeCode.Int64:
                    return "long";

                case PrimitiveTypeCode.IntPtr:
                    return typeof(IntPtr).ToString();

                case PrimitiveTypeCode.Object:
                    return "object";

                case PrimitiveTypeCode.SByte:
                    return "sbyte";

                case PrimitiveTypeCode.Single:
                    return "float";

                case PrimitiveTypeCode.String:
                    return "string";

                case PrimitiveTypeCode.TypedReference:
                    throw new NotImplementedException("dotnet core does not implement TypedReference");

                case PrimitiveTypeCode.UInt16:
                    return "ushort";

                case PrimitiveTypeCode.UInt32:
                    return "uint";

                case PrimitiveTypeCode.UInt64:
                    return "ulong";

                case PrimitiveTypeCode.UIntPtr:
                    return typeof(UIntPtr).ToString();

                case PrimitiveTypeCode.Void:
                    return "void";

                default:
                    throw new ArgumentOutOfRangeException("Unrecognized primitive type");
            }
        }

        /// <summary>
        /// Get a string reprenting "System.Type"
        /// </summary>
        /// <returns>"System.Type"</returns>
        public string GetSystemType()
        {
            return typeof(System.Type).ToString();
        }

        /// <summary>
        /// Get a string representing a single-dimensional, zero-based array of the given element type
        /// </summary>
        /// <param name="elementType">the base type of elements in the array</param>
        /// <returns>the string representing an SZArray of elements of type <paramref name="elementType"/></returns>
        public string GetSZArrayType(string elementType)
        {
            return elementType + "[]";
        }

        /// <summary>
        /// Get a string representing a type read in from a type definition
        /// </summary>
        /// <param name="reader">the metadata reader containing the type definition data</param>
        /// <param name="handle">the handle to the given type definition</param>
        /// <param name="rawTypeKind"></param>
        /// <returns>the string representation of a type definition</returns>
        public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);

            string name = typeDef.Namespace.IsNil
                ? reader.GetString(typeDef.Name)
                : reader.GetString(typeDef.Namespace) + "." + reader.GetString(typeDef.Name);

            // Test if the typedef is nested -- future implementations of System.Reflection.Metadata have typeDef.Attributes.IsNested()
            if (typeDef.Attributes.HasFlag((System.Reflection.TypeAttributes)0x6))
            {
                TypeDefinitionHandle declaringTypeHandle = typeDef.GetDeclaringType();
                return GetTypeFromDefinition(reader, declaringTypeHandle) + "/" + name;
            }

            return name;
        }

        /// <summary>
        /// Get a string representing a type reference
        /// </summary>
        /// <param name="reader">the metadata reader containing the type reference data</param>
        /// <param name="handle">the handle to the given type reference</param>
        /// <param name="rawTypeKind"></param>
        /// <returns>the string representation of the given type reference</returns>
        public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            TypeReference reference = reader.GetTypeReference(handle);
            Handle scope = reference.ResolutionScope;

            string name = reference.Namespace.IsNil
                ? reader.GetString(reference.Name)
                : reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name);

            switch (scope.Kind)
            {
                case HandleKind.ModuleReference:
                    return "[.module" + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)scope).Name) + "]" + name;

                case HandleKind.AssemblyReference:
                    var assemblyReference = reader.GetAssemblyReference((AssemblyReferenceHandle)scope);
                    return "[" + reader.GetString(assemblyReference.Name) + "]" + name;

                case HandleKind.TypeReference:
                    return GetTypeFromReference(reader, (TypeReferenceHandle)scope) + "/" + name;

                default:
                    if (scope == Handle.ModuleDefinition || scope.IsNil)
                    {
                        return name;
                    }
                    throw new ArgumentOutOfRangeException("Unrecognized type handle scope reference");
            }
        }

        /// <summary>
        /// Get a string representing a serialized name
        /// </summary>
        /// <param name="name">the serialized name</param>
        /// <returns>the serialized name</returns>
        public string GetTypeFromSerializedName(string name)
        {
            return name;
        }

        /// <summary>
        /// Get a string representing a type specification
        /// </summary>
        /// <param name="reader">the metadata reader containing the type specification data</param>
        /// <param name="genericContext">the generic context in which the type specification occurs, providing generic type resolution</param>
        /// <param name="handle">a handle to the given type specification</param>
        /// <param name="rawTypeKind"></param>
        /// <returns>the string representation of the given type specification</returns>
        public string GetTypeFromSpecification(MetadataReader reader, IGenericContext<string> genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        }

        /// <summary>
        /// Get the underlying primitive type of an enum given its type name.
        /// Since this relies on us knowing about the enum in advance, it is only valid for enum types known here
        /// </summary>
        /// <param name="type">the name of the enum</param>
        /// <returns>the underlying primitive type of the enum</returns>
        public PrimitiveTypeCode GetUnderlyingEnumType(string type)
        {
            string typeName = type.Replace('/', '+');

            if (typeName == nameof(DynamicKeywordNameMode) || typeName == nameof(DynamicKeywordBodyMode))
            {
                return PrimitiveTypeCode.Int32;
            }

            throw new ArgumentOutOfRangeException("Unrecognized enumerated type");
        }

        /// <summary>
        /// Check whether a string represents "System.Type"
        /// </summary>
        /// <param name="type">the type name to check</param>
        /// <returns>true if <paramref name="type"/>represents <see cref="System.Type"/>, false otherwise</returns>
        public bool IsSystemType(string type)
        {
            return type == "[System.Runtime]System.Type" || Type.GetType(type) == typeof(Type);
        }
    }

    /// <summary>
    /// Type provider to translate the MetadataReader's decoded type into a Type. This is only implemented to the extent needed
    /// for resolving known parameter types such as <see cref="KeywordPropertyAttribute"/> and <see cref="DynamicKeywordBodyMode"/>;
    /// it is likely erroneous for more sophisticated types, especially those not from C#-compiled DLLs, so should not be used outside of
    /// this current restrictive usage
    /// </summary>
    internal class KnownTypeTypeProvider : ITypeProvider<Type, IGenericContext<Type>>
    {
        /// <summary>
        /// Gets type representation of an array based on its element type -- this throws an exception since we are not checking for known array types
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="shape"></param>
        /// <returns></returns>
        public Type GetArrayType(Type elementType, ArrayShape shape)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a type based on a reference to that type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public Type GetByReferenceType(Type elementType)
        {
            return elementType;
        }

        /// <summary>
        /// Get the type of a function pointer based on a typed methodsignature -- this throws an exception since we are not looking for known method signature types
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public Type GetFunctionPointerType(MethodSignature<Type> signature)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the type of a generic instantiation of a given type
        /// </summary>
        /// <param name="genericType"></param>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
        {
            string typeName = genericType.ToString() + "<" + String.Join(",", typeArguments.Select(t => t.ToString())) + ">";

            return Type.GetType(typeName);
        }

        /// <summary>
        /// Get the instantiated type of a generic method parameter
        /// </summary>
        /// <param name="genericContext"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Type GetGenericMethodParameter(IGenericContext<Type> genericContext, int index)
        {
            return genericContext.MethodParameters[index];
        }

        /// <summary>
        /// Get the type of a type parameter in a generic type instantiation
        /// </summary>
        /// <param name="genericContext"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Type GetGenericTypeParameter(IGenericContext<Type> genericContext, int index)
        {
            return genericContext.TypeParameters[index];
        }

        /// <summary>
        /// Get the type of a modified type -- this may not do what it promises, but we do not expect modified types
        /// </summary>
        /// <param name="modifier"></param>
        /// <param name="unmodifiedType"></param>
        /// <param name="isRequired"></param>
        /// <returns></returns>
        public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
        {
            return unmodifiedType;
        }

        /// <summary>
        /// Get the type of a pinned type -- we do not expect to see pinned types
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public Type GetPinnedType(Type elementType)
        {
            return elementType;
        }

        /// <summary>
        /// Get the type of a pointer to a type -- we do not expect pointer types
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public Type GetPointerType(Type elementType)
        {
            return elementType;
        }

        /// <summary>
        /// Get the Type representation corresponding to a primitive type code.
        /// TypedReferences are not supported in dotnetCore and will fail
        /// </summary>
        /// <param name="typeCode">the cil metadata type code of the value</param>
        /// <returns>a C# type corresponding to the given type code</returns>
        public Type GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean:
                    return typeof(bool);

                case PrimitiveTypeCode.Byte:
                    return typeof(byte);

                case PrimitiveTypeCode.Char:
                    return typeof(char);

                case PrimitiveTypeCode.Double:
                    return typeof(double);

                case PrimitiveTypeCode.Int16:
                    return typeof(short);

                case PrimitiveTypeCode.Int32:
                    return typeof(int);

                case PrimitiveTypeCode.Int64:
                    return typeof(long);

                case PrimitiveTypeCode.IntPtr:
                    return typeof(IntPtr);

                case PrimitiveTypeCode.Object:
                    return typeof(object);

                case PrimitiveTypeCode.SByte:
                    return typeof(sbyte);

                case PrimitiveTypeCode.Single:
                    return typeof(float);

                case PrimitiveTypeCode.String:
                    return typeof(string);

                case PrimitiveTypeCode.TypedReference:
                    throw new NotImplementedException("TypedReference not supported in dotnetCore");

                case PrimitiveTypeCode.UInt16:
                    return typeof(ushort);

                case PrimitiveTypeCode.UInt32:
                    return typeof(uint);

                case PrimitiveTypeCode.UInt64:
                    return typeof(ulong);

                case PrimitiveTypeCode.UIntPtr:
                    return typeof(UIntPtr);

                case PrimitiveTypeCode.Void:
                    return typeof(void);

                default:
                    throw new ArgumentOutOfRangeException("Unrecognized primitive type: " + typeCode.ToString());
            }
        }

        /// <summary>
        /// Get the Type representation of System.Type
        /// </summary>
        /// <returns></returns>
        public Type GetSystemType()
        {
            return typeof(Type);
        }

        /// <summary>
        /// Get the type representation of a simple array of a given type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        public Type GetSZArrayType(Type elementType)
        {
            return Type.GetType(elementType.ToString() + "[]");
        }

        /// <summary>
        /// Get the type of a type definition
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="handle"></param>
        /// <param name="rawTypeKind"></param>
        /// <returns></returns>
        public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind=0)
        {
            TypeDefinition typeDef = reader.GetTypeDefinition(handle);

            string typeDefName = reader.GetString(typeDef.Name);

            // Check if type definition is nested
            // This will be typeDef.Attributes.IsNested() in later releases
            if (typeDef.Attributes.HasFlag((System.Reflection.TypeAttributes)0x00000006))
            {
                TypeDefinitionHandle declaringTypeHandle = typeDef.GetDeclaringType();
                Type enclosingType = GetTypeFromDefinition(reader, declaringTypeHandle);
                return Type.GetType(Assembly.CreateQualifiedName(enclosingType.AssemblyQualifiedName, enclosingType.ToString() + "+" + typeDefName));
            }

            string typeDefNamespace = reader.GetString(typeDef.Namespace);
            return Type.GetType(Assembly.CreateQualifiedName(typeDefNamespace, typeDefName));
        }

        /// <summary>
        /// Get the type of a type reference
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="handle"></param>
        /// <param name="rawTypeKind"></param>
        /// <returns></returns>
        public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind=0)
        {
            TypeReference typeRef = reader.GetTypeReference(handle);
            string typeRefName = reader.GetString(typeRef.Name);
            if (typeRef.Namespace.IsNil)
            {
                return Type.GetType(typeRefName);
            }
            string typeRefNamespace = reader.GetString(typeRef.Namespace);
            return Type.GetType(Assembly.CreateQualifiedName(typeRefNamespace, typeRefName));
        }

        /// <summary>
        /// Get the type of a serialized type name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Type GetTypeFromSerializedName(string name)
        {
            return Type.GetType(name);
        }

        /// <summary>
        /// Get the type of a type specification
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="genericContext"></param>
        /// <param name="handle"></param>
        /// <param name="rawTypeKind"></param>
        /// <returns></returns>
        public Type GetTypeFromSpecification(MetadataReader reader, IGenericContext<Type> genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the underlying type of an enum type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public PrimitiveTypeCode GetUnderlyingEnumType(Type type)
        {
            Type underlyingType = type.GetEnumUnderlyingType();

            if (underlyingType == typeof(int))
            {
                return PrimitiveTypeCode.Int32;
            }
            else if (underlyingType == typeof(byte))
            {
                return PrimitiveTypeCode.Byte;
            }
            else if (underlyingType == typeof(long))
            {
                return PrimitiveTypeCode.Int64;
            }
            else if (underlyingType == typeof(short))
            {
                return PrimitiveTypeCode.Int16;
            }
            else if (underlyingType == typeof(char))
            {
                return PrimitiveTypeCode.Char;
            }

            throw new ArgumentOutOfRangeException("Not a known enum type");
        }

        /// <summary>
        /// Check whether a type is <see cref="System.Type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsSystemType(Type type)
        {
            return type == typeof(Type);
        }
    }

    #endregion /* Type Providers */

    #endregion /* Dynamic Keyword Metadata Reader */

    #region Dynamic Keyword Reflection Type Loader

    /// <summary>
    /// Loads runtime information from DynamicKeywords
    /// </summary>
    internal class DynamicKeywordLoader
    {
        // Global keywords to be loaded in (which may contain inner keywords to be loaded)
        private readonly ImmutableList<DynamicKeyword> _topLevelKeywordsToLoad;
        // Dictionary to record where a given keyword should be loaded from
        private IImmutableDictionary<DynamicKeyword, Assembly> _keywordAssemblies;

        /// <summary>
        /// Construct a fresh DynamicKeywordLoader around a list of keywords to load
        /// </summary>
        /// <param name="keywordsToLoad"></param>
        public DynamicKeywordLoader(IEnumerable<DynamicKeyword> keywordsToLoad)
        {
            _topLevelKeywordsToLoad = keywordsToLoad.ToImmutableList();
        }

        /// <summary>
        /// Perform the loading process
        /// </summary>
        public void Load()
        {
            // Make sure we do no loading if there are no keywords to load
            if (_topLevelKeywordsToLoad.Count == 0)
            {
                return;
            }

            ValidateKeywords();
            LoadModules();
            LoadKeywords();
        }

        /// <summary>
        /// Load the modules in, being careful not to load any twice and recording which keyword belongs to which module.
        /// Generally, there will only be one module -- but we want to be flexible
        /// </summary>
        private void LoadModules()
        {
            var loadedModules = new Dictionary<PSModuleInfo, Assembly>();
            var keywordAssemblies = new Dictionary<DynamicKeyword, Assembly>();
            // Go through the keywords we want to load and load their respective modules
            foreach (var keyword in _topLevelKeywordsToLoad)
            {
                if (!loadedModules.ContainsKey(keyword.ImplementingModuleInfo))
                {
                    loadedModules.Add(keyword.ImplementingModuleInfo, ClrFacade.LoadFrom(keyword.ImplementingModuleInfo.Path));
                }

                keywordAssemblies.Add(keyword, loadedModules[keyword.ImplementingModuleInfo]);
            }
            // Remember the assemblies we loaded so we can find types later
            _keywordAssemblies = keywordAssemblies.ToImmutableDictionary();
        }

        /// <summary>
        /// Once the assemblies are loaded, goes through the keywords and checks we can construct their defining commands.
        /// Then, when all keywords have been checked, constructs the keywords
        /// </summary>
        private void LoadKeywords()
        {
            var loadPairs = new List<KeyValuePair<DynamicKeyword, Keyword>>();

            // Go through all the keywords now the types are loaded and prepare the keyword for loading
            foreach (var keyword in _topLevelKeywordsToLoad)
            {
                PrepareKeywordLoad(keyword, loadPairs);
            }

            // Now everything is checked, we can load all the keywords without worrying about partial state mutation
            foreach (var loadPair in loadPairs)
            {
                DynamicKeyword keyword = loadPair.Key;
                keyword.PreParse = loadPair.Value.PreParse;
                keyword.PostParse = loadPair.Value.PostParse;
                keyword.SemanticCheck = loadPair.Value.SemanticCheck;
            }
        }

        /// <summary>
        /// Check there is a type in the module with the same name as the keyword, and that it is of type <see cref="Keyword"/>,
        /// then add the <see cref="DynamicKeyword"/>/<see cref="Keyword"/> pair to the list for delegate loading
        /// </summary>
        /// <param name="keyword">the dynamic keyword to prepare for loading</param>
        /// <param name="loadPairs">the list of dynamic keyword/keyword instance pairs to add to</param>
        private void PrepareKeywordLoad(DynamicKeyword keyword, List<KeyValuePair<DynamicKeyword, Keyword>> loadPairs)
        {
            // Make sure there is a keyword instance type defined as we expect
            Type definingType = _keywordAssemblies[keyword].GetType(keyword.Keyword);
            if (definingType == null)
            {
                var msg = String.Format("The keyword '{0}' could not be loaded from the assembly at '{1}' -- check that it has not been modified", keyword, _keywordAssemblies[keyword].Location);
                throw new RuntimeException(msg);
            }

            // Now check the keyword instance type actually inherits from Keyword
            Keyword definitionInstance = Activator.CreateInstance(definingType) as Keyword;
            if (definitionInstance == null)
            {
                var msg = String.Format("The keyword instance for '{0}' does not inherit from the required type '{1}'", keyword, nameof(Keyword));
                throw new RuntimeException(msg);
            }

            // Finally, add the pair for loading
            loadPairs.Add(new KeyValuePair<DynamicKeyword, Keyword>(keyword, definitionInstance));

            // Now recurse to child keywords
            foreach (var innerKeyword in keyword.InnerKeywords.Values)
            {
                PrepareKeywordLoad(innerKeyword, loadPairs);
            }
        }

        /// <summary>
        /// Perform validation on keywords before loading any assemblies so that we can try to ensure everything will work
        /// before doing something irreversible. This employs a number of closures.
        /// </summary>
        private void ValidateKeywords()
        {
            // Check that all keywords are defined in the same module as their parents
            PSModuleInfo topLevelKeywordModule = null;
            DynamicKeyword parent = null;
            Action<DynamicKeyword> moduleInfoChecker = keyword =>
            {
                if (keyword.ImplementingModuleInfo != topLevelKeywordModule)
                {
                    var msg = String.Format("Keyword '{0}' has a different moduleInfo to its top level keyword '{1}'", keyword, parent);
                    throw new RuntimeException(msg);
                }
            };

            // Check that there are no cycles in the DynamicKeyword definition tree -- this may be an expensive check, and unlikely to occur
            var seenKeywords = new HashSet<DynamicKeyword>();
            Action<DynamicKeyword> cycleChecker = keyword =>
            {
                if (seenKeywords.Contains(keyword))
                {
                    var msg = String.Format("Keyword '{0}' contains a cyclic reference to itself", keyword);
                    throw new RuntimeException(msg);
                }
                seenKeywords.Add(keyword);
            };

            foreach (var keyword in _topLevelKeywordsToLoad)
            {
                // Also make sure the parent keywords have a module
                if (keyword.ImplementingModuleInfo == null)
                {
                    var msg = String.Format("Keyword '{0}' does not have a module defined and cannot be loaded", keyword);
                    throw new RuntimeException(msg);
                }

                // Perform the checks
                topLevelKeywordModule = keyword.ImplementingModuleInfo;
                parent = keyword;
                VisitKeywordChildren(keyword, new[] { moduleInfoChecker, cycleChecker });
                seenKeywords = new HashSet<DynamicKeyword>();
            }
        }

        /// <summary>
        /// Visit a keyword and all its children with a list of delegates, executed in enumeration order
        /// </summary>
        /// <param name="keyword">the top keyword to visit</param>
        /// <param name="visitors">the action delegates to execute on the keywords</param>
        private void VisitKeywordChildren(DynamicKeyword keyword, IEnumerable<Action<DynamicKeyword>> visitors)
        {
            foreach (var visitor in visitors)
            {
                visitor(keyword);
            }
            foreach (var innerKeyword in keyword.InnerKeywords.Values)
            {
                VisitKeywordChildren(innerKeyword, visitors);
            }
        }
    }

    #endregion /* Dynamic Keyword Reflection Type Loader */
}