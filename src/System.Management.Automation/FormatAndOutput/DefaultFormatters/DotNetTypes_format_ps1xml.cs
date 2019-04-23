// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class DotNetTypes_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            yield return new ExtendedTypeDefinition(
                "System.CodeDom.Compiler.CompilerError",
                ViewsOf_System_CodeDom_Compiler_CompilerError());

            yield return new ExtendedTypeDefinition(
                "System.Reflection.Assembly",
                ViewsOf_System_Reflection_Assembly());

            yield return new ExtendedTypeDefinition(
                "System.Reflection.AssemblyName",
                ViewsOf_System_Reflection_AssemblyName());

            yield return new ExtendedTypeDefinition(
                "System.Globalization.CultureInfo",
                ViewsOf_System_Globalization_CultureInfo());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.FileVersionInfo",
                ViewsOf_System_Diagnostics_FileVersionInfo());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.EventLogEntry",
                ViewsOf_System_Diagnostics_EventLogEntry());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.EventLog",
                ViewsOf_System_Diagnostics_EventLog());

            yield return new ExtendedTypeDefinition(
                "System.Version",
                ViewsOf_System_Version());

            yield return new ExtendedTypeDefinition(
                "System.Version#IncludeLabel",
                ViewsOf_System_Version_With_Label());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.SemanticVersion",
                ViewsOf_Semantic_Version_With_Label());

            yield return new ExtendedTypeDefinition(
                "System.Drawing.Printing.PrintDocument",
                ViewsOf_System_Drawing_Printing_PrintDocument());

            yield return new ExtendedTypeDefinition(
                "System.Collections.DictionaryEntry",
                ViewsOf_System_Collections_DictionaryEntry());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.ProcessModule",
                ViewsOf_System_Diagnostics_ProcessModule());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.Process",
                ViewsOf_System_Diagnostics_Process());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.Process#IncludeUserName",
                ViewsOf_System_Diagnostics_Process_IncludeUserName());

            yield return new ExtendedTypeDefinition(
                "System.DirectoryServices.DirectoryEntry",
                ViewsOf_System_DirectoryServices_DirectoryEntry());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSSnapInInfo",
                ViewsOf_System_Management_Automation_PSSnapInInfo());

            yield return new ExtendedTypeDefinition(
                "System.ServiceProcess.ServiceController",
                ViewsOf_System_ServiceProcess_ServiceController());

            yield return new ExtendedTypeDefinition(
                "System.TimeSpan",
                ViewsOf_System_TimeSpan());

            yield return new ExtendedTypeDefinition(
                "System.AppDomain",
                ViewsOf_System_AppDomain());

            yield return new ExtendedTypeDefinition(
                "System.DateTime",
                ViewsOf_System_DateTime());

            yield return new ExtendedTypeDefinition(
                "System.Security.AccessControl.ObjectSecurity",
                ViewsOf_System_Security_AccessControl_ObjectSecurity());

            yield return new ExtendedTypeDefinition(
                "System.Management.ManagementClass",
                ViewsOf_System_Management_ManagementClass());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimClass",
                ViewsOf_Microsoft_Management_Infrastructure_CimClass());

            yield return new ExtendedTypeDefinition(
                "System.Guid",
                ViewsOf_System_Guid());

            yield return new ExtendedTypeDefinition(
                @"System.Management.ManagementObject#root\cimv2\Win32_PingStatus",
                ViewsOf_System_Management_ManagementObject_root_cimv2_Win32_PingStatus());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PingStatus());

            yield return new ExtendedTypeDefinition(
                @"System.Management.ManagementObject#root\default\SystemRestore",
                ViewsOf_System_Management_ManagementObject_root_default_SystemRestore());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/default/SystemRestore",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_default_SystemRestore());

            yield return new ExtendedTypeDefinition(
                @"System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering",
                ViewsOf_System_Management_ManagementObject_root_cimv2_Win32_QuickFixEngineering());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_QuickFixEngineering());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Process());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_ComputerSystem());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_PROCESSOR());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DCOMApplication());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DESKTOP());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DESKTOPMONITOR());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DeviceMemoryAddress());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskDrive());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskQuota());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Environment());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Directory());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Group());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_IDEController());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_IRQResource());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_ScheduledJob());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LoadOrderGroup());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LogicalDisk());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LogonSession());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PhysicalMemoryArray());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_OnBoardDevice());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_OperatingSystem());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskPartition());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PortConnector());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_QuotaSetting());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_SCSIController());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Service());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_UserAccount());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkProtocol());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkAdapter());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkAdapterConfiguration());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NTDomain());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Printer());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PrintJob());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Product());

            yield return new ExtendedTypeDefinition(
                "System.Net.NetworkCredential",
                ViewsOf_System_Net_NetworkCredential());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSMethod",
                ViewsOf_System_Management_Automation_PSMethod());

            yield return new ExtendedTypeDefinition(
                "Microsoft.Management.Infrastructure.CimInstance#__PartialCIMInstance",
                ViewsOf_Microsoft_Management_Infrastructure_CimInstance___PartialCIMInstance());

            yield return new ExtendedTypeDefinition(
                "System.Threading.Tasks.Task",
                ViewsOf_System_Threading_Tasks_Task());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_CodeDom_Compiler_CompilerError()
        {
            yield return new FormatViewDefinition("System.CodeDom.Compiler.CompilerError",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"ErrorText")
                        .AddItemProperty(@"Line")
                        .AddItemProperty(@"Column")
                        .AddItemProperty(@"ErrorNumber")
                        .AddItemProperty(@"LineSource")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Reflection_Assembly()
        {
            yield return new FormatViewDefinition("System.Reflection.Assembly",
                TableControl.Create()
                    .AddHeader(label: "GAC", width: 6)
                    .AddHeader(label: "Version", width: 14)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("GlobalAssemblyCache")
                        .AddPropertyColumn("ImageRuntimeVersion")
                        .AddPropertyColumn("Location")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Reflection.Assembly",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"CodeBase")
                        .AddItemProperty(@"EntryPoint")
                        .AddItemProperty(@"EscapedCodeBase")
                        .AddItemProperty(@"FullName")
                        .AddItemProperty(@"GlobalAssemblyCache")
                        .AddItemProperty(@"HostContext")
                        .AddItemProperty(@"ImageFileMachine")
                        .AddItemProperty(@"ImageRuntimeVersion")
                        .AddItemProperty(@"Location")
                        .AddItemProperty(@"ManifestModule")
                        .AddItemProperty(@"MetadataToken")
                        .AddItemProperty(@"PortableExecutableKind")
                        .AddItemProperty(@"ReflectionOnly")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Reflection_AssemblyName()
        {
            yield return new FormatViewDefinition("System.Reflection.AssemblyName",
                TableControl.Create()
                    .AddHeader(width: 14)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Version")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Globalization_CultureInfo()
        {
            yield return new FormatViewDefinition("System.Globalization.CultureInfo",
                TableControl.Create()
                    .AddHeader(width: 16)
                    .AddHeader(width: 16)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("LCID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("DisplayName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_FileVersionInfo()
        {
            yield return new FormatViewDefinition("System.Diagnostics.FileVersionInfo",
                TableControl.Create()
                    .AddHeader(width: 16)
                    .AddHeader(width: 16)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("ProductVersion")
                        .AddPropertyColumn("FileVersion")
                        .AddPropertyColumn("FileName")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Diagnostics.FileVersionInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"OriginalFileName")
                        .AddItemProperty(@"FileDescription")
                        .AddItemProperty(@"ProductName")
                        .AddItemProperty(@"Comments")
                        .AddItemProperty(@"CompanyName")
                        .AddItemProperty(@"FileName")
                        .AddItemProperty(@"FileVersion")
                        .AddItemProperty(@"ProductVersion")
                        .AddItemProperty(@"IsDebug")
                        .AddItemProperty(@"IsPatched")
                        .AddItemProperty(@"IsPreRelease")
                        .AddItemProperty(@"IsPrivateBuild")
                        .AddItemProperty(@"IsSpecialBuild")
                        .AddItemProperty(@"Language")
                        .AddItemProperty(@"LegalCopyright")
                        .AddItemProperty(@"LegalTrademarks")
                        .AddItemProperty(@"PrivateBuild")
                        .AddItemProperty(@"SpecialBuild")
                        .AddItemProperty(@"FileVersionRaw")
                        .AddItemProperty(@"ProductVersionRaw")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_EventLogEntry()
        {
            yield return new FormatViewDefinition("System.Diagnostics.EventLogEntry",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Index", width: 8)
                    .AddHeader(label: "Time", width: 13)
                    .AddHeader(label: "EntryType", width: 11)
                    .AddHeader(label: "Source", width: 20)
                    .AddHeader(Alignment.Right, label: "InstanceID", width: 12)
                    .AddHeader(label: "Message")
                    .StartRowDefinition()
                        .AddPropertyColumn("Index")
                        .AddPropertyColumn("TimeGenerated", format: "{0:MMM} {0:dd} {0:HH}:{0:mm}")
                        .AddScriptBlockColumn("$_.EntryType")
                        .AddPropertyColumn("Source")
                        .AddPropertyColumn("InstanceID")
                        .AddPropertyColumn("Message")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Diagnostics.EventLogEntry",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Index")
                        .AddItemProperty(@"EntryType")
                        .AddItemProperty(@"InstanceID")
                        .AddItemProperty(@"Message")
                        .AddItemProperty(@"Category")
                        .AddItemProperty(@"CategoryNumber")
                        .AddItemProperty(@"ReplacementStrings")
                        .AddItemProperty(@"Source")
                        .AddItemProperty(@"TimeGenerated")
                        .AddItemProperty(@"TimeWritten")
                        .AddItemProperty(@"UserName")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_EventLog()
        {
            yield return new FormatViewDefinition("System.Diagnostics.EventLog",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Max(K)", width: 8)
                    .AddHeader(Alignment.Right, label: "Retain", width: 6)
                    .AddHeader(label: "OverflowAction", width: 18)
                    .AddHeader(Alignment.Right, label: "Entries", width: 10)
                    .AddHeader(label: "Log")
                    .StartRowDefinition()
                        .AddScriptBlockColumn("$_.MaximumKilobytes.ToString('N0')")
                        .AddPropertyColumn("MinimumRetentionDays")
                        .AddPropertyColumn("OverflowAction")
                        .AddScriptBlockColumn("$_.Entries.Count.ToString('N0')")
                        .AddPropertyColumn("Log")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Diagnostics.EventLog",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Log")
                        .AddItemProperty(@"EnableRaisingEvents")
                        .AddItemProperty(@"MaximumKilobytes")
                        .AddItemProperty(@"MinimumRetentionDays")
                        .AddItemProperty(@"OverflowAction")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Version()
        {
            yield return new FormatViewDefinition("System.Version",
                TableControl.Create()
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 8)
                    .StartRowDefinition()
                        .AddPropertyColumn("Major")
                        .AddPropertyColumn("Minor")
                        .AddPropertyColumn("Build")
                        .AddPropertyColumn("Revision")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Version_With_Label()
        {
            yield return new FormatViewDefinition("System.Version",
                TableControl.Create()
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 8)
                    .AddHeader(width: 26)
                    .AddHeader(width: 27)
                    .StartRowDefinition()
                        .AddPropertyColumn("Major")
                        .AddPropertyColumn("Minor")
                        .AddPropertyColumn("Build")
                        .AddPropertyColumn("Revision")
                        .AddPropertyColumn("PSSemVerPreReleaseLabel")
                        .AddPropertyColumn("PSSemVerBuildLabel")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Semantic_Version_With_Label()
        {
            yield return new FormatViewDefinition("System.Management.Automation.SemanticVersion",
                TableControl.Create()
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 6)
                    .AddHeader(width: 15)
                    .AddHeader(width: 11)
                    .StartRowDefinition()
                        .AddPropertyColumn("Major")
                        .AddPropertyColumn("Minor")
                        .AddPropertyColumn("Patch")
                        .AddPropertyColumn("PreReleaseLabel")
                        .AddPropertyColumn("BuildLabel")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Drawing_Printing_PrintDocument()
        {
            yield return new FormatViewDefinition("System.Drawing.Printing.PrintDocument",
                TableControl.Create()
                    .AddHeader(width: 10)
                    .AddHeader(width: 10)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Color")
                        .AddPropertyColumn("Duplex")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Collections_DictionaryEntry()
        {
            yield return new FormatViewDefinition("Dictionary",
                TableControl.Create()
                    .AddHeader(width: 30)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Value")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Collections.DictionaryEntry",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Value")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_ProcessModule()
        {
            yield return new FormatViewDefinition("ProcessModule",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "Size(K)", width: 10)
                    .AddHeader(width: 50)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddScriptBlockColumn("$_.Size")
                        .AddPropertyColumn("ModuleName")
                        .AddPropertyColumn("FileName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_Process()
        {
            yield return new FormatViewDefinition("process",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "NPM(K)", width: 7)
                    .AddHeader(Alignment.Right, label: "PM(M)", width: 8)
                    .AddHeader(Alignment.Right, label: "WS(M)", width: 10)
                    .AddHeader(Alignment.Right, label: "CPU(s)", width: 10)
                    .AddHeader(Alignment.Right, width: 7)
                    .AddHeader(Alignment.Right, width: 3)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddScriptBlockColumn("[long]($_.NPM / 1024)")
                        .AddScriptBlockColumn("\"{0:N2}\" -f [float]($_.PM / 1MB)")
                        .AddScriptBlockColumn("\"{0:N2}\" -f [float]($_.WS / 1MB)")
                        .AddScriptBlockColumn("\"{0:N2}\" -f [float]($_.CPU)")
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("SI")
                        .AddPropertyColumn("ProcessName")
                    .EndRowDefinition()
                .EndTable());
            yield return new FormatViewDefinition("Priority",
                TableControl.Create()
                    .GroupByProperty("PriorityClass", label: "PriorityClass")
                    .AddHeader(width: 20)
                    .AddHeader(Alignment.Right, width: 10)
                    .AddHeader(Alignment.Right, width: 12)
                    .StartRowDefinition()
                        .AddPropertyColumn("ProcessName")
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("WorkingSet64")
                    .EndRowDefinition()
                .EndTable());
            yield return new FormatViewDefinition("StartTime",
                TableControl.Create()
                    .GroupByScriptBlock("$_.StartTime.ToShortDateString()", label: "StartTime.ToShortDateString()")
                    .AddHeader(width: 20)
                    .AddHeader(Alignment.Right, width: 10)
                    .AddHeader(Alignment.Right, width: 12)
                    .StartRowDefinition()
                        .AddPropertyColumn("ProcessName")
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("WorkingSet64")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("process",
                WideControl.Create()
                    .AddPropertyEntry("ProcessName")
                .EndWideControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_Process_IncludeUserName()
        {
            yield return new FormatViewDefinition("ProcessWithUserName",
                TableControl.Create()
                    .AddHeader(Alignment.Right, label: "WS(M)", width: 10)
                    .AddHeader(Alignment.Right, label: "CPU(s)", width: 8)
                    .AddHeader(Alignment.Right, width: 7)
                    .AddHeader(width: 30)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddScriptBlockColumn("\"{0:N2}\" -f [float]($_.WS / 1MB)")
                        .AddScriptBlockColumn("\"{0:N2}\" -f [float]($_.CPU)")
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("UserName")
                        .AddPropertyColumn("ProcessName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_DirectoryServices_DirectoryEntry()
        {
            yield return new FormatViewDefinition("DirectoryEntry",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"distinguishedName", label: "distinguishedName")
                        .AddItemProperty(@"path", label: "Path")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSSnapInInfo()
        {
            yield return new FormatViewDefinition("PSSnapInInfo",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name", label: "Name")
                        .AddItemProperty(@"PSVersion", label: "PSVersion")
                        .AddItemProperty(@"Description", label: "Description")
                    .EndEntry()
                .EndList());
            yield return new FormatViewDefinition("PSSnapInInfo",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "Name", width: 30)
                    .AddHeader(Alignment.Left, label: "PSVersion", width: 20)
                    .AddHeader(Alignment.Left, label: "Description", width: 30)
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("PSVersion")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_ServiceProcess_ServiceController()
        {
            yield return new FormatViewDefinition("service",
                TableControl.Create()
                    .AddHeader(width: 8)
                    .AddHeader(width: 18)
                    .AddHeader(width: 38)
                    .StartRowDefinition()
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("DisplayName")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.ServiceProcess.ServiceController",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"DisplayName")
                        .AddItemProperty(@"Status")
                        .AddItemProperty(@"DependentServices")
                        .AddItemProperty(@"ServicesDependedOn")
                        .AddItemProperty(@"CanPauseAndContinue")
                        .AddItemProperty(@"CanShutdown")
                        .AddItemProperty(@"CanStop")
                        .AddItemProperty(@"ServiceType")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_TimeSpan()
        {
            yield return new FormatViewDefinition("System.TimeSpan",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Days")
                        .AddItemProperty(@"Hours")
                        .AddItemProperty(@"Minutes")
                        .AddItemProperty(@"Seconds")
                        .AddItemProperty(@"Milliseconds")
                        .AddItemProperty(@"Ticks")
                        .AddItemProperty(@"TotalDays")
                        .AddItemProperty(@"TotalHours")
                        .AddItemProperty(@"TotalMinutes")
                        .AddItemProperty(@"TotalSeconds")
                        .AddItemProperty(@"TotalMilliseconds")
                    .EndEntry()
                .EndList());
            yield return new FormatViewDefinition("System.TimeSpan",
                TableControl.Create()
                    .StartRowDefinition()
                        .AddPropertyColumn("Days")
                        .AddPropertyColumn("Hours")
                        .AddPropertyColumn("Minutes")
                        .AddPropertyColumn("Seconds")
                        .AddPropertyColumn("Milliseconds")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.TimeSpan",
                WideControl.Create()
                    .AddPropertyEntry("TotalMilliseconds")
                .EndWideControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_AppDomain()
        {
            yield return new FormatViewDefinition("System.AppDomain",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"FriendlyName")
                        .AddItemProperty(@"Id")
                        .AddItemProperty(@"ApplicationDescription")
                        .AddItemProperty(@"BaseDirectory")
                        .AddItemProperty(@"DynamicDirectory")
                        .AddItemProperty(@"RelativeSearchPath")
                        .AddItemProperty(@"SetupInformation")
                        .AddItemProperty(@"ShadowCopyFiles")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_DateTime()
        {
            yield return new FormatViewDefinition("DateTime",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"DateTime")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Security_AccessControl_ObjectSecurity()
        {
            yield return new FormatViewDefinition("System.Security.AccessControl.ObjectSecurity",
                TableControl.Create()
                    .AddHeader()
                    .AddHeader()
                    .AddHeader(label: "Access")
                    .StartRowDefinition()
                        .AddPropertyColumn("Path")
                        .AddPropertyColumn("Owner")
                        .AddPropertyColumn("AccessToString")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Security.AccessControl.ObjectSecurity",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Path")
                        .AddItemProperty(@"Owner")
                        .AddItemProperty(@"Group")
                        .AddItemProperty(@"AccessToString", label: "Access")
                        .AddItemProperty(@"AuditToString", label: "Audit")
                        .AddItemProperty(@"Sddl")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_ManagementClass()
        {
            yield return new FormatViewDefinition("System.Management.ManagementClass",
                TableControl.Create()
                    .GroupByProperty("__Namespace", label: "NameSpace")
                    .AddHeader(width: 35)
                    .AddHeader(width: 20)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Methods")
                        .AddPropertyColumn("Properties")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimClass()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimClass",
                TableControl.Create()
                    .GroupByScriptBlock("$_.CimSystemProperties.Namespace", label: "NameSpace")
                    .AddHeader(label: "CimClassName", width: 35)
                    .AddHeader(width: 20)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("CimClassName")
                        .AddPropertyColumn("CimClassMethods")
                        .AddPropertyColumn("CimClassProperties")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Guid()
        {
            yield return new FormatViewDefinition("System.Guid",
                TableControl.Create()
                    .StartRowDefinition()
                        .AddPropertyColumn("Guid")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_ManagementObject_root_cimv2_Win32_PingStatus()
        {
            yield return new FormatViewDefinition(@"System.Management.ManagementObject#root\cimv2\Win32_PingStatus",
                TableControl.Create()
                    .AddHeader(label: "Source", width: 13)
                    .AddHeader(label: "Destination", width: 15)
                    .AddHeader(label: "IPV4Address", width: 16)
                    .AddHeader(label: "IPV6Address", width: 40)
                    .AddHeader(label: "Bytes", width: 8)
                    .AddHeader(label: "Time(ms)", width: 9)
                    .StartRowDefinition()
                        .AddPropertyColumn("__Server")
                        .AddPropertyColumn("Address")
                        .AddPropertyColumn("IPV4Address")
                        .AddPropertyColumn("IPV6Address")
                        .AddPropertyColumn("BufferSize")
                        .AddPropertyColumn("ResponseTime")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PingStatus()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus",
                TableControl.Create()
                    .AddHeader(label: "Source", width: 13)
                    .AddHeader(label: "Destination", width: 15)
                    .AddHeader(label: "IPV4Address", width: 16)
                    .AddHeader(label: "IPV6Address", width: 40)
                    .AddHeader(label: "Bytes", width: 8)
                    .AddHeader(label: "Time(ms)", width: 9)
                    .StartRowDefinition()
                         .AddScriptBlockColumn(@"
                            $sourceName = $_.PSComputerName;
                            if($sourceName -eq ""."")
                            {$sourceName = $env:COMPUTERNAME;}

                            return $sourceName;
                        ")
                        .AddPropertyColumn("Address")
                        .AddPropertyColumn("IPV4Address")
                        .AddPropertyColumn("IPV6Address")
                        .AddPropertyColumn("BufferSize")
                        .AddPropertyColumn("ResponseTime")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_ManagementObject_root_default_SystemRestore()
        {
            yield return new FormatViewDefinition(@"System.Management.ManagementObject#root\default\SystemRestore",
                TableControl.Create()
                    .AddHeader(label: "CreationTime", width: 22)
                    .AddHeader(label: "Description", width: 30)
                    .AddHeader(label: "SequenceNumber", width: 17)
                    .AddHeader(label: "EventType", width: 17)
                    .AddHeader(label: "RestorePointType", width: 22)
                    .StartRowDefinition()
                        .AddScriptBlockColumn(@"
                    return $_.ConvertToDateTime($_.CreationTime)
                ")
                        .AddPropertyColumn("Description")
                        .AddPropertyColumn("SequenceNumber")
                        .AddScriptBlockColumn(@"
                    $eventType = $_.EventType;
                    if($_.EventType -eq 100)
                    {$eventType = ""BEGIN_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 101)
                    {$eventType = ""END_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 102)
                    {$eventType = ""BEGIN_NESTED_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 103)
                    {$eventType = ""END_NESTED_SYSTEM_CHANGE"";}

                    return $eventType;
                ")
                        .AddScriptBlockColumn(@"
                $RestorePointType = $_.RestorePointType;
                if($_.RestorePointType -eq 0)
                { $RestorePointType = ""APPLICATION_INSTALL"";}

                if($_.RestorePointType -eq 1)
                { $RestorePointType = ""APPLICATION_UNINSTALL"";}

                if($_.RestorePointType -eq 10)
                { $RestorePointType = ""DEVICE_DRIVER_INSTALL"";}

                if($_.RestorePointType -eq 12)
                { $RestorePointType = ""MODIFY_SETTINGS"";}

                if($_.RestorePointType -eq 13)
                { $RestorePointType = ""CANCELLED_OPERATION"";}

                    return $RestorePointType;
                ")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_default_SystemRestore()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/default/SystemRestore",
                TableControl.Create()
                    .AddHeader(label: "CreationTime", width: 22)
                    .AddHeader(label: "Description", width: 30)
                    .AddHeader(label: "SequenceNumber", width: 17)
                    .AddHeader(label: "EventType", width: 17)
                    .AddHeader(label: "RestorePointType", width: 22)
                    .StartRowDefinition()
                        .AddScriptBlockColumn(@"
                    return $_.ConvertToDateTime($_.CreationTime)
                  ")
                        .AddPropertyColumn("Description")
                        .AddPropertyColumn("SequenceNumber")
                        .AddScriptBlockColumn(@"
                    $eventType = $_.EventType;
                    if($_.EventType -eq 100)
                    {$eventType = ""BEGIN_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 101)
                    {$eventType = ""END_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 102)
                    {$eventType = ""BEGIN_NESTED_SYSTEM_CHANGE"";}

                    if($_.EventType -eq 103)
                    {$eventType = ""END_NESTED_SYSTEM_CHANGE"";}

                    return $eventType;
                  ")
                        .AddScriptBlockColumn(@"
                    $RestorePointType = $_.RestorePointType;
                    if($_.RestorePointType -eq 0)
                    { $RestorePointType = ""APPLICATION_INSTALL"";}

                    if($_.RestorePointType -eq 1)
                    { $RestorePointType = ""APPLICATION_UNINSTALL"";}

                    if($_.RestorePointType -eq 10)
                    { $RestorePointType = ""DEVICE_DRIVER_INSTALL"";}

                    if($_.RestorePointType -eq 12)
                    { $RestorePointType = ""MODIFY_SETTINGS"";}

                    if($_.RestorePointType -eq 13)
                    { $RestorePointType = ""CANCELLED_OPERATION"";}

                    return $RestorePointType;
                  ")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_ManagementObject_root_cimv2_Win32_QuickFixEngineering()
        {
            yield return new FormatViewDefinition(@"System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering",
                TableControl.Create()
                    .AddHeader(label: "Source", width: 13)
                    .AddHeader(label: "Description", width: 16)
                    .AddHeader(label: "HotFixID", width: 13)
                    .AddHeader(label: "InstalledBy", width: 20)
                    .AddHeader(label: "InstalledOn", width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("__SERVER")
                        .AddPropertyColumn("Description")
                        .AddPropertyColumn("HotFixID")
                        .AddPropertyColumn("InstalledBy")
                        .AddPropertyColumn("InstalledOn")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_QuickFixEngineering()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering",
                TableControl.Create()
                    .AddHeader(label: "Source", width: 13)
                    .AddHeader(label: "Description", width: 16)
                    .AddHeader(label: "HotFixID", width: 13)
                    .AddHeader(label: "InstalledBy", width: 20)
                    .AddHeader(label: "InstalledOn", width: 25)
                    .StartRowDefinition()
                        .AddPropertyColumn("ComputerName")
                        .AddPropertyColumn("Description")
                        .AddPropertyColumn("HotFixID")
                        .AddPropertyColumn("InstalledBy")
                        .AddPropertyColumn("InstalledOn")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Process()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process",
                TableControl.Create()
                    .AddHeader(label: "ProcessId")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "HandleCount")
                    .AddHeader(label: "WorkingSetSize")
                    .AddHeader(label: "VirtualSize")
                    .StartRowDefinition()
                        .AddPropertyColumn("ProcessId")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("HandleCount")
                        .AddPropertyColumn("WorkingSetSize")
                        .AddPropertyColumn("VirtualSize")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_ComputerSystem()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "PrimaryOwnerName")
                    .AddHeader(label: "Domain")
                    .AddHeader(label: "TotalPhysicalMemory")
                    .AddHeader(label: "Model")
                    .AddHeader(label: "Manufacturer")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("PrimaryOwnerName")
                        .AddPropertyColumn("Domain")
                        .AddPropertyColumn("TotalPhysicalMemory")
                        .AddPropertyColumn("Model")
                        .AddPropertyColumn("Manufacturer")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_PROCESSOR()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR",
                TableControl.Create()
                    .AddHeader(label: "DeviceID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "MaxClockSpeed")
                    .AddHeader(label: "SocketDesignation")
                    .AddHeader(label: "Manufacturer")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("MaxClockSpeed")
                        .AddPropertyColumn("SocketDesignation")
                        .AddPropertyColumn("Manufacturer")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DCOMApplication()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication",
                TableControl.Create()
                    .AddHeader(label: "AppID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "InstallDate")
                    .StartRowDefinition()
                        .AddPropertyColumn("AppID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("InstallDate")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DESKTOP()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP",
                TableControl.Create()
                    .AddHeader(label: "SettingID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "ScreenSaverActive")
                    .AddHeader(label: "ScreenSaverSecure")
                    .AddHeader(label: "ScreenSaverTimeout")
                    .StartRowDefinition()
                        .AddPropertyColumn("SettingID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("ScreenSaverActive")
                        .AddPropertyColumn("ScreenSaverSecure")
                        .AddPropertyColumn("ScreenSaverTimeout")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_WIN32_DESKTOPMONITOR()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR",
                TableControl.Create()
                    .AddHeader(label: "DeviceID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "DisplayType")
                    .AddHeader(label: "MonitorManufacturer")
                    .AddHeader(label: "ScreenHeight")
                    .AddHeader(label: "ScreenWidth")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("DisplayType")
                        .AddPropertyColumn("MonitorManufacturer")
                        .AddPropertyColumn("ScreenHeight")
                        .AddPropertyColumn("ScreenWidth")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DeviceMemoryAddress()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "MemoryType")
                    .AddHeader(label: "Status")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("MemoryType")
                        .AddPropertyColumn("Status")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskDrive()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive",
                TableControl.Create()
                    .AddHeader(label: "DeviceID")
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "Partitions")
                    .AddHeader(label: "Size")
                    .AddHeader(label: "Model")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceID")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("Partitions")
                        .AddPropertyColumn("Size")
                        .AddPropertyColumn("Model")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskQuota()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota",
                TableControl.Create()
                    .AddHeader(label: "DiskSpaceUsed")
                    .AddHeader(label: "Limit")
                    .AddHeader(label: "QuotaVolume")
                    .AddHeader(label: "User")
                    .StartRowDefinition()
                        .AddPropertyColumn("DiskSpaceUsed")
                        .AddPropertyColumn("Limit")
                        .AddPropertyColumn("QuotaVolume")
                        .AddPropertyColumn("User")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Environment()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "UserName")
                    .AddHeader(label: "VariableValue")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("UserName")
                        .AddPropertyColumn("VariableValue")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Directory()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Hidden")
                    .AddHeader(label: "Archive")
                    .AddHeader(label: "Writeable")
                    .AddHeader(label: "LastModified")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Hidden")
                        .AddPropertyColumn("Archive")
                        .AddPropertyColumn("Writeable")
                        .AddPropertyColumn("LastModified")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Group()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group",
                TableControl.Create()
                    .AddHeader(label: "SID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "Domain")
                    .StartRowDefinition()
                        .AddPropertyColumn("SID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("Domain")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_IDEController()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Status")
                    .AddHeader(label: "StatusInfo")
                    .AddHeader(label: "ProtocolSupported")
                    .AddHeader(label: "Manufacturer")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("StatusInfo")
                        .AddPropertyColumn("ProtocolSupported")
                        .AddPropertyColumn("Manufacturer")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_IRQResource()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "IRQNumber")
                    .AddHeader(label: "Hardware")
                    .AddHeader(label: "TriggerLevel")
                    .AddHeader(label: "TriggerType")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("IRQNumber")
                        .AddPropertyColumn("Hardware")
                        .AddPropertyColumn("TriggerLevel")
                        .AddPropertyColumn("TriggerType")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_ScheduledJob()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob",
                TableControl.Create()
                    .AddHeader(label: "JobId")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Owner")
                    .AddHeader(label: "Priority")
                    .AddHeader(label: "Command")
                    .StartRowDefinition()
                        .AddPropertyColumn("JobId")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Owner")
                        .AddPropertyColumn("Priority")
                        .AddPropertyColumn("Command")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LoadOrderGroup()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "GroupOrder")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("GroupOrder")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LogicalDisk()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk",
                TableControl.Create()
                    .AddHeader(label: "DeviceID")
                    .AddHeader(label: "DriveType")
                    .AddHeader(label: "ProviderName", width: 16)
                    .AddHeader(label: "VolumeName", width: 16)
                    .AddHeader(label: "Size")
                    .AddHeader(label: "FreeSpace")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceID")
                        .AddPropertyColumn("DriveType")
                        .AddPropertyColumn("ProviderName")
                        .AddPropertyColumn("VolumeName")
                        .AddPropertyColumn("Size")
                        .AddPropertyColumn("FreeSpace")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_LogonSession()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession",
                TableControl.Create()
                    .AddHeader(label: "LogonId")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "LogonType")
                    .AddHeader(label: "StartTime")
                    .AddHeader(label: "Status")
                    .AddHeader(label: "AuthenticationPackage")
                    .StartRowDefinition()
                        .AddPropertyColumn("LogonId")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("LogonType")
                        .AddPropertyColumn("StartTime")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("AuthenticationPackage")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PhysicalMemoryArray()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "MemoryDevices")
                    .AddHeader(label: "MaxCapacity")
                    .AddHeader(label: "Model")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("MemoryDevices")
                        .AddPropertyColumn("MaxCapacity")
                        .AddPropertyColumn("Model")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_OnBoardDevice()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice",
                TableControl.Create()
                    .AddHeader(label: "DeviceType")
                    .AddHeader(label: "SerialNumber")
                    .AddHeader(label: "Enabled")
                    .AddHeader(label: "Description")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceType")
                        .AddPropertyColumn("SerialNumber")
                        .AddPropertyColumn("Enabled")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_OperatingSystem()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem",
                TableControl.Create()
                    .AddHeader(label: "SystemDirectory")
                    .AddHeader(label: "Organization")
                    .AddHeader(label: "BuildNumber")
                    .AddHeader(label: "RegisteredUser")
                    .AddHeader(label: "SerialNumber")
                    .AddHeader(label: "Version")
                    .StartRowDefinition()
                        .AddPropertyColumn("SystemDirectory")
                        .AddPropertyColumn("Organization")
                        .AddPropertyColumn("BuildNumber")
                        .AddPropertyColumn("RegisteredUser")
                        .AddPropertyColumn("SerialNumber")
                        .AddPropertyColumn("Version")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_DiskPartition()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "NumberOfBlocks")
                    .AddHeader(label: "BootPartition")
                    .AddHeader(label: "PrimaryPartition")
                    .AddHeader(label: "Size")
                    .AddHeader(label: "Index")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("NumberOfBlocks")
                        .AddPropertyColumn("BootPartition")
                        .AddPropertyColumn("PrimaryPartition")
                        .AddPropertyColumn("Size")
                        .AddPropertyColumn("Index")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PortConnector()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector",
                TableControl.Create()
                    .AddHeader(label: "Tag")
                    .AddHeader(label: "PortType")
                    .AddHeader(label: "ConnectorType")
                    .AddHeader(label: "SerialNumber")
                    .AddHeader(label: "ExternalReferenceDesignator")
                    .StartRowDefinition()
                        .AddPropertyColumn("Tag")
                        .AddPropertyColumn("PortType")
                        .AddPropertyColumn("ConnectorType")
                        .AddPropertyColumn("SerialNumber")
                        .AddPropertyColumn("ExternalReferenceDesignator")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_QuotaSetting()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting",
                TableControl.Create()
                    .AddHeader(label: "SettingID")
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "State")
                    .AddHeader(label: "VolumePath")
                    .AddHeader(label: "DefaultLimit")
                    .AddHeader(label: "DefaultWarningLimit")
                    .StartRowDefinition()
                        .AddPropertyColumn("SettingID")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("State")
                        .AddPropertyColumn("VolumePath")
                        .AddPropertyColumn("DefaultLimit")
                        .AddPropertyColumn("DefaultWarningLimit")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_SCSIController()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "DriverName", width: 16)
                    .AddHeader(label: "Status")
                    .AddHeader(label: "StatusInfo")
                    .AddHeader(label: "ProtocolSupported")
                    .AddHeader(label: "Manufacturer")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("DriverName")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("StatusInfo")
                        .AddPropertyColumn("ProtocolSupported")
                        .AddPropertyColumn("Manufacturer")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Service()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service",
                TableControl.Create()
                    .AddHeader(label: "ProcessId")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "StartMode")
                    .AddHeader(label: "State")
                    .AddHeader(label: "Status")
                    .AddHeader(label: "ExitCode")
                    .StartRowDefinition()
                        .AddPropertyColumn("ProcessId")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("StartMode")
                        .AddPropertyColumn("State")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("ExitCode")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_UserAccount()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "AccountType")
                    .AddHeader(label: "SID")
                    .AddHeader(label: "Domain")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("AccountType")
                        .AddPropertyColumn("SID")
                        .AddPropertyColumn("Domain")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkProtocol()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "Status")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("Status")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkAdapter()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter",
                TableControl.Create()
                    .AddHeader(label: "DeviceID")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "AdapterType")
                    .AddHeader(label: "ServiceName")
                    .StartRowDefinition()
                        .AddPropertyColumn("DeviceID")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("AdapterType")
                        .AddPropertyColumn("ServiceName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NetworkAdapterConfiguration()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration",
                TableControl.Create()
                    .AddHeader(label: "ServiceName", width: 16)
                    .AddHeader(label: "DHCPEnabled")
                    .AddHeader(label: "Index")
                    .AddHeader(label: "Description")
                    .StartRowDefinition()
                        .AddPropertyColumn("ServiceName")
                        .AddPropertyColumn("DHCPEnabled")
                        .AddPropertyColumn("Index")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_NTDomain()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain",
                TableControl.Create()
                    .AddHeader(label: "DomainName", width: 16)
                    .AddHeader(label: "DnsForestName")
                    .AddHeader(label: "DomainControllerName")
                    .StartRowDefinition()
                        .AddPropertyColumn("DomainName")
                        .AddPropertyColumn("DnsForestName")
                        .AddPropertyColumn("DomainControllerName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Printer()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "ShareName")
                    .AddHeader(label: "SystemName")
                    .AddHeader(label: "PrinterState")
                    .AddHeader(label: "PrinterStatus")
                    .AddHeader(label: "Location")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("ShareName")
                        .AddPropertyColumn("SystemName")
                        .AddPropertyColumn("PrinterState")
                        .AddPropertyColumn("PrinterStatus")
                        .AddPropertyColumn("Location")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_PrintJob()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob",
                TableControl.Create()
                    .AddHeader(label: "JobId")
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "JobStatus")
                    .AddHeader(label: "Owner")
                    .AddHeader(label: "Priority")
                    .AddHeader(label: "Size")
                    .AddHeader(label: "Document")
                    .StartRowDefinition()
                        .AddPropertyColumn("JobId")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("JobStatus")
                        .AddPropertyColumn("Owner")
                        .AddPropertyColumn("Priority")
                        .AddPropertyColumn("Size")
                        .AddPropertyColumn("Document")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance_root_cimv2_Win32_Product()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product",
                TableControl.Create()
                    .AddHeader(label: "Name", width: 16)
                    .AddHeader(label: "Caption")
                    .AddHeader(label: "Vendor")
                    .AddHeader(label: "Version")
                    .AddHeader(label: "IdentifyingNumber")
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Caption")
                        .AddPropertyColumn("Vendor")
                        .AddPropertyColumn("Version")
                        .AddPropertyColumn("IdentifyingNumber")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Net_NetworkCredential()
        {
            yield return new FormatViewDefinition("System.Net.NetworkCredential",
                TableControl.Create()
                    .AddHeader(label: "UserName", width: 50)
                    .AddHeader(label: "Domain", width: 50)
                    .StartRowDefinition()
                        .AddPropertyColumn("UserName")
                        .AddPropertyColumn("Domain")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSMethod()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSMethod",
                TableControl.Create()
                    .AddHeader(label: "OverloadDefinitions")
                    .StartRowDefinition(wrap: true)
                        .AddScriptBlockColumn(@"
                                    $_.OverloadDefinitions | Out-String
                                ")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_Management_Infrastructure_CimInstance___PartialCIMInstance()
        {
            yield return new FormatViewDefinition("Microsoft.Management.Infrastructure.CimInstance#__PartialCIMInstance",
                CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddScriptBlockExpressionBinding(@"
                                            $str = $_ | Microsoft.PowerShell.Utility\Format-List -Property * | Microsoft.PowerShell.Utility\Out-String
                                            $str
                                        ")
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Threading_Tasks_Task()
        {
            // Avoid referencing the Result property in these views to avoid potential
            // deadlocks that may occur. Result should only be referenced once the task
            // is actually completed.
            yield return new FormatViewDefinition(
                "System.Threading.Tasks.Task",
                TableControl
                    .Create()
                        .AddHeader(label: "Id")
                        .AddHeader(label: "IsCompleted")
                        .AddHeader(label: "Status")
                        .StartRowDefinition()
                            .AddPropertyColumn("Id")
                            .AddPropertyColumn("IsCompleted")
                            .AddPropertyColumn("Status")
                        .EndRowDefinition()
                    .EndTable());

            yield return new FormatViewDefinition(
                "System.Threading.Tasks.Task",
                ListControl
                    .Create()
                        .StartEntry()
                            .AddItemProperty(@"AsyncState")
                            .AddItemProperty(@"AsyncWaitHandle")
                            .AddItemProperty(@"CompletedSynchronously")
                            .AddItemProperty(@"CreationOptions")
                            .AddItemProperty(@"Exception")
                            .AddItemProperty(@"Id")
                            .AddItemProperty(@"IsCanceled")
                            .AddItemProperty(@"IsCompleted")
                            .AddItemProperty(@"IsCompletedSuccessfully")
                            .AddItemProperty(@"IsFaulted")
                            .AddItemScriptBlock(
                                @"
                                    if ($_.IsCompleted) {
                                        $_.Result
                                    }
                                ",
                                label: "Result")
                            .AddItemProperty(@"Status")
                        .EndEntry()
                    .EndList());
        }
    }
}
