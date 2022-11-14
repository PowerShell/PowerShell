// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

using Xunit;

namespace PSTests.Parallel
{
    public class ArgumentToVersionTransformationAttributeTests
    {
        [Theory]
        [MemberData(nameof(TestCases))]
        public void TestConversion(object inputData, object expected)
        {
            var transformation = new ArgumentToVersionTransformationAttribute();
            var result = transformation.Transform(default, inputData);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> TestCases()
        {
            // strings
            yield return new object[] { "1.1", "1.1" };
            yield return new object[] { "1", new Version(1, 0) };
            yield return new object[] { string.Empty, new Version(0, 0) };

            // doubles
            yield return new object[] { 1.0, 1.0 };
            yield return new object[] { 1.1, 1.1 };

            // ints
            yield return new object[] { 1, new Version(1, 0) };
            yield return new object[] { 2, new Version(2, 0) };

            // PSObjects
            yield return new object[] { new PSObject(obj: 0), new Version(0, 0) };
            yield return new object[] { new PSObject(obj: 1), new Version(1, 0) };

            // unhandled
            var obj = new object();
            yield return new object[] { obj, obj };

            var date = DateTimeOffset.UtcNow;
            yield return new object[] { date, date };
        }
    }
}
