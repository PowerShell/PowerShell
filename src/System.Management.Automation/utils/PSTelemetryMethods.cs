// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if LEGACYTELEMETRY

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PowerShell.Telemetry.Internal
{
    /// <summary>
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static class TelemetryAPI
    {
        #region Public API

        /// <summary>
        /// Public API to expose Telemetry in PowerShell
        /// Provide meaningful message. Ex: PSCONSOLE_START, PSRUNSPACE_START
        /// arguments are of anonymous type. Ex: new { PSVersion = "5.0", PSRemotingProtocolVersion = "2.2"}
        /// </summary>
        public static void TraceMessage<T>(string message, T arguments)
        {
            TelemetryWrapper.TraceMessage(message, arguments);
        }

        #endregion

        private static int s_anyPowerShellSessionOpen;
        private static DateTime s_sessionStartTime;

        private enum HostIsInteractive
        {
            Unknown,
            Interactive,
            NonInteractive
        }

        /// <summary>
        /// Called either after opening a runspace (the default), or by the host application.
        /// </summary>
        public static void ReportStartupTelemetry(IHostProvidesTelemetryData ihptd)
        {
            // Avoid reporting startup more than once, except if we report "exited" and
            // another runspace gets opened.
            if (Interlocked.CompareExchange(ref s_anyPowerShellSessionOpen, 1, 0) == 1)
                return;

            bool is32Bit = !Environment.Is64BitProcess;
            var psversion = PSVersionInfo.PSVersion.ToString();
            var hostName = Process.GetCurrentProcess().ProcessName;
            if (ihptd != null)
            {
                TelemetryWrapper.TraceMessage("PSHostStart", new
                {
                    Interactive = ihptd.HostIsInteractive ? HostIsInteractive.Interactive : HostIsInteractive.NonInteractive,
                    ProfileLoadTime = ihptd.ProfileLoadTimeInMS,
                    ReadyForInputTime = ihptd.ReadyForInputTimeInMS,
                    Is32Bit = is32Bit,
                    PSVersion = psversion,
                    ProcessName = hostName,
                });
            }
            else
            {
                TelemetryWrapper.TraceMessage("PSHostStart", new
                {
                    Interactive = HostIsInteractive.Unknown,
                    ProfileLoadTime = 0,
                    ReadyForInputTime = 0,
                    Is32Bit = is32Bit,
                    PSVersion = psversion,
                    ProcessName = hostName,
                });
            }

            s_sessionStartTime = DateTime.Now;
        }

        /// <summary>
        /// Called after there are no more open runspaces. In some host applications, this could
        /// report multiple exits.
        /// </summary>
        public static void ReportExitTelemetry(IHostProvidesTelemetryData ihptd)
        {
            TelemetryWrapper.TraceMessage("PSHostStop", new
            {
                InteractiveCommandCount = ihptd != null ? ihptd.InteractiveCommandCount : 0,
                TabCompletionTimes = s_tabCompletionTimes,
                TabCompletionCounts = s_tabCompletionCounts,
                TabCompletionResultCounts = s_tabCompletionResultCounts,
                SessionTime = (DateTime.Now - s_sessionStartTime).TotalMilliseconds
            });

            // In case a host opens another runspace, we will want another PSHostStart event,
            // so reset our flag here to allow that event to fire.
            s_anyPowerShellSessionOpen = 0;
        }

        /// <summary>
        /// Report Get-Help requests, how many results are returned, and how long it took.
        /// </summary>
        internal static void ReportGetHelpTelemetry(string name, int topicsFound, long timeInMS, bool updatedHelp)
        {
            TelemetryWrapper.TraceMessage("PSHelpRequest", new
            {
                TopicCount = topicsFound,
                TimeInMS = timeInMS,
                RanUpdateHelp = updatedHelp,
                HelpTopic = name,
            });
        }

        /// <summary>
        /// Report when Get-Command fails to find something.
        /// </summary>
        internal static void ReportGetCommandFailed(string[] name, long timeInMS)
        {
            TelemetryWrapper.TraceMessage("PSGetCommandFailed", new { TimeInMS = timeInMS, CommandNames = name });
        }

        private static long[] s_tabCompletionTimes = new long[(int)CompletionResultType.DynamicKeyword + 1];
        private static int[] s_tabCompletionCounts = new int[(int)CompletionResultType.DynamicKeyword + 1];
        private static int[] s_tabCompletionResultCounts = new int[(int)CompletionResultType.DynamicKeyword + 1];
        internal static void ReportTabCompletionTelemetry(long elapsedMilliseconds, int count, CompletionResultType completionResultType)
        {
            // We'll collect some general statistics.
            int idx = (int)completionResultType;
            if (idx >= 0 && idx <= (int)CompletionResultType.DynamicKeyword)
            {
                s_tabCompletionTimes[idx] += elapsedMilliseconds;
                s_tabCompletionCounts[idx]++;
                s_tabCompletionResultCounts[idx] += count;
            }

            // Also write an event for any slow tab completion (> 250ms).
            if (elapsedMilliseconds > 250)
            {
                TelemetryWrapper.TraceMessage("PSSlowTabCompletion", new
                {
                    Time = elapsedMilliseconds,
                    Count = count,
                    Type = completionResultType,
                });
            }
        }

        /// <summary>
        /// Report that a module was loaded, but only do so for modules that *might* be authored by Microsoft. We can't
        /// be 100% certain, but we'll ignore non-Microsoft module names when looking at any data, so it's best to
        /// at least attempt avoiding collecting data we'll ignore.
        /// </summary>
        internal static void ReportModuleLoad(PSModuleInfo foundModule)
        {
            var modulePath = foundModule.Path;
            var companyName = foundModule.CompanyName;
            bool couldBeMicrosoftModule =
                (modulePath != null &&
                 (modulePath.StartsWith(Utils.DefaultPowerShellAppBase, StringComparison.OrdinalIgnoreCase) ||
                  // The following covers both 64 and 32 bit Program Files by assuming 32bit is just ...\Program Files + " (x86)"
                  modulePath.StartsWith(Platform.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase))) ||
                (companyName != null &&
                 foundModule.CompanyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase));
            if (couldBeMicrosoftModule)
            {
                TelemetryWrapper.TraceMessage("PSImportModule", new
                {
                    ModuleName = foundModule.Name,
                    Version = foundModule.Version.ToString()
                });
            }
        }

        /// <summary>
        /// Report that a new local session (runspace) is created.
        /// </summary>
        internal static void ReportLocalSessionCreated(
            System.Management.Automation.Runspaces.InitialSessionState iss,
            System.Management.Automation.Host.TranscriptionData transcriptionData)
        {
            bool isConstrained = (iss != null) && (iss.DefaultCommandVisibility != SessionStateEntryVisibility.Public) && (iss.LanguageMode != PSLanguageMode.FullLanguage);
            bool isTranscripting = (transcriptionData != null) && (transcriptionData.SystemTranscript != null);

            TelemetryWrapper.TraceMessage("PSNewLocalSession", new
            {
                Constrained = isConstrained,
                Transcripting = isTranscripting
            });
        }

        private enum RemoteSessionType
        {
            Unknown,
            LocalProcess,
            WinRMRemote,
            HyperVRemote,
            ContainerRemote
        }

        private enum RemoteConfigurationType
        {
            Unknown,
            PSDefault,
            PSWorkflow,
            ServerManagerWorkflow,
            Custom
        }

        /// <summary>
        /// Report that a new remote session (runspace) is created.
        /// </summary>
        internal static void ReportRemoteSessionCreated(
            System.Management.Automation.Runspaces.RunspaceConnectionInfo connectionInfo)
        {
            RemoteSessionType sessionType = RemoteSessionType.Unknown;
            RemoteConfigurationType configurationType = RemoteConfigurationType.Unknown;
            if (connectionInfo is System.Management.Automation.Runspaces.NewProcessConnectionInfo)
            {
                sessionType = RemoteSessionType.LocalProcess;
                configurationType = RemoteConfigurationType.PSDefault;
            }
            else
            {
                System.Management.Automation.Runspaces.WSManConnectionInfo wsManConnectionInfo = connectionInfo as System.Management.Automation.Runspaces.WSManConnectionInfo;
                if (wsManConnectionInfo != null)
                {
                    sessionType = RemoteSessionType.WinRMRemote;

                    // Parse configuration name from ShellUri:
                    //  ShellUri = 'http://schemas.microsoft.com/powershell/Microsoft.PowerShell'
                    //  ConfigName = 'Microsoft.PowerShell'
                    string configurationName = wsManConnectionInfo.ShellUri;
                    if (!string.IsNullOrEmpty(configurationName))
                    {
                        int index = configurationName.LastIndexOf('/');
                        if (index > -1)
                        {
                            configurationName = configurationName.Substring(index + 1);
                        }
                    }

                    configurationType = GetConfigurationTypefromName(configurationName);
                }
                else
                {
                    System.Management.Automation.Runspaces.VMConnectionInfo vmConnectionInfo = connectionInfo as System.Management.Automation.Runspaces.VMConnectionInfo;
                    if (vmConnectionInfo != null)
                    {
                        sessionType = RemoteSessionType.HyperVRemote;
                        configurationType = GetConfigurationTypefromName(vmConnectionInfo.ConfigurationName);
                    }
                    else
                    {
                        System.Management.Automation.Runspaces.ContainerConnectionInfo containerConnectionInfo = connectionInfo as System.Management.Automation.Runspaces.ContainerConnectionInfo;
                        if (containerConnectionInfo != null)
                        {
                            sessionType = RemoteSessionType.ContainerRemote;
                            configurationType = GetConfigurationTypefromName(
                                (containerConnectionInfo.ContainerProc != null) ? containerConnectionInfo.ContainerProc.ConfigurationName : string.Empty);
                        }
                    }
                }
            }

            TelemetryWrapper.TraceMessage("PSNewRemoteSession", new
            {
                Type = sessionType,
                Configuration = configurationType
            });
        }

        private static RemoteConfigurationType GetConfigurationTypefromName(string name)
        {
            string configName = (name != null) ? name.Trim() : string.Empty;

            if (string.IsNullOrEmpty(configName) ||
                configName.Equals("microsoft.powershell", StringComparison.OrdinalIgnoreCase) ||
                configName.Equals("microsoft.powershell32", StringComparison.OrdinalIgnoreCase))
            {
                return RemoteConfigurationType.PSDefault;
            }
            else if (configName.Equals("microsoft.powershell.workflow", StringComparison.OrdinalIgnoreCase))
            {
                return RemoteConfigurationType.PSWorkflow;
            }
            else if (configName.Equals("microsoft.windows.servermanagerworkflows", StringComparison.OrdinalIgnoreCase))
            {
                return RemoteConfigurationType.ServerManagerWorkflow;
            }
            else
            {
                return RemoteConfigurationType.Custom;
            }
        }

        private enum ScriptFileType
        {
            None = 0,
            Ps1 = 1,
            Psd1 = 2,
            Psm1 = 3,
            Other = 4,
        }

        private static readonly int s_promptHashCode = "prompt".GetHashCode();

        /// <summary>
        /// Report some telemetry about the scripts that are run.
        /// </summary>
        internal static void ReportScriptTelemetry(Ast ast, bool dotSourced, long compileTimeInMS)
        {
            if (ast.Parent != null || !TelemetryWrapper.IsEnabled)
                return;

            Task.Run(() =>
            {
                var extent = ast.Extent;
                var text = extent.Text;
                var hash = text.GetHashCode();

                // Ignore 'prompt' so we don't generate an event for every 'prompt' that is invoked.
                // (We really should only create 'prompt' once, but we don't.
                if (hash == s_promptHashCode)
                    return;

                var visitor = new ScriptBlockTelemetry();

                ast.Visit(visitor);

                var scriptFileType = ScriptFileType.None;
                var fileName = extent.File;
                if (fileName != null)
                {
                    var ext = System.IO.Path.GetExtension(fileName);
                    if (".ps1".Equals(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        scriptFileType = ScriptFileType.Ps1;
                    }
                    else if (".psd1".Equals(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        scriptFileType = ScriptFileType.Psd1;
                    }
                    else if (".psm1".Equals(ext, StringComparison.OrdinalIgnoreCase))
                    {
                        scriptFileType = ScriptFileType.Psm1;
                    }
                    else
                    {
                        // Reachable?
                        scriptFileType = ScriptFileType.Other;
                    }
                }

                TelemetryWrapper.TraceMessage("PSScriptDetails", new
                {
                    Hash = hash,
                    IsDotSourced = dotSourced,
                    ScriptFileType = scriptFileType,
                    Length = text.Length,
                    LineCount = extent.EndLineNumber - extent.StartLineNumber,
                    CompileTimeInMS = compileTimeInMS,
                    StatementCount = visitor.StatementCount,
                    CountOfCommands = visitor.CountOfCommands,
                    CountOfDotSourcedCommands = visitor.CountOfDotSourcedCommands,
                    MaxArrayLength = visitor.MaxArraySize,
                    ArrayLiteralCount = visitor.ArrayLiteralCount,
                    ArrayLiteralCumulativeSize = visitor.ArrayLiteralCumulativeSize,
                    MaxStringLength = visitor.MaxStringSize,
                    StringLiteralCount = visitor.StringLiteralCount,
                    StringLiteralCumulativeSize = visitor.StringLiteralCumulativeSize,
                    MaxPipelineDepth = visitor.MaxPipelineDepth,
                    PipelineCount = visitor.PipelineCount,
                    FunctionCount = visitor.FunctionCount,
                    ScriptBlockCount = visitor.ScriptBlockCount,
                    ClassCount = visitor.ClassCount,
                    EnumCount = visitor.EnumCount,
                    CommandsCalled = visitor.CommandsCalled,
                });
            });
        }
    }

    internal class ScriptBlockTelemetry : AstVisitor2
    {
        internal ScriptBlockTelemetry()
        {
            CommandsCalled = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        internal Dictionary<string, int> CommandsCalled { get; private set; }

        internal int CountOfCommands { get; private set; }

        internal int CountOfDotSourcedCommands { get; private set; }

        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            CountOfCommands++;
            var commandName = commandAst.GetCommandName();
            if (commandName != null)
            {
                int commandCount;
                CommandsCalled.TryGetValue(commandName, out commandCount);
                CommandsCalled[commandName] = commandCount + 1;
            }

            if (commandAst.InvocationOperator == TokenKind.Dot)
                CountOfDotSourcedCommands++;

            return AstVisitAction.Continue;
        }

        internal int MaxStringSize { get; private set; }

        internal int StringLiteralCount { get; private set; }

        internal int StringLiteralCumulativeSize { get; private set; }

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            var stringSize = stringConstantExpressionAst.Value.Length;
            StringLiteralCount += 1;
            StringLiteralCumulativeSize += stringSize;
            MaxStringSize = Math.Max(MaxStringSize, stringSize);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
        {
            var stringSize = expandableStringExpressionAst.Value.Length;
            StringLiteralCount += 1;
            StringLiteralCumulativeSize += stringSize;
            MaxStringSize = Math.Max(MaxStringSize, stringSize);
            return AstVisitAction.Continue;
        }

        internal int MaxArraySize { get; private set; }

        internal int ArrayLiteralCount { get; private set; }

        internal int ArrayLiteralCumulativeSize { get; private set; }

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            var elementCount = arrayLiteralAst.Elements.Count;
            ArrayLiteralCount += 1;
            ArrayLiteralCumulativeSize += elementCount;
            MaxArraySize = Math.Max(MaxArraySize, elementCount);
            return AstVisitAction.Continue;
        }

        internal int StatementCount { get; private set; }

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst)
        {
            StatementCount += blockStatementAst.Body.Statements.Count;
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst)
        {
            StatementCount += namedBlockAst.Statements.Count;
            return AstVisitAction.Continue;
        }

        internal int FunctionCount { get; private set; }

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
        {
            FunctionCount += 1;
            return AstVisitAction.Continue;
        }

        internal int ScriptBlockCount { get; private set; }

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            ScriptBlockCount += 1;
            return AstVisitAction.Continue;
        }

        internal int MaxPipelineDepth { get; private set; }

        internal int PipelineCount { get; private set; }

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst)
        {
            MaxPipelineDepth = Math.Max(MaxPipelineDepth, pipelineAst.PipelineElements.Count);
            PipelineCount += 1;
            return AstVisitAction.Continue;
        }

        internal int ClassCount { get; private set; }

        internal int EnumCount { get; private set; }

        public override AstVisitAction VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
        {
            if (typeDefinitionAst.IsClass)
                ClassCount += 1;
            else if (typeDefinitionAst.IsEnum)
                EnumCount += 1;
            return AstVisitAction.Continue;
        }
    }

    /// <summary>
    /// If implemented by the host, the host should call <see cref="TelemetryAPI.ReportStartupTelemetry"/> and <see cref="TelemetryAPI.ReportExitTelemetry"/>
    /// and track the data defined by this interface.
    /// </summary>
    public interface IHostProvidesTelemetryData
    {
        /// <summary>A host sets this property as appropriate - used when reporting telemetry.</summary>
        bool HostIsInteractive { get; }

        /// <summary>A host sets this property as appropriate - used when reporting telemetry.</summary>
        double ProfileLoadTimeInMS { get; }

        /// <summary>A host sets this property as appropriate - used when reporting telemetry.</summary>
        double ReadyForInputTimeInMS { get; }

        /// <summary>A host sets this property as appropriate - used when reporting telemetry.</summary>
        int InteractiveCommandCount { get; }
    }
}
#endif
