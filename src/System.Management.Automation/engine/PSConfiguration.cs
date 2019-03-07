// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Configuration
{
    /// <summary>
    /// The scope of the configuration file.
    /// </summary>
    public enum ConfigScope
    {
        /// <summary>
        /// AllUsers configuration applies to all users.
        /// </summary>
        AllUsers = 0,

        /// <summary>
        /// CurrentUser configuration applies to the current user.
        /// </summary>
        CurrentUser = 1
    }

    /// <summary>
    /// Reads from and writes to the JSON configuration files.
    /// The config values were originally stored in the Windows registry.
    /// </summary>
    internal sealed class PowerShellConfig
    {
        // Provide a singleton
        private static readonly PowerShellConfig s_instance = new PowerShellConfig();
        internal static PowerShellConfig Instance => s_instance;

        // The json file containing system-wide configuration settings.
        // When passed as a pwsh command-line option,
        // overrides the system wide configuration file.
        private string systemWideConfigFile;
        private string systemWideConfigDirectory;

        // The json file containing the per-user configuration settings.
        private string perUserConfigFile;
        private string perUserConfigDirectory;

        private const string configFileName = "powershell.config.json";

        /// <summary>
        /// Lock used to enable multiple concurrent readers and singular write locks within a single process.
        /// TODO: This solution only works for IO from a single process.
        ///       A more robust solution is needed to enable ReaderWriterLockSlim behavior between processes.
        /// </summary>
        private ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim();

        private PowerShellConfig()
        {
            // Sets the system-wide configuration file.
            systemWideConfigDirectory = Utils.DefaultPowerShellAppBase;
            systemWideConfigFile = Path.Combine(systemWideConfigDirectory, configFileName);

            // Sets the per-user configuration directory
            // Note: This directory may or may not exist depending upon the
            // execution scenario. Writes will attempt to create the directory
            // if it does not already exist.
            perUserConfigDirectory = Utils.GetUserConfigurationDirectory();
            perUserConfigFile = Path.Combine(perUserConfigDirectory, configFileName);
        }

        private string GetConfigFilePath(ConfigScope scope)
        {
            return (scope == ConfigScope.CurrentUser) ? perUserConfigFile : systemWideConfigFile;
        }

        /// <summary>
        /// Sets the system wide configuration file path.
        /// </summary>
        /// <param name="value">A fully qualified path to the system wide configuration file.</param>
        /// <exception cref="FileNotFoundException"><paramref name="value"/> is a null reference or the associated file does not exist.</exception>
        /// <remarks>
        /// This method is for use when processing the -SettingsFile configuration setting and should not be used for any other purpose.
        /// </remarks>
        internal void SetSystemConfigFilePath(string value)
        {
            if (!string.IsNullOrEmpty(value) && !File.Exists(value))
            {
                throw new FileNotFoundException(value);
            }

            FileInfo info = new FileInfo(value);
            systemWideConfigFile = info.FullName;
            systemWideConfigDirectory = info.Directory.FullName;
        }

        /// <summary>
        /// Existing Key = HKLM:\System\CurrentControlSet\Control\Session Manager\Environment
        /// Proposed value = %ProgramFiles%\PowerShell\Modules by default
        /// Note: There is no setter because this value is immutable.
        /// </summary>
        /// <param name="scope">Whether this is a system-wide or per-user setting.</param>
        /// <returns>Value if found, null otherwise. The behavior matches ModuleIntrinsics.GetExpandedEnvironmentVariable().</returns>
        internal string GetModulePath(ConfigScope scope)
        {
            string modulePath = ReadValueFromFile<string>(scope, Constants.PSModulePathEnvVar);
            if (!string.IsNullOrEmpty(modulePath))
            {
                modulePath = Environment.ExpandEnvironmentVariables(modulePath);
            }

            return modulePath;
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
        /// Proposed value = Existing default execution policy if not already specified
        ///
        /// Schema:
        /// {
        ///     "shell-ID-string:ExecutionPolicy" : "execution policy string"
        /// }
        ///
        /// TODO: In a single config file, it might be better to nest this. It is unnecessary complexity until a need arises for more nested values.
        /// </summary>
        /// <param name="scope">Whether this is a system-wide or per-user setting.</param>
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell".</param>
        /// <returns>The execution policy if found. Null otherwise.</returns>
        internal string GetExecutionPolicy(ConfigScope scope, string shellId)
        {
            string execPolicy = null;

            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            string rawExecPolicy = ReadValueFromFile<string>(scope, valueName);

            if (!string.IsNullOrEmpty(rawExecPolicy))
            {
                execPolicy = rawExecPolicy;
            }

            return execPolicy;
        }

        internal void RemoveExecutionPolicy(ConfigScope scope, string shellId)
        {
            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            RemoveValueFromFile<string>(scope, valueName);
        }

        internal void SetExecutionPolicy(ConfigScope scope, string shellId, string executionPolicy)
        {
            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            WriteValueToFile<string>(scope, valueName, executionPolicy);
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = existing default. Probably "1"
        ///
        /// Schema:
        /// {
        ///     "ConsolePrompting" : bool
        /// }
        /// </summary>
        /// <returns>Whether console prompting should happen. If the value cannot be read it defaults to false.</returns>
        internal bool GetConsolePrompting()
        {
            return ReadValueFromFile<bool>(ConfigScope.AllUsers, "ConsolePrompting");
        }

        internal void SetConsolePrompting(bool shouldPrompt)
        {
            WriteValueToFile<bool>(ConfigScope.AllUsers, "ConsolePrompting", shouldPrompt);
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell
        /// Proposed value = Existing default. Probably "0"
        ///
        /// Schema:
        /// {
        ///     "DisablePromptToUpdateHelp" : bool
        /// }
        /// </summary>
        /// <returns>Boolean indicating whether Update-Help should prompt. If the value cannot be read, it defaults to false.</returns>
        internal bool GetDisablePromptToUpdateHelp()
        {
            return ReadValueFromFile<bool>(ConfigScope.AllUsers, "DisablePromptToUpdateHelp");
        }

        internal void SetDisablePromptToUpdateHelp(bool prompt)
        {
            WriteValueToFile<bool>(ConfigScope.AllUsers, "DisablePromptToUpdateHelp", prompt);
        }

        /// <summary>
        /// Get the names of experimental features enabled in the config file.
        /// </summary>
        internal string[] GetExperimentalFeatures()
        {
            string[] features = Array.Empty<string>();
            if (File.Exists(perUserConfigFile))
            {
                features = ReadValueFromFile<string[]>(ConfigScope.CurrentUser, "ExperimentalFeatures", Array.Empty<string>());
            }

            if (features.Length == 0)
            {
                features = ReadValueFromFile<string[]>(ConfigScope.AllUsers, "ExperimentalFeatures", Array.Empty<string>());
            }

            return features;
        }

        /// <summary>
        /// Set the enabled list of experimental features in the config file.
        /// </summary>
        /// <param name="scope">The ConfigScope of the configuration file to update.</param>
        /// <param name="featureName">The name of the experimental feature to change in the configuration.</param>
        /// <param name="setEnabled">If true, add to configuration; otherwise, remove from configuration.</param>
        internal void SetExperimentalFeatures(ConfigScope scope, string featureName, bool setEnabled)
        {
            var features = new List<string>(GetExperimentalFeatures());
            bool containsFeature = features.Contains(featureName);
            if (setEnabled && !containsFeature)
            {
                features.Add(featureName);
                WriteValueToFile<string[]>(scope, "ExperimentalFeatures", features.ToArray());
            }
            else if (!setEnabled && containsFeature)
            {
                features.Remove(featureName);
                WriteValueToFile<string[]>(scope, "ExperimentalFeatures", features.ToArray());
            }
        }

        /// <summary>
        /// Corresponding settings of the original Group Policies.
        /// </summary>
        internal PowerShellPolicies GetPowerShellPolicies(ConfigScope scope)
        {
            return ReadValueFromFile<PowerShellPolicies>(scope, nameof(PowerShellPolicies));
        }

#if UNIX
        /// <summary>
        /// Gets the identity name to use for writing to syslog.
        /// </summary>
        /// <returns>
        /// The string identity to use for writing to syslog. The default value is 'powershell'.
        /// </returns>
        internal string GetSysLogIdentity()
        {
            string identity = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogIdentity");

            if (string.IsNullOrEmpty(identity) ||
                identity.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
            {
                identity = "powershell";
            }

            return identity;
        }

        /// <summary>
        /// Gets the log level filter.
        /// </summary>
        /// <returns>
        /// One of the PSLevel values indicating the level to log. The default value is PSLevel.Informational.
        /// </returns>
        internal PSLevel GetLogLevel()
        {
            string levelName = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogLevel");
            PSLevel level;

            if (string.IsNullOrEmpty(levelName) ||
                levelName.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase) ||
                !Enum.TryParse<PSLevel>(levelName, true, out level))
            {
                level = PSLevel.Informational;
            }

            return level;
        }

        /// <summary>
        /// The supported separator characters for listing channels and keywords in configuration.
        /// </summary>
        static readonly char[] s_valueSeparators = new char[] {' ', ',', '|'};

        /// <summary>
        /// Provides a string name to indicate the default for a configuration setting.
        /// </summary>
        const string LogDefaultValue = "default";

        /// <summary>
        /// Gets the bitmask of the PSChannel values to log.
        /// </summary>
        /// <returns>
        /// A bitmask of PSChannel.Operational and/or PSChannel.Analytic. The default value is PSChannel.Operational.
        /// </returns>
        internal PSChannel GetLogChannels()
        {
            string values = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogChannels");

            PSChannel result = 0;
            if (!string.IsNullOrEmpty(values))
            {
                string[] names = values.Split(s_valueSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string name in names)
                {
                    if (name.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = 0;
                        break;
                    }

                    PSChannel value;
                    if (Enum.TryParse<PSChannel>(name, true, out value))
                    {
                        result |= value;
                    }
                }
            }

            if (result == 0)
            {
                result = System.Management.Automation.Tracing.PSSysLogProvider.DefaultChannels;
            }

            return result;
        }

        /// <summary>
        /// Gets the bitmask of keywords to log.
        /// </summary>
        /// <returns>
        /// A bitmask of PSKeyword values. The default value is all keywords other than UseAlwaysAnalytic.
        /// </returns>
        internal PSKeyword GetLogKeywords()
        {
            string values = ReadValueFromFile<string>(ConfigScope.AllUsers, "LogKeywords");

            PSKeyword result = 0;
            if (!string.IsNullOrEmpty(values))
            {
                string[] names = values.Split(s_valueSeparators, StringSplitOptions.RemoveEmptyEntries);

                foreach (string name in names)
                {
                    if (name.Equals(LogDefaultValue, StringComparison.OrdinalIgnoreCase))
                    {
                        result = 0;
                        break;
                    }

                    PSKeyword value;
                    if (Enum.TryParse<PSKeyword>(name, true, out value))
                    {
                        result |= value;
                    }
                }
            }

            if (result == 0)
            {
                result = System.Management.Automation.Tracing.PSSysLogProvider.DefaultKeywords;
            }

            return result;
        }
#endif // UNIX

        /// <summary>
        /// Read a value from the configuration file.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="scope">The ConfigScope of the configuration file to update.</param>
        /// <param name="key">The string key of the value.</param>
        /// <param name="defaultValue">The default value to return if the key is not present.</param>
        /// <param name="readImpl"></param>
        private T ReadValueFromFile<T>(ConfigScope scope, string key, T defaultValue = default(T),
                                       Func<JToken, JsonSerializer, T, T> readImpl = null)
        {
            string fileName = GetConfigFilePath(scope);
            if (!File.Exists(fileName)) { return defaultValue; }

            // Open file for reading, but allow multiple readers
            fileLock.EnterReadLock();
            try
            {
                // The config file can be locked by another process
                // so we wait some milliseconds in 'WaitForFile()' for recovery before stop current process.
                using (var readerStream = WaitForFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var streamReader = new StreamReader(readerStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var settings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None, MaxDepth = 10 };
                    var serializer = JsonSerializer.Create(settings);

                    var configData = serializer.Deserialize<JObject>(jsonReader);
                    if (configData != null && configData.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out JToken jToken))
                    {
                        return readImpl != null ? readImpl(jToken, serializer, defaultValue) : jToken.ToObject<T>(serializer);
                    }
                }
            }
            finally
            {
                fileLock.ExitReadLock();
            }

            return defaultValue;
        }

        private FileStream WaitForFile(string fullPath, FileMode mode, FileAccess access, FileShare share)
        {
            const int MaxTries = 5;
            for (int numTries = 0; numTries < MaxTries; numTries++)
            {
                try
                {
                    return new FileStream(fullPath, mode, access, share);
                }
                catch (IOException)
                {
                    if (numTries == (MaxTries - 1))
                    {
                        throw;
                    }

                    Thread.Sleep(50);
                }
            }

            throw new IOException(nameof(WaitForFile));
        }

        /// <summary>
        /// Update a value in the configuration file.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="scope">The ConfigScope of the configuration file to update.</param>
        /// <param name="key">The string key of the value.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="addValue">Whether the key-value pair should be added to or removed from the file.</param>
        private void UpdateValueInFile<T>(ConfigScope scope, string key, T value, bool addValue)
        {
            string fileName = GetConfigFilePath(scope);
            fileLock.EnterWriteLock();
            try
            {
                // Since multiple properties can be in a single file, replacement
                // is required instead of overwrite if a file already exists.
                // Handling the read and write operations within a single FileStream
                // prevents other processes from reading or writing the file while
                // the update is in progress. It also locks out readers during write
                // operations.
                using (FileStream fs = WaitForFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    JObject jsonObject = null;

                    // UTF8, BOM detection, and bufferSize are the same as the basic stream constructor.
                    // The most important parameter here is the last one, which keeps the StreamReader
                    // (and FileStream) open during Dispose so that it can be reused for the write
                    // operation.
                    using (StreamReader streamRdr = new StreamReader(fs, Encoding.UTF8, true, 1024, true))
                    using (JsonTextReader jsonReader = new JsonTextReader(streamRdr))
                    {
                        // Safely determines whether there is content to read from the file
                        bool isReadSuccess = jsonReader.Read();
                        if (isReadSuccess)
                        {
                            // Read the stream into a root JObject for manipulation
                            jsonObject = (JObject) JToken.ReadFrom(jsonReader);
                            JProperty propertyToModify = jsonObject.Property(key);

                            if (propertyToModify == null)
                            {
                                // The property doesn't exist, so add it
                                if (addValue)
                                {
                                    jsonObject.Add(new JProperty(key, value));
                                }
                                // else the property doesn't exist so there is nothing to remove
                            }
                            // The property exists
                            else
                            {
                                if (addValue)
                                {
                                    propertyToModify.Replace(new JProperty(key, value));
                                }
                                else
                                {
                                    propertyToModify.Remove();
                                }
                            }
                        }
                        else
                        {
                            // The file doesn't already exist and we want to write to it
                            // or it exists with no content.
                            // A new file will be created that contains only this value.
                            // If the file doesn't exist and a we don't want to write to it, no
                            // action is necessary.
                            if (addValue)
                            {
                                jsonObject = new JObject(new JProperty(key, value));
                            }
                            else
                            {
                                return;
                            }
                        }
                    }

                    // Reset the stream position to the beginning so that the
                    // changes to the file can be written to disk
                    fs.Seek(0, SeekOrigin.Begin);

                    // Update the file with new content
                    using (StreamWriter streamWriter = new StreamWriter(fs))
                    using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                    {
                        // The entire document exists within the root JObject.
                        // I just need to write that object to produce the document.
                        jsonObject.WriteTo(jsonWriter);

                        // This trims the file if the file shrank. If the file grew,
                        // it is a no-op. The purpose is to trim extraneous characters
                        // from the file stream when the resultant JObject is smaller
                        // than the input JObject.
                        fs.SetLength(fs.Position);
                    }
                }
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// TODO: Should this return success, fail, or throw?
        /// </summary>
        /// <typeparam name="T">The type of value to write.</typeparam>
        /// <param name="scope">The ConfigScope of the file to update.</param>
        /// <param name="key">The string key of the value.</param>
        /// <param name="value">The value to write.</param>
        private void WriteValueToFile<T>(ConfigScope scope, string key, T value)
        {
            // Defaults to system wide.
            if (ConfigScope.CurrentUser == scope)
            {
                // Exceptions are not caught so that they will propagate to the
                // host for display to the user.
                // CreateDirectory will succeed if the directory already exists
                // so there is no reason to check Directory.Exists().
                Directory.CreateDirectory(perUserConfigDirectory);
            }

            UpdateValueInFile<T>(scope, key, value, true);
        }

        /// <summary>
        /// TODO: Should this return success, fail, or throw?
        /// </summary>
        /// <typeparam name="T">The type of value to remove.</typeparam>
        /// <param name="scope">The ConfigScope of the file to update.</param>
        /// <param name="key">The string key of the value.</param>
        private void RemoveValueFromFile<T>(ConfigScope scope, string key)
        {
            string fileName = GetConfigFilePath(scope);
            // Optimization: If the file doesn't exist, there is nothing to remove
            if (File.Exists(fileName))
            {
                UpdateValueInFile<T>(scope, key, default(T), false);
            }
        }
    }

    #region GroupPolicy Configs

    /// <summary>
    /// The GroupPolicy related settings used in PowerShell are as follows in Registry:
    ///  - Software\Policies\Microsoft\PowerShellCore -- { EnableScripts (0 or 1); ExecutionPolicy (string) }
    ///      SubKeys                  Name-Value-Pairs
    ///      - ScriptBlockLogging     { EnableScriptBlockLogging (0 or 1); EnableScriptBlockInvocationLogging (0 or 1) }
    ///      - ModuleLogging          { EnableModuleLogging (0 or 1); ModuleNames (string[]) }
    ///      - Transcription          { EnableTranscripting (0 or 1); OutputDirectory (string); EnableInvocationHeader (0 or 1) }
    ///      - UpdatableHelp          { DefaultSourcePath (string) }
    ///      - ConsoleSessionConfiguration { EnableConsoleSessionConfiguration (0 or 1); ConsoleSessionConfigurationName (string) }
    ///  - Software\Policies\Microsoft\Windows\EventLog
    ///     SubKeys                   Name-Value-Pairs
    ///      - ProtectedEventLogging  { EnableProtectedEventLogging (0 or 1); EncryptionCertificate (string[]) }
    ///
    /// The JSON representation is in sync with the 'PowerShellPolicies' type. Here is an example:
    /// {
    ///   "PowerShellPolicies": {
    ///     "ScriptExecution": {
    ///       "ExecutionPolicy": "RemoteSigned"
    ///     },
    ///     "ScriptBlockLogging": {
    ///       "EnableScriptBlockInvocationLogging": true,
    ///       "EnableScriptBlockLogging": false
    ///     },
    ///     "ProtectedEventLogging": {
    ///       "EnableProtectedEventLogging": false,
    ///       "EncryptionCertificate": [
    ///         "Joe"
    ///       ]
    ///     },
    ///     "Transcription": {
    ///       "EnableTranscripting": true,
    ///       "EnableInvocationHeader": true,
    ///       "OutputDirectory": "c:\\tmp"
    ///     },
    ///     "UpdatableHelp": {
    ///       "DefaultSourcePath": "f:\\temp"
    ///     },
    ///     "ConsoleSessionConfiguration": {
    ///       "EnableConsoleSessionConfiguration": true,
    ///       "ConsoleSessionConfigurationName": "name"
    ///     }
    ///   }
    /// }
    /// </summary>
    internal sealed class PowerShellPolicies
    {
        public ScriptExecution ScriptExecution { get; set; }
        public ScriptBlockLogging ScriptBlockLogging { get; set; }
        public ModuleLogging ModuleLogging { get; set; }
        public ProtectedEventLogging ProtectedEventLogging { get; set; }
        public Transcription Transcription { get; set; }
        public UpdatableHelp UpdatableHelp { get; set; }
        public ConsoleSessionConfiguration ConsoleSessionConfiguration { get; set; }
    }

    internal abstract class PolicyBase { }

    /// <summary>
    /// Setting about ScriptExecution.
    /// </summary>
    internal sealed class ScriptExecution : PolicyBase
    {
        public string ExecutionPolicy { get; set; }
        public bool? EnableScripts { get; set; }
    }

    /// <summary>
    /// Setting about ScriptBlockLogging.
    /// </summary>
    internal sealed class ScriptBlockLogging : PolicyBase
    {
        public bool? EnableScriptBlockInvocationLogging { get; set; }
        public bool? EnableScriptBlockLogging { get; set; }
    }

    /// <summary>
    /// Setting about ModuleLogging.
    /// </summary>
    internal sealed class ModuleLogging : PolicyBase
    {
        public bool? EnableModuleLogging { get; set; }
        public string[] ModuleNames { get; set; }
    }

    /// <summary>
    /// Setting about Transcription.
    /// </summary>
    internal sealed class Transcription : PolicyBase
    {
        public bool? EnableTranscripting { get; set; }
        public bool? EnableInvocationHeader { get; set; }
        public string OutputDirectory { get; set; }
    }

    /// <summary>
    /// Setting about UpdatableHelp.
    /// </summary>
    internal sealed class UpdatableHelp : PolicyBase
    {
        public bool? EnableUpdateHelpDefaultSourcePath { get; set; }
        public string DefaultSourcePath { get; set; }
    }

    /// <summary>
    /// Setting about ConsoleSessionConfiguration.
    /// </summary>
    internal sealed class ConsoleSessionConfiguration : PolicyBase
    {
        public bool? EnableConsoleSessionConfiguration { get; set; }
        public string ConsoleSessionConfigurationName { get; set; }
    }

    /// <summary>
    /// Setting about ProtectedEventLogging.
    /// </summary>
    internal sealed class ProtectedEventLogging : PolicyBase
    {
        public bool? EnableProtectedEventLogging { get; set; }
        public string[] EncryptionCertificate { get; set; }
    }

    #endregion
}
