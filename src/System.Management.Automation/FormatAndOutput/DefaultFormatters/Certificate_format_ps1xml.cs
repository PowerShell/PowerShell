// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Certificate_Format_Ps1Xml
    {
        internal static IEnumerable<ExtendedTypeDefinition> GetFormatData()
        {
            var SignatureTypes_GroupingFormat = CustomControl.Create()
                    .StartEntry()
                        .StartFrame()
                            .AddText(FileSystemProviderStrings.DirectoryDisplayGrouping)
                            .AddScriptBlockExpressionBinding(@"split-path $_.Path")
                        .EndFrame()
                    .EndEntry()
                .EndControl();

            var sharedControls = new CustomControl[] {
                SignatureTypes_GroupingFormat
            };

            yield return new ExtendedTypeDefinition(
                "System.Security.Cryptography.X509Certificates.X509Certificate2",
                ViewsOf_System_Security_Cryptography_X509Certificates_X509Certificate2());

            var td2 = new ExtendedTypeDefinition(
                "Microsoft.PowerShell.Commands.X509StoreLocation",
                ViewsOf_CertificateProviderTypes());
            td2.TypeNames.Add("System.Security.Cryptography.X509Certificates.X509Certificate2");
            td2.TypeNames.Add("System.Security.Cryptography.X509Certificates.X509Store");
            yield return td2;

            yield return new ExtendedTypeDefinition(
                "System.Management.Automation.Signature",
                ViewsOf_System_Management_Automation_Signature(sharedControls));

            yield return new ExtendedTypeDefinition(
                "System.Security.Cryptography.X509Certificates.X509CertificateEx",
                ViewsOf_System_Security_Cryptography_X509Certificates_X509CertificateEx());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Security_Cryptography_X509Certificates_X509Certificate2()
        {
            yield return new FormatViewDefinition("ThumbprintTable",
                TableControl.Create()
                    .GroupByProperty("PSParentPath")
                    .AddHeader(width: 41)
                    .AddHeader(width: 20)
                    .AddHeader(label: "EnhancedKeyUsageList")
                    .StartRowDefinition()
                        .AddPropertyColumn("Thumbprint")
                        .AddPropertyColumn("Subject")
                        .AddScriptBlockColumn("$_.EnhancedKeyUsageList.FriendlyName")
                    .EndRowDefinition()
                .EndTable());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_CertificateProviderTypes()
        {
            yield return new FormatViewDefinition("ThumbprintList",
                ListControl.Create()
                    .StartEntry(entrySelectedByType: new[] { "Microsoft.PowerShell.Commands.X509StoreLocation" })
                        .AddItemProperty(@"Location")
                        .AddItemProperty(@"StoreNames")
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "System.Security.Cryptography.X509Certificates.X509Store" })
                        .AddItemProperty(@"Name")
                    .EndEntry()
                    .StartEntry()
                        .AddItemScriptBlock(@"$_.SubjectName.Name", label: "Subject")
                        .AddItemScriptBlock(@"$_.IssuerName.Name", label: "Issuer")
                        .AddItemProperty(@"Thumbprint")
                        .AddItemProperty(@"FriendlyName")
                        .AddItemProperty(@"NotBefore")
                        .AddItemProperty(@"NotAfter")
                        .AddItemProperty(@"Extensions")
                    .EndEntry()
                .EndList());

            yield return new FormatViewDefinition("ThumbprintWide",
                WideControl.Create()
                    .GroupByProperty("PSParentPath")
                    .AddPropertyEntry("Thumbprint")
                .EndWideControl());

            yield return new FormatViewDefinition("PathOnly",
                ListControl.Create()
                    .StartEntry()
                        .AddItemProperty(@"PSPath")
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "Microsoft.PowerShell.Commands.X509StoreLocation" })
                        .AddItemProperty(@"PSPath")
                    .EndEntry()
                    .StartEntry(entrySelectedByType: new[] { "System.Security.Cryptography.X509Certificates.X509Store" })
                        .AddItemProperty(@"PSPath")
                    .EndEntry()
                .EndList());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Management_Automation_Signature(CustomControl[] sharedControls)
        {
            yield return new FormatViewDefinition("PSThumbprintTable",
                TableControl.Create()
                    .GroupByScriptBlock("split-path $_.Path", customControl: sharedControls[0])
                    .AddHeader(label: "SignerCertificate", width: 41)
                    .AddHeader()
                    .AddHeader()
                    .AddHeader(label: "Path")
                    .StartRowDefinition()
                        .AddScriptBlockColumn("$_.SignerCertificate.Thumbprint")
                        .AddPropertyColumn("Status")
                        .AddPropertyColumn("StatusMessage")
                        .AddScriptBlockColumn("split-path $_.Path -leaf")
                    .EndRowDefinition()
                .EndTable());

            yield return new FormatViewDefinition("PSThumbprintWide",
                WideControl.Create()
                    .GroupByScriptBlock("split-path $_.Path", customControl: sharedControls[0])
                    .AddScriptBlockEntry(@"""$(split-path $_.Path -leaf): $($_.Status)""")
                .EndWideControl());
        }

        private static IEnumerable<FormatViewDefinition> ViewsOf_System_Security_Cryptography_X509Certificates_X509CertificateEx()
        {
            yield return new FormatViewDefinition("System.Security.Cryptography.X509Certificates.X509CertificateEx",
                TableControl.Create()
                    .GroupByProperty("PSParentPath")
                    .AddHeader(width: 41)
                    .AddHeader()
                    .StartRowDefinition()
                        .AddPropertyColumn("Thumbprint")
                        .AddPropertyColumn("Subject")
                    .EndRowDefinition()
                .EndTable());
        }
    }
}
