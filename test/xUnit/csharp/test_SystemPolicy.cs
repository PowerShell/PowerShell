// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX
using System;
using System.IO;
using System.Management.Automation.Security;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Xunit;

namespace PSTests.Parallel
{
    [SupportedOSPlatform("windows")]
    public static class SystemPolicyTests
    {
        [Fact]
        public static void TestIsPolicyKeyEmpty()
        {
            string testKeyPath = @"Software\PowerShellTests\SystemPolicy_" + Guid.NewGuid().ToString("N");
            string codeIdentifiers = testKeyPath + @"\CodeIdentifiers";
            try
            {
                // Key does not exist.
                Assert.True(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, "AuthenticodeEnabled"));

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(codeIdentifiers))
                {
                    // Key exists without values or subkeys.
                    Assert.True(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, "AuthenticodeEnabled"));

                    // Only the ignored value.
                    key.SetValue("authenticodeenabled", 0, RegistryValueKind.DWord);
                    Assert.True(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, "AuthenticodeEnabled"));
                    Assert.False(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, ignoredValueName: null));

                    // Any other value means a policy is configured.
                    key.SetValue("DefaultLevel", 0x40000, RegistryValueKind.DWord);
                    Assert.False(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, "AuthenticodeEnabled"));
                    key.DeleteValue("DefaultLevel");

                    // So does any subkey (rules).
                    key.CreateSubKey(@"262144\Paths").Dispose();
                    Assert.False(SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, codeIdentifiers, "AuthenticodeEnabled"));
                }
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, throwOnMissingSubKey: false);
            }
        }

        [Fact]
        public static void TestPolicyFileMayExist()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SystemPolicy_" + Guid.NewGuid().ToString("N"));
            string file = Path.Combine(directory, "Script.AppLocker");

            // Directory does not exist.
            Assert.False(SystemPolicy.PolicyFileMayExist(file));

            Directory.CreateDirectory(directory);
            try
            {
                Assert.False(SystemPolicy.PolicyFileMayExist(file));

                File.WriteAllText(file, string.Empty);
                Assert.True(SystemPolicy.PolicyFileMayExist(file));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public static void TestHasAppLockerOrSaferScriptPolicyMatchesMachine()
        {
            bool expected = File.Exists(Path.Combine(Environment.SystemDirectory, "AppLocker", "Script.AppLocker"))
                || !SystemPolicy.IsPolicyKeyEmpty(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SrpV2\Script", ignoredValueName: null)
                || !SystemPolicy.IsPolicyKeyEmpty(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers", "AuthenticodeEnabled")
                || !SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers", "AuthenticodeEnabled");

            Assert.Equal(expected, SystemPolicy.HasAppLockerOrSaferScriptPolicy());
        }
    }
}
#endif
