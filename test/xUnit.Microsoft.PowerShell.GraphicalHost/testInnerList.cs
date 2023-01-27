// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.UI.Internal;

namespace Microsoft.PowerShell.GraphicalHost.xUnit.tests
{
    public class testInnerList
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("Some Text", "Some Text")]
        [InlineData("Some \t in Text", "\"Some \t in Text\"")]
        [InlineData("\t", "\"\t\"")]
        [InlineData("Some \" in Text", "\"Some \"\" in Text\"")]
        [InlineData("\"", "\"\"\"\"")]
        [InlineData("Some \r in Text", "\"Some \r in Text\"")]
        [InlineData("\r", "\"\r\"")]
        [InlineData("Some \n in Text", "\"Some \n in Text\"")]
        [InlineData("\n", "\"\n\"")]
        public void EscapeClipboardTableCellValue(string input, string expectedOutput)
        {
            Assert.Equal(expectedOutput, InnerList.EscapeClipboardTableCellValue(input));
        }
    }
}
