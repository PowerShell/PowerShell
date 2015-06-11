using Xunit;
using System;

namespace PSTests
{
    public static class PlatformTests
    {
        public static void testIsLinux()
        {
            Assert.Equal(System.Management.Automation.Platform.IsLinux(), true);
        }
    }
}
