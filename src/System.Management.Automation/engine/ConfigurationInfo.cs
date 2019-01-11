// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a configuration that is stored in session state.
    /// </summary>
    public class ConfigurationInfo : FunctionInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the ConfigurationInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context) : this(name, configuration, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the ConfigurationInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ExecutionContext context, string helpFile)
            : base(name, configuration, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
        }

        /// <summary>
        /// Creates an instance of the ConfigurationInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context) : this(name, configuration, options, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the ConfigurationInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <param name="isMetaConfig">The configuration is a meta configuration.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context, string helpFile, bool isMetaConfig)
            : base(name, configuration, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Configuration);
            IsMetaConfiguration = isMetaConfig;
        }

        /// <summary>
        /// Creates an instance of the ConfigurationInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the configuration.
        /// </param>
        /// <param name="configuration">
        /// The ScriptBlock for the configuration
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the configuration.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the configuration.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        internal ConfigurationInfo(string name, ScriptBlock configuration, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : this(name, configuration, options, context, helpFile, false)
        {
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal ConfigurationInfo(ConfigurationInfo other)
            : base(other)
        {
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal ConfigurationInfo(string name, ConfigurationInfo other)
            : base(name, other)
        {
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            var copy = new ConfigurationInfo(this) { IsGetCommandCopy = true, Arguments = arguments };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Configuration; }
        }

        /// <summary>
        /// Indication whether the configuration is a meta-configuration.
        /// </summary>
        public bool IsMetaConfiguration
        { get; internal set; }
    }
}
