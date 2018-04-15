// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation.Language;

namespace System.Management.Automation
{
    /// <summary>
    /// This attribute is used to specify an argument completer for a parameter to a cmdlet or function.
    /// <example>
    ///     [Parameter()]
    ///     [ArgumentCompleter(typeof(NounArgumentCompleter))]
    ///     public string Noun { get; set; }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ArgumentCompleterAttribute : Attribute
    {
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type { get; private set; }

        /// <summary/>
        public ScriptBlock ScriptBlock { get; private set; }

        /// <param name="type">The type must implement <see cref="IArgumentCompleter"/> and have a default constructor.</param>
        public ArgumentCompleterAttribute(Type type)
        {
            if (type == null || (type.GetInterfaces().All(t => t != typeof(IArgumentCompleter))))
            {
                throw PSTraceSource.NewArgumentException("type");
            }

            Type = type;
        }

        /// <summary>
        /// This constructor is used primarily via PowerShell scripts.
        /// </summary>
        /// <param name="scriptBlock"></param>
        public ArgumentCompleterAttribute(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentNullException("scriptBlock");
            }

            ScriptBlock = scriptBlock;
        }
    }

    /// <summary>
    /// A type specified by the <see cref="ArgumentCompleterAttribute"/> must implement this interface.
    /// </summary>
    public interface IArgumentCompleter
    {
        /// <summary>
        /// Implementations of this function are called by PowerShell to complete arguments.
        /// </summary>
        /// <param name="commandName">The name of the command that needs argument completion.</param>
        /// <param name="parameterName">The name of the parameter that needs argument completion.</param>
        /// <param name="wordToComplete">The (possibly empty) word being completed.</param>
        /// <param name="commandAst">The command ast in case it is needed for completion.</param>
        /// <param name="fakeBoundParameters">
        /// This parameter is similar to $PSBoundParameters, except that sometimes PowerShell cannot or
        /// will not attempt to evaluate an argument, in which case you may need to use <paramref name="commandAst"/>.
        /// </param>
        /// <returns>
        /// A collection of completion results, most like with <see cref="CompletionResult.ResultType"/> set to
        /// <see cref="CompletionResultType.ParameterValue"/>.
        /// </returns>
        IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters);
    }

    /// <summary>
    ///
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "ArgumentCompleter", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=528576")]
    public class RegisterArgumentCompleterCommand : PSCmdlet
    {
        /// <summary>
        ///
        /// </summary>
        [Parameter(ParameterSetName = "NativeSet", Mandatory = true)]
        [Parameter(ParameterSetName = "PowerShellSet")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CommandName { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Parameter(ParameterSetName = "PowerShellSet", Mandatory = true)]
        public string ParameterName { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Parameter(Mandatory = true)]
        [AllowNull()]
        public ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Parameter(ParameterSetName = "NativeSet")]
        public SwitchParameter Native { get; set; }

        /// <summary>
        ///
        /// </summary>
        protected override void EndProcessing()
        {
            Dictionary<string, ScriptBlock> completerDictionary;
            if (ParameterName != null)
            {
                completerDictionary = Context.CustomArgumentCompleters ??
                                      (Context.CustomArgumentCompleters = new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                completerDictionary = Context.NativeArgumentCompleters ??
                                      (Context.NativeArgumentCompleters = new Dictionary<string, ScriptBlock>(StringComparer.OrdinalIgnoreCase));
            }

            if (CommandName == null || CommandName.Length == 0)
            {
                CommandName = new[] { "" };
            }

            for (int i = 0; i < CommandName.Length; i++)
            {
                var key = CommandName[i];
                if (!string.IsNullOrWhiteSpace(ParameterName))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        key = key + ":" + ParameterName;
                    }
                    else
                    {
                        key = ParameterName;
                    }
                }

                completerDictionary[key] = ScriptBlock;
            }
        }
    }

    /// <summary>
    /// This attribute is used to specify an argument completions for a parameter of a cmdlet or function
    /// based on string array.
    /// <example>
    ///     [Parameter()]
    ///     [ArgumentCompletions("Option1","Option2","Option3")]
    ///     public string Noun { get; set; }
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ArgumentCompletionsAttribute : Attribute
    {
        private string[] _completions;

        /// <summary>
        /// Initializes a new instance of the ArgumentCompletionsAttribute class
        /// </summary>
        /// <param name="completions">list of complete values</param>
        /// <exception cref="ArgumentNullException">for null arguments</exception>
        /// <exception cref="ArgumentOutOfRangeException">for invalid arguments</exception>
        public ArgumentCompletionsAttribute(params string[] completions)
        {
            if (completions == null)
            {
                throw PSTraceSource.NewArgumentNullException("completions");
            }

            if (completions.Length == 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("completions", completions);
            }

            _completions = completions;
        }

        /// <summary>
        /// The function returns completions for arguments.
        /// </summary>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var wordToCompletePattern = WildcardPattern.Get(string.IsNullOrWhiteSpace(wordToComplete) ? "*" : wordToComplete + "*", WildcardOptions.IgnoreCase);

            foreach (var str in _completions)
            {
                if (wordToCompletePattern.IsMatch(str))
                {
                    yield return new CompletionResult(str, str, CompletionResultType.ParameterValue, str);
                }
            }
        }
    }


    /// <summary>
    ///     Base class for writing custom Argument Completers
    /// </summary>
    /// <para>Derived classes should override <see cref="AddCompletionsFor(string,string,IDictionary)"/> </para>
    public abstract class ArgumentCompleterBase : IArgumentCompleter
    {
        private List<CompletionResult> _results;

        /// <summary>
        ///     The word to complete
        /// </summary>
        protected string WordToComplete { get; private set; }

        /// <summary>
        ///     The Command Ast for the command to complete
        /// </summary>
        protected CommandAst CommandAst { get; private set; }

        private List<CompletionResult> Results => _results ?? (_results = new List<CompletionResult>());

        IEnumerable<CompletionResult> IArgumentCompleter.CompleteArgument(string commandName, string parameterName,
            string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            WordToComplete = wordToComplete;
            CommandAst = commandAst;
            AddCompletionsFor(commandName, parameterName, fakeBoundParameters);
            return _results;
        }

        /// <summary>
        ///     Override in child class to add completions by calling <see ref="CompleteWith"/>
        /// </summary>
        /// <param name="commandName">the command to complete parameters for</param>
        /// <param name="parameterName">the parameter to complete</param>
        /// <param name="fakeBoundParameters">previously specified command parameters</param>
        protected abstract void AddCompletionsFor(string commandName, string parameterName, IDictionary fakeBoundParameters);

        /// <summary>
        ///     Adds a completion result to the result set
        /// </summary>
        /// <param name="completionResult">the completion result to add</param>
        protected void CompleteWith(CompletionResult completionResult)
            => Results.Add(item: completionResult);

        /// <summary>
        ///     Adds a completion result to the result set with the specified parameters
        /// </summary>
        /// <param name="text">the text to be used as the auto completion result</param>
        /// <param name="listItemText">the text to be displayed in a list</param>
        /// <param name="toolTip">the text for the tooltip with details to be displayed about the object</param>
        /// <param name="resultType">the type of completion result</param>
        protected void CompleteWith(string text, string listItemText = null, string toolTip = null,
            CompletionResultType resultType = CompletionResultType.ParameterValue)
        {
            var quotedText = QuoteIfSpace(text: text);
            var completionResult = new CompletionResult(completionText: quotedText, listItemText: listItemText ?? text,
                resultType: resultType, toolTip: toolTip ?? text);
            Results.Add(item: completionResult);
        }

        /// <summary>
        ///     Adds a completion result to the result set if the text starts with <see cref="WordToComplete" />
        /// </summary>
        /// <para>The comparison is case insensitive</para>
        /// <param name="text">the text to be used as the auto completion result</param>
        /// <param name="listItemText">the text to be displayed in a list</param>
        /// <param name="toolTip">the text for the tooltip with details to be displayed about the object</param>
        /// <param name="resultType">the type of completion result</param>
        protected void CompleteWithIfTextStartsWithWordToComplete(string text, string listItemText = null,
            string toolTip = null, CompletionResultType resultType = CompletionResultType.ParameterValue)
        {
            if (StartWithWordToComplete(text: text))
                CompleteWith(text: text, listItemText: listItemText ?? text, toolTip: toolTip ?? text,
                    resultType: resultType);
        }

        /// <summary>
        ///     Adds a completion result to the result set if the any string argument starts with <see cref="WordToComplete" />
        /// </summary>
        /// <para>The comparison is case insensitive</para>
        /// <param name="text">the text to be used as the auto completion result</param>
        /// <param name="listItemText">the text to be displayed in a list</param>
        /// <param name="toolTip">the text for the tooltip with details to be displayed about the object</param>
        /// <param name="resultType">the type of completion result</param>
        protected void CompleteWithIfAnyStartsWithWordToComplete(string text, string listItemText = null,
            string toolTip = null, CompletionResultType resultType = CompletionResultType.ParameterValue)
        {
            if (StartWithWordToComplete(text: text) || StartWithWordToComplete(listItemText) || StartWithWordToComplete(toolTip))
                CompleteWith(text: text, listItemText: listItemText ?? text, toolTip: toolTip ?? text,
                    resultType: resultType);
        }


        /// <summary>
        ///     Adds a completion result to the result set if the any string argument starts with <see cref="WordToComplete" />
        /// </summary>
        /// <para>The comparison is case insensitive</para>
        /// <param name="text">the text to be used as the auto completion result</param>
        /// <param name="listItemText">the text to be displayed in a list</param>
        /// <param name="toolTip">the text for the tooltip with details to be displayed about the object</param>
        /// <param name="resultType">the type of completion result</param>
        protected void CompleteWithIfAnyContainsWordToComplete(string text, string listItemText = null,
            string toolTip = null, CompletionResultType resultType = CompletionResultType.ParameterValue)
        {
            if (ContainsWordToComplete(text) || ContainsWordToComplete(listItemText) || ContainsWordToComplete(toolTip))
                CompleteWith(text: text, listItemText: listItemText ?? text, toolTip: toolTip ?? text,
                    resultType: resultType);
        }

        /// <summary>
        ///     Adds a completion result to the result set if the text contains <see cref="WordToComplete" />
        /// </summary>
        /// <para>The comparison is case insensitive</para>
        /// <param name="text">the text to be used as the auto completion result</param>
        /// <param name="listItemText">the text to be displayed in a list</param>
        /// <param name="toolTip">the text for the tooltip with details to be displayed about the object</param>
        /// <param name="resultType">the type of completion result</param>
        protected void CompleteWithIfTextContainsWordToComplete(string text, string listItemText = null,
            string toolTip = null, CompletionResultType resultType = CompletionResultType.ParameterValue)
        {
            if (ContainsWordToComplete(text))
                CompleteWith(text: text, listItemText: listItemText ?? text, toolTip: toolTip ?? text,
                    resultType: resultType);
        }

        private static string QuoteIfSpace(string text)
            => text.Contains(" ") ? $@"""{text}""" : text;

        /// <summary>
        ///     Predicate to test if a string starts with <see cref="WordToComplete" />
        /// </summary>
        /// <param name="text"></param>
        /// <returns>true if the text contains <see cref="WordToComplete" />, otherwise false</returns>
        protected bool StartWithWordToComplete(string text)
            => text.StartsWith(value: WordToComplete, comparisonType: StringComparison.CurrentCultureIgnoreCase);

        /// <summary>
        ///     Predicate to test if a string starts with <see cref="WordToComplete" />
        /// </summary>
        /// <param name="text">the text to test</param>
        /// <param name="stringComparison">The StringComparison to use when comparing</param>
        /// <returns>true if the text contains <see cref="WordToComplete" />, otherwise false</returns>
        protected bool StartWithWordToComplete(string text, StringComparison stringComparison)
            => text.StartsWith(value: WordToComplete, comparisonType: stringComparison);

        /// <summary>
        ///     Predicate to test if a string starts with <see cref="WordToComplete" />
        /// </summary>
        /// <param name="text">the text to test</param>
        /// <returns>true if the text contains <see cref="WordToComplete" />, otherwise false</returns>
        protected bool ContainsWordToComplete(string text)
            => text.IndexOf(value: WordToComplete, comparisonType: StringComparison.CurrentCultureIgnoreCase) != -1;

        /// <summary>
        ///     Predicate to test if a string contains <see cref="WordToComplete" />
        /// </summary>
        /// <param name="text">the text to test</param>
        /// <param name="stringComparison">The StringComparison to use when comparing</param>
        /// <returns>true if the text contains <see cref="WordToComplete" />, otherwise false</returns>
        protected bool ContainsWordToComplete(string text, StringComparison stringComparison)
            => text.IndexOf(value: WordToComplete, comparisonType: stringComparison) != -1;
    }
}
