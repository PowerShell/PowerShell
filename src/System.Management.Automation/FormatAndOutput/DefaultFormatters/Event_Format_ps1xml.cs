// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Event_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.Eventing.Reader.EventLogRecord",
                ViewsOf_System_Diagnostics_Eventing_Reader_EventLogRecord());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.Eventing.Reader.EventLogConfiguration",
                ViewsOf_System_Diagnostics_Eventing_Reader_EventLogConfiguration());

            yield return new ExtendedTypeDefinition(
                "System.Diagnostics.Eventing.Reader.ProviderMetadata",
                ViewsOf_System_Diagnostics_Eventing_Reader_ProviderMetadata());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_Eventing_Reader_EventLogRecord()
        {
            yield return new FormatViewDefinition("Default",
                TableControl.Create()
                    .GroupByProperty("ProviderName", label: "ProviderName")
                    .AddHeader(width: 26)
                    .AddHeader(Alignment.Right, width: 8)
                    .AddHeader(width: 16)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("TimeCreated")
                        .AddPropertyColumn("Id")
                        .AddPropertyColumn("LevelDisplayName")
                        .AddPropertyColumn("Message")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_Eventing_Reader_EventLogConfiguration()
        {
            yield return new FormatViewDefinition("Default",
                TableControl.Create()
                    .AddHeader(label: "LogMode", width: 9)
                    .AddHeader(Alignment.Right, label: "MaximumSizeInBytes", width: 18)
                    .AddHeader(Alignment.Right, label: "RecordCount", width: 11)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("LogMode")
                        .AddPropertyColumn("MaximumSizeInBytes")
                        .AddPropertyColumn("RecordCount")
                        .AddPropertyColumn("LogName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Diagnostics_Eventing_Reader_ProviderMetadata()
        {
            yield return new FormatViewDefinition("Default",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"LogLinks")
                        .AddItemProperty(@"Opcodes")
                        .AddItemProperty(@"Tasks")
                    .EndEntry()
                .EndList());
        }
    }
}
