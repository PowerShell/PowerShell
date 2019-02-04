// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a function that is stored in session state.
    /// </summary>
    public class FunctionInfo : CommandInfo, IScriptCommandInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the FunctionInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the function.
        /// </param>
        /// <param name="function">
        /// The ScriptBlock for the function
        /// </param>
        /// <param name="context">
        /// The execution context for the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="function"/> is null.
        /// </exception>
        internal FunctionInfo(string name, ScriptBlock function, ExecutionContext context) : this(name, function, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the FunctionInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the function.
        /// </param>
        /// <param name="function">
        /// The ScriptBlock for the function
        /// </param>
        /// <param name="context">
        /// The execution context for the function.
        /// </param>
        /// <param name="helpFile">
        /// The name of the help file associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="function"/> is null.
        /// </exception>
        internal FunctionInfo(string name, ScriptBlock function, ExecutionContext context, string helpFile) : base(name, CommandTypes.Function, context)
        {
            if (function == null)
            {
                throw PSTraceSource.NewArgumentNullException("function");
            }

            _scriptBlock = function;

            CmdletInfo.SplitCmdletName(name, out _verb, out _noun);

            this.Module = function.Module;
            _helpFile = helpFile;
        }

        /// <summary>
        /// Creates an instance of the FunctionInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the function.
        /// </param>
        /// <param name="function">
        /// The ScriptBlock for the function
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="function"/> is null.
        /// </exception>
        internal FunctionInfo(string name, ScriptBlock function, ScopedItemOptions options, ExecutionContext context) : this(name, function, options, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the FunctionInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the function.
        /// </param>
        /// <param name="function">
        /// The ScriptBlock for the function
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the function.
        /// </param>
        /// <param name="helpFile">
        /// The name of the help file associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="function"/> is null.
        /// </exception>
        internal FunctionInfo(string name, ScriptBlock function, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : this(name, function, context, helpFile)
        {
            _options = options;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal FunctionInfo(FunctionInfo other)
            : base(other)
        {
            CopyFieldsFromOther(other);
        }

        private void CopyFieldsFromOther(FunctionInfo other)
        {
            _verb = other._verb;
            _noun = other._noun;
            _scriptBlock = other._scriptBlock;
            _description = other._description;
            _options = other._options;
            _helpFile = other._helpFile;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal FunctionInfo(string name, FunctionInfo other)
            : base(name, other)
        {
            CopyFieldsFromOther(other);

            // Get the verb and noun from the name
            CmdletInfo.SplitCmdletName(name, out _verb, out _noun);
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            FunctionInfo copy = new FunctionInfo(this) { IsGetCommandCopy = true, Arguments = arguments };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Function; }
        }

        /// <summary>
        /// Gets the ScriptBlock which is the implementation of the function.
        /// </summary>
        public ScriptBlock ScriptBlock
        {
            get { return _scriptBlock; }
        }

        private ScriptBlock _scriptBlock;

        /// <summary>
        /// Updates a function.
        /// </summary>
        /// <param name="newFunction">
        /// The script block that the function should represent.
        /// </param>
        /// <param name="force">
        /// If true, the script block will be applied even if the filter is ReadOnly.
        /// </param>
        /// <param name="options">
        /// Any options to set on the new function, null if none.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="newFunction"/> is null.
        /// </exception>
        internal void Update(ScriptBlock newFunction, bool force, ScopedItemOptions options)
        {
            Update(newFunction, force, options, null);
            this.DefiningLanguageMode = newFunction.LanguageMode;
        }

        /// <summary/>
        protected internal virtual void Update(FunctionInfo newFunction, bool force, ScopedItemOptions options, string helpFile)
        {
            Update(newFunction.ScriptBlock, force, options, helpFile);
        }

        /// <summary>
        /// Updates a function.
        /// </summary>
        /// <param name="newFunction">
        /// The script block that the function should represent.
        /// </param>
        /// <param name="force">
        /// If true, the script block will be applied even if the filter is ReadOnly.
        /// </param>
        /// <param name="options">
        /// Any options to set on the new function, null if none.
        /// </param>
        /// <param name="helpFile">
        /// The helpfile for this function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="newFunction"/> is null.
        /// </exception>
        internal void Update(ScriptBlock newFunction, bool force, ScopedItemOptions options, string helpFile)
        {
            if (newFunction == null)
            {
                throw PSTraceSource.NewArgumentNullException("function");
            }

            if ((_options & ScopedItemOptions.Constant) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Function,
                            "FunctionIsConstant",
                            SessionStateStrings.FunctionIsConstant);

                throw e;
            }

            if (!force && (_options & ScopedItemOptions.ReadOnly) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Function,
                            "FunctionIsReadOnly",
                            SessionStateStrings.FunctionIsReadOnly);

                throw e;
            }

            _scriptBlock = newFunction;

            this.Module = newFunction.Module;
            _commandMetadata = null;
            this._parameterSets = null;
            this.ExternalCommandMetadata = null;

            if (options != ScopedItemOptions.Unspecified)
            {
                this.Options = options;
            }

            _helpFile = helpFile;
        }

        /// <summary>
        /// Returns <c>true</c> if this function uses cmdlet binding mode for its parameters; otherwise returns <c>false</c>.
        /// </summary>
        public bool CmdletBinding
        {
            get
            {
                return this.ScriptBlock.UsesCmdletBinding;
            }
        }

        /// <summary>
        /// Gets the name of the default parameter set.
        /// Returns <c>null</c> if this function doesn't use cmdlet parameter binding or if the default parameter set wasn't specified.
        /// </summary>
        public string DefaultParameterSet
        {
            get
            {
                return this.CmdletBinding ? this.CommandMetadata.DefaultParameterSetName : null;
            }
        }

        /// <summary>
        /// Gets the definition of the function which is the
        /// ToString() of the ScriptBlock that implements the function.
        /// </summary>
        public override string Definition { get { return _scriptBlock.ToString(); } }

        /// <summary>
        /// Gets or sets the scope options for the function.
        /// </summary>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the trying to set a function that is constant or
        ///     if the value trying to be set is ScopedItemOptions.Constant
        /// </exception>
        public ScopedItemOptions Options
        {
            get
            {
                return CopiedCommand == null ? _options : ((FunctionInfo)CopiedCommand).Options;
            }

            set
            {
                if (CopiedCommand == null)
                {
                    // Check to see if the function is constant, if so
                    // throw an exception because the options cannot be changed.

                    if ((_options & ScopedItemOptions.Constant) != 0)
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    Name,
                                    SessionStateCategory.Function,
                                    "FunctionIsConstant",
                                    SessionStateStrings.FunctionIsConstant);

                        throw e;
                    }

                    // Now check to see if the caller is trying to set
                    // the options to constant. This is only allowed at
                    // variable creation

                    if ((value & ScopedItemOptions.Constant) != 0)
                    {
                        // user is trying to set the function to constant after
                        // creating the function. Do not allow this (as per spec).

                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    Name,
                                    SessionStateCategory.Function,
                                    "FunctionCannotBeMadeConstant",
                                    SessionStateStrings.FunctionCannotBeMadeConstant);

                        throw e;
                    }

                    // Ensure we are not trying to remove the AllScope option

                    if ((value & ScopedItemOptions.AllScope) == 0 &&
                        (_options & ScopedItemOptions.AllScope) != 0)
                    {
                        SessionStateUnauthorizedAccessException e =
                            new SessionStateUnauthorizedAccessException(
                                    this.Name,
                                    SessionStateCategory.Function,
                                    "FunctionAllScopeOptionCannotBeRemoved",
                                    SessionStateStrings.FunctionAllScopeOptionCannotBeRemoved);

                        throw e;
                    }

                    _options = value;
                }
                else
                {
                    ((FunctionInfo)CopiedCommand).Options = value;
                }
            }
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// Gets or sets the description associated with the function.
        /// </summary>
        public string Description
        {
            get
            {
                return CopiedCommand == null ? _description : ((FunctionInfo)CopiedCommand).Description;
            }

            set
            {
                if (CopiedCommand == null)
                {
                    _description = value;
                }
                else
                {
                    ((FunctionInfo)CopiedCommand).Description = value;
                }
            }
        }

        private string _description = null;

        /// <summary>
        /// Gets the verb of the function.
        /// </summary>
        public string Verb
        {
            get
            {
                return _verb;
            }
        }

        private string _verb = string.Empty;

        /// <summary>
        /// Gets the noun of the function.
        /// </summary>
        public string Noun
        {
            get
            {
                return _noun;
            }
        }

        private string _noun = string.Empty;

        /// <summary>
        /// Gets the help file path for the function.
        /// </summary>
        public string HelpFile
        {
            get
            {
                return _helpFile;
            }

            internal set
            {
                _helpFile = value;
            }
        }

        private string _helpFile = string.Empty;

        /// <summary>
        /// Returns the syntax of a command.
        /// </summary>
        internal override string Syntax
        {
            get
            {
                StringBuilder synopsis = new StringBuilder();

                foreach (CommandParameterSetInfo parameterSet in ParameterSets)
                {
                    synopsis.AppendLine();
                    synopsis.AppendLine(
                        string.Format(
                            Globalization.CultureInfo.CurrentCulture,
                            "{0} {1}",
                            Name,
                            parameterSet.ToString()));
                }

                return synopsis.ToString();
            }
        }

        /// <summary>
        /// True if the command has dynamic parameters, false otherwise.
        /// </summary>
        internal override bool ImplementsDynamicParameters
        {
            get { return ScriptBlock.HasDynamicParameters; }
        }

        /// <summary>
        /// The command metadata for the function or filter.
        /// </summary>
        internal override CommandMetadata CommandMetadata
        {
            get
            {
                return _commandMetadata ??
                       (_commandMetadata =
                        new CommandMetadata(this.ScriptBlock, this.Name, LocalPipeline.GetExecutionContextFromTLS()));
            }
        }

        private CommandMetadata _commandMetadata;

        /// <summary>
        /// The output type(s) is specified in the script block.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return ScriptBlock.OutputType; }
        }
    }
}
