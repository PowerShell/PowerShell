// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PSTests.Internal;
using Xunit;

namespace PSTests.Sequential
{
    [TestCaseOrderer("TestOrder.TestCaseOrdering.PriorityOrderer", "powershell-tests")]
    public class PowerShellPolicyFixture : IDisposable
    {
        private const string ConfigFileName = "powershell.config.json";

        private readonly string systemWideConfigFile;
        private readonly string currentUserConfigFile;

        private readonly string systemWideConfigBackupFile;
        private readonly string currentUserConfigBackupFile;

        private readonly string systemWideConfigDirectory;
        private readonly string currentUserConfigDirectory;

        private readonly JsonSerializer serializer;

        private readonly PowerShellPolicies systemWidePolicies;
        private readonly PowerShellPolicies currentUserPolicies;

        private readonly bool originalTestHookValue;

        // Reflection handle to the private, mutable 'machineFolderConfigFile' field of the PowerShellConfig
        // singleton. On non-packaged installs this field is null, so the MachineFolder scope has no backing
        // file. Tests point it at a temp file to exercise the 3-scope (CurrentUser/MachineFolder/AllUsers)
        // merge behavior used by MSIX installs, and it is restored to its original value on cleanup.
        private readonly FieldInfo machineFolderConfigFileField;
        private readonly string originalMachineFolderConfigFile;
        private readonly string machineFolderTestConfigDirectory;
        private readonly string machineFolderTestConfigFile;

        public PowerShellPolicyFixture()
        {
            systemWideConfigDirectory = Utils.DefaultPowerShellAppBase;
            currentUserConfigDirectory = Platform.ConfigDirectory;

            if (!Directory.Exists(currentUserConfigDirectory))
            {
                // Create the CurrentUser config directory if it doesn't exist
                Directory.CreateDirectory(currentUserConfigDirectory);
            }

            systemWideConfigFile = Path.Combine(systemWideConfigDirectory, ConfigFileName);
            currentUserConfigFile = Path.Combine(currentUserConfigDirectory, ConfigFileName);

            if (File.Exists(systemWideConfigFile))
            {
                systemWideConfigBackupFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Move(systemWideConfigFile, systemWideConfigBackupFile);
            }

            if (File.Exists(currentUserConfigFile))
            {
                currentUserConfigBackupFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                File.Move(currentUserConfigFile, currentUserConfigBackupFile);
            }

            var settings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.None,
                MaxDepth = 10,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            serializer = JsonSerializer.Create(settings);

            machineFolderConfigFileField = typeof(PowerShellConfig).GetField("machineFolderConfigFile", BindingFlags.NonPublic | BindingFlags.Instance);
            originalMachineFolderConfigFile = (string)machineFolderConfigFileField.GetValue(PowerShellConfig.Instance);
            machineFolderTestConfigDirectory = Path.Combine(Path.GetTempPath(), "PSMachineFolderTest_" + Guid.NewGuid().ToString("N"));
            machineFolderTestConfigFile = Path.Combine(machineFolderTestConfigDirectory, ConfigFileName);

            systemWidePolicies = new PowerShellPolicies()
            {
                ScriptExecution = new ScriptExecution() { ExecutionPolicy = "RemoteSigned", EnableScripts = true },
                ScriptBlockLogging = new ScriptBlockLogging() { EnableScriptBlockInvocationLogging = true, EnableScriptBlockLogging = false },
                ModuleLogging = new ModuleLogging() { EnableModuleLogging = false, ModuleNames = new string[] { "PSReadline", "PowerShellGet" } },
                ProtectedEventLogging = new ProtectedEventLogging() { EnableProtectedEventLogging = false, EncryptionCertificate = new string[] { "Joe" } },
                Transcription = new Transcription() { EnableInvocationHeader = true, EnableTranscripting = true, OutputDirectory = @"c:\tmp" },
                UpdatableHelp = new UpdatableHelp() { DefaultSourcePath = @"f:\temp" },
                ConsoleSessionConfiguration = new ConsoleSessionConfiguration() { EnableConsoleSessionConfiguration = true, ConsoleSessionConfigurationName = "name" }
            };

            currentUserPolicies = new PowerShellPolicies()
            {
                ScriptExecution = new ScriptExecution() { ExecutionPolicy = "RemoteSigned" },
                ScriptBlockLogging = new ScriptBlockLogging() { EnableScriptBlockLogging = false },
                ModuleLogging = new ModuleLogging() { EnableModuleLogging = false },
                ProtectedEventLogging = new ProtectedEventLogging() { EncryptionCertificate = new string[] { "Joe" } }
            };

            // Set the test hook to disable policy caching
            originalTestHookValue = InternalTestHooks.BypassGroupPolicyCaching;
            InternalTestHooks.BypassGroupPolicyCaching = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupConfigFiles();
                CleanupMachineFolderConfig();
                if (systemWideConfigBackupFile != null)
                {
                    File.Move(systemWideConfigBackupFile, systemWideConfigFile);
                }

                if (currentUserConfigBackupFile != null)
                {
                    File.Move(currentUserConfigBackupFile, currentUserConfigFile);
                }

                InternalTestHooks.BypassGroupPolicyCaching = originalTestHookValue;
            }
        }

