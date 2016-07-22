using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Threading;

using System.Management.Automation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if CORECLR
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#else
//using Microsoft.Win32;
#endif

using Microsoft.Win32;

namespace System.Management.Automation
{

    /// <summary>
    /// Leverages the strategy pattern to abstract away the details of gathering properties from outside sources.
    /// Note: This is a class so that it can be internal.
    /// </summary>
    internal abstract class PropertyAccessor
    {
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

        /// <summary>
        /// Existing Key = HKLM:\System\CurrentControlSet\Control\Session Manager\Environment
        /// Proposed value = %ProgramFiles%\PowerShell\Modules by default
        /// 
        /// Note: There is no setter because this value is immutable.
        /// </summary>
        /// <returns>Module path values from the config file.</returns>
        internal abstract string GetModulePath();

        /// <summary>
        /// Existing Key = HKCU and HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
        /// Proposed value = Existing default execution policy if not already specified
        /// </summary>
        /// <param name="scope">Where it should check for the value.</param>
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell"</param>
        /// <returns></returns>
        internal abstract string GetMachineExecutionPolicy(PropertyScope scope, string shellId);
        internal abstract void RemoveMachineExecutionPolicy(PropertyScope scope, string shellId); // TODO: Necessary?
        internal abstract void SetMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = Existing value, otherwise 10.
        /// </summary>
        /// <returns>Max stack size in MB. If not set, defaults to 10 MB.</returns>
        internal abstract int GetPipeLineMaxStackSizeMb(int defaultValue);

        internal abstract void SetPipeLineMaxStackSizeMb(int maxStackSize);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = existing default. Probably "1"
        /// </summary>
        /// <returns>Whether console prompting should happen.</returns>
        internal abstract bool GetConsolePrompting(ref Exception exception);
        internal abstract void SetConsolePrompting(bool shouldPrompt);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell
        /// Proposed value = Existing default. Probably "0"
        /// </summary>
        /// <returns>Boolean indicating whether Update-Help should prompt</returns>
        internal abstract bool GetDisablePromptToUpdateHelp();
        internal abstract void SetDisablePromptToUpdateHelp(bool prompt);

        /// <summary>
        /// Existing Key = HKCU and HKLM\Software\Policies\Microsoft\Windows\PowerShell\UpdatableHelp
        /// Proposed value = blank.This should be supported though
        /// </summary>
        /// <returns></returns>
        internal abstract string GetDefaultSourcePath();
        internal abstract void SetDefaultSourcePath(string defaultPath);
    }

    internal class PropertyAccessorFactory
    {
        /// <summary>
        /// Template method to generate the appropriate accessor for a given environment.
        /// TODO: Is it really necessary to make this Singleton-ish? 
        /// </summary>
        /// <returns></returns>
        internal static PropertyAccessor GetPropertyAccessor()
        {
            if (null == _activePropertyAccessor)
            {
#if CORECLR
                // TODO: I must differentiate between inbox PS and side-by-side PS here!
                _activePropertyAccessor = new JsonConfigFileAccessor();
#else
                _activePropertyAccessor = new RegistryAccessor();
#endif
            }
            return _activePropertyAccessor;
        }

        private static PropertyAccessor _activePropertyAccessor;
    }

    /// <summary>
    /// JSON configuration file accessor
    ///
    /// Reads from and writes to configuration files. The values stored were 
    /// originally stored in the Windows registry.
    /// </summary>
    internal class JsonConfigFileAccessor : PropertyAccessor
    {
        private string psHomeConfigDirectory;
        private string appDataConfigDirectory;
        private const string configDirectoryName = "Configuration";
        private const string execPolicyFileName = "ExecutionPolicy.json";
        private const string maxStackSizeFileName = "PipeLineMaxStackSizeMB.json";
        private const string consolePromptingFileName = "ConsolePrompting.json";
        private const string updateHelpPromptFileName = "UpdateHelpPrompt.json";
        private const string updatableHelpSourcePathFileName = "UpdatableHelpDefaultSourcePath.json";

