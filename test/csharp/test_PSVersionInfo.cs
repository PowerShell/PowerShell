using Xunit;
using System;
using System.Management.Automation;

namespace PSTests.Parallel
{
    public static class PSVersionInfoTests
    {
        [Fact]
        public static void TestVersions()
        {
            // test that a non-null version table is returned, and
            // that it does not throw
            Assert.NotNull(PSVersionInfo.GetPSVersionTable());
        }
    }
}
