// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Registry_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var Registry_GroupingFormat = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddText("    Hive: ")
                            .AddScriptBlockExpressionBinding(@"$_.PSParentPath.Replace(""Microsoft.PowerShell.Core\Registry::"", """")")
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                Registry_GroupingFormat
            };

            var td1 = new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.Internal.TransactedRegistryKey",
                ViewsOf_Microsoft_PowerShell_Commands_Internal_TransactedRegistryKey_Microsoft_Win32_RegistryKey_System_Management_Automation_TreatAs_RegistryValue(sharedControls));
            td1.TypeNames.Add("Microsoft.Win32.RegistryKey");
            td1.TypeNames.Add("System.Management.Automation.TreatAs.RegistryValue");
            yield return td1;
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_PowerShell_Commands_Internal_TransactedRegistryKey_Microsoft_Win32_RegistryKey_System_Management_Automation_TreatAs_RegistryValue(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("children",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", customControl: sharedControls[0])
                    .AddHeader(label: "Name", width: 30)
                    .AddHeader(label: "Property")
                    .StartRowDefinition(wrap: true)
                        .AddPropertyColumn("PSChildName")
                        .AddScriptBlockColumn(@"
                                  $result = (Get-ItemProperty -LiteralPath $_.PSPath |
                                      Select * -Exclude PSPath,PSParentPath,PSChildName,PSDrive,PsProvider |
                                      Format-List | Out-String | Sort).Trim()
                                  $result = $result.Substring(0, [Math]::Min($result.Length, 5000) )
                                  if($result.Length -eq 5000) { $result += ""`u{2026}"" }

                                  $result
                                ")
                    .EndRowDefinition()
                .EndTable());
        }
    }
}
