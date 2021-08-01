// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

using Microsoft.PowerShell.Commands;

using Xunit;

namespace PSTests.Parallel
{
    public class HttpVersionTransformationAttributeTests
    {
        [Theory]
        [MemberData(nameof(StringCases))]
        public void FromString(string inputData, object expected)
        {
            var transformation = new HttpVersionTransformationAttribute();
            var result = transformation.Transform(default, inputData);
    
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> StringCases()
        {
            yield return new object[] { "1.1", "1.1" };
            yield return new object[] { "1.", "1." };
            yield return new object[] { "1", new Version(1, 0) };
        }

        [Theory]
        [InlineData(1.0, "1.0")]
        [InlineData(1.1, "1.1")]
        public void FromDouble(double inputData, object expected)
        {
            var transformation = new HttpVersionTransformationAttribute();
            var result = transformation.Transform(default, inputData);

            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(IntCases))]
        public void FromInt(int inputData, object expected)
        {
            var transformation = new HttpVersionTransformationAttribute();
            var result = transformation.Transform(default, inputData);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> IntCases()
        {
            yield return new object[] { 1, new Version(1, 0) };
            yield return new object[] { 2, new Version(2, 0) };
        }

        [Theory]
        [MemberData(nameof(PSObjectCases))]
        public void FromSwitch(PSObject inputData, object expected)
        {
            var transformation = new HttpVersionTransformationAttribute();
            var result = transformation.Transform(default, inputData);

            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> PSObjectCases()
        {
            yield return new object[] { new PSObject(false), new Version(0, 0) };
            yield return new object[] { new PSObject(true), new Version(1, 0) };
        }
    }
}
