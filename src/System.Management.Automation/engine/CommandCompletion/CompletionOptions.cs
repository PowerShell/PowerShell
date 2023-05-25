// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Command for getting the completion options.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CompletionOptions", HelpUri = "https://github.com/PowerShell/PowerShell")]
    public sealed class GetCompletionOptionsCommand : PSCmdlet
    {
        /// <summary>
        /// Writes the completion options to the pipeline.
        /// </summary>
        protected override void EndProcessing()
        {
            WriteObject(Context.CompletionOptions);
        }
    }

    /// <summary>
    /// Command for setting completion options.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "CompletionOptions", HelpUri = "https://github.com/PowerShell/PowerShell")]
    public sealed class SetCompletionOptionsCommand : PSCmdlet
    {
        /// <summary>
        /// Specifies if a separator character should automatically be added to container results.
        /// </summary>
        [Parameter()]
        public bool AddTrailingSeparatorForContainers { get; set; }

        /// <summary>
        /// Specifies how path separators are treated in the completion results.
        /// </summary>
        [Parameter()]
        public PathSeparator PreferredPathSeparator { get; set; }

        /// <summary>
        /// Specifies how paths should be sorted in the completion results.
        /// </summary>
        [Parameter()]
        public PathSorting PathSorting { get; set; }

        /// <summary>
        /// Specifies modules that should not be included in command completion results.
        /// </summary>
        [Parameter()]
        public string[] ExcludedModules { get; set; }

        /// <summary>
        /// Sets the user specified completion options.
        /// </summary>
        protected override void EndProcessing()
        {
            var completionOptions = Context.CompletionOptions;

            foreach (var Key in MyInvocation.BoundParameters.Keys)
            {
                switch (Key)
                {
                    case "AddTrailingSeparatorForContainers":
                        completionOptions.AddTrailingSeparatorForContainers = AddTrailingSeparatorForContainers;
                        break;

                    case "PreferredPathSeparator":
                        completionOptions.PreferredPathSeparator = PreferredPathSeparator;
                        break;

                    case "PathSorting":
                        completionOptions.PathSorting = PathSorting;
                        break;

                    case "ExcludedModules":
                        completionOptions.ExcludedModules = ExcludedModules;
                        break;

                    default:
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Specifies settings used when PowerShell is generating completion results.
    /// </summary>
    public sealed class CompletionOptions
    {
        /// <summary>
        /// Specifies if a separator character should automatically be added to container results.
        /// </summary>
        public bool AddTrailingSeparatorForContainers { get; internal set; }

        /// <summary>
        /// Specifies how path separators are treated in the completion results.
        /// </summary>
        public PathSeparator PreferredPathSeparator { get; internal set; }

        /// <summary>
        /// Specifies how paths should be sorted in the completion results.
        /// </summary>
        public PathSorting PathSorting { get; internal set; }

        /// <summary>
        /// Specifies modules that should not be included in command completion results.
        /// </summary>
        public string[] ExcludedModules { get; internal set; }

        internal CompletionOptions()
        {
            ResetToDefault();
        }

        internal void ResetToDefault()
        {
            AddTrailingSeparatorForContainers = false;
            PreferredPathSeparator = PathSeparator.Default;
            PathSorting = PathSorting.FullPath;
            ExcludedModules = null;
        }
    }

    /// <summary>
    /// Specifies how path separators are treated in the completion results.
    /// </summary>
    public enum PathSeparator
    {
        /// <summary>
        /// Always use the default provider separator.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Complete provider paths with the last separator in the input string.
        /// If no separator has been used, the default provider separator is used.
        /// If the provider doesn't support alternative separators, the supported separator will be used.
        /// </summary>
        LastUsed = 1,

        /// <summary>
        /// Always complete provider paths with "/", unless the provider explicitly don't support it.
        /// </summary>
        Slash = 2,

        /// <summary>
        /// Always complete provider paths with "\", unless the provider explicitly don't support it.
        /// </summary>
        Backslash = 3
    }

    /// <summary>
    /// Specifies how paths should be sorted in the completion results.
    /// </summary>
    public enum PathSorting
    {
        /// <summary>
        /// No sorting for file path completion results.
        /// </summary>
        None = 0,

        /// <summary>
        /// Sorts file path completion results alphabetically by the full path.
        /// </summary>
        FullPath = 1,

        /// <summary>
        /// Sorts file path completion results alphabetically by the full path, and lists containers first.
        /// </summary>
        ContainersFirst = 2
    }
}
