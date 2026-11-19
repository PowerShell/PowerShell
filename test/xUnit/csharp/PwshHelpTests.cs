// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Globalization;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace PSTests.Parallel;

public class PwshHelpTests
{
    // SHA256 snapshots of UsageHelp + ExtendedHelp before the resource migration.
    // XML resource readers normalize line endings to LF; retain every other character.
    [Theory]
    [InlineData("", "596A43D44F669294FC6516C21254E044AEC7D4A7B5F8357C99FB40C8D3413DEA")]
    [InlineData("cs", "0F446E7B5FF584AF99964494A79612A8E51BD2C3C05159AEF8BF1D1452AD709E")]
    [InlineData("de", "C5B2F8FCA3C65178C9636886E48CBC7896495D1F98D25EA321B94A3D00C27F1B")]
    [InlineData("es", "78FF24BA96D51F9D6446B1B914EC100CE1C43B14C24BF7780D8112FDF9913149")]
    [InlineData("fr", "C2A171954764524E53CD462A1AF43B6F55953596A7BE2A600187C6867413699C")]
    [InlineData("it", "EA228E69BEFCA635487351976A843D14431535CB8617572D6ED2D776AE7809CE")]
    [InlineData("ja", "77FA7CB9398491582A52331BFC85066D3BF8F03DE040B38D888446233C733E1C")]
    [InlineData("ko", "03EC6CA054DF924BB3A64AE028DCB60183745F23B79CC435C156E1C7BD934DBF")]
    [InlineData("pl", "D7DFB493A29070A81EF4056ABA96D39086F63768678A9D8FC85501EBC54FA96B")]
    [InlineData("pt-BR", "E756AAAA2A663C433B0EC196D4FCC4AD851D447B931EBD6B53B0251C66ADCF0D")]
    [InlineData("ru", "DA02925C93F8D1D5C856F1B00EB2ED2BB0F9124B7EF6C0824A9A50EAA71059CC")]
    [InlineData("tr", "9EDDCB11F251A62995A02A9339A1B3B2F30B31F4E72990FE601C76D5A0A3655A")]
    [InlineData("zh-Hans", "79832A0B08508162AD4A853E9C3AE7DE67C79A14CEDDDD7D57EFA57D8C86D8F1")]
    [InlineData("zh-Hant", "EE49B938A8F823C5A3B80DE01678269D7E75FC8B4B4EB1B2F5ED4AA874761A24")]
    public void AssembledHelpPreservesExistingTranslations(string cultureName, string expectedHash)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        string help = PwshHelp.GetString("UsageHelp", culture) + PwshHelp.GetExtendedHelp(culture);
        Assert.Equal(expectedHash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(help.Replace("\r\n", "\n")))));
    }

    [Fact]
    public void AdvertisedOptionsHaveLocalizedHelp()
    {
        foreach (PwshStartupOption option in PwshStartupOptions.Options)
        {
            if (option.Advertised)
            {
                Assert.False(string.IsNullOrWhiteSpace(PwshHelp.GetString(option.ResourceKey)));
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("cs")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("pl")]
    [InlineData("pt-BR")]
    [InlineData("ru")]
    [InlineData("tr")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    public void OptionTooltipsAreConciseDescriptions(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        foreach (PwshStartupOption option in PwshStartupOptions.Options)
        {
            if (option.Advertised)
            {
                string summary = PwshHelp.GetString(option.ResourceKey, culture);
                Assert.InRange(summary.Split('\n').Length, 3, 8);
                Assert.NotNull(PwshHelp.GetString(option.ResourceKey + "Details", culture));
            }
        }

        foreach (string name in new[] { "Command", "File", "CommandWithArgs" })
        {
            string summary = PwshHelp.GetString("Option" + name, culture);
            string details = PwshHelp.GetString("Option" + name + "Details", culture);
            Assert.False(string.IsNullOrWhiteSpace(details));
            Assert.True(summary.Length < (summary.Length + details.Length) / 2);
        }

        // The experimental marker must not replace the actual description.
        Assert.Contains("Executes", PwshHelp.GetString("OptionCommandWithArgs", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("XML")]
    [InlineData("Normal")]
    [InlineData("Minimized")]
    [InlineData("Maximized")]
    [InlineData("Hidden")]
    [InlineData("AllSigned")]
    [InlineData("Bypass")]
    [InlineData("Default")]
    [InlineData("RemoteSigned")]
    [InlineData("Restricted")]
    [InlineData("Undefined")]
    [InlineData("Unrestricted")]
    [InlineData("StandardInput")]
    public void FiniteValueDescriptionsFallBackToInvariantResources(string value)
    {
        string invariant = PwshHelp.GetString("Value" + value, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(invariant));
        Assert.Equal(invariant, PwshHelp.GetString("Value" + value, CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void ResourceLookupUsesCurrentUICultureAndParentFallback()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            string french = PwshHelp.GetString("OptionFile", CultureInfo.GetCultureInfo("fr"));
            Assert.Equal(french, PwshHelp.GetString("OptionFile"));

            // Existing extended-help translations may still contain English text;
            // the translated usage heading demonstrates that the satellite was loaded.
            Assert.NotEqual(PwshHelp.GetString("UsageHelp", CultureInfo.InvariantCulture), PwshHelp.UsageHelp);
            Assert.Equal(PwshHelp.GetExtendedHelp(CultureInfo.CurrentUICulture), PwshHelp.ExtendedHelp);
            Assert.Equal(PwshHelp.GetString("UsageHelp", CultureInfo.CurrentUICulture), PwshHelp.UsageHelp);
            Assert.Equal(
                PwshHelp.GetString("OptionFile", CultureInfo.InvariantCulture),
                PwshHelp.GetString("OptionFile", CultureInfo.GetCultureInfo("en-GB")));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
