// Copyright (c) Microsoft Corporation. All rights reserved.
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
                        .StartFrame(leftIndent: 4)
                            .AddText(FileSystemProviderStrings.DirectoryDisplayGrouping)
                            .AddScriptBlockExpressionBinding(@"Split-Path -Parent $_.Path | ForEach-Object { if([Version]::TryParse((Split-Path $_ -Leaf), [ref]$null)) { Split-Path -Parent $_} else {$_} } | Split-Path -Parent")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var ByteCollection_GroupHeader = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddScriptBlockExpressionBinding(@"
                      $header = ""                       00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F""
                      if($_.Path) { $header = ""                       "" + [Microsoft.PowerShell.Commands.UtilityResources]::FormatHexPathPrefix + $_.Path + ""`r`n`r`n"" + $header }

                      $header
                    ")
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                AvailableModules_GroupingFormat,
                ByteCollection_GroupHeader
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
                "System.Management.Automation.ShellVariable",
                ViewsOf_System_Management_Automation_ShellVariable());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ScriptBlock",
                ViewsOf_System_Management_Automation_ScriptBlock());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.ErrorRecord",
                ViewsOf_System_Management_Automation_ErrorRecord());

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
                "Microsoft.PowerShell.Commands.ByteCollection",
                ViewsOf_Microsoft_PowerShell_Commands_ByteCollection(sharedControls));

            yield return new ExtendedTypeDefinition(
                "System.Exception",
                ViewsOf_System_Exception());

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
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Id")
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
                        .AddScriptBlockExpressionBinding(@"$_.ToString(((get-location).path))")
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
                        .AddItemProperty(@"AssemblyInfo")
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
                        .AddItemProperty(@"AssemblyInfo")
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

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_ErrorRecord()
        {
            yield return new FormatViewDefinition("ErrorInstance",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"
                                    if (($_.FullyQualifiedErrorId -ne ""NativeCommandErrorMessage"" -and $_.FullyQualifiedErrorId -ne ""NativeCommandError"") -and $ErrorView -ne ""CategoryView"")
                                    {
                                        $myinv = $_.InvocationInfo
                                        if ($myinv -and $myinv.MyCommand)
                                        {
                                            switch -regex ( $myinv.MyCommand.CommandType )
                                            {
                                                ([System.Management.Automation.CommandTypes]::ExternalScript)
                                                {
                                                    if ($myinv.MyCommand.Path)
                                                    {
                                                        $myinv.MyCommand.Path + "" : ""
                                                    }

                                                    break
                                                }

                                                ([System.Management.Automation.CommandTypes]::Script)
                                                {
                                                    if ($myinv.MyCommand.ScriptBlock)
                                                    {
                                                        $myinv.MyCommand.ScriptBlock.ToString() + "" : ""
                                                    }

                                                    break
                                                }
                                                default
                                                {
                                                    if ($myinv.InvocationName -match '^[&\.]?$')
                                                    {
                                                        if ($myinv.MyCommand.Name)
                                                        {
                                                            $myinv.MyCommand.Name + "" : ""
                                                        }
                                                    }
                                                    else
                                                    {
                                                        $myinv.InvocationName + "" : ""
                                                    }

                                                    break
                                                }
                                            }
                                        }
                                        elseif ($myinv -and $myinv.InvocationName)
                                        {
                                            $myinv.InvocationName + "" : ""
                                        }
                                    }
                                ")
                        .AddScriptBlockExpressionBinding(@"
                                   if ($_.FullyQualifiedErrorId -eq ""NativeCommandErrorMessage"" -or $_.FullyQualifiedErrorId -eq ""NativeCommandError"") {
                                        $_.Exception.Message
                                   }
                                   else
                                   {
                                        $myinv = $_.InvocationInfo
                                        if ($myinv -and ($myinv.MyCommand -or ($_.CategoryInfo.Category -ne 'ParserError'))) {
                                            $posmsg = $myinv.PositionMessage
                                        } else {
                                            $posmsg = """"
                                        }

                                        if ($posmsg -ne """")
                                        {
                                            $posmsg = ""`n"" + $posmsg
                                        }

                                        if ( & { Set-StrictMode -Version 1; $_.PSMessageDetails } ) {
                                            $posmsg = "" : "" +  $_.PSMessageDetails + $posmsg
                                        }

                                        $indent = 4

                                        $errorCategoryMsg = & { Set-StrictMode -Version 1; $_.ErrorCategory_Message }

                                        if ($null -ne $errorCategoryMsg)
                                        {
                                            $indentString = ""+ CategoryInfo          : "" + $_.ErrorCategory_Message
                                        }
                                        else
                                        {
                                            $indentString = ""+ CategoryInfo          : "" + $_.CategoryInfo
                                        }

                                        $posmsg += ""`n"" + $indentString

                                        $indentString = ""+ FullyQualifiedErrorId : "" + $_.FullyQualifiedErrorId
                                        $posmsg += ""`n"" + $indentString

                                        $originInfo = & { Set-StrictMode -Version 1; $_.OriginInfo }

                                        if (($null -ne $originInfo) -and ($null -ne $originInfo.PSComputerName))
                                        {
                                            $indentString = ""+ PSComputerName        : "" + $originInfo.PSComputerName
                                            $posmsg += ""`n"" + $indentString
                                        }

                                        if ($ErrorView -eq ""CategoryView"") {
                                            $_.CategoryInfo.GetMessage()
                                        }
                                        elseif (! $_.ErrorDetails -or ! $_.ErrorDetails.Message) {
                                            $_.Exception.Message + $posmsg + ""`n ""
                                        } else {
                                            $_.ErrorDetails.Message + $posmsg
                                        }
                                   }
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

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_ByteCollection(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("ByteCollection",
                CustomControl.Create()
                    .GroupByScriptBlock("if($_.Path) { $_.Path } else { $_.GetHashCode() }", customControl: sharedControls[1])
                    .StartEntry()
                        .StartFrame()
                            .AddScriptBlockExpressionBinding(@"$_.ToString()")
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Exception()
        {
            yield return new FormatViewDefinition("Exception",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_.Message")
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
                    if ($_.ConnectionInfo -is [System.Management.Automation.Runspaces.WSManConnectionInfo])
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

        private static IEnumerable<FormatViewDefinition> ViewsOf_ModuleInfoGrouping(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("Module",
                TableControl.Create()
                    .GroupByScriptBlock("Split-Path -Parent $_.Path | ForEach-Object { if([Version]::TryParse((Split-Path $_ -Leaf), [ref]$null)) { Split-Path -Parent $_} else {$_} } | Split-Path -Parent", customControl: sharedControls[0])
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, width: 10)
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Left, width: 9, label: "PSEdition")
                    .AddHeader(Alignment.Left, label: "ExportedCommands")
                    .StartRowDefinition()
                        .AddPropertyColumn("ModuleType")
                        .AddPropertyColumn("Version")
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
                    .AddHeader(Alignment.Left, width: 35)
                    .AddHeader(Alignment.Left, label: "ExportedCommands")
                    .StartRowDefinition()
                        .AddPropertyColumn("ModuleType")
                        .AddPropertyColumn("Version")
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
    }
}
