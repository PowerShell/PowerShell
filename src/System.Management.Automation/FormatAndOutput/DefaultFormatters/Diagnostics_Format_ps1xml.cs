// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Diagnostics_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSampleSet",
                ViewsOf_Microsoft_PowerShell_Commands_GetCounter_PerformanceCounterSampleSet());

            yield return new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.GetCounter.CounterFileInfo",
                ViewsOf_Microsoft_PowerShell_Commands_GetCounter_CounterFileInfo());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_GetCounter_PerformanceCounterSampleSet()
        {
            yield return new FormatViewDefinition("Counter",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "Timestamp", width: 26)
                    .AddHeader(Alignment.Left, label: "CounterSamples", width: 100)
                    .StartRowDefinition(wrap: true)
                        .AddPropertyColumn("Timestamp")
                        .AddPropertyColumn("Readings")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_GetCounter_CounterFileInfo()
        {
            yield return new FormatViewDefinition("Counter",
                TableControl.Create()
                    .AddHeader(Alignment.Left, width: 30)
                    .AddHeader(Alignment.Left, width: 30)
                    .AddHeader(Alignment.Left, width: 30)
                    .StartRowDefinition(wrap: true)
                        .AddPropertyColumn("OldestRecord")
                        .AddPropertyColumn("NewestRecord")
                        .AddPropertyColumn("SampleCount")
                    .EndRowDefinition()
                .EndTable());
        }
    }
}
