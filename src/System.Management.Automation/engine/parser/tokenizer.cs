// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.PowerShell.Commands;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.DSC;
using Microsoft.PowerShell.DesiredStateConfiguration.Internal;

namespace System.Management.Automation.Language
{
    /// <summary>
    /// Defines the name modes for a dynamic keyword. A name expression may be required, optional or not permitted.
    /// </summary>
    public enum DynamicKeywordNameMode
    {
        /// <summary>
        /// This keyword does not take a name value.
        /// </summary>
        NoName = 0,
        /// <summary>
        /// Name must be present and simple non-empty bare word.
        /// </summary>
        SimpleNameRequired = 1,
        /// <summary>
        /// Name must be present but can also be an expression.
        /// </summary>
        NameRequired = 2,
        /// <summary>
        /// Name may be optionally present, but if it is present, it must be a non-empty bare word.
        /// </summary>
        SimpleOptionalName = 3,
        /// <summary>
        /// Name may be optionally present, expression or bare word.
        /// </summary>
        OptionalName = 4,
    }

    /// <summary>
    /// Defines the body mode for a dynamic keyword. It can be a scriptblock, hashtable or command which means no body.
    /// </summary>
    public enum DynamicKeywordBodyMode
    {
        /// <summary>
        /// The keyword act like a command.
        /// </summary>
        Command = 0,
        /// <summary>
        /// The keyword has a scriptblock body.
        /// </summary>
        ScriptBlock = 1,
        /// <summary>
        /// The keyword has hashtable body.
        /// </summary>
        Hashtable = 2,
    }