        internal JsonConfigFileAccessor()
        {
            //
            // Initialize (and create if necessary) the system-wide configuration directory
            //
            // TODO: Use Utils.GetApplicationBase("Microsoft.PowerShell") instead?
            Assembly assembly = typeof(PSObject).GetTypeInfo().Assembly;
            psHomeConfigDirectory = Path.Combine(Path.GetDirectoryName(assembly.Location), configDirectoryName);

            if (!Directory.Exists(psHomeConfigDirectory))
            {
                Directory.CreateDirectory(psHomeConfigDirectory);
            }

            //
            // Initialize (and create if necessary) the per-user configuration directory
            //
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string psConfigPath = Path.Combine("PowerShell", "1.0", configDirectoryName);
            appDataConfigDirectory = Path.Combine(appDataPath, psConfigPath);

            if (!Directory.Exists(appDataConfigDirectory))
            {
                Directory.CreateDirectory(appDataConfigDirectory);
            }
        }

        internal override string GetModulePath()
        {
            return string.Empty;
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
        /// Proposed value = Existing default execution policy if not already specified
        /// 
        /// Schema:
        /// {
        ///     "shell ID string" : "execution policy string"
        /// }
        /// 
        /// Note: If we switch to a single config file, then this will need to be nested
        /// </summary>
        /// <param name="scope">Whether this is a system-wide or per-user setting.</param>
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell"</param>
        /// <returns></returns>
        internal override string GetMachineExecutionPolicy(PropertyScope scope, string shellId)
        {
            string execPolicy = "Undefined";
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if(PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, execPolicyFileName);

            string rawExecPolicy = ReadValueFromFile<string>(fileName, shellId);

            if (!String.IsNullOrEmpty(rawExecPolicy))
            {
                execPolicy = rawExecPolicy;
            }
            // TODO: Throw if NullOrEmpty?
            return execPolicy;
        }

        internal override void RemoveMachineExecutionPolicy(PropertyScope scope, string shellId) // TODO: Necessary?
        {
            // TODO: Work to do here if I decide to support it
        }

        internal override void SetMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy)
        {
            string scopeDirectory = psHomeConfigDirectory;

            // Defaults to system wide.
            if (PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = appDataConfigDirectory;
            }

            string fileName = Path.Combine(scopeDirectory, execPolicyFileName);

            WriteValueToFile<string>(fileName, shellId, executionPolicy);
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = Existing value, otherwise 10. 
        /// 
        /// Schema:
        /// {
        ///     "PipeLineMaxStackSizeMB" : int
        /// }
        /// </summary>
        /// <returns>Max stack size in MB. If not set, defaults to 10 MB.</returns>
        internal override int GetPipeLineMaxStackSizeMb(int defaultValue)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, maxStackSizeFileName);

            int maxStackSize = defaultValue;
            int rawMaxStackSize = ReadValueFromFile<int>(fileName, "PipeLineMaxStackSizeMB");

            if (0 != rawMaxStackSize)
            {
                maxStackSize = rawMaxStackSize;
            }
            return maxStackSize;
        }

