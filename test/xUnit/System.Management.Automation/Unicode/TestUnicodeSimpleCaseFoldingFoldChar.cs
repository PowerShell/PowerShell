// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Unicode;
using Xunit;

namespace PSTests.Parallel.System.Management.Automation.Unicode
{
    public class FoldCharTests
    {
        [Fact]
        public static void Fold_Char()
        {
            for (int i = 0; i <= 0xffff; i++)
            {
                var expected = i;
                if (CharUnicodeInfoTestData.CaseFoldingPairs.TryGetValue((char)i, out int foldedCharOut))
                {
                    expected = foldedCharOut;
                }

                var foldedChar = (int)SimpleCaseFolding.Fold((char)i);
                Assert.Equal(expected, foldedChar);
            }
        }
    }
}
