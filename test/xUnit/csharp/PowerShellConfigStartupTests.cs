// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Configuration;
using System.Reflection;
using Xunit;

namespace PSTests.Sequential
{
    public sealed class PowerShellConfigStartupTests : IDisposable
    {
        private readonly DirectoryInfo configDirectory = Directory.CreateTempSubdirectory("PSConfiguration-");
        private readonly PowerShellConfig config;
        private readonly string configPath;

        public PowerShellConfigStartupTests()
        {
            configPath = Path.Combine(configDirectory.FullName, "powershell.config.json");
            File.WriteAllText(configPath, "{}");
            config = (PowerShellConfig)Activator.CreateInstance(typeof(PowerShellConfig), nonPublic: true);
            config.SetSystemConfigFilePath(configPath);
            typeof(PowerShellConfig).GetField("perUserConfigFile", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(config, configPath);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \r\n ")]
        [InlineData("/* empty configuration */")]
        [InlineData("null")]
        [InlineData("/* configuration */ null")]
        [InlineData("{}")]
        public void EmptyConfigurationUsesDefaults(string contents)
        {
            File.WriteAllText(configPath, contents);
            Assert.Null(config.GetModulePath(ConfigScope.AllUsers));
            Assert.Null(config.GetPowerShellPolicies(ConfigScope.AllUsers));
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("true")]
        [InlineData("{broken")]
        [InlineData("{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":{\"a\":0}}}}}}}}}}}")]
        public void InvalidConfigurationPreservesScopeErrorHandling(string contents)
        {
            File.WriteAllText(configPath, contents);
            Assert.Throws<PSInvalidOperationException>(() => config.GetModulePath(ConfigScope.AllUsers));
            Assert.Null(config.GetModulePath(ConfigScope.CurrentUser));
        }

        [Fact]
        public void ConfigurationPreservesCommentsDuplicateKeysAndTypedPolicies()
        {
            string contents = """
                /* configuration */
                {
                    "psmodulepath": "first",
                    "psmodulepath": "second",
                    "PowerShellPolicies": {
                        "ScriptBlockLogging": { "EnableScriptBlockLogging": true }
                    }
                }
                """;
            File.WriteAllText(configPath, contents);

            Assert.Equal("second", config.GetModulePath(ConfigScope.AllUsers));
            Assert.True(config.GetPowerShellPolicies(ConfigScope.AllUsers).ScriptBlockLogging.EnableScriptBlockLogging);
        }

        [Fact]
        public void UpdatingConfigurationPreservesExistingProperties()
        {
            File.WriteAllText(configPath, """{"PSModulePath":"existing"}""");
            config.SetExecutionPolicy(ConfigScope.AllUsers, "Microsoft.PowerShell", "RemoteSigned");

            Assert.Equal("existing", config.GetModulePath(ConfigScope.AllUsers));
            Assert.Equal("RemoteSigned", config.GetExecutionPolicy(ConfigScope.AllUsers, "Microsoft.PowerShell"));
        }

        public void Dispose()
        {
            File.Delete(configPath);
            configDirectory.Delete();
        }
    }
}
