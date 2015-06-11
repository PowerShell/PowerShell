using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class PlatformTests
    {
        public static void TestIsLinux()
        {
            Assert.True(Platform.IsLinux());
        }

        public static void TestHasCom()
        {
            Assert.False(Platform.HasCom());
        }
    }
}
