// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class FileSystem_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var FileSystemTypes_GroupingFormat = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddText(FileSystemProviderStrings.DirectoryDisplayGrouping)
                            .AddScriptBlockExpressionBinding(@"
                                                  $_.PSParentPath.Replace(""Microsoft.PowerShell.Core\FileSystem::"", """")
                                              ")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                FileSystemTypes_GroupingFormat
            };

            var td1 = new ExtendedTypeDefinition(
                "System.IO.DirectoryInfo",
                ViewsOf_FileSystemTypes(sharedControls));
            td1.TypeNames.Add("System.IO.FileInfo");
            yield return td1;

            yield return new ExtendedTypeDefinition(
                "System.Security.AccessControl.FileSystemSecurity",
                ViewsOf_System_Security_AccessControl_FileSystemSecurity(sharedControls));

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.AlternateStreamData",
                ViewsOf_Microsoft_PowerShell_Commands_AlternateStreamData());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_FileSystemTypes(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("children",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .AddHeader(Alignment.Left, label: "Mode", width: 7)
                    .AddHeader(Alignment.Right, label: "LastWriteTime", width: 25)
                    .AddHeader(Alignment.Right, label: "Length", width: 14)
                    .AddHeader(Alignment.Left, label: "Name")
                    .StartRowDefinition(wrap: true)
                        .AddPropertyColumn("ModeWithoutHardLink")
                        .AddPropertyColumn("LastWriteTimeString")
                        .AddPropertyColumn("LengthString")
                        .AddPropertyColumn("NameString")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("childrenWithHardlink",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .AddHeader(Alignment.Left, label: "Mode", width: 7)
                    .AddHeader(Alignment.Right, label: "LastWriteTime", width: 25)
                    .AddHeader(Alignment.Right, label: "Length", width: 14)
                    .AddHeader(Alignment.Left, label: "Name")
                    .StartRowDefinition(wrap: true)
                        .AddPropertyColumn("Mode")
                        .AddPropertyColumn("LastWriteTimeString")
                        .AddPropertyColumn("LengthString")
                        .AddPropertyColumn("NameString")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("children",
                ListControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .StartEntry(entrySelectedByType: new[] { "System.IO.FileInfo" })
                        .AddItemProperty(@"Name")
                        .AddItemProperty("LengthString", label: "Length")
                        .AddItemProperty(@"CreationTime")
                        .AddItemProperty(@"LastWriteTime")
                        .AddItemProperty(@"LastAccessTime")
                        .AddItemProperty(@"Mode")
                        .AddItemProperty(@"LinkType")
                        .AddItemProperty(@"Target")
                        .AddItemProperty(@"VersionInfo")
                    .EndEntry()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"CreationTime")
                        .AddItemProperty(@"LastWriteTime")
                        .AddItemProperty(@"LastAccessTime")
                        .AddItemProperty(@"Mode")
                        .AddItemProperty(@"LinkType")
                        .AddItemProperty(@"Target")
                    .EndEntry()
                .EndList());

            yield return new FormatViewDefinition("children",
                WideControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .AddPropertyEntry("Name")
                    .AddPropertyEntry("Name", format: "[{0}]", entrySelectedByType: new[] { "System.IO.DirectoryInfo" })
                .EndWideControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Security_AccessControl_FileSystemSecurity(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("FileSecurityTable",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .AddHeader(label: "Path")
                    .AddHeader()
                    .AddHeader(label: "Access")
                    .StartRowDefinition()
                        .AddScriptBlockColumn(@"
                                    split-path $_.Path -leaf
                                ")
                        .AddPropertyColumn("Owner")
                        .AddScriptBlockColumn(@"
                                    $_.AccessToString
                                ")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_AlternateStreamData()
        {
            yield return new FormatViewDefinition("FileSystemStream",
                TableControl.Create()
                    .GroupByProperty("Filename")
                    .AddHeader(Alignment.Left, width: 20)
                    .AddHeader(Alignment.Right, width: 10)
                    .StartRowDefinition()
                        .AddPropertyColumn("Stream")
                        .AddPropertyColumn("Length")
                    .EndRowDefinition()
                .EndTable());
        }
    }
}
