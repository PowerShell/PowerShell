// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// The command information for MSH scripts that are directly executable by MSH.
    /// </summary>
    public class ScriptInfo : CommandInfo, IScriptCommandInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the ScriptInfo class with the specified name, and script.
        /// </summary>
        /// <param name="name">
        /// The name of the script.
        /// </param>
        /// <param name="script">
        /// The script definition
        /// </param>
        /// <param name="context">
        /// The execution context for the script.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="script"/> is null.
        /// </exception>
        internal ScriptInfo(string name, ScriptBlock script, ExecutionContext context)
            : base(name, CommandTypes.Script, context)
        {
            if (script == null)
            {
                throw PSTraceSource.NewArgumentException("script");
            }

            this.ScriptBlock = script;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal ScriptInfo(ScriptInfo other)
            : base(other)
        {
            this.ScriptBlock = other.ScriptBlock;
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] argumentList)
        {
            ScriptInfo copy = new ScriptInfo(this) { IsGetCommandCopy = true, Arguments = argumentList };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.ScriptCommand; }
        }

        /// <summary>
        /// Gets the ScriptBlock that represents the implementation of the script.
        /// </summary>
        public ScriptBlock ScriptBlock { get; private set; }

        // Path

        /// <summary>
        /// Gets the definition of the ScriptBlock for the script. This is the ToString() of
        /// the ScriptBlock.
        /// </summary>
        public override string Definition
        {
            get
            {
                return ScriptBlock.ToString();
            }
        }

        /// <summary>
        /// The output type(s) is specified in the script block.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return ScriptBlock.OutputType; }
        }

        /// <summary>
        /// For diagnostic purposes.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ScriptBlock.ToString();
        }

        /// <summary>
        /// True if the command has dynamic parameters, false otherwise.
        /// </summary>
        internal override bool ImplementsDynamicParameters
        {
            get { return ScriptBlock.HasDynamicParameters; }
        }

        /// <summary>
        /// The command metadata for the script.
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
    }
}
