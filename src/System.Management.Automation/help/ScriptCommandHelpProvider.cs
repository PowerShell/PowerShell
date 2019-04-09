// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Class ScriptCommandHelpProvider implement the help provider for Functions/ExternalScripts.
    /// This class does the same thing as CommandHelpProvider except for decision making: whether
    /// a particular command is Function/Script or not.
    /// </summary>
    /// <remarks>
    /// Command Help information are stored in 'help.xml' files. Location of these files
    /// can be found from through the engine execution context.
    /// </remarks>
    internal class ScriptCommandHelpProvider : CommandHelpProvider
    {
        /// <summary>
        /// Constructor for CommandHelpProvider.
        /// </summary>
        internal ScriptCommandHelpProvider(HelpSystem helpSystem)
            : base(helpSystem)
        {
        }

        #region Overrides

        /// <summary>
        /// Help category for this provider, which is a constant: HelpCategory.Command.
        /// </summary>
        /// <value>Help category for this provider</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return
                    HelpCategory.ExternalScript |
                    HelpCategory.Filter |
                    HelpCategory.Function |
                    HelpCategory.Configuration |
                    HelpCategory.ScriptCommand |
                    HelpCategory.Workflow;
            }
        }

        /// <summary>
        /// Gets a command searcher used for ExactMatch help lookup.
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override CommandSearcher GetCommandSearcherForExactMatch(string commandName, ExecutionContext context)
        {
            CommandSearcher searcher = new CommandSearcher(
                commandName,
                SearchResolutionOptions.None,
                CommandTypes.Filter | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration,
                context);

            return searcher;
        }

        /// <summary>
        /// Gets a command searcher used for searching help.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        internal override CommandSearcher GetCommandSearcherForSearch(string pattern, ExecutionContext context)
        {
            CommandSearcher searcher =
                    new CommandSearcher(
                        pattern,
                        SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveFunctionPatterns,
                        CommandTypes.Filter | CommandTypes.Function | CommandTypes.ExternalScript | CommandTypes.Configuration,
                        context);

            return searcher;
        }

        #endregion
    }
}