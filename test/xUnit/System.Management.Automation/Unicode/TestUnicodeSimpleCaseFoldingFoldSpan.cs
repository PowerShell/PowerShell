// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Unicode;
using System;
using Xunit;

namespace PSTests.Parallel.System.Management.Automation.Unicode
{
    public class FoldStringAndSpanTests
    {
        [Fact]
        public static void Fold_String_Span_Roundtrips()
        {
            string s = "Turkish I \u0131s TROUBL\u0130NG!";
            ReadOnlySpan<char> span = s.AsSpan();
            Span<char> span2 = stackalloc char[s.Length];
            s.AsSpan().CopyTo(span2);
            var foldedString = s.SimpleCaseFold();
            ReadOnlySpan<char> foldedSpan1 = span.SimpleCaseFold();

            Assert.Equal(foldedString, foldedSpan1.ToString());

            span2.SimpleCaseFold();
            Assert.Equal(foldedString, span2.ToString());
            Assert.Equal(0, foldedString.AsSpan().SequenceCompareTo(span2));
            Assert.Equal(foldedSpan1.ToString(), span2.ToString());
            Assert.Equal(0, foldedSpan1.SequenceCompareTo(span2));
        }

        [Theory]
        [InlineData("Hello", "hello")]
        [InlineData("Turkish I \u0131s TROUBL\u0130NG!", "turkish i \u0131s troubl\u0130ng!")]
        public static void Fold_String_And_Span(string s, string target)
        {
            Assert.Equal(0, String.CompareOrdinal(s.SimpleCaseFold(), target));

            Assert.Equal(0, String.CompareOrdinal(s.AsSpan().SimpleCaseFold().ToString(), target));
            Assert.Equal(0, s.AsSpan().SimpleCaseFold().SequenceCompareTo(target.AsSpan()));
        }
    }
}
