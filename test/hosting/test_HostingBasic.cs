// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Security;
using Microsoft.Management.Infrastructure;
using Xunit;

namespace PowerShell.Hosting.SDK.Tests
{
    public static class HostingTests
    {
        [Fact]
        public static void TestCommandFromUtility()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("Get-Verb -Verb get").Invoke();

                foreach (dynamic item in results)
                {
                    Assert.Equal("Get", item.Verb);
                }
            }
        }

        [Fact]
        public static void TestCommandFromManagement()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var path = Environment.CurrentDirectory;
                var results = ps.AddCommand("Test-Path").AddParameter("Path", path).Invoke<bool>();

                foreach (dynamic item in results)
                {
                    Assert.True(item);
                }
            }
        }

        [Fact]
        public static void TestCommandFromCore()
        {
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript(@"$i = 0 ; 1..3 | ForEach-Object { $i += $_} ; $i").Invoke<int>();

                foreach (dynamic item in results)
                {
                    Assert.Equal(6,item);
                }
            }
        }

        [SkippableFact]
        public static void TestCommandFromMMI()
        {
            // Test is disabled since we do not have a CimCmdlets module released in the SDK.
            Skip.IfNot(Platform.IsWindows);
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("[Microsoft.Management.Infrastructure.CimInstance]::new('Win32_Process')").Invoke();
                Assert.True(results.Count > 0);
            }
        }

        [SkippableFact]
        public static void TestCommandFromDiagnostics()
        {
            Skip.IfNot(Platform.IsWindows);
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("Get-WinEvent -ListLog Application").Invoke();

                foreach (dynamic item in results)
                {
                    Assert.Equal("Application", item.LogName);
                }
            }
        }

        [SkippableFact]
        public static void TestCommandFromSecurity()
        {
            Skip.IfNot(Platform.IsWindows);
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("ConvertTo-SecureString -String test -AsPlainText -Force").Invoke<SecureString>();
                Assert.IsType<SecureString>(results[0]);
            }
        }

        [SkippableFact]
        public static void TestCommandFromWSMan()
        {
            Skip.IfNot(Platform.IsWindows);
            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                var results = ps.AddScript("Test-WSMan").Invoke();

                foreach (dynamic item in results)
                {
                    Assert.Equal("Microsoft Corporation", item.ProductVendor);
                }
            }
        }

        [Fact]
        public static void TestCommandFromNative()
        {
            var fs = File.Create(Path.GetTempFileName());
            fs.Close();

            string target = fs.Name;
            string path = Path.GetTempFileName();

            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                // New-Item -ItemType SymbolicLink uses libpsl-native, hence using it for validating native dependencies.
                string command = $"New-Item -ItemType SymbolicLink -Path {path} -Target {target}";
                var results = ps.AddScript(command).Invoke<FileInfo>();

                foreach (var item in results)
                {
                    Assert.Equal(path, item.FullName);
                }
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(target))
            {
                File.Delete(target);
            }
        }
    }
}
