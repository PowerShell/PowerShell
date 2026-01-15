// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Parallel
{
    public static class ConvertToJsonTests
    {
        /// <summary>
        /// Verifies that JsonStringEscapeHandling enum values match Newtonsoft.Json.StringEscapeHandling
        /// for backward compatibility. These values must not change.
        /// </summary>
        [Fact]
        public static void JsonStringEscapeHandling_Values_MatchNewtonsoftStringEscapeHandling()
        {
            Assert.Equal((int)Newtonsoft.Json.StringEscapeHandling.Default, (int)JsonStringEscapeHandling.Default);
            Assert.Equal((int)Newtonsoft.Json.StringEscapeHandling.EscapeNonAscii, (int)JsonStringEscapeHandling.EscapeNonAscii);
            Assert.Equal((int)Newtonsoft.Json.StringEscapeHandling.EscapeHtml, (int)JsonStringEscapeHandling.EscapeHtml);
        }
    }
}
