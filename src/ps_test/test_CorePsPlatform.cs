using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public static class PlatformTests
    {
        [Fact]
        public static void TestIsLinux()
        {
            Assert.True(Platform.IsLinux());
        }

        [Fact]
        public static void TestHasCom()
        {
            Assert.False(Platform.HasCom());
        }

        [Fact]
        public static void TestHasAmsi()
        {
            Assert.False(Platform.HasAmsi());
        }

        [Fact]
        public static void TestUsesCodeSignedAssemblies()
        {
            Assert.False(Platform.UsesCodeSignedAssemblies());
        }

        [Fact]
        public static void TestHasDriveAutoMounting()
        {
            Assert.False(Platform.HasDriveAutoMounting());
        }

        [Fact]
        public static void TestHasRegistrySupport()
        {
            Assert.False(Platform.HasRegistrySupport());
        }
    }
}
