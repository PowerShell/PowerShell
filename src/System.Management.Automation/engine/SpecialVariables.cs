// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    //
    // SpecialVariables contains the names and variable paths to any variable that PowerShell depends
    // on in some way, either in that it is an automatic variable (created and updated automatically)
    // or configuration variables that users may or may not set.
    //
    // The convention is to have a const string field with either the exact variable name, or if that's
    // not possible, a suggestive name, such as Underbar.  Having a field is preferred over explicit strings
    // to make searching easier.
    //
    // The other convention is to have a VariablePath field with "VarPath" appended to the string
    // field name.  In general, it is preferred to use the VariablePath instead of the string
    // because we'll end up creating a VariablePath anyway, so doing it once is faster.
    //
    internal static class SpecialVariables
    {
        internal const string HistorySize = "MaximumHistoryCount";

        internal static readonly VariablePath HistorySizeVarPath = new VariablePath(HistorySize);

        internal const string MyInvocation = "MyInvocation";

        internal static readonly VariablePath MyInvocationVarPath = new VariablePath(MyInvocation);

        internal const string OFS = "OFS";

        internal static readonly VariablePath OFSVarPath = new VariablePath(OFS);

        internal const string PSStyle = "PSStyle";

        internal static readonly VariablePath PSStyleVarPath = new VariablePath(PSStyle);

        internal const string OutputEncoding = "OutputEncoding";

        internal static readonly VariablePath OutputEncodingVarPath = new VariablePath(OutputEncoding);

        internal const string VerboseHelpErrors = "VerboseHelpErrors";

        internal static readonly VariablePath VerboseHelpErrorsVarPath = new VariablePath(VerboseHelpErrors);

        #region Logging Variables

        internal const string LogEngineHealthEvent = "LogEngineHealthEvent";

        internal static readonly VariablePath LogEngineHealthEventVarPath = new VariablePath(LogEngineHealthEvent);

        internal const string LogEngineLifecycleEvent = "LogEngineLifecycleEvent";

        internal static readonly VariablePath LogEngineLifecycleEventVarPath = new VariablePath(LogEngineLifecycleEvent);

        internal const string LogCommandHealthEvent = "LogCommandHealthEvent";

        internal static readonly VariablePath LogCommandHealthEventVarPath = new VariablePath(LogCommandHealthEvent);

        internal const string LogCommandLifecycleEvent = "LogCommandLifecycleEvent";

        internal static readonly VariablePath LogCommandLifecycleEventVarPath = new VariablePath(LogCommandLifecycleEvent);

        internal const string LogProviderHealthEvent = "LogProviderHealthEvent";

        internal static readonly VariablePath LogProviderHealthEventVarPath = new VariablePath(LogProviderHealthEvent);

        internal const string LogProviderLifecycleEvent = "LogProviderLifecycleEvent";

        internal static readonly VariablePath LogProviderLifecycleEventVarPath = new VariablePath(LogProviderLifecycleEvent);

        internal const string LogSettingsEvent = "LogSettingsEvent";

        internal static readonly VariablePath LogSettingsEventVarPath = new VariablePath(LogSettingsEvent);

        internal const string PSLogUserData = "PSLogUserData";

        internal static readonly VariablePath PSLogUserDataPath = new VariablePath(PSLogUserData);

        #endregion Logging Variables

        internal const string NestedPromptLevel = "NestedPromptLevel";

        internal static readonly VariablePath NestedPromptCounterVarPath = new VariablePath("global:" + NestedPromptLevel);

        internal const string CurrentlyExecutingCommand = "CurrentlyExecutingCommand";

        internal static readonly VariablePath CurrentlyExecutingCommandVarPath = new VariablePath(CurrentlyExecutingCommand);

        internal const string PSBoundParameters = "PSBoundParameters";

        internal static readonly VariablePath PSBoundParametersVarPath = new VariablePath(PSBoundParameters);

        internal const string Matches = "Matches";

        internal static readonly VariablePath MatchesVarPath = new VariablePath(Matches);

        internal const string LastExitCode = "LASTEXITCODE";

        internal static readonly VariablePath LastExitCodeVarPath = new VariablePath("global:" + LastExitCode);

        internal const string PSDebugContext = "PSDebugContext";

        internal static readonly VariablePath PSDebugContextVarPath = new VariablePath(PSDebugContext);

        internal const string StackTrace = "StackTrace";

        internal static readonly VariablePath StackTraceVarPath = new VariablePath("global:" + StackTrace);

        internal const string FirstToken = "^";

        internal static readonly VariablePath FirstTokenVarPath = new VariablePath("global:" + FirstToken);

        internal const string LastToken = "$";

        internal static readonly VariablePath LastTokenVarPath = new VariablePath("global:" + LastToken);

        internal static bool IsUnderbar(string name) { return name.Length == 1 && name[0] == '_'; }

        internal const string PSItem = "PSItem";  // simple alias for $_
        internal const string Underbar = "_";

        internal static readonly VariablePath UnderbarVarPath = new VariablePath(Underbar);

        internal const string Question = "?";

        internal static readonly VariablePath QuestionVarPath = new VariablePath(Question);

        internal const string Args = "args";

        internal static readonly VariablePath ArgsVarPath = new VariablePath("local:" + Args);

        internal const string This = "this";

        internal static readonly VariablePath ThisVarPath = new VariablePath("this");

        internal const string Input = "input";

        internal static readonly VariablePath InputVarPath = new VariablePath("local:" + Input);

        internal const string PSCmdlet = "PSCmdlet";

        internal static readonly VariablePath PSCmdletVarPath = new VariablePath("PSCmdlet");

        internal const string Error = "error";

        internal static readonly VariablePath ErrorVarPath = new VariablePath("global:" + Error);

        internal const string EventError = "error";

        internal static readonly VariablePath EventErrorVarPath = new VariablePath("script:" + EventError);
#if !UNIX
        internal const string PathExt = "env:PATHEXT";

        internal static readonly VariablePath PathExtVarPath = new VariablePath(PathExt);
#endif
        internal const string PSEmailServer = "PSEmailServer";

        internal static readonly VariablePath PSEmailServerVarPath = new VariablePath(PSEmailServer);

        internal const string PSDefaultParameterValues = "PSDefaultParameterValues";

        internal static readonly VariablePath PSDefaultParameterValuesVarPath = new VariablePath(PSDefaultParameterValues);

        internal const string PSScriptRoot = "PSScriptRoot";

        internal static readonly VariablePath PSScriptRootVarPath = new VariablePath(PSScriptRoot);

        internal const string PSCommandPath = "PSCommandPath";

        internal static readonly VariablePath PSCommandPathVarPath = new VariablePath(PSCommandPath);

        internal const string PSSenderInfo = "PSSenderInfo";

        internal static readonly VariablePath PSSenderInfoVarPath = new VariablePath(PSSenderInfo);

        internal const string @foreach = "foreach";

        internal static readonly VariablePath foreachVarPath = new VariablePath("local:" + @foreach);

        internal const string @switch = "switch";

        internal static readonly VariablePath switchVarPath = new VariablePath("local:" + @switch);

        internal const string pwd = "PWD";

        internal static readonly VariablePath PWDVarPath = new VariablePath("global:" + pwd);

        internal const string Null = "null";

        internal static readonly VariablePath NullVarPath = new VariablePath("null");

        internal const string True = "true";

        internal static readonly VariablePath TrueVarPath = new VariablePath("true");

        internal const string False = "false";

        internal static readonly VariablePath FalseVarPath = new VariablePath("false");

        internal const string PSModuleAutoLoading = "PSModuleAutoLoadingPreference";

        internal static readonly VariablePath PSModuleAutoLoadingPreferenceVarPath = new VariablePath("global:" + PSModuleAutoLoading);

        #region Platform Variables

        internal const string IsLinux = "IsLinux";

        internal static readonly VariablePath IsLinuxPath = new VariablePath("IsLinux");

        internal const string IsMacOS = "IsMacOS";

        internal static readonly VariablePath IsMacOSPath = new VariablePath("IsMacOS");

        internal const string IsWindows = "IsWindows";

        internal static readonly VariablePath IsWindowsPath = new VariablePath("IsWindows");

        internal const string IsCoreCLR = "IsCoreCLR";

        internal static readonly VariablePath IsCoreCLRPath = new VariablePath("IsCoreCLR");

        #endregion

        #region Preference Variables

        internal const string DebugPreference = "DebugPreference";

        internal static readonly VariablePath DebugPreferenceVarPath = new VariablePath(DebugPreference);

        internal const string ErrorActionPreference = "ErrorActionPreference";

        internal static readonly VariablePath ErrorActionPreferenceVarPath = new VariablePath(ErrorActionPreference);

        internal const string ProgressPreference = "ProgressPreference";

        internal static readonly VariablePath ProgressPreferenceVarPath = new VariablePath(ProgressPreference);

        internal const string VerbosePreference = "VerbosePreference";

        internal static readonly VariablePath VerbosePreferenceVarPath = new VariablePath(VerbosePreference);

        internal const string WarningPreference = "WarningPreference";

        internal static readonly VariablePath WarningPreferenceVarPath = new VariablePath(WarningPreference);

        internal const string WhatIfPreference = "WhatIfPreference";

        internal static readonly VariablePath WhatIfPreferenceVarPath = new VariablePath(WhatIfPreference);

        internal const string ConfirmPreference = "ConfirmPreference";

        internal static readonly VariablePath ConfirmPreferenceVarPath = new VariablePath(ConfirmPreference);

        internal const string InformationPreference = "InformationPreference";

        internal static readonly VariablePath InformationPreferenceVarPath = new VariablePath(InformationPreference);

        #endregion Preference Variables

        internal const string PSNativeCommandUseErrorActionPreference = nameof(PSNativeCommandUseErrorActionPreference);

        internal static readonly VariablePath PSNativeCommandUseErrorActionPreferenceVarPath =
            new(PSNativeCommandUseErrorActionPreference);

        // Native command argument passing style
        internal const string NativeArgumentPassing = "PSNativeCommandArgumentPassing";

        internal static readonly VariablePath NativeArgumentPassingVarPath = new VariablePath(NativeArgumentPassing);

        internal const string ErrorView = "ErrorView";

        internal static readonly VariablePath ErrorViewVarPath = new VariablePath(ErrorView);

        /// <summary>
        /// Shell environment variable.
        /// </summary>
        internal const string PSSessionConfigurationName = "PSSessionConfigurationName";

        internal static readonly VariablePath PSSessionConfigurationNameVarPath = new VariablePath("global:" + PSSessionConfigurationName);

        /// <summary>
        /// Environment variable that will define the default
        /// application name for the connection uri.
        /// </summary>
        internal const string PSSessionApplicationName = "PSSessionApplicationName";

        internal static readonly VariablePath PSSessionApplicationNameVarPath = new VariablePath("global:" + PSSessionApplicationName);

        #region AllScope variables created in every session

        internal const string ExecutionContext = "ExecutionContext";
        internal const string Home = "HOME";
        internal const string Host = "Host";
        internal const string PID = "PID";
        internal const string PSCulture = "PSCulture";
        internal const string PSHome = "PSHOME";
        internal const string PSUICulture = "PSUICulture";
        internal const string PSVersionTable = "PSVersionTable";
        internal const string PSEdition = "PSEdition";
        internal const string ShellId = "ShellId";
        internal const string EnabledExperimentalFeatures = "EnabledExperimentalFeatures";

        #endregion AllScope variables created in every session

        internal static readonly string[] AutomaticVariables = {
                                                                   SpecialVariables.Underbar,
                                                                   SpecialVariables.Args,
                                                                   SpecialVariables.This,
                                                                   SpecialVariables.Input,
                                                                   SpecialVariables.PSCmdlet,
                                                                   SpecialVariables.PSBoundParameters,
                                                                   SpecialVariables.MyInvocation,
                                                                   SpecialVariables.PSScriptRoot,
                                                                   SpecialVariables.PSCommandPath,
                                                               };

        internal static readonly Type[] AutomaticVariableTypes = {
                                                                   /* Underbar */          typeof(object),
                                                                   /* Args */              typeof(object[]),
                                                                   /* This */              typeof(object),
                                                                   /* Input */             typeof(object),
                                                                   /* PSCmdlet */          typeof(PSScriptCmdlet),
                                                                   /* PSBoundParameters */ typeof(PSBoundParametersDictionary),
                                                                   /* MyInvocation */      typeof(InvocationInfo),
                                                                   /* PSScriptRoot */      typeof(string),
                                                                   /* PSCommandPath */     typeof(string),
                                                                 };

        // This array and the one below it exist to optimize the way common parameters work in advanced functions.
        // Common parameters work by setting preference variables in the scope of the function and restoring the old value afterward.
        // Variables that don't correspond to common cmdlet parameters don't need to be added here.
        internal static readonly string[] PreferenceVariables =
        {
            SpecialVariables.DebugPreference,
            SpecialVariables.VerbosePreference,
            SpecialVariables.ErrorActionPreference,
            SpecialVariables.WhatIfPreference,
            SpecialVariables.WarningPreference,
            SpecialVariables.InformationPreference,
            SpecialVariables.ConfirmPreference,
        };

        internal static readonly Type[] PreferenceVariableTypes =
        {
            /* DebugPreference */                         typeof(ActionPreference),
            /* VerbosePreference */                       typeof(ActionPreference),
            /* ErrorPreference */                         typeof(ActionPreference),
            /* WhatIfPreference */                        typeof(SwitchParameter),
            /* WarningPreference */                       typeof(ActionPreference),
            /* InformationPreference */                   typeof(ActionPreference),
            /* ConfirmPreference */                       typeof(ConfirmImpact),
        };

        // The following variables are created in every session w/ AllScope.  We avoid creating local slots when we
        // see an assignment to any of these variables so that they get handled properly (either throwing an exception
        // because they are constant/readonly, or having the value persist in parent scopes where the allscope variable
        // also exists.
        internal static readonly Dictionary<string, Type> AllScopeVariables = new(StringComparer.OrdinalIgnoreCase) {
            { Question, typeof(bool) },
            { ExecutionContext, typeof(EngineIntrinsics) },
            { False, typeof(bool) },
            { Home, typeof(string) },
            { Host, typeof(object) },
            { PID, typeof(int) },
            { PSCulture, typeof(string) },
            { PSHome, typeof(string) },
            { PSUICulture, typeof(string) },
            { PSVersionTable, typeof(PSVersionHashTable) },
            { PSEdition, typeof(string) },
            { ShellId, typeof(string) },
            { True, typeof(bool) },
            { EnabledExperimentalFeatures, typeof(ReadOnlyBag<string>) }
        };

        private static readonly HashSet<string> s_classMethodsAccessibleVariables = new HashSet<string>
            (
                new string[]
                {
                    SpecialVariables.LastExitCode,
                    SpecialVariables.Error,
                    SpecialVariables.StackTrace,
                    SpecialVariables.OutputEncoding,
                    SpecialVariables.NestedPromptLevel,
                    SpecialVariables.pwd,
                    SpecialVariables.Matches,
                },
                StringComparer.OrdinalIgnoreCase
            );

        internal static bool IsImplicitVariableAccessibleInClassMethod(VariablePath variablePath)
        {
            return s_classMethodsAccessibleVariables.Contains(variablePath.UserPath);
        }
    }

    internal enum AutomaticVariable
    {
        Underbar = 0,
        Args = 1,
        This = 2,
        Input = 3,
        PSCmdlet = 4,
        PSBoundParameters = 5,
        MyInvocation = 6,
        PSScriptRoot = 7,
        PSCommandPath = 8,
        NumberOfAutomaticVariables // 1 + the last, used to initialize global scope.
    }

    internal enum PreferenceVariable
    {
        Debug = 9,
        Verbose = 10,
        Error = 11,
        WhatIf = 12,
        Warning = 13,
        Information = 14,
        Confirm = 15,
        Progress = 16,
    }
}
