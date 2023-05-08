// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class WSMan_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            yield return new ExtendedTypeDefinition(
                "System.Xml.XmlElement#http://schemas.dmtf.org/wbem/wsman/identity/1/wsmanidentity.xsd#IdentifyResponse",
                ViewsOf_System_Xml_XmlElement_http___schemas_dmtf_org_wbem_wsman_identity_1_wsmanidentity_xsd_IdentifyResponse());

            yield return new ExtendedTypeDefinition(
                "Microsoft.WSMan.Management.WSManConfigElement",
                ViewsOf_Microsoft_WSMan_Management_WSManConfigElement());

            yield return new ExtendedTypeDefinition(
                "Microsoft.WSMan.Management.WSManConfigContainerElement",
                ViewsOf_Microsoft_WSMan_Management_WSManConfigContainerElement());

            yield return new ExtendedTypeDefinition(
                "Microsoft.WSMan.Management.WSManConfigLeafElement",
                ViewsOf_Microsoft_WSMan_Management_WSManConfigLeafElement());

            yield return new ExtendedTypeDefinition(
                "Microsoft.WSMan.Management.WSManConfigLeafElement#InitParams",
                ViewsOf_Microsoft_WSMan_Management_WSManConfigLeafElement_InitParams());

            yield return new ExtendedTypeDefinition(
                "Microsoft.WSMan.Management.WSManConfigContainerElement#ComputerLevel",
                ViewsOf_Microsoft_WSMan_Management_WSManConfigContainerElement_ComputerLevel());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Xml_XmlElement_http___schemas_dmtf_org_wbem_wsman_identity_1_wsmanidentity_xsd_IdentifyResponse()
        {
            yield return new FormatViewDefinition("System.Xml.XmlElement#http://schemas.dmtf.org/wbem/wsman/identity/1/wsmanidentity.xsd#IdentifyResponse",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"wsmid")
                        .AddItemProperty(@"ProtocolVersion")
                        .AddItemProperty(@"ProductVendor")
                        .AddItemProperty(@"ProductVersion")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_WSMan_Management_WSManConfigElement()
        {
            yield return new FormatViewDefinition("Microsoft.WSMan.Management.WSManConfigElement",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", label: "WSManConfig")
                    .AddHeader(label: "Type", width: 15)
                    .AddHeader(label: "Name", width: 30)
                    .StartRowDefinition()
                        .AddPropertyColumn("TypeNameOfElement")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_WSMan_Management_WSManConfigContainerElement()
        {
            yield return new FormatViewDefinition("Microsoft.WSMan.Management.WSManConfigContainerElement",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", label: "WSManConfig")
                    .AddHeader(label: "Type", width: 15)
                    .AddHeader(label: "Keys", width: 35)
                    .AddHeader(label: "Name")
                    .StartRowDefinition()
                        .AddPropertyColumn("TypeNameOfElement")
                        .AddPropertyColumn("Keys")
                        .AddPropertyColumn("Name")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_WSMan_Management_WSManConfigLeafElement()
        {
            yield return new FormatViewDefinition("Microsoft.WSMan.Management.WSManConfigLeafElement",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", label: "WSManConfig")
                    .AddHeader(label: "Type", width: 15)
                    .AddHeader(label: "Name", width: 30)
                    .AddHeader(label: "SourceOfValue", width: 15)
                    .AddHeader(label: "Value")
                    .StartRowDefinition()
                        .AddPropertyColumn("TypeNameOfElement")
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("SourceOfValue")
                        .AddPropertyColumn("Value")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_WSMan_Management_WSManConfigLeafElement_InitParams()
        {
            yield return new FormatViewDefinition("Microsoft.WSMan.Management.WSManConfigLeafElement#InitParams",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", label: "WSManConfig")
                    .AddHeader(label: "ParamName", width: 30)
                    .AddHeader(label: "ParamValue", width: 20)
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("Value")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_Microsoft_WSMan_Management_WSManConfigContainerElement_ComputerLevel()
        {
            yield return new FormatViewDefinition("Microsoft.WSMan.Management.WSManConfigContainerElement#ComputerLevel",
                TableControl.Create()
                    .GroupByProperty("PSParentPath", label: "WSManConfig")
                    .AddHeader(label: "ComputerName", width: 45)
                    .AddHeader(label: "Type", width: 20)
                    .StartRowDefinition()
                        .AddPropertyColumn("Name")
                        .AddPropertyColumn("TypeNameOfElement")
                    .EndRowDefinition()
                .EndTable());
        }
    }
}
