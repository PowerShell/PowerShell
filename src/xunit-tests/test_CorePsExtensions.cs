using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class CorePsExtensionsTests
    {
        [Fact]
        public static void TestEnvironmentOSVersion()
        {
            // this test primarily checks that there is no exception thrown
            // NOTE: the type name is explicitly written here to make clear what
            //       type and function is tested
            System.Management.Automation.Environment.OperatingSystem os = System.Management.Automation.Environment.OSVersion;
            Assert.NotNull(os);
        }
    }
}