    /// <summary>
    /// Defines the schema/behaviour for a dynamic keyword.
    /// a constrained.
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
                return t_dynamicKeywords ??= new Dictionary<string, DynamicKeyword>(StringComparer.OrdinalIgnoreCase);
            }
        }

        [ThreadStatic]
        private static Dictionary<string, DynamicKeyword> t_dynamicKeywords;

        /// <summary>
        /// Stack of DynamicKeywords Cache.
        /// </summary>
        private static Stack<Dictionary<string, DynamicKeyword>> DynamicKeywordsStack
        {
            get
            {
                return t_dynamicKeywordsStack ??= new Stack<Dictionary<string, DynamicKeyword>>();
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
        /// Push current dynamicKeywords cache into stack.
        /// </summary>
        public static void Push()
        {
            DynamicKeywordsStack.Push(t_dynamicKeywords);
            Reset();
        }

        /// <summary>
        /// Pop up previous dynamicKeywords cache.
        /// </summary>
        public static void Pop()
        {
            t_dynamicKeywords = DynamicKeywordsStack.Pop();
        }

        /// <summary>
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
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool ContainsKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException(nameof(name));
                throw e;
            }

            return DynamicKeyword.DynamicKeywords.ContainsKey(name);
        }

        /// <summary>
        /// </summary>
        /// <param name="keywordToAdd"></param>
        public static void AddKeyword(DynamicKeyword keywordToAdd)
        {
            if (keywordToAdd == null)
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException(nameof(keywordToAdd));
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
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException(nameof(name));
                throw e;
            }

            DynamicKeyword.DynamicKeywords.Remove(name);
        }

        /// <summary>
        /// Check if it is a hidden keyword.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool IsHiddenKeyword(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSArgumentNullException e = PSTraceSource.NewArgumentNullException(nameof(name));
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
        /// Duplicates the DynamicKeyword.
        /// </summary>
        /// <returns>A copy of the DynamicKeyword.</returns>
        public DynamicKeyword Copy()
        {
            DynamicKeyword keyword = new DynamicKeyword()
            {
                ImplementingModule = this.ImplementingModule,
                ImplementingModuleVersion = this.ImplementingModuleVersion,
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
                SemanticCheck = this.SemanticCheck
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
        /// The keyword string
        /// If an alias qualifier exist, use alias.
        /// </summary>
        public string Keyword { get; set; }

        /// <summary>
        /// The keyword resource name string.
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Set to true if we should be looking for a scriptblock instead of a hashtable.
        /// </summary>
        public DynamicKeywordBodyMode BodyMode { get; set; }

        /// <summary>
        /// If true, then don't use the marshalled call. Just
        /// rewrite the node as a simple direct function call.
        /// If NameMode is other than NoName, then the name of the instance
        /// will be passed as the parameter -InstanceName.
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
        /// Indicate that the keyword is reserved for future use by powershell.
        /// </summary>
        public bool IsReservedKeyword { get; set; }

        /// <summary>
        /// Contains the list of properties that are reserved for future use.
        /// </summary>
        public bool HasReservedProperties { get; set; }

        /// <summary>
        /// A list of the properties allowed for this constructor.
        /// </summary>
        public Dictionary<string, DynamicKeywordProperty> Properties
        {
            get
            {
                return _properties ??= new Dictionary<string, DynamicKeywordProperty>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private Dictionary<string, DynamicKeywordProperty> _properties;

        /// <summary>
        /// A list of the parameters allowed for this constructor.
        /// </summary>
        public Dictionary<string, DynamicKeywordParameter> Parameters
        {
            get
            {
                return _parameters ??= new Dictionary<string, DynamicKeywordParameter>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private Dictionary<string, DynamicKeywordParameter> _parameters;

        /// <summary>
        /// A custom function that gets executed at parsing time before parsing dynamickeyword block
        /// The delegate has one parameter: DynamicKeyword.
        /// </summary>
        public Func<DynamicKeyword, ParseError[]> PreParse { get; set; }

        /// <summary>
        /// A custom function that gets executed at parsing time after parsing dynamickeyword block.
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
                ICrossPlatformDsc dscSubsystem = SubsystemManager.GetSubsystem<ICrossPlatformDsc>();
                if (dscSubsystem != null)
                {
                    dscSubsystem.IsDefaultModuleNameForMetaConfigResource(implementingModule);
                }
                else
                {
                    return implementingModule.Equals(DscClassCache.DefaultModuleInfoForMetaConfigResource.Item1, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        internal static bool IsCompatibleWithConfigurationType(this DynamicKeyword keyword, ConfigurationType ConfigurationType)
        {
            return ((ConfigurationType == ConfigurationType.Meta && keyword.IsMetaDSCResource()) ||
                    (ConfigurationType != ConfigurationType.Meta && !keyword.IsMetaDSCResource()));
        }

        private static readonly Dictionary<string, List<string>> s_excludeKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            {@"Node", new List<string> {@"Node"}},
        };

        /// <summary>
        /// Get allowed keyword list for a given keyword.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="allowedKeywords"></param>
        /// <returns>NULL if no keyword allowed for a given <see cref="DynamicKeyword"/></returns>
        internal static IEnumerable<DynamicKeyword> GetAllowedKeywords(this DynamicKeyword keyword, IEnumerable<DynamicKeyword> allowedKeywords)
        {
            string keywordName = keyword.Keyword;
            if (string.Equals(keywordName, @"Node", StringComparison.OrdinalIgnoreCase))
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
        /// The name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The required type of the property.
        /// </summary>
        public string TypeConstraint { get; set; }

        /// <summary>
        /// Any attributes that the property has.
        /// </summary>
        public List<string> Attributes
        {
            get { return _attributes ??= new List<string>(); }
        }

        private List<string> _attributes;

        /// <summary>
        /// List of strings that may be used as values for this property.
        /// </summary>
        public List<string> Values
        {
            get { return _values ??= new List<string>(); }
        }

        private List<string> _values;

        /// <summary>
        /// Mapping the descriptive values to the actual values.
        /// </summary>
        public Dictionary<string, string> ValueMap
        {
            get { return _valueMap ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
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
        /// Indicates a range constraint on the property value.
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
        /// Type if this is a switch parameter and takes no argument.
        /// </summary>
        public bool Switch { get; set; }
    }

    internal enum TokenizerMode
    {
        Command,
        Expression,
        TypeName,
        Signature, // i.e. class or method declaration
    }

    /// <summary>
    /// Indicates which suffix character(s) are present in the numeric literal being parsed by TryGetNumberValue.
    /// </summary>
    [Flags]
    internal enum NumberSuffixFlags
    {
        /// <summary>
        /// Indicates no suffix, a raw numeric literal. May be parsed as Int32, Int64, or Double.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Indicates 'u' suffix for unsigned integers. May be parsed as UInt32 or UInt64, depending on the value.
        /// </summary>
        Unsigned = 0x1,

        /// <summary>
        /// Indicates 'y' suffix for signed byte (sbyte) values.
        /// </summary>
        SignedByte = 0x2,

        /// <summary>
        /// Indicates 'uy' suffix for unsigned byte values.
        /// This is a compound value, representing both SignedByte and Unsigned flags being set.
        /// </summary>
        UnsignedByte = 0x3,

        /// <summary>
        /// Indicates 's' suffix for short (Int16) integers.
        /// </summary>
        Short = 0x4,

        /// <summary>
        /// Indicates 'us' suffix for ushort (UInt16) integers.
        /// This is a compound flag value, representing both Unsigned and Short flags being set.
        /// </summary>
        UnsignedShort = 0x5,

        /// <summary>
        /// Indicates 'l' suffix for long (Int64) integers.
        /// </summary>
        Long = 0x8,

        /// <summary>
        /// Indicates 'ul' suffix for ulong (UInt64) integers.
        /// This is a compound flag value, representing both Unsigned and Long flags being set.
        /// </summary>
        UnsignedLong = 0x9,

        /// <summary>
        /// Indicates 'd' suffix for decimal (128-bit) real numbers.
        /// </summary>
        Decimal = 0x10,

        /// <summary>
        /// Indicates 'N' suffix for BigInteger (arbitrarily large integer) numerals.
        /// </summary>
        BigInteger = 0x20
    }

    /// <summary>
    /// Indicates the format of a numeric literal.
    /// </summary>
    internal enum NumberFormat
    {
        /// <summary>
        /// Indicates standard decimal literal, no necessary prefix.
        /// </summary>
        Decimal = 0x0,

        /// <summary>
        /// Indicates hexadecimal literal, with '0x' prefix.
        /// </summary>
        Hex = 0x1,

        /// <summary>
        /// Indicates binary literal, with '0b' prefix.
        /// </summary>
        Binary = 0x2
    }

    //
    // Class used to do a partial snapshot of the state of the tokenizer.
    // This is used for nested scans on the same string.
    //
    internal class TokenizerState
    {
        internal int NestedTokensAdjustment;
        internal string Script;
        internal int TokenStart;
        internal int CurrentIndex;
        internal Token FirstToken;
        internal Token LastToken;
        internal BitArray SkippedCharOffsets;
        internal List<Token> TokenList;
    }

    [DebuggerDisplay("Mode = {Mode}; Script = {_script}")]
    internal class Tokenizer
    {
        private static readonly Dictionary<string, TokenKind> s_keywordTable
            = new Dictionary<string, TokenKind>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, TokenKind> s_operatorTable
            = new Dictionary<string, TokenKind>(StringComparer.OrdinalIgnoreCase);

        private static readonly char s_invalidChar = char.MaxValue;
        private static readonly int s_maxNumberOfUnicodeHexDigits = 6;

        private readonly Parser _parser;
        private PositionHelper _positionHelper;
        private int _nestedTokensAdjustment;

        // This BitArray is used to help ensure we get the correct extent in a corner case that looks something like:
        //     $(""abc"")
        // In the above, we scan the characters between the parens once and create a token that is scanned later. This
        // first pass scan replaces doubled quotes with single quotes so that a subsequent scan will see: "abc" instead
        // of: ""abc"".  (This isn't really necessary, but is done for backwards compatibility.)  If we didn't track
        // the skipped quotes, we'd generate incorrect extent of the string token on subsequent rescans.
        private BitArray _skippedCharOffsets;

        private string _script;
        private int _tokenStart;
        private int _currentIndex;
        private InternalScriptExtent _beginSignatureExtent;

        #region Tables for initialization

        private static readonly string[] s_keywordText = new string[] {
        /*1*/    "elseif",                  "if",               "else",             "switch",                     /*1*/
        /*2*/    "foreach",                 "from",             "in",               "for",                        /*2*/
        /*3*/    "while",                   "until",            "do",               "try",                        /*3*/
        /*4*/    "catch",                   "finally",          "trap",             "data",                       /*4*/
        /*5*/    "return",                  "continue",         "break",            "exit",                       /*5*/
        /*6*/    "throw",                   "begin",            "process",          "end",                        /*6*/
        /*7*/    "dynamicparam",            "function",         "filter",           "param",                      /*7*/
        /*8*/    "class",                   "define",           "var",              "using",                      /*8*/
        /*9*/    "workflow",                "parallel",         "sequence",         "inlinescript",               /*9*/
        /*A*/    "configuration",           "public",           "private",          "static",                     /*A*/
        /*B*/    "interface",               "enum",             "namespace",        "module",                     /*B*/
        /*C*/    "type",                    "assembly",         "command",          "hidden",                     /*C*/
        /*D*/    "base",                    "default",          "clean",                                          /*D*/
        };

        private static readonly TokenKind[] s_keywordTokenKind = new TokenKind[] {
        /*1*/    TokenKind.ElseIf,          TokenKind.If,       TokenKind.Else,      TokenKind.Switch,             /*1*/
        /*2*/    TokenKind.Foreach,         TokenKind.From,     TokenKind.In,        TokenKind.For,                /*2*/
        /*3*/    TokenKind.While,           TokenKind.Until,    TokenKind.Do,        TokenKind.Try,                /*3*/
        /*4*/    TokenKind.Catch,           TokenKind.Finally,  TokenKind.Trap,      TokenKind.Data,               /*4*/
        /*5*/    TokenKind.Return,          TokenKind.Continue, TokenKind.Break,     TokenKind.Exit,               /*5*/
        /*6*/    TokenKind.Throw,           TokenKind.Begin,    TokenKind.Process,   TokenKind.End,                /*6*/
        /*7*/    TokenKind.Dynamicparam,    TokenKind.Function, TokenKind.Filter,    TokenKind.Param,              /*7*/
        /*8*/    TokenKind.Class,           TokenKind.Define,   TokenKind.Var,       TokenKind.Using,              /*8*/
        /*9*/    TokenKind.Workflow,        TokenKind.Parallel, TokenKind.Sequence,  TokenKind.InlineScript,       /*9*/
        /*A*/    TokenKind.Configuration,   TokenKind.Public,   TokenKind.Private,   TokenKind.Static,             /*A*/
        /*B*/    TokenKind.Interface,       TokenKind.Enum,     TokenKind.Namespace, TokenKind.Module,             /*B*/
        /*C*/    TokenKind.Type,            TokenKind.Assembly, TokenKind.Command,   TokenKind.Hidden,             /*C*/
        /*D*/    TokenKind.Base,            TokenKind.Default,  TokenKind.Clean,                                   /*D*/
        };

        internal static readonly string[] _operatorText = new string[] {
        /*1*/   "bnot",                 "not",                  "eq",                   "ieq",                    /*1*/
        /*2*/   "ceq",                  "ne",                   "ine",                  "cne",                    /*2*/
        /*3*/   "ge",                   "ige",                  "cge",                  "gt",                     /*3*/
        /*4*/   "igt",                  "cgt",                  "lt",                   "ilt",                    /*4*/
        /*5*/   "clt",                  "le",                   "ile",                  "cle",                    /*5*/
        /*6*/   "like",                 "ilike",                "clike",                "notlike",                /*6*/
        /*7*/   "inotlike",             "cnotlike",             "match",                "imatch",                 /*7*/
        /*8*/   "cmatch",               "notmatch",             "inotmatch",            "cnotmatch",              /*8*/
        /*9*/   "replace",              "ireplace",             "creplace",             "contains",               /*9*/
        /*10*/  "icontains",            "ccontains",            "notcontains",          "inotcontains",           /*10*/
        /*11*/  "cnotcontains",         "in",                   "iin",                  "cin",                    /*11*/
        /*12*/  "notin",                "inotin",               "cnotin",               "split",                  /*12*/
        /*13*/  "isplit",               "csplit",               "isnot",                "is",                     /*13*/
        /*14*/  "as",                   "f",                    "and",                  "band",                   /*14*/
        /*15*/  "or",                   "bor",                  "xor",                  "bxor",                   /*15*/
        /*16*/  "join",                 "shl",                  "shr",                                            /*16*/
        };

        private static readonly TokenKind[] s_operatorTokenKind = new TokenKind[] {
        /*1*/   TokenKind.Bnot,         TokenKind.Not,          TokenKind.Ieq,          TokenKind.Ieq,            /*1*/
        /*2*/   TokenKind.Ceq,          TokenKind.Ine,          TokenKind.Ine,          TokenKind.Cne,            /*2*/
        /*3*/   TokenKind.Ige,          TokenKind.Ige,          TokenKind.Cge,          TokenKind.Igt,            /*3*/
        /*4*/   TokenKind.Igt,          TokenKind.Cgt,          TokenKind.Ilt,          TokenKind.Ilt,            /*4*/
        /*5*/   TokenKind.Clt,          TokenKind.Ile,          TokenKind.Ile,          TokenKind.Cle,            /*5*/
        /*6*/   TokenKind.Ilike,        TokenKind.Ilike,        TokenKind.Clike,        TokenKind.Inotlike,       /*6*/
        /*7*/   TokenKind.Inotlike,     TokenKind.Cnotlike,     TokenKind.Imatch,       TokenKind.Imatch,         /*7*/
        /*8*/   TokenKind.Cmatch,       TokenKind.Inotmatch,    TokenKind.Inotmatch,    TokenKind.Cnotmatch,      /*8*/
        /*9*/   TokenKind.Ireplace,     TokenKind.Ireplace,     TokenKind.Creplace,     TokenKind.Icontains,      /*9*/
        /*10*/  TokenKind.Icontains,    TokenKind.Ccontains,    TokenKind.Inotcontains, TokenKind.Inotcontains,   /*10*/
        /*11*/  TokenKind.Cnotcontains, TokenKind.Iin,          TokenKind.Iin,          TokenKind.Cin,            /*11*/
        /*12*/  TokenKind.Inotin,       TokenKind.Inotin,       TokenKind.Cnotin,       TokenKind.Isplit,         /*12*/
        /*13*/  TokenKind.Isplit,       TokenKind.Csplit,       TokenKind.IsNot,        TokenKind.Is,             /*13*/
        /*14*/  TokenKind.As,           TokenKind.Format,       TokenKind.And,          TokenKind.Band,           /*14*/
        /*15*/  TokenKind.Or,           TokenKind.Bor,          TokenKind.Xor,          TokenKind.Bxor,           /*15*/
        /*16*/  TokenKind.Join,         TokenKind.Shl,          TokenKind.Shr,                                    /*16*/
        };

        #endregion Tables for initialization

        static Tokenizer()
        {
            Diagnostics.Assert(s_keywordText.Length == s_keywordTokenKind.Length, "Keyword table sizes must match");
            Diagnostics.Assert(_operatorText.Length == s_operatorTokenKind.Length, "Operator table sizes must match");

            for (int i = 0; i < s_keywordText.Length; ++i)
            {
                s_keywordTable.Add(s_keywordText[i], s_keywordTokenKind[i]);
            }

            for (int i = 0; i < _operatorText.Length; ++i)
            {
                s_operatorTable.Add(_operatorText[i], s_operatorTokenKind[i]);
            }

            // The real signature (in mshsip.cpp) has spaces, but we ignore whitespace when looking
            // for signatures because we only care about things that look like a signature.
            // The hash we compute is intentionally dumb, we want collisions to catch similar strings,
            // so we just sum up the characters.
            const string beginSig = "sig#beginsignatureblock";
            beginSig.Aggregate(0, static (current, t) => current + t);

            // Spot check to help make sure the arrays are in sync
            Diagnostics.Assert(s_keywordTable["using"] == TokenKind.Using, "Keyword table out of sync w/ enum");
            Diagnostics.Assert(s_operatorTable["join"] == TokenKind.Join, "Operator table out of sync w/ enum");
        }

        internal Tokenizer(Parser parser)
        {
            _parser = parser;
        }

        internal TokenizerMode Mode { get; set; }

        internal bool AllowSignedNumbers { get; set; }

        // TODO: use auto-properties when making 'ternary operator' an official feature.
        private bool _forceEndNumberOnTernaryOpChars;

        internal bool ForceEndNumberOnTernaryOpChars
        {
            get { return _forceEndNumberOnTernaryOpChars; }
            set { _forceEndNumberOnTernaryOpChars = value; }
        }

        internal bool WantSimpleName { get; set; }

        internal bool InWorkflowContext { get; set; }

        internal List<Token> TokenList { get; set; }

        internal Token FirstToken { get; private set; }

        internal Token LastToken { get; private set; }

        private List<Token> RequiresTokens { get; set; }

        private bool InCommandMode() { return Mode == TokenizerMode.Command; }

        private bool InExpressionMode() { return Mode == TokenizerMode.Expression; }

        private bool InTypeNameMode() { return Mode == TokenizerMode.TypeName; }

        private bool InSignatureMode() { return Mode == TokenizerMode.Signature; }

        internal void Initialize(string fileName, string input, List<Token> tokenList)
        {
            _positionHelper = new PositionHelper(fileName, input);
            _script = input;
            this.TokenList = tokenList;
            this.FirstToken = null;
            this.LastToken = null;
            this.RequiresTokens = null;
            _beginSignatureExtent = null;

            List<int> lineStartMap = new List<int>(100) { 0 };
            for (int i = 0; i < input.Length; ++i)
            {
                char c = input[i];

                if (c == '\r')
                {
                    if ((i + 1) < input.Length && input[i + 1] == '\n')
                    {
                        i += 1;
                    }

                    lineStartMap.Add(i + 1);
                }

                if (c == '\n')
                {
                    lineStartMap.Add(i + 1);
                }
            }

            _currentIndex = 0;
            Mode = TokenizerMode.Command;

            _positionHelper.LineStartMap = lineStartMap.ToArray();
        }

        internal TokenizerState StartNestedScan(UnscannedSubExprToken nestedText)
        {
            TokenizerState ts = new TokenizerState
            {
                CurrentIndex = _currentIndex,
                NestedTokensAdjustment = _nestedTokensAdjustment,
                Script = _script,
                TokenStart = _tokenStart,
                FirstToken = FirstToken,
                LastToken = LastToken,
                SkippedCharOffsets = _skippedCharOffsets,
                TokenList = TokenList,
            };

            _currentIndex = 0;
            _nestedTokensAdjustment = ((InternalScriptExtent)nestedText.Extent).StartOffset;
            _script = nestedText.Value;
            _tokenStart = 0;
            _skippedCharOffsets = nestedText.SkippedCharOffsets;
            TokenList = (TokenList != null) ? new List<Token>() : null;

            return ts;
        }

        internal void FinishNestedScan(TokenizerState ts)
        {
            _currentIndex = ts.CurrentIndex;
            _nestedTokensAdjustment = ts.NestedTokensAdjustment;
            _script = ts.Script;
            _tokenStart = ts.TokenStart;
            FirstToken = ts.FirstToken;
            LastToken = ts.LastToken;
            _skippedCharOffsets = ts.SkippedCharOffsets;
            TokenList = ts.TokenList;
        }

        #region Utilities

        private char GetChar()
        {
            Diagnostics.Assert(_currentIndex >= 0, "GetChar reading before start of input.");
            Diagnostics.Assert(_currentIndex <= _script.Length + 1, "GetChar reading after end of input.");

            // Increment _currentIndex, even if it goes over the Length so callers can call UngetChar to unget EOF.
            int current = _currentIndex++;
            if (current >= _script.Length)
            {
                return '\0';
            }

            return _script[current];
        }

        private void UngetChar()
        {
            Diagnostics.Assert(_currentIndex > 0, "UngetChar ungetting before start of input.");

            _currentIndex -= 1;
        }

        private char PeekChar()
        {
            Diagnostics.Assert(_currentIndex >= 0 && _currentIndex <= _script.Length, "PeekChar out of range.");

            if (_currentIndex == _script.Length)
            {
                return '\0';
            }

            return _script[_currentIndex];
        }

        private void SkipChar()
        {
            Diagnostics.Assert((_currentIndex + 1) <= _script.Length, "SkipChar can't skip past EOF");

            _currentIndex += 1;
        }

        private bool AtEof()
        {
            return _currentIndex > _script.Length;
        }

        internal static bool IsKeyword(string str)
        {
            if (s_keywordTable.ContainsKey(str))
            {
                return true;
            }

            if (DynamicKeyword.ContainsKeyword(str) && !DynamicKeyword.IsHiddenKeyword(str))
            {
                return true;
            }

            return false;
        }

        internal void SkipNewlines(bool skipSemis)
        {
        // We normally don't create any tokens in a Skip method, but the
        // V2 tokenizer api returns newline, semi-colon, and line
        // continuation tokens so we create them as they are encountered.
        again:
            char c = GetChar();
            switch (c)
            {
                case ' ':
                case '\t':
                case '\f':
                case '\v':
                case SpecialChars.NoBreakSpace:
                case SpecialChars.NextLine:
                    SkipWhiteSpace();
                    goto again;

                case '\r':
                case '\n':
                    ScanNewline(c);
                    goto again;

                case ';':
                    if (skipSemis)
                    {
                        ScanSemicolon();
                        goto again;
                    }

                    break;

                case '#':
                    _tokenStart = _currentIndex - 1;
                    ScanLineComment();
                    goto again;

                case '<':
                    if (PeekChar() == '#')
                    {
                        _tokenStart = _currentIndex - 1;
                        SkipChar();
                        ScanBlockComment();
                        goto again;
                    }

                    break;

                case '`':
                    char c1 = GetChar();
                    if (c1 == '\n' || c1 == '\r')
                    {
                        ScanLineContinuation(c1);
                        goto again;
                    }

                    if (char.IsWhiteSpace(c1))
                    {
                        SkipWhiteSpace();
                        goto again;
                    }

                    UngetChar();
                    break;

                default:
                    if (c.IsWhitespace())
                    {
                        SkipWhiteSpace();
                        goto again;
                    }

                    break;
            }

            UngetChar();
        }

        private void SkipWhiteSpace()
        {
            while (true)
            {
                char c = PeekChar();
                if (!c.IsWhitespace())
                {
                    break;
                }

                SkipChar();
            }
        }

        private void ScanNewline(char c)
        {
            _tokenStart = _currentIndex - 1;
            NormalizeCRLF(c);

            // Memory optimization: only create the token if it will be stored
            if (TokenList != null)
            {
                NewToken(TokenKind.NewLine);
            }
        }

        private void ScanSemicolon()
        {
            _tokenStart = _currentIndex - 1;

            // Memory optimization: only create the token if it will be stored
            if (TokenList != null)
            {
                NewToken(TokenKind.Semi);
            }
        }

        private void ScanLineContinuation(char c)
        {
            _tokenStart = _currentIndex - 2;
            NormalizeCRLF(c);

            // Memory optimization: only create the token if it will be stored
            if (TokenList != null)
            {
                NewToken(TokenKind.LineContinuation);
            }
        }

        internal int GetRestorePoint()
        {
            _tokenStart = _currentIndex;
            return CurrentExtent().StartOffset;
        }

        internal void Resync(Token token)
        {
            // The parser has decided to backtrack and the tokenizer needs to pretend that it's
            // starting over from the beginning of token.

            Resync(((InternalScriptExtent)token.Extent).StartOffset);
        }

        internal void Resync(int start)
        {
            int adjustment = _nestedTokensAdjustment;
            if (_skippedCharOffsets != null)
            {
                for (int i = _nestedTokensAdjustment; i < start - 1 && i < _skippedCharOffsets.Length; ++i)
                {
                    if (_skippedCharOffsets[i])
                    {
                        adjustment += 1;
                    }
                }
            }

            _currentIndex = start - adjustment;
            if (_currentIndex > _script.Length + 1)
            {
                _currentIndex = _script.Length + 1;
            }
            else if (_currentIndex < 0)
            {
                _currentIndex = 0;
            }

            if (FirstToken != null && _currentIndex <= ((InternalScriptExtent)FirstToken.Extent).StartOffset)
            {
                FirstToken = null;
            }

            if (TokenList != null && TokenList.Count > 0)
            {
                // If we were saving tokens, remove all tokens from token to the end of the saved tokens.
                RemoveTokensFromListDuringResync(TokenList, start);
            }

            if (RequiresTokens != null && RequiresTokens.Count > 0)
            {
                RemoveTokensFromListDuringResync(RequiresTokens, start);
            }
        }

        internal void RemoveTokensFromListDuringResync(List<Token> tokenList, int start)
        {
            int removeFrom = 0;
            // If we were saving tokens, remove all tokens from token to the end of the saved tokens.
            int i = tokenList.Count - 1;
            if (i >= 0 && tokenList[i].Kind == TokenKind.EndOfInput)
            {
                i -= 1;
            }

            for (; i >= 0; i--)
            {
                if (((InternalScriptExtent)tokenList[i].Extent).EndOffset <= start)
                {
                    removeFrom = i + 1;
                    break;
                }
            }

            tokenList.RemoveRange(removeFrom, tokenList.Count - removeFrom);
        }

        internal void ReplaceSavedTokens(Token firstOldToken, Token lastOldToken, Token newToken)
        {
            int startOffset = ((InternalScriptExtent)firstOldToken.Extent).StartOffset;
            int endOffset = ((InternalScriptExtent)lastOldToken.Extent).EndOffset;
            int lastTokenToReplace = -1;
            for (int i = TokenList.Count - 1; i >= 0; i--)
            {
                if (((InternalScriptExtent)TokenList[i].Extent).EndOffset == endOffset)
                {
                    lastTokenToReplace = i;
                    continue;
                }

                if (((InternalScriptExtent)TokenList[i].Extent).StartOffset == startOffset)
                {
                    TokenList.RemoveRange(i, lastTokenToReplace - i + 1);
                    TokenList.Insert(i, newToken);
                    break;
                }
            }
        }

        private void NormalizeCRLF(char c)
        {
            // CRs in Windows line endings are ignored
            if (c == '\r' && PeekChar() == '\n')
            {
                SkipChar();
            }
        }

        internal void CheckAstIsBeforeSignature(Ast ast)
        {
            if (_beginSignatureExtent == null)
                return;

            if (_beginSignatureExtent.StartOffset < ast.Extent.StartOffset)
            {
                ReportError(ast.Extent,
                    nameof(ParserStrings.TokenAfterEndOfValidScriptText),
                    ParserStrings.TokenAfterEndOfValidScriptText);
            }
        }

        private void ReportError(int errorOffset, string errorId, string errorMsg, params object[] args)
        {
            _parser.ReportError(NewScriptExtent(errorOffset, errorOffset + 1), errorId, errorMsg, args);
        }

        private void ReportError(IScriptExtent extent, string errorId, string errorMsg)
        {
            _parser.ReportError(extent, errorId, errorMsg);
        }

        private void ReportError(IScriptExtent extent, string errorId, string errorMsg, object arg)
        {
            _parser.ReportError(extent, errorId, errorMsg, arg);
        }

        private void ReportError(IScriptExtent extent, string errorId, string errorMsg, object arg1, object arg2)
        {
            _parser.ReportError(extent, errorId, errorMsg, arg1, arg2);
        }

        private void ReportIncompleteInput(int errorOffset, string errorId, string errorMsg)
        {
            _parser.ReportIncompleteInput(NewScriptExtent(errorOffset, _currentIndex), errorId, errorMsg);
        }

        private void ReportIncompleteInput(int errorOffset, string errorId, string errorMsg, object arg)
        {
            _parser.ReportIncompleteInput(NewScriptExtent(errorOffset, _currentIndex), errorId, errorMsg, arg);
        }

        private InternalScriptExtent NewScriptExtent(int start, int end)
        {
            return new InternalScriptExtent(_positionHelper, start + _nestedTokensAdjustment, end + _nestedTokensAdjustment);
        }

        internal InternalScriptExtent CurrentExtent()
        {
            int start = _tokenStart + _nestedTokensAdjustment;
            int end = _currentIndex + _nestedTokensAdjustment;
            if (_skippedCharOffsets != null)
            {
                int i = _nestedTokensAdjustment;
                for (; i < start && i < _skippedCharOffsets.Length; ++i)
                {
                    if (_skippedCharOffsets[i])
                    {
                        start += 1;
                        end += 1;
                    }
                }

                for (; i < end && i < _skippedCharOffsets.Length; ++i)
                {
                    if (_skippedCharOffsets[i])
                    {
                        end += 1;
                    }
                }
            }

            return new InternalScriptExtent(_positionHelper, start, end);
        }

        internal IScriptExtent GetScriptExtent()
        {
            return NewScriptExtent(0, _script.Length);
        }

        private Token NewCommentToken()
        {
            return SaveToken(new Token(CurrentExtent(), TokenKind.Comment, TokenFlags.None));
        }

        private T SaveToken<T>(T token) where T : Token
        {
            TokenList?.Add(token);

            // Keep track of the first and last token even if we're not saving tokens
            // for the special variables $$ and $^.
            switch (token.Kind)
            {
                case TokenKind.NewLine:
                case TokenKind.LineContinuation:
                case TokenKind.Comment:
                case TokenKind.EndOfInput:
                    // Don't remember these tokens, they aren't useful in $$ and $^.
                    break;
                default:
                    FirstToken ??= token;

                    LastToken = token;
                    break;
            }

            return token;
        }

        private Token NewToken(TokenKind kind)
        {
            return SaveToken(new Token(CurrentExtent(), kind, TokenFlags.None));
        }

        private Token NewNumberToken(object value)
        {
            return SaveToken(new NumberToken(CurrentExtent(), value, TokenFlags.None));
        }

        private Token NewParameterToken(string name, bool sawColon)
        {
            return SaveToken(new ParameterToken(CurrentExtent(), name, sawColon));
        }

        private VariableToken NewVariableToken(VariablePath path, bool splatted)
        {
            return SaveToken(new VariableToken(CurrentExtent(), path, TokenFlags.None, splatted));
        }

        private StringToken NewStringLiteralToken(string value, TokenKind tokenKind, TokenFlags flags)
        {
            return SaveToken(new StringLiteralToken(CurrentExtent(), flags, tokenKind, value));
        }

        private StringToken NewStringExpandableToken(string value, string formatString, TokenKind tokenKind, List<Token> nestedTokens, TokenFlags flags)
        {
            if (nestedTokens != null && nestedTokens.Count == 0)
            {
                nestedTokens = null;
            }
            else if ((flags & TokenFlags.TokenInError) == 0)
            {
                if (nestedTokens.Any(static tok => tok.HasError))
                {
                    flags |= TokenFlags.TokenInError;
                }
            }

            return SaveToken(new StringExpandableToken(CurrentExtent(), tokenKind, value, formatString, nestedTokens, flags));
        }

        private Token NewGenericExpandableToken(string value, string formatString, List<Token> nestedTokens)
        {
            return NewStringExpandableToken(value, formatString, TokenKind.Generic, nestedTokens, TokenFlags.None);
        }

        private Token NewGenericToken(string value)
        {
            return NewStringLiteralToken(value, TokenKind.Generic, TokenFlags.None);
        }

        private Token NewInputRedirectionToken()
        {
            return SaveToken(new InputRedirectionToken(CurrentExtent()));
        }

        private Token NewFileRedirectionToken(int from, bool append, bool fromSpecifiedExplicitly)
        {
            if (fromSpecifiedExplicitly && InExpressionMode())
            {
                UngetChar();
                if (append)
                {
                    UngetChar();
                }

                return NewNumberToken(from);
            }

            return SaveToken(new FileRedirectionToken(CurrentExtent(), (RedirectionStream)from, append));
        }

        private Token NewMergingRedirectionToken(int from, int to)
        {
            return SaveToken(new MergingRedirectionToken(CurrentExtent(), (RedirectionStream)from, (RedirectionStream)to));
        }

        private LabelToken NewLabelToken(string value)
        {
            return SaveToken(new LabelToken(CurrentExtent(), TokenFlags.None, value));
        }

        internal bool IsAtEndOfScript(IScriptExtent extent, bool checkCommentsAndWhitespace = false)
        {
            var scriptExtent = (InternalScriptExtent)extent;
            return scriptExtent.EndOffset >= _script.Length
                || (checkCommentsAndWhitespace && OnlyWhitespaceOrCommentsAfterExtent(scriptExtent));
        }

        private bool OnlyWhitespaceOrCommentsAfterExtent(InternalScriptExtent extent)
        {
            for (int i = extent.EndOffset; i < _script.Length; ++i)
            {
                if (_script[i] == '#')
                {
                    // SkipLineComment will return the position after the comment end
                    // which is either at the end of the file, or a cr or lf.
                    i = SkipLineComment(i + 1) - 1;
                    continue;
                }

                if (_script[i] == '<' && (i + 1) < _script.Length && _script[i + 1] == '#')
                {
                    i = SkipBlockComment(i + 2) - 1;
                    continue;
                }

                if (!_script[i].IsWhitespace())
                {
                    return false;
                }
            }

            return true;
        }

        internal bool IsPipeContinuation(IScriptExtent extent)
        {
            // If the first non-whitespace & non-comment (regular or block) character following a newline is a pipe, we have
            // pipe continuation.
            return extent.EndOffset < _script.Length && ContinuationAfterExtent(extent, continuationChar: '|');
        }

        private bool ContinuationAfterExtent(IScriptExtent extent, char continuationChar)
        {
            bool lastNonWhitespaceIsNewline = true;
            int i = extent.EndOffset;

            // Since some token pattern matching looks for multiple characters (e.g. newline or block comment)
            // we stop searching at _script.Length - 1 and perform one additional check after the while loop.
            // This avoids having to compare i + 1 against the script length in multiple locations inside the
            // loop.
            while (i < _script.Length - 1)
            {
                char c = _script[i];

                if (c.IsWhitespace())
                {
                    i++;
                    continue;
                }

                if (c == '\n')
                {
                    if (lastNonWhitespaceIsNewline)
                    {
                        // blank or whitespace-only lines are not allowed in automatic line continuation
                        return false;
                    }

                    lastNonWhitespaceIsNewline = true;
                    i++;
                    continue;
                }
                else if (c == '\r')
                {
                    if (lastNonWhitespaceIsNewline)
                    {
                        // blank or whitespace-only lines are not allowed in automatic line continuation
                        return false;
                    }

                    lastNonWhitespaceIsNewline = true;
                    i += _script[i + 1] == '\n' ? 2 : 1;
                    continue;
                }

                lastNonWhitespaceIsNewline = false;

                if (c == '#')
                {
                    // SkipLineComment will return the position after the comment end
                    // which is either at the end of the file, or a cr or lf.
                    i = SkipLineComment(i + 1);
                    continue;
                }

                if (c == '<' && _script[i + 1] == '#')
                {
                    i = SkipBlockComment(i + 2);
                    continue;
                }

                return c == continuationChar;
            }

            return _script[_script.Length - 1] == continuationChar;
        }

        private int SkipLineComment(int i)
        {
            for (; i < _script.Length; ++i)
            {
                char c = _script[i];

                if (c == '\r' || c == '\n')
                {
                    break;
                }
            }

            return i;
        }

        private int SkipBlockComment(int i)
        {
            for (; i < _script.Length; ++i)
            {
                char c = _script[i];

                if (c == '#' && (i + 1) < _script.Length && _script[i + 1] == '>')
                {
                    return i + 2;
                }
            }

            return i;
        }

        private char Backtick(char c, out char surrogateCharacter)
        {
            surrogateCharacter = s_invalidChar;

            switch (c)
            {
                case '0':
                    return '\0';
                case 'a':
                    return '\a';
                case 'b':
                    return '\b';
                case 'e':
                    return '\u001b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'u':
                    return ScanUnicodeEscape(out surrogateCharacter);
                case 'v':
                    return '\v';
                default:
                    return c;
            }
        }

        private char ScanUnicodeEscape(out char surrogateCharacter)
        {
            int escSeqStartIndex = _currentIndex - 2;
            surrogateCharacter = s_invalidChar;

            char c = GetChar();
            if (c != '{')
            {
                UngetChar();

                IScriptExtent errorExtent = NewScriptExtent(escSeqStartIndex, _currentIndex);
                ReportError(errorExtent,
                    nameof(ParserStrings.InvalidUnicodeEscapeSequence),
                    ParserStrings.InvalidUnicodeEscapeSequence);
                return s_invalidChar;
            }

            // Scan the rest of the Unicode escape sequence - one to six hex digits terminated plus the closing '}'.
            var sb = GetStringBuilder();
            int i;
            for (i = 0; i < s_maxNumberOfUnicodeHexDigits + 1; i++)
            {
                c = GetChar();

                // Sequence has been terminated.
                if (c == '}')
                {
                    if (i == 0)
                    {
                        // Sequence must have at least one hex char.
                        Release(sb);
                        IScriptExtent errorExtent = NewScriptExtent(escSeqStartIndex, _currentIndex);
                        ReportError(errorExtent,
                            nameof(ParserStrings.InvalidUnicodeEscapeSequence),
                            ParserStrings.InvalidUnicodeEscapeSequence);
                        return s_invalidChar;
                    }

                    break;
                }
                else if (!c.IsHexDigit())
                {
                    UngetChar();

                    Release(sb);
                    if (i < s_maxNumberOfUnicodeHexDigits)
                    {
                        ReportError(_currentIndex,
                            nameof(ParserStrings.InvalidUnicodeEscapeSequence),
                            ParserStrings.InvalidUnicodeEscapeSequence);
                    }
                    else
                    {
                        ReportError(_currentIndex,
                            nameof(ParserStrings.MissingUnicodeEscapeSequenceTerminator),
                            ParserStrings.MissingUnicodeEscapeSequenceTerminator);
                    }

                    return s_invalidChar;
                }
                else if (i == s_maxNumberOfUnicodeHexDigits)
                {
                    UngetChar();

                    Release(sb);
                    ReportError(_currentIndex,
                        nameof(ParserStrings.TooManyDigitsInUnicodeEscapeSequence),
                        ParserStrings.TooManyDigitsInUnicodeEscapeSequence);
                    return s_invalidChar;
                }

                sb.Append(c);
            }

            string hexStr = GetStringAndRelease(sb);

            uint unicodeValue = uint.Parse(hexStr, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
            if (unicodeValue <= char.MaxValue)
            {
                return ((char)unicodeValue);
            }
            else if (unicodeValue <= 0x10FFFF)
            {
                return GetCharsFromUtf32(unicodeValue, out surrogateCharacter);
            }
            else
            {
                // Place the error indicator under only the hex digits in the esc sequence.
                IScriptExtent errorExtent = NewScriptExtent(escSeqStartIndex + 3, _currentIndex - 1);
                ReportError(errorExtent,
                    nameof(ParserStrings.InvalidUnicodeEscapeSequenceValue),
                    ParserStrings.InvalidUnicodeEscapeSequenceValue);
                return s_invalidChar;
            }
        }

        private static char GetCharsFromUtf32(uint codepoint, out char lowSurrogate)
        {
            if (codepoint < (uint)0x00010000)
            {
                lowSurrogate = s_invalidChar;
                return (char)codepoint;
            }
            else
            {
                Diagnostics.Assert((codepoint > 0x0000FFFF) && (codepoint <= 0x0010FFFF), "Codepoint is out of range for a surrogate pair");
                lowSurrogate = (char)((codepoint - 0x00010000) % 0x0400 + 0xDC00);
                return (char)((codepoint - 0x00010000) / 0x0400 + 0xD800);
            }
        }

        private void ScanToEndOfCommentLine(out bool sawBeginSig, out bool matchedRequires)
        {
            // When we get here, we are scanning a line comment.  To avoid rescanning,
            // we look for a script signature while we scan to the end of the line.
            //
            // We want to find both real signatures and fake signatures, with the goal of disallowing any
            // code after anything that looks like a signature because people often stop reading a script
            // once they see a signature - making it relatively easy to "hide" trojan like code in a script
            // after a fake signature.
            //
            // To "match" a signature, we compare its similarity (using Levenshtein Distance) to find
            // comments that look confusingly similar to the actual signature block (mapping to lowercase,
            // and ignoring spaces).
            //
            // At the same time, we also want to match #requires.  We do this with a simple state machine,
            // incrementing the state as we continue to match, or set requiresMatchState to -1 if we failed to match.

            var commentLine = GetStringBuilder();

            int requiresMatchState = 0;
            matchedRequires = false;
            while (true)
            {
                char c = GetChar();

                if (!c.IsWhitespace())
                {
                    commentLine.Append(c);
                }

                switch (c)
                {
                    case 'e':
                    case 'E':
                        if (requiresMatchState == 1 || requiresMatchState == 6)
                            requiresMatchState += 1;
                        else
                            requiresMatchState = -1;
                        break;
                    case 'i':
                    case 'I':
                        if (requiresMatchState == 4)
                            requiresMatchState += 1;
                        else
                            requiresMatchState = -1;
                        break;
                    case 'q':
                    case 'Q':
                        if (requiresMatchState == 2)
                            requiresMatchState += 1;
                        else
                            requiresMatchState = -1;
                        break;
                    case 'r':
                    case 'R':
                        if (requiresMatchState == 0 || requiresMatchState == 5)
                            requiresMatchState += 1;
                        else
                            requiresMatchState = -1;
                        break;
                    case 's':
                    case 'S':
                        if (requiresMatchState == 7)
                            matchedRequires = true;
                        else
                            requiresMatchState = -1;
                        break;
                    case 'u':
                    case 'U':
                        if (requiresMatchState == 3)
                            requiresMatchState += 1;
                        else
                            requiresMatchState = -1;
                        break;
                    case '\0':
                        if (AtEof())
                        {
                            goto case '\n';
                        }

                        goto default;
                    case '\r':
                    case '\n':
                        UngetChar();

                        // Detect a line comment that disguises itself to look like the beginning of a signature block.
                        // This could be used to hide code at the bottom of a script, since people might assume there is nothing else after the signature.
                        //
                        // The token similarity threshold was chosen by instrumenting the tokenizer and
                        // analyzing every comment from PoshCode, Technet Script Center, and Windows.
                        //
                        // The closest comments above "10" had a score of 11, which are marginal and
                        // appropriately close to being tricky.
                        //
                        // # END BEGIN SCRIPT BLOCK
                        // # SET TEXT SIGNATURE
                        //
                        // Below 11 were only actual signature blocks:
                        //
                        // # SIG # END SIGNATURE BLOCK
                        // # SIG # BEGIN SIGNATURE BLOCK
                        //
                        // There were only 279 (out of 269,387) comments with a similarity of 11,12,13,14, or 15.
                        // At a similarity of 16-77, there were thousands of comments per similarity bucket.

                        const string beginSignatureTextNoSpace = "sig#beginsignatureblock\n";
                        const int beginTokenSimilarityThreshold = 10;

                        const int beginTokenSimilarityUpperBound = 34; // beginSignatureTextNoSpace.Length + beginTokenSimilarityThreshold
                        const int beginTokenSimilarityLowerBound = 14; // beginSignatureTextNoSpace.Length - beginTokenSimilarityThreshold

                        // Quick exit - the comment line is more than 'threshold' longer, or is less than 'threshold' shorter. Therefore,
                        // its similarity will be over the threshold.
                        if (commentLine.Length > beginTokenSimilarityUpperBound || commentLine.Length < beginTokenSimilarityLowerBound)
                        {
                            sawBeginSig = false;
                        }
                        else
                        {
                            // Perf note - the GetStringSimilarity function is able to evaluate approximately 50kb of pure comments
                            // (1000 lines, each of length between 10 and 80 characters) in about 40ms, compared to 6ms it took to
                            // doing the error-prone hashing approach we had implemented before.
                            //
                            // The average script is 14% comments and parses in about 5.05 ms with this algorithm,
                            // about 4.45 ms with the more simplistic algorithm.

                            string commentLineComparison = commentLine.ToString().ToLowerInvariant();
                            if (_beginTokenSimilarity2dArray == null)
                            {
                                // Create the 2 dimensional array for edit distance calculation if it hasn't been created yet.
                                _beginTokenSimilarity2dArray = new int[beginTokenSimilarityUpperBound + 1, beginSignatureTextNoSpace.Length + 1];
                            }
                            else
                            {
                                // Zero out the 2 dimensional array before using it.
                                Array.Clear(_beginTokenSimilarity2dArray, 0, _beginTokenSimilarity2dArray.Length);
                            }

                            int sawBeginTokenSimilarity = GetStringSimilarity(commentLineComparison, beginSignatureTextNoSpace, _beginTokenSimilarity2dArray);
                            sawBeginSig = sawBeginTokenSimilarity < beginTokenSimilarityThreshold;
                        }

                        Release(commentLine);
                        return;
                    default:
                        requiresMatchState = -1;
                        break;
                }
            }
        }

        #endregion Utilities

        #region Object reuse

        // A two-dimensional integer array reused for calculating string similarity.
        private int[,] _beginTokenSimilarity2dArray;

        private readonly Queue<StringBuilder> _stringBuilders = new Queue<StringBuilder>();

        private StringBuilder GetStringBuilder()
        {
            return _stringBuilders.Count == 0 ? new StringBuilder() : _stringBuilders.Dequeue();
        }

        private void Release(StringBuilder sb)
        {
            // We don't want to cache too much, so limit to 10 string builders
            // and don't keep any that have > 1kb
            if (_stringBuilders.Count < 10 && sb.Capacity < 1024)
            {
                sb.Clear();
                _stringBuilders.Enqueue(sb);
            }
        }

        private string GetStringAndRelease(StringBuilder sb)
        {
            var result = sb.ToString();
            Release(sb);
            return result;
        }

        #endregion Object reuse

        #region Comments

        private void ScanLineComment()
        {
            bool sawBeginSig;
            bool matchedRequires;
            ScanToEndOfCommentLine(out sawBeginSig, out matchedRequires);
            var token = NewCommentToken();
            if (sawBeginSig)
            {
                _beginSignatureExtent = CurrentExtent();
            }
            else if (matchedRequires && _nestedTokensAdjustment == 0)
            {
                RequiresTokens ??= new List<Token>();
                RequiresTokens.Add(token);
            }
        }

        private void ScanBlockComment()
        {
            int errorIndex = _currentIndex - 2;
            while (true)
            {
                char c = GetChar();

                if (c == '#' && PeekChar() == '>')
                {
                    SkipChar();
                    break;
                }

                if (c == '\r')
                {
                    NormalizeCRLF(c);
                }
                else if (c == '\0' && AtEof())
                {
                    UngetChar();
                    ReportIncompleteInput(errorIndex,
                        nameof(ParserStrings.MissingTerminatorMultiLineComment),
                        ParserStrings.MissingTerminatorMultiLineComment);
                    break;
                }
            }

            NewCommentToken();
        }

        // Implementation of the Levenshtein Distance algorithm
        // https://en.wikipedia.org/wiki/Levenshtein_distance
        private static int GetStringSimilarity(string first, string second, int[,] distanceMap = null)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second), "Caller never calls us with empty strings");

            // Store a distance map to store the number of edits required to
            // convert the first <row> letters of First to the first <column>
            // letters of Second.
            distanceMap ??= new int[first.Length + 1, second.Length + 1];

            // Initialize the first row and column of the matrix - the number
            // of edits required when one of the strings is empty is just
            // the length of the non-empty string
            for (int row = 0; row <= first.Length; row++)
            {
                distanceMap[row, 0] = row;
            }

            for (int column = 0; column <= second.Length; column++)
            {
                distanceMap[0, column] = column;
            }

            // Visit all prefixes and determine the minimum edit distance
            for (int row = 1; row <= first.Length; row++)
            {
                for (int column = 1; column <= second.Length; column++)
                {
                    // If these two characters are the same, then
                    // The edit distance is the same as it was for the
                    // two shorter substrings without this character.
                    if (first[row - 1] == second[column - 1])
                    {
                        distanceMap[row, column] = distanceMap[row - 1, column - 1];
                    }
                    else
                    {
                        // Otherwise, the edit distance is the minimum
                        // of doing an addition of a character, a deletion
                        // of a character, or a substitution of a character
                        distanceMap[row, column] = Math.Min(
                            Math.Min(
                                distanceMap[row - 1, column] + 1,
                                distanceMap[row, column - 1] + 1),
                                distanceMap[row - 1, column - 1] + 1);
                    }
                }
            }

            return distanceMap[first.Length, second.Length];
        }

        #region Requires

        internal ScriptRequirements GetScriptRequirements()
        {
            if (RequiresTokens == null)
                return null;

            // Make sure a nested scan of the #requires lines don't affect our processing here.
            var requiresTokens = RequiresTokens.ToArray();
            RequiresTokens = null;

            string requiredShellId = null;
            Version requiredVersion = null;
            List<string> requiredEditions = null;
            List<ModuleSpecification> requiredModules = null;
            List<PSSnapInSpecification> requiredSnapins = null;
            List<string> requiredAssemblies = null;
            bool requiresElevation = false;

            foreach (var token in requiresTokens)
            {
                var requiresExtent = new InternalScriptExtent(_positionHelper, token.Extent.StartOffset + 1, token.Extent.EndOffset);
                var state = StartNestedScan(new UnscannedSubExprToken(requiresExtent, TokenFlags.None, requiresExtent.Text, null));
                var commandAst = _parser.CommandRule(forDynamicKeyword: false) as CommandAst;
                _parser._ungotToken = null;
                FinishNestedScan(state);

                string snapinName = null;
                Version snapinVersion = null;

                if (commandAst != null)
                {
                    var commandName = commandAst.GetCommandName();
                    if (!string.Equals(commandName, "requires", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportError(commandAst.Extent,
                            nameof(DiscoveryExceptions.ScriptRequiresInvalidFormat),
                            DiscoveryExceptions.ScriptRequiresInvalidFormat);
                    }

                    var snapinSpecified = false;
                    for (int i = 1; i < commandAst.CommandElements.Count; i++)
                    {
                        var parameter = commandAst.CommandElements[i] as CommandParameterAst;

                        if (parameter != null &&
                            PSSnapinToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
                        {
                            snapinSpecified = true;
                            requiredSnapins ??= new List<PSSnapInSpecification>();

                            break;
                        }
                    }

                    for (int i = 1; i < commandAst.CommandElements.Count; i++)
                    {
                        var parameter = commandAst.CommandElements[i] as CommandParameterAst;
                        if (parameter != null)
                        {
                            HandleRequiresParameter(parameter, commandAst.CommandElements, snapinSpecified,
                                ref i, ref snapinName, ref snapinVersion,
                                ref requiredShellId, ref requiredVersion, ref requiredEditions, ref requiredModules, ref requiredAssemblies, ref requiresElevation);
                        }
                        else
                        {
                            ReportError(commandAst.CommandElements[i].Extent,
                                nameof(DiscoveryExceptions.ScriptRequiresInvalidFormat),
                                DiscoveryExceptions.ScriptRequiresInvalidFormat);
                        }
                    }

                    if (snapinName != null)
                    {
                        Diagnostics.Assert(PSSnapInInfo.IsPSSnapinIdValid(snapinName), "we shouldn't set snapinName if it wasn't valid");
                        requiredSnapins.Add(new PSSnapInSpecification(snapinName) { Version = snapinVersion });
                    }
                }
            }

            return new ScriptRequirements
            {
                RequiredApplicationId = requiredShellId,
                RequiredPSVersion = requiredVersion,
                RequiredPSEditions = requiredEditions != null
                                                    ? new ReadOnlyCollection<string>(requiredEditions)
                                                    : ScriptRequirements.EmptyEditionCollection,
                RequiredAssemblies = requiredAssemblies != null
                                                    ? new ReadOnlyCollection<string>(requiredAssemblies)
                                                    : ScriptRequirements.EmptyAssemblyCollection,
                RequiredModules = requiredModules != null
                                                    ? new ReadOnlyCollection<ModuleSpecification>(requiredModules)
                                                    : ScriptRequirements.EmptyModuleCollection,
                IsElevationRequired = requiresElevation
            };
        }

        private const string shellIDToken = "shellid";
        private const string PSSnapinToken = "pssnapin";
        private const string versionToken = "version";
        private const string editionToken = "psedition";
        private const string assemblyToken = "assembly";
        private const string modulesToken = "modules";
        private const string elevationToken = "runasadministrator";

        private void HandleRequiresParameter(CommandParameterAst parameter,
                                             ReadOnlyCollection<CommandElementAst> commandElements,
                                             bool snapinSpecified,
                                             ref int index,
                                             ref string snapinName,
                                             ref Version snapinVersion,
                                             ref string requiredShellId,
                                             ref Version requiredVersion,
                                             ref List<string> requiredEditions,
                                             ref List<ModuleSpecification> requiredModules,
                                             ref List<string> requiredAssemblies,
                                             ref bool requiresElevation)
        {
            Ast argumentAst = parameter.Argument ?? (index + 1 < commandElements.Count ? commandElements[++index] : null);

            if (elevationToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                requiresElevation = true;
                if (argumentAst != null)
                {
                    ReportError(parameter.Extent,
                        nameof(ParserStrings.ParameterCannotHaveArgument),
                        ParserStrings.ParameterCannotHaveArgument,
                        parameter.ParameterName);
                }

                return;
            }

            if (argumentAst == null)
            {
                ReportError(parameter.Extent,
                    nameof(ParserStrings.ParameterRequiresArgument),
                    ParserStrings.ParameterRequiresArgument,
                    parameter.ParameterName);
                return;
            }

            object argumentValue;
            if (!IsConstantValueVisitor.IsConstant(argumentAst, out argumentValue, forRequires: true))
            {
                ReportError(argumentAst.Extent,
                    nameof(ParserStrings.RequiresArgumentMustBeConstant),
                    ParserStrings.RequiresArgumentMustBeConstant);
                return;
            }

            if (shellIDToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                if (requiredShellId != null)
                {
                    ReportError(parameter.Extent,
                        nameof(ParameterBinderStrings.ParameterAlreadyBound),
                        ParameterBinderStrings.ParameterAlreadyBound,
                        null,
                        shellIDToken);
                    return;
                }

                if (argumentValue is not string)
                {
                    ReportError(argumentAst.Extent,
                        nameof(ParserStrings.RequiresInvalidStringArgument),
                        ParserStrings.RequiresInvalidStringArgument,
                        shellIDToken);
                    return;
                }

                requiredShellId = (string)argumentValue;
            }
            else if (PSSnapinToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                if (argumentValue is not string)
                {
                    ReportError(argumentAst.Extent,
                        nameof(ParserStrings.RequiresInvalidStringArgument),
                        ParserStrings.RequiresInvalidStringArgument,
                        PSSnapinToken);
                    return;
                }

                if (snapinName != null)
                {
                    ReportError(parameter.Extent,
                        nameof(ParameterBinderStrings.ParameterAlreadyBound),
                        ParameterBinderStrings.ParameterAlreadyBound,
                        null,
                        PSSnapinToken);
                    return;
                }

                if (!PSSnapInInfo.IsPSSnapinIdValid((string)argumentValue))
                {
                    ReportError(argumentAst.Extent,
                        nameof(MshSnapInCmdletResources.InvalidPSSnapInName),
                        MshSnapInCmdletResources.InvalidPSSnapInName);
                    return;
                }

                snapinName = (string)argumentValue;
            }
            else if (editionToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                if (requiredEditions != null)
                {
                    ReportError(parameter.Extent,
                        nameof(ParameterBinderStrings.ParameterAlreadyBound),
                        ParameterBinderStrings.ParameterAlreadyBound,
                        null,
                        editionToken);
                    return;
                }

                if (argumentValue is string || argumentValue is not IEnumerable)
                {
                    requiredEditions = HandleRequiresPSEditionArgument(argumentAst, argumentValue, ref requiredEditions);
                }
                else
                {
                    foreach (var arg in (IEnumerable)argumentValue)
                    {
                        requiredEditions = HandleRequiresPSEditionArgument(argumentAst, arg, ref requiredEditions);
                    }
                }
            }
            else if (versionToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                var argumentText = argumentValue as string ?? argumentAst.Extent.Text;
                var version = Utils.StringToVersion(argumentText);
                if (version == null)
                {
                    ReportError(argumentAst.Extent,
                        nameof(ParserStrings.RequiresVersionInvalid),
                        ParserStrings.RequiresVersionInvalid);
                    return;
                }

                if (snapinSpecified)
                {
                    if (snapinVersion != null)
                    {
                        ReportError(parameter.Extent,
                            nameof(ParameterBinderStrings.ParameterAlreadyBound),
                            ParameterBinderStrings.ParameterAlreadyBound,
                            null,
                            versionToken);
                        return;
                    }

                    snapinVersion = version;
                }
                else
                {
                    if (requiredVersion != null && !requiredVersion.Equals(version))
                    {
                        ReportError(parameter.Extent,
                            nameof(ParameterBinderStrings.ParameterAlreadyBound),
                            ParameterBinderStrings.ParameterAlreadyBound,
                            null,
                            versionToken);
                        return;
                    }

                    requiredVersion = version;
                }
            }
            else if (assemblyToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                if (argumentValue is string || argumentValue is not IEnumerable)
                {
                    requiredAssemblies = HandleRequiresAssemblyArgument(argumentAst, argumentValue, requiredAssemblies);
                }
                else
                {
                    foreach (var arg in (IEnumerable)argumentValue)
                    {
                        requiredAssemblies = HandleRequiresAssemblyArgument(argumentAst, arg, requiredAssemblies);
                    }
                }
            }
            else if (modulesToken.StartsWith(parameter.ParameterName, StringComparison.OrdinalIgnoreCase))
            {
                var enumerable = argumentValue as object[] ?? new[] { argumentValue };
                foreach (var arg in enumerable)
                {
                    ModuleSpecification moduleSpecification;
                    try
                    {
                        moduleSpecification = LanguagePrimitives.ConvertTo<ModuleSpecification>(arg);
                    }
                    catch (InvalidCastException e)
                    {
                        ReportError(argumentAst.Extent,
                            nameof(ParserStrings.RequiresModuleInvalid),
                            ParserStrings.RequiresModuleInvalid,
                            e.Message);
                        return;
                    }
                    catch (ArgumentException e)
                    {
                        ReportError(argumentAst.Extent,
                            nameof(ParserStrings.RequiresModuleInvalid),
                            ParserStrings.RequiresModuleInvalid,
                            e.Message);
                        return;
                    }

                    requiredModules ??= new List<ModuleSpecification>();
                    requiredModules.Add(moduleSpecification);
                }
            }
            else
            {
                ReportError(parameter.Extent,
                    nameof(DiscoveryExceptions.ScriptRequiresInvalidFormat),
                    DiscoveryExceptions.ScriptRequiresInvalidFormat);
            }
        }

        private List<string> HandleRequiresAssemblyArgument(Ast argumentAst, object arg, List<string> requiredAssemblies)
        {
            if (arg is not string)
            {
                ReportError(argumentAst.Extent,
                    nameof(ParserStrings.RequiresInvalidStringArgument),
                    ParserStrings.RequiresInvalidStringArgument,
                    assemblyToken);
            }
            else
            {
                requiredAssemblies ??= new List<string>();

                if (!requiredAssemblies.Contains((string)arg))
                {
                    requiredAssemblies.Add((string)arg);
                }
            }

            return requiredAssemblies;
        }

        private List<string> HandleRequiresPSEditionArgument(Ast argumentAst, object arg, ref List<string> requiredEditions)
        {
            if (arg is not string)
            {
                ReportError(argumentAst.Extent,
                    nameof(ParserStrings.RequiresInvalidStringArgument),
                    ParserStrings.RequiresInvalidStringArgument,
                    editionToken);
            }
            else
            {
                requiredEditions ??= new List<string>();

                var edition = (string)arg;
                if (!Utils.IsValidPSEditionValue(edition))
                {
                    ReportError(argumentAst.Extent,
                        nameof(ParserStrings.RequiresPSEditionInvalid),
                        ParserStrings.RequiresPSEditionInvalid,
                        editionToken);
                }

                if (!requiredEditions.Contains(edition, StringComparer.OrdinalIgnoreCase))
                {
                    requiredEditions.Add(edition);
                }
                else
                {
                    ReportError(argumentAst.Extent,
                        nameof(ParserStrings.RequiresPSEditionValueIsAlreadySpecified),
                        ParserStrings.RequiresPSEditionValueIsAlreadySpecified,
                        editionToken);
                }
            }

            return requiredEditions;
        }
        #endregion Requires

        #endregion Comments

        #region Strings

        // When processing a verbatim command argument, read until the end
        // of line completely ignoring all PowerShell semantics.
        internal StringToken GetVerbatimCommandArgument()
        {
            SkipWhiteSpace();
            _tokenStart = _currentIndex;

            bool inQuotes = false;
            while (true)
            {
                char c = GetChar();

                if (c == '\r' || c == '\n' || (c == '\0' && AtEof()))
                {
                    UngetChar();
                    break;
                }

                if (c.IsDoubleQuote())
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && (c == '|' || (c == '&' && !AtEof() && PeekChar() == '&')))
                {
                    UngetChar();
                    break;
                }
            }

            InternalScriptExtent currentExtent = CurrentExtent();
            string tokenValue = currentExtent.Text;
            return NewStringLiteralToken(tokenValue, TokenKind.Generic, TokenFlags.None);
        }

        private TokenFlags ScanStringLiteral(StringBuilder sb)
        {
            int errorIndex = _currentIndex - 1;
            TokenFlags flags = TokenFlags.None;

            char c = GetChar();
            while (c != '\0' || !AtEof())
            {
                if (c.IsSingleQuote())
                {
                    // Check for 2 quotes in a row.  If so, the first is the "escape"
                    // and append the second quote.  Otherwise, we're done collecting.
                    if (!PeekChar().IsSingleQuote())
                    {
                        break;
                    }

                    c = GetChar();
                }

                sb.Append(c);
                c = GetChar();
            }

            if (c == '\0')
            {
                // error - reached end of input without seeing terminator
                UngetChar();
                ReportIncompleteInput(errorIndex,
                    nameof(ParserStrings.TerminatorExpectedAtEndOfString),
                    ParserStrings.TerminatorExpectedAtEndOfString,
                    "'");
                flags = TokenFlags.TokenInError;
            }

            return flags;
        }

        private Token ScanStringLiteral()
        {
            var sb = GetStringBuilder();
            var flags = ScanStringLiteral(sb);
            return NewStringLiteralToken(GetStringAndRelease(sb), TokenKind.StringLiteral, flags);
        }

        private Token ScanSubExpression(bool hereString)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            _tokenStart = _currentIndex - 2;
            var sb = GetStringBuilder();
            sb.Append("$(");
            int parenCount = 1;
            TokenFlags flags = TokenFlags.None;
            bool scanning = true;
            List<int> skippedCharOffsets = new List<int>();
            while (scanning)
            {
                char c = GetChar();
                switch (c)
                {
                    case '(':
                        sb.Append(c);
                        ++parenCount;
                        break;

                    case ')':
                        sb.Append(c);
                        if (--parenCount == 0)
                        {
                            scanning = false;
                        }

                        break;

                    case '`':
                    case '"':
                    case SpecialChars.QuoteDoubleLeft:
                    case SpecialChars.QuoteDoubleRight:
                    case SpecialChars.QuoteLowDoubleLeft:
                        char c1 = PeekChar();
                        if (!hereString && c1.IsDoubleQuote())
                        {
                            SkipChar();
                            sb.Append(c1);
                            skippedCharOffsets.Add(_currentIndex - 2 + _nestedTokensAdjustment);
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;

                    case '\0':
                        if (!AtEof())
                            goto default;

                        UngetChar();
                        ReportIncompleteInput(_tokenStart,
                            nameof(ParserStrings.IncompleteDollarSubexpressionReference),
                            ParserStrings.IncompleteDollarSubexpressionReference);
                        flags = TokenFlags.TokenInError;
                        scanning = false;
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }

            BitArray skippedCharBitArray;
            if (skippedCharOffsets.Count > 0)
            {
                skippedCharBitArray = new BitArray(skippedCharOffsets.Last() + 1);
                foreach (int i in skippedCharOffsets)
                {
                    skippedCharBitArray.Set(i, true);
                }
            }
            else
            {
                skippedCharBitArray = _skippedCharOffsets;
            }

            var extent = CurrentExtent();
            Diagnostics.Assert(
                (extent.Text[0] == '$' && extent.Text[1] == '(' && extent.Text[extent.Text.Length - 1] == ')') || (flags & TokenFlags.TokenInError) != 0,
                "Extent computed incorrectly.");
            return new UnscannedSubExprToken(extent, flags, GetStringAndRelease(sb), skippedCharBitArray);
        }

        private TokenFlags ScanStringExpandable(StringBuilder sb, StringBuilder formatSb, List<Token> nestedTokens)
        {
            TokenFlags flags = TokenFlags.None;
            int errorIndex = _currentIndex - 1;

            char c = GetChar();
            for (; c != '\0' || !AtEof(); c = GetChar())
            {
                if (c.IsDoubleQuote())
                {
                    // Check for 2 quotes in a row.  If so, the first is the "escape"
                    // and append the second quote.  Otherwise, we're done collecting.
                    if (!PeekChar().IsDoubleQuote())
                    {
                        break;
                    }

                    c = GetChar();
                }
                else if (c == '$')
                {
                    if (ScanDollarInStringExpandable(sb, formatSb, false, nestedTokens))
                    {
                        continue;
                    }
                }
                else if (c == '`')
                {
                    // If end of input, go ahead and append the backtick and issue an error later
                    char c1 = PeekChar();
                    if (c1 != 0)
                    {
                        SkipChar();
                        c = Backtick(c1, out char surrogateCharacter);
                        if (surrogateCharacter != s_invalidChar)
                        {
                            sb.Append(c).Append(surrogateCharacter);
                            formatSb.Append(c).Append(surrogateCharacter);
                            continue;
                        }
                    }
                }

                if (c == '{' || c == '}')
                {
                    // In the format string, we need to double up the curlies because we're
                    // replacing variable references and sub-expressions with the appropriate
                    // format expression for string.Format.
                    formatSb.Append(c);
                }

                sb.Append(c);
                formatSb.Append(c);
            }

            if (c == '\0')
            {
                UngetChar();
                ReportIncompleteInput(errorIndex,
                    nameof(ParserStrings.TerminatorExpectedAtEndOfString),
                    ParserStrings.TerminatorExpectedAtEndOfString,
                    "\"");
                flags = TokenFlags.TokenInError;
            }

            return flags;
        }

        // Returns true if a variable or sub-expression is successfully scanned, false otherwise.
        private bool ScanDollarInStringExpandable(StringBuilder sb, StringBuilder formatSb, bool hereString, List<Token> nestedTokens)
        {
            int dollarIndex = _currentIndex - 1;
            char c1 = PeekChar();
            int saveTokenStart = _tokenStart;
            var oldTokenizerMode = Mode;
            var oldTokenList = TokenList;
            Token nestedToken = null;

            try
            {
                // None of these tokens should be saved
                TokenList = null;
                Mode = TokenizerMode.Expression;

                if (c1 == '(')
                {
                    SkipChar();
                    nestedToken = ScanSubExpression(hereString);
                }
                else if (c1.IsVariableStart() || c1 == '{')
                {
                    _tokenStart = _currentIndex - 1;
                    nestedToken = ScanVariable(false, true);
                }
            }
            finally
            {
                TokenList = oldTokenList;
                _tokenStart = saveTokenStart;
                Mode = oldTokenizerMode;
            }

            if (nestedToken != null)
            {
                sb.Append(_script, dollarIndex, _currentIndex - dollarIndex);
                formatSb.Append('{');
                formatSb.Append(nestedTokens.Count);
                formatSb.Append('}');
                nestedTokens.Add(nestedToken);
                return true;
            }

            // Make sure we didn't consume anything because we didn't find
            // any nested tokens (no variable or subexpression.)
            Diagnostics.Assert(PeekChar() == c1, "We accidentally consumed a character we shouldn't have.");

            return false;
        }

        private Token ScanStringExpandable()
        {
            var sb = GetStringBuilder();
            var formatSb = GetStringBuilder();
            List<Token> nestedTokens = new List<Token>();

            TokenFlags flags = ScanStringExpandable(sb, formatSb, nestedTokens);
            return NewStringExpandableToken(GetStringAndRelease(sb), GetStringAndRelease(formatSb), TokenKind.StringExpandable, nestedTokens, flags);
        }

        private bool ScanAfterHereStringHeader(string header)
        {
            // On entry, we've see the header.  We allow whitespace and require a newline before the actual string starts
            int headerOffset = _currentIndex - 2;

            char c;
            do
            {
                c = GetChar();
            } while (c.IsWhitespace());

            if (c == '\r')
            {
                NormalizeCRLF(c);
            }
            else if (c != '\n')
            {
                if (c == '\0' && AtEof())
                {
                    UngetChar();
                    ReportIncompleteInput(headerOffset,
                        nameof(ParserStrings.TerminatorExpectedAtEndOfString),
                        ParserStrings.TerminatorExpectedAtEndOfString,
                        string.Concat(header[1], '@'));
                    return false;
                }

                UngetChar();

                // ErrorRecovery: just ignore the characters and look for a terminator.  If no terminator is found, resume
                // scanning at the end of the line.  Don't skip the newline so we have a newline to terminate the current
                // expression.

                ReportError(_currentIndex,
                    nameof(ParserStrings.UnexpectedCharactersAfterHereStringHeader),
                    ParserStrings.UnexpectedCharactersAfterHereStringHeader);

                while (true)
                {
                    c = GetChar();
                    if (c == header[1] && (PeekChar() == '@'))
                    {
                        SkipChar();
                        break;
                    }

                    if (c == '\r' || c == '\n' || (c == '\0' && AtEof()))
                    {
                        UngetChar();
                        break;
                    }
                }

                return false;
            }

            return true;
        }

        private bool ScanPossibleHereStringFooter(Func<char, bool> test, Action<char> appendChar, ref int falseFooterOffset)
        {
            char c = GetChar();

            // First, check if we found the real terminator.
            if (test(c) && PeekChar() == '@')
            {
                SkipChar();
                return true;
            }

            // Catch whitespace before the terminator so we can issue
            // a better error message than "missing terminator".

            while (c.IsWhitespace())
            {
                appendChar(c);
                c = GetChar();
            }

            if (c == '\r' || c == '\n' || (c == '\0' && AtEof()))
            {
                UngetChar();
                return false;
            }

            if (test(c) && PeekChar() == '@')
            {
                appendChar(c);
                if (falseFooterOffset == -1)
                {
                    // If we don't find a real footer, we'll use this position
                    // to give a helpful error message.
                    falseFooterOffset = _currentIndex - 1;
                }

                appendChar(GetChar());  // append the '@'
            }
            else
            {
                // Unget the character, it might be a '$' or '`' and if we're in a expandable here string, the caller must
                // handle the character.
                UngetChar();
            }

            return false;
        }

        private Token ScanHereStringLiteral()
        {
            // On entry, we've see @'.  Remember the position in case we reach the end of file and want
            // to use this position as the error position.
            int headerOffset = _currentIndex - 2;
            int falseFooterOffset = -1;

            if (!ScanAfterHereStringHeader("@'"))
            {
                return NewStringLiteralToken(string.Empty, TokenKind.HereStringLiteral, TokenFlags.TokenInError);
            }

            TokenFlags flags = TokenFlags.None;
            var sb = GetStringBuilder();
            Action<char> appendChar = c => sb.Append(c);
            if (!ScanPossibleHereStringFooter(CharExtensions.IsSingleQuote, appendChar, ref falseFooterOffset))
            {
                while (true)
                {
                    char c = GetChar();

                    if (c == '\r' || c == '\n')
                    {
                        // Remember the length, we may remove this newline (which is 1 or 2 characters).
                        int length = sb.Length;

                        sb.Append(c);
                        if (c == '\r' && PeekChar() == '\n')
                        {
                            SkipChar();
                            sb.Append('\n');
                        }

                        if (ScanPossibleHereStringFooter(CharExtensions.IsSingleQuote, appendChar, ref falseFooterOffset))
                        {
                            // Remove the last newline appended.
                            sb.Length = length;
                            break;
                        }
                    }
                    else if (c != '\0' || !AtEof())
                    {
                        sb.Append(c);
                    }
                    else
                    {
                        UngetChar();
                        if (falseFooterOffset != -1)
                        {
                            ReportIncompleteInput(falseFooterOffset,
                                nameof(ParserStrings.WhitespaceBeforeHereStringFooter),
                                ParserStrings.WhitespaceBeforeHereStringFooter);
                        }
                        else
                        {
                            ReportIncompleteInput(headerOffset,
                                nameof(ParserStrings.TerminatorExpectedAtEndOfString),
                                ParserStrings.TerminatorExpectedAtEndOfString,
                                "'@");
                        }

                        flags = TokenFlags.TokenInError;
                        break;
                    }
                }
            }

            return NewStringLiteralToken(GetStringAndRelease(sb), TokenKind.HereStringLiteral, flags);
        }

        private Token ScanHereStringExpandable()
        {
            // On entry, we've see @'.  Remember the position in case we reach the end of file and want
            // to use this position as the error position.
            int headerOffset = _currentIndex - 2;

            if (!ScanAfterHereStringHeader("@\""))
            {
                return NewStringExpandableToken(string.Empty, string.Empty, TokenKind.HereStringExpandable, null, TokenFlags.TokenInError);
            }

            TokenFlags flags = TokenFlags.None;
            List<Token> nestedTokens = new List<Token>();
            int falseFooterOffset = -1;
            var sb = GetStringBuilder();
            var formatSb = GetStringBuilder();
            Action<char> appendChar = c => { sb.Append(c); formatSb.Append(c); };
            if (!ScanPossibleHereStringFooter(CharExtensions.IsDoubleQuote, appendChar, ref falseFooterOffset))
            {
                while (true)
                {
                    char c = GetChar();

                    if (c == '\r' || c == '\n')
                    {
                        // Remember the length, we may remove this newline (which is 1 or 2 characters).
                        int length = sb.Length;
                        int formatLength = formatSb.Length;

                        sb.Append(c);
                        formatSb.Append(c);
                        if (c == '\r' && PeekChar() == '\n')
                        {
                            SkipChar();
                            sb.Append('\n');
                            formatSb.Append('\n');
                        }

                        if (ScanPossibleHereStringFooter(CharExtensions.IsDoubleQuote, appendChar, ref falseFooterOffset))
                        {
                            // Remove the last newline appended.
                            sb.Length = length;
                            formatSb.Length = formatLength;
                            break;
                        }

                        continue;
                    }

                    if (c == '$')
                    {
                        if (ScanDollarInStringExpandable(sb, formatSb, true, nestedTokens))
                        {
                            continue;
                        }
                    }
                    else if (c == '`')
                    {
                        // If end of input, go ahead and append the backtick and issue an error later
                        char c1 = PeekChar();
                        if (c1 != 0)
                        {
                            SkipChar();
                            c = Backtick(c1, out char surrogateCharacter);
                            if (surrogateCharacter != s_invalidChar)
                            {
                                sb.Append(c).Append(surrogateCharacter);
                                formatSb.Append(c).Append(surrogateCharacter);
                                continue;
                            }
                        }
                    }

                    if (c == '{' || c == '}')
                    {
                        // In the format string, we need to double up the curlies because we're
                        // replacing variable references and sub-expressions with the appropriate
                        // format expression for string.Format.
                        formatSb.Append(c);
                    }

                    if (c != '\0' || !AtEof())
                    {
                        sb.Append(c);
                        formatSb.Append(c);
                    }
                    else
                    {
                        UngetChar();
                        if (falseFooterOffset != -1)
                        {
                            ReportIncompleteInput(falseFooterOffset,
                                nameof(ParserStrings.WhitespaceBeforeHereStringFooter),
                                ParserStrings.WhitespaceBeforeHereStringFooter);
                        }
                        else
                        {
                            ReportIncompleteInput(headerOffset,
                                nameof(ParserStrings.TerminatorExpectedAtEndOfString),
                                ParserStrings.TerminatorExpectedAtEndOfString,
                                "\"@");
                        }

                        flags = TokenFlags.TokenInError;
                        break;
                    }
                }
            }

            return NewStringExpandableToken(GetStringAndRelease(sb), GetStringAndRelease(formatSb), TokenKind.HereStringExpandable, nestedTokens, flags);
        }

        #endregion Strings

        #region Variables

        // Scan a variable - the first character ($ or @) has been consumed already.
        private Token ScanVariable(bool splatted, bool inStringExpandable)
        {
            int errorStartPosition = _currentIndex;

            var sb = GetStringBuilder();
            char c = GetChar();

            VariablePath path;
            if (c == '{')
            {
                // Braced variable
                Diagnostics.Assert(!splatted, "Splatting is not supported with braced variables.");

                while (true)
                {
                    c = GetChar();

                    switch (c)
                    {
                        case '}':
                            goto end_braced_variable_scan;
                        case '`':
                            {
                                char c1 = GetChar();
                                if (c1 == '\0' && AtEof())
                                {
                                    UngetChar();
                                    goto end_braced_variable_scan;
                                }

                                c = Backtick(c1, out char surrogateCharacter);
                                if (surrogateCharacter != s_invalidChar)
                                {
                                    sb.Append(c).Append(surrogateCharacter);
                                    continue;
                                }

                                break;
                            }
                        case '"':
                        case SpecialChars.QuoteDoubleLeft:
                        case SpecialChars.QuoteDoubleRight:
                        case SpecialChars.QuoteLowDoubleLeft:
                            if (inStringExpandable)
                            {
                                char c1 = GetChar();
                                if (c1 == '\0' && AtEof())
                                {
                                    UngetChar();
                                    goto end_braced_variable_scan;
                                }

                                if (c1.IsDoubleQuote())
                                {
                                    c = c1;
                                }
                                else
                                {
                                    UngetChar();
                                }
                            }

                            break;
                        case '{':
                            ReportError(_currentIndex,
                                nameof(ParserStrings.OpenBraceNeedsToBeBackTickedInVariableName),
                                ParserStrings.OpenBraceNeedsToBeBackTickedInVariableName);
                            break;
                        case '\0':
                            if (AtEof())
                            {
                                UngetChar();
                                goto end_braced_variable_scan;
                            }

                            break;
                    }

                    sb.Append(c);
                }

            end_braced_variable_scan:

                string name = GetStringAndRelease(sb);
                if (c != '}')
                {
                    ReportIncompleteInput(errorStartPosition,
                        nameof(ParserStrings.IncompleteDollarVariableReference),
                        ParserStrings.IncompleteDollarVariableReference);
                }

                if (name.Length == 0)
                {
                    if (c == '}')
                    {
                        ReportError(_currentIndex - 1,
                            nameof(ParserStrings.EmptyVariableReference),
                            ParserStrings.EmptyVariableReference);
                    }

                    name = ":Error:";
                }

                if (InCommandMode())
                {
                    char c1 = PeekChar();

                    // A '.' or '[' after the variable name in command mode is an operator, not part of the current token.
                    if (!c1.ForceStartNewToken() && c1 != '.' && c1 != '[')
                    {
                        // The simple way to get this variable included in the nested tokens is to just start
                        // scanning all over again, but from the context of scanning a generic token.
                        _currentIndex = _tokenStart;
                        return ScanGenericToken(GetStringBuilder());
                    }
                }

                path = new VariablePath(name);
                if (string.IsNullOrEmpty(path.UnqualifiedPath))
                {
                    // Enable if we decide we still need to support
                    //     "${}"  or "$var:"
                    // if (inStringExpandable)
                    // {
                    //    return NewToken(TokenKind.Unknown);
                    // }

                    ReportError(NewScriptExtent(_tokenStart, _currentIndex),
                        nameof(ParserStrings.InvalidBracedVariableReference),
                        ParserStrings.InvalidBracedVariableReference);
                }

                return NewVariableToken(path, false);
            }

            if (!c.IsVariableStart())
            {
                UngetChar();
                sb.Append('$');
                return ScanGenericToken(sb);
            }

            // Normal variable, not braced.
            sb.Append(c);

            // $$, $?, and $^ can only be single character variables.  Otherwise keep scanning.
            if (!(c == '$' || c == '?' || c == '^'))
            {
                bool scanning = true;
                while (scanning)
                {
                    c = GetChar();
                    switch (c)
                    {
                        case 'a':
                        case 'b':
                        case 'c':
                        case 'd':
                        case 'e':
                        case 'f':
                        case 'g':
                        case 'h':
                        case 'i':
                        case 'j':
                        case 'k':
                        case 'l':
                        case 'm':
                        case 'n':
                        case 'o':
                        case 'p':
                        case 'q':
                        case 'r':
                        case 's':
                        case 't':
                        case 'u':
                        case 'v':
                        case 'w':
                        case 'x':
                        case 'y':
                        case 'z':
                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                        case 'E':
                        case 'F':
                        case 'G':
                        case 'H':
                        case 'I':
                        case 'J':
                        case 'K':
                        case 'L':
                        case 'M':
                        case 'N':
                        case 'O':
                        case 'P':
                        case 'Q':
                        case 'R':
                        case 'S':
                        case 'T':
                        case 'U':
                        case 'V':
                        case 'W':
                        case 'X':
                        case 'Y':
                        case 'Z':
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '_':
                        case '?':
                            sb.Append(c);
                            break;

                        case ':':
                            if (PeekChar() == ':')
                            {
                                // Something like $a::b is static member access
                                UngetChar();
                                scanning = false;
                            }
                            else
                            {
                                sb.Append(c);
                            }

                            break;

                        case '\0':
                        case '\t':
                        case '\r':
                        case '\n':
                        case ' ':
                        case '&':
                        case '(':
                        case ')':
                        case ',':
                        case ';':
                        case '{':
                        case '}':
                        case '|':
                        // The above cases would also be handled correctly below in the default case,
                        // but we can avoid extra checks on some of these characters which commonly
                        // occur after a variable.
                        case '.':
                        case '[':
                        // Something like $a.b or $a[1].
                        case '=':
                            // Something like $a=
                            UngetChar();
                            scanning = false;
                            break;

                        default:
                            if (char.IsLetterOrDigit(c))
                            {
                                sb.Append(c);
                            }
                            else if (InCommandMode() && !c.ForceStartNewToken())
                            {
                                _currentIndex = _tokenStart;
                                sb.Clear();
                                return ScanGenericToken(sb);
                            }
                            else
                            {
                                UngetChar();
                                scanning = false;
                            }

                            break;
                    }
                }
            }
            else if (InCommandMode() && !PeekChar().ForceStartNewToken())
            {
                _currentIndex = _tokenStart;
                sb.Clear();
                return ScanGenericToken(sb);
            }

            path = new VariablePath(GetStringAndRelease(sb));
            if (string.IsNullOrEmpty(path.UnqualifiedPath))
            {
                string errorId;
                string errorMsg;
                if (path.IsDriveQualified)
                {
                    errorId = nameof(ParserStrings.InvalidVariableReferenceWithDrive);
                    errorMsg = ParserStrings.InvalidVariableReferenceWithDrive;
                }
                else
                {
                    errorId = nameof(ParserStrings.InvalidVariableReference);
                    errorMsg = ParserStrings.InvalidVariableReference;
                }

                ReportError(NewScriptExtent(_tokenStart, _currentIndex), errorId, errorMsg);
            }

            return NewVariableToken(path, splatted);
        }

        #endregion Variables

        // Scan either a parameter or an operator, in either case it must start with a dash.
        private Token ScanParameter()
        {
            var sb = GetStringBuilder();

            bool scanning = true;
            bool sawColonAtEnd = false;
            while (scanning)
            {
                char c = GetChar();

                if (c.IsWhitespace())
                {
                    UngetChar();
                    break;
                }

                switch (c)
                {
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                    case ';':
                    case ',':
                    case '|':
                    case '&':
                    case '.':
                    case '[':
                    case '\r':
                    case '\n':
                    case '\0':
                        UngetChar();
                        scanning = false;
                        break;

                    case ':':
                        scanning = false;
                        sawColonAtEnd = true;
                        if (!InCommandMode())
                        {
                            UngetChar();
                        }

                        break;

                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                        sb.Append(c);
                        break;

                    case '\'':
                    case SpecialChars.QuoteSingleLeft:
                    case SpecialChars.QuoteSingleRight:
                    case SpecialChars.QuoteSingleBase:
                    case SpecialChars.QuoteReversed:
                    case '"':
                    case SpecialChars.QuoteDoubleLeft:
                    case SpecialChars.QuoteDoubleRight:
                    case SpecialChars.QuoteLowDoubleLeft:
                        if (InCommandMode())
                        {
                            // Quotes are never part of a parameter.  Treat the token as an argument.
                            UngetChar();
                            sb.Insert(0, _script[_tokenStart]); // Insert the '-' that we skipped.
                            return ScanGenericToken(sb);
                        }

                        UngetChar();
                        scanning = false;
                        break;

                    default:
                        if (InCommandMode())
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            UngetChar();
                            scanning = false;
                        }

                        break;
                }
            }

            var str = GetStringAndRelease(sb);

            if (InExpressionMode())
            {
                TokenKind operatorKind;
                if (s_operatorTable.TryGetValue(str, out operatorKind))
                {
                    return NewToken(operatorKind);
                }
            }

            if (str.Length == 0)
            {
                return NewToken(TokenKind.Minus);
            }

            return NewParameterToken(str, sawColonAtEnd);
        }

        private Token CheckOperatorInCommandMode(char c, TokenKind tokenKind)
        {
            if (InCommandMode() && !PeekChar().ForceStartNewToken())
            {
                return ScanGenericToken(c);
            }

            return NewToken(tokenKind);
        }

        private Token CheckOperatorInCommandMode(char c1, char c2, TokenKind tokenKind)
        {
            if (InCommandMode() && !PeekChar().ForceStartNewToken())
            {
                var sb = GetStringBuilder();
                sb.Append(c1);
                sb.Append(c2);
                return ScanGenericToken(sb);
            }

            return NewToken(tokenKind);
        }

        private Token ScanGenericToken(char firstChar)
        {
            var sb = GetStringBuilder();
            sb.Append(firstChar);
            return ScanGenericToken(sb);
        }

        private Token ScanGenericToken(char firstChar, char surrogateCharacter)
        {
            var sb = GetStringBuilder();
            sb.Append(firstChar);
            if (surrogateCharacter != s_invalidChar)
            {
                sb.Append(surrogateCharacter);
            }

            return ScanGenericToken(sb);
        }

        private Token ScanGenericToken(StringBuilder sb)
        {
            // On entry, we've already scanned an unknown number of characters
            // and found a character that didn't end the token, but made the
            // token something other than what we thought it was.  Examples:
            //    77z   <= it looks like a number, but the 'z' makes it an argument
            //    $+    <= it looks like a variable, but the '+' makes it an argument
            //
            // A generic token is typically either command name or command argument
            // (though a generic token is accepted in other places, such as a hash key
            // or function name.)
            //
            // A generic token can be taken literally or treated as an expandable
            // string, depending on the context.  A command argument would treat
            // a generic token as an expandable string whereas a command name would
            // not.
            //
            // We optimize for the command argument case - if we find anything expandable,
            // we continue processing assuming the string is expandable, so we'll tokenize
            // sub-expressions and variable names.  This would be considered extra work
            // if the token was a command name, so we assume that will happen rarely, and
            // indeed, '$' is not commonly used in command names.

            // Make sure our token does not start with any of these characters.
            // Contract.Requires(Contract.ForAll("{}()@#;,|&\r\n\t ", c1 => sb[0] != c1));

            List<Token> nestedTokens = new List<Token>();
            var formatSb = GetStringBuilder();
            formatSb.Append(sb);

            char c = GetChar();
            for (; !c.ForceStartNewToken(); c = GetChar())
            {
                if (c == '`')
                {
                    // If end of input, we'll just append the backtick - there should be no error.
                    char c1 = PeekChar();
                    if (c1 != 0)
                    {
                        SkipChar();
                        c = Backtick(c1, out char surrogateCharacter);
                        if (surrogateCharacter != s_invalidChar)
                        {
                            sb.Append(c).Append(surrogateCharacter);
                            formatSb.Append(c).Append(surrogateCharacter);
                            continue;
                        }
                    }
                }
                else if (c.IsSingleQuote())
                {
                    int len = sb.Length;
                    ScanStringLiteral(sb);
                    for (int i = len; i < sb.Length; ++i)
                    {
                        formatSb.Append(sb[i]);
                    }

                    continue;
                }
                else if (c.IsDoubleQuote())
                {
                    ScanStringExpandable(sb, formatSb, nestedTokens);
                    continue;
                }
                else if (c == '$')
                {
                    if (ScanDollarInStringExpandable(sb, formatSb, false, nestedTokens))
                    {
                        continue;
                    }
                }

                sb.Append(c);
                formatSb.Append(c);
                if (c == '{' || c == '}')
                {
                    formatSb.Append(c);
                }
            }

            UngetChar();

            var str = GetStringAndRelease(sb);
            if (nestedTokens.Count > 0)
            {
                return NewGenericExpandableToken(str, GetStringAndRelease(formatSb), nestedTokens);
            }

            Release(formatSb);

            if (DynamicKeyword.ContainsKeyword(str) && !DynamicKeyword.IsHiddenKeyword(str))
            {
                return NewToken(TokenKind.DynamicKeyword);
            }

            return NewGenericToken(str);
        }

        #region Numbers

        private void ScanHexDigits(StringBuilder sb)
        {
            char c = PeekChar();
            while (c.IsHexDigit())
            {
                SkipChar();
                sb.Append(c);
                c = PeekChar();
            }
        }

        private int ScanDecimalDigits(StringBuilder sb)
        {
            int countDigits = 0;
            char c = PeekChar();
            while (c.IsDecimalDigit())
            {
                countDigits += 1;
                SkipChar();
                sb.Append(c);
                c = PeekChar();
            }

            return countDigits;
        }

        private void ScanBinaryDigits(StringBuilder sb)
        {
            char c = PeekChar();
            while (c.IsBinaryDigit())
            {
                SkipChar();
                sb.Append(c);
                c = PeekChar();
            }
        }

        private void ScanExponent(StringBuilder sb, ref int signIndex, ref bool notNumber)
        {
            char c = PeekChar();
            if (c == '+' || c.IsDash())
            {
                SkipChar();
                // Append the sign - but remember where it was appended.  If we really
                // do have a number, we'll replace en-dash/em-dash/horizontal-bar with dash,
                // but if it's a generic token, we don't want to do the replacement.
                signIndex = sb.Length;
                sb.Append(c);
            }

            if (ScanDecimalDigits(sb) == 0)
            {
                notNumber = true;
            }
        }

        private void ScanNumberAfterDot(StringBuilder sb, ref int signIndex, ref bool notNumber)
        {
            ScanDecimalDigits(sb);
            // No need to verify we saw a digit here.
            //    1.e1 is valid
            //    .e1 is not, but ScanDot ensures the next character is a digit

            char c = PeekChar();
            if (c == 'e' || c == 'E')
            {
                SkipChar();
                sb.Append(c);
                ScanExponent(sb, ref signIndex, ref notNumber);
            }
        }

        private static bool TryGetNumberValue(
            ReadOnlySpan<char> strNum,
            NumberFormat format,
            NumberSuffixFlags suffix,
            bool real,
            long multiplier,
            out object result)
        {
            checked
            {
                try
                {
                    NumberStyles style = NumberStyles.AllowLeadingSign
                        | NumberStyles.AllowDecimalPoint
                        | NumberStyles.AllowExponent;

                    if (real)
                    {
                        // Decimal parser does not accept hex literals, and 'd' is a valid hex character, so will
                        // never be read as Decimal literal
                        // e.g., 0x1d == 29
                        if (suffix == NumberSuffixFlags.Decimal)
                        {
                            if (decimal.TryParse(strNum, style, NumberFormatInfo.InvariantInfo, out decimal d))
                            {
                                result = d * multiplier;
                                return true;
                            }

                            result = null;
                            return false;
                        }

                        if (double.TryParse(strNum, style, NumberFormatInfo.InvariantInfo, out double doubleValue))
                        {
                            // TryParse incorrectly return +0 when the result should be -0, so check for that case
                            if (doubleValue == 0.0 && strNum[0] == '-')
                            {
                                doubleValue = -0.0;
                            }

                            doubleValue *= multiplier;
                            switch (suffix)
                            {
                                case NumberSuffixFlags.None:
                                    result = doubleValue;
                                    return true;
                                case NumberSuffixFlags.SignedByte:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out sbyte sb))
                                    {
                                        result = sb;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.UnsignedByte:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out byte b))
                                    {
                                        result = b;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.Short:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out short s))
                                    {
                                        result = s;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.Long:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out long l))
                                    {
                                        result = l;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.UnsignedShort:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out ushort us))
                                    {
                                        result = us;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.Unsigned:
                                    BigInteger testValue = doubleValue.AsBigInt();
                                    if (Utils.TryCast(testValue, out uint u))
                                    {
                                        result = u;
                                        return true;
                                    }
                                    else if (Utils.TryCast(testValue, out ulong ul))
                                    {
                                        result = ul;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.UnsignedLong:
                                    if (Utils.TryCast(doubleValue.AsBigInt(), out ulong ulValue))
                                    {
                                        result = ulValue;
                                        return true;
                                    }

                                    break;
                                case NumberSuffixFlags.BigInteger:
                                    result = doubleValue.AsBigInt();
                                    return true;
                            }

                            // Invalid NumberSuffixFlags combination, or outside bounds of specified type.
                            result = null;
                            return false;
                        }

                        // TryParse for real numeric literal failed
                        result = null;
                        return false;
                    }

                    BigInteger bigValue;

                    switch (format)
                    {
                        case NumberFormat.Hex:
                            if (!strNum[0].IsHexDigit())
                            {
                                if (strNum[0] == '-')
                                {
                                    multiplier = -multiplier;
                                }

                                // Remove leading char (expected: - or +)
                                strNum = strNum.Slice(1);
                            }

                            // If we're expecting a sign bit, remove the leading 0 added in ScanNumberHelper
                            if (!suffix.HasFlag(NumberSuffixFlags.Unsigned))
                            {
                                var expectedLength = suffix switch
                                {
                                    NumberSuffixFlags.SignedByte => 2,
                                    NumberSuffixFlags.Short => 4,
                                    NumberSuffixFlags.Long => 16,
                                    // No suffix flag can mean int or long depending on input string length
                                    _ => strNum.Length < 16 ? 8 : 16
                                };

                                if (strNum.Length == expectedLength + 1)
                                {
                                    strNum = strNum.Slice(1);
                                }
                            }

                            style = NumberStyles.AllowHexSpecifier;
                            if (!BigInteger.TryParse(strNum, style, NumberFormatInfo.InvariantInfo, out bigValue))
                            {
                                result = null;
                                return false;
                            }

                            // If we have a hex literal denoting (u)int64, treat it as such, even if the value is low
                            if (strNum.Length == 16 && (suffix == NumberSuffixFlags.None || suffix == NumberSuffixFlags.Unsigned))
                            {
                                suffix |= NumberSuffixFlags.Long;
                            }

                            break;
                        case NumberFormat.Binary:
                            if (!strNum[0].IsBinaryDigit())
                            {
                                if (strNum[0] == '-')
                                {
                                    multiplier = -multiplier;
                                }

                                // Remove leading char (expected: - or +)
                                strNum = strNum.Slice(1);
                            }

                            bigValue = Utils.ParseBinary(strNum, suffix.HasFlag(NumberSuffixFlags.Unsigned));

                            // If we have a binary literal denoting (u)int64, treat it as such
                            if (strNum.Length == 64 && (suffix == NumberSuffixFlags.None || suffix == NumberSuffixFlags.Unsigned))
                            {
                                suffix |= NumberSuffixFlags.Long;
                            }

                            break;
                        default:
                            style = NumberStyles.AllowLeadingSign;
                            if (!BigInteger.TryParse(strNum, style, NumberFormatInfo.InvariantInfo, out bigValue))
                            {
                                result = null;
                                return false;
                            }

                            break;
                    }

                    // Apply multiplier before attempting casting to prevent overflow
                    bigValue *= multiplier;

                    switch (suffix)
                    {
                        case NumberSuffixFlags.SignedByte:
                            if (Utils.TryCast(bigValue, out sbyte sb))
                            {
                                result = sb;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.UnsignedByte:
                            if (Utils.TryCast(bigValue, out byte b))
                            {
                                result = b;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.Short:
                            if (Utils.TryCast(bigValue, out short s))
                            {
                                result = s;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.Long:
                            if (Utils.TryCast(bigValue, out long l))
                            {
                                result = l;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.UnsignedShort:
                            if (Utils.TryCast(bigValue, out ushort us))
                            {
                                result = us;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.Unsigned:
                            if (Utils.TryCast(bigValue, out uint u))
                            {
                                result = u;
                                return true;
                            }
                            else if (Utils.TryCast(bigValue, out ulong ul))
                            {
                                result = ul;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.UnsignedLong:
                            if (Utils.TryCast(bigValue, out ulong ulValue))
                            {
                                result = ulValue;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.Decimal:
                            if (Utils.TryCast(bigValue, out decimal dm))
                            {
                                result = dm;
                                return true;
                            }

                            break;
                        case NumberSuffixFlags.BigInteger:
                            result = bigValue;
                            return true;
                        case NumberSuffixFlags.None:
                            // Type not specified; fit value into narrowest signed type available, int32 minimum
                            if (Utils.TryCast(bigValue, out int i))
                            {
                                result = i;
                                return true;
                            }

                            if (Utils.TryCast(bigValue, out long lValue))
                            {
                                result = lValue;
                                return true;
                            }

                            // Result is too big for anything else; fallback to decimal or double
                            if (format == NumberFormat.Decimal)
                            {
                                if (Utils.TryCast(bigValue, out decimal dmValue))
                                {
                                    result = dmValue;
                                    return true;
                                }

                                if (Utils.TryCast(bigValue, out double d))
                                {
                                    result = d;
                                    return true;
                                }
                            }

                            // Hex or Binary value, too big for generic non-suffixed parsing
                            result = null;
                            return false;
                    }

                    // Value cannot be contained in type specified by suffix, or invalid suffix flags.
                    result = null;
                    return false;
                }
                catch (Exception)
                {
                }
            }

            result = null;
            return false;
        }

        private Token ScanNumber(char firstChar)
        {
            Diagnostics.Assert(
                firstChar == '.' || (firstChar >= '0' && firstChar <= '9')
                || (AllowSignedNumbers && (firstChar == '+' || firstChar.IsDash())), "Number must start with '.', '-', or digit.");

            string strNum = ScanNumberHelper(firstChar, out NumberFormat format, out NumberSuffixFlags suffix, out bool real, out long multiplier);

            // the token is not a number. i.e. 77z.exe
            if (strNum == null)
            {
                // Rescan the characters, this is simpler than keeping track of the suffix, multiplier, and 0x prefix.
                _currentIndex = _tokenStart;
                return ScanGenericToken(GetStringBuilder());
            }

            object value;
            if (!TryGetNumberValue(strNum, format, suffix, real, multiplier, out value))
            {
                if (!InExpressionMode())
                {
                    // Rescan the characters, this is simpler than keeping track of the dashes and the hex prefix.
                    _currentIndex = _tokenStart;
                    return ScanGenericToken(GetStringBuilder());
                }

                ReportError(
                    NewScriptExtent(_tokenStart, _currentIndex),
                    nameof(ParserStrings.BadNumericConstant),
                    ParserStrings.BadNumericConstant,
                    _script.Substring(_tokenStart, _currentIndex - _tokenStart));
            }

            return NewNumberToken(value);
        }

        /// <summary>
        /// Scans a numeric string to determine its characteristics.
        /// </summary>
        /// <param name="firstChar">The first character.</param>
        /// <param name="format">Indicate if it's a hex, binary, or decimal number.</param>
        /// <param name="suffix">Indicate the format suffix.</param>
        /// <param name="real">Indicate if the number is real (non-integer).</param>
        /// <param name="multiplier">Indicate the specified multiplier.</param>
        /// <returns>
        /// Return null if the token is not a number
        /// OR
        /// Return the string format of the number.
        /// </returns>
        private string ScanNumberHelper(char firstChar, out NumberFormat format, out NumberSuffixFlags suffix, out bool real, out long multiplier)
        {
            format = NumberFormat.Decimal;
            suffix = NumberSuffixFlags.None;
            real = false;
            multiplier = 1;

            bool notNumber = false;
            int signIndex = -1;
            char c;
            var sb = GetStringBuilder();

            if (firstChar.IsDash() || firstChar == '+')
            {
                sb.Append(firstChar);
                firstChar = GetChar();
            }

            if (firstChar == '.')
            {
                sb.Append('.');
                ScanNumberAfterDot(sb, ref signIndex, ref notNumber);
                real = true;
            }
            else
            {
                c = PeekChar();
                bool isHexOrBinary = firstChar == '0' && (c == 'x' || c == 'X' || c == 'b' || c == 'B');

                if (isHexOrBinary)
                {
                    SkipChar();

                    switch (c)
                    {
                        case 'x':
                        case 'X':
                            sb.Append('0'); // Prepend a 0 to the number before any numeric digits are added
                            ScanHexDigits(sb);
                            if (sb.Length == 0)
                            {
                                notNumber = true;
                            }

                            format = NumberFormat.Hex;
                            break;
                        case 'b':
                        case 'B':
                            ScanBinaryDigits(sb);
                            if (sb.Length == 0)
                            {
                                notNumber = true;
                            }

                            format = NumberFormat.Binary;
                            break;
                    }
                }
                else
                {
                    sb.Append(firstChar);
                    ScanDecimalDigits(sb);
                    c = PeekChar();
                    switch (c)
                    {
                        case '.':
                            SkipChar();
                            if (PeekChar() == '.')
                            {
                                // We just found the range operator, so unget the first dot so
                                // we can stop scanning as a number.
                                UngetChar();
                            }
                            else
                            {
                                sb.Append(c);
                                real = true;
                                ScanNumberAfterDot(sb, ref signIndex, ref notNumber);
                            }

                            break;
                        case 'E':
                        case 'e':
                            SkipChar();
                            sb.Append(c);
                            real = true;
                            ScanExponent(sb, ref signIndex, ref notNumber);
                            break;
                    }
                }
            }

            c = PeekChar();
            if (c.IsTypeSuffix())
            {
                SkipChar();
                switch (c)
                {
                    case 'u':
                    case 'U':
                        suffix |= NumberSuffixFlags.Unsigned;
                        break;
                    case 's':
                    case 'S':
                        suffix |= NumberSuffixFlags.Short;
                        break;
                    case 'l':
                    case 'L':
                        suffix |= NumberSuffixFlags.Long;
                        break;
                    case 'd':
                    case 'D':
                        suffix |= NumberSuffixFlags.Decimal;
                        break;
                    case 'y':
                    case 'Y':
                        suffix |= NumberSuffixFlags.SignedByte;
                        break;
                    case 'n':
                    case 'N':
                        suffix |= NumberSuffixFlags.BigInteger;
                        break;
                    default:
                        notNumber = true;
                        break;
                }

                c = PeekChar();

                if (c.IsTypeSuffix())
                {
                    SkipChar();
                    switch (suffix)
                    {
                        case NumberSuffixFlags.Unsigned:
                            switch (c)
                            {
                                case 'l':
                                case 'L':
                                    suffix |= NumberSuffixFlags.Long;
                                    break;
                                case 's':
                                case 'S':
                                    suffix |= NumberSuffixFlags.Short;
                                    break;
                                case 'y':
                                case 'Y':
                                    suffix |= NumberSuffixFlags.SignedByte;
                                    break;
                                default:
                                    notNumber = true;
                                    break;
                            }

                            break;
                        default:
                            notNumber = true;
                            break;
                    }

                    c = PeekChar();
                }
            }

            if (c.IsMultiplierStart())
            {
                SkipChar();

                switch (c)
                {
                    case 'k':
                    case 'K':
                        multiplier = 1024;
                        break;
                    case 'm':
                    case 'M':
                        multiplier = 1024 * 1024;
                        break;
                    case 'g':
                    case 'G':
                        multiplier = 1024 * 1024 * 1024;
                        break;
                    case 't':
                    case 'T':
                        multiplier = 1024L * 1024 * 1024 * 1024;
                        break;
                    case 'p':
                    case 'P':
                        multiplier = 1024L * 1024 * 1024 * 1024 * 1024;
                        break;
                }

                char c1 = PeekChar();
                if (c1 == 'b' || c1 == 'B')
                {
                    SkipChar();
                    c = PeekChar();
                }
                else
                {
                    notNumber = true;
                }
            }

            if (!c.ForceStartNewToken())
            {
                if (!InExpressionMode() || !c.ForceStartNewTokenAfterNumber(ForceEndNumberOnTernaryOpChars))
                {
                    notNumber = true;
                }
            }

            if (notNumber)
            {
                Release(sb);
                return null;
            }

            // TryParse won't accept odd dashes for a sign, so replace them.
            if (signIndex != -1 && sb[signIndex] != '-' && sb[signIndex].IsDash())
            {
                sb[signIndex] = '-';
            }

            if (sb[0] != '-' && sb[0].IsDash())
            {
                sb[0] = '-';
            }

            return GetStringAndRelease(sb);
        }

        #endregion Numbers

        internal Token GetMemberAccessOperator(bool allowLBracket)
        {
            // No skipping whitespace here - whitespace isn't allowed before member access operators.
            // We do skip multi-line comments though, they are allowed (mostly for backwards compatibility.)

            char c = PeekChar();

            while (c == '<')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                if (PeekChar() == '#')
                {
                    SkipChar();
                    ScanBlockComment();
                    c = PeekChar();
                }
                else
                {
                    UngetChar();
                    return null;
                }
            }

            if (c == '.')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                c = PeekChar();
                if (c != '.')
                {
                    if (InCommandMode() && (c.IsWhitespace() || c == '\0' || c == '\r' || c == '\n'))
                    {
                        UngetChar();
                        return null;
                    }

                    return NewToken(TokenKind.Dot);
                }

                UngetChar();
                return null;
            }

            if (c == ':')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                c = PeekChar();
                if (c == ':')
                {
                    SkipChar();
                    c = PeekChar();
                    if (InCommandMode() && (c.IsWhitespace() || c == '\0' || c == '\r' || c == '\n'))
                    {
                        UngetChar();
                        UngetChar();
                        return null;
                    }

                    return NewToken(TokenKind.ColonColon);
                }

                UngetChar();
                return null;
            }

            if (c == '[' && allowLBracket)
            {
                _tokenStart = _currentIndex;
                SkipChar();
                return NewToken(TokenKind.LBracket);
            }

            if (c == '?')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                c = PeekChar();
                if (c == '.')
                {
                    SkipChar();
                    return NewToken(TokenKind.QuestionDot);
                }
                else if (c == '[' && allowLBracket)
                {
                    SkipChar();
                    return NewToken(TokenKind.QuestionLBracket);
                }

                UngetChar();
                return null;
            }

            return null;
        }

        internal Token GetInvokeMemberOpenParen()
        {
            // No skipping whitespace here - whitespace isn't allowed before the open paren of a function invocation.
            var c = PeekChar();
            if (c == '(')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                return NewToken(TokenKind.LParen);
            }

            if (c == '{')
            {
                _tokenStart = _currentIndex;
                SkipChar();
                return NewToken(TokenKind.LCurly);
            }

            return null;
        }

        internal Token GetLBracket()
        {
            // We know we want a '[' token or no token.  We are in a context where we expect an attribute/type constraint
            // and allow any whitespace/comments before the '[', but nothing else (the caller has already skipped newlines
            // if appropriate.)  This is handled specially because in command mode, a generic token may begin with '[', but
            // we don't want anything more than the '['.

            // Remember where we started.  In some rare cases, we may need to sync back to make things a little
            // simpler in the parser.

            int resyncPoint = _currentIndex;
            bool resyncIfMemberAccess = false;
        again:
            _tokenStart = _currentIndex;
            char c = GetChar();
            switch (c)
            {
                case ' ':
                case '\t':
                case '\f':
                case '\v':
                case SpecialChars.NoBreakSpace:
                case SpecialChars.NextLine:
                    resyncIfMemberAccess = true;
                    SkipWhiteSpace();
                    goto again;

                case '#':
                    resyncIfMemberAccess = true;
                    ScanLineComment();
                    goto again;

                case '<':
                    if (PeekChar() == '#')
                    {
                        // We resync if we find any whitespace, but only if the whitespace occurs after a
                        // multi-line comment (rationale: backwards compatibility.)
                        resyncIfMemberAccess = false;
                        SkipChar();
                        ScanBlockComment();
                        goto again;
                    }

                    UngetChar();
                    break;

                case '[':
                    return NewToken(TokenKind.LBracket);

                case '.':
                case ':':
                    // We don't call resync here unless we might have a member access token, in which case
                    // we want to rescan the comments to ensure there is no whitespace between the expression
                    // and the member access token.  The resync here should rarely do much other than move
                    // the _currentIndex because there will rarely be any comment tokens after the closing ']'
                    // in an attribute and the member access token.
                    if (resyncIfMemberAccess)
                    {
                        Resync(resyncPoint);
                    }
                    else
                    {
                        UngetChar();
                    }

                    break;

                default:
                    if (c.IsWhitespace())
                    {
                        resyncIfMemberAccess = true;
                        SkipWhiteSpace();
                        goto again;
                    }

                    UngetChar();
                    break;
            }

            return null;
        }

        private Token ScanDot()
        {
            char c = PeekChar();
            if (c == '.')
            {
                SkipChar();
                c = PeekChar();
                if (InCommandMode() && !c.ForceStartNewToken())
                {
                    UngetChar();  // Unget the second dot, let ScanGenericToken consume it.
                    return ScanGenericToken('.');
                }

                return NewToken(TokenKind.DotDot);
            }

            if (c.IsDecimalDigit())
            {
                return ScanNumber('.');
            }

            // The following are all dotting commands, not a command starting with dot:
            //     .$command
            //     ."command with spaces"
            //     .'command with spaces'
            if (InCommandMode() && !c.ForceStartNewToken() && c != '$' && c != '"' && c != '\'')
            {
                return ScanGenericToken('.');
            }

            return NewToken(TokenKind.Dot);
        }

        // The first character is an ascii letter.  We could be scanning a keyword,
        // a command name or argument, or a method or property name.
        private Token ScanIdentifier(char firstChar)
        {
            var sb = GetStringBuilder();
            char c;
            sb.Append(firstChar);

            while (true)
            {
                c = GetChar();

                if (c.IsIdentifierFollow())
                {
                    sb.Append(c);
                }
                else
                {
                    UngetChar();
                    break;
                }
            }

            // In typename mode, we want, well, typenames.
            if (InTypeNameMode())
            {
                Release(sb);
                return ScanTypeName();
            }

            // In expression mode, we only want simple identifiers so we're done,
            // but in command mode, we keep scanning if the current character
            // doesn't terminate the token.
            if (!WantSimpleName && InCommandMode() && !c.ForceStartNewToken())
            {
                return ScanGenericToken(sb);
            }

            if (!WantSimpleName && (InCommandMode() || InSignatureMode()))
            {
                TokenKind tokenKind;
                var ident = GetStringAndRelease(sb);
                sb = null;
                if (s_keywordTable.TryGetValue(ident, out tokenKind))
                {
                    if (tokenKind != TokenKind.InlineScript || InWorkflowContext)
                        return NewToken(tokenKind);
                }

                if (DynamicKeyword.ContainsKeyword(ident) && !DynamicKeyword.IsHiddenKeyword(ident))
                {
                    return NewToken(TokenKind.DynamicKeyword);
                }
            }

            if (sb != null)
                Release(sb);
            return NewToken(TokenKind.Identifier);
        }

        #region Type Names

        private Token ScanTypeName()
        {
            while (true)
            {
                char c = GetChar();

                switch (c)
                {
                    case '.':
                    case '`':
                    case '_':
                    case '+':
                    case '#':
                    case '\\':
                        continue;
                    default:
                        if (char.IsLetterOrDigit(c))
                        {
                            continue;
                        }

                        break;
                }

                UngetChar();
                break;
            }

            var result = NewToken(TokenKind.Identifier);
            result.TokenFlags |= TokenFlags.TypeName;
            return result;
        }

        private void ScanAssemblyNameSpecToken(StringBuilder sb)
        {
            // Whitespace, ',', '=', ']', and newlines terminate a token.

            SkipWhiteSpace();

            _tokenStart = _currentIndex;
            while (true)
            {
                char c = GetChar();
                if (c.ForceStartNewTokenInAssemblyNameSpec())
                {
                    UngetChar();
                    break;
                }

                sb.Append(c);
            }

            var token = NewToken(TokenKind.Identifier);
            token.TokenFlags |= TokenFlags.TypeName;
            SkipWhiteSpace();
        }

        internal string GetAssemblyNameSpec()
        {
            // G  assembly-name-spec:
            // G      assembly-name
            // G      assembly-name   ','   assembly-properties
            // G  assembly-name:
            // G      assembly-token
            // G  assembly-properties:
            // G      assembly-property
            // G      assembly-properties   ','   assembly-property
            // G  assembly-property:
            // G      assembly-property-name   '='   assembly-property-value
            // G  assembly-property-name:  one of
            // G      'Version'
            // G      'PublicKey'
            // G      'PublicKeyToken'
            // G      'Culture'
            // G      'Custom'
            // G  assembly-property:
            // G      assembly-token
            // G  assembly-token:
            // G      any sequence of characters not ending in whitespace, newlines, ',', '=', or ']'.

            // The above grammar is specified by the CLR (except assembly-token).  We defer validation to the CLR, but
            // use the above grammar to collect the name of the assembly.

            var sb = GetStringBuilder();

            // Assembly name first, followed by optional properties.
            ScanAssemblyNameSpecToken(sb);

            while (PeekChar() == ',')
            {
                _tokenStart = _currentIndex;
                sb.Append(", ");
                SkipChar();
                NewToken(TokenKind.Comma);

                ScanAssemblyNameSpecToken(sb);
                if (PeekChar() == '=')
                {
                    _tokenStart = _currentIndex;
                    sb.Append('=');
                    SkipChar();
                    NewToken(TokenKind.Equals);
                    ScanAssemblyNameSpecToken(sb);
                }
                // else error?
            }

            return GetStringAndRelease(sb);
        }

        #endregion Type Names

        private Token ScanLabel()
        {
            var sb = GetStringBuilder();

            char c = GetChar();
            if (!c.IsIdentifierStart())
            {
                // Must be a generic token then
                sb.Append(':');
                if (c == '\0')
                {
                    UngetChar();
                    return NewGenericToken(GetStringAndRelease(sb));
                }

                UngetChar();
                return ScanGenericToken(sb);
            }

            while (c.IsIdentifierFollow())
            {
                sb.Append(c);
                c = GetChar();
            }

            // In command mode, we keep scanning if the current character
            // doesn't terminate the token.
            if (InCommandMode() && !c.ForceStartNewToken())
            {
                sb.Insert(0, ':');
                sb.Append(c);
                return ScanGenericToken(sb);
            }

            UngetChar();
            return NewLabelToken(GetStringAndRelease(sb));
        }

        internal Token NextToken()
        {
            char c1;

        again:
            _tokenStart = _currentIndex;
            char c = GetChar();
            switch (c)
            {
                case ' ':
                case '\t':
                case '\f':
                case '\v':
                case SpecialChars.NoBreakSpace:
                case SpecialChars.NextLine:
                    SkipWhiteSpace();
                    goto again;

                case '\'':
                case SpecialChars.QuoteSingleLeft:
                case SpecialChars.QuoteSingleRight:
                case SpecialChars.QuoteSingleBase:
                case SpecialChars.QuoteReversed:
                    return ScanStringLiteral();

                case '"':
                case SpecialChars.QuoteDoubleLeft:
                case SpecialChars.QuoteDoubleRight:
                case SpecialChars.QuoteLowDoubleLeft:
                    return ScanStringExpandable();

                case '@':
                    // Could be start of hash literal, array operator, multi-line string, splatted variable
                    c1 = GetChar();
                    if (c1 == '{')
                    {
                        return NewToken(TokenKind.AtCurly);
                    }

                    if (c1 == '(')
                    {
                        return NewToken(TokenKind.AtParen);
                    }

                    if (c1.IsSingleQuote())
                    {
                        return ScanHereStringLiteral();
                    }

                    if (c1.IsDoubleQuote())
                    {
                        return ScanHereStringExpandable();
                    }

                    UngetChar();
                    if (c1.IsVariableStart())
                    {
                        return ScanVariable(true, false);
                    }

                    ReportError(_currentIndex - 1,
                        nameof(ParserStrings.UnrecognizedToken),
                        ParserStrings.UnrecognizedToken);
                    return NewToken(TokenKind.Unknown);

                case '#':
                    ScanLineComment();
                    goto again;

                case '\n':
                    return NewToken(TokenKind.NewLine);

                case '\r':
                    NormalizeCRLF(c);
                    goto case '\n';

                case '`':
                    c1 = GetChar();
                    if (c1 == '\r')
                    {
                        NormalizeCRLF(c1);
                    }

                    if (c1 == '\n' || c1 == '\r')
                    {
                        NewToken(TokenKind.LineContinuation);
                        goto again;
                    }

                    if (char.IsWhiteSpace(c1))
                    {
                        SkipWhiteSpace();
                        goto again;
                    }

                    if (c1 == '\0' && AtEof())
                    {
                        ReportIncompleteInput(_currentIndex,
                            nameof(ParserStrings.IncompleteString),
                            ParserStrings.IncompleteString);

                        // Unget the EOF so we can return an EOF token.
                        UngetChar();
                        goto again;
                    }

                    c = Backtick(c1, out char surrogateCharacter);
                    return ScanGenericToken(c, surrogateCharacter);

                case '=':
                    return CheckOperatorInCommandMode(c, TokenKind.Equals);

                case '+':
                    c1 = PeekChar();
                    if (c1 == '+')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.PlusPlus);
                    }

                    if (c1 == '=')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.PlusEquals);
                    }

                    if (AllowSignedNumbers && (char.IsDigit(c1) || c1 == '.'))
                    {
                        return ScanNumber(c);
                    }

                    return CheckOperatorInCommandMode(c, TokenKind.Plus);

                case '-':
                case SpecialChars.EmDash:
                case SpecialChars.EnDash:
                case SpecialChars.HorizontalBar:
                    c1 = PeekChar();
                    if (c1.IsDash())
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.MinusMinus);
                    }

                    if (c1 == '=')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.MinusEquals);
                    }

                    if (char.IsLetter(c1) || c1 == '_' || c1 == '?')
                    {
                        return ScanParameter();
                    }

                    if (AllowSignedNumbers && (char.IsDigit(c1) || c1 == '.'))
                    {
                        return ScanNumber(c);
                    }

                    return CheckOperatorInCommandMode(c, TokenKind.Minus);

                case '*':
                    c1 = PeekChar();
                    if (c1 == '=')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.MultiplyEquals);
                    }

                    if (c1 == '>')
                    {
                        SkipChar();
                        c1 = PeekChar();
                        if (c1 == '>')
                        {
                            SkipChar();
                            return NewFileRedirectionToken(0, append: true, fromSpecifiedExplicitly: false);
                        }

                        if (c1 == '&')
                        {
                            SkipChar();
                            c1 = PeekChar();
                            if (c1 == '1')
                            {
                                SkipChar();
                                return NewMergingRedirectionToken(0, 1);
                            }

                            UngetChar();
                        }

                        return NewFileRedirectionToken(0, append: false, fromSpecifiedExplicitly: false);
                    }

                    return CheckOperatorInCommandMode(c, TokenKind.Multiply);

                case '/':
                    c1 = PeekChar();
                    if (c1 == '=')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.DivideEquals);
                    }

                    return CheckOperatorInCommandMode(c, TokenKind.Divide);

                case '%':
                    c1 = PeekChar();
                    if (c1 == '=')
                    {
                        SkipChar();
                        return CheckOperatorInCommandMode(c, c1, TokenKind.RemainderEquals);
                    }

                    return CheckOperatorInCommandMode(c, TokenKind.Rem);

                case '$':
                    if (PeekChar() == '(')
                    {
                        SkipChar();
                        return NewToken(TokenKind.DollarParen);
                    }

                    return ScanVariable(false, false);

                case '<':
                    if (PeekChar() == '#')
                    {
                        SkipChar();
                        ScanBlockComment();
                        goto again;
                    }

                    return NewInputRedirectionToken();

                case '>':
                    if (PeekChar() == '>')
                    {
                        SkipChar();
                        return NewFileRedirectionToken(1, append: true, fromSpecifiedExplicitly: false);
                    }

                    return NewFileRedirectionToken(1, append: false, fromSpecifiedExplicitly: false);

                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    return ScanIdentifier(c);

                case '(':
                    return NewToken(TokenKind.LParen);
                case ')':
                    return NewToken(TokenKind.RParen);
                case '[':
                    if (InCommandMode() && !PeekChar().ForceStartNewToken())
                    {
                        return ScanGenericToken('[');
                    }

                    return NewToken(TokenKind.LBracket);
                case ']':
                    return NewToken(TokenKind.RBracket);
                case '{':
                    return NewToken(TokenKind.LCurly);
                case '}':
                    return NewToken(TokenKind.RCurly);
                case '.':
                    return ScanDot();
                case ';':
                    return NewToken(TokenKind.Semi);
                case ',':
                    return NewToken(TokenKind.Comma);

                case '0':
                case '7':
                case '8':
                case '9':
                    return ScanNumber(c);

                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                    if (PeekChar() == '>')
                    {
                        SkipChar();
                        c1 = PeekChar();
                        if (c1 == '>')
                        {
                            SkipChar();
                            return NewFileRedirectionToken(c - '0', append: true, fromSpecifiedExplicitly: true);
                        }

                        if (c1 == '&')
                        {
                            SkipChar();
                            c1 = PeekChar();
                            if (c1 == '1' || c1 == '2')
                            {
                                SkipChar();
                                return NewMergingRedirectionToken(c - '0', c1 - '0');
                            }

                            UngetChar();
                        }

                        return NewFileRedirectionToken(c - '0', append: false, fromSpecifiedExplicitly: true);
                    }

                    return ScanNumber(c);

                case '&':
                    if (PeekChar() == '&')
                    {
                        SkipChar();
                        return NewToken(TokenKind.AndAnd);
                    }

                    return NewToken(TokenKind.Ampersand);

                case '|':
                    if (PeekChar() == '|')
                    {
                        SkipChar();
                        return NewToken(TokenKind.OrOr);
                    }

                    return NewToken(TokenKind.Pipe);

                case '!':
                    c1 = PeekChar();
                    if ((InCommandMode() && !c1.ForceStartNewToken()) ||
                        (InExpressionMode() && c1.IsIdentifierStart()))
                    {
                        return ScanGenericToken(c);
                    }

                    if (InExpressionMode() && (char.IsDigit(c1) || c1 == '.'))
                    {
                        // check if the next token is actually a number
                        string strNum = ScanNumberHelper(c, out _, out _, out _, out _);
                        // rescan characters after the check
                        _currentIndex = _tokenStart;
                        c = GetChar();

                        if (strNum == null)
                        {
                            return ScanGenericToken(c);
                        }
                    }

                    return NewToken(TokenKind.Exclaim);

                case ':':
                    if (PeekChar() == ':')
                    {
                        SkipChar();
                        if (InCommandMode() && !WantSimpleName && !PeekChar().ForceStartNewToken())
                        {
                            var sb = GetStringBuilder();
                            sb.Append("::");
                            return ScanGenericToken(sb);
                        }

                        return NewToken(TokenKind.ColonColon);
                    }

                    if (InCommandMode())
                    {
                        return ScanLabel();
                    }

                    return this.NewToken(TokenKind.Colon);

                case '?' when InExpressionMode():
                    c1 = PeekChar();

                    if (c1 == '?')
                    {
                        SkipChar();
                        c1 = PeekChar();

                        if (c1 == '=')
                        {
                            SkipChar();
                            return this.NewToken(TokenKind.QuestionQuestionEquals);
                        }

                        return this.NewToken(TokenKind.QuestionQuestion);
                    }

                    return this.NewToken(TokenKind.QuestionMark);

                case '\0':
                    if (AtEof())
                    {
                        return SaveToken(new Token(NewScriptExtent(_tokenStart + 1, _tokenStart + 1), TokenKind.EndOfInput, TokenFlags.None));
                    }

                    return ScanGenericToken(c);

                default:
                    if (c.IsWhitespace())
                    {
                        SkipWhiteSpace();
                        goto again;
                    }

                    if (char.IsLetter(c))
                    {
                        return ScanIdentifier(c);
                    }

                    return ScanGenericToken(c);
            }
        }
    }
}
