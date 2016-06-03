/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Linq;
#if CORECLR
using System.Runtime.InteropServices;
#endif

namespace Microsoft.PowerShell
{

#pragma warning disable 1591

    public enum TokenClassification
    {
        None,
        Comment,
        Keyword,
        String,
        Operator,
        Variable,
        Command,
        Parameter,
        Type,
        Number,
        Member,
    }

    public enum EditMode
    {
        Windows,
        Emacs,
        Vi,
    }

    public enum BellStyle
    {
        None,
        Visual,
        Audible
    }

    #region vi
    public enum ViModeStyle
    {
        None,
        Prompt,
        Cursor
    }

    public enum ViMode
    {
        Insert,
        Command
    }
    #endregion vi

    public enum HistorySaveStyle
    {
        SaveIncrementally,
        SaveAtExit,
        SaveNothing
    }

    public class PSConsoleReadlineOptions
    {
#if CORECLR
        public const ConsoleColor DefaultCommentForegroundColor   = ConsoleColor.Magenta;
        public const ConsoleColor DefaultKeywordForegroundColor   = ConsoleColor.Green;
        public const ConsoleColor DefaultStringForegroundColor    = ConsoleColor.DarkCyan;
        public const ConsoleColor DefaultOperatorForegroundColor  = ConsoleColor.Gray;
        public const ConsoleColor DefaultVariableForegroundColor  = ConsoleColor.Green;
        public const ConsoleColor DefaultCommandForegroundColor   = ConsoleColor.Yellow;
        public const ConsoleColor DefaultParameterForegroundColor = ConsoleColor.Green;
        public const ConsoleColor DefaultTypeForegroundColor      = ConsoleColor.Gray;
        public const ConsoleColor DefaultNumberForegroundColor    = ConsoleColor.White;
        public const ConsoleColor DefaultMemberForegroundColor    = ConsoleColor.Gray;
        public const ConsoleColor DefaultEmphasisForegroundColor  = ConsoleColor.Cyan;
        public const ConsoleColor DefaultErrorForegroundColor     = ConsoleColor.Red;
#else
        public const ConsoleColor DefaultCommentForegroundColor   = ConsoleColor.DarkGreen;
        public const ConsoleColor DefaultKeywordForegroundColor   = ConsoleColor.Green;
        public const ConsoleColor DefaultStringForegroundColor    = ConsoleColor.DarkCyan;
        public const ConsoleColor DefaultOperatorForegroundColor  = ConsoleColor.DarkGray;
        public const ConsoleColor DefaultVariableForegroundColor  = ConsoleColor.Green;
        public const ConsoleColor DefaultCommandForegroundColor   = ConsoleColor.Yellow;
        public const ConsoleColor DefaultParameterForegroundColor = ConsoleColor.DarkGray;
        public const ConsoleColor DefaultTypeForegroundColor      = ConsoleColor.Gray;
        public const ConsoleColor DefaultNumberForegroundColor    = ConsoleColor.White;
        public const ConsoleColor DefaultMemberForegroundColor    = ConsoleColor.Gray;
        public const ConsoleColor DefaultEmphasisForegroundColor  = ConsoleColor.Cyan;
        public const ConsoleColor DefaultErrorForegroundColor     = ConsoleColor.Red;
#endif

        public const EditMode DefaultEditMode = EditMode.Windows;

        public const string DefaultContinuationPrompt = ">> ";

        /// <summary>
        /// The maximum number of commands to store in the history.
        /// </summary>
        public const int DefaultMaximumHistoryCount = 4096;

        /// <summary>
        /// The maximum number of items to store in the kill ring.
        /// </summary>
        public const int DefaultMaximumKillRingCount = 10;

        /// <summary>
        /// In Emacs, when searching history, the cursor doesn't move.
        /// In 4NT, the cursor moves to the end.  This option allows
        /// for either behavior.
        /// </summary>
        public const bool DefaultHistorySearchCursorMovesToEnd = false;

        /// <summary>
        /// When displaying possible completions, either display
        /// tooltips or dipslay just the completions.
        /// </summary>
        public const bool DefaultShowToolTips = false;

        /// <summary>
        /// When ringing the bell, what frequency do we use?
        /// </summary>
        public const int DefaultDingTone = 1221;

