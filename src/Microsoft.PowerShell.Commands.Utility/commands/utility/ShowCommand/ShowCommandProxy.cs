#if !CORECLR
//-----------------------------------------------------------------------
// <copyright file="ShowCommandProxy.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Internal;
    using System.Reflection;
    using System.Threading;
    using System.Collections.ObjectModel;
    using Microsoft.PowerShell.Commands.ShowCommandExtension;

    /// <summary>
    /// Help show-command create WPF object and invoke WPF windows with the 
    /// Microsoft.PowerShell.Commands.ShowCommandInternal.ShowCommandHelperhelp type defined in Microsoft.PowerShell.GraphicalHost.dll
    /// </summary>
    internal class ShowCommandProxy
    {
        private const string ShowCommandHelperName = "Microsoft.PowerShell.Commands.ShowCommandInternal.ShowCommandHelper";

        private ShowCommandCommand cmdlet;

        private GraphicalHostReflectionWrapper graphicalHostReflectionWrapper;

        internal ShowCommandProxy(ShowCommandCommand cmdlet)
        {
            this.cmdlet = cmdlet;
            this.graphicalHostReflectionWrapper = GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(cmdlet, ShowCommandProxy.ShowCommandHelperName);
        }

        internal void ShowAllModulesWindow(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameter, bool passThrough)
        {
            this.graphicalHostReflectionWrapper.CallMethod("ShowAllModulesWindow", this.cmdlet, importedModules, commands, noCommonParameter, this.cmdlet.Width, this.cmdlet.Height, passThrough);
        }

        internal void ShowCommandWindow(object commandViewModelObj, bool passThrough)
        {
            this.graphicalHostReflectionWrapper.CallMethod("ShowCommandWindow", this.cmdlet, commandViewModelObj, this.cmdlet.Width, this.cmdlet.Height, passThrough);
        }

        internal void CloseWindow()
        {
            this.graphicalHostReflectionWrapper.CallMethod("CloseWindow");
        }

        internal string GetScript()
        {
            return (string)this.graphicalHostReflectionWrapper.CallMethod("GetScript");
        }

        internal void ShowErrorString(string error)
        {
            this.graphicalHostReflectionWrapper.CallMethod("ShowErrorString", error);
        }

        internal bool SetPendingISECommand(string command)
        {
            return (bool)this.graphicalHostReflectionWrapper.CallMethod("SetPendingISECommand", command);
        }

        internal object GetCommandViewModel(ShowCommandCommandInfo command, bool noCommonParameter, Dictionary<string, ShowCommandModuleInfo> importedModules, bool moduleQualify)
        {
            return this.graphicalHostReflectionWrapper.CallStaticMethod("GetCommandViewModel", command, noCommonParameter, importedModules, moduleQualify);
        }

        internal string GetShowCommandCommand(string commandName, bool includeAliasAndModules)
        {
            return (string)this.graphicalHostReflectionWrapper.CallStaticMethod("GetShowCommandCommand", commandName, includeAliasAndModules);
        }

        internal string GetShowAllModulesCommand()
        {
            return (string)this.graphicalHostReflectionWrapper.CallStaticMethod("GetShowAllModulesCommand", false, true);
        }

        internal Dictionary<String, ShowCommandModuleInfo> GetImportedModulesDictionary(object[] moduleObjects)
        {
            return (Dictionary<String, ShowCommandModuleInfo>)this.graphicalHostReflectionWrapper.CallStaticMethod("GetImportedModulesDictionary", new object[] { moduleObjects });
        }

        internal List<ShowCommandCommandInfo> GetCommandList(object[] commandObjects)
        {
            return (List<ShowCommandCommandInfo>)this.graphicalHostReflectionWrapper.CallStaticMethod("GetCommandList", new object[] { commandObjects });
        }

        internal bool HasHostWindow
        {
            get
            {
                return (bool)this.graphicalHostReflectionWrapper.GetPropertyValue("HasHostWindow");
            }
        }

        internal AutoResetEvent WindowClosed
        {
            get
            {
                return (AutoResetEvent)this.graphicalHostReflectionWrapper.GetPropertyValue("WindowClosed");
            }
        }

        internal AutoResetEvent HelpNeeded
        {
            get
            {
                return (AutoResetEvent)this.graphicalHostReflectionWrapper.GetPropertyValue("HelpNeeded");
            }
        }

        internal AutoResetEvent ImportModuleNeeded
        {
            get
            {
                return (AutoResetEvent)this.graphicalHostReflectionWrapper.GetPropertyValue("ImportModuleNeeded");
            }
        }

        internal AutoResetEvent WindowLoaded
        {
            get
            {
                return (AutoResetEvent)this.graphicalHostReflectionWrapper.GetPropertyValue("WindowLoaded");
            }
        }


        internal string CommandNeedingHelp
        {
            get
            {
                return (string)this.graphicalHostReflectionWrapper.GetPropertyValue("CommandNeedingHelp");
            }
        }

        internal string ParentModuleNeedingImportModule
        {
            get
            {
                return (string)this.graphicalHostReflectionWrapper.GetPropertyValue("ParentModuleNeedingImportModule");
            }
        }

        internal void DisplayHelp(Collection<PSObject> helpResults)
        {
            this.graphicalHostReflectionWrapper.CallMethod("DisplayHelp", helpResults);
        }


        internal string GetImportModuleCommand(string module)
        {
            return (string)this.graphicalHostReflectionWrapper.CallStaticMethod("GetImportModuleCommand", module, false, true);
        }

        internal string GetHelpCommand(string command)
        {
            return (string)this.graphicalHostReflectionWrapper.CallStaticMethod("GetHelpCommand", command);
        }

        internal void ImportModuleDone(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands)
        {
            this.graphicalHostReflectionWrapper.CallMethod("ImportModuleDone", importedModules, commands);
        }

        internal void ImportModuleFailed(Exception reason)
        {
            this.graphicalHostReflectionWrapper.CallMethod("ImportModuleFailed", reason);
        }

        internal void ActivateWindow()
        {
            this.graphicalHostReflectionWrapper.CallMethod("ActivateWindow");
        }

        internal double ScreenWidth
        {
            get
            {
                return (double)this.graphicalHostReflectionWrapper.GetStaticPropertyValue("ScreenWidth");
            }
        }

        internal double ScreenHeight
        {
            get
            {
                return (double)this.graphicalHostReflectionWrapper.GetStaticPropertyValue("ScreenHeight");
            }
        }
    }
}

#endif
