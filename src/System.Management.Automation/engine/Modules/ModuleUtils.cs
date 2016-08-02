/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation.Runspaces;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    internal static class ModuleUtils
    {
        internal static bool IsPossibleModuleDirectory(string dir)
        {
            // We shouldn't be searching in hidden directories.
            var attributes = File.GetAttributes(dir);
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

#if !CORECLR
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
#endif

            return true;
        }

        /// <summary>
        /// Get a list of all module files
        /// which can be imported just by specifying a non rooted file name of the module
        /// (Import-Module foo\bar.psm1;  but not Import-Module .\foo\bar.psm1)
        /// </summary>
        /// <remarks>When obtaining all module files we return all possible
        /// combinations for a given file. For example, for foo we return both
        /// foo.psd1 and foo.psm1 if found. Get-Module will create the module
        /// info only for the first one</remarks>
        internal static IEnumerable<string> GetAllAvailableModuleFiles(string topDirectoryToCheck)
        {
            Queue<string> directoriesToCheck = new Queue<string>();
            directoriesToCheck.Enqueue(topDirectoryToCheck);

            while (directoriesToCheck.Count > 0)
            {
                var directoryToCheck = directoriesToCheck.Dequeue();
                try
                {
                    var subDirectories = Directory.GetDirectories(directoryToCheck, "*", SearchOption.TopDirectoryOnly);
                    foreach (var toAdd in subDirectories)
                    {
                        if (IsPossibleModuleDirectory(toAdd))
                        {
                            directoriesToCheck.Enqueue(toAdd);
                        }
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                var files = Directory.GetFiles(directoryToCheck, "*", SearchOption.TopDirectoryOnly);
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

        internal static IEnumerable<string> GetDefaultAvailableModuleFiles(bool force, bool isForAutoDiscovery, ExecutionContext context)
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

        internal static List<string> GetModuleVersionsFromAbsolutePath(string directory)
        {
            List<string> result = new List<string>();
            string fileName = Path.GetFileName(directory);
            Version moduleVersion;
            // if the user give the module path including version, we should be able to find the module as well
            if (Version.TryParse(fileName, out moduleVersion) && Directory.Exists(Directory.GetParent(directory).ToString()))
            {
                fileName = Directory.GetParent(directory).Name;
            }
            foreach (var version in GetModuleVersionSubfolders(directory))
            {
                var qualifiedPathWithVersion = Path.Combine(directory, Path.Combine(version.ToString(), fileName));
                string manifestPath = qualifiedPathWithVersion + StringLiterals.PowerShellDataFileExtension;
                if (File.Exists(manifestPath))
                {
                    bool isValidModuleVersion = version.Equals(ModuleIntrinsics.GetManifestModuleVersion(manifestPath));

                    if (isValidModuleVersion)
                    {
                        result.Add(manifestPath);
                    }
                }
            }

            foreach (string ext in ModuleIntrinsics.PSModuleExtensions)
            {
                string moduleFile = Path.Combine(directory, fileName) + ext;

                if (!Utils.NativeFileExists(moduleFile))
                {
                    continue;
                }

                result.Add(moduleFile);

                // when finding the default modules we stop when the first
                // match is hit - searching in order .psd1, .psm1, .dll
                // if a file is found but is not readable then it is an 
                // error
                break;
            }

            return result;
        }

        /// <summary>
        /// Get a list of the available module files
        /// which can be imported just by specifying a non rooted directory name of the module
        /// (Import-Module foo\bar;  but not Import-Module .\foo\bar or Import-Module .\foo\bar.psm1)
        /// </summary>       
        internal static IEnumerable<string> GetDefaultAvailableModuleFiles(string topDirectoryToCheck)
        {
            List<Version> versionDirectories = new List<Version>();
            LinkedList<string> directoriesToCheck = new LinkedList<string>();
            directoriesToCheck.AddLast(topDirectoryToCheck);

            while (directoriesToCheck.Count > 0)
            {
                versionDirectories.Clear();
                string[] subdirectories;
                var directoryToCheck = directoriesToCheck.First.Value;
                directoriesToCheck.RemoveFirst();
                try
                {
                    subdirectories = Directory.GetDirectories(directoryToCheck, "*", SearchOption.TopDirectoryOnly);
                    ProcessPossibleVersionSubdirectories(subdirectories, versionDirectories);
                }
                catch (IOException) { subdirectories = Utils.EmptyArray<string>(); }
                catch (UnauthorizedAccessException) { subdirectories = Utils.EmptyArray<string>(); }

                bool isModuleDirectory = false;
                string proposedModuleName = Path.GetFileName(directoryToCheck);
                foreach (var version in versionDirectories)
                {
                    var qualifiedPathWithVersion = Path.Combine(directoryToCheck, Path.Combine(version.ToString(), proposedModuleName));
                    string manifestPath = qualifiedPathWithVersion + StringLiterals.PowerShellDataFileExtension;
                    if (File.Exists(manifestPath))
                    {
                        isModuleDirectory = true;
                        yield return manifestPath;
                    }
                }

                foreach (string ext in ModuleIntrinsics.PSModuleExtensions)
                {
                    string moduleFile = Path.Combine(directoryToCheck, proposedModuleName) + ext;
                    if (!Utils.NativeFileExists(moduleFile))
                    {
                        continue;
                    }

                    isModuleDirectory = true;
                    yield return moduleFile;

                    // when finding the default modules we stop when the first
                    // match is hit - searching in order .psd1, .psm1, .dll
                    // if a file is found but is not readable then it is an 
                    // error
                    break;
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
                var subdirectories = Directory.GetDirectories(moduleBase);
                ProcessPossibleVersionSubdirectories(subdirectories, versionFolders);
            }

            return versionFolders;
        }

        private static void ProcessPossibleVersionSubdirectories(string[] subdirectories, List<Version> versionFolders)
        {
            foreach (var subdir in subdirectories)
            {
                var subdirName = Path.GetFileName(subdir);
                Version version;
                if (Version.TryParse(subdirName, out version))
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
            var folderName = Path.GetDirectoryName(modulePath);
            if (folderName != null)
            {
                folderName = Path.GetFileName(folderName);
                return Version.TryParse(folderName, out version);
            }
            return false;
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
                foreach (string modulePath in GetDefaultAvailableModuleFiles(true, false, context))
                {
                    // Skip modules that have already been loaded so that we don't expose private commands.
                    string moduleName = Path.GetFileNameWithoutExtension(modulePath);
                    var modules = context.Modules.GetExactMatchModules(moduleName, all: false, exactMatch: true);
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
                            tempModuleInfo = new PSModuleInfo(psModule.Name, psModule.Path, null, null);
                            tempModuleInfo.SetModuleBase(psModule.ModuleBase);

                            foreach (var entry in psModule.ExportedCommands)
                            {
                                if (commandPattern.IsMatch(entry.Value.Name))
                                {
                                    CommandInfo current = null;
                                    switch (entry.Value.CommandType)
                                    {
                                        case CommandTypes.Alias:
                                            current = new AliasInfo(entry.Value.Name, null, context);
                                            break;
                                        case CommandTypes.Workflow:
                                            current = new WorkflowInfo(entry.Value.Name, ScriptBlock.EmptyScriptBlock, context);
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
                                            current = new CmdletInfo(entry.Value.Name, null, null, null, context);
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
                    var exportedCommands = AnalysisCache.GetExportedCommands(modulePath, false, context);

                    if (exportedCommands == null) { continue; }

                    tempModuleInfo = new PSModuleInfo(moduleShortName, modulePath, null, null);
                    if (InitialSessionState.IsEngineModule(moduleShortName))
                    {
                        tempModuleInfo.SetModuleBase(Utils.GetApplicationBase(Utils.DefaultPowerShellShellID));
                    }

                    //moduleVersionRequired is bypassed by FullyQualifiedModule from calling method. This is the only place where guid will be involved.
                    if (moduleVersionRequired && modulePath.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        tempModuleInfo.SetVersion(ModuleIntrinsics.GetManifestModuleVersion(modulePath));
                        tempModuleInfo.SetGuid(ModuleIntrinsics.GetManifestGuid(modulePath));
                    }

                    foreach (var pair in exportedCommands)
                    {
                        var commandName = pair.Key;
                        var commandTypes = pair.Value;

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
                                if ((commandTypes & CommandTypes.Workflow) == CommandTypes.Workflow)
                                {
                                    yield return new WorkflowInfo(commandName, ScriptBlock.EmptyScriptBlock, context)
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