        internal PowerShellPolicies SystemWidePolicies
        {
            get { return systemWidePolicies; }
        }

        internal PowerShellPolicies CurrentUserPolicies
        {
            get { return currentUserPolicies; }
        }

        #region Compare_Policy_Settings

        internal void CompareScriptExecution(ScriptExecution a, ScriptExecution b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableScripts, b.EnableScripts);
                Assert.Equal(a.ExecutionPolicy, b.ExecutionPolicy);
            }
        }

        internal void CompareScriptBlockLogging(ScriptBlockLogging a, ScriptBlockLogging b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableScriptBlockInvocationLogging, b.EnableScriptBlockInvocationLogging);
                Assert.Equal(a.EnableScriptBlockLogging, b.EnableScriptBlockLogging);
            }
        }

        internal void CompareModuleLogging(ModuleLogging a, ModuleLogging b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableModuleLogging, b.EnableModuleLogging);
                if (a.ModuleNames == null)
                {
                    Assert.Null(b.ModuleNames);
                }
                else
                {
                    Assert.Equal(a.ModuleNames.Length, b.ModuleNames.Length);
                    for (int i = 0; i < a.ModuleNames.Length; i++)
                    {
                        Assert.Equal(a.ModuleNames[i], b.ModuleNames[i]);
                    }
                }
            }
        }

        internal void CompareProtectedEventLogging(ProtectedEventLogging a, ProtectedEventLogging b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableProtectedEventLogging, b.EnableProtectedEventLogging);
                if (a.EncryptionCertificate == null)
                {
                    Assert.Null(b.EncryptionCertificate);
                }
                else
                {
                    Assert.Equal(a.EncryptionCertificate.Length, b.EncryptionCertificate.Length);
                    for (int i = 0; i < a.EncryptionCertificate.Length; i++)
                    {
                        Assert.Equal(a.EncryptionCertificate[i], b.EncryptionCertificate[i]);
                    }
                }
            }
        }

        internal void CompareTranscription(Transcription a, Transcription b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableTranscripting, b.EnableTranscripting);
                Assert.Equal(a.EnableInvocationHeader, b.EnableInvocationHeader);
                Assert.Equal(a.OutputDirectory, b.OutputDirectory);
            }
        }

        internal void CompareUpdatableHelp(UpdatableHelp a, UpdatableHelp b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.DefaultSourcePath, b.DefaultSourcePath);
            }
        }

        internal void CompareConsoleSessionConfiguration(ConsoleSessionConfiguration a, ConsoleSessionConfiguration b)
        {
            if (a == null)
            {
                Assert.Null(b);
            }
            else
            {
                Assert.Equal(a.EnableConsoleSessionConfiguration, b.EnableConsoleSessionConfiguration);
                Assert.Equal(a.ConsoleSessionConfigurationName, b.ConsoleSessionConfigurationName);
            }
        }

        internal void CompareTwoPolicies(PowerShellPolicies a, PowerShellPolicies b)
        {
            // Compare 'ScriptExecution' settings
            CompareScriptExecution(a.ScriptExecution, b.ScriptExecution);

            // Compare 'ScriptBlockLogging' settings
            CompareScriptBlockLogging(a.ScriptBlockLogging, b.ScriptBlockLogging);

            // Compare 'ModuleLogging' settings
            CompareModuleLogging(a.ModuleLogging, b.ModuleLogging);

            // Compare 'ProtectedEventLogging' settings
            CompareProtectedEventLogging(a.ProtectedEventLogging, b.ProtectedEventLogging);

            // Compare 'Transcription' settings
            CompareTranscription(a.Transcription, b.Transcription);

            // Compare 'UpdatableHelp' settings
            CompareUpdatableHelp(a.UpdatableHelp, b.UpdatableHelp);

            // Compare 'ConsoleSessionConfiguration' settings
            CompareConsoleSessionConfiguration(a.ConsoleSessionConfiguration, b.ConsoleSessionConfiguration);
        }

        #endregion

        #region Configuration_File_Setup

        public void CleanupConfigFiles()
        {
            var maxPause = 10;

            while (maxPause-- != 0 && (File.Exists(systemWideConfigFile) || File.Exists(currentUserConfigFile)))
            {
                var pause = false;

                try
                {
                    File.Delete(systemWideConfigFile);
                }
                catch (IOException)
                {
                    pause = true;
                }

                try
                {
                    File.Delete(currentUserConfigFile);
                }
                catch (IOException)
                {
                    pause = true;
                }

                if (pause)
                {
                    Thread.Sleep(5);
                }
            }
        }

        public void SetupConfigFile1()
        {
            CleanupConfigFiles();

            // System wide config file has all policy settings
            var systemWideConfig = new { ConsolePrompting = true, PowerShellPolicies = systemWidePolicies };
            using (var streamWriter = new StreamWriter(systemWideConfigFile))
            {
                serializer.Serialize(streamWriter, systemWideConfig);
            }

            // Current user config file has partial policy settings
            var currentUserConfig = new { DisablePromptToUpdateHelp = false, PowerShellPolicies = currentUserPolicies };
            using (var streamWriter = new StreamWriter(currentUserConfigFile))
            {
                serializer.Serialize(streamWriter, currentUserConfig);
            }
        }

        public void SetupConfigFile2()
        {
            CleanupConfigFiles();

            // System wide config file has all policy settings
            var systemWideConfig = new { ConsolePrompting = true, PowerShellPolicies = systemWidePolicies };
            using (var streamWriter = new StreamWriter(systemWideConfigFile))
            {
                serializer.Serialize(streamWriter, systemWideConfig);
            }

            // Current user config file is empty
            CreateEmptyFile(currentUserConfigFile);
        }

        public void SetupConfigFile3()
        {
            CleanupConfigFiles();

            // System wide config file is empty
            CreateEmptyFile(systemWideConfigFile);

            // Current user config file has partial policy settings
            var currentUserConfig = new { DisablePromptToUpdateHelp = false, PowerShellPolicies = currentUserPolicies };
            using (var streamWriter = new StreamWriter(currentUserConfigFile))
            {
                serializer.Serialize(streamWriter, currentUserConfig);
            }
        }

        public void SetupConfigFile4()
        {
            CleanupConfigFiles();

            // System wide config file is empty
            CreateEmptyFile(systemWideConfigFile);

            // Current user config file is empty
            CreateEmptyFile(currentUserConfigFile);
        }

        private static void CreateEmptyFile(string fileName)
        {
            File.Create(fileName).Dispose();
        }

        public void SetupConfigFile5()
        {
            CleanupConfigFiles();

            // System wide config file is broken
            CreateBrokenConfigFile(systemWideConfigFile);

            // Current user config file is broken
            CreateBrokenConfigFile(currentUserConfigFile);
        }

        private static void CreateBrokenConfigFile(string fileName)
        {
            File.WriteAllText(fileName, "[abbra");
        }

        /// <summary>
        /// Writes powershell.config.json content for the AllUsers ($PSHOME) and CurrentUser scopes and,
        /// optionally, provisions a MachineFolder scope backed by a temp file (as happens for MSIX installs
        /// where system-wide settings live in an admin-writable per-machine data store). Pass null for a
        /// scope to leave it without a config file. The MachineFolder scope is disabled again whenever
        /// <paramref name="machineFolderConfig"/> is null so tests do not leak state to one another.
        /// </summary>
        public void SetupWinCompatConfig(object allUsersConfig, object currentUserConfig, object machineFolderConfig = null)
        {
            CleanupConfigFiles();
            CleanupMachineFolderConfig();

            if (allUsersConfig != null)
            {
                WriteConfigFile(systemWideConfigFile, allUsersConfig);
            }

            if (currentUserConfig != null)
            {
                WriteConfigFile(currentUserConfigFile, currentUserConfig);
            }

            if (machineFolderConfig != null)
            {
                Directory.CreateDirectory(machineFolderTestConfigDirectory);
                WriteConfigFile(machineFolderTestConfigFile, machineFolderConfig);
                machineFolderConfigFileField.SetValue(PowerShellConfig.Instance, machineFolderTestConfigFile);
            }
        }

        private void WriteConfigFile(string path, object config)
        {
            using var streamWriter = new StreamWriter(path);
            serializer.Serialize(streamWriter, config);
        }

        private void CleanupMachineFolderConfig()
        {
            // Restore the process-wide singleton field and remove any temp files created for MachineFolder tests.
            machineFolderConfigFileField.SetValue(PowerShellConfig.Instance, originalMachineFolderConfigFile);
            if (Directory.Exists(machineFolderTestConfigDirectory))
            {
                Directory.Delete(machineFolderTestConfigDirectory, recursive: true);
            }
        }

        internal void ForceReadingFromFile()
        {
            // Reset the cached roots for every scope (AllUsers, CurrentUser, MachineFolder).
            FieldInfo roots = typeof(PowerShellConfig).GetField("configRoots", BindingFlags.NonPublic | BindingFlags.Instance);
            JObject[] value = (JObject[])roots.GetValue(PowerShellConfig.Instance);
            for (int i = 0; i < value.Length; i++)
            {
                value[i] = null;
            }
        }

        #endregion
    }

    public class PowerShellPolicyTests : IClassFixture<PowerShellPolicyFixture>
    {
        private readonly PowerShellPolicyFixture fixture;

        public PowerShellPolicyTests(PowerShellPolicyFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact, Priority(1)]
        public void PowerShellConfig_GetPowerShellPolicies_BothConfigFilesNotEmpty()
        {
            fixture.SetupConfigFile1();
            fixture.ForceReadingFromFile();

            var sysPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers);
            var userPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser);

            Assert.NotNull(sysPolicies);
            Assert.NotNull(userPolicies);

            fixture.CompareTwoPolicies(sysPolicies, fixture.SystemWidePolicies);
            fixture.CompareTwoPolicies(userPolicies, fixture.CurrentUserPolicies);
        }

        [Fact, Priority(2)]
        public void PowerShellConfig_GetPowerShellPolicies_EmptyUserConfig()
        {
            fixture.SetupConfigFile2();
            fixture.ForceReadingFromFile();

            var sysPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers);
            var userPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser);

            Assert.NotNull(sysPolicies);
            Assert.Null(userPolicies);

            fixture.CompareTwoPolicies(sysPolicies, fixture.SystemWidePolicies);
        }

        [Fact, Priority(3)]
        public void PowerShellConfig_GetPowerShellPolicies_EmptySystemConfig()
        {
            fixture.SetupConfigFile3();
            fixture.ForceReadingFromFile();

            var sysPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers);
            var userPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser);

            Assert.Null(sysPolicies);
            Assert.NotNull(userPolicies);

            fixture.CompareTwoPolicies(userPolicies, fixture.CurrentUserPolicies);
        }

        [Fact, Priority(4)]
        public void PowerShellConfig_GetPowerShellPolicies_BothConfigFilesEmpty()
        {
            fixture.SetupConfigFile4();
            fixture.ForceReadingFromFile();

            var sysPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers);
            var userPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser);

            Assert.Null(sysPolicies);
            Assert.Null(userPolicies);
        }

        [Fact, Priority(5)]
        public void PowerShellConfig_GetPowerShellPolicies_BothConfigFilesNotExist()
        {
            fixture.CleanupConfigFiles();
            fixture.ForceReadingFromFile();

            var sysPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers);
            var userPolicies = PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser);

            Assert.Null(sysPolicies);
            Assert.Null(userPolicies);
        }

        [Fact, Priority(6)]
        public void Utils_GetPolicySetting_BothConfigFilesNotEmpty()
        {
            fixture.SetupConfigFile1();
            fixture.ForceReadingFromFile();

            ScriptExecution scriptExecution;
            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.SystemWidePolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.CurrentUserPolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.SystemWidePolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.CurrentUserPolicies.ScriptExecution);

            ScriptBlockLogging scriptBlockLogging;
            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.SystemWidePolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.CurrentUserPolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.SystemWidePolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.CurrentUserPolicies.ScriptBlockLogging);

            ModuleLogging moduleLogging;
            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.SystemWidePolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.CurrentUserPolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.SystemWidePolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.CurrentUserPolicies.ModuleLogging);

            ProtectedEventLogging protectedEventLogging;
            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.SystemWidePolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.CurrentUserPolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.SystemWidePolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.CurrentUserPolicies.ProtectedEventLogging);

            // The CurrentUser config doesn't contain any settings for 'Transcription', 'UpdatableHelp' and 'ConsoleSessionConfiguration'
            Transcription transcription;
            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideOnlyConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            UpdatableHelp updatableHelp;
            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            ConsoleSessionConfiguration consoleSessionConfiguration;
            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);
        }

        [Fact, Priority(7)]
        public void Utils_GetPolicySetting_EmptyUserConfig()
        {
            fixture.SetupConfigFile2();
            fixture.ForceReadingFromFile();

            // The CurrentUser config is empty
            ScriptExecution scriptExecution;
            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.SystemWidePolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.SystemWidePolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.SystemWidePolicies.ScriptExecution);

            ScriptBlockLogging scriptBlockLogging;
            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.SystemWidePolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.SystemWidePolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.SystemWidePolicies.ScriptBlockLogging);

            ModuleLogging moduleLogging;
            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.SystemWidePolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.SystemWidePolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.SystemWidePolicies.ModuleLogging);

            ProtectedEventLogging protectedEventLogging;
            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.SystemWidePolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.SystemWidePolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.SystemWidePolicies.ProtectedEventLogging);

            Transcription transcription;
            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideOnlyConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareTranscription(transcription, fixture.SystemWidePolicies.Transcription);

            UpdatableHelp updatableHelp;
            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareUpdatableHelp(updatableHelp, fixture.SystemWidePolicies.UpdatableHelp);

            ConsoleSessionConfiguration consoleSessionConfiguration;
            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, fixture.SystemWidePolicies.ConsoleSessionConfiguration);
        }

        [Fact, Priority(8)]
        public void Utils_GetPolicySetting_EmptySystemConfig()
        {
            fixture.SetupConfigFile3();
            fixture.ForceReadingFromFile();

            // The SystemWide config is empty
            ScriptExecution scriptExecution;
            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.CurrentUserPolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.CurrentUserPolicies.ScriptExecution);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptExecution(scriptExecution, fixture.CurrentUserPolicies.ScriptExecution);

            ScriptBlockLogging scriptBlockLogging;
            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.CurrentUserPolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.CurrentUserPolicies.ScriptBlockLogging);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, fixture.CurrentUserPolicies.ScriptBlockLogging);

            ModuleLogging moduleLogging;
            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.CurrentUserPolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.CurrentUserPolicies.ModuleLogging);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareModuleLogging(moduleLogging, fixture.CurrentUserPolicies.ModuleLogging);

            ProtectedEventLogging protectedEventLogging;
            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.CurrentUserPolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.CurrentUserPolicies.ProtectedEventLogging);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, fixture.CurrentUserPolicies.ProtectedEventLogging);

            // The CurrentUser config doesn't contain any settings for 'Transcription', 'UpdatableHelp' and 'ConsoleSessionConfiguration'
            Transcription transcription;
            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareTranscription(transcription, null);

            UpdatableHelp updatableHelp;
            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            ConsoleSessionConfiguration consoleSessionConfiguration;
            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);
        }

        [Fact, Priority(9)]
        public void Utils_GetPolicySetting_BothConfigFilesEmpty()
        {
            fixture.SetupConfigFile4();
            fixture.ForceReadingFromFile();

            // Both config files are empty
            ScriptExecution scriptExecution;
            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            ScriptBlockLogging scriptBlockLogging;
            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            ModuleLogging moduleLogging;
            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            ProtectedEventLogging protectedEventLogging;
            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            // The CurrentUser config doesn't contain any settings for 'Transcription', 'UpdatableHelp' and 'ConsoleSessionConfiguration'
            Transcription transcription;
            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareTranscription(transcription, null);

            UpdatableHelp updatableHelp;
            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            ConsoleSessionConfiguration consoleSessionConfiguration;
            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);
        }

        [Fact, Priority(10)]
        public void Utils_GetPolicySetting_BothConfigFilesNotExist()
        {
            fixture.CleanupConfigFiles();
            fixture.ForceReadingFromFile();

            // Both config files don't exist
            ScriptExecution scriptExecution;
            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            scriptExecution = Utils.GetPolicySetting<ScriptExecution>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptExecution(scriptExecution, null);

            ScriptBlockLogging scriptBlockLogging;
            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            scriptBlockLogging = Utils.GetPolicySetting<ScriptBlockLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareScriptBlockLogging(scriptBlockLogging, null);

            ModuleLogging moduleLogging;
            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            moduleLogging = Utils.GetPolicySetting<ModuleLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareModuleLogging(moduleLogging, null);

            ProtectedEventLogging protectedEventLogging;
            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserOnlyConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            protectedEventLogging = Utils.GetPolicySetting<ProtectedEventLogging>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareProtectedEventLogging(protectedEventLogging, null);

            // The CurrentUser config doesn't contain any settings for 'Transcription', 'UpdatableHelp' and 'ConsoleSessionConfiguration'
            Transcription transcription;
            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserOnlyConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareTranscription(transcription, null);

            transcription = Utils.GetPolicySetting<Transcription>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareTranscription(transcription, null);

            UpdatableHelp updatableHelp;
            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserOnlyConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            updatableHelp = Utils.GetPolicySetting<UpdatableHelp>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareUpdatableHelp(updatableHelp, null);

            ConsoleSessionConfiguration consoleSessionConfiguration;
            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserOnlyConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.SystemWideThenCurrentUserConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);

            consoleSessionConfiguration = Utils.GetPolicySetting<ConsoleSessionConfiguration>(Utils.CurrentUserThenSystemWideConfig);
            fixture.CompareConsoleSessionConfiguration(consoleSessionConfiguration, null);
        }

        [Fact, Priority(11)]
        public void PowerShellConfig_GetPowerShellPolicies_BrokenSystemConfig()
        {
            fixture.SetupConfigFile5();
            fixture.ForceReadingFromFile();

            Assert.Throws<System.Management.Automation.PSInvalidOperationException>(() => PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.AllUsers));
            Assert.Throws<System.Management.Automation.PSInvalidOperationException>(() => PowerShellConfig.Instance.GetPowerShellPolicies(ConfigScope.CurrentUser));
        }

        #region WindowsPowerShellCompatibility_Merge

        // The Windows PowerShell compatibility module deny list is a *policy*: its entries are unioned across
        // every config scope so a module added by a product update in $PSHOME (AllUsers) is never dropped
        // because a higher-precedence scope (MachineFolder or CurrentUser) also defines the key.

        [Fact, Priority(12)]
        public void PowerShellConfig_GetWinCompatModuleDenyList_UnionsAcrossScopes()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "SystemModule" } },
                currentUserConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "UserModule" } });
            fixture.ForceReadingFromFile();

            string[] denyList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityModuleDenyList();

            Assert.NotNull(denyList);
            Assert.Equal(2, denyList.Length);
            Assert.Contains("SystemModule", denyList);
            Assert.Contains("UserModule", denyList);
        }

        [Fact, Priority(13)]
        public void PowerShellConfig_GetWinCompatModuleDenyList_DeduplicatesCaseInsensitively()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "DuplicateModule" } },
                currentUserConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "duplicatemodule" } });
            fixture.ForceReadingFromFile();

            string[] denyList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityModuleDenyList();

            Assert.NotNull(denyList);
            Assert.Single(denyList);
        }

        [Fact, Priority(14)]
        public void PowerShellConfig_GetWinCompatModuleDenyList_IncludesMachineFolderScope()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "SystemModule" } },
                currentUserConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "UserModule" } },
                machineFolderConfig: new { WindowsPowerShellCompatibilityModuleDenyList = new[] { "MachineModule" } });
            fixture.ForceReadingFromFile();

            string[] denyList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityModuleDenyList();

            Assert.NotNull(denyList);
            Assert.Equal(3, denyList.Length);
            Assert.Contains("SystemModule", denyList);
            Assert.Contains("UserModule", denyList);
            Assert.Contains("MachineModule", denyList);
        }

        [Fact, Priority(15)]
        public void PowerShellConfig_GetWinCompatModuleDenyList_ReturnsNullWhenUndefined()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { ConsolePrompting = true },
                currentUserConfig: new { ConsolePrompting = true });
            fixture.ForceReadingFromFile();

            string[] denyList = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityModuleDenyList();

            Assert.Null(denyList);
        }

        // The no-clobber module list is a *preference*: only the highest-precedence scope that defines it
        // wins (CurrentUser > MachineFolder > AllUsers); lower scopes are ignored once a higher one sets it.

        [Fact, Priority(16)]
        public void PowerShellConfig_GetWinCompatNoClobberList_CurrentUserWins()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityNoClobberModuleList = new[] { "SystemModule" } },
                currentUserConfig: new { WindowsPowerShellCompatibilityNoClobberModuleList = new[] { "UserModule" } });
            fixture.ForceReadingFromFile();

            string[] noClobber = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityNoClobberModuleList();

            Assert.NotNull(noClobber);
            Assert.Single(noClobber);
            Assert.Equal("UserModule", noClobber[0]);
        }

        [Fact, Priority(17)]
        public void PowerShellConfig_GetWinCompatNoClobberList_FallsBackToAllUsers()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityNoClobberModuleList = new[] { "SystemModule" } },
                currentUserConfig: new { ConsolePrompting = true });
            fixture.ForceReadingFromFile();

            string[] noClobber = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityNoClobberModuleList();

            Assert.NotNull(noClobber);
            Assert.Single(noClobber);
            Assert.Equal("SystemModule", noClobber[0]);
        }

        [Fact, Priority(18)]
        public void PowerShellConfig_GetWinCompatNoClobberList_MachineFolderOverridesAllUsers()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { WindowsPowerShellCompatibilityNoClobberModuleList = new[] { "SystemModule" } },
                currentUserConfig: new { ConsolePrompting = true },
                machineFolderConfig: new { WindowsPowerShellCompatibilityNoClobberModuleList = new[] { "MachineModule" } });
            fixture.ForceReadingFromFile();

            string[] noClobber = PowerShellConfig.Instance.GetWindowsPowerShellCompatibilityNoClobberModuleList();

            Assert.NotNull(noClobber);
            Assert.Single(noClobber);
            Assert.Equal("MachineModule", noClobber[0]);
        }

        // DisableImplicitWinCompat is a *preference* bool: IsImplicitWinCompatEnabled() returns the negation
        // of the value from the highest-precedence scope that sets it, defaulting to enabled when unset.

        [Fact, Priority(19)]
        public void PowerShellConfig_IsImplicitWinCompatEnabled_TrueWhenUndefined()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { ConsolePrompting = true },
                currentUserConfig: new { ConsolePrompting = true });
            fixture.ForceReadingFromFile();

            Assert.True(PowerShellConfig.Instance.IsImplicitWinCompatEnabled());
        }

        [Fact, Priority(20)]
        public void PowerShellConfig_IsImplicitWinCompatEnabled_FalseWhenAllUsersDisables()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { DisableImplicitWinCompat = true },
                currentUserConfig: new { ConsolePrompting = true });
            fixture.ForceReadingFromFile();

            Assert.False(PowerShellConfig.Instance.IsImplicitWinCompatEnabled());
        }

        [Fact, Priority(21)]
        public void PowerShellConfig_IsImplicitWinCompatEnabled_CurrentUserReEnables()
        {
            fixture.SetupWinCompatConfig(
                allUsersConfig: new { DisableImplicitWinCompat = true },
                currentUserConfig: new { DisableImplicitWinCompat = false });
            fixture.ForceReadingFromFile();

            Assert.True(PowerShellConfig.Instance.IsImplicitWinCompatEnabled());
        }

        #endregion
    }
}