        public const int DefaultDingDuration = 50;

        public const int DefaultCompletionQueryItems = 100;

        // Default includes all characters PowerShell treats like a dash - em dash, en dash, horizontal bar
        public const string DefaultWordDelimiters = @";:,.[]{}()/\|^&*-=+'""" + "\u2013\u2014\u2015";

        /// <summary>
        /// When ringing the bell, what should be done?
        /// </summary>
        public const BellStyle DefaultBellStyle = BellStyle.Audible;

        public const bool DefaultHistorySearchCaseSensitive = false;

        public const HistorySaveStyle DefaultHistorySaveStyle = HistorySaveStyle.SaveIncrementally;

        public PSConsoleReadlineOptions(string hostName)
        {
            ResetColors();
            EditMode = DefaultEditMode;
            ContinuationPrompt = DefaultContinuationPrompt;
            ContinuationPromptBackgroundColor = Console.BackgroundColor;
            ContinuationPromptForegroundColor = Console.ForegroundColor;
            ExtraPromptLineCount = DefaultExtraPromptLineCount;
            AddToHistoryHandler = null;
            HistoryNoDuplicates = DefaultHistoryNoDuplicates;
            MaximumHistoryCount = DefaultMaximumHistoryCount;
            MaximumKillRingCount = DefaultMaximumKillRingCount;
            HistorySearchCursorMovesToEnd = DefaultHistorySearchCursorMovesToEnd;
            ShowToolTips = DefaultShowToolTips;
            DingDuration = DefaultDingDuration;
            DingTone = DefaultDingTone;
            BellStyle = DefaultBellStyle;
            CompletionQueryItems = DefaultCompletionQueryItems;
            WordDelimiters = DefaultWordDelimiters;
            HistorySearchCaseSensitive = DefaultHistorySearchCaseSensitive;
            HistorySaveStyle = DefaultHistorySaveStyle;
#if CORECLR
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))   // MS Windows
            {
                HistorySavePath = System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%AppData%"),
                                                         @"\Microsoft\Windows\PowerShell\PSReadline\",
                                                         hostName + "_history.txt");
            }

            else
            {
                //PSReadline does not have access to Utils.CorePSPlatform. Must set PSReadline path seperately                
                string historypath = System.Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                    
                if (!String.IsNullOrEmpty(historypath))
                {
                    historypath = System.IO.Path.Combine(historypath, "powershell");
                    HistorySavePath = historypath; 
                }

                else
                {
                    historypath = System.IO.Path.Combine(
                                                        Environment.GetEnvironmentVariable("HOME"), 
                                                        ".local/share/powershell/modules");
                        
                    HistorySavePath = System.IO.Path.Combine(
                                                             Environment.GetEnvironmentVariable("HOME"), 
                                                             historypath,
                                                             "PSReadLine",
                                                             hostName + "_history.txt");
                }
            }
#else
                HistorySavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                + @"\Microsoft\Windows\PowerShell\PSReadline\" + hostName + "_history.txt";
