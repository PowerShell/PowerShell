// Copyright (c) Microsoft Corporation.
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
    /// <code>
    ///     [Parameter()]
    ///     [ArgumentCompleter(typeof(NounArgumentCompleter))]
    ///     public string Noun { get; set; }
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ArgumentCompleterAttribute : Attribute
    {
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public Type Type { get; }

        /// <summary/>
        public ScriptBlock ScriptBlock { get; }

        /// <param name="type">The type must implement <see cref="IArgumentCompleter"/> and have a default constructor.</param>
        public ArgumentCompleterAttribute(Type type)
        {
            if (type == null || (type.GetInterfaces().All(static t => t != typeof(IArgumentCompleter))))
            {
                throw PSTraceSource.NewArgumentException(nameof(type));
            }

            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentCompleterAttribute"/> class.
        /// This constructor is used by derived attributes implementing <see cref="IArgumentCompleterFactory"/>.
        /// </summary>
        protected ArgumentCompleterAttribute()
        {
            if (this is not IArgumentCompleterFactory)
            {
                throw PSTraceSource.NewInvalidOperationException();
            }
        }

        /// <summary>
        /// This constructor is used primarily via PowerShell scripts.
        /// </summary>
        /// <param name="scriptBlock"></param>
        public ArgumentCompleterAttribute(ScriptBlock scriptBlock)
        {
            if (scriptBlock is null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(scriptBlock));
            }

            ScriptBlock = scriptBlock;
        }

        internal IArgumentCompleter CreateArgumentCompleter()
        {
            return Type != null
                ? Activator.CreateInstance(Type) as IArgumentCompleter
                : this is IArgumentCompleterFactory factory
                    ? factory.Create()
                    : null;
        }
    }

    /// <summary>
    /// A type specified by the <see cref="ArgumentCompleterAttribute"/> must implement this interface.
    /// </summary>
#nullable enable
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
            IDictionary fakeBoundParameters)
        {
            ArgumentCompleterOptions options = new(
                commandName,
                parameterName,
                wordToComplete,
                commandAst,
                fakeBoundParameters);

            return CompleteArgumentWithOptions(ConfigureArgumentCompleterOptions(options));
        }

        /// <summary>
        /// Configures argument completer options.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        /// <returns>Configured options.</returns>
        ArgumentCompleterOptions ConfigureArgumentCompleterOptions(ArgumentCompleterOptions options) => options;

        /// <summary>
        /// Complete argument with options.
        /// </summary>
        /// <param name="options">The options to complete arguments with.</param>
        /// <returns>A collection of completion results.</returns>
        IEnumerable<CompletionResult> CompleteArgumentWithOptions(ArgumentCompleterOptions options)
        {
            if (!options.ShouldComplete)
            {
                yield break;
            }

            string wordToComplete = options.WordToComplete;
            string quote = CompletionCompleters.HandleDoubleAndSingleQuote(ref wordToComplete);

            foreach (string value in options.PossibleCompletionValues)
            {
                if (value.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                {
                    string completionText = QuoteCompletionText(value, quote, options.EscapeGlobbingPath);
                    string toolTip = options.ToolTipMapping?.Invoke(value) ?? value;
                    string listItemText = options.ListItemTextMapping?.Invoke(value) ?? value;

                    yield return new CompletionResult(completionText, listItemText, options.CompletionResultType, toolTip);
                }
            }
        }

        private static string QuoteCompletionText(string completionText, string quote, bool escapeGlobbingPath)
        {
            if (CompletionCompleters.CompletionRequiresQuotes(completionText, escapeGlobbingPath))
            {
                string quoteInUse = string.IsNullOrEmpty(quote) ? "'" : quote;

                completionText = quoteInUse == "'"
                    ? completionText.Replace("'", "''")
                    : completionText.Replace("`", "``").Replace("$", "`$");

                if (escapeGlobbingPath)
                {
                    completionText = quoteInUse == "'"
                        ? completionText.Replace("[", "`[").Replace("]", "`]")
                        : completionText.Replace("[", "``[").Replace("]", "``]");
                }

                return quoteInUse + completionText + quoteInUse;
            }

            return quote + completionText + quote;
        }

    }
