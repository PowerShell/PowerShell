// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    internal static class ModuleUtils
    {
        // Default option for local file system enumeration:
        //  - Ignore files/directories when access is denied;
        //  - Search top directory only.
        private readonly static System.IO.EnumerationOptions s_defaultEnumerationOptions =
                                        new System.IO.EnumerationOptions() { AttributesToSkip = 0 };

        // Default option for UNC path enumeration. Same as above plus a large buffer size.
        // For network shares, a large buffer may result in better performance as more results can be batched over the wire.
        // The buffer size 16K is recommended in the comment of the 'BufferSize' property:
        //    "A "large" buffer, for example, would be 16K. Typical is 4K."
        private readonly static System.IO.EnumerationOptions s_uncPathEnumerationOptions =
                                        new System.IO.EnumerationOptions() { AttributesToSkip = 0, BufferSize = 16384 };

        /// <summary>
        /// Check if a directory could be a module folder.
        /// </summary>
        internal static bool IsPossibleModuleDirectory(string dir)
        {
            // We shouldn't be searching in hidden directories.
            FileAttributes attributes = File.GetAttributes(dir);
            if (0 != (attributes & FileAttributes.Hidden))
            {
                return false;
            }

            // Assume locale directories do not contain modules.
            if (dir.EndsWith(@"\en", StringComparison.OrdinalIgnoreCase) ||
                dir.EndsWith(@"\en-us", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            dir = Path.GetFileName(dir);
            // Use some simple pattern matching to avoid the call into GetCultureInfo when we know it will fail (and throw).
            if ((dir.Length == 2 && char.IsLetter(dir[0]) && char.IsLetter(dir[1]))
                ||
                (dir.Length == 5 && char.IsLetter(dir[0]) && char.IsLetter(dir[1]) && (dir[2] == '-') && char.IsLetter(dir[3]) && char.IsLetter(dir[4])))
            {
                try
                {
                    // This might not throw on invalid culture still
                    // 4096 is considered the unknown locale - so assume that could be a module
                    var cultureInfo = new CultureInfo(dir);
                    return cultureInfo.LCID == 4096;
                }
                catch { }
            }

            return true;
        }

        /// <summary>
        /// Get all module files by searching the given directory recursively.
        /// All sub-directories that could be a module folder will be searched.
        /// </summary>
        internal static IEnumerable<string> GetAllAvailableModuleFiles(string topDirectoryToCheck)
        {
            if (!Directory.Exists(topDirectoryToCheck)) { yield break; }
            
            var options = Utils.PathIsUnc(topDirectoryToCheck) ? s_uncPathEnumerationOptions : s_defaultEnumerationOptions;
            Queue<string> directoriesToCheck = new Queue<string>();
            directoriesToCheck.Enqueue(topDirectoryToCheck);

            while (directoriesToCheck.Count > 0)
            {
                string directoryToCheck = directoriesToCheck.Dequeue();
                try
                {
                    string[] subDirectories = Directory.GetDirectories(directoryToCheck, "*", options);
                    foreach (string toAdd in subDirectories)
                    {
                        if (IsPossibleModuleDirectory(toAdd))
                        {
                            directoriesToCheck.Enqueue(toAdd);
                        }
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                string[] files = Directory.GetFiles(directoryToCheck, "*", options);
                foreach (string moduleFile in files)
                {
                    foreach (string ext in ModuleIntrinsics.PSModuleExtensions)
                    {
                        if (moduleFile.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return moduleFile;
                            break; // one file can have only one extension
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the CompatiblePSEditions field of a given module
        /// declares compatibility with the running PowerShell edition.
        /// </summary>
        /// <param name="moduleManifestPath">The path to the module manifest being checked.</param>
        /// <param name="compatiblePSEditions">The value of the CompatiblePSEditions field of the module manifest.</param>
        /// <returns>True if the module is compatible with the running PowerShell edition, false otherwise.</returns>
        internal static bool IsPSEditionCompatible(
            string moduleManifestPath,
            IEnumerable<string> compatiblePSEditions)
        {
#if UNIX
            return true;
#else
            if (!ModuleUtils.IsOnSystem32ModulePath(moduleManifestPath))
            {
                return true;
            }

            return Utils.IsPSEditionSupported(compatiblePSEditions);
#endif
        }

        internal static IEnumerable<string> GetDefaultAvailableModuleFiles(bool isForAutoDiscovery, ExecutionContext context)
        {
            HashSet<string> uniqueModuleFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string directory in ModuleIntrinsics.GetModulePath(isForAutoDiscovery, context))
            {
                var needWriteProgressCompleted = false;
                ProgressRecord analysisProgress = null;

                // Write a progress message for UNC paths, so that users know what is happening
                try
                {
                    if ((context.CurrentCommandProcessor != null) && Utils.PathIsUnc(directory))
                    {
                        analysisProgress = new ProgressRecord(0,
                            Modules.DeterminingAvailableModules,
                            String.Format(CultureInfo.InvariantCulture, Modules.SearchingUncShare, directory))
                        {
                            RecordType = ProgressRecordType.Processing
                        };

                        context.CurrentCommandProcessor.CommandRuntime.WriteProgress(analysisProgress);
                        needWriteProgressCompleted = true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // This may be called when we are not allowed to write progress,
                    // So eat the invalid operation
                }

                try
                {
                    foreach (string moduleFile in ModuleUtils.GetDefaultAvailableModuleFiles(directory))
                    {
                        if (uniqueModuleFiles.Add(moduleFile))
                        {
                            yield return moduleFile;
                        }
                    }
                }
                finally
                {
                    if (needWriteProgressCompleted)
                    {
                        analysisProgress.RecordType = ProgressRecordType.Completed;
                        context.CurrentCommandProcessor.CommandRuntime.WriteProgress(analysisProgress);
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of module files from the given directory without recursively searching all sub-directories.
        /// This method assumes the given directory is a module folder or a version sub-directory of a module folder.
        /// </summary>
        internal static List<string> GetModuleFilesFromAbsolutePath(string directory)
        {
            List<string> result = new List<string>();
            string fileName = Path.GetFileName(directory);

            // If the given directory doesn't exist or it's the root folder, then return an empty list.
            if (!Directory.Exists(directory) || string.IsNullOrEmpty(fileName)) { return result; }

            // If the user give the module path including version, the module name could be the parent folder name.
            if (Version.TryParse(fileName, out Version ver))
            {
                string parentDirPath = Path.GetDirectoryName(directory);
                string parentDirName = Path.GetFileName(parentDirPath);

                // If the parent directory is NOT a root folder, then it could be the module folder.
                if (!string.IsNullOrEmpty(parentDirName))
                {
                    string manifestPath = Path.Combine(directory, parentDirName);
                    manifestPath += StringLiterals.PowerShellDataFileExtension;
                    if (File.Exists(manifestPath) && ver.Equals(ModuleIntrinsics.GetManifestModuleVersion(manifestPath)))
                    {
                        result.Add(manifestPath);
                        return result;
                    }
                }
            }

            // If we reach here, then use the given directory as the module folder.
            foreach (Version version in GetModuleVersionSubfolders(directory))
            {
                string manifestPath = Path.Combine(directory, version.ToString(), fileName);
                manifestPath += StringLiterals.PowerShellDataFileExtension;
                if (File.Exists(manifestPath) && version.Equals(ModuleIntrinsics.GetManifestModuleVersion(manifestPath)))
                {
                    result.Add(manifestPath);
                }
            }

            foreach (string ext in ModuleIntrinsics.PSModuleExtensions)
            {
                string moduleFile = Path.Combine(directory, fileName) + ext;
                if (File.Exists(moduleFile))
                {
                    result.Add(moduleFile);

                    // when finding the default modules we stop when the first
                    // match is hit - searching in order .psd1, .psm1, .dll,
                    // if a file is found but is not readable then it is an error.
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Get a list of the available module files from the given directory.
        /// Search all module folders under the specified directory, but do not search sub-directories under a module folder.
        /// </summary>
        internal static IEnumerable<string> GetDefaultAvailableModuleFiles(string topDirectoryToCheck)
        {
            if (!Directory.Exists(topDirectoryToCheck)) { yield break; }

            var options = Utils.PathIsUnc(topDirectoryToCheck) ? s_uncPathEnumerationOptions : s_defaultEnumerationOptions;
            List<Version> versionDirectories = new List<Version>();
            LinkedList<string> directoriesToCheck = new LinkedList<string>();
            directoriesToCheck.AddLast(topDirectoryToCheck);

            while (directoriesToCheck.Count > 0)
            {
                versionDirectories.Clear();
                string[] subdirectories;
                string directoryToCheck = directoriesToCheck.First.Value;
                directoriesToCheck.RemoveFirst();
                try
                {
                    subdirectories = Directory.GetDirectories(directoryToCheck, "*", options);
                    ProcessPossibleVersionSubdirectories(subdirectories, versionDirectories);
                }
                catch (IOException) { subdirectories = Utils.EmptyArray<string>(); }
                catch (UnauthorizedAccessException) { subdirectories = Utils.EmptyArray<string>(); }

                bool isModuleDirectory = false;
                string proposedModuleName = Path.GetFileName(directoryToCheck);
                foreach (Version version in versionDirectories)
                {
                    string manifestPath = Path.Combine(directoryToCheck, version.ToString(), proposedModuleName);
                    manifestPath += StringLiterals.PowerShellDataFileExtension;
                    if (File.Exists(manifestPath))
                    {
                        isModuleDirectory = true;
                        yield return manifestPath;
                    }
                }

                if (!isModuleDirectory)
                {
                    foreach (string ext in ModuleIntrinsics.PSModuleExtensions)
                    {
                        string moduleFile = Path.Combine(directoryToCheck, proposedModuleName) + ext;
                        if (File.Exists(moduleFile))
                        {
                            isModuleDirectory = true;
                            yield return moduleFile;

                            // when finding the default modules we stop when the first
                            // match is hit - searching in order .psd1, .psm1, .dll
                            // if a file is found but is not readable then it is an
                            // error
                            break;
                        }
                    }
                }

                if (!isModuleDirectory)
                {
                    foreach (var subdirectory in subdirectories)
                    {
                        if (IsPossibleModuleDirectory(subdirectory))
                        {
                            if (subdirectory.EndsWith("Microsoft.PowerShell.Management", StringComparison.OrdinalIgnoreCase) ||
                                subdirectory.EndsWith("Microsoft.PowerShell.Utility", StringComparison.OrdinalIgnoreCase))
                            {
                                directoriesToCheck.AddFirst(subdirectory);
                            }
                            else
                            {
                                directoriesToCheck.AddLast(subdirectory);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the list of versions under the specified module base path in descending sorted order
        /// </summary>
        /// <param name="moduleBase">module base path</param>
        /// <returns>sorted list of versions</returns>
        internal static List<Version> GetModuleVersionSubfolders(string moduleBase)
        {
            var versionFolders = new List<Version>();

            if (!string.IsNullOrWhiteSpace(moduleBase) && Directory.Exists(moduleBase))
            {
                var options = Utils.PathIsUnc(moduleBase) ? s_uncPathEnumerationOptions : s_defaultEnumerationOptions;
                string[] subdirectories = Directory.GetDirectories(moduleBase, "*", options);
                ProcessPossibleVersionSubdirectories(subdirectories, versionFolders);
            }

            return versionFolders;
        }

        private static void ProcessPossibleVersionSubdirectories(string[] subdirectories, List<Version> versionFolders)
        {
            foreach (string subdir in subdirectories)
            {
                string subdirName = Path.GetFileName(subdir);
                if (Version.TryParse(subdirName, out Version version))
                {
                    versionFolders.Add(version);
                }
            }
            if (versionFolders.Count > 1)
            {
                versionFolders.Sort((x, y) => y.CompareTo(x));
            }
        }

        internal static bool IsModuleInVersionSubdirectory(string modulePath, out Version version)
        {
            version = null;
            string folderName = Path.GetDirectoryName(modulePath);
            if (folderName != null)
            {
                folderName = Path.GetFileName(folderName);
                return Version.TryParse(folderName, out version);
            }
            return false;
        }

        internal static bool IsOnSystem32ModulePath(string path)
        {
#if UNIX
            return false;
#else
            Dbg.Assert(!String.IsNullOrEmpty(path), $"Caller to verify that {nameof(path)} is not null or empty");

            string windowsPowerShellPSHomePath = ModuleIntrinsics.GetWindowsPowerShellPSHomeModulePath();
            return path.StartsWith(windowsPowerShellPSHomePath, StringComparison.OrdinalIgnoreCase);
#endif
        }

        /// <summary>
        /// Gets a list of matching commands
        /// </summary>
        /// <param name="pattern">command pattern</param>
        /// <param name="commandOrigin"></param>
        /// <param name="context"></param>
        /// <param name="rediscoverImportedModules"></param>
        /// <param name="moduleVersionRequired"></param>
        /// <returns></returns>
        internal static IEnumerable<CommandInfo> GetMatchingCommands(string pattern, ExecutionContext context, CommandOrigin commandOrigin, bool rediscoverImportedModules = false, bool moduleVersionRequired = false)
        {
            // Otherwise, if it had wildcards, just return the "AvailableCommand"
            // type of command info.
            WildcardPattern commandPattern = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);

            CmdletInfo cmdletInfo = context.SessionState.InvokeCommand.GetCmdlet("Microsoft.PowerShell.Core\\Get-Module");
            PSModuleAutoLoadingPreference moduleAutoLoadingPreference = CommandDiscovery.GetCommandDiscoveryPreference(context, SpecialVariables.PSModuleAutoLoadingPreferenceVarPath, "PSModuleAutoLoadingPreference");

            if ((moduleAutoLoadingPreference != PSModuleAutoLoadingPreference.None) &&
                    ((commandOrigin == CommandOrigin.Internal) || ((cmdletInfo != null) && (cmdletInfo.Visibility == SessionStateEntryVisibility.Public))
                    )
                )
            {
                foreach (string modulePath in GetDefaultAvailableModuleFiles(isForAutoDiscovery: false, context))
                {
                    // Skip modules that have already been loaded so that we don't expose private commands.
                    string moduleName = Path.GetFileNameWithoutExtension(modulePath);
                    List<PSModuleInfo> modules = context.Modules.GetExactMatchModules(moduleName, all: false, exactMatch: true);
                    PSModuleInfo tempModuleInfo = null;

                    if (modules.Count != 0)
                    {
                        // 1. We continue to the next module path if we don't want to re-discover those imported modules
                        // 2. If we want to re-discover the imported modules, but one or more commands from the module were made private,
                        //    then we don't do re-discovery
                        if (!rediscoverImportedModules || modules.Exists(module => module.ModuleHasPrivateMembers))
                        {
                            continue;
                        }

                        if (modules.Count == 1)
                        {
                            PSModuleInfo psModule = modules[0];
                            tempModuleInfo = new PSModuleInfo(psModule.Name, psModule.Path, context: null, sessionState: null);
                            tempModuleInfo.SetModuleBase(psModule.ModuleBase);

                            foreach (KeyValuePair<string, CommandInfo> entry in psModule.ExportedCommands)
                            {
                                if (commandPattern.IsMatch(entry.Value.Name))
                                {
                                    CommandInfo current = null;
                                    switch (entry.Value.CommandType)
                                    {
                                        case CommandTypes.Alias:
                                            current = new AliasInfo(entry.Value.Name, definition: null, context);
                                            break;
                                        case CommandTypes.Function:
                                            current = new FunctionInfo(entry.Value.Name, ScriptBlock.EmptyScriptBlock, context);
                                            break;
                                        case CommandTypes.Filter:
                                            current = new FilterInfo(entry.Value.Name, ScriptBlock.EmptyScriptBlock, context);
                                            break;
                                        case CommandTypes.Configuration:
                                            current = new ConfigurationInfo(entry.Value.Name, ScriptBlock.EmptyScriptBlock, context);
                                            break;
                                        case CommandTypes.Cmdlet:
                                            current = new CmdletInfo(entry.Value.Name, implementingType: null, helpFile: null, PSSnapin: null, context);
                                            break;
                                        default:
                                            Dbg.Assert(false, "cannot be hit");
                                            break;
                                    }

                                    current.Module = tempModuleInfo;
                                    yield return current;
                                }
                            }

                            continue;
                        }
                    }

                    string moduleShortName = System.IO.Path.GetFileNameWithoutExtension(modulePath);

                    IDictionary<string, CommandTypes> exportedCommands = AnalysisCache.GetExportedCommands(modulePath, testOnly: false, context);

                    if (exportedCommands == null) { continue; }

                    tempModuleInfo = new PSModuleInfo(moduleShortName, modulePath, sessionState: null, context: null);
                    if (InitialSessionState.IsEngineModule(moduleShortName))
                    {
                        tempModuleInfo.SetModuleBase(Utils.DefaultPowerShellAppBase);
                    }

                    //moduleVersionRequired is bypassed by FullyQualifiedModule from calling method. This is the only place where guid will be involved.
                    if (moduleVersionRequired && modulePath.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        tempModuleInfo.SetVersion(ModuleIntrinsics.GetManifestModuleVersion(modulePath));
                        tempModuleInfo.SetGuid(ModuleIntrinsics.GetManifestGuid(modulePath));
                    }

                    foreach (KeyValuePair<string, CommandTypes> pair in exportedCommands)
                    {
                        string commandName = pair.Key;
                        CommandTypes commandTypes = pair.Value;

                        if (commandPattern.IsMatch(commandName))
                        {
                            bool shouldExportCommand = true;

                            // Verify that we don't already have it represented in the initial session state.
                            if ((context.InitialSessionState != null) && (commandOrigin == CommandOrigin.Runspace))
                            {
                                foreach (SessionStateCommandEntry commandEntry in context.InitialSessionState.Commands[commandName])
                                {
                                    string moduleCompareName = null;

                                    if (commandEntry.Module != null)
                                    {
                                        moduleCompareName = commandEntry.Module.Name;
                                    }
                                    else if (commandEntry.PSSnapIn != null)
                                    {
                                        moduleCompareName = commandEntry.PSSnapIn.Name;
                                    }

                                    if (String.Equals(moduleShortName, moduleCompareName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (commandEntry.Visibility == SessionStateEntryVisibility.Private)
                                        {
                                            shouldExportCommand = false;
                                        }
                                    }
                                }
                            }

                            if (shouldExportCommand)
                            {
                                if ((commandTypes & CommandTypes.Alias) == CommandTypes.Alias)
                                {
                                    yield return new AliasInfo(commandName, null, context)
                                    {
                                        Module = tempModuleInfo
                                    };
                                }
                                if ((commandTypes & CommandTypes.Cmdlet) == CommandTypes.Cmdlet)
                                {
                                    yield return new CmdletInfo(commandName, implementingType: null, helpFile: null, PSSnapin: null, context: context)
                                    {
                                        Module = tempModuleInfo
                                    };
                                }
                                if ((commandTypes & CommandTypes.Function) == CommandTypes.Function)
                                {
                                    yield return new FunctionInfo(commandName, ScriptBlock.EmptyScriptBlock, context)
                                    {
                                        Module = tempModuleInfo
                                    };
                                }
                                if ((commandTypes & CommandTypes.Configuration) == CommandTypes.Configuration)
                                {
                                    yield return new ConfigurationInfo(commandName, ScriptBlock.EmptyScriptBlock, context)
                                    {
                                        Module = tempModuleInfo
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

