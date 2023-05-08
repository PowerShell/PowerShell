// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class PowerShellTrace_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.PSTraceSource",
                ViewsOf_System_Management_Automation_PSTraceSource());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_PSTraceSource()
        {
            yield return new FormatViewDefinition("System.Management.Automation.PSTraceSource",
                TableControl.Create()
                    .AddHeader(width: 8)
                    .AddHeader(width: 20)
                    .AddHeader(width: 20)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Options")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Listeners")
                        .AddPropertyColumn("Description")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("System.Management.Automation.PSTraceSource",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"Name")
                        .AddItemProperty(@"Description")
                        .AddItemProperty(@"Options")
                        .AddItemProperty(@"Listeners")
                        .AddItemProperty(@"Attributes")
                        .AddItemProperty(@"Switch")
                    .EndEntry()
                .EndList());
        }
    }
}