#nullable restore

    /// <summary>
    /// Represents options for argument completers.
    /// </summary>
    public sealed class ArgumentCompleterOptions
    {
        /// <summary>
        /// Gets the name of the command that needs argument completion.
        /// </summary>
        /// <value>The name of the command.</value>
        public string CommandName { get; private set; }

        /// <summary>
        /// Gets the name of the parameter that needs argument completion.
        /// </summary>
        /// <value>The name of the parameter.</value>
        public string ParameterName { get; private set; }

        /// <summary>
        /// Gets the word being completed.
        /// </summary>
        /// <value>The word being completed.</value>
        public string WordToComplete { get; private set; }

        /// <summary>
        /// Gets the command abstract syntax tree (AST).
        /// </summary>
        /// <value>The command AST.</value>
        public CommandAst CommandAst { get; private set; }

        /// <summary>
        /// Gets the fake bound parameters similar to $PSBoundParameters.
        /// </summary>
        /// <value>The fake bound parameters.</value>
        public IDictionary FakeBoundParameters { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the argument should be completed.
        /// </summary>
        /// <value><c>true</c> if the argument should be completed; otherwise, <c>false</c>.</value>
        public bool ShouldComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets the type of the completion result.
        /// </summary>
        /// <value>The type of the completion result.</value>
        public CompletionResultType CompletionResultType { get; set; } = CompletionResultType.Text;

        /// <summary>
        /// Gets or sets the mapping function for tooltips.
        /// </summary>
        /// <value>The mapping function for tooltips.</value>
        public Func<string, string> ToolTipMapping { get; set; }

        /// <summary>
        /// Gets or sets the mapping function for list item texts.
        /// </summary>
        /// <value>The mapping function for list item texts.</value>
        public Func<string, string> ListItemTextMapping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to escape globbing paths.
        /// </summary>
        /// <value><c>true</c> if globbing paths should be escaped; otherwise, <c>false</c>.</value>
        public bool EscapeGlobbingPath { get; set; }

        /// <summary>
        /// Gets or sets the possible completion values.
        /// </summary>
        /// <value>The possible completion values.</value>
        public IEnumerable<string> PossibleCompletionValues { get; set; } = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentCompleterOptions"/> class.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="wordToComplete">The word being completed.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        public ArgumentCompleterOptions(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            CommandName = commandName;
            ParameterName = parameterName;
            WordToComplete = wordToComplete;
            CommandAst = commandAst;
            FakeBoundParameters = fakeBoundParameters;
        }
    }

    /// <summary>
    /// Creates a new argument completer.
    /// </summary>
    /// <para>
    /// If an attribute that derives from <see cref="ArgumentCompleterAttribute"/> implements this interface,
    /// it will be used to create the <see cref="IArgumentCompleter"/>, thus giving a way to parameterize a completer.
    /// The derived attribute can have properties or constructor arguments that are used when creating the completer.
    /// </para>
    /// <example>
    /// This example shows the intended usage of <see cref="IArgumentCompleterFactory"/> to pass arguments to an argument completer.
    /// <code>
    /// public class NumberCompleterAttribute : ArgumentCompleterAttribute, IArgumentCompleterFactory {
    ///    private readonly int _from;
    ///    private readonly int _to;
    ///
    ///    public NumberCompleterAttribute(int from, int to){
    ///       _from = from;
    ///       _to = to;
    ///    }
    ///
    ///    // use the attribute parameters to create a parameterized completer
    ///    IArgumentCompleter Create() => new NumberCompleter(_from, _to);
    /// }
    ///
    /// class NumberCompleter : IArgumentCompleter {
    ///    private readonly int _from;
    ///    private readonly int _to;
    ///
    ///    public NumberCompleter(int from, int to){
    ///       _from = from;
    ///       _to = to;
    ///    }
    ///
    ///    IEnumerable{CompletionResult} CompleteArgument(string commandName, string parameterName, string wordToComplete,
    ///       CommandAst commandAst, IDictionary fakeBoundParameters) {
    ///       for(int i = _from; i &lt; _to; i++) {
    ///           yield return new CompletionResult(i.ToString());
    ///       }
    ///    }
    /// }
    /// </code>
    /// </example>
    public interface IArgumentCompleterFactory
    {
        /// <summary>
        /// Creates an instance of a class implementing the <see cref="IArgumentCompleter"/> interface.
        /// </summary>
        /// <returns>An IArgumentCompleter instance.</returns>
        IArgumentCompleter Create();
    }

    /// <summary>
    /// Base class for parameterized argument completer attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ArgumentCompleterFactoryAttribute : ArgumentCompleterAttribute, IArgumentCompleterFactory
    {
        /// <inheritdoc />
        public abstract IArgumentCompleter Create();
    }

    /// <summary>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Register, "ArgumentCompleter", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=528576")]
    public class RegisterArgumentCompleterCommand : PSCmdlet
    {
        /// <summary>
        /// </summary>
        [Parameter(ParameterSetName = "NativeSet", Mandatory = true)]
        [Parameter(ParameterSetName = "PowerShellSet")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CommandName { get; set; }

        /// <summary>
        /// </summary>
        [Parameter(ParameterSetName = "PowerShellSet", Mandatory = true)]
        public string ParameterName { get; set; }

        /// <summary>
        /// </summary>
        [Parameter(Mandatory = true)]
        [AllowNull()]
        public ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        /// </summary>
        [Parameter(ParameterSetName = "NativeSet")]
        public SwitchParameter Native { get; set; }

        /// <summary>
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
                CommandName = new[] { string.Empty };
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
        private readonly string[] _completions;

        /// <summary>
        /// Initializes a new instance of the ArgumentCompletionsAttribute class.
        /// </summary>
        /// <param name="completions">List of complete values.</param>
        /// <exception cref="ArgumentNullException">For null arguments.</exception>
        /// <exception cref="ArgumentOutOfRangeException">For invalid arguments.</exception>
        public ArgumentCompletionsAttribute(params string[] completions)
        {
            if (completions == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(completions));
            }

            if (completions.Length == 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(completions), completions);
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
}
