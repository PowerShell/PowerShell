// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Xunit.Sdk;
using Xunit.v3;

namespace TestOrder.TestCaseOrdering
{
    public class SourceDeclarationOrderer : ITestCaseOrderer
    {
        public IReadOnlyCollection<TTestCase> OrderTestCases<TTestCase>(IReadOnlyCollection<TTestCase> testCases) where TTestCase : notnull, ITestCase
            => new SortedSet<TTestCase>(testCases, SourceDeclarationComparer<TTestCase>.Instance);

        private sealed class SourceDeclarationComparer<TTestCase> : IComparer<TTestCase>
            where TTestCase : notnull, ITestCase
        {
            private SourceDeclarationComparer()
            {
            }

#pragma warning disable CA1000 // Do not declare static members on generic types - fine in this case.
            public static IComparer<TTestCase> Instance { get; } = new SourceDeclarationComparer<TTestCase>();
#pragma warning restore CA1000 // Do not declare static members on generic types

            public int Compare(TTestCase x, TTestCase y)
            {
                if (x.SourceFilePath != y.SourceFilePath)
                {
                    return x.SourceFilePath.CompareTo(y.SourceFilePath);
                }

                return x.SourceLineNumber.Value.CompareTo(y.SourceLineNumber.Value);
            }
        }
    }
}
