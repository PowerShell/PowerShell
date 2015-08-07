using Xunit;
using System;
using System.Diagnostics;
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

        [Fact]
        public static void TestGetUserName()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"/usr/bin/env",
                Arguments = "whoami",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            Process process = Process.Start(startInfo);
            // The process should always exit, but wait a set time just in case
            process.WaitForExit(1000);
            // The process should return an exit code of 0 on success
            Assert.Equal(0, process.ExitCode);
            // Get output of call to whoami without trailing newline
            string username = process.StandardOutput.ReadToEnd().Trim();
            // It should be the same as what our platform code returns
            Assert.Equal(username, Platform.NonWindowsGetUserName());
        }
    }
}
