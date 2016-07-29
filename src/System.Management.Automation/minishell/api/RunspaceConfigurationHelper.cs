/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// RunspaceConfigurationHelper define some constants to be used by 
    /// both Minishell api and makekit.
    /// 
    /// Be very careful when trying to change values for strings below since
    /// it will be used by makekit process also.
    /// 
    /// This file will be built with both monad engine and makekit.
    /// </summary>
    internal static class RunspaceConfigurationHelper
    {
        internal const string IntrinsicTypeResourceName = "intrinsicTypes";
        internal const string BuiltInTypeResourceName = "builtInTypes";
        internal const string IntrinsicFormatResourceName = "intrinsicFormats";
        internal const string BuiltInFormatResourceName = "builtInFormats";
        internal const string ScriptResourceName = "script";
        internal const string ProfileResourceName = "initialization";
        internal const string HelpResourceName = "help";
        internal const string ShellHelpResourceKey = "ShellHelp";
        internal const string ShellBannerResourceKey = "ShellBanner";
        internal const string ResourceListKey = "__resourceList__";
    }
}
