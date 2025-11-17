// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using Xunit;

namespace PSTests.Parallel
{
    // Not static because a test requires non-const variables
    public class MshSnapinInfoTests
    {
        // Test that it does not throw an exception
        [Fact]
        public void TestReadRegistryInfo()
        {
            Assert.SkipUnless(Platform.IsWindows, "Only supported on Windows");
            Version someVersion = null;
            string someString = null;
            PSSnapInReader.ReadRegistryInfo(out someVersion, out someString, out someString, out someString, out someVersion);
        }

        // PublicKeyToken is null on Linux
        [Fact]
        public void TestReadCoreEngineSnapIn()
        {
            Assert.SkipUnless(Platform.IsWindows, "Only supported on Windows");
            PSSnapInInfo pSSnapInInfo = PSSnapInReader.ReadCoreEngineSnapIn();
            Assert.Contains("PublicKeyToken=31bf3856ad364e35", pSSnapInInfo.AssemblyName);
        }
    }
}
