// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;

using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Help show-command create WPF object and invoke WPF windows with the
    /// Microsoft.PowerShell.Commands.ShowCommandInternal.ShowCommandHelperhelp type defined in Microsoft.PowerShell.GraphicalHost.dll.
    /// </summary>
    internal class ShowCommandProxy
    {
        private const string ShowCommandHelperName = "Microsoft.PowerShell.Commands.ShowCommandInternal.ShowCommandHelper";

        private readonly ShowCommandCommand _cmdlet;

        private readonly GraphicalHostReflectionWrapper _graphicalHostReflectionWrapper;

        internal ShowCommandProxy(ShowCommandCommand cmdlet)
        {
            _cmdlet = cmdlet;
            _graphicalHostReflectionWrapper = GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(cmdlet, ShowCommandProxy.ShowCommandHelperName);
        }

        internal void ShowAllModulesWindow(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameter, bool passThrough)
        {
            _graphicalHostReflectionWrapper.CallMethod("ShowAllModulesWindow", _cmdlet, importedModules, commands, noCommonParameter, _cmdlet.Width, _cmdlet.Height, passThrough);
        }

        internal void ShowCommandWindow(object commandViewModelObj, bool passThrough)
        {
            _graphicalHostReflectionWrapper.CallMethod("ShowCommandWindow", _cmdlet, commandViewModelObj, _cmdlet.Width, _cmdlet.Height, passThrough);
        }

        internal void CloseWindow()
        {
            _graphicalHostReflectionWrapper.CallMethod("CloseWindow");
        }

        internal string GetScript()
        {
            return (string)_graphicalHostReflectionWrapper.CallMethod("GetScript");
        }

        internal void ShowErrorString(string error)
        {
            _graphicalHostReflectionWrapper.CallMethod("ShowErrorString", error);
        }

        internal bool SetPendingISECommand(string command)
        {
            return (bool)_graphicalHostReflectionWrapper.CallMethod("SetPendingISECommand", command);
        }

        internal object GetCommandViewModel(ShowCommandCommandInfo command, bool noCommonParameter, Dictionary<string, ShowCommandModuleInfo> importedModules, bool moduleQualify)
        {
            return _graphicalHostReflectionWrapper.CallStaticMethod("GetCommandViewModel", command, noCommonParameter, importedModules, moduleQualify);
        }

        internal string GetShowCommandCommand(string commandName, bool includeAliasAndModules)
        {
            return (string)_graphicalHostReflectionWrapper.CallStaticMethod("GetShowCommandCommand", commandName, includeAliasAndModules);
        }

        internal string GetShowAllModulesCommand()
        {
            return (string)_graphicalHostReflectionWrapper.CallStaticMethod("GetShowAllModulesCommand", false, true);
        }

        internal Dictionary<string, ShowCommandModuleInfo> GetImportedModulesDictionary(object[] moduleObjects)
        {
            return (Dictionary<string, ShowCommandModuleInfo>)_graphicalHostReflectionWrapper.CallStaticMethod("GetImportedModulesDictionary", new object[] { moduleObjects });
        }

        internal List<ShowCommandCommandInfo> GetCommandList(object[] commandObjects)
        {
            return (List<ShowCommandCommandInfo>)_graphicalHostReflectionWrapper.CallStaticMethod("GetCommandList", new object[] { commandObjects });
        }

        internal bool HasHostWindow
        {
            get
            {
                return (bool)_graphicalHostReflectionWrapper.GetPropertyValue("HasHostWindow");
            }
        }

        internal AutoResetEvent WindowClosed
        {
            get
            {
                return (AutoResetEvent)_graphicalHostReflectionWrapper.GetPropertyValue("WindowClosed");
            }
        }

        internal AutoResetEvent HelpNeeded
        {
            get
            {
                return (AutoResetEvent)_graphicalHostReflectionWrapper.GetPropertyValue("HelpNeeded");
            }
        }

        internal AutoResetEvent ImportModuleNeeded
        {
            get
            {
                return (AutoResetEvent)_graphicalHostReflectionWrapper.GetPropertyValue("ImportModuleNeeded");
            }
        }

        internal AutoResetEvent WindowLoaded
        {
            get
            {
                return (AutoResetEvent)_graphicalHostReflectionWrapper.GetPropertyValue("WindowLoaded");
            }
        }

        internal string CommandNeedingHelp
        {
            get
            {
                return (string)_graphicalHostReflectionWrapper.GetPropertyValue("CommandNeedingHelp");
            }
        }

        internal string ParentModuleNeedingImportModule
        {
            get
            {
                return (string)_graphicalHostReflectionWrapper.GetPropertyValue("ParentModuleNeedingImportModule");
            }
        }

        internal void DisplayHelp(Collection<PSObject> helpResults)
        {
            _graphicalHostReflectionWrapper.CallMethod("DisplayHelp", helpResults);
        }

        internal string GetImportModuleCommand(string module)
        {
            return (string)_graphicalHostReflectionWrapper.CallStaticMethod("GetImportModuleCommand", module, false, true);
        }

        internal string GetHelpCommand(string command)
        {
            return (string)_graphicalHostReflectionWrapper.CallStaticMethod("GetHelpCommand", command);
        }

        internal void ImportModuleDone(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands)
        {
            _graphicalHostReflectionWrapper.CallMethod("ImportModuleDone", importedModules, commands);
        }

        internal void ImportModuleFailed(Exception reason)
        {
            _graphicalHostReflectionWrapper.CallMethod("ImportModuleFailed", reason);
        }

        internal void ActivateWindow()
        {
            _graphicalHostReflectionWrapper.CallMethod("ActivateWindow");
        }

        internal double ScreenWidth
        {
            get
            {
                return (double)_graphicalHostReflectionWrapper.GetStaticPropertyValue("ScreenWidth");
            }
        }

        internal double ScreenHeight
        {
            get
            {
                return (double)_graphicalHostReflectionWrapper.GetStaticPropertyValue("ScreenHeight");
            }
        }
    }
}
