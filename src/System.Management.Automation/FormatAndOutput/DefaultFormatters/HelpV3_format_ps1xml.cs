// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Runspaces
{
    internal sealed class HelpV3_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var MamlTypeControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name", enumerateCollection: true)
                    .EndEntry()
                .EndControl();

            var MamlParameterValueControl = CustomControl.Create()
                    .StartEntry()
                        .AddText(" ")
                        .AddScriptBlockExpressionBinding(@"""[""", selectedByScript: @"$_.required -ne ""true""")
                        .AddScriptBlockExpressionBinding(@"""<"" + $_ + "">""")
                        .AddScriptBlockExpressionBinding(@"""]""", selectedByScript: @"$_.required -ne ""true""")
                    .EndEntry()
                .EndControl();

            var control1 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Value")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var MamlPossibleValueControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValue", enumerateCollection: true, customControl: control1)
                    .EndEntry()
                .EndControl();

            var MamlShortDescriptionControl = CustomControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "MamlParaTextItem" })
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                    .StartEntry()
                        .AddText(" ")
                    .EndEntry()
                .EndControl();

            var MamlDescriptionControl = CustomControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "MamlParaTextItem" })
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlOrderedListTextItem" })
                        .StartFrame(firstLineHanging: 4)
                            .AddPropertyExpressionBinding(@"Tag")
                            .AddPropertyExpressionBinding(@"Text")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlUnorderedListTextItem" })
                        .StartFrame(firstLineHanging: 2)
                            .AddPropertyExpressionBinding(@"Tag")
                            .AddPropertyExpressionBinding(@"Text")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlDefinitionTextItem" })
                        .AddPropertyExpressionBinding(@"Term")
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Definition")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                    .StartEntry()
                        .AddText(" ")
                    .EndEntry()
                .EndControl();

            var MamlFieldCustomControl = CustomControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "MamlPSClassHelpInfo#field" })
                        .AddScriptBlockExpressionBinding(@"""["" + $_.fieldData.type.name + ""] $"" + $_.fieldData.name")
                        .AddNewline()
                        .AddScriptBlockExpressionBinding(@"$_.introduction.text")
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                    .StartEntry()
                        .AddText(" ")
                    .EndEntry()
                .EndControl();

            var MamlMethodCustomControl = CustomControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "MamlPSClassHelpInfo#method" })
                        .StartFrame()
                            .AddNewline()
                            .AddScriptBlockExpressionBinding(@"function GetParam
{
    if(-not $_.Parameters) { return $null }

    $_.Parameters.Parameter | ForEach-Object {
        if($_.type) { $param = ""[$($_.type.name)] `$$($_.name), "" }
        else { $param = ""[object] `$$($_.name), "" }

        $params += $param
    }

    $params = $params.Remove($params.Length - 2)
    return $params
}

$paramOutput = GetParam
""["" + $_.returnValue.type.name + ""] "" + $_.title + ""("" + $($paramOutput) + "")""")
                            .AddNewline()
                            .AddScriptBlockExpressionBinding(@"$_.introduction.text")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                    .StartEntry()
                        .AddText(" ")
                    .EndEntry()
                .EndControl();

            var RelatedLinksHelpInfoControl = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddScriptBlockExpressionBinding(StringUtil.Format(@"Set-StrictMode -Off
if (($_.relatedLinks -ne $()) -and ($_.relatedLinks.navigationLink -ne $()) -and ($_.relatedLinks.navigationLink.Length -ne 0))
{{
    ""    {0}`""get-help $($_.Details.Name) -online`""""
}}", HelpDisplayStrings.RelatedLinksHelpInfo))
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var MamlParameterControl = CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(
@"$optional = $_.required -ne 'true'
$positional = (($_.position -ne $()) -and ($_.position -ne '') -and ($_.position -notmatch 'named') -and ([int]$_.position -ne $()))
$parameterValue = if ($null -ne $_.psobject.Members['ParameterValueGroup']) {
    "" {$($_.ParameterValueGroup.ParameterValue -join ' | ')}""
} elseif ($null -ne $_.psobject.Members['ParameterValue']) {
    "" <$($_.ParameterValue)>""
} else {
    ''
}
$(if ($optional -and $positional) { '[[-{0}]{1}] ' }
elseif ($optional)   { '[-{0}{1}] ' }
elseif ($positional) { '[-{0}]{1} ' }
else                 { '-{0}{1} ' }) -f $_.Name, $parameterValue")
                    .EndEntry()
                .EndControl();

            var control2 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Text")
                    .EndEntry()
                .EndControl();

            var MamlExampleControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Title")
                        .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: control2)
                        .AddPropertyExpressionBinding(@"Code", enumerateCollection: true)
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"results")
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"remarks", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                null,//MamlParameterValueGroupControl,
                MamlParameterControl,
                MamlTypeControl,
                MamlParameterValueControl,
                MamlPossibleValueControl,
                MamlShortDescriptionControl,
                MamlDescriptionControl,
                MamlExampleControl,
                MamlFieldCustomControl,
                MamlMethodCustomControl,
                RelatedLinksHelpInfoControl
            };

            yield return new ExtendedTypeDefinition(
                "ExtendedCmdletHelpInfo",
                ViewsOf_ExtendedCmdletHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "ExtendedCmdletHelpInfo#DetailedView",
                ViewsOf_ExtendedCmdletHelpInfo_DetailedView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "ExtendedCmdletHelpInfo#FullView",
                ViewsOf_ExtendedCmdletHelpInfo_FullView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "ExtendedCmdletHelpInfo#ExamplesView",
                ViewsOf_ExtendedCmdletHelpInfo_ExamplesView());

            yield return new ExtendedTypeDefinition(
                "ExtendedCmdletHelpInfo#parameter",
                ViewsOf_ExtendedCmdletHelpInfo_parameter(sharedControls));

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.VerboseRecord",
                ViewsOf_System_Management_Automation_VerboseRecord());

            yield return new ExtendedTypeDefinition(
                "Deserialized.System.Management.Automation.VerboseRecord",
                ViewsOf_Deserialized_System_Management_Automation_VerboseRecord());

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.DebugRecord",
                ViewsOf_System_Management_Automation_DebugRecord());

            yield return new ExtendedTypeDefinition(
                "Deserialized.System.Management.Automation.DebugRecord",
                ViewsOf_Deserialized_System_Management_Automation_DebugRecord());

            yield return new ExtendedTypeDefinition(
                "DscResourceHelpInfo#FullView",
                ViewsOf_DscResourceHelpInfo_FullView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "DscResourceHelpInfo#DetailedView",
                ViewsOf_DscResourceHelpInfo_DetailedView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "DscResourceHelpInfo",
                ViewsOf_DscResourceHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "PSClassHelpInfo#FullView",
                ViewsOf_PSClassHelpInfo_FullView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "PSClassHelpInfo#DetailedView",
                ViewsOf_PSClassHelpInfo_DetailedView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "PSClassHelpInfo",
                ViewsOf_PSClassHelpInfo(sharedControls));
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ExtendedCmdletHelpInfo(CustomControl[] sharedControls)
        {
            var control7 = CustomControl.Create()
                    .StartEntry()
                        .AddText(StringUtil.Format("[{0}]", HelpDisplayStrings.CommonParameters))
                    .EndEntry()
                .EndControl();

            var control5 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddText(" ")
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: sharedControls[1])
                        .AddScriptBlockExpressionBinding(@" ", selectedByScript: "$_.CommonParameters -eq $true", customControl: control7)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control4 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"SyntaxItem", enumerateCollection: true, customControl: control5)
                    .EndEntry()
                .EndControl();

            var control3 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ReducedDefaultCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control3)
                        .AddText(HelpDisplayStrings.Syntax)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Syntax", customControl: control4)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.AliasesSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Aliases")
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Remarks")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ExtendedCmdletHelpInfo_DetailedView(CustomControl[] sharedControls)
        {
            var control17 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.None)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control16 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.CommonParameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.BaseCmdletInformation)
                        .EndFrame()
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control14 = CustomControl.Create()
                    .StartEntry()
                        .AddText("-")
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[3])
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control13 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: control14)
                    .EndEntry()
                .EndControl();

            var control12 = CustomControl.Create()
                    .StartEntry()
                        .AddText("[")
                        .AddText(HelpDisplayStrings.CommonParameters)
                        .AddText("]")
                    .EndEntry()
                .EndControl();

            var control10 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddText(" ")
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: sharedControls[1])
                        .AddScriptBlockExpressionBinding(@" ", selectedByScript: "$_.CommonParameters -eq $true", customControl: control12)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control9 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"SyntaxItem", enumerateCollection: true, customControl: control10)
                    .EndEntry()
                .EndControl();

            var control8 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ReducedVerboseCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control8)
                        .AddText(HelpDisplayStrings.Syntax)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Syntax", customControl: control9)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Parameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Parameters", customControl: control13)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: "$_.CommonParameters -eq $true", customControl: control16)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: "($_.CommonParameters -eq $false) -and ($_.parameters.parameter.count -eq 0)", customControl: control17)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.AliasesSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Aliases")
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Remarks")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ExtendedCmdletHelpInfo_FullView(CustomControl[] sharedControls)
        {
            var control35 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"type", customControl: sharedControls[2])
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control34 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"ReturnValue", enumerateCollection: true, customControl: control35)
                    .EndEntry()
                .EndControl();

            var control33 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"type", customControl: sharedControls[2])
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control32 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InputType", enumerateCollection: true, customControl: control33)
                    .EndEntry()
                .EndControl();

            var control31 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.None)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control30 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.CommonParameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.BaseCmdletInformation)
                        .EndFrame()
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control28 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.NamedParameter)
                    .EndEntry()
                .EndControl();

            var control27 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.FalseShort)
                    .EndEntry()
                .EndControl();

            var control26 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TrueShort)
                    .EndEntry()
                .EndControl();

            var control25 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValues", customControl: sharedControls[4])
                    .EndEntry()
                .EndControl();

            var control24 = CustomControl.Create()
                    .StartEntry()
                        .AddText("-")
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[3])
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", selectedByScript: "$_.description -ne $()")
                            .AddCustomControlExpressionBinding(control25, selectedByScript: "$_.possibleValues -ne $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterRequired)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""true"")", customControl: control26)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""false"")", customControl: control27)
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterPosition)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"($_.position -eq  $()) -or ($_.position -eq """")", customControl: control28)
                            .AddScriptBlockExpressionBinding(@"$_.position", selectedByScript: "$_.position  -ne  $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.AcceptsPipelineInput)
                            .AddPropertyExpressionBinding(@"pipelineInput")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterSetName)
                            .AddPropertyExpressionBinding(@"parameterSetName")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterAliases)
                            .AddPropertyExpressionBinding(@"aliases")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterIsDynamic)
                            .AddPropertyExpressionBinding(@"isDynamic")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control23 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: control24)
                    .EndEntry()
                .EndControl();

            var control22 = CustomControl.Create()
                    .StartEntry()
                        .AddText("[")
                        .AddText(HelpDisplayStrings.CommonParameters)
                        .AddText("]")
                    .EndEntry()
                .EndControl();

            var control20 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddText(" ")
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: sharedControls[1])
                        .AddScriptBlockExpressionBinding(@" ", selectedByScript: "$_.CommonParameters -eq $true", customControl: control22)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control19 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"SyntaxItem", enumerateCollection: true, customControl: control20)
                    .EndEntry()
                .EndControl();

            var control18 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ReducedFullCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control18)
                        .AddText(HelpDisplayStrings.Syntax)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Syntax", customControl: control19)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Parameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Parameters", customControl: control23)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: "$_.CommonParameters -eq $true", customControl: control30)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: "($_.CommonParameters -eq $false) -and ($_.parameters.parameter.count -eq 0)", customControl: control31)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.InputType)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"InputTypes", customControl: control32)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.ReturnType)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"ReturnValues", customControl: control34)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.AliasesSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Aliases")
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Remarks")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ExtendedCmdletHelpInfo_ExamplesView()
        {
            var control36 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ReducedExamplesCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control36)
                        .AddNewline()
                        .AddText(HelpDisplayStrings.AliasesSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Aliases")
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Remarks")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ExtendedCmdletHelpInfo_parameter(CustomControl[] sharedControls)
        {
            var control41 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.NamedParameter)
                    .EndEntry()
                .EndControl();

            var control40 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.FalseShort)
                    .EndEntry()
                .EndControl();

            var control39 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TrueShort)
                    .EndEntry()
                .EndControl();

            var control38 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValues", customControl: sharedControls[4])
                    .EndEntry()
                .EndControl();

            var control37 = CustomControl.Create()
                    .StartEntry()
                        .AddText("-")
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[3])
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", selectedByScript: "$_.description -ne $()")
                            .AddCustomControlExpressionBinding(control38, selectedByScript: "$_.possibleValues -ne $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterRequired)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""true"")", customControl: control39)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""false"")", customControl: control40)
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterPosition)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"($_.position -eq  $()) -or ($_.position -eq """")", customControl: control41)
                            .AddScriptBlockExpressionBinding(@"$_.position", selectedByScript: "$_.position  -ne  $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.AcceptsPipelineInput)
                            .AddPropertyExpressionBinding(@"pipelineInput")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterSetName)
                            .AddPropertyExpressionBinding(@"parameterSetName")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterAliases)
                            .AddPropertyExpressionBinding(@"aliases")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterIsDynamic)
                            .AddPropertyExpressionBinding(@"isDynamic")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ReducedCommandHelpParameterView",
                CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(control37, enumerateCollection: true)
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_VerboseRecord()
        {
            yield return new FormatViewDefinition("VerboseRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_System_Management_Automation_VerboseRecord()
        {
            yield return new FormatViewDefinition("DeserializedVerboseRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InformationalRecord_Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_DebugRecord()
        {
            yield return new FormatViewDefinition("DebugRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Deserialized_System_Management_Automation_DebugRecord()
        {
            yield return new FormatViewDefinition("DeserializedDebugRecord",
                CustomControl.Create(outOfBand: true)
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InformationalRecord_Message")
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_DscResourceHelpInfo_FullView(CustomControl[] sharedControls)
        {
            var control50 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control49 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control50)
                    .EndEntry()
                .EndControl();

            var control48 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: sharedControls[7])
                    .EndEntry()
                .EndControl();

            var control47 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.FalseShort)
                    .EndEntry()
                .EndControl();

            var control46 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TrueShort)
                    .EndEntry()
                .EndControl();

            var control45 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValues", customControl: sharedControls[4])
                    .EndEntry()
                .EndControl();

            var control44 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[3])
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[6])
                            .AddCustomControlExpressionBinding(control45, selectedByScript: "$_.possibleValues -ne $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterRequired)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""true"")", customControl: control46)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"$_.required.ToLower().Equals(""false"")", customControl: control47)
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control43 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: control44)
                    .EndEntry()
                .EndControl();

            var control42 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("DscResourceHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control42)
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: sharedControls[6])
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Properties)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Properties", customControl: control43)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Examples)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: control48)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control49)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_DscResourceHelpInfo_DetailedView(CustomControl[] sharedControls)
        {
            var control56 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control55 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control56)
                    .EndEntry()
                .EndControl();

            var control54 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: sharedControls[7])
                    .EndEntry()
                .EndControl();

            var control53 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[3])
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[6])
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control52 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: control53)
                    .EndEntry()
                .EndControl();

            var control51 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("DscResourceHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control51)
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: sharedControls[6])
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Properties)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Properties", customControl: control52)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Examples)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: control54)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control55)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_DscResourceHelpInfo(CustomControl[] sharedControls)
        {
            var control59 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control58 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control59)
                    .EndEntry()
                .EndControl();

            var control57 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("DscResourceHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: control57)
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: sharedControls[6])
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control58)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_PSClassHelpInfo_FullView(CustomControl[] sharedControls)
        {
            var control68 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control67 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control68)
                    .EndEntry()
                .EndControl();

            var control66 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: sharedControls[7])
                    .EndEntry()
                .EndControl();

            var control65 = CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(sharedControls[9])
                    .EndEntry()
                .EndControl();

            var control64 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"member", enumerateCollection: true, customControl: control65)
                    .EndEntry()
                .EndControl();

            var control63 = CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(sharedControls[8])
                    .EndEntry()
                .EndControl();

            var control62 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"member", enumerateCollection: true, customControl: control63)
                    .EndEntry()
                .EndControl();

            var control61 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Properties)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"members", customControl: control62)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Methods)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"members", customControl: control64)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Examples)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: control66)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control67)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("PSClassHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(control61)
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_PSClassHelpInfo_DetailedView(CustomControl[] sharedControls)
        {
            var control77 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control76 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control77)
                    .EndEntry()
                .EndControl();

            var control75 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: sharedControls[7])
                    .EndEntry()
                .EndControl();

            var control74 = CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(sharedControls[9])
                    .EndEntry()
                .EndControl();

            var control73 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"member", enumerateCollection: true, customControl: control74)
                    .EndEntry()
                .EndControl();

            var control72 = CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(sharedControls[8])
                    .EndEntry()
                .EndControl();

            var control71 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"member", enumerateCollection: true, customControl: control72)
                    .EndEntry()
                .EndControl();

            var control70 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Properties)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"members", customControl: control71)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Methods)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"members", customControl: control73)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Examples)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: control75)
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control76)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Details.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("PSClassHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(control70)
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_PSClassHelpInfo(CustomControl[] sharedControls)
        {
            var control81 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding(@""" """, selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control80 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control81)
                    .EndEntry()
                .EndControl();

            var control79 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: sharedControls[5])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("PSClassHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(control79)
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: control80)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Name + "" -examples""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Name + "" -detailed""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""get-help "" + $_.Name + "" -full""")
                            .AddText(@""".")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[10])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }
    }
}
