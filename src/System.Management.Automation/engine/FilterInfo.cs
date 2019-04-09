// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information about a filter that is stored in session state.
    /// </summary>
    public class FilterInfo : FunctionInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the FilterInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context) : this(name, filter, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the FilterInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="context">
        /// The ExecutionContext for the filter.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ExecutionContext context, string helpFile)
            : base(name, filter, context, helpFile)
        {
            SetCommandType(CommandTypes.Filter);
        }

        /// <summary>
        /// Creates an instance of the FilterInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ScopedItemOptions options, ExecutionContext context) : this(name, filter, options, context, null)
        {
        }

        /// <summary>
        /// Creates an instance of the FilterInfo class with the specified name and ScriptBlock.
        /// </summary>
        /// <param name="name">
        /// The name of the filter.
        /// </param>
        /// <param name="filter">
        /// The ScriptBlock for the filter
        /// </param>
        /// <param name="options">
        /// The options to set on the function. Note, Constant can only be set at creation time.
        /// </param>
        /// <param name="context">
        /// The execution context for the filter.
        /// </param>
        /// <param name="helpFile">
        /// The help file for the filter.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="filter"/> is null.
        /// </exception>
        internal FilterInfo(string name, ScriptBlock filter, ScopedItemOptions options, ExecutionContext context, string helpFile)
            : base(name, filter, options, context, helpFile)
        {
            SetCommandType(CommandTypes.Filter);
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal FilterInfo(FilterInfo other)
            : base(other)
        {
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal FilterInfo(string name, FilterInfo other)
            : base(name, other)
        {
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            FilterInfo copy = new FilterInfo(this);
            copy.IsGetCommandCopy = true;
            copy.Arguments = arguments;
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Filter; }
        }
    }
}
