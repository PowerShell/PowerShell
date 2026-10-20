// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;

using Microsoft.PowerShell;
using Xunit;

namespace PSTests.Parallel
{
    public static class UpdatesNotificationTests
    {
        private const string ReleaseTag = "7.6.7";

        private static readonly string[] TemplateNames = new[]
        {
            nameof(ManagedEntranceStrings.StableUpdateNotificationMessage),
            nameof(ManagedEntranceStrings.LTSUpdateNotificationMessage),
            nameof(ManagedEntranceStrings.PreviewUpdateNotificationMessage),
        };

        // Keep in sync with the localized resources under src/Microsoft.PowerShell.ConsoleHost/resources.
        private static readonly string[] LocalizedCultures = new[]
        {
            "cs", "de", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "tr", "zh-Hans", "zh-Hant",
        };

        public static IEnumerable<object[]> NeutralTemplates()
        {
            foreach (string templateName in TemplateNames)
            {
                yield return new object[] { templateName };
            }
        }

        public static IEnumerable<object[]> LocalizedTemplates()
        {
            foreach (string cultureName in LocalizedCultures)
            {
                foreach (string templateName in TemplateNames)
                {
                    yield return new object[] { cultureName, templateName };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NeutralTemplates))]
        public static void TestNeutralTemplateLinesHaveSameWidth(string templateName)
        {
            string template = ManagedEntranceStrings.ResourceManager.GetString(templateName, CultureInfo.InvariantCulture);

            string message = UpdatesNotification.FormatUpdateNotification(template, ReleaseTag, supportsVirtualTerminal: true);

            string[] lines = message.Split('\n');
            Assert.Equal(lines[0].Length, lines[1].Length);
            Assert.Equal(lines[0].Length, lines[2].Length);
        }

        [Theory]
        [InlineData("  {1} Short {0} {2}\n  {1} A second line that is longer than the first one:{3}{2}\n  {1}   https://aka.ms/PowerShell-Release?tag=v{0} {4}{2}\n")]
        [InlineData("  {1} New release: v{0} {2}\n  {1} Upgrade now:{3}{2}\n  {1}   https://aka.ms/PowerShell-Release?tag=v{0} {4}{2}\n")]
        public static void TestTemplateWithLongerLaterLine(string template)
        {
            string message = UpdatesNotification.FormatUpdateNotification(template, ReleaseTag, supportsVirtualTerminal: true);

            Assert.Contains($"tag=v{ReleaseTag}", message);
        }

        [SkippableTheory]
        [MemberData(nameof(LocalizedTemplates))]
        public static void TestLocalizedTemplate(string cultureName, string templateName)
        {
            string template = ManagedEntranceStrings.ResourceManager.GetString(templateName, new CultureInfo(cultureName));
            string neutralTemplate = ManagedEntranceStrings.ResourceManager.GetString(templateName, CultureInfo.InvariantCulture);
            Skip.If(template == neutralTemplate, $"The '{cultureName}' resources are not available.");

            string message = UpdatesNotification.FormatUpdateNotification(template, ReleaseTag, supportsVirtualTerminal: true);

            Assert.Contains($"tag=v{ReleaseTag}", message);
        }
    }
}
