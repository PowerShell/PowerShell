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
            IDictionary fakeBoundParameters);
    }
#nullable restore

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
