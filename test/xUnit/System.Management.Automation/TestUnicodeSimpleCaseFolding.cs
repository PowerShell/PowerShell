// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Unicode;
using Xunit;

namespace System.Management.Automation
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.Equal(0, "".CompareFolded(""));
        }
    }
}
