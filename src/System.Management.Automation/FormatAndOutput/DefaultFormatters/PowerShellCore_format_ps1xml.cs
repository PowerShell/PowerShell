// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class PowerShellCore_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var AvailableModules_GroupingFormat = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddText(FileSystemProviderStrings.DirectoryDisplayGrouping)
                            .AddScriptBlockExpressionBinding(@"Split-Path -Parent $_.Path | ForEach-Object { if([Version]::TryParse((Split-Path $_ -Leaf), [ref]$null)) { Split-Path -Parent $_} else {$_} } | Split-Path -Parent")
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                AvailableModules_GroupingFormat
            };

            yield return new ExtendedTypeDefinition(
                "System.RuntimeType",
                ViewsOf_System_RuntimeType());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.MemberDefinition",
                ViewsOf_Microsoft_PowerShell_Commands_MemberDefinition());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.GroupInfo",
                ViewsOf_Microsoft_PowerShell_Commands_GroupInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.GroupInfoNoElement",
                ViewsOf_Microsoft_PowerShell_Commands_GroupInfoNoElement());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.HistoryInfo",
                ViewsOf_Microsoft_PowerShell_Commands_HistoryInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.MatchInfo",
                ViewsOf_Microsoft_PowerShell_Commands_MatchInfo());

            yield return new ExtendedTypeDefinition(
                "Deserialized.Microsoft.PowerShell.Commands.MatchInfo",
                ViewsOf_Deserialized_Microsoft_PowerShell_Commands_MatchInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSVariable",
                ViewsOf_System_Management_Automation_PSVariable());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PathInfo",
                ViewsOf_System_Management_Automation_PathInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.CommandInfo",
                ViewsOf_System_Management_Automation_CommandInfo());

            var td10 = new ExtendedTypeDefinition(
                "System.Management.Automation.AliasInfo",
                ViewsOf_System_Management_Automation_AliasInfo_System_Management_Automation_ApplicationInfo_System_Management_Automation_CmdletInfo_System_Management_Automation_ExternalScriptInfo_System_Management_Automation_FilterInfo_System_Management_Automation_FunctionInfo_System_Management_Automation_ScriptInfo());
            td10.TypeNames.Add("System.Management.Automation.ApplicationInfo");
            td10.TypeNames.Add("System.Management.Automation.CmdletInfo");
            td10.TypeNames.Add("System.Management.Automation.ExternalScriptInfo");
            td10.TypeNames.Add("System.Management.Automation.FilterInfo");
            td10.TypeNames.Add("System.Management.Automation.FunctionInfo");
            td10.TypeNames.Add("System.Management.Automation.ScriptInfo");
            yield return td10;

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Runspaces.TypeData",
                ViewsOf_System_Management_Automation_Runspaces_TypeData());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.ControlPanelItem",
                ViewsOf_Microsoft_PowerShell_Commands_ControlPanelItem());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ApplicationInfo",
                ViewsOf_System_Management_Automation_ApplicationInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ScriptInfo",
                ViewsOf_System_Management_Automation_ScriptInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ExternalScriptInfo",
                ViewsOf_System_Management_Automation_ExternalScriptInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.FunctionInfo",
                ViewsOf_System_Management_Automation_FunctionInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.FilterInfo",
                ViewsOf_System_Management_Automation_FilterInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.AliasInfo",
                ViewsOf_System_Management_Automation_AliasInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.ListCommand+MemberInfo",
                ViewsOf_Microsoft_PowerShell_Commands_ListCommand_MemberInfo());

            var td20 = new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.ActiveDirectoryProvider+ADPSDriveInfo",
                ViewsOf_Microsoft_PowerShell_Commands_ActiveDirectoryProvider_ADPSDriveInfo_System_Management_Automation_PSDriveInfo());
            td20.TypeNames.Add("System.Management.Automation.PSDriveInfo");
            yield return td20;

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ProviderInfo",
                ViewsOf_System_Management_Automation_ProviderInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.CmdletInfo",
                ViewsOf_System_Management_Automation_CmdletInfo());

            var td23 = new ExtendedTypeDefinition(
                "System.Management.Automation.FilterInfo",
                ViewsOf_System_Management_Automation_FilterInfo_System_Management_Automation_FunctionInfo());
            td23.TypeNames.Add("System.Management.Automation.FunctionInfo");
            yield return td23;

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSDriveInfo",
                ViewsOf_System_Management_Automation_PSDriveInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Subsystem.SubsystemInfo",
                ViewsOf_System_Management_Automation_Subsystem_SubsystemInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Subsystem.SubsystemInfo+ImplementationInfo",
                ViewsOf_System_Management_Automation_Subsystem_SubsystemInfo_ImplementationInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ShellVariable",
                ViewsOf_System_Management_Automation_ShellVariable());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ScriptBlock",
                ViewsOf_System_Management_Automation_ScriptBlock());

            var extendedError = new ExtendedTypeDefinition(
                "System.Management.Automation.ErrorRecord#PSExtendedError",
                ViewsOf_System_Management_Automation_GetError());
            extendedError.TypeNames.Add("System.Exception#PSExtendedError");
            yield return extendedError;

            var errorRecord_Exception = new ExtendedTypeDefinition(
                "System.Management.Automation.ErrorRecord",
                ViewsOf_System_Management_Automation_ErrorRecord());
            yield return errorRecord_Exception;

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.WarningRecord",
                ViewsOf_System_Management_Automation_WarningRecord());

            yield return new ExtendedTypeDefinition(
                "Deserialized.System.Management.Automation.WarningRecord",
                ViewsOf_Deserialized_System_Management_Automation_WarningRecord());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.InformationRecord",
                ViewsOf_System_Management_Automation_InformationRecord());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.CommandParameterSetInfo",
                ViewsOf_System_Management_Automation_CommandParameterSetInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Runspaces.Runspace",
                ViewsOf_System_Management_Automation_Runspaces_Runspace());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Runspaces.PSSession",
                ViewsOf_System_Management_Automation_Runspaces_PSSession());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Job",
                ViewsOf_System_Management_Automation_Job());

            yield return new ExtendedTypeDefinition(
                "Deserialized.Microsoft.PowerShell.Commands.TextMeasureInfo",
                ViewsOf_Deserialized_Microsoft_PowerShell_Commands_TextMeasureInfo());

            yield return new ExtendedTypeDefinition(
                "Deserialized.Microsoft.PowerShell.Commands.GenericMeasureInfo",
                ViewsOf_Deserialized_Microsoft_PowerShell_Commands_GenericMeasureInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.CallStackFrame",
                ViewsOf_System_Management_Automation_CallStackFrame());

            var td40 = new ExtendedTypeDefinition(
                "System.Management.Automation.CommandBreakpoint",
                ViewsOf_BreakpointTypes());
            td40.TypeNames.Add("System.Management.Automation.LineBreakpoint");
            td40.TypeNames.Add("System.Management.Automation.VariableBreakpoint");
            yield return td40;

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration",
                ViewsOf_Microsoft_PowerShell_Commands_PSSessionConfigurationCommands_PSSessionConfiguration());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.ComputerChangeInfo",
                ViewsOf_Microsoft_PowerShell_Commands_ComputerChangeInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.RenameComputerChangeInfo",
                ViewsOf_Microsoft_PowerShell_Commands_RenameComputerChangeInfo());

            yield return new ExtendedTypeDefinition(
                "ModuleInfoGrouping",
                ViewsOf_ModuleInfoGrouping(sharedControls));

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSModuleInfo",
                ViewsOf_System_Management_Automation_PSModuleInfo());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ExperimentalFeature",
                ViewsOf_System_Management_Automation_ExperimentalFeature());

            var td46 = new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject",
                ViewsOf_Microsoft_PowerShell_Commands_BasicHtmlWebResponseObject());
            yield return td46;

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.WebResponseObject",
                ViewsOf_Microsoft_PowerShell_Commands_WebResponseObject());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.FileHashInfo",
                ViewsOf_Microsoft_Powershell_Utility_FileHashInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.PSRunspaceDebug",
                ViewsOf_Microsoft_PowerShell_Commands_PSRunspaceDebug());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.MarkdownRender.PSMarkdownOptionInfo",
                ViewsOf_Microsoft_PowerShell_MarkdownRender_MarkdownOptionInfo());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus",
                ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_PingStatus());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+PingMtuStatus",
                ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_PingMtuStatus());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+TraceStatus",
                ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_TraceStatus());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.ByteCollection",
                ViewsOf_Microsoft_PowerShell_Commands_ByteCollection());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle",
                ViewsOf_System_Management_Automation_PSStyle());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle+FormattingData",
                ViewsOf_System_Management_Automation_PSStyleFormattingData());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle+ProgressConfiguration",
                ViewsOf_System_Management_Automation_PSStyleProgressConfiguration());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle+FileInfoFormatting",
                ViewsOf_System_Management_Automation_PSStyleFileInfoFormat());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle+ForegroundColor",
                ViewsOf_System_Management_Automation_PSStyleForegroundColor());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSStyle+BackgroundColor",
                ViewsOf_System_Management_Automation_PSStyleBackgroundColor());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_RuntimeType()
        {
            yield return new FormatViewDefinition("System.RuntimeType",
                TableControl.Create()
                    .AddHeader(label: "IsPublic", width: 8)
                    .AddHeader(label: "IsSerial", width: 8)
                    .AddHeader(width: 40)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("IsPublic")
                        .AddPropertyColumn("IsSerializable")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("BaseType")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_MemberDefinition()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.MemberDefinition",
                TableControl.Create(autoSize: true)
                    .GroupByProperty("TypeName")
                    .AddHeader(label: "Name")
                    .AddHeader(label: "MemberType")
                    .AddHeader(label: "Definition")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("MemberType")
                        .AddPropertyColumn("Definition")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_GroupInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.GroupInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Count", width: 5)
                    .AddHeader(width: 25)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Count")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Group")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.GroupInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Count")
                        .AddItemProperty(@"Group")
                        .AddItemProperty(@"Values")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_GroupInfoNoElement()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.GroupInfoNoElement",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Count", width: 5)
                    .AddHeader(width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("Count")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.GroupInfoNoElement",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Count")
                        .AddItemProperty(@"Values")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_HistoryInfo()
        {
            yield return new FormatViewDefinition("history",
                TableControl.Create()
                    .AddHeader(Alignment.Right, width: 4)
                    .AddHeader(Alignment.Right, label: "Duration", width: 12)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Id")
                        .AddScriptBlockColumn(@"
                                if ($_.Duration.TotalHours -ge 10) {
                                    return ""{0}:{1:mm}:{1:ss}.{1:fff}"" -f [int]$_.Duration.TotalHours, $_.Duration
                                }
                                elseif ($_.Duration.TotalHours -ge 1) {
                                    $formatString = ""h\:mm\:ss\.fff""
                                }
                                elseif ($_.Duration.TotalMinutes -ge 1) {
                                    $formatString = ""m\:ss\.fff""
                                }
                                else {
                                    $formatString = ""s\.fff""
                                }

                                $_.Duration.ToString($formatString)
                              ")
                        .AddPropertyColumn("CommandLine")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("history",
                WideControl.Create()
                    .AddPropertyEntry("CommandLine")
                .EndWideControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_MatchInfo()
        {
            yield return new FormatViewDefinition("MatchInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_.ToEmphasizedString(((get-location).path))")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_Microsoft_PowerShell_Commands_MatchInfo()
        {
            yield return new FormatViewDefinition("MatchInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_.Line")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSVariable()
        {
            yield return new FormatViewDefinition("Variable",
                TableControl.Create()
                    .AddHeader(width: 30)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Value")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PathInfo()
        {
            yield return new FormatViewDefinition("PathInfo",
                TableControl.Create()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Path")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_CommandInfo()
        {
            yield return new FormatViewDefinition("CommandInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_AliasInfo_System_Management_Automation_ApplicationInfo_System_Management_Automation_CmdletInfo_System_Management_Automation_ExternalScriptInfo_System_Management_Automation_FilterInfo_System_Management_Automation_FunctionInfo_System_Management_Automation_ScriptInfo()
        {
            yield return new FormatViewDefinition("CommandInfo",
                TableControl.Create()
                    .AddHeader(label: "CommandType", width: 15)
                    .AddHeader(label: "Name", width: 50)
                    .AddHeader(label: "Version", width: 10)
                    .AddHeader(label: "Source")
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddScriptBlockColumn(@"
                                if ($_.CommandType -eq ""Alias"")
                                {
                                  $_.DisplayName
                                }
                                else
                                {
                                  $_.Name
                                }
                              ")
                        .AddPropertyColumn("Version")
                        .AddPropertyColumn("Source")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Runspaces_TypeData()
        {
            yield return new FormatViewDefinition("TypeData",
                TableControl.Create()
                    .AddHeader(label: "TypeName")
                    .AddHeader(label: "Members")
                    .StartRowDefinition()
                        .AddPropertyColumn("TypeName")
                        .AddPropertyColumn("Members")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ControlPanelItem()
        {
            yield return new FormatViewDefinition("ControlPanelItem",
                TableControl.Create()
                    .AddHeader(label: "Name")
                    .AddHeader(label: "CanonicalName")
                    .AddHeader(label: "Category")
                    .AddHeader(label: "Description")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("CanonicalName")
                        .AddPropertyColumn("Category")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ApplicationInfo()
        {
            yield return new FormatViewDefinition("ApplicationInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Path")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Management.Automation.ApplicationInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"CommandType")
                        .AddItemProperty(@"Definition")
                        .AddItemProperty(@"Extension")
                        .AddItemProperty(@"Path")
                        .AddItemProperty(@"FileVersionInfo")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ScriptInfo()
        {
            yield return new FormatViewDefinition("ScriptInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Definition")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Management.Automation.ScriptInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"CommandType")
                        .AddItemProperty(@"Definition")
                        .AddItemProperty(@"Path")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ExternalScriptInfo()
        {
            yield return new FormatViewDefinition("ExternalScriptInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Path")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_FunctionInfo()
        {
            yield return new FormatViewDefinition("FunctionInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Function")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_FilterInfo()
        {
            yield return new FormatViewDefinition("FilterInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Filter")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_AliasInfo()
        {
            yield return new FormatViewDefinition("AliasInfo",
                TableControl.Create()
                    .AddHeader(width: 15)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CommandType")
                        .AddPropertyColumn("DisplayName")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Management.Automation.AliasInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"DisplayName")
                        .AddItemProperty(@"CommandType")
                        .AddItemProperty(@"Definition")
                        .AddItemProperty(@"ReferencedCommand")
                        .AddItemProperty(@"ResolvedCommand")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ListCommand_MemberInfo()
        {
            yield return new FormatViewDefinition("memberinfo",
                TableControl.Create()
                    .AddHeader(label: "Class", width: 11)
                    .AddHeader(width: 25)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("MemberClass")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("MemberData")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.ListCommand+MemberInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"MemberClass")
                        .AddItemProperty(@"MemberData")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ActiveDirectoryProvider_ADPSDriveInfo_System_Management_Automation_PSDriveInfo()
        {
            yield return new FormatViewDefinition("drive",
                TableControl.Create()
                    .AddHeader(width: 10)
                    .AddHeader(label: "Used (GB)", width: 13)
                    .AddHeader(label: "Free (GB)", width: 13)
                    .AddHeader(label: "Provider", width: 13)
                    .AddHeader(label: "Root", width: 35)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddScriptBlockColumn(@"if($_.Used -or $_.Free) { ""{0:###0.00}"" -f ($_.Used / 1GB) }", alignment: Alignment.Right)
                        .AddScriptBlockColumn(@"if($_.Used -or $_.Free) { ""{0:###0.00}"" -f ($_.Free / 1GB) }", alignment: Alignment.Right)
                        .AddScriptBlockColumn("$_.Provider.Name")
                        .AddScriptBlockColumn("if($null -ne $_.DisplayRoot) { $_.DisplayRoot } else { $_.Root }")
                        .AddPropertyColumn("CurrentLocation", alignment: Alignment.Right)
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ProviderInfo()
        {
            yield return new FormatViewDefinition("provider",
                TableControl.Create()
                    .AddHeader(width: 20)
                    .AddHeader()
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Capabilities")
                        .AddPropertyColumn("Drives")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("provider",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Drives")
                        .AddItemProperty(@"Path")
                        .AddItemProperty(@"Home")
                        .AddItemProperty(@"Description")
                        .AddItemProperty(@"Capabilities")
                        .AddItemProperty(@"ImplementingType")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_CmdletInfo()
        {
            yield return new FormatViewDefinition("System.Management.Automation.CmdletInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"CommandType")
                        .AddItemProperty(@"Definition")
                        .AddItemProperty(@"Path")
                        .AddItemProperty(@"DLL")
                        .AddItemProperty(@"HelpFile")
                        .AddItemProperty(@"ParameterSets")
                        .AddItemProperty(@"ImplementingType")
                        .AddItemProperty(@"Verb")
                        .AddItemProperty(@"Noun")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_FilterInfo_System_Management_Automation_FunctionInfo()
        {
            yield return new FormatViewDefinition("System.Management.Automation.CommandInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"CommandType")
                        .AddItemProperty(@"Definition")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSDriveInfo()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSDriveInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Description")
                        .AddItemProperty(@"Provider")
                        .AddItemProperty(@"Root")
                        .AddItemProperty(@"CurrentLocation")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Subsystem_SubsystemInfo()
        {
            yield return new FormatViewDefinition(
                "System.Management.Automation.Subsystem.SubsystemInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Left, width: 17, label: "Kind")
                    .AddHeader(Alignment.Left, width: 18, label: "SubsystemType")
                    .AddHeader(Alignment.Right, width: 12, label: "IsRegistered")
                    .AddHeader(Alignment.Left, label: "Implementations")
                    .StartRowDefinition()
                        .AddPropertyColumn("Kind")
                        .AddScriptBlockColumn("$_.SubsystemType.Name")
                        .AddPropertyColumn("IsRegistered")
                        .AddPropertyColumn("Implementations")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Subsystem_SubsystemInfo_ImplementationInfo()
        {
            yield return new FormatViewDefinition(
                "System.Management.Automation.Subsystem.SubsystemInfo+ImplementationInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Id")
                        .AddItemProperty(@"Kind")
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Description")
                        .AddItemProperty(@"ImplementationType")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ShellVariable()
        {
            yield return new FormatViewDefinition("ShellVariable",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Value")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ScriptBlock()
        {
            yield return new FormatViewDefinition("ScriptBlock",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_")
                    .EndEntry()
                .EndControl());
        }

        // This generates a custom view for ErrorRecords and Exceptions making
        // specific nested types defined in $expandTypes visible.  It also handles
        // IEnumerable types.  Nested types are indented by 4 spaces.
        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_GetError()
        {
            yield return new FormatViewDefinition("GetErrorInstance",
                CustomControl.Create()
                    .GroupByProperty("PSErrorIndex", label: "ErrorIndex")
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"
                            Set-StrictMode -Off

                            $maxDepth = 10
                            $ellipsis = ""`u{2026}""
                            $resetColor = ''
                            $errorColor = ''
                            $accentColor = ''

                            if ($Host.UI.SupportsVirtualTerminal -and ([string]::IsNullOrEmpty($env:__SuppressAnsiEscapeSequences))) {
                                $resetColor = $PSStyle.Reset
                                $errorColor = $psstyle.Formatting.Error
                                $accentColor = $PSStyle.Formatting.FormatAccent
                            }

                            function Show-ErrorRecord($obj, [int]$indent = 0, [int]$depth = 1) {
                                $newline = [Environment]::Newline
                                $output = [System.Text.StringBuilder]::new()
                                $prefix = ' ' * $indent

                                $expandTypes = @(
                                    'Microsoft.Rest.HttpRequestMessageWrapper'
                                    'Microsoft.Rest.HttpResponseMessageWrapper'
                                    'System.Management.Automation.InvocationInfo'
                                )

                                # if object is an Exception, add an ExceptionType property
                                if ($obj -is [Exception]) {
                                    $obj | Add-Member -NotePropertyName Type -NotePropertyValue $obj.GetType().FullName -ErrorAction Ignore
                                }

                                # first find the longest property so we can indent properly
                                $propLength = 0
                                foreach ($prop in $obj.PSObject.Properties) {
                                    if ($prop.Value -ne $null -and $prop.Value -ne [string]::Empty -and $prop.Name.Length -gt $propLength) {
                                        $propLength = $prop.Name.Length
                                    }
                                }

                                $addedProperty = $false
                                foreach ($prop in $obj.PSObject.Properties) {

                                    # don't show empty properties or our added property for $error[index]
                                    if ($prop.Value -ne $null -and $prop.Value -ne [string]::Empty -and $prop.Value.count -gt 0 -and $prop.Name -ne 'PSErrorIndex') {
                                        $addedProperty = $true
                                        $null = $output.Append($prefix)
                                        $null = $output.Append($accentColor)
                                        $null = $output.Append($prop.Name)
                                        $propNameIndent = ' ' * ($propLength - $prop.Name.Length)
                                        $null = $output.Append($propNameIndent)
                                        $null = $output.Append(' : ')
                                        $null = $output.Append($resetColor)

                                        $newIndent = $indent + 4

                                        # only show nested objects that are Exceptions, ErrorRecords, or types defined in $expandTypes and types not in $ignoreTypes
                                        if ($prop.Value -is [Exception] -or $prop.Value -is [System.Management.Automation.ErrorRecord] -or
                                            $expandTypes -contains $prop.TypeNameOfValue -or ($prop.TypeNames -ne $null -and $expandTypes -contains $prop.TypeNames[0])) {

                                            if ($depth -ge $maxDepth) {
                                                $null = $output.Append($ellipsis)
                                            }
                                            else {
                                                $null = $output.Append($newline)
                                                $null = $output.Append((Show-ErrorRecord $prop.Value $newIndent ($depth + 1)))
                                            }
                                        }
                                        # `TargetSite` has many members that are not useful visually, so we have a reduced view of the relevant members
                                        elseif ($prop.Name -eq 'TargetSite' -and $prop.Value.GetType().Name -eq 'RuntimeMethodInfo') {
                                            if ($depth -ge $maxDepth) {
                                                $null = $output.Append($ellipsis)
                                            }
                                            else {
                                                $targetSite = [PSCustomObject]@{
                                                    Name = $prop.Value.Name
                                                    DeclaringType = $prop.Value.DeclaringType
                                                    MemberType = $prop.Value.MemberType
                                                    Module = $prop.Value.Module
                                                }

                                                $null = $output.Append($newline)
                                                $null = $output.Append((Show-ErrorRecord $targetSite $newIndent ($depth + 1)))
                                            }
                                        }
                                        # `StackTrace` is handled specifically because the lines are typically long but necessary so they are left justified without additional indentation
                                        elseif ($prop.Name -eq 'StackTrace') {
                                            # for a stacktrace which is usually quite wide with info, we left justify it
                                            $null = $output.Append($newline)
                                            $null = $output.Append($prop.Value)
                                        }
                                        # Dictionary and Hashtable we want to show as Key/Value pairs, we don't do the extra whitespace alignment here
                                        elseif ($prop.Value.GetType().Name.StartsWith('Dictionary') -or $prop.Value.GetType().Name -eq 'Hashtable') {
                                            $isFirstElement = $true
                                            foreach ($key in $prop.Value.Keys) {
                                                if ($isFirstElement) {
                                                    $null = $output.Append($newline)
                                                }

                                                if ($key -eq 'Authorization') {
                                                    $null = $output.Append(""${prefix}    ${accentColor}${key} : ${resetColor}${ellipsis}${newline}"")
                                                }
                                                else {
                                                    $null = $output.Append(""${prefix}    ${accentColor}${key} : ${resetColor}$($prop.Value[$key])${newline}"")
                                                }

                                                $isFirstElement = $false
                                            }
                                        }
                                        # if the object implements IEnumerable and not a string, we try to show each object
                                        # We ignore the `Data` property as it can contain lots of type information by the interpreter that isn't useful here
                                        elseif (!($prop.Value -is [System.String]) -and $prop.Value.GetType().GetInterface('IEnumerable') -ne $null -and $prop.Name -ne 'Data') {

                                            if ($depth -ge $maxDepth) {
                                                $null = $output.Append($ellipsis)
                                            }
                                            else {
                                                $isFirstElement = $true
                                                foreach ($value in $prop.Value) {
                                                    $null = $output.Append($newline)
                                                    if (!$isFirstElement) {
                                                        $null = $output.Append($newline)
                                                    }
                                                    $null = $output.Append((Show-ErrorRecord $value $newIndent ($depth + 1)))
                                                    $isFirstElement = $false
                                                }
                                            }
                                        }
                                        # Anything else, we convert to string.
                                        # ToString() can throw so we use LanguagePrimitives.TryConvertTo() to hide a convert error
                                        else {
                                            $value = $null
                                            if ([System.Management.Automation.LanguagePrimitives]::TryConvertTo($prop.Value, [string], [ref]$value) -and $value -ne $null)
                                            {
                                                if ($prop.Name -eq 'PositionMessage') {
                                                    $value = $value.Insert($value.IndexOf('~'), $errorColor)
                                                }
                                                elseif ($prop.Name -eq 'Message') {
                                                    $value = $errorColor + $value
                                                }

                                                $isFirstLine = $true
                                                if ($value.Contains($newline)) {
                                                    # the 3 is to account for ' : '
                                                    $valueIndent = ' ' * ($propLength + 3)
                                                    # need to trim any extra whitespace already in the text
                                                    foreach ($line in $value.Split($newline)) {
                                                        if (!$isFirstLine) {
                                                            $null = $output.Append(""${newline}${prefix}${valueIndent}"")
                                                        }
                                                        $null = $output.Append($line.Trim())
                                                        $isFirstLine = $false
                                                    }
                                                }
                                                else {
                                                    $null = $output.Append($value)
                                                }
                                            }
                                        }

                                        $null = $output.Append($newline)
                                    }
                                }

                                # if we had added nested properties, we need to remove the last newline
                                if ($addedProperty) {
                                    $null = $output.Remove($output.Length - $newline.Length, $newline.Length)
                                }

                                $output.ToString()
                            }

                            # Add back original typename and remove PSExtendedError
                            if ($_.PSObject.TypeNames.Contains('System.Management.Automation.ErrorRecord#PSExtendedError')) {
                                $_.PSObject.TypeNames.Add('System.Management.Automation.ErrorRecord')
                                $null = $_.PSObject.TypeNames.Remove('System.Management.Automation.ErrorRecord#PSExtendedError')
                            }
                            elseif ($_.PSObject.TypeNames.Contains('System.Exception#PSExtendedError')) {
                                $_.PSObject.TypeNames.Add('System.Exception')
                                $null = $_.PSObject.TypeNames.Remove('System.Exception#PSExtendedError')
                            }

                            Show-ErrorRecord $_
                        ")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ErrorRecord()
        {
            yield return new FormatViewDefinition("ErrorInstance",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"
                                    $errorColor = ''
                                    $commandPrefix = ''
                                    if (@('NativeCommandErrorMessage','NativeCommandError') -notcontains $_.FullyQualifiedErrorId -and @('CategoryView','ConciseView','DetailedView') -notcontains $ErrorView)
                                    {
                                        $myinv = $_.InvocationInfo
                                        if ($Host.UI.SupportsVirtualTerminal) {
                                            $errorColor = $PSStyle.Formatting.Error
                                        }

                                        $commandPrefix = if ($myinv -and $myinv.MyCommand) {
                                            switch -regex ( $myinv.MyCommand.CommandType )
                                            {
                                                ([System.Management.Automation.CommandTypes]::ExternalScript)
                                                {
                                                    if ($myinv.MyCommand.Path)
                                                    {
                                                        $myinv.MyCommand.Path + ' : '
                                                    }

                                                    break
                                                }

                                                ([System.Management.Automation.CommandTypes]::Script)
                                                {
                                                    if ($myinv.MyCommand.ScriptBlock)
                                                    {
                                                        $myinv.MyCommand.ScriptBlock.ToString() + ' : '
                                                    }

                                                    break
                                                }
                                                default
                                                {
                                                    if ($myinv.InvocationName -match '^[&\.]?$')
                                                    {
                                                        if ($myinv.MyCommand.Name)
                                                        {
                                                            $myinv.MyCommand.Name + ' : '
                                                        }
                                                    }
                                                    else
                                                    {
                                                        $myinv.InvocationName + ' : '
                                                    }

                                                    break
                                                }
                                            }
                                        }
                                        elseif ($myinv -and $myinv.InvocationName)
                                        {
                                            $myinv.InvocationName + ' : '
                                        }
                                    }

                                    $errorColor + $commandPrefix
                                ")
                        .AddScriptBlockExpressionBinding(@"
                                    Set-StrictMode -Off
                                    $newline = [Environment]::Newline

                                    $resetColor = ''
                                    $errorColor = ''
                                    $accentColor = ''

                                    if ($Host.UI.SupportsVirtualTerminal -and ([string]::IsNullOrEmpty($env:__SuppressAnsiEscapeSequences))) {
                                        $resetColor = $PSStyle.Reset
                                        $errorColor = $PSStyle.Formatting.Error
                                        $accentColor = $PSStyle.Formatting.ErrorAccent
                                    }

                                    function Get-ConciseViewPositionMessage {

                                        # returns a string cut to last whitespace
                                        function Get-TruncatedString($string, [int]$length) {

                                            if ($string.Length -le $length) {
                                                return $string
                                            }

                                            return ($string.Substring(0,$length) -split '\s',-2)[0]
                                        }

                                        $posmsg = ''
                                        $headerWhitespace = ''
                                        $offsetWhitespace = ''
                                        $message = ''
                                        $prefix = ''

                                        # Don't show line information if script module
                                        if (($myinv -and $myinv.ScriptName -or $myinv.ScriptLineNumber -gt 1 -or $err.CategoryInfo.Category -eq 'ParserError') -and !($myinv.ScriptName.EndsWith('.psm1', [System.StringComparison]::OrdinalIgnoreCase))) {
                                            $useTargetObject = $false

                                            # Handle case where there is a TargetObject and we can show the error at the target rather than the script source
                                            if ($_.TargetObject.Line -and $_.TargetObject.LineText) {
                                                $posmsg = ""${resetcolor}$($_.TargetObject.File)${newline}""
                                                $useTargetObject = $true
                                            }
                                            elseif ($myinv.ScriptName) {
                                                if ($env:TERM_PROGRAM -eq 'vscode') {
                                                    # If we are running in vscode, we know the file:line:col links are clickable so we use this format
                                                    $posmsg = ""${resetcolor}$($myinv.ScriptName):$($myinv.ScriptLineNumber):$($myinv.OffsetInLine)${newline}""
                                                }
                                                else {
                                                    $posmsg = ""${resetcolor}$($myinv.ScriptName):$($myinv.ScriptLineNumber)${newline}""
                                                }
                                            }
                                            else {
                                                $posmsg = ""${newline}""
                                            }

                                            if ($useTargetObject) {
                                                $scriptLineNumber = $_.TargetObject.Line
                                                $scriptLineNumberLength = $_.TargetObject.Line.ToString().Length
                                            }
                                            else {
                                                $scriptLineNumber = $myinv.ScriptLineNumber
                                                $scriptLineNumberLength = $myinv.ScriptLineNumber.ToString().Length
                                            }

                                            if ($scriptLineNumberLength -gt 4) {
                                                $headerWhitespace = ' ' * ($scriptLineNumberLength - 4)
                                            }

                                            $lineWhitespace = ''
                                            if ($scriptLineNumberLength -lt 4) {
                                                $lineWhitespace = ' ' * (4 - $scriptLineNumberLength)
                                            }

                                            $verticalBar = '|'
                                            $posmsg += ""${accentColor}${headerWhitespace}Line ${verticalBar}${newline}""

                                            $highlightLine = ''
                                            if ($useTargetObject) {
                                                $line = $_.TargetObject.LineText.Trim()
                                                $offsetLength = 0
                                                $offsetInLine = 0
                                            }
                                            else {
                                                $positionMessage = $myinv.PositionMessage.Split($newline)
                                                $line = $positionMessage[1].Substring(1) # skip the '+' at the start
                                                $highlightLine = $positionMessage[$positionMessage.Count - 1].Substring(1)
                                                $offsetLength = $highlightLine.Trim().Length
                                                $offsetInLine = $highlightLine.IndexOf('~')
                                            }

                                            if (-not $line.EndsWith($newline)) {
                                                $line += $newline
                                            }

                                            # don't color the whole line
                                            if ($offsetLength -lt $line.Length - 1) {
                                                $line = $line.Insert($offsetInLine + $offsetLength, $resetColor).Insert($offsetInLine, $accentColor)
                                            }

                                            $posmsg += ""${accentColor}${lineWhitespace}${ScriptLineNumber} ${verticalBar} ${resetcolor}${line}""
                                            $offsetWhitespace = ' ' * $offsetInLine
                                            $prefix = ""${accentColor}${headerWhitespace}     ${verticalBar} ${errorColor}""
                                            if ($highlightLine -ne '') {
                                                $posMsg += ""${prefix}${highlightLine}${newline}""
                                            }
                                            $message = ""${prefix}""
                                        }

                                        if (! $err.ErrorDetails -or ! $err.ErrorDetails.Message) {
                                            if ($err.CategoryInfo.Category -eq 'ParserError' -and $err.Exception.Message.Contains(""~$newline"")) {
                                                # need to parse out the relevant part of the pre-rendered positionmessage
                                                $message += $err.Exception.Message.split(""~$newline"")[1].split(""${newline}${newline}"")[0]
                                            }
                                            elseif ($err.Exception) {
                                                $message += $err.Exception.Message
                                            }
                                            elseif ($err.Message) {
                                                $message += $err.Message
                                            }
                                            else {
                                                $message += $err.ToString()
                                            }
                                        }
                                        else {
                                            $message += $err.ErrorDetails.Message
                                        }

                                        # if rendering line information, break up the message if it's wider than the console
                                        if ($myinv -and $myinv.ScriptName -or $err.CategoryInfo.Category -eq 'ParserError') {
                                            $prefixLength = [System.Management.Automation.Internal.StringDecorated]::new($prefix).ContentLength
                                            $prefixVtLength = $prefix.Length - $prefixLength

                                            # replace newlines in message so it lines up correct
                                            $message = $message.Replace($newline, ' ').Replace(""`n"", ' ').Replace(""`t"", ' ')

                                            $windowWidth = 120
                                            if ($Host.UI.RawUI -ne $null) {
                                                $windowWidth = $Host.UI.RawUI.WindowSize.Width
                                            }

                                            if ($windowWidth -gt 0 -and ($message.Length - $prefixVTLength) -gt $windowWidth) {
                                                $sb = [Text.StringBuilder]::new()
                                                $substring = Get-TruncatedString -string $message -length ($windowWidth + $prefixVTLength)
                                                $null = $sb.Append($substring)
                                                $remainingMessage = $message.Substring($substring.Length).Trim()
                                                $null = $sb.Append($newline)
                                                while (($remainingMessage.Length + $prefixLength) -gt $windowWidth) {
                                                    $subMessage = $prefix + $remainingMessage
                                                    $substring = Get-TruncatedString -string $subMessage -length ($windowWidth + $prefixVtLength)

                                                    if ($substring.Length - $prefix.Length -gt 0)
                                                    {
                                                        $null = $sb.Append($substring)
                                                        $null = $sb.Append($newline)
                                                        $remainingMessage = $remainingMessage.Substring($substring.Length - $prefix.Length).Trim()
                                                    }
                                                    else
                                                    {
                                                        break
                                                    }
                                                }
                                                $null = $sb.Append($prefix + $remainingMessage.Trim())
                                                $message = $sb.ToString()
                                            }

                                            $message += $newline
                                        }

                                        $posmsg += ""${errorColor}"" + $message

                                        $reason = 'Error'
                                        if ($err.Exception -and $err.Exception.WasThrownFromThrowStatement) {
                                            $reason = 'Exception'
                                        }
                                        # MyCommand can be the script block, so we don't want to show that so check if it's an actual command
                                        elseif ($myinv.MyCommand -and $myinv.MyCommand.Name -and (Get-Command -Name $myinv.MyCommand -ErrorAction Ignore))
                                        {
                                            $reason = $myinv.MyCommand
                                        }
                                        # If it's a scriptblock, better to show the command in the scriptblock that had the error
                                        elseif ($_.CategoryInfo.Activity) {
                                            $reason = $_.CategoryInfo.Activity
                                        }
                                        elseif ($myinv.MyCommand) {
                                            $reason = $myinv.MyCommand
                                        }
                                        elseif ($myinv.InvocationName) {
                                            $reason = $myinv.InvocationName
                                        }
                                        elseif ($err.CategoryInfo.Category) {
                                            $reason = $err.CategoryInfo.Category
                                        }
                                        elseif ($err.CategoryInfo.Reason) {
                                            $reason = $err.CategoryInfo.Reason
                                        }

                                        $errorMsg = 'Error'

                                        ""${errorColor}${reason}: ${posmsg}${resetcolor}""
                                    }

                                    $myinv = $_.InvocationInfo
                                    $err = $_
                                    if (!$myinv -and $_.ErrorRecord -and $_.ErrorRecord.InvocationInfo) {
                                        $err = $_.ErrorRecord
                                        $myinv = $err.InvocationInfo
                                    }

                                    if ($err.FullyQualifiedErrorId -eq 'NativeCommandErrorMessage' -or $err.FullyQualifiedErrorId -eq 'NativeCommandError') {
                                        return ""${errorColor}$($err.Exception.Message)${resetcolor}""
                                    }

                                    if ($ErrorView -eq 'DetailedView') {
                                        $message = Get-Error | Out-String
                                        return ""${errorColor}${message}${resetcolor}""
                                    }

                                    if ($ErrorView -eq 'CategoryView') {
                                        $message = $err.CategoryInfo.GetMessage()
                                        return ""${errorColor}${message}${resetcolor}""
                                    }

                                    $posmsg = ''
                                    if ($ErrorView -eq 'ConciseView') {
                                        $posmsg = Get-ConciseViewPositionMessage
                                    }
                                    elseif ($myinv -and ($myinv.MyCommand -or ($err.CategoryInfo.Category -ne 'ParserError'))) {
                                        $posmsg = $myinv.PositionMessage
                                        if ($posmsg -ne '') {
                                            $posmsg = $newline + $posmsg
                                        }
                                    }

                                    if ($err.PSMessageDetails) {
                                        $posmsg = ' : ' +  $err.PSMessageDetails + $posmsg
                                    }

                                    if ($ErrorView -eq 'ConciseView') {
                                        if ($err.PSMessageDetails) {
                                            $posmsg = ""${errorColor}${posmsg}""
                                        }
                                        return $posmsg
                                    }

                                    $indent = 4

                                    $errorCategoryMsg = $err.ErrorCategory_Message

                                    if ($null -ne $errorCategoryMsg)
                                    {
                                        $indentString = '+ CategoryInfo          : ' + $err.ErrorCategory_Message
                                    }
                                    else
                                    {
                                        $indentString = '+ CategoryInfo          : ' + $err.CategoryInfo
                                    }

                                    $posmsg += $newline + $indentString

                                    $indentString = ""+ FullyQualifiedErrorId : "" + $err.FullyQualifiedErrorId
                                    $posmsg += $newline + $indentString

                                    $originInfo = $err.OriginInfo

                                    if (($null -ne $originInfo) -and ($null -ne $originInfo.PSComputerName))
                                    {
                                        $indentString = ""+ PSComputerName        : "" + $originInfo.PSComputerName
                                        $posmsg += $newline + $indentString
                                    }

                                    $finalMsg = if ($err.ErrorDetails.Message) {
                                        $err.ErrorDetails.Message + $posmsg
                                    } else {
                                        $err.Exception.Message + $posmsg
                                    }

                                    ""${errorColor}${finalMsg}${resetcolor}""
                                ")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_WarningRecord()
        {
            yield return new FormatViewDefinition("WarningRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_System_Management_Automation_WarningRecord()
        {
            yield return new FormatViewDefinition("DeserializedWarningRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InformationalRecord_Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_InformationRecord()
        {
            yield return new FormatViewDefinition("InformationRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_.ToString()")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_CommandParameterSetInfo()
        {
            var FmtParameterAttributes = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 2)
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var FmtParameterInfo = CustomControl.Create()
                    .StartEntry()
                        .AddNewline()
                        .StartFrame(leftIndent: 2)
                            .AddText("Parameter Name: ")
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .StartFrame(leftIndent: 2)
                                .AddText("ParameterType = ")
                                .AddPropertyExpressionBinding(@"ParameterType")
                                .AddNewline()
                                .AddText("Position = ")
                                .AddPropertyExpressionBinding(@"Position")
                                .AddNewline()
                                .AddText("IsMandatory = ")
                                .AddPropertyExpressionBinding(@"IsMandatory")
                                .AddNewline()
                                .AddText("IsDynamic = ")
                                .AddPropertyExpressionBinding(@"IsDynamic")
                                .AddNewline()
                                .AddText("HelpMessage = ")
                                .AddPropertyExpressionBinding(@"HelpMessage")
                                .AddNewline()
                                .AddText("ValueFromPipeline = ")
                                .AddPropertyExpressionBinding(@"ValueFromPipeline")
                                .AddNewline()
                                .AddText("ValueFromPipelineByPropertyName = ")
                                .AddPropertyExpressionBinding(@"ValueFromPipelineByPropertyName")
                                .AddNewline()
                                .AddText("ValueFromRemainingArguments = ")
                                .AddPropertyExpressionBinding(@"ValueFromRemainingArguments")
                                .AddNewline()
                                .AddText("Aliases = ")
                                .AddPropertyExpressionBinding(@"Aliases")
                                .AddNewline()
                                .AddText("Attributes =")
                                .AddNewline()
                                .AddPropertyExpressionBinding(@"Attributes", enumerateCollection: true, customControl: FmtParameterAttributes)
                            .EndFrame()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("CommandParameterSetInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText("Parameter Set Name: ")
                        .AddPropertyExpressionBinding(@"Name")
                        .AddNewline()
                        .AddText("Is default parameter set: ")
                        .AddPropertyExpressionBinding(@"IsDefault")
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"Parameters", enumerateCollection: true, customControl: FmtParameterInfo)
                        .AddNewline()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Runspaces_Runspace()
        {
            yield return new FormatViewDefinition("Runspace",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Id", width: 3)
                    .AddHeader(Alignment.Left, label: "Name", width: 15)
                    .AddHeader(Alignment.Left, label: "ComputerName", width: 15)
                    .AddHeader(Alignment.Left, label: "Type", width: 13)
                    .AddHeader(Alignment.Left, label: "State", width: 13)
                    .AddHeader(Alignment.Left, label: "Availability", width: 15)
                    .StartRowDefinition()
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("Name")
                        .AddScriptBlockColumn(@"
                    if ($null -ne $_.ConnectionInfo)
                    {
                      $_.ConnectionInfo.ComputerName
                    }
                    else
                    {
                      ""localhost""
                    }
                  ")
                        .AddScriptBlockColumn(@"
                    if ($null -ne $_.ConnectionInfo)
                    {
                      ""Remote""
                    }
                    else
                    {
                      ""Local""
                    }
                  ")
                        .AddScriptBlockColumn("$_.RunspaceStateInfo.State")
                        .AddScriptBlockColumn(@"
                    if (($null -ne $_.Debugger) -and ($_.Debugger.InBreakpoint))
                    {
                        ""InBreakpoint""
                    }
                    else
                    {
                        $_.RunspaceAvailability
                    }
                  ")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Runspaces_PSSession()
        {
            yield return new FormatViewDefinition("PSSession",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Id", width: 3)
                    .AddHeader(Alignment.Left, label: "Name", width: 15)
                    .AddHeader(Alignment.Left, label: "Transport", width: 9)
                    .AddHeader(Alignment.Left, label: "ComputerName", width: 15)
                    .AddHeader(Alignment.Left, label: "ComputerType", width: 15)
                    .AddHeader(Alignment.Left, label: "State", width: 13)
                    .AddHeader(Alignment.Left, label: "ConfigurationName", width: 20)
                    .AddHeader(Alignment.Right, label: "Availability", width: 13)
                    .StartRowDefinition()
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Transport")
                        .AddPropertyColumn("ComputerName")
                        .AddPropertyColumn("ComputerType")
                        .AddPropertyColumn("State")
                        .AddPropertyColumn("ConfigurationName")
                        .AddPropertyColumn("Availability")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Job()
        {
            yield return new FormatViewDefinition("Job",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "Id", width: 6)
                    .AddHeader(Alignment.Left, label: "Name", width: 15)
                    .AddHeader(Alignment.Left, label: "PSJobTypeName", width: 15)
                    .AddHeader(Alignment.Left, label: "State", width: 13)
                    .AddHeader(Alignment.Left, label: "HasMoreData", width: 15)
                    .AddHeader(Alignment.Left, label: "Location", width: 20)
                    .AddHeader(Alignment.Left, label: "Command", width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("PSJobTypeName")
                        .AddPropertyColumn("State")
                        .AddPropertyColumn("HasMoreData")
                        .AddPropertyColumn("Location")
                        .AddPropertyColumn("Command")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_Microsoft_PowerShell_Commands_TextMeasureInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.TextMeasureInfo",
                TableControl.Create()
                    .AddHeader(label: "Lines")
                    .AddHeader(label: "Words")
                    .AddHeader(label: "Characters")
                    .AddHeader(label: "Property")
                    .StartRowDefinition()
                        .AddPropertyColumn("Lines")
                        .AddPropertyColumn("Words")
                        .AddPropertyColumn("Characters")
                        .AddPropertyColumn("Property")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_Microsoft_PowerShell_Commands_GenericMeasureInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.GenericMeasureInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Count")
                        .AddItemProperty(@"Average")
                        .AddItemProperty(@"Sum")
                        .AddItemProperty(@"Maximum")
                        .AddItemProperty(@"Minimum")
                        .AddItemProperty(@"Property")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_CallStackFrame()
        {
            yield return new FormatViewDefinition("CallStackFrame",
                TableControl.Create()
                    .AddHeader(label: "Command")
                    .AddHeader(label: "Arguments")
                    .AddHeader(label: "Location")
                    .StartRowDefinition()
                        .AddPropertyColumn("Command")
                        .AddPropertyColumn("Arguments")
                        .AddPropertyColumn("Location")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_BreakpointTypes()
        {
            yield return new FormatViewDefinition("Breakpoint",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "ID", width: 4)
                    .AddHeader(label: "Script")
                    .AddHeader(Alignment.Right, label: "Line", width: 4)
                    .AddHeader(label: "Command")
                    .AddHeader(label: "Variable")
                    .AddHeader(label: "Action")
                    .StartRowDefinition()
                        .AddPropertyColumn("ID")
                        .AddScriptBlockColumn("if ($_.Script) { [System.IO.Path]::GetFileName($_.Script) }")
                        .AddPropertyColumn("Line")
                        .AddPropertyColumn("Command")
                        .AddPropertyColumn("Variable")
                        .AddPropertyColumn("Action")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("Breakpoint",
                ListControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "System.Management.Automation.LineBreakpoint" })
                        .AddItemProperty(@"ID")
                        .AddItemProperty(@"Script")
                        .AddItemProperty(@"Line")
                        .AddItemProperty(@"Column")
                        .AddItemProperty(@"Enabled")
                        .AddItemProperty(@"HitCount")
                        .AddItemProperty(@"Action")
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "System.Management.Automation.VariableBreakpoint" })
                        .AddItemProperty(@"ID")
                        .AddItemProperty(@"Variable")
                        .AddItemProperty(@"AccessMode")
                        .AddItemProperty(@"Enabled")
                        .AddItemProperty(@"HitCount")
                        .AddItemProperty(@"Action")
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "System.Management.Automation.CommandBreakpoint" })
                        .AddItemProperty(@"ID")
                        .AddItemProperty(@"Command")
                        .AddItemProperty(@"Enabled")
                        .AddItemProperty(@"HitCount")
                        .AddItemProperty(@"Action")
                    .EndEntry()
                    .StartEntry()
                        .AddItemProperty(@"ID")
                        .AddItemProperty(@"Script")
                        .AddItemProperty(@"Enabled")
                        .AddItemProperty(@"HitCount")
                        .AddItemProperty(@"Action")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_PSSessionConfigurationCommands_PSSessionConfiguration()
        {
            yield return new FormatViewDefinition("PSSessionConfiguration",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"PSVersion")
                        .AddItemProperty(@"StartupScript")
                        .AddItemProperty(@"RunAsUser")
                        .AddItemProperty(@"Permission")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ComputerChangeInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.ComputerChangeInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "HasSucceeded", width: 12)
                    .AddHeader(label: "ComputerName", width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("HasSucceeded")
                        .AddPropertyColumn("ComputerName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_RenameComputerChangeInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.RenameComputerChangeInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "HasSucceeded", width: 12)
                    .AddHeader(label: "OldComputerName", width: 25)
                    .AddHeader(label: "NewComputerName", width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("HasSucceeded")
                        .AddPropertyColumn("OldComputerName")
                        .AddPropertyColumn("NewComputerName")
                    .EndRowDefinition()
                .EndTable());
        }

        private const string PreReleaseStringScriptBlock = @"
                            if ($_.PrivateData -and
                                $_.PrivateData.ContainsKey('PSData') -and
                                $_.PrivateData.PSData.ContainsKey('PreRelease'))
                            {
                                    $_.PrivateData.PSData.PreRelease
                            }";

        private static IEnumerable<FormatViewDefinition> ViewsOf_ModuleInfoGrouping(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("Module",
                TableControl.Create()
                    .GroupByScriptBlock("Split-Path -Parent $_.Path | ForEach-Object { if([Version]::TryParse((Split-Path $_ -Leaf), [ref]$null)) { Split-Path -Parent $_} else {$_} } | Split-Path -Parent", customControl: sharedControls[0])
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, label: "PreRelease", width: 10)
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Left, width: 9, label: "PSEdition")
                    .AddHeader(Alignment.Left, label: "ExportedCommands")
                    .StartRowDefinition()
                        .AddPropertyColumn("ModuleType")
                        .AddPropertyColumn("Version")
                        .AddScriptBlockColumn(PreReleaseStringScriptBlock)
                        .AddPropertyColumn("Name")
                        .AddScriptBlockColumn(@"
                            $result = [System.Collections.ArrayList]::new()
                            $editions = $_.CompatiblePSEditions
                            if (-not $editions)
                            {
                                $editions = @('Desktop')
                            }

                            foreach ($edition in $editions)
                            {
                                $result += $edition.Substring(0,4)
                            }

                            ($result | Sort-Object) -join ','")
                        .AddScriptBlockColumn("$_.ExportedCommands.Keys")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSModuleInfo()
        {
            yield return new FormatViewDefinition("Module",
                TableControl.Create()
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, label: "PreRelease", width: 10)
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Left, label: "ExportedCommands")
                    .StartRowDefinition()
                        .AddPropertyColumn("ModuleType")
                        .AddPropertyColumn("Version")
                        .AddScriptBlockColumn(PreReleaseStringScriptBlock)
                        .AddPropertyColumn("Name")
                        .AddScriptBlockColumn("$_.ExportedCommands.Keys")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("Module",
                WideControl.Create()
                    .AddPropertyEntry("Name")
                .EndWideControl());

            yield return new FormatViewDefinition("Module",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Path")
                        .AddItemProperty(@"Description")
                        .AddItemProperty(@"ModuleType")
                        .AddItemProperty(@"Version")
                        .AddItemScriptBlock(
                            PreReleaseStringScriptBlock,
                            label: "PreRelease")
                        .AddItemProperty(@"NestedModules")
                        .AddItemScriptBlock(@"$_.ExportedFunctions.Keys", label: "ExportedFunctions")
                        .AddItemScriptBlock(@"$_.ExportedCmdlets.Keys", label: "ExportedCmdlets")
                        .AddItemScriptBlock(@"$_.ExportedVariables.Keys", label: "ExportedVariables")
                        .AddItemScriptBlock(@"$_.ExportedAliases.Keys", label: "ExportedAliases")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ExperimentalFeature()
        {
            yield return new FormatViewDefinition("ExperimentalFeature",
                TableControl.Create()
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Right, width: 7)
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Left)
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Enabled")
                        .AddPropertyColumn("Source")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("ExperimentalFeature",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty("Name")
                        .AddItemProperty("Enabled")
                        .AddItemProperty("Source")
                        .AddItemProperty("Description")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_BasicHtmlWebResponseObject()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"StatusCode")
                        .AddItemProperty(@"StatusDescription")
                        .AddItemScriptBlock(@"
                                  $result = $_.Content
                                  $result = $result.Substring(0, [Math]::Min($result.Length, 200) )
                                  if($result.Length -eq 200) { $result += ""`u{2026}"" }

                                  $result
                                ", label: "Content")
                        .AddItemScriptBlock(@"
                                  $result = $_.RawContent
                                  $result = $result.Substring(0, [Math]::Min($result.Length, 200) )
                                  if($result.Length -eq 200) { $result += ""`u{2026}"" }

                                  $result
                                ", label: "RawContent")
                        .AddItemProperty(@"Headers")
                        .AddItemProperty(@"Images")
                        .AddItemProperty(@"InputFields")
                        .AddItemProperty(@"Links")
                        .AddItemProperty(@"RawContentLength")
                        .AddItemProperty(@"RelationLink")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_WebResponseObject()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.WebResponseObject",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"StatusCode")
                        .AddItemProperty(@"StatusDescription")
                        .AddItemProperty(@"Content")
                        .AddItemScriptBlock(@"
                                  $result = $_.RawContent
                                  $result = $result.Substring(0, [Math]::Min($result.Length, 200) )
                                  if($result.Length -eq 200) { $result += ""`u{2026}"" }

                                  $result
                                ", label: "RawContent")
                        .AddItemProperty(@"Headers")
                        .AddItemProperty(@"RawContentLength")
                        .AddItemProperty(@"RelationLink")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Powershell_Utility_FileHashInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.Commands.FileHashInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Left, width: 15)
                    .AddHeader(Alignment.Left, width: 70)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Algorithm")
                        .AddPropertyColumn("Hash")
                        .AddPropertyColumn("Path")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_PSRunspaceDebug()
        {
            yield return new FormatViewDefinition("PSRunspaceDebug>",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Id", width: 3)
                    .AddHeader(Alignment.Left, label: "Name", width: 20)
                    .AddHeader(Alignment.Left, label: "Enabled", width: 10)
                    .AddHeader(Alignment.Left, label: "BreakAll", width: 10)
                    .StartRowDefinition()
                        .AddPropertyColumn("RunspaceId")
                        .AddPropertyColumn("RunspaceName")
                        .AddPropertyColumn("Enabled")
                        .AddPropertyColumn("BreakAll")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_MarkdownRender_MarkdownOptionInfo()
        {
            yield return new FormatViewDefinition("Microsoft.PowerShell.MarkdownRender.PSMarkdownOptionInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header1')", label: "Header1")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header2')", label: "Header2")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header3')", label: "Header3")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header4')", label: "Header4")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header5')", label: "Header5")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Header6')", label: "Header6")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Code')", label: "Code")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Link')", label: "Link")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('Image')", label: "Image")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('EmphasisBold')", label: "EmphasisBold")
                        .AddItemScriptBlock(@"$_.AsEscapeSequence('EmphasisItalics')", label: "EmphasisItalics")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_PingStatus()
        {
            yield return new FormatViewDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Ping", width: 4)
                    .AddHeader(Alignment.Left, label: "Source", width: 16)
                    .AddHeader(Alignment.Left, label: "Address", width: 25)
                    .AddHeader(Alignment.Right, label: "Latency(ms)", width: 7)
                    .AddHeader(Alignment.Right, label: "BufferSize(B)", width: 10)
                    .AddHeader(Alignment.Left, label: "Status", width: 16)
                    .StartRowDefinition()
                        .AddPropertyColumn("Ping")
                        .AddPropertyColumn("Source")
                        .AddPropertyColumn("DisplayAddress")
                        .AddScriptBlockColumn(@"
                            if ($_.Status -eq 'TimedOut') {
                                '*'
                            }
                            else {
                                $_.Latency
                            }
                        ")
                        .AddScriptBlockColumn(@"
                            if ($_.Status -eq 'TimedOut') {
                                '*'
                            }
                            else {
                                $_.BufferSize
                            }
                        ")
                        .AddPropertyColumn("Status")
                    .EndRowDefinition()
                    .GroupByProperty("Destination")
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_PingMtuStatus()
        {
            yield return new FormatViewDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+PingMtuStatus",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "Source", width: 16)
                    .AddHeader(Alignment.Left, label: "Address", width: 25)
                    .AddHeader(Alignment.Right, label: "Latency(ms)", width: 7)
                    .AddHeader(Alignment.Left, label: "Status", width: 16)
                    .AddHeader(Alignment.Right, label: "MtuSize(B)", width: 7)
                    .StartRowDefinition()
                        .AddPropertyColumn("Source")
                        .AddPropertyColumn("DisplayAddress")
                        .AddScriptBlockColumn(@"
                            if ($_.Status -eq 'TimedOut') {
                                '*'
                            }
                            else {
                                $_.Latency
                            }
                        ")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("MtuSize")
                    .EndRowDefinition()
                    .GroupByProperty("Destination")
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_TestConnectionCommand_TraceStatus()
        {
            yield return new FormatViewDefinition(
                "Microsoft.PowerShell.Commands.TestConnectionCommand+TraceStatus",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Hop", width: 3)
                    .AddHeader(Alignment.Left, label: "Hostname", width: 25)
                    .AddHeader(Alignment.Right, label: "Ping", width: 4)
                    .AddHeader(Alignment.Right, label: "Latency(ms)", width: 7)
                    .AddHeader(Alignment.Left, label: "Status", width: 16)
                    .AddHeader(Alignment.Left, label: "Source", width: 12)
                    .AddHeader(Alignment.Left, label: "TargetAddress", width: 15)
                    .StartRowDefinition()
                        .AddPropertyColumn("Hop")
                        .AddScriptBlockColumn(@"
                            if ($_.Hostname) {
                                $_.HostName
                            }
                            else {
                                '*'
                            }
                        ")
                        .AddPropertyColumn("Ping")
                        .AddScriptBlockColumn(@"
                            if ($_.Status -eq 'TimedOut') {
                                '*'
                            }
                            else {
                                $_.Latency
                            }
                        ")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("Source")
                        .AddPropertyColumn("TargetAddress")
                    .EndRowDefinition()
                    .GroupByProperty("Target")
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ByteCollection()
        {
            yield return new FormatViewDefinition(
                "Microsoft.PowerShell.Commands.ByteCollection",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Offset", width: 16)
                    .AddHeader(Alignment.Left, label: "Bytes\n00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F", width: 47)
                    .AddHeader(Alignment.Left, label: "Ascii", width: 16)
                    .StartRowDefinition()
                        .AddPropertyColumn("HexOffset")
                        .AddPropertyColumn("HexBytes")
                        .AddPropertyColumn("Ascii")
                    .EndRowDefinition()
                    .GroupByProperty("Label")
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyle()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.Reset)$($_.Reset.Replace(""""`e"""",'`e'))""", label: "Reset")
                        .AddItemScriptBlock(@"""$($_.BlinkOff)$($_.BlinkOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "BlinkOff")
                        .AddItemScriptBlock(@"""$($_.Blink)$($_.Blink.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Blink")
                        .AddItemScriptBlock(@"""$($_.BoldOff)$($_.BoldOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "BoldOff")
                        .AddItemScriptBlock(@"""$($_.Bold)$($_.Bold.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Bold")
                        .AddItemScriptBlock(@"""$($_.DimOff)$($_.DimOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "DimOff")
                        .AddItemScriptBlock(@"""$($_.Dim)$($_.Dim.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Dim")
                        .AddItemScriptBlock(@"""$($_.Hidden)$($_.Hidden.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Hidden")
                        .AddItemScriptBlock(@"""$($_.HiddenOff)$($_.HiddenOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "HiddenOff")
                        .AddItemScriptBlock(@"""$($_.Reverse)$($_.Reverse.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Reverse")
                        .AddItemScriptBlock(@"""$($_.ReverseOff)$($_.ReverseOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "ReverseOff")
                        .AddItemScriptBlock(@"""$($_.ItalicOff)$($_.ItalicOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "ItalicOff")
                        .AddItemScriptBlock(@"""$($_.Italic)$($_.Italic.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Italic")
                        .AddItemScriptBlock(@"""$($_.UnderlineOff)$($_.UnderlineOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "UnderlineOff")
                        .AddItemScriptBlock(@"""$($_.Underline)$($_.Underline.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Underline")
                        .AddItemScriptBlock(@"""$($_.StrikethroughOff)$($_.StrikethroughOff.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "StrikethroughOff")
                        .AddItemScriptBlock(@"""$($_.Strikethrough)$($_.Strikethrough.Replace(""""`e"""",'`e'))$($_.Reset)""", label: "Strikethrough")
                        .AddItemProperty(@"OutputRendering")
                        .AddItemScriptBlock(@"""$($_.Formatting.FormatAccent)$($_.Formatting.FormatAccent.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.FormatAccent")
                        .AddItemScriptBlock(@"""$($_.Formatting.ErrorAccent)$($_.Formatting.ErrorAccent.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.ErrorAccent")
                        .AddItemScriptBlock(@"""$($_.Formatting.Error)$($_.Formatting.Error.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.Error")
                        .AddItemScriptBlock(@"""$($_.Formatting.Warning)$($_.Formatting.Warning.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.Warning")
                        .AddItemScriptBlock(@"""$($_.Formatting.Verbose)$($_.Formatting.Verbose.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.Verbose")
                        .AddItemScriptBlock(@"""$($_.Formatting.Debug)$($_.Formatting.Debug.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.Debug")
                        .AddItemScriptBlock(@"""$($_.Formatting.TableHeader)$($_.Formatting.TableHeader.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.TableHeader")
                        .AddItemScriptBlock(@"""$($_.Formatting.CustomTableHeaderLabel)$($_.Formatting.CustomTableHeaderLabel.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.CustomTableHeaderLabel")
                        .AddItemScriptBlock(@"""$($_.Formatting.FeedbackName)$($_.Formatting.FeedbackName.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.FeedbackName")
                        .AddItemScriptBlock(@"""$($_.Formatting.FeedbackText)$($_.Formatting.FeedbackText.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.FeedbackText")
                        .AddItemScriptBlock(@"""$($_.Formatting.FeedbackAction)$($_.Formatting.FeedbackAction.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Formatting.FeedbackAction")
                        .AddItemScriptBlock(@"""$($_.Progress.Style)$($_.Progress.Style.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Progress.Style")
                        .AddItemScriptBlock(@"""$($_.Progress.MaxWidth)""", label: "Progress.MaxWidth")
                        .AddItemScriptBlock(@"""$($_.Progress.View)""", label: "Progress.View")
                        .AddItemScriptBlock(@"""$($_.Progress.UseOSCIndicator)""", label: "Progress.UseOSCIndicator")
                        .AddItemScriptBlock(@"""$($_.FileInfo.Directory)$($_.FileInfo.Directory.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FileInfo.Directory")
                        .AddItemScriptBlock(@"""$($_.FileInfo.SymbolicLink)$($_.FileInfo.SymbolicLink.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FileInfo.SymbolicLink")
                        .AddItemScriptBlock(@"""$($_.FileInfo.Executable)$($_.FileInfo.Executable.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FileInfo.Executable")
                        .AddItemScriptBlock(@"""$([string]::Join(',',$_.FileInfo.Extension.Keys))""", label: "FileInfo.Extension")
                        .AddItemScriptBlock(@"""$($_.Foreground.Black)$($_.Foreground.Black.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Black")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightBlack)$($_.Foreground.BrightBlack.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightBlack")
                        .AddItemScriptBlock(@"""$($_.Foreground.White)$($_.Foreground.White.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.White")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightWhite)$($_.Foreground.BrightWhite.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightWhite")
                        .AddItemScriptBlock(@"""$($_.Foreground.Red)$($_.Foreground.Red.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Red")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightRed)$($_.Foreground.BrightRed.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightRed")
                        .AddItemScriptBlock(@"""$($_.Foreground.Magenta)$($_.Foreground.Magenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Magenta")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightMagenta)$($_.Foreground.BrightMagenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightMagenta")
                        .AddItemScriptBlock(@"""$($_.Foreground.Blue)$($_.Foreground.Blue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Blue")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightBlue)$($_.Foreground.BrightBlue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightBlue")
                        .AddItemScriptBlock(@"""$($_.Foreground.Cyan)$($_.Foreground.Cyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Cyan")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightCyan)$($_.Foreground.BrightCyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightCyan")
                        .AddItemScriptBlock(@"""$($_.Foreground.Green)$($_.Foreground.Green.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Green")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightGreen)$($_.Foreground.BrightGreen.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightGreen")
                        .AddItemScriptBlock(@"""$($_.Foreground.Yellow)$($_.Foreground.Yellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.Yellow")
                        .AddItemScriptBlock(@"""$($_.Foreground.BrightYellow)$($_.Foreground.BrightYellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Foreground.BrightYellow")
                        .AddItemScriptBlock(@"""$($_.Background.Black)$($_.Background.Black.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Black")
                        .AddItemScriptBlock(@"""$($_.Background.BrightBlack)$($_.Background.BrightBlack.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightBlack")
                        .AddItemScriptBlock(@"""$($_.Background.White)$($_.Background.White.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.White")
                        .AddItemScriptBlock(@"""$($_.Background.BrightWhite)$($_.Background.BrightWhite.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightWhite")
                        .AddItemScriptBlock(@"""$($_.Background.Red)$($_.Background.Red.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Red")
                        .AddItemScriptBlock(@"""$($_.Background.BrightRed)$($_.Background.BrightRed.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightRed")
                        .AddItemScriptBlock(@"""$($_.Background.Magenta)$($_.Background.Magenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Magenta")
                        .AddItemScriptBlock(@"""$($_.Background.BrightMagenta)$($_.Background.BrightMagenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightMagenta")
                        .AddItemScriptBlock(@"""$($_.Background.Blue)$($_.Background.Blue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Blue")
                        .AddItemScriptBlock(@"""$($_.Background.BrightBlue)$($_.Background.BrightBlue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightBlue")
                        .AddItemScriptBlock(@"""$($_.Background.Cyan)$($_.Background.Cyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Cyan")
                        .AddItemScriptBlock(@"""$($_.Background.BrightCyan)$($_.Background.BrightCyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightCyan")
                        .AddItemScriptBlock(@"""$($_.Background.Green)$($_.Background.Green.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Green")
                        .AddItemScriptBlock(@"""$($_.Background.BrightGreen)$($_.Background.BrightGreen.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightGreen")
                        .AddItemScriptBlock(@"""$($_.Background.Yellow)$($_.Background.Yellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.Yellow")
                        .AddItemScriptBlock(@"""$($_.Background.BrightYellow)$($_.Background.BrightYellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Background.BrightYellow")
                        .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyleFormattingData()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle+FormattingData",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.FormatAccent)$($_.FormatAccent.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FormatAccent")
                        .AddItemScriptBlock(@"""$($_.ErrorAccent)$($_.ErrorAccent.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "ErrorAccent")
                        .AddItemScriptBlock(@"""$($_.Error)$($_.Error.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Error")
                        .AddItemScriptBlock(@"""$($_.Warning)$($_.Warning.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Warning")
                        .AddItemScriptBlock(@"""$($_.Verbose)$($_.Verbose.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Verbose")
                        .AddItemScriptBlock(@"""$($_.Debug)$($_.Debug.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Debug")
                        .AddItemScriptBlock(@"""$($_.TableHeader)$($_.TableHeader.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "TableHeader")
                        .AddItemScriptBlock(@"""$($_.CustomTableHeaderLabel)$($_.CustomTableHeaderLabel.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "CustomTableHeaderLabel")
                        .AddItemScriptBlock(@"""$($_.FeedbackName)$($_.FeedbackName.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FeedbackName")
                        .AddItemScriptBlock(@"""$($_.FeedbackText)$($_.FeedbackText.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FeedbackText")
                        .AddItemScriptBlock(@"""$($_.FeedbackAction)$($_.FeedbackAction.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "FeedbackAction")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyleProgressConfiguration()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle+ProgressConfiguration",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.Style)$($_.Style.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Style")
                        .AddItemProperty(@"MaxWidth")
                        .AddItemProperty(@"View")
                        .AddItemProperty(@"UseOSCIndicator")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyleFileInfoFormat()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle+FileInfoFormatting",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.Directory)$($_.Directory.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Directory")
                        .AddItemScriptBlock(@"""$($_.SymbolicLink)$($_.SymbolicLink.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "SymbolicLink")
                        .AddItemScriptBlock(@"""$($_.Executable)$($_.Executable.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Executable")
                        .AddItemScriptBlock(@"
                            $sb = [System.Text.StringBuilder]::new()
                            $maxKeyLength = 0
                            foreach ($key in $_.Extension.Keys) {
                                if ($key.Length -gt $maxKeyLength) {
                                    $maxKeyLength = $key.Length
                                }
                            }

                            foreach ($key in $_.Extension.Keys) {
                                $null = $sb.Append($key.PadRight($maxKeyLength))
                                $null = $sb.Append(' = ""')
                                $null = $sb.Append($_.Extension[$key])
                                $null = $sb.Append($_.Extension[$key].Replace(""`e"",'`e'))
                                $null = $sb.Append($PSStyle.Reset)
                                $null = $sb.Append('""')
                                $null = $sb.Append([Environment]::NewLine)
                            }

                            $sb.ToString()",
                            label: "Extension")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyleForegroundColor()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle+ForegroundColor",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.Black)$($_.Black.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Black")
                        .AddItemScriptBlock(@"""$($_.BrightBlack)$($_.BrightBlack.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightBlack")
                        .AddItemScriptBlock(@"""$($_.White)$($_.White.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "White")
                        .AddItemScriptBlock(@"""$($_.BrightWhite)$($_.BrightWhite.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightWhite")
                        .AddItemScriptBlock(@"""$($_.Red)$($_.Red.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Red")
                        .AddItemScriptBlock(@"""$($_.BrightRed)$($_.BrightRed.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightRed")
                        .AddItemScriptBlock(@"""$($_.Magenta)$($_.Magenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Magenta")
                        .AddItemScriptBlock(@"""$($_.BrightMagenta)$($_.BrightMagenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightMagenta")
                        .AddItemScriptBlock(@"""$($_.Blue)$($_.Blue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Blue")
                        .AddItemScriptBlock(@"""$($_.BrightBlue)$($_.BrightBlue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightBlue")
                        .AddItemScriptBlock(@"""$($_.Cyan)$($_.Cyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Cyan")
                        .AddItemScriptBlock(@"""$($_.BrightCyan)$($_.BrightCyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightCyan")
                        .AddItemScriptBlock(@"""$($_.Green)$($_.Green.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Green")
                        .AddItemScriptBlock(@"""$($_.BrightGreen)$($_.BrightGreen.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightGreen")
                        .AddItemScriptBlock(@"""$($_.Yellow)$($_.Yellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Yellow")
                        .AddItemScriptBlock(@"""$($_.BrightYellow)$($_.BrightYellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightYellow")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSStyleBackgroundColor()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSStyle+BackgroundColor",
                ListControl.Create()
                    .StartEntry()
                        .AddItemScriptBlock(@"""$($_.Black)$($_.Black.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Black")
                        .AddItemScriptBlock(@"""$($_.BrightBlack)$($_.BrightBlack.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightBlack")
                        .AddItemScriptBlock(@"""$($_.White)$($_.White.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "White")
                        .AddItemScriptBlock(@"""$($_.BrightWhite)$($_.BrightWhite.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightWhite")
                        .AddItemScriptBlock(@"""$($_.Red)$($_.Red.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Red")
                        .AddItemScriptBlock(@"""$($_.BrightRed)$($_.BrightRed.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightRed")
                        .AddItemScriptBlock(@"""$($_.Magenta)$($_.Magenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Magenta")
                        .AddItemScriptBlock(@"""$($_.BrightMagenta)$($_.BrightMagenta.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightMagenta")
                        .AddItemScriptBlock(@"""$($_.Blue)$($_.Blue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Blue")
                        .AddItemScriptBlock(@"""$($_.BrightBlue)$($_.BrightBlue.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightBlue")
                        .AddItemScriptBlock(@"""$($_.Cyan)$($_.Cyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Cyan")
                        .AddItemScriptBlock(@"""$($_.BrightCyan)$($_.BrightCyan.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightCyan")
                        .AddItemScriptBlock(@"""$($_.Green)$($_.Green.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Green")
                        .AddItemScriptBlock(@"""$($_.BrightGreen)$($_.BrightGreen.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightGreen")
                        .AddItemScriptBlock(@"""$($_.Yellow)$($_.Yellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "Yellow")
                        .AddItemScriptBlock(@"""$($_.BrightYellow)$($_.BrightYellow.Replace(""""`e"""",'`e'))$($PSStyle.Reset)""", label: "BrightYellow")
                    .EndEntry()
                .EndList());
        }
    }
}
