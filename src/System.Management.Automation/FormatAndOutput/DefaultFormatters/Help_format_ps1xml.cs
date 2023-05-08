// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Help_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var TextPropertyControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Text")
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

            var MamlParameterValueControl = CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"if ($_.required -ne 'true') { "" [<$_>]"" } else { "" <$_>"" }")
                    .EndEntry()
                .EndControl();

            var MamlTextItem = CustomControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "MamlParaTextItem" })
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlOrderedListTextItem" })
                        .AddPropertyExpressionBinding(@"Tag")
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlUnorderedListTextItem" })
                        .AddPropertyExpressionBinding(@"Tag")
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlDefinitionTextItem" })
                        .AddPropertyExpressionBinding(@"Term")
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Definition")
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "MamlPreformattedTextItem" })
                        .AddPropertyExpressionBinding(@"Text")
                    .EndEntry()
                    .StartEntry()
                        .AddText(" ")
                    .EndEntry()
                .EndControl();

            var MamlAlertControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Text")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control4 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.FalseShort)
                    .EndEntry()
                .EndControl();

            var control3 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TrueShort)
                    .EndEntry()
                .EndControl();

            var MamlTrueFalseShortControl = CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@";", selectedByScript: @"$_.Equals('true', [System.StringComparison]::OrdinalIgnoreCase)", customControl: control3)
                        .AddScriptBlockExpressionBinding(@";", selectedByScript: @"$_.Equals('false', [System.StringComparison]::OrdinalIgnoreCase)", customControl: control4)
                    .EndEntry()
                .EndControl();

            var RelatedLinksHelpInfoControl = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddScriptBlockExpressionBinding(StringUtil.Format(@"Set-StrictMode -Off
if (($_.relatedLinks -ne $()) -and ($_.relatedLinks.navigationLink -ne $()) -and ($_.relatedLinks.navigationLink.Length -ne 0))
{{
    ""    {0}`""Get-Help $($_.Details.Name) -Online`""""
}}", HelpDisplayStrings.RelatedLinksHelpInfo))
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control6 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"linkText")
                        .AddScriptBlockExpressionBinding("' '", selectedByScript: "$_.linkText.Length -ne 0")
                        .AddPropertyExpressionBinding(@"uri")
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var MamlRelatedLinksControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"navigationLink", enumerateCollection: true, customControl: control6)
                    .EndEntry()
                .EndControl();

            var MamlDetailsControl = CustomControl.Create()
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
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var MamlExampleControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: TextPropertyControl)
                        .AddPropertyExpressionBinding(@"Code", enumerateCollection: true)
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"results")
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"remarks", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                    .EndEntry()
                .EndControl();

            var control1 = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control0 = CustomControl.Create()
                    .StartEntry()
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"uri")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var MamlTypeControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name", enumerateCollection: true)
                        .AddCustomControlExpressionBinding(control0, selectedByScript: "$_.uri")
                        .AddCustomControlExpressionBinding(control1, selectedByScript: "$_.description")
                    .EndEntry()
                .EndControl();

            var control2 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Value")
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var MamlPossibleValueControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValue", enumerateCollection: true, customControl: control2)
                    .EndEntry()
                .EndControl();

            var MamlIndentedDescriptionControl = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: MamlDescriptionControl)
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control5 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"name")
                        .AddText(" ")
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: MamlParameterControl)
                        .AddText("[" + HelpDisplayStrings.CommonParameters + "]")
                        .AddNewline(2)
                    .EndEntry()
                .EndControl();

            var MamlSyntaxControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"SyntaxItem", enumerateCollection: true, customControl: control5)
                    .EndEntry()
                .EndControl();

            var ExamplesControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: MamlExampleControl)
                    .EndEntry()
                .EndControl();

            var MamlTypeWithDescriptionControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"type", customControl: MamlTypeControl)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                        .EndFrame()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var ErrorControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"errorId")
                        .AddText(HelpDisplayStrings.Category)
                        .AddPropertyExpressionBinding(@"category")
                        .AddText(")")
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"description", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.TypeColon)
                        .AddPropertyExpressionBinding(@"type", customControl: MamlTypeControl)
                        .AddNewline()
                        .AddText(HelpDisplayStrings.TargetObjectTypeColon)
                        .AddPropertyExpressionBinding(@"targetObjectType", customControl: MamlTypeControl)
                        .AddNewline()
                        .AddText(HelpDisplayStrings.SuggestedActionColon)
                        .AddPropertyExpressionBinding(@"recommendedAction", enumerateCollection: true, customControl: MamlShortDescriptionControl)
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var MamlPossibleValuesControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"possibleValues", customControl: MamlPossibleValueControl)
                    .EndEntry()
                .EndControl();

            var MamlIndentedSyntaxControl = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Syntax)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Syntax", customControl: MamlSyntaxControl)
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control7 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.NamedParameter)
                    .EndEntry()
                .EndControl();

            var MamlFullParameterControl = CustomControl.Create()
                    .StartEntry()
                        .AddText("-")
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: MamlParameterValueControl)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: MamlDescriptionControl)
                            .AddNewline()
                            .AddCustomControlExpressionBinding(MamlPossibleValuesControl, selectedByScript: "$_.possibleValues -ne $()")
                            .AddText(HelpDisplayStrings.ParameterRequired)
                            .AddPropertyExpressionBinding(@"required", customControl: MamlTrueFalseShortControl)
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterPosition)
                            .AddScriptBlockExpressionBinding(@" ", selectedByScript: @"($_.position -eq  $()) -or ($_.position -eq '')", customControl: control7)
                            .AddScriptBlockExpressionBinding(@"$_.position", selectedByScript: "$_.position  -ne  $()")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.ParameterDefaultValue)
                            .AddPropertyExpressionBinding(@"defaultValue")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.AcceptsPipelineInput)
                            .AddPropertyExpressionBinding(@"pipelineInput")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.AcceptsWildCardCharacters)
                            .AddPropertyExpressionBinding(@"globbing", customControl: MamlTrueFalseShortControl)
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control8 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: MamlFullParameterControl)
                    .EndEntry()
                .EndControl();

            var zzz = CustomControl.Create()
                    .StartEntry()
                        .AddCustomControlExpressionBinding(control8)
                        .AddText(HelpDisplayStrings.CommonParameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.BaseCmdletInformation)
                        .EndFrame()
                        .AddNewline()
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                TextPropertyControl,
                MamlShortDescriptionControl,
                MamlDetailsControl,
                MamlIndentedDescriptionControl,
                MamlDescriptionControl,
                MamlParameterControl,
                MamlParameterValueControl,
                ExamplesControl,
                MamlExampleControl,
                MamlTypeControl,
                MamlTextItem,
                MamlAlertControl,
                MamlPossibleValuesControl,
                MamlPossibleValueControl,
                MamlTrueFalseShortControl,
                MamlIndentedSyntaxControl,
                MamlSyntaxControl,
                MamlTypeWithDescriptionControl,
                RelatedLinksHelpInfoControl,
                MamlRelatedLinksControl,
                ErrorControl,
                MamlFullParameterControl,
                zzz
            };

            yield return new ExtendedTypeDefinition(
                "HelpInfoShort",
                ViewsOf_HelpInfoShort());

            yield return new ExtendedTypeDefinition(
                "CmdletHelpInfo",
                ViewsOf_CmdletHelpInfo());

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo",
                ViewsOf_MamlCommandHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#DetailedView",
                ViewsOf_MamlCommandHelpInfo_DetailedView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#ExamplesView",
                ViewsOf_MamlCommandHelpInfo_ExamplesView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#FullView",
                ViewsOf_MamlCommandHelpInfo_FullView(sharedControls));

            yield return new ExtendedTypeDefinition(
                "ProviderHelpInfo",
                ViewsOf_ProviderHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "FaqHelpInfo",
                ViewsOf_FaqHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "GeneralHelpInfo",
                ViewsOf_GeneralHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "GlossaryHelpInfo",
                ViewsOf_GlossaryHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "ScriptHelpInfo",
                ViewsOf_ScriptHelpInfo(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#Examples",
                ViewsOf_MamlCommandHelpInfo_Examples(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#Example",
                ViewsOf_MamlCommandHelpInfo_Example(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#commandDetails",
                ViewsOf_MamlCommandHelpInfo_commandDetails(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#Parameters",
                ViewsOf_MamlCommandHelpInfo_Parameters(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#Parameter",
                ViewsOf_MamlCommandHelpInfo_Parameter(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#Syntax",
                ViewsOf_MamlCommandHelpInfo_Syntax(sharedControls));

            var td18 = new ExtendedTypeDefinition(
                "MamlDefinitionTextItem",
                ViewsOf_MamlDefinitionTextItem_MamlOrderedListTextItem_MamlParaTextItem_MamlUnorderedListTextItem(sharedControls));
            td18.TypeNames.Add("MamlOrderedListTextItem");
            td18.TypeNames.Add("MamlParaTextItem");
            td18.TypeNames.Add("MamlUnorderedListTextItem");
            yield return td18;

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#inputTypes",
                ViewsOf_MamlCommandHelpInfo_inputTypes(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#nonTerminatingErrors",
                ViewsOf_MamlCommandHelpInfo_nonTerminatingErrors(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#terminatingErrors",
                ViewsOf_MamlCommandHelpInfo_terminatingErrors(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#relatedLinks",
                ViewsOf_MamlCommandHelpInfo_relatedLinks(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#returnValues",
                ViewsOf_MamlCommandHelpInfo_returnValues(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#alertSet",
                ViewsOf_MamlCommandHelpInfo_alertSet(sharedControls));

            yield return new ExtendedTypeDefinition(
                "MamlCommandHelpInfo#details",
                ViewsOf_MamlCommandHelpInfo_details(sharedControls));
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_HelpInfoShort()
        {
            yield return new FormatViewDefinition("help",
                TableControl.Create()
                    .AddHeader(Alignment.Left, label: "Name", width: 33)
                    .AddHeader(Alignment.Left, label: "Category", width: 9)
                    .AddHeader(Alignment.Left, label: "Module", width: 25)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Category")
                        .AddScriptBlockColumn("if ($null -ne $_.ModuleName) { $_.ModuleName } else {$_.PSSnapIn}")
                        .AddPropertyColumn("Synopsis")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_CmdletHelpInfo()
        {
            yield return new FormatViewDefinition("CmdletHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Name)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Syntax)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Syntax")
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("DefaultCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: sharedControls[2])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[15])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[3])
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: sharedControls[19])
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo + "\"")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Examples""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo + "\"")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Detailed""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Full""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[18])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_DetailedView(CustomControl[] sharedControls)
        {
            var control10 = CustomControl.Create()
                    .StartEntry()
                        .AddText("-")
                        .AddPropertyExpressionBinding(@"name")
                        .AddPropertyExpressionBinding(@"ParameterValue", customControl: sharedControls[6])
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[4])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control9 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Parameter", enumerateCollection: true, customControl: control10)
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("VerboseCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: sharedControls[2])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[15])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[3])
                        .AddText(HelpDisplayStrings.Parameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Parameters", customControl: control9)
                            .AddText(HelpDisplayStrings.CommonParameters)
                            .AddNewline()
                            .StartFrame(leftIndent: 4)
                                .AddText(HelpDisplayStrings.BaseCmdletInformation)
                            .EndFrame()
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: sharedControls[7])
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RemarksSection)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddText(HelpDisplayStrings.ExampleHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Examples""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.VerboseHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Detailed""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddText(HelpDisplayStrings.FullHelpInfo)
                            .AddText(@"""")
                            .AddScriptBlockExpressionBinding(@"""Get-Help "" + $_.Details.Name + "" -Full""")
                            .AddText(@"""")
                            .AddNewline()
                            .AddCustomControlExpressionBinding(sharedControls[18])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_ExamplesView(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("ExampleCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: sharedControls[2])
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: sharedControls[7])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_FullView(CustomControl[] sharedControls)
        {
            var control16 = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"title")
                            .AddNewline()
                            .AddNewline()
                            .StartFrame(leftIndent: 4)
                                .AddPropertyExpressionBinding(@"alert", enumerateCollection: true, customControl: sharedControls[11])
                            .EndFrame()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control15 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.Notes)
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control14 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.NonHyphenTerminatingErrors)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"nonTerminatingError", enumerateCollection: true, customControl: sharedControls[20])
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control13 = CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TerminatingErrors)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"terminatingError", enumerateCollection: true, customControl: sharedControls[20])
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var control12 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"ReturnValue", enumerateCollection: true, customControl: sharedControls[17])
                    .EndEntry()
                .EndControl();

            var control11 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InputType", enumerateCollection: true, customControl: sharedControls[17])
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("FullCommandHelp",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Details", customControl: sharedControls[2])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[15])
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[3])
                        .AddText(HelpDisplayStrings.Parameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Parameters", customControl: sharedControls[22])
                        .EndFrame()
                        .AddText(HelpDisplayStrings.InputType)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"InputTypes", customControl: control11)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.ReturnType)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"ReturnValues", customControl: control12)
                            .AddNewline()
                        .EndFrame()
                        .AddPropertyExpressionBinding(@"terminatingErrors", selectedByScript: @"
                      (($null -ne $_.terminatingErrors) -and
                      ($null -ne $_.terminatingErrors.terminatingError))
                    ", customControl: control13)
                        .AddPropertyExpressionBinding(@"nonTerminatingErrors", selectedByScript: @"
                      (($null -ne $_.nonTerminatingErrors) -and
                      ($null -ne $_.nonTerminatingErrors.nonTerminatingError))
                    ", customControl: control14)
                        .AddPropertyExpressionBinding(@"alertSet", selectedByScript: "$null -ne $_.alertSet", customControl: control15)
                        .AddPropertyExpressionBinding(@"alertSet", enumerateCollection: true, selectedByScript: "$null -ne $_.alertSet", customControl: control16)
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Examples", customControl: sharedControls[7])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: sharedControls[19])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ProviderHelpInfo(CustomControl[] sharedControls)
        {
            var TaskExampleControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: sharedControls[0])
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"Code", enumerateCollection: true)
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"results")
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"remarks", enumerateCollection: true, customControl: sharedControls[10])
                    .EndEntry()
                .EndControl();

            var DynamicPossibleValues = CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Value")
                            .AddNewline()
                        .EndFrame()
                        .StartFrame(leftIndent: 8)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var TaskExamplesControl = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Example", enumerateCollection: true, customControl: TaskExampleControl)
                    .EndEntry()
                .EndControl();

            var control18 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"PossibleValue", enumerateCollection: true, customControl: DynamicPossibleValues)
                    .EndEntry()
                .EndControl();

            var control17 = CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"""<"" + $_.Name + "">""")
                    .EndEntry()
                .EndControl();

            var DynamicParameterControl = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddText("-")
                            .AddPropertyExpressionBinding(@"Name")
                            .AddText(" ")
                            .AddPropertyExpressionBinding(@"Type", customControl: control17)
                            .AddNewline()
                        .EndFrame()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description")
                            .AddNewline()
                            .AddNewline()
                            .AddPropertyExpressionBinding(@"PossibleValues", customControl: control18)
                            .AddNewline()
                            .AddText(HelpDisplayStrings.CmdletsSupported)
                            .AddPropertyExpressionBinding(@"CmdletSupported")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var Task = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddText(HelpDisplayStrings.Task)
                            .AddPropertyExpressionBinding(@"Title")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Description", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                            .AddNewline()
                            .AddPropertyExpressionBinding(@"Examples", enumerateCollection: true, customControl: TaskExamplesControl)
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var ProviderTasks = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Task", enumerateCollection: true, customControl: Task)
                        .AddNewline()
                    .EndEntry()
                .EndControl();

            var control19 = CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"DynamicParameter", enumerateCollection: true, customControl: DynamicParameterControl)
                    .EndEntry()
                .EndControl();

            yield return new FormatViewDefinition("ProviderHelpInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.ProviderName)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Name")
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Drives)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Drives", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Synopsis)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Synopsis", enumerateCollection: true)
                            .AddNewline()
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"DetailedDescription", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Capabilities)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Capabilities", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Tasks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Tasks", enumerateCollection: true, customControl: ProviderTasks)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.DynamicParameters)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"DynamicParameters", customControl: control19)
                            .AddNewline()
                        .EndFrame()
                        .AddText(HelpDisplayStrings.Notes)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Notes")
                            .AddNewline()
                        .EndFrame()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.RelatedLinks)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"relatedLinks", customControl: sharedControls[19])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_FaqHelpInfo(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("FaqHelpInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TitleColon)
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddText(HelpDisplayStrings.QuestionColon)
                        .AddPropertyExpressionBinding(@"Question")
                        .AddNewline()
                        .AddText(HelpDisplayStrings.Answer)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Answer", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_GeneralHelpInfo(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("GeneralHelpInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TitleColon)
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddNewline()
                        .AddText(HelpDisplayStrings.DetailedDescription)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Content", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_GlossaryHelpInfo(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("GlossaryHelpInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TermColon)
                        .AddPropertyExpressionBinding(@"Name")
                        .AddNewline()
                        .AddText(HelpDisplayStrings.DefinitionColon)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Definition", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_ScriptHelpInfo(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("ScriptHelpInfo",
                CustomControl.Create()
                    .StartEntry()
                        .AddText(HelpDisplayStrings.TitleColon)
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddText(HelpDisplayStrings.ContentColon)
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"Content", enumerateCollection: true, customControl: sharedControls[10])
                            .AddNewline()
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_Examples(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandExamples",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[7])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_Example(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandExample",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"Title")
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"Introduction", enumerateCollection: true, customControl: sharedControls[0])
                        .AddPropertyExpressionBinding(@"Code", enumerateCollection: true)
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"results")
                        .AddNewline()
                        .AddNewline()
                        .AddPropertyExpressionBinding(@"remarks", enumerateCollection: true, customControl: sharedControls[1])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_commandDetails(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandDetails",
                CustomControl.Create()
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
                            .AddPropertyExpressionBinding(@"commandDescription", enumerateCollection: true, customControl: sharedControls[1])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_Parameters(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandParameters",
                CustomControl.Create()
                    .StartEntry()
                        .StartFrame(leftIndent: 4)
                            .AddCustomControlExpressionBinding(sharedControls[22])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_Parameter(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandParameterView",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[21])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_Syntax(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlCommandSyntax",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[16])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlDefinitionTextItem_MamlOrderedListTextItem_MamlParaTextItem_MamlUnorderedListTextItem(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlText",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[4])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_inputTypes(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlInputTypes",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"InputType", enumerateCollection: true, customControl: sharedControls[17])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_nonTerminatingErrors(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlNonTerminatingErrors",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"nonTerminatingError", enumerateCollection: true, customControl: sharedControls[20])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_terminatingErrors(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlTerminatingErrors",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"terminatingError", enumerateCollection: true, customControl: sharedControls[20])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_relatedLinks(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlRelatedLinks",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[19])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_returnValues(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlReturnTypes",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"ReturnValue", enumerateCollection: true, customControl: sharedControls[17])
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_alertSet(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlAlertSet",
                CustomControl.Create()
                    .StartEntry()
                        .AddPropertyExpressionBinding(@"title")
                        .AddNewline()
                        .AddNewline()
                        .StartFrame(leftIndent: 4)
                            .AddPropertyExpressionBinding(@"alert", enumerateCollection: true, customControl: sharedControls[11])
                        .EndFrame()
                    .EndEntry()
                .EndControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_MamlCommandHelpInfo_details(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("MamlDetails",
                CustomControl.Create()
                    .StartEntry()
                        .AddScriptBlockExpressionBinding(@"$_", customControl: sharedControls[2])
                    .EndEntry()
                .EndControl());
        }
    }
}
