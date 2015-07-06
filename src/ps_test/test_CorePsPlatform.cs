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

        public static void TestHasAmsi()
        {
            Assert.False(Platform.HasAmsi());
        }

        public static void TestUsesCodeSignedAssemblies()
        {
            Assert.False(Platform.UsesCodeSignedAssemblies());
        }

        public static void TestHasDriveAutoMounting()
        {
            Assert.False(Platform.HasDriveAutoMounting());
        }

        public static void TestHasRegistrySupport()
        {
            Assert.False(Platform.HasRegistrySupport());
        }
    }
}