#endif
            CommandValidationHandler = null;
            CommandsToValidateScriptBlockArguments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ForEach-Object", "%",
                "Invoke-Command", "icm",
                "Measure-Command",
                "New-Module", "nmo",
                "Register-EngineEvent",
                "Register-ObjectEvent",
                "Register-WMIEvent",
                "Set-PSBreakpoint", "sbp",
                "Start-Job", "sajb",
                "Trace-Command", "trcm",
                "Use-Transaction",
                "Where-Object", "?", "where",
            };
        }
        
        public EditMode EditMode { get; set; }

        public string ContinuationPrompt { get; set; }
        public ConsoleColor ContinuationPromptForegroundColor { get; set; }
        public ConsoleColor ContinuationPromptBackgroundColor { get; set; }

        /// <summary>
        /// Prompts are typically 1 line, but sometimes they may span lines.  This
        /// count is used to make sure we can display the full prompt after showing
        /// ambiguous completions
        /// </summary>
        public int ExtraPromptLineCount { get; set; }
        public const int DefaultExtraPromptLineCount = 0;

        /// <summary>
        /// This handler is called before adding a command line to history.
        /// The return value indicates if the command line should be added
        /// to history or not.
        /// </summary>
        public Func<string, bool> AddToHistoryHandler { get; set; }

        /// <summary>
        /// This handler is called from ValidateAndAcceptLine.
        /// If an exception is thrown, validation fails and the error is reported.
        /// </summary>
        public Action<CommandAst> CommandValidationHandler { get; set; }

        /// <summary>
        /// Most commands do not accept script blocks, but for those that do,
        /// we want to validate commands in the script block arguments.
        /// Unfortunately, we can't know how the argument is used.  In the worst
        /// case, for commands like Get-ADUser, the script block actually
        /// specifies a different language.
        ///
        /// Because we can't know ahead of time all of the commands that do
        /// odd things with script blocks, we create a white-list of commands
        /// that do invoke the script block - this covers the most useful cases.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public HashSet<string> CommandsToValidateScriptBlockArguments { get; set; }

        /// <summary>
        /// When true, duplicates will not be added to the history.
        /// </summary>
        public bool HistoryNoDuplicates { get; set; }
        public const bool DefaultHistoryNoDuplicates = false;

        public int MaximumHistoryCount { get; set; }
        public int MaximumKillRingCount { get; set; }
        public bool HistorySearchCursorMovesToEnd { get; set; }
        public bool ShowToolTips { get; set; }
        public int DingTone { get; set; }
        public int CompletionQueryItems { get; set; }
        public string WordDelimiters { get; set; }

        /// <summary>
        /// When ringing the bell, how long (in ms)?
        /// </summary>
        public int DingDuration { get; set; }
        public BellStyle BellStyle { get; set; }

        public bool HistorySearchCaseSensitive { get; set; }
        internal StringComparison HistoryStringComparison
        {
            get { return HistorySearchCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        #region vi
        public ViModeStyle ViModeIndicator { get; set; }
        #endregion vi

        /// <summary>
        /// The path to the saved history.
        /// </summary>
        public string HistorySavePath { get; set; }
        public HistorySaveStyle HistorySaveStyle { get; set; }

        public ConsoleColor DefaultTokenForegroundColor { get; set; }
        public ConsoleColor CommentForegroundColor { get; set; }
        public ConsoleColor KeywordForegroundColor { get; set; }
        public ConsoleColor StringForegroundColor { get; set; }
        public ConsoleColor OperatorForegroundColor { get; set; }
        public ConsoleColor VariableForegroundColor { get; set; }
        public ConsoleColor CommandForegroundColor { get; set; }
        public ConsoleColor ParameterForegroundColor { get; set; }
        public ConsoleColor TypeForegroundColor { get; set; }
        public ConsoleColor NumberForegroundColor { get; set; }
        public ConsoleColor MemberForegroundColor { get; set; }
        public ConsoleColor DefaultTokenBackgroundColor { get; set; }
        public ConsoleColor CommentBackgroundColor { get; set; }
        public ConsoleColor KeywordBackgroundColor { get; set; }
        public ConsoleColor StringBackgroundColor { get; set; }
        public ConsoleColor OperatorBackgroundColor { get; set; }
        public ConsoleColor VariableBackgroundColor { get; set; }
        public ConsoleColor CommandBackgroundColor { get; set; }
        public ConsoleColor ParameterBackgroundColor { get; set; }
        public ConsoleColor TypeBackgroundColor { get; set; }
        public ConsoleColor NumberBackgroundColor { get; set; }
        public ConsoleColor MemberBackgroundColor { get; set; }
        public ConsoleColor EmphasisForegroundColor { get; set; }
        public ConsoleColor EmphasisBackgroundColor { get; set; }
        public ConsoleColor ErrorForegroundColor { get; set; }
        public ConsoleColor ErrorBackgroundColor { get; set; }

        internal void ResetColors()
        {
            DefaultTokenForegroundColor = Console.ForegroundColor;
            CommentForegroundColor      = DefaultCommentForegroundColor;
            KeywordForegroundColor      = DefaultKeywordForegroundColor;
            StringForegroundColor       = DefaultStringForegroundColor;
            OperatorForegroundColor     = DefaultOperatorForegroundColor;
            VariableForegroundColor     = DefaultVariableForegroundColor;
            CommandForegroundColor      = DefaultCommandForegroundColor;
            ParameterForegroundColor    = DefaultParameterForegroundColor;
            TypeForegroundColor         = DefaultTypeForegroundColor;
            NumberForegroundColor       = DefaultNumberForegroundColor;
            MemberForegroundColor       = DefaultNumberForegroundColor;
            EmphasisForegroundColor     = DefaultEmphasisForegroundColor;
            ErrorForegroundColor        = DefaultErrorForegroundColor;
            DefaultTokenBackgroundColor = Console.BackgroundColor;
            CommentBackgroundColor      = Console.BackgroundColor;
            KeywordBackgroundColor      = Console.BackgroundColor;
            StringBackgroundColor       = Console.BackgroundColor;
            OperatorBackgroundColor     = Console.BackgroundColor;
            VariableBackgroundColor     = Console.BackgroundColor;
            CommandBackgroundColor      = Console.BackgroundColor;
            ParameterBackgroundColor    = Console.BackgroundColor;
            TypeBackgroundColor         = Console.BackgroundColor;
            NumberBackgroundColor       = Console.BackgroundColor;
            MemberBackgroundColor       = Console.BackgroundColor;
            EmphasisBackgroundColor     = Console.BackgroundColor;
            ErrorBackgroundColor        = Console.BackgroundColor;
        }

        internal void SetForegroundColor(TokenClassification tokenKind, ConsoleColor color)
        {
            switch (tokenKind)
            {
            case TokenClassification.None:      DefaultTokenForegroundColor = color; break;
            case TokenClassification.Comment:   CommentForegroundColor = color; break;
            case TokenClassification.Keyword:   KeywordForegroundColor = color; break;
            case TokenClassification.String:    StringForegroundColor = color; break;
            case TokenClassification.Operator:  OperatorForegroundColor = color; break;
            case TokenClassification.Variable:  VariableForegroundColor = color; break;
            case TokenClassification.Command:   CommandForegroundColor = color; break;
            case TokenClassification.Parameter: ParameterForegroundColor = color; break;
            case TokenClassification.Type:      TypeForegroundColor = color; break;
            case TokenClassification.Number:    NumberForegroundColor = color; break;
            case TokenClassification.Member:    MemberForegroundColor = color; break;
            }
        }

        internal void SetBackgroundColor(TokenClassification tokenKind, ConsoleColor color)
        {
            switch (tokenKind)
            {
            case TokenClassification.None:      DefaultTokenBackgroundColor = color; break;
            case TokenClassification.Comment:   CommentBackgroundColor = color; break;
            case TokenClassification.Keyword:   KeywordBackgroundColor = color; break;
            case TokenClassification.String:    StringBackgroundColor = color; break;
            case TokenClassification.Operator:  OperatorBackgroundColor = color; break;
            case TokenClassification.Variable:  VariableBackgroundColor = color; break;
            case TokenClassification.Command:   CommandBackgroundColor = color; break;
            case TokenClassification.Parameter: ParameterBackgroundColor = color; break;
            case TokenClassification.Type:      TypeBackgroundColor = color; break;
            case TokenClassification.Number:    NumberBackgroundColor = color; break;
            case TokenClassification.Member:    MemberBackgroundColor = color; break;
            }
        }
    }

    [Cmdlet("Get", "PSReadlineOption", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=528808")]
    [OutputType(typeof(PSConsoleReadlineOptions))]
    public class GetPSReadlineOption : PSCmdlet
    {
#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected override void EndProcessing()
        {
            WriteObject(PSConsoleReadLine.GetOptions());
        }
    }

    [Cmdlet("Set", "PSReadlineOption", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=528811")]
    public class SetPSReadlineOption : PSCmdlet
    {
        [Parameter(ParameterSetName = "OptionsSet")]
        public EditMode EditMode
        {
            get { return _editMode.GetValueOrDefault(); }
            set { _editMode = value; }
        }
        internal EditMode? _editMode;

        [Parameter(ParameterSetName = "OptionsSet")]
        [AllowEmptyString]
        public string ContinuationPrompt { get; set; }

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor ContinuationPromptForegroundColor
        {
            get { return _continuationPromptForegroundColor.GetValueOrDefault(); }
            set { _continuationPromptForegroundColor = value; }
        }
        internal ConsoleColor? _continuationPromptForegroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor ContinuationPromptBackgroundColor
        {
            get { return _continuationPromptBackgroundColor.GetValueOrDefault(); }
            set { _continuationPromptBackgroundColor = value; }
        }
        internal ConsoleColor? _continuationPromptBackgroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor EmphasisForegroundColor
        {
            get { return _emphasisForegroundColor.GetValueOrDefault(); }
            set { _emphasisForegroundColor = value; }
        }
        internal ConsoleColor? _emphasisForegroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor EmphasisBackgroundColor
        {
            get { return _emphasisBackgroundColor.GetValueOrDefault(); }
            set { _emphasisBackgroundColor = value; }
        }
        internal ConsoleColor? _emphasisBackgroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor ErrorForegroundColor
        {
            get { return _errorForegroundColor.GetValueOrDefault(); }
            set { _errorForegroundColor = value; }
        }
        internal ConsoleColor? _errorForegroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public ConsoleColor ErrorBackgroundColor
        {
            get { return _errorBackgroundColor.GetValueOrDefault(); }
            set { _errorBackgroundColor = value; }
        }
        internal ConsoleColor? _errorBackgroundColor;

        [Parameter(ParameterSetName = "OptionsSet")]
        public SwitchParameter HistoryNoDuplicates
        {
            get { return _historyNoDuplicates.GetValueOrDefault(); }
            set { _historyNoDuplicates = value; }
        }
        internal SwitchParameter? _historyNoDuplicates;

        [Parameter(ParameterSetName = "OptionsSet")]
        [AllowNull]
        public Func<string, bool> AddToHistoryHandler
        {
            get { return _addToHistoryHandler; }
            set
            {
                _addToHistoryHandler = value;
                _addToHistoryHandlerSpecified = true;
            }
        }
        private Func<string, bool> _addToHistoryHandler;
        internal bool _addToHistoryHandlerSpecified;

        [Parameter(ParameterSetName = "OptionsSet")]
        [AllowNull]
        public Action<CommandAst> CommandValidationHandler
        {
            get { return _commandValidationHandler; }
            set
            {
                _commandValidationHandler = value;
                _commandValidationHandlerSpecified = true;
            }
        }
        private Action<CommandAst> _commandValidationHandler;
        internal bool _commandValidationHandlerSpecified;

        [Parameter(ParameterSetName = "OptionsSet")]
        public SwitchParameter HistorySearchCursorMovesToEnd
        {
            get { return _historySearchCursorMovesToEnd.GetValueOrDefault(); }
            set { _historySearchCursorMovesToEnd = value; }
        }
        internal SwitchParameter? _historySearchCursorMovesToEnd;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int MaximumHistoryCount
        {
            get { return _maximumHistoryCount.GetValueOrDefault(); }
            set { _maximumHistoryCount = value; }
        }
        internal int? _maximumHistoryCount;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int MaximumKillRingCount
        {
            get { return _maximumKillRingCount.GetValueOrDefault(); }
            set { _maximumKillRingCount = value; }
        }
        internal int? _maximumKillRingCount;

        [Parameter(ParameterSetName = "OptionsSet")]
        public SwitchParameter ResetTokenColors
        {
            get { return _resetTokenColors.GetValueOrDefault(); }
            set { _resetTokenColors = value; }
        }
        internal SwitchParameter? _resetTokenColors;

        [Parameter(ParameterSetName = "OptionsSet")]
        public SwitchParameter ShowToolTips
        {
            get { return _showToolTips.GetValueOrDefault(); }
            set { _showToolTips = value; }
        }
        internal SwitchParameter? _showToolTips;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int ExtraPromptLineCount
        {
            get { return _extraPromptLineCount.GetValueOrDefault(); }
            set { _extraPromptLineCount = value; }
        }
        internal int? _extraPromptLineCount;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int DingTone
        {
            get { return _dingTone.GetValueOrDefault(); }
            set { _dingTone = value; }
        }
        internal int? _dingTone;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int DingDuration
        {
            get { return _dingDuration.GetValueOrDefault(); }
            set { _dingDuration = value; }
        }
        internal int? _dingDuration;

        [Parameter(ParameterSetName = "OptionsSet")]
        public BellStyle BellStyle
        {
            get { return _bellStyle.GetValueOrDefault(); }
            set { _bellStyle = value; }
        }
        internal BellStyle? _bellStyle;

        [Parameter(ParameterSetName = "OptionsSet")]
        public int CompletionQueryItems
        {
            get { return _completionQueryItems.GetValueOrDefault(); }
            set { _completionQueryItems = value; }
        }
        internal int? _completionQueryItems;

        [Parameter(ParameterSetName = "OptionsSet")]
        public string WordDelimiters { get; set; }

        [Parameter(ParameterSetName = "OptionsSet")]
        public SwitchParameter HistorySearchCaseSensitive
        {
            get { return _historySearchCaseSensitive.GetValueOrDefault(); }
            set { _historySearchCaseSensitive = value; }
        }
        internal SwitchParameter? _historySearchCaseSensitive;

        [Parameter(ParameterSetName = "OptionsSet")]
        public HistorySaveStyle HistorySaveStyle
        {
            get { return _historySaveStyle.GetValueOrDefault(); }
            set { _historySaveStyle = value; }
        }
        internal HistorySaveStyle? _historySaveStyle;

        [Parameter(ParameterSetName = "OptionsSet")]
        [ValidateNotNullOrEmpty]
        public string HistorySavePath { get; set; }

        #region vi
        [Parameter(ParameterSetName = "OptionsSet")]
        public ViModeStyle ViModeIndicator
        {
            get { return _viModeIndicator.GetValueOrDefault(); }
            set { _viModeIndicator = value; }
        }
        internal ViModeStyle? _viModeIndicator;
        #endregion vi

        [Parameter(ParameterSetName = "ColorSet", Position = 0, Mandatory = true)]
        public TokenClassification TokenKind
        {
            get { return _tokenKind.GetValueOrDefault(); }
            set { _tokenKind = value; }
        }
        internal TokenClassification? _tokenKind;

        [Parameter(ParameterSetName = "ColorSet", Position = 1)]
        public ConsoleColor ForegroundColor
        {
            get { return _foregroundColor.GetValueOrDefault(); }
            set { _foregroundColor = value; }
        }
        internal ConsoleColor? _foregroundColor;

        [Parameter(ParameterSetName = "ColorSet", Position = 2)]
        public ConsoleColor BackgroundColor
        {
            get { return _backgroundColor.GetValueOrDefault(); }
            set { _backgroundColor = value; }
        }
        internal ConsoleColor? _backgroundColor;

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected override void EndProcessing()
        {
            PSConsoleReadLine.SetOptions(this);
        }
    }

    public class ChangePSReadlineKeyHandlerCommandBase : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true)]
        [Alias("Key")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Chord { get; set; }

        [Parameter]
        public ViMode ViMode { get; set; }

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected IDisposable UseRequestedDispatchTables()
        {
            bool inViMode = PSConsoleReadLine.GetOptions().EditMode == EditMode.Vi;
            bool viModeParamPresent = MyInvocation.BoundParameters.ContainsKey("ViMode");

            if (inViMode || viModeParamPresent)
            {
                if (!inViMode)
                {
                    // "-ViMode" must have been specified explicitly. Well, okay... we can
                    // modify the Vi tables... but isn't that an odd thing to do from
                    // not-vi mode?
                    WriteWarning(PSReadLineResources.NotInViMode);
                }

                if (ViMode == ViMode.Command)
                    return PSConsoleReadLine.UseViCommandModeTables();
                else // default if -ViMode not specified, invalid, or "Insert"
                    return PSConsoleReadLine.UseViInsertModeTables();
            }

            return null;
        }
    }

    [Cmdlet("Set", "PSReadlineKeyHandler", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=528810")]
    public class SetPSReadlineKeyHandlerCommand : ChangePSReadlineKeyHandlerCommandBase, IDynamicParameters
    {
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "ScriptBlock")]
        [ValidateNotNull]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter(ParameterSetName = "ScriptBlock")]
        public string BriefDescription { get; set; }

        [Parameter(ParameterSetName = "ScriptBlock")]
        [Alias("LongDescription")]  // Alias to stay comptible with previous releases
        public string Description { get; set; }

        private const string FunctionParameter = "Function";
        private const string FunctionParameterSet = "Function";

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected override void EndProcessing()
        {
            using (UseRequestedDispatchTables())
            {
                if (ParameterSetName.Equals(FunctionParameterSet))
                {
                    var function = (string)_dynamicParameters.Value[FunctionParameter].Value;
#if CORECLR
                    MethodInfo mi = typeof (PSConsoleReadLine).GetMethod(function);
                    var keyHandler = (Action<ConsoleKeyInfo?, object>)
                        mi.CreateDelegate(typeof (Action<ConsoleKeyInfo?, object>));
#else
                    var keyHandler = (Action<ConsoleKeyInfo?, object>)
                        Delegate.CreateDelegate(typeof (Action<ConsoleKeyInfo?, object>),
                            typeof (PSConsoleReadLine).GetMethod(function));
#endif
                    BriefDescription = function;
                    PSConsoleReadLine.SetKeyHandler(Chord, keyHandler, BriefDescription, Description);
                }
                else
                {
                    PSConsoleReadLine.SetKeyHandler(Chord, ScriptBlock, BriefDescription, Description);
                }
            }
        }

        private readonly Lazy<RuntimeDefinedParameterDictionary> _dynamicParameters =
            new Lazy<RuntimeDefinedParameterDictionary>(CreateDynamicParametersResult);

        private static RuntimeDefinedParameterDictionary CreateDynamicParametersResult()
        {
            var bindableFunctions = (typeof(PSConsoleReadLine).GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method =>
                    {
                        var parameters = method.GetParameters();
                        return parameters.Length == 2
                               && parameters[0].ParameterType == typeof(ConsoleKeyInfo?)
                               && parameters[1].ParameterType == typeof(object);
                    })
                .Select(method => method.Name)
                .OrderBy(name => name);

            var attributes = new Collection<Attribute>
            {
                new ParameterAttribute
                {
                    Position = 1,
                    Mandatory = true,
                    ParameterSetName = FunctionParameterSet
                },
                new ValidateSetAttribute(bindableFunctions.ToArray())
            };
            var parameter = new RuntimeDefinedParameter(FunctionParameter, typeof(string), attributes);
            var result = new RuntimeDefinedParameterDictionary {{FunctionParameter, parameter}};
            return result;
        }

        public object GetDynamicParameters()
        {
            return _dynamicParameters.Value;
        }
    }

    [Cmdlet("Get", "PSReadlineKeyHandler", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=528807")]
    [OutputType(typeof(KeyHandler))]
    public class GetKeyHandlerCommand : PSCmdlet
    {
        [Parameter]
        public SwitchParameter Bound
        {
            get { return _bound.GetValueOrDefault(); }
            set { _bound = value; }
        }
        private SwitchParameter? _bound;

        [Parameter]
        public SwitchParameter Unbound
        {
            get { return _unbound.GetValueOrDefault(); }
            set { _unbound = value; }
        }
        private SwitchParameter? _unbound;

#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected override void EndProcessing()
        {
            bool bound = true;
            bool unbound = true;
            if (_bound.HasValue && _unbound.HasValue)
            {
                bound = _bound.Value.IsPresent;
                unbound = _unbound.Value.IsPresent;
            }
            else if (_bound.HasValue)
            {
                bound = _bound.Value.IsPresent;
                unbound = false;
            }
            else if (_unbound.HasValue)
            {
                bound = false;
                unbound = _unbound.Value.IsPresent;
            }
            WriteObject(PSConsoleReadLine.GetKeyHandlers(bound, unbound), true);
        }
    }

    [Cmdlet("Remove", "PSReadlineKeyHandler", HelpUri = "http://go.microsoft.com/fwlink/?LinkId=528809")]
    public class RemoveKeyHandlerCommand : ChangePSReadlineKeyHandlerCommandBase
    {
#if !CORECLR
        [ExcludeFromCodeCoverage]
#endif
        protected override void EndProcessing()
        {
            using (UseRequestedDispatchTables())
            {
                PSConsoleReadLine.RemoveKeyHandler(Chord);
            }
        }
    }

#pragma warning restore 1591

}
