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
        private const string PowerShellSetName = "PowerShellSet";
        private const string NativeCommandSetName = "NativeCommandSet";
        private const string NativeFallbackSetName = "NativeFallbackSet";

        // Use a key that is unlikely to be a file name or path to indicate the fallback completer for native commands.
        internal const string FallbackCompleterKey = "___ps::<native_fallback_key>@@___";

        /// <summary>
        /// Gets or sets the command names for which the argument completer is registered.
        /// </summary>
        [Parameter(ParameterSetName = NativeCommandSetName, Mandatory = true)]
        [Parameter(ParameterSetName = PowerShellSetName)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CommandName { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter for which the argument completer is registered.
        /// </summary>
        [Parameter(ParameterSetName = PowerShellSetName, Mandatory = true)]
        public string ParameterName { get; set; }

        /// <summary>
        /// Gets or sets the script block that will be executed to provide argument completions.
        /// </summary>
        [Parameter(Mandatory = true)]
        [AllowNull()]
        public ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        /// Indicates the argument completer is for native commands.
        /// </summary>
        [Parameter(ParameterSetName = NativeCommandSetName)]
        public SwitchParameter Native { get; set; }

        /// <summary>
        /// Indicates the argument completer is a fallback for any native commands that don't have a completer registered.
        /// </summary>
        [Parameter(ParameterSetName = NativeFallbackSetName)]
        public SwitchParameter NativeFallback { get; set; }

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            Dictionary<string, ScriptBlock> completerDictionary;

            if (ParameterSetName is NativeFallbackSetName)
            {
                completerDictionary = Context.NativeArgumentCompleters ??= new(StringComparer.OrdinalIgnoreCase);

                SetKeyValue(completerDictionary, FallbackCompleterKey, ScriptBlock);
            }
            else if (ParameterSetName is NativeCommandSetName)
            {
                completerDictionary = Context.NativeArgumentCompleters ??= new(StringComparer.OrdinalIgnoreCase);

                foreach (string command in CommandName)
                {
                    var key = command?.Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    SetKeyValue(completerDictionary, key, ScriptBlock);
                }
            }
            else if (ParameterSetName is PowerShellSetName)
            {
                completerDictionary = Context.CustomArgumentCompleters ??= new(StringComparer.OrdinalIgnoreCase);

                string paramName = ParameterName.Trim();
                if (paramName.Length is 0)
                {
                    return;
                }

                if (CommandName is null || CommandName.Length is 0)
                {
                    SetKeyValue(completerDictionary, paramName, ScriptBlock);
                    return;
                }

                foreach (string command in CommandName)
                {
                    var key = command?.Trim();
                    key = string.IsNullOrEmpty(key)
                        ? paramName
                        : $"{key}:{paramName}";

                    SetKeyValue(completerDictionary, key, ScriptBlock);
                }
            }

            static void SetKeyValue(Dictionary<string, ScriptBlock> table, string key, ScriptBlock value)
            {
                if (value is null)
                {
                    table.Remove(key);
                }
                else
                {
                    table[key] = value;
                }
            }
        }
    }

    /// <summary>
    /// Specifies the type of argument completer.
    /// </summary>
    public enum ArgumentCompleterType
    {
        /// <summary>
        /// A completer for PowerShell command parameters.
        /// </summary>
        PowerShell,

        /// <summary>
        /// A completer for native command arguments.
        /// </summary>
        Native,

        /// <summary>
        /// A fallback completer for native commands that don't have a specific completer.
        /// </summary>
        NativeFallback,
    }

    /// <summary>
    /// Represents information about a registered argument completer.
    /// </summary>
    public sealed class ArgumentCompleterInfo
    {
        /// <summary>
        /// Gets the command name associated with this completer.
        /// For PowerShell completers without a command, this may be null.
        /// For native fallback completers, this is null.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Gets the parameter name for PowerShell completers.
        /// Null for native command completers.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Gets the script block that provides completions.
        /// </summary>
        public ScriptBlock ScriptBlock { get; }

        /// <summary>
        /// Gets the type of this argument completer.
        /// </summary>
        public ArgumentCompleterType Type { get; }

        internal ArgumentCompleterInfo(string commandName, string parameterName, ScriptBlock scriptBlock, ArgumentCompleterType type)
        {
            CommandName = commandName;
            ParameterName = parameterName;
            ScriptBlock = scriptBlock;
            Type = type;
        }
    }

    /// <summary>
    /// Gets registered argument completers.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ArgumentCompleter", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=528576")]
    [OutputType(typeof(ArgumentCompleterInfo))]
    public class GetArgumentCompleterCommand : PSCmdlet
    {
        private const string PowerShellSetName = "PowerShellSet";
        private const string NativeSetName = "NativeSet";

        /// <summary>
        /// Gets or sets the command name to filter completers.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = PowerShellSetName)]
        [Parameter(Position = 0, ParameterSetName = NativeSetName)]
        [SupportsWildcards]
        public string[] CommandName { get; set; }

        /// <summary>
        /// Gets or sets the parameter name to filter PowerShell completers.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = PowerShellSetName)]
        [SupportsWildcards]
        public string[] ParameterName { get; set; }

        /// <summary>
        /// If specified, returns native command completers.
        /// </summary>
        [Parameter(ParameterSetName = NativeSetName)]
        public SwitchParameter Native { get; set; }

        /// <summary>
        /// EndProcessing implementation.
        /// </summary>
        protected override void EndProcessing()
        {
            var commandPatterns = CreateWildcardPatterns(CommandName);
            var parameterPatterns = CreateWildcardPatterns(ParameterName);

            if (Native.IsPresent)
            {
                // Return native command completers
                var nativeCompleters = Context.NativeArgumentCompleters;
                if (nativeCompleters != null)
                {
                    foreach (var kvp in nativeCompleters)
                    {
                        if (kvp.Key == RegisterArgumentCompleterCommand.FallbackCompleterKey)
                        {
                            // Only include fallback if no CommandName filter or if filtering explicitly
                            if (commandPatterns == null)
                            {
                                WriteObject(new ArgumentCompleterInfo(null, null, kvp.Value, ArgumentCompleterType.NativeFallback));
                            }
                        }
                        else
                        {
                            if (MatchesWildcardPatterns(kvp.Key, commandPatterns))
                            {
                                WriteObject(new ArgumentCompleterInfo(kvp.Key, null, kvp.Value, ArgumentCompleterType.Native));
                            }
                        }
                    }
                }
            }
            else
            {
                // Return PowerShell command completers
                var customCompleters = Context.CustomArgumentCompleters;
                if (customCompleters != null)
                {
                    foreach (var kvp in customCompleters)
                    {
                        // Key format is either "ParameterName" or "CommandName:ParameterName"
                        var colonIndex = kvp.Key.IndexOf(':');
                        string cmdName = null;
                        string paramName;

                        if (colonIndex >= 0)
                        {
                            cmdName = kvp.Key.Substring(0, colonIndex);
                            paramName = kvp.Key.Substring(colonIndex + 1);
                        }
                        else
                        {
                            paramName = kvp.Key;
                        }

                        // Apply filters
                        if (!MatchesWildcardPatterns(cmdName, commandPatterns))
                        {
                            continue;
                        }

                        if (!MatchesWildcardPatterns(paramName, parameterPatterns))
                        {
                            continue;
                        }

                        WriteObject(new ArgumentCompleterInfo(cmdName, paramName, kvp.Value, ArgumentCompleterType.PowerShell));
                    }
                }
            }
        }

        private static WildcardPattern[] CreateWildcardPatterns(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return null;
            }

            var patterns = new WildcardPattern[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                patterns[i] = WildcardPattern.Get(values[i], WildcardOptions.IgnoreCase);
            }

            return patterns;
        }

        private static bool MatchesWildcardPatterns(string value, WildcardPattern[] patterns)
        {
            if (patterns == null)
            {
                return true;
            }

            if (value == null)
            {
                // For null values, only match if one of the patterns is "*" or null
                foreach (var pattern in patterns)
                {
                    if (pattern.IsMatch(string.Empty))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var pattern in patterns)
            {
                if (pattern.IsMatch(value))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Unregisters argument completers.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Unregister, "ArgumentCompleter", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=528576")]
    public class UnregisterArgumentCompleterCommand : PSCmdlet
    {
        private const string PowerShellSetName = "PowerShellSet";
        private const string NativeCommandSetName = "NativeCommandSet";
        private const string NativeFallbackSetName = "NativeFallbackSet";

        /// <summary>
        /// Gets or sets the command names for which to unregister the argument completer.
        /// </summary>
        [Parameter(ParameterSetName = NativeCommandSetName, Mandatory = true)]
        [Parameter(ParameterSetName = PowerShellSetName)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CommandName { get; set; }

        /// <summary>
        /// Gets or sets the parameter name for which to unregister the argument completer.
        /// </summary>
        [Parameter(ParameterSetName = PowerShellSetName, Mandatory = true)]
        public string ParameterName { get; set; }

        /// <summary>
        /// Indicates the argument completer is for native commands.
        /// </summary>
        [Parameter(ParameterSetName = NativeCommandSetName)]
        public SwitchParameter Native { get; set; }

        /// <summary>
        /// Indicates to unregister the fallback completer for native commands.
        /// </summary>
        [Parameter(ParameterSetName = NativeFallbackSetName, Mandatory = true)]
        public SwitchParameter NativeFallback { get; set; }

        /// <summary>
        /// EndProcessing implementation.
        /// </summary>
        protected override void EndProcessing()
        {
            if (ParameterSetName is NativeFallbackSetName)
            {
                var nativeCompleters = Context.NativeArgumentCompleters;
                if (nativeCompleters != null)
                {
                    nativeCompleters.Remove(RegisterArgumentCompleterCommand.FallbackCompleterKey);
                }
            }
            else if (ParameterSetName is NativeCommandSetName)
            {
                var nativeCompleters = Context.NativeArgumentCompleters;
                if (nativeCompleters != null)
                {
                    foreach (string command in CommandName)
                    {
                        var key = command?.Trim();
                        if (string.IsNullOrEmpty(key))
                        {
                            continue;
                        }

                        nativeCompleters.Remove(key);
                    }
                }
            }
            else if (ParameterSetName is PowerShellSetName)
            {
                var customCompleters = Context.CustomArgumentCompleters;
                if (customCompleters != null)
                {
                    string paramName = ParameterName.Trim();
                    if (paramName.Length is 0)
                    {
                        return;
                    }

                    if (CommandName is null || CommandName.Length is 0)
                    {
                        customCompleters.Remove(paramName);
                        return;
                    }

                    foreach (string command in CommandName)
                    {
                        var key = command?.Trim();
                        key = string.IsNullOrEmpty(key)
                            ? paramName
                            : $"{key}:{paramName}";

                        customCompleters.Remove(key);
                    }
                }
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