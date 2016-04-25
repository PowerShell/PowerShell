// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

using Microsoft.PackageManagement.Internal.Utility.Platform;

namespace Microsoft.PackageManagement.MetaProvider.PowerShell.Internal {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Platform;

    internal static class PowerShellExtensions {
        internal static PowerShell Clear(this PowerShell powershell) {
            if (powershell != null) {
                powershell.WaitForReady();
                powershell.Commands = new PSCommand();
            }
            return powershell;
        }

        internal static PSModuleInfo ImportModule(this PowerShell powershell, string name, bool force = false) {
            if (powershell != null) {
                powershell.Clear().AddCommand("Import-Module");
                powershell.AddParameter("Name", name);
                powershell.AddParameter("PassThru");

                if (force) {
                    powershell.AddParameter("Force");
                }
                return powershell.Invoke<PSModuleInfo>().ToArray().FirstOrDefault();
            }
            return null;
        }

        internal static IEnumerable<PSModuleInfo> TestModuleManifest(this PowerShell powershell, string path) {
            if (powershell != null) {
                return powershell
                    .Clear()
                    .AddCommand("Test-ModuleManifest")
                    .AddParameter("Path", path)
                    .Invoke<PSModuleInfo>().ToArray();
            }
            return Enumerable.Empty<PSModuleInfo>();
        }

        internal static IEnumerable<PSModuleInfo> GetModule(this PowerShell powershell, string moduleName)
        {
            if ((powershell != null) && (!string.IsNullOrWhiteSpace(moduleName)))
            {
                return powershell
                    .Clear()
                    .AddCommand("Get-Module")
                    .AddParameter("Name", moduleName)
                    .AddParameter("ListAvailable")
                    .Invoke<PSModuleInfo>();
            }
            return Enumerable.Empty<PSModuleInfo>();
        }

        internal static PowerShell SetVariable(this PowerShell powershell, string variable, object value) {
            if (powershell != null) {
                powershell
                    .Clear()
                    .AddCommand("Set-Variable")
                    .AddParameter("Name", variable)
                    .AddParameter("Value", value)
                    .Invoke();
            }
            return powershell;
        }

        /// <summary>
        /// Return the last output to the user. This will be null if the operation did not succeed.
        /// outputAction is the action that will be performed whenever an object is outputted to the powershell pipeline
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="powershell"></param>
        /// <param name="command"></param>
        /// <param name="outputAction"></param>
        /// <param name="errorAction"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static T InvokeFunction<T>(this PowerShell powershell, string command,
            EventHandler<DataAddedEventArgs> outputAction, EventHandler<DataAddedEventArgs> errorAction, params object[] args) {

            if (powershell != null) {
                powershell.Clear().AddCommand(command);
                foreach (var arg in args) {
                    powershell.AddArgument(arg);
                }
#if DEBUG
                NativeMethods.OutputDebugString("[Cmdlet:debugging] -- InvokeFunction ({0}, {1})".format(command, args.Select( each => (each ?? "<NULL>").ToString()).JoinWithComma(), powershell.InvocationStateInfo.Reason));
#endif
            
                var input = new PSDataCollection<PSObject>();
                input.Complete();

                var output = new PSDataCollection<PSObject>();

                if (outputAction != null)
                {
                    output.DataAdded += outputAction;
                }

                if (errorAction != null)
                {
                    powershell.Streams.Error.DataAdded += errorAction;
                }

                powershell.Invoke(null, output, new PSInvocationSettings());

                if (output.Count == 0)
                {
                    return default(T);
                }

                // return the last output to the user
                PSObject last = output.Last();

                if (last != null)
                {
                    // convert last to T
                    return (T)last.ImmediateBaseObject;
                }
            }

            return default(T);
        }

        internal static PowerShell WaitForReady(this PowerShell powershell) {
            try {
                if (powershell != null) {
                    switch (powershell.InvocationStateInfo.State) {
                        case PSInvocationState.Stopping:
                            while (powershell.InvocationStateInfo.State == PSInvocationState.Stopping) {
                                Thread.Sleep(10);
                            }
                            break;

                        case PSInvocationState.Running:
                            powershell.Stop();
                            while (powershell.InvocationStateInfo.State == PSInvocationState.Stopping) {
                                Thread.Sleep(10);
                            }
                            break;

                        case PSInvocationState.Failed:
                            break;
                        case PSInvocationState.Completed:
                            break;

                        case PSInvocationState.Stopped:
                            break;

                        case PSInvocationState.NotStarted:
                            break;
                        case PSInvocationState.Disconnected:
                            break;
                    }
                }
            }
            catch (Exception e) {
                e.Dump();
            }
            return powershell;
        }
    }
}