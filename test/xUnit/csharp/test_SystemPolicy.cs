// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX
using System;
using System.IO;
using System.Management.Automation.Security;
using System.Runtime.Versioning;
using System.Security.Principal;
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
        public static void TestRegistryKeyMayExist()
        {
            string testKeyPath = @"Software\PowerShellTests\SystemPolicy_" + Guid.NewGuid().ToString("N");
            string script = testKeyPath + @"\SrpV2\Script";
            try
            {
                Assert.False(SystemPolicy.RegistryKeyMayExist(Registry.CurrentUser, script));

                // An existing AppLocker key counts even when it is empty.
                Registry.CurrentUser.CreateSubKey(script).Dispose();
                Assert.True(SystemPolicy.RegistryKeyMayExist(Registry.CurrentUser, script));
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, throwOnMissingSubKey: false);
            }
        }

        [Fact]
        public static void TestHasAppLockerOrSaferScriptPolicyMatchesMachine()
        {
            bool expected = (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                || SystemPolicy.IsThreadImpersonating()
                || SystemPolicy.PolicyFileMayExist(Path.Combine(Environment.SystemDirectory, "AppLocker", "Script.AppLocker"))
                || SystemPolicy.PolicyFileMayExist(Path.Combine(Environment.SystemDirectory, "AppLocker", "MDM"))
                || SystemPolicy.RegistryKeyMayExist(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\SrpV2\Script")
                || !SystemPolicy.IsPolicyKeyEmpty(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers", "AuthenticodeEnabled")
                || !SystemPolicy.IsPolicyKeyEmpty(Registry.CurrentUser, @"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers", "AuthenticodeEnabled");

            Assert.Equal(expected, SystemPolicy.HasAppLockerOrSaferScriptPolicy());
        }

        [Fact]
        public static void TestHasAppLockerOrSaferScriptPolicyWhileImpersonating()
        {
            // Registry.CurrentUser may not be the impersonated user's hive, so the policy check must not be skipped.
            Assert.False(SystemPolicy.IsThreadImpersonating());
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsIdentity.RunImpersonated(identity.AccessToken, () =>
            {
                Assert.True(SystemPolicy.IsThreadImpersonating());
                Assert.True(SystemPolicy.HasAppLockerOrSaferScriptPolicy());
            });
        }

        [Theory]
        [InlineData("None", false)]
        [InlineData("AuthenticodeEnabledOnly", false)]
        [InlineData("SaferMachineRule", true)]
        [InlineData("SaferMachineDefaultLevel", true)]
        [InlineData("SaferUserRule", true)]
        [InlineData("AppLockerScriptKey", true)]
        [InlineData("AppLockerScriptPolicyFile", true)]
        [InlineData("AppLockerMdmFolder", true)]
        public static void TestSaferScriptCheckForPolicyState(string state, bool scriptPolicyExpected)
        {
            const string CodeIdentifiers = @"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers";
            string testKeyPath = @"Software\PowerShellTests\SystemPolicy_" + Guid.NewGuid().ToString("N");
            string appLockerDirectory = Path.Combine(Path.GetTempPath(), "SystemPolicy_" + Guid.NewGuid().ToString("N"));
            try
            {
                using RegistryKey machine = Registry.CurrentUser.CreateSubKey(testKeyPath + @"\Machine");
                using RegistryKey user = Registry.CurrentUser.CreateSubKey(testKeyPath + @"\User");

                // An AppLocker folder without policies, as on a machine where AppLocker was never configured.
                Directory.CreateDirectory(appLockerDirectory);
                File.WriteAllText(Path.Combine(appLockerDirectory, "AppCache.dat.LOG1"), string.Empty);

                switch (state)
                {
                    case "AuthenticodeEnabledOnly":
                        using (RegistryKey key = machine.CreateSubKey(CodeIdentifiers))
                        {
                            key.SetValue("authenticodeenabled", 0, RegistryValueKind.DWord);
                        }

                        break;

                    case "SaferMachineRule":
                        using (RegistryKey key = machine.CreateSubKey(CodeIdentifiers))
                        {
                            key.SetValue("authenticodeenabled", 0, RegistryValueKind.DWord);
                            key.CreateSubKey(@"0\Paths\{" + Guid.NewGuid() + "}").Dispose();
                        }

                        break;

                    case "SaferMachineDefaultLevel":
                        using (RegistryKey key = machine.CreateSubKey(CodeIdentifiers))
                        {
                            key.SetValue("DefaultLevel", 0, RegistryValueKind.DWord);
                        }

                        break;

                    case "SaferUserRule":
                        user.CreateSubKey(CodeIdentifiers + @"\0\Hashes\{" + Guid.NewGuid() + "}").Dispose();
                        break;

                    case "AppLockerScriptKey":
                        machine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\SrpV2\Script").Dispose();
                        break;

                    case "AppLockerScriptPolicyFile":
                        File.WriteAllText(Path.Combine(appLockerDirectory, "Script.AppLocker"), string.Empty);
                        break;

                    case "AppLockerMdmFolder":
                        Directory.CreateDirectory(Path.Combine(appLockerDirectory, "MDM"));
                        break;

                    case "None":
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }

                bool scriptPolicyMayExist = SystemPolicy.HasAppLockerOrSaferScriptPolicy(machine, user, appLockerDirectory);
                Assert.Equal(scriptPolicyExpected, scriptPolicyMayExist);

                // SAFER is asked about each script only when a policy may exist, unless the system lockdown policy
                // (WDAC) is Enforce. Then scripts run in ConstrainedLanguage and SAFER is not asked at all.
                Assert.Equal(scriptPolicyExpected, SystemPolicy.IsSaferScriptCheckRequired(SystemEnforcementMode.None, () => scriptPolicyMayExist));
                Assert.Equal(scriptPolicyExpected, SystemPolicy.IsSaferScriptCheckRequired(SystemEnforcementMode.Audit, () => scriptPolicyMayExist));
                Assert.False(SystemPolicy.IsSaferScriptCheckRequired(SystemEnforcementMode.Enforce, () => throw new InvalidOperationException()));
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(testKeyPath, throwOnMissingSubKey: false);
                Directory.Delete(appLockerDirectory, recursive: true);
            }
        }
    }
}
#endif
