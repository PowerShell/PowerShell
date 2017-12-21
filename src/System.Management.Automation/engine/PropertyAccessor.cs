using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace System.Management.Automation.Configuration
{
    /// <summary>
    /// JSON configuration file accessor.
    /// Reads from and writes to configuration files. The config values were originally stored in the Windows registry.
    /// </summary>
    internal sealed class PowerShellConfig
    {
        #region Enums

        /// <summary>
        /// Describes the scope of the property query.
        /// SystemWide properties apply to all users.
        /// CurrentUser properties apply to the current user that is impersonated.
        /// </summary>
        internal enum PropertyScope
        {
            SystemWide = 0,
            CurrentUser = 1
        }

        #endregion // Enums

        // Provide a singleton
        private static readonly PowerShellConfig s_instance = new PowerShellConfig();
        internal static PowerShellConfig Instance => s_instance;

        private string psHomeConfigDirectory;
        private string appDataConfigDirectory;
        private const string configFileName = "PowerShellProperties.json";

        /// <summary>
        /// Lock used to enable multiple concurrent readers and singular write locks within a single process.
        /// TODO: This solution only works for IO from a single process.
        ///       A more robust solution is needed to enable ReaderWriterLockSlim behavior between processes.
        /// </summary>
        private ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim();

        private PowerShellConfig()
        {
            // Sets the system-wide configuration directory
            psHomeConfigDirectory = Utils.DefaultPowerShellAppBase;

            // Sets the per-user configuration directory
            // Note: This directory may or may not exist depending upon the
            // execution scenario. Writes will attempt to create the directory
            // if it does not already exist.
            appDataConfigDirectory = Utils.GetUserConfigurationDirectory();
        }

        /// <summary>
        /// Existing Key = HKLM:\System\CurrentControlSet\Control\Session Manager\Environment
        /// Proposed value = %ProgramFiles%\PowerShell\Modules by default
        /// Note: There is no setter because this value is immutable.
        /// </summary>
        /// <param name="scope">Whether this is a system-wide or per-user setting.</param>
        /// <returns>Value if found, null otherwise. The behavior matches ModuleIntrinsics.GetExpandedEnvironmentVariable().</returns>
        internal string GetModulePath(PropertyScope scope)
        {
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if (PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, configFileName);

            string modulePath = ReadValueFromFile<string>(fileName, Constants.PSModulePathEnvVar);
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
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell"</param>
        /// <returns>The execution policy if found. Null otherwise.</returns>
        internal string GetExecutionPolicy(PropertyScope scope, string shellId)
        {
            string execPolicy = null;
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if(PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, configFileName);
            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            string rawExecPolicy = ReadValueFromFile<string>(fileName, valueName);

            if (!String.IsNullOrEmpty(rawExecPolicy))
            {
                execPolicy = rawExecPolicy;
            }
            return execPolicy;
        }

        internal void RemoveExecutionPolicy(PropertyScope scope, string shellId)
        {
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if (PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, configFileName);
            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            RemoveValueFromFile<string>(fileName, valueName);
        }

        internal void SetExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy)
        {
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if (PropertyScope.CurrentUser == scope)
            {
                // Exceptions are not caught so that they will propagate to the
                // host for display to the user.
                // CreateDirectory will succeed if the directory already exists
                // so there is no reason to check Directory.Exists().
                Directory.CreateDirectory(appDataConfigDirectory);
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, configFileName);
            string valueName = string.Concat(shellId, ":", "ExecutionPolicy");
            WriteValueToFile<string>(fileName, valueName, executionPolicy);
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
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            return ReadValueFromFile<bool>(fileName, "ConsolePrompting");
        }

        internal void SetConsolePrompting(bool shouldPrompt)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            WriteValueToFile<bool>(fileName, "ConsolePrompting", shouldPrompt);
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
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            return ReadValueFromFile<bool>(fileName, "DisablePromptToUpdateHelp");
        }

        internal void SetDisablePromptToUpdateHelp(bool prompt)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            WriteValueToFile<bool>(fileName, "DisablePromptToUpdateHelp", prompt);
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\Software\Policies\Microsoft\Windows\PowerShell\UpdatableHelp
        /// Proposed value = blank.This should be supported though
        ///
        /// Schema:
        /// {
        ///     "DefaultSourcePath" : "path to local updatable help location"
        /// }
        /// </summary>
        /// <returns>The source path if found, null otherwise.</returns>
        internal string GetDefaultSourcePath()
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);

            string rawExecPolicy = ReadValueFromFile<string>(fileName, "DefaultSourcePath");

            if (!String.IsNullOrEmpty(rawExecPolicy))
            {
                return rawExecPolicy;
            }
            return null;
        }

        internal void SetDefaultSourcePath(string defaultPath)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);

            WriteValueToFile<string>(fileName, "DefaultSourcePath", defaultPath);
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
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            string identity = ReadValueFromFile<string>(fileName, "LogIdentity");

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
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            string levelName = ReadValueFromFile<string>(fileName, "LogLevel");
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

        const PSChannel DefaultChannels = PSChannel.Operational;

        /// <summary>
        /// Gets the bitmask of the PSChannel values to log.
        /// </summary>
        /// <returns>
        /// A bitmask of PSChannel.Operational and/or PSChannel.Analytic. The default value is PSChannel.Operational.
        /// </returns>
        internal PSChannel GetLogChannels()
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            string values = ReadValueFromFile<string>(fileName, "LogChannels");

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
                result = DefaultChannels;
            }

            return result;
        }

        // by default, do not include analytic events.
        const PSKeyword DefaultKeywords = (PSKeyword) (0xFFFFFFFFFFFFFFFF & ~(ulong)PSKeyword.UseAlwaysAnalytic);

        /// <summary>
        /// Gets the bitmask of keywords to log.
        /// </summary>
        /// <returns>
        /// A bitmask of PSKeyword values. The default value is all keywords other than UseAlwaysAnalytic.
        /// </returns>
        internal PSKeyword GetLogKeywords()
        {
            string fileName = Path.Combine(psHomeConfigDirectory, configFileName);
            string values = ReadValueFromFile<string>(fileName, "LogKeywords");

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
                result = DefaultKeywords;
            }

            return result;
        }