        internal override void SetPipeLineMaxStackSizeMb(int maxStackSize)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, maxStackSizeFileName);
            WriteValueToFile<int>(fileName, "PipeLineMaxStackSizeMB", maxStackSize);
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
        /// <returns>Whether console prompting should happen.</returns>
        internal override bool GetConsolePrompting(ref Exception exception)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, consolePromptingFileName);
            return ReadValueFromFile<bool>(fileName, "ConsolePrompting");
        }

        internal override void SetConsolePrompting(bool shouldPrompt)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, consolePromptingFileName);
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
        /// <returns>Boolean indicating whether Update-Help should prompt</returns>
        internal override bool GetDisablePromptToUpdateHelp()
        {
            string fileName = Path.Combine(psHomeConfigDirectory, updateHelpPromptFileName);
            return ReadValueFromFile<bool>(fileName, "DisablePromptToUpdateHelp");
        }

        internal override void SetDisablePromptToUpdateHelp(bool prompt)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, updateHelpPromptFileName);
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
        /// <returns></returns>
        internal override string GetDefaultSourcePath()
        {
            string fileName = Path.Combine(psHomeConfigDirectory, updatableHelpSourcePathFileName);

            string rawExecPolicy = ReadValueFromFile<string>(fileName, "DefaultSourcePath");

            if (!String.IsNullOrEmpty(rawExecPolicy))
            {
                return rawExecPolicy;
            }
            // TODO: Throw if NullOrEmpty?
            return String.Empty;
        }

        internal override void SetDefaultSourcePath(string defaultPath)
        {
            string fileName = Path.Combine(psHomeConfigDirectory, updatableHelpSourcePathFileName);

            WriteValueToFile<string>(fileName, "DefaultSourcePath", defaultPath);
        }

        private T ReadValueFromFile<T>(string fileName, string key)
        {
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                using (StreamReader streamRdr = new StreamReader(fs))
                using (JsonTextReader jsonReader = new JsonTextReader(streamRdr))
                {
                    JObject jsonObject = (JObject) JToken.ReadFrom(jsonReader);
                    return jsonObject.GetValue(key).ToObject<T>();
                }
            }
            catch (ArgumentException) { }
            //catch (ArgumentNullException) { }
            catch (NotSupportedException) { }
            catch (FileNotFoundException) { }
            catch (System.IO.IOException) { }
            //catch (DirectoryNotFoundException) { }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
            //catch (PathTooLongException) { }
            //catch (ArgumentOutOfRangeException) { }

            return default(T);
        }

        /// <summary>
        /// TODO: Should this return success fail or throw?
        /// 
        /// TODO: Catch exceptions
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void WriteValueToFile<T>(string fileName, string key, T value)
        {
            JObject objectToWrite = new JObject();

            if (File.Exists(fileName))
            {
                // Since multiple properties can be in a single file, replacement
                // is required instead of overwrite if a file already exists.
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                using (StreamReader streamRdr = new StreamReader(fs))
                using (JsonTextReader jsonReader = new JsonTextReader(streamRdr))
                {
                    JObject jsonObject = (JObject) JToken.ReadFrom(jsonReader);
                    IEnumerable<JProperty> properties = jsonObject.Properties();
                    foreach (JProperty property in properties)
                    {
                        if (String.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                        {
                            // Add the updated value to the output object instead
                            // of the preexisting one
                            objectToWrite.Add(new JProperty(key, value));
                        }
                        else
                        {
                            // This preserves existing properties that do not
                            // match the one to update
                            objectToWrite.Add(new JProperty(property));
                        }
                    }
                }
            }
            else
            {
                // The file doesn't exist, so create it with the new property
                objectToWrite.Add(new JProperty(key, value));
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fs))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                objectToWrite.WriteTo(jsonWriter);
            }
        }
    }
    /*
    /// <summary>
    /// TODO: Add caching so for faster access? Would prevent runtime changes from affecting runtime reads though...
    /// </summary>
    internal class XmlConfigFileAccessor : PropertyAccessor
    {
        private string fileName = string.Empty;

        internal XmlConfigFileAccessor()
        {
            Assembly assembly = typeof(PSObject).GetTypeInfo().Assembly;
            var result = Path.GetDirectoryName(ClrFacade.GetAssemblyLocation(assembly));
            fileName = result + "\\PowerShellConfig.xml";
        }

        internal override string GetApplicationBase(string version)
        {
            return ReadElementContentFromFileAsString("PSHome");
        }

        internal override string GetModulePath()
        {
            return ReadElementContentFromFileAsString("PSModulePath");
        }

        internal override string GetMachineShellPath(string shellId)
        {
            return ReadElementContentFromFileAsString("MachinePowerShellExePath");
        }

        internal override string GetMachineExecutionPolicy(string shellId)
        {

            return ReadElementContentFromFileAsString("MachineExecutionPolicy");
        }

        internal override void SetMachineExecutionPolicy(string shellId, string executionPolicy)
        {
            this.ReplaceElementValueInFile("MachineExecutionPolicy", executionPolicy);
        }

        internal override void RemoveMachineExecutionPolicy(string shellId)
        {
            this.RemoveElementFromFile("MachineExecutionPolicy");
        }

        private string ReadElementContentFromFileAsString(string elementName)
        {
            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader streamRdr = new StreamReader(fs))
            {
                XmlReaderSettings xmlReaderSettings =
                    InternalDeserializer.XmlReaderSettingsForUntrustedXmlDocument.Clone();

                using (XmlReader reader = XmlReader.Create(streamRdr, xmlReaderSettings))
                {
                    reader.MoveToContent();
                    if (reader.ReadToDescendant(elementName))
                    {
                        return reader.ReadElementContentAsString();
                    }
                }
            }
            return string.Empty;
        }

        private void AddElementToFile(string elementName, string elementValue)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader streamRdr = new StreamReader(fs))
            {
                doc.Load(streamRdr);

                XmlNodeList nodeList = doc.GetElementsByTagName(elementName);

                if (nodeList.Count > 0)
                {
                    // Element exists. Replace it.
                    this.ReplaceElementValueInFile(elementName, elementValue);
                }
                else
                {
                    XmlElement newElem = doc.CreateElement(elementName);
                    newElem.InnerText = elementValue;
                    doc.DocumentElement.AppendChild(newElem);
                }
            }
            WriteDocumentToFile(doc);
        }

        private void RemoveElementFromFile(string elementName)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader streamRdr = new StreamReader(fs))
            {
                doc.Load(streamRdr);

                XmlNodeList nodeList = doc.GetElementsByTagName(elementName);

                if (0 == nodeList.Count)
                {
                    // There is nothing to do because the specified node does not exist
                    return;
                }
                else if (nodeList.Count > 1)
                {
                    // throw docuemnt format exception? Nodes should be unique...
                    return;
                }
                else
                {
                    XmlNode first = nodeList.Item(0);
                    if (null != first)
                    {
                        doc.DocumentElement.RemoveChild(first);
                    }
                }
            }
            WriteDocumentToFile(doc);
        }

        private void ReplaceElementValueInFile(string elementName, string elementValue)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            using (StreamReader streamRdr = new StreamReader(fs))
            {
                doc.Load(streamRdr);

                XmlNodeList nodeList = doc.GetElementsByTagName(elementName);

                if (0 == nodeList.Count)
                {
                    // There is nothing to do because the specified node does not exist
                    return;
                }
                else if (nodeList.Count > 1)
                {
                    // throw docuemnt format exception? Nodes should be unique...
                    return;
                }
                else
                {
                    XmlNode first = nodeList.Item(0);
                    if (null != first)
                    {
                        first.InnerText = elementValue;
                    }
                }
                // Alternate technique if the first one doesnt work
                // Node is present, so replace it
                //XmlElement newElem = doc.CreateElement("title");
                //newElem.InnerText = elementValue;

                //Replace the title element.
                //doc.DocumentElement.ReplaceChild(elem, root.FirstChild);
                
            }
            WriteDocumentToFile(doc);
        }

        private void WriteDocumentToFile(XmlDocument doc)
        {
            using (FileStream fs = new FileStream(this.fileName, FileMode.Create, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fs))
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Encoding = Encoding.UTF8;
                xmlSettings.CloseOutput = true;
                xmlSettings.Indent = true;

                using (XmlWriter writer = XmlWriter.Create(streamWriter, xmlSettings))
                {
                    doc.WriteTo(writer);
                }
            }
        }
    }
    */

    internal class RegistryAccessor : PropertyAccessor
    {
        private const string DisablePromptToUpdateHelpRegPath = "Software\\Microsoft\\PowerShell";
        private const string DisablePromptToUpdateHelpRegPath32 = "Software\\Wow6432Node\\Microsoft\\PowerShell";
        private const string DisablePromptToUpdateHelpRegKey = "DisablePromptToUpdateHelp";
        private const string DefaultSourcePathRegPath = "Software\\Policies\\Microsoft\\Windows\\PowerShell\\UpdatableHelp";
        private const string DefaultSourcePathRegKey = "DefaultSourcePath";

        internal RegistryAccessor()
        {
        }
        /*
        internal override string GetApplicationBase(string version)
        {
            string engineKeyPath = RegistryStrings.MonadRootKeyPath + "\\" + version + "\\" + RegistryStrings.MonadEngineKey;

            return GetHklmString(engineKeyPath, RegistryStrings.MonadEngine_ApplicationBase);
        }
        */
        internal override string GetModulePath()
        {
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="shellId"></param>
        /// <returns>The execution policy string if found, otherwise null.</returns>
        internal override string GetMachineExecutionPolicy(PropertyScope scope, string shellId)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            RegistryKey scopedKey = Registry.LocalMachine;

            // Override if set to another value;
            if (PropertyScope.CurrentUser == scope)
            {
                scopedKey = Registry.CurrentUser;
            }

            return GetRegistryString(scopedKey, regKeyName, "ExecutionPolicy");
        }

        internal override void SetMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            RegistryKey scopedKey = Registry.LocalMachine;

            // Override if set to another value;
            if (PropertyScope.CurrentUser == scope)
            {
                scopedKey = Registry.CurrentUser;
            }

            using (RegistryKey key = scopedKey.CreateSubKey(regKeyName))
            {
                if (null != key)
                {
                    key.SetValue("ExecutionPolicy", executionPolicy, RegistryValueKind.String);
                }
            }
        }

        internal override void RemoveMachineExecutionPolicy(PropertyScope scope, string shellId)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            RegistryKey scopedKey = Registry.LocalMachine;

            // Override if set to another value;
            if (PropertyScope.CurrentUser == scope)
            {
                scopedKey = Registry.CurrentUser;
            }

            using (RegistryKey key = scopedKey.OpenSubKey(regKeyName, true))
            {
                if (key != null)
                {
                    if (key.GetValue("ExecutionPolicy") != null)
                        key.DeleteValue("ExecutionPolicy");
                }
            }
        }

        internal override int GetPipeLineMaxStackSizeMb(int defaultValue)
        {
            string regKeyName = Utils.GetRegistryConfigurationPrefix();

            int? tempInt = GetRegistryDword(Registry.LocalMachine, regKeyName, "PipelineMaxStackSizeMB");

            return (tempInt.HasValue ? tempInt.Value : defaultValue);
        }

        internal override void SetPipeLineMaxStackSizeMb(int maxStackSize)
        {
            string regKeyName = Utils.GetRegistryConfigurationPrefix();
            SetRegistryDword(Registry.LocalMachine, regKeyName, "PipelineMaxStackSizeMB", maxStackSize);
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = existing default. Probably "1"
        /// </summary>
        /// <returns>Whether console prompting should happen.</returns>
        internal override bool GetConsolePrompting(ref Exception exception)
        {
            string policyKeyName = Utils.GetRegistryConfigurationPrefix();
            string tempPrompt = GetRegistryString(Registry.LocalMachine, policyKeyName, "ConsolePrompting", ref exception);

            if (null != tempPrompt)
            {
                // TODO: It is difficult to tell from the original code how this value is actually stored in the registry.
                // I am inferring that it is a "true" or "false" string based on the original code.
                return Convert.ToBoolean(tempPrompt, CultureInfo.InvariantCulture); 
            }
            else
            {
                return false;
            }
        }

        internal override void SetConsolePrompting(bool shouldPrompt)
        {
            string policyKeyName = Utils.GetRegistryConfigurationPrefix();
            SetRegistryString(Registry.LocalMachine, policyKeyName, "ConsolePrompting", shouldPrompt.ToString());
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell
        /// Proposed value = Existing default. Probably "0"
        /// </summary>
        /// <returns>Boolean indicating whether Update-Help should prompt</returns>
        internal override bool GetDisablePromptToUpdateHelp()
        {
            using (RegistryKey hklm = Registry.LocalMachine.OpenSubKey(DisablePromptToUpdateHelpRegPath))
            {
                if (hklm != null)
                {
                    object disablePromptToUpdateHelp = hklm.GetValue(DisablePromptToUpdateHelpRegKey, null, RegistryValueOptions.None);

                    if (disablePromptToUpdateHelp == null)
                    {
                        return true;
                    }
                    else
                    {
                        int result;

                        if (LanguagePrimitives.TryConvertTo<int>(disablePromptToUpdateHelp, out result))
                        {
                            return (result != 1);
                        }

                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        internal override void SetDisablePromptToUpdateHelp(bool prompt)
        {
            int valueToSet = prompt ? 1 : 0;
            try
            {
                using (RegistryKey hklm = Registry.LocalMachine.OpenSubKey(DisablePromptToUpdateHelpRegPath, true))
                {
                    if (hklm != null)
                    {
                        hklm.SetValue(DisablePromptToUpdateHelpRegKey, valueToSet, RegistryValueKind.DWord);
                    }
                }

                using (RegistryKey hklm = Registry.LocalMachine.OpenSubKey(DisablePromptToUpdateHelpRegPath32, true))
                {
                    if (hklm != null)
                    {
                        hklm.SetValue(DisablePromptToUpdateHelpRegKey, valueToSet, RegistryValueKind.DWord);
                    }
                }
            }
            catch (UnauthorizedAccessException) {}
            catch (System.Security.SecurityException) {}
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\Software\Policies\Microsoft\Windows\PowerShell\UpdatableHelp
        /// Proposed value = blank.This should be supported though
        /// </summary>
        /// <returns></returns>
        internal override string GetDefaultSourcePath()
        {
            return GetRegistryString(Registry.LocalMachine, DefaultSourcePathRegPath, DefaultSourcePathRegKey);
        }

        internal override void SetDefaultSourcePath(string defaultPath)
        {
            SetRegistryString(Registry.LocalMachine, DefaultSourcePathRegPath, DefaultSourcePathRegKey, defaultPath);
        }

        private int? GetRegistryDword(RegistryKey rootKey, string pathToKey, string valueName)
        {
            try
            {
                using (RegistryKey regKey = rootKey.OpenSubKey(pathToKey))
                {
                    if (null == regKey)
                    {
                        // Key not found
                        return null;
                    }

                    // verify the value kind as a string
                    RegistryValueKind kind = regKey.GetValueKind(valueName);

                    if (kind == RegistryValueKind.DWord)
                    {
                        return regKey.GetValue(valueName) as int?;
                    }
                    else
                    {
                        // The function expected a DWORD, but got another type. This is a coding error or a registry key typing error.
                        return null;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }

            return null;
        }

        private void SetRegistryDword(RegistryKey rootKey, string pathToKey, string valueName, int value)
        {
            try
            {
                using (RegistryKey regKey = rootKey.OpenSubKey(pathToKey))
                {
                    if (null != regKey)
                    {
                        regKey.SetValue(valueName, value, RegistryValueKind.DWord);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }
        }

        private string GetRegistryString(RegistryKey rootKey, string pathToKey, string valueName)
        {
            Exception e = null;
            return GetRegistryString(rootKey, pathToKey, valueName, ref e);
        }

        private string GetRegistryString(RegistryKey rootKey, string pathToKey, string valueName, ref Exception exception)
        {
            try
            {
                using (RegistryKey regKey = rootKey.OpenSubKey(pathToKey))
                {
                    if (null == regKey)
                    {
                        // Key not found
                        return null;
                    }

                    // verify the value kind as a string
                    RegistryValueKind kind = regKey.GetValueKind(valueName);

                    if (kind == RegistryValueKind.ExpandString ||
                        kind == RegistryValueKind.String)
                    {
                        return regKey.GetValue(valueName) as string;
                    }
                    else
                    {
                        // The function expected a string, but got another type. This is a coding error or a registry key typing error.
                        return null;
                    }
                }
            }
            catch (ObjectDisposedException e) { exception = e; }
            catch (System.Security.SecurityException e) { exception = e; }
            catch (ArgumentException e) { exception = e; }
            catch (System.IO.IOException e) { exception = e; }
            catch (UnauthorizedAccessException e) { exception = e; }
            catch (FormatException e) { exception = e; }
            catch (OverflowException e) { exception = e; }
            catch (InvalidCastException e) { exception = e; }

            return null;
        }

        private string SetRegistryString(RegistryKey rootKey, string pathToKey, string valueName, string value)
        {
            try
            {
                using (RegistryKey key = rootKey.CreateSubKey(pathToKey))
                {
                    if (null != key)
                    {
                        key.SetValue(valueName, value, RegistryValueKind.String);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (System.Security.SecurityException) { }
            catch (ArgumentException) { }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            catch (InvalidCastException) { }

            return null;
        }
    }
} // Namespace System.Management.Automation

