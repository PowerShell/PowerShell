using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Reflection;

using System.Management.Automation;

#if CORECLR
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#endif

// TODO: Add non-windows guard here or move to a different file
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
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell"</param>
        /// <returns></returns>
        internal abstract string GetMachineExecutionPolicy(PropertyScope scope, string shellId);
        internal abstract void RemoveMachineExecutionPolicy(PropertyScope scope, string shellId); // TODO: Necessary?
        internal abstract void AddMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy); // TODO: Necessary?
        internal abstract void SetMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = Existing value, otherwise 10.
        /// </summary>
        /// <returns>Max stack size in MB. If not set, defaults to 10 MB.</returns>
        internal abstract int GetPipeLineMaxStackSizeMb();

        internal abstract void SetPipeLineMaxStackSizeMb(int maxStackSize);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = existing default. Probably "1"
        /// </summary>
        /// <returns>Whether console prompting should happen.</returns>
        internal abstract Boolean GetConsolePrompting();
        internal abstract void SetConsolePrompting(Boolean shouldPrompt);

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell
        /// Proposed value = Existing default. Probably "0"
        /// </summary>
        /// <returns>Boolean indicating whether Update-Help should prompt</returns>
        internal abstract Boolean GetDisablePromptToUpdateHelp();
        internal abstract void SetDisablePromptToUpdateHelp(Boolean prompt);

        /// <summary>
        /// Existing Key = HKCU and HKLM\Software\Policies\Microsoft\Windows\PowerShell\UpdatableHelp
        /// Proposed value = blank.This should be supported though
        /// </summary>
        /// <returns></returns>
        internal abstract string GetDefaultSourcePath(PropertyScope scope);
        internal abstract void SetDefaultSourcePath(PropertyScope scope, string defaultPath);
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
                _activePropertyAccessor = new XmlConfigFileAccessor();
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
        private static string configDirectoryName = "Configuration";
        private static string execPolicyFileName = "ExecutionPolicy.json";

        internal JsonConfigFileAccessor()
        {
            // TODO: Use Utils.GetApplicationBase("Microsoft.PowerShell") instead?
            Assembly assembly = typeof(PSObject).GetTypeInfo().Assembly;
            psHomeConfigDirectory = Path.Combine(Path.GetDirectoryName(ClrFacade.GetAssemblyLocation(assembly)), configDirectoryName);


        }

        internal override string GetModulePath()
        {
            return string.Empty;
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell
        /// Proposed value = Existing default execution policy if not already specified
        /// </summary>
        /// <param name="scope">Whether this is a system-wide or per-user setting.</param>
        /// <param name="shellId">The shell associated with this policy. Typically, it is "Microsoft.PowerShell"</param>
        /// <returns></returns>
        internal override string GetMachineExecutionPolicy(PropertyScope scope, string shellId)
        {
            string execPolicy = "Restricted";
            string scopeDirectory = psHomeConfigDirectory;
            string fileToUse;

            if(PropertyScope.CurrentUser == scope)
            {
                scopeDirectory = getUserConfigLocation();
            }

            fileToUse = Path.Combine(scopeDirectory, execPolicyFileName);
            return execPolicy;
        }

        internal override void RemoveMachineExecutionPolicy(PropertyScope scope, string shellId) // TODO: Necessary?
        {
        }

        internal override void AddMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy)
            // TODO: Necessary?
        {
        }

        internal override void SetMachineExecutionPolicy(PropertyScope scope, string shellId, string executionPolicy)
        {
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = Existing value, otherwise 10.
        /// </summary>
        /// <returns>Max stack size in MB. If not set, defaults to 10 MB.</returns>
        internal override int GetPipeLineMaxStackSizeMb()
        {
            return 0;
        }

        internal override void SetPipeLineMaxStackSizeMb(int maxStackSize)
        {
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell\1\ShellIds
        /// Proposed value = existing default. Probably "1"
        /// </summary>
        /// <returns>Whether console prompting should happen.</returns>
        internal override Boolean GetConsolePrompting()
        {
            return true;
        }

        internal override void SetConsolePrompting(Boolean shouldPrompt)
        {
            
        }

        /// <summary>
        /// Existing Key = HKLM\SOFTWARE\Microsoft\PowerShell
        /// Proposed value = Existing default. Probably "0"
        /// </summary>
        /// <returns>Boolean indicating whether Update-Help should prompt</returns>
        internal override Boolean GetDisablePromptToUpdateHelp()
        {
            return true;
        }

        internal override void SetDisablePromptToUpdateHelp(Boolean prompt)
        {
        }

        /// <summary>
        /// Existing Key = HKCU and HKLM\Software\Policies\Microsoft\Windows\PowerShell\UpdatableHelp
        /// Proposed value = blank.This should be supported though
        /// </summary>
        /// <returns></returns>
        internal override string GetDefaultSourcePath(PropertyScope scope)
        {
            return string.Empty;
        }

        internal override void SetDefaultSourcePath(PropertyScope scope, string defaultPath)
        {
        }

        private string getUserConfigLocation()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string psConfigPath = Path.Combine("PowerShell", "1.0", configDirectoryName);
            return Path.Combine(appDataPath, psConfigPath);
        }
    }

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

        internal override void AddMachineExecutionPolicy(string shellId, string executionPolicy)
        {
            this.AddElementToFile("MachineExecutionPolicy", executionPolicy);
        }

        internal override void RemoveMachineExecutionPolicy(string shellId)
        {
            this.RemoveElementFromFile("MachineExecutionPolicy");
        }

        private string ReadElementContentFromFileAsString(string elementName)
        {
            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            {
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
            }
            return string.Empty;
        }

        private void AddElementToFile(string elementName, string elementValue)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            {
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
            }
            WriteDocumentToFile(doc);
        }

        private void RemoveElementFromFile(string elementName)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            {
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
            }
            WriteDocumentToFile(doc);
        }

        private void ReplaceElementValueInFile(string elementName, string elementValue)
        {
            XmlDocument doc = new XmlDocument();

            using (FileStream fs = new FileStream(this.fileName, FileMode.Open, FileAccess.Read))
            {
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
                    /*  // Alternate technique if the first one doesnt work
                    // Node is present, so replace it
                    XmlElement newElem = doc.CreateElement("title");
                    newElem.InnerText = elementValue;

                    //Replace the title element.
                    doc.DocumentElement.ReplaceChild(elem, root.FirstChild);
                    */
                }
            }
            WriteDocumentToFile(doc);
        }

        private void WriteDocumentToFile(XmlDocument doc)
        {
            using (FileStream fs = new FileStream(this.fileName, FileMode.Create, FileAccess.Write))
            {
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
    }

    internal class RegistryAccessor : PropertyAccessor
    {
        internal RegistryAccessor()
        {
        }

        internal override string GetApplicationBase(string version)
        {
            string engineKeyPath = RegistryStrings.MonadRootKeyPath + "\\" + version + "\\" + RegistryStrings.MonadEngineKey;

            return GetHklmString(engineKeyPath, RegistryStrings.MonadEngine_ApplicationBase);
        }

        internal override string GetModulePath()
        {
            return string.Empty;
        }

        internal override string GetMachineShellPath(string shellId)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            return GetHklmString(regKeyName, "Path");
        }

        internal override string GetMachineExecutionPolicy(string shellId)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            return GetHklmString(regKeyName, "ExecutionPolicy");
        }

        internal override void SetMachineExecutionPolicy(string shellId, string executionPolicy)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regKeyName))
            {
                key.SetValue("ExecutionPolicy", executionPolicy, RegistryValueKind.String);
            }
        }

        internal override void AddMachineExecutionPolicy(string shellId, string executionPolicy)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(regKeyName))
            {
                key.SetValue("ExecutionPolicy", executionPolicy, RegistryValueKind.String);
            }
        }

        internal override void RemoveMachineExecutionPolicy(string shellId)
        {
            string regKeyName = Utils.GetRegistryConfigurationPath(shellId);
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regKeyName, true))
            {
                if (key != null)
                {
                    if (key.GetValue("ExecutionPolicy") != null)
                        key.DeleteValue("ExecutionPolicy");
                }
            }
        }

        private string GetHklmString(string pathToKey, string valueName)
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(pathToKey))
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

        private string GetHkcuString(string pathToKey, string valueName)
        {
            try
            {
                using (RegistryKey regKey = Registry.CurrentUser.OpenSubKey(pathToKey))
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