#endif // UNIX

        private T ReadValueFromFile<T>(string fileName, string key)
        {
            fileLock.EnterReadLock();
            try
            {
                // Open file for reading, but allow multiple readers
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader streamRdr = new StreamReader(fs))
                using (JsonTextReader jsonReader = new JsonTextReader(streamRdr))
                {
                    // Safely determines whether there is content to read from the file
                    bool isReadSuccess = jsonReader.Read();
                    if (isReadSuccess)
                    {
                        JObject jsonObject = (JObject) JToken.ReadFrom(jsonReader);
                        JToken value = jsonObject.GetValue(key);
                        if (null != value)
                        {
                            return value.ToObject<T>();
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // The file doesn't exist. Treat this the same way as if the
                // key was not present in the file.
            }
            catch (DirectoryNotFoundException)
            {
                // A directory in the path does not exist. Treat this as if the
                // key is not present in the file.
            }
            finally
            {
                fileLock.ExitReadLock();
            }

            return default(T);
        }

        /// <summary>
        /// TODO: Should this return success fail or throw?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="addValue">Whether the key-value pair should be added to or removed from the file</param>
        private void UpdateValueInFile<T>(string fileName, string key, T value, bool addValue)
        {
            fileLock.EnterWriteLock();
            try
            {
                // Since multiple properties can be in a single file, replacement
                // is required instead of overwrite if a file already exists.
                // Handling the read and write operations within a single FileStream
                // prevents other processes from reading or writing the file while
                // the update is in progress. It also locks out readers during write
                // operations.
                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
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

                            if (null == propertyToModify)
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
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void WriteValueToFile<T>(string fileName, string key, T value)
        {
            UpdateValueInFile<T>(fileName, key, value, true);
        }

        /// <summary>
        /// TODO: Should this return success, fail, or throw?
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        private void RemoveValueFromFile<T>(string fileName, string key)
        {
            // Optimization: If the file doesn't exist, there is nothing to remove
            if (File.Exists(fileName))
            {
                UpdateValueInFile<T>(fileName, key, default(T), false);
            }
        }
    }
} // Namespace System.Management.Automation

