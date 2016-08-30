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
    using Messages = Microsoft.PackageManagement.MetaProvider.PowerShell.Resources.Messages;
    using System.Collections.Concurrent;

    public class PowerShellProviderBase : IDisposable {
        private object _lock = new Object();
        protected PSModuleInfo _module;
        private PowerShell _powershell;
        private ManualResetEvent _reentrancyLock = new ManualResetEvent(true);
        private readonly Dictionary<string, CommandInfo> _allCommands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CommandInfo> _methods = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        public PowerShellProviderBase(PowerShell ps, PSModuleInfo module) {
            if (module == null) {
                throw new ArgumentNullException("module");
            }

            _powershell = ps;
            _module = module;

            // combine all the cmdinfos we care about
            // but normalize the keys as we go (remove any '-' '_' chars)
            foreach (var k in _module.ExportedAliases.Keys) {
                _allCommands.AddOrSet(k.Replace("-", "").Replace("_", ""), _module.ExportedAliases[k]);
            }
            foreach (var k in _module.ExportedCmdlets.Keys) {
                _allCommands.AddOrSet(k.Replace("-", "").Replace("_", ""), _module.ExportedCmdlets[k]);
            }
            foreach (var k in _module.ExportedFunctions.Keys) {
                _allCommands.AddOrSet(k.Replace("-", "").Replace("_", ""), _module.ExportedFunctions[k]);
            }
        }

        public string ModulePath {
            get {
                return _module.Path;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_powershell != null) {
                    _powershell.Dispose();
                    _powershell = null;
                }

                if (_reentrancyLock != null) {
                    _reentrancyLock.Dispose();
                    _reentrancyLock = null;
                }

                _module = null;
            }
        }

        internal CommandInfo GetMethod(string methodName) {
            return _methods.GetOrAdd(methodName, () => {
                if (_allCommands.ContainsKey(methodName)) {
                    return _allCommands[methodName];
                }

                // try simple plurals to single
                if (methodName.EndsWith("s", StringComparison.OrdinalIgnoreCase)) {
                    var meth = methodName.Substring(0, methodName.Length - 1);
                    if (_allCommands.ContainsKey(meth)) {
                        return _allCommands[meth];
                    }
                }

                // try words like Dependencies to Dependency
                if (methodName.EndsWith("cies", StringComparison.OrdinalIgnoreCase)) {
                    var meth = methodName.Substring(0, methodName.Length - 4) + "cy";
                    if (_allCommands.ContainsKey(meth)) {
                        return _allCommands[meth];
                    }
                }

                // try IsFoo to Test-IsFoo
                if (methodName.IndexOf("Is", StringComparison.OrdinalIgnoreCase) == 0) {
                    var meth = "test" + methodName;
                    if (_allCommands.ContainsKey(meth)) {
                        return _allCommands[meth];
                    }
                }

                if (methodName.IndexOf("add", StringComparison.OrdinalIgnoreCase) == 0) {
                    // try it with 'register' instead
                    var result = GetMethod("register" + methodName.Substring(3));
                    if (result != null) {
                        return result;
                    }
                }

                if (methodName.IndexOf("remove", StringComparison.OrdinalIgnoreCase) == 0) {
                    // try it with 'register' instead
                    var result = GetMethod("unregister" + methodName.Substring(6));
                    if (result != null) {
                        return result;
                    }
                }

                // can't find one, return null.
                return null;
            });

            // hmm, it is possible to get the parameter types to match better when binding.
            // module.ExportedFunctions.FirstOrDefault().Value.Parameters.Values.First().ParameterType
        }

        internal object CallPowerShellWithoutRequest(string method, params object[] args) {
            var cmdInfo = GetMethod(method);
            if (cmdInfo == null) {
                return null;
            }

            var result = _powershell.InvokeFunction<object>(cmdInfo.Name, null, null, args);
            if (result == null) {
                // failure!
                throw new Exception(Messages.PowershellScriptFunctionReturnsNull.format(_module.Name, method));
            }

            return result;
        }

        // lock is on this instance only

        internal void ReportErrors(PsRequest request, IEnumerable<ErrorRecord> errors) {
            foreach (var error in errors) {
                request.Error(error.FullyQualifiedErrorId, error.CategoryInfo.Category.ToString(), error.TargetObject == null ? null : error.TargetObject.ToString(), error.ErrorDetails == null ? error.Exception.Message : error.ErrorDetails.Message);
                if (!string.IsNullOrWhiteSpace(error.Exception.StackTrace)) {
                    // give a debug hint if we have a script stack trace. How nice of us.
                    // the exception stack trace gives better stack than the script stack trace
                    request.Debug(Constants.ScriptStackTrace, error.Exception.StackTrace);
                }
            }
        }

        private IAsyncResult _stopResult;
        private object _stopLock = new object();

        internal void CancelRequest() {
            if (!_reentrancyLock.WaitOne(0)) {
                // it's running right now.
#if DEBUG
                    NativeMethods.OutputDebugString("[Cmdlet:debugging] -- Stopping powershell script.");
#endif
                lock (_stopLock) {
                    if (_stopResult == null) {
                        _stopResult = _powershell.BeginStop(ar => { }, null);
                    }
                }
            }
        }

        internal object CallPowerShell(PsRequest request, params object[] args) {
            // the lock ensures that we're not re-entrant into the same powershell runspace
            lock (_lock) {
                if (!_reentrancyLock.WaitOne(0)) {
                    // this lock is set to false, meaning we're still in a call **ON THIS THREAD**
                    // this is bad karma -- powershell won't let us call into the runspace again
                    // we're going to throw an error here because this indicates that the currently
                    // running powershell call is calling back into PM, and it has called back
                    // into this provider. That's just bad bad bad.
                    throw new Exception("Reentrancy Violation in powershell module");
                }
                
                try {
                    // otherwise, this is the first time we've been here during this call.
                    _reentrancyLock.Reset();

                    _powershell.SetVariable("request", request);
                    _powershell.Streams.ClearStreams();

                    object finalValue = null;
                    ConcurrentBag<ErrorRecord> errors = new ConcurrentBag<ErrorRecord>();

                    request.Debug("INVOKING PowerShell Fn {0} with args {1} that has length {2}", request.CommandInfo.Name, String.Join(", ", args), args.Length);

                    var result = _powershell.InvokeFunction<object>(request.CommandInfo.Name,
                        (sender, e) => output_DataAdded(sender, e, request, ref finalValue),
                        (sender, e) => error_DataAdded(sender, e, request, errors),
                        args);

                    if (result == null)
                    {
                        // result is null but it does not mean that the call fails because the command may return nothing
                        request.Debug(Messages.PowershellScriptFunctionReturnsNull.format(_module.Name, request.CommandInfo.Name));
                    }

                    if (errors.Count > 0)
                    {
                        // report the error if there are any
                        ReportErrors(request, errors);
                        _powershell.Streams.Error.Clear();
                    }

                    return finalValue;
                } catch (CmdletInvocationException cie) {
                    var error = cie.ErrorRecord;
                    request.Error(error.FullyQualifiedErrorId, error.CategoryInfo.Category.ToString(), error.TargetObject == null ? null : error.TargetObject.ToString(), error.ErrorDetails == null ? error.Exception.Message : error.ErrorDetails.Message);
                } finally {
                    lock (_stopLock) {
                        if (_stopResult != null){
                            _powershell.EndStop(_stopResult);
                            _stopResult = null;
                        }
                    }
                    _powershell.Clear();
                    _powershell.SetVariable("request", null);
                    // it's ok if someone else calls into this module now.
                    request.Debug("Done calling powershell", request.CommandInfo.Name, _module.Name);
                    _reentrancyLock.Set();
                }

                return null;
            }
        }

        private void error_DataAdded(object sender, DataAddedEventArgs e, PsRequest request, ConcurrentBag<ErrorRecord> errors)
        {
            PSDataCollection<ErrorRecord> errorStream = sender as PSDataCollection<ErrorRecord>;

            if (errorStream == null)
            {
                return;
            }

            var error = errorStream[e.Index];

            if (error != null)
            {
                // add the error so we can report them later
                errors.Add(error);
            }
        
        }

        private void output_DataAdded(object sender, DataAddedEventArgs e, PsRequest request, ref object finalValue)
        {
            PSDataCollection<PSObject> outputstream = sender as PSDataCollection<PSObject>;

            if (outputstream == null)
            {
                return;
            }

            PSObject psObject = outputstream[e.Index];
            if (psObject != null)
            {
                var value = psObject.ImmediateBaseObject;
                var y = value as Yieldable;
                if (y != null)
                {
                    // yield it to stream the result gradually
                    y.YieldResult(request);
                }
                else
                {
                    finalValue = value;
                    return;
                }
            }
        }
    }
}
