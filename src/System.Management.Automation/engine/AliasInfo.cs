// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a mapping between a command name and a real command.
    /// </summary>
    public class AliasInfo : CommandInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the AliasInfo class with the specified name and referenced command.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="definition">
        /// The token that the alias refers to.
        /// </param>
        /// <param name="context">
        /// The execution context for this engine, used to lookup the current session state.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="definition"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        internal AliasInfo(string name, string definition, ExecutionContext context) : base(name, CommandTypes.Alias)
        {
            _definition = definition;
            this.Context = context;

            if (context != null)
            {
                this.Module = context.SessionState.Internal.Module;
            }
        }

        /// <summary>
        /// Creates an instance of the AliasInfo class with the specified name and referenced command.
        /// </summary>
        /// <param name="name">
        /// The name of the command.
        /// </param>
        /// <param name="definition">
        /// The token that the alias refers to.
        /// </param>
        /// <param name="context">
        /// The execution context for this engine instance, used to look up session state.
        /// </param>
        /// <param name="options">
        /// The options to set on the alias. Note, Constant can only be set at creation time.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="definition"/> is null or empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        internal AliasInfo(
            string name,
            string definition,
            ExecutionContext context,
            ScopedItemOptions options) : base(name, CommandTypes.Alias)
        {
            _definition = definition;
            this.Context = context;
            _options = options;

            if (context != null)
            {
                this.Module = context.SessionState.Internal.Module;
            }
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Alias; }
        }

        /// <summary>
        /// Gets the command information for the command that is immediately referenced by this alias.
        /// </summary>
        public CommandInfo ReferencedCommand
        {
            get
            {
                // Need to lookup the referenced command every time
                // to ensure we get the latest session state information

                CommandInfo referencedCommand = null;

                if ((_definition != null) && (Context != null))
                {
                    CommandSearcher commandSearcher =
                        new CommandSearcher(
                            _definition,
                            SearchResolutionOptions.None,
                            CommandTypes.All,
                            Context);

                    if (commandSearcher.MoveNext())
                    {
                        System.Collections.Generic.IEnumerator<CommandInfo> ie = commandSearcher;
                        referencedCommand = ie.Current;
                        // referencedCommand = commandSearcher.Current;
                    }
                }

                return referencedCommand;
            }
        }

        /// <summary>
        /// Gets the command information for the command that
        /// the alias eventually resolves to.
        /// </summary>
        /// <remarks>
        /// An alias may reference another alias. This property follows the reference
        /// chain of aliases to its end.
        /// </remarks>
        /// <!--
        /// If the command didn't resolve to anything but aliases, the UnresolvedCommandName
        /// property contains the last name the resolution succeeded in finding.
        /// -->
        public CommandInfo ResolvedCommand
        {
            get
            {
                // Need to lookup the resolved command every time to ensure
                // we use the latest session state information

                CommandInfo result = null;

                if (_definition != null)
                {
                    List<string> cyclePrevention = new List<string>();
                    cyclePrevention.Add(Name);

                    string commandNameToResolve = _definition;
                    result = ReferencedCommand;
                    while (result != null && result.CommandType == CommandTypes.Alias)
                    {
                        result = ((AliasInfo)result).ReferencedCommand;

                        if (result is AliasInfo)
                        {
                            // Check for the cycle by checking for the alias name
                            // in the cyclePrevention dictionary

                            if (SessionStateUtilities.CollectionContainsValue(cyclePrevention, result.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                result = null;
                                break;
                            }

                            cyclePrevention.Add(result.Name);

                            commandNameToResolve = result.Definition;
                        }
                    }

                    if (result == null)
                    {
                        // Since we couldn't resolve the command that the alias
                        // points to, remember the definition so that we can
                        // provide better error reporting.

                        UnresolvedCommandName = commandNameToResolve;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the name of the command to which the alias refers.
        /// </summary>
        public override string Definition
        {
            get
            {
                return _definition;
            }
        }

        private string _definition = string.Empty;

        /// <summary>
        /// Sets the new definition for the alias.
        /// </summary>
        /// <param name="definition">
        /// The new definition for the alias.
        /// </param>
        /// <param name="force">
        /// If true, the value will be set even if the alias is ReadOnly.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the alias is readonly or constant.
        /// </exception>
        internal void SetDefinition(string definition, bool force)
        {
            // Check to see if the variable is writable

            if ((_options & ScopedItemOptions.Constant) != 0 ||
                (!force && (_options & ScopedItemOptions.ReadOnly) != 0))
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Alias,
                            "AliasNotWritable",
                            SessionStateStrings.AliasNotWritable);

                throw e;
            }

            _definition = definition;
        }

        /// <summary>
        /// Gets or sets the scope options for the alias.
        /// </summary>
        /// <exception cref="System.Management.Automation.SessionStateUnauthorizedAccessException">
        /// If the trying to set an alias that is constant or
        ///     if the value trying to be set is ScopedItemOptions.Constant
        /// </exception>
        public ScopedItemOptions Options
        {
            get
            {
                return _options;
            }

            set
            {
                SetOptions(value, false);
            }
        }

        /// <summary>
        /// Sets the options for the alias and allows changes ReadOnly options only if force is specified.
        /// </summary>
        /// <param name="newOptions">
        /// The new options value.
        /// </param>
        /// <param name="force">
        /// If true the change to the options will happen even if the existing options are read-only.
        /// </param>
        internal void SetOptions(ScopedItemOptions newOptions, bool force)
        {
            // Check to see if the variable is constant, if so
            // throw an exception because the options cannot be changed.

            if ((_options & ScopedItemOptions.Constant) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Alias,
                            "AliasIsConstant",
                            SessionStateStrings.AliasIsConstant);

                throw e;
            }

            // Check to see if the variable is readonly, if so
            // throw an exception because the options cannot be changed.

            if (!force && (_options & ScopedItemOptions.ReadOnly) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Alias,
                            "AliasIsReadOnly",
                            SessionStateStrings.AliasIsReadOnly);

                throw e;
            }

            // Now check to see if the caller is trying to set
            // the options to constant. This is only allowed at
            // variable creation

            if ((newOptions & ScopedItemOptions.Constant) != 0)
            {
                // user is trying to set the variable to constant after
                // creating the variable. Do not allow this (as per spec).

                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Alias,
                            "AliasCannotBeMadeConstant",
                            SessionStateStrings.AliasCannotBeMadeConstant);

                throw e;
            }

            if ((newOptions & ScopedItemOptions.AllScope) == 0 &&
                (_options & ScopedItemOptions.AllScope) != 0)
            {
                // user is trying to remove the AllScope option from the alias.
                // Do not allow this (as per spec).

                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            this.Name,
                            SessionStateCategory.Alias,
                            "AliasAllScopeOptionCannotBeRemoved",
                            SessionStateStrings.AliasAllScopeOptionCannotBeRemoved);

                throw e;
            }

            _options = newOptions;
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// Gets or sets the description for the alias.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// If ResolvedCommand returns null, this property will
        /// return the name of the command that could not be resolved.
        /// If ResolvedCommand has not yet been called or was able
        /// to resolve the command, this this property will return null.
        /// </summary>
        internal string UnresolvedCommandName { get; private set; }

        /// <summary>
        /// The objects output from an alias are the objects output from the resolved
        /// command.  If we can't resolve the command, assume nothing is output - so use void.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                CommandInfo resolvedCommand = this.ResolvedCommand;
                if (resolvedCommand != null)
                {
                    return resolvedCommand.OutputType;
                }

                return null;
            }
        }
    }
}
