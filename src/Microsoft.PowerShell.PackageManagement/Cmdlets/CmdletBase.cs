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

namespace Microsoft.PowerShell.PackageManagement.Cmdlets {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Security;
    using System.Threading;
    using Microsoft.PackageManagement;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Resources;
    using Utility;
    using Constants = PackageManagement.Constants;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Microsoft.PackageManagement.Packaging;
    using Telemetry.Internal;
    using System.Management.Automation.Runspaces;
    using System.Net;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;


    public delegate string GetMessageString(string messageId, string defaultText);

    public abstract class CmdletBase : AsyncCmdlet, IHostApi {
        private static int _globalCallCount = 1;
        private static readonly object _lockObject = new object();
        private bool _messageResolverNotResponding = false;
        private readonly int _callCount;
        private readonly Hashtable _dynamicOptions = new Hashtable();
        private string _bootstrapNuGet = "false";

        [Parameter]
        public SwitchParameter Force;

        [Parameter]
        public SwitchParameter ForceBootstrap;

        private GetMessageString _messageResolver;

        protected CmdletBase() {
            _callCount = _globalCallCount++;
            ShouldLogError = true;
        }

        protected bool IsRooted(string filePath)
        {
            return (Path.IsPathRooted(filePath) ||
                    filePath.StartsWith(@".\", StringComparison.Ordinal) ||
                    filePath.StartsWith(@"./", StringComparison.Ordinal) ||
                    filePath.StartsWith(@"..\", StringComparison.Ordinal) ||
                    filePath.StartsWith(@"../", StringComparison.Ordinal) ||
                    filePath.StartsWith(@"~/", StringComparison.Ordinal) ||
                    filePath.StartsWith(@"~\", StringComparison.Ordinal));
        }

        protected void ValidateVersion(string version) {
            if (string.IsNullOrWhiteSpace(version)) {
                return;
            }
            try {
                Version.Parse(version);
            } catch (Exception ex) {
                Error(Constants.Errors.InvalidVersion, version, ex.Message);
            }
        }

        protected bool ShouldLogError {get; set;}

        protected bool ShouldSelectAllProviders { get; set; }

        protected bool WaitForActivity<T>(IEnumerable<IAsyncEnumerable<T>> enumerables) {
            var handles = enumerables.Select(each => each.Ready).ConcatSingleItem(CancellationEvent.Token.WaitHandle).ToArray();
            if (handles.Length > 1) {
                if (handles.Length <= 64)
                {
                    // less than or equal to 64 handles so we can use waithandle
                    WaitHandle.WaitAny(handles);
                }
                else
                {
                    // throw error saying we don't support
                    Error(Constants.Errors.TooManyPackages, handles.Length);
                    
                    //// divide handles in chunks of 64 each
                    //int numberOfTasks = (int)Math.Ceiling(handles.Length / 64.0);
                    //var cts = new CancellationTokenSource();
                    //Task[] tasks = new Task[numberOfTasks];
                    //for (int i = 0; i < numberOfTasks; i += 1)
                    //{
                    //    // now each task will use the waithandle.waitany since only 64 handles will be assigned to each task
                    //    // so we won't get exception
                    //    tasks[i] = Task.Factory.StartNew(() =>
                    //    {
                    //        if ((handles.Length - i * 64) > 0)
                    //        {
                    //            // either 64 items or whatever left
                    //            int waitHandleArrayLength = Math.Min(64, handles.Length - i * 64);
                    //            WaitHandle[] waithandles = new WaitHandle[waitHandleArrayLength];
                    //            Array.Copy(handles, i * 64, waithandles, 0, waitHandleArrayLength);
                    //            WaitHandle.WaitAny(waithandles);
                    //        }
                    //    }, cts.Token);
                    //}

                    //// just need a task to finish
                    //Task.WaitAny(tasks);
                    //// cancel the rest
                    //cts.Cancel();
                }
                return !IsCanceled;
            }
            // we are finished when there is only handle to wait on (the cancellation token)
            return false;
        }

        protected abstract IEnumerable<string> ParameterSets {get;}

        protected bool IsPackageBySearch {
            get {
                return ParameterSetName == Constants.ParameterSets.PackageBySearchSet;
            }
        }

        protected bool IsPackageByObject {
            get {
                return ParameterSetName == Constants.ParameterSets.PackageByInputObjectSet;
            }
        }

        protected bool IsSourceByObject {
            get {
                return ParameterSetName == Constants.ParameterSets.SourceByInputObjectSet;
            }
        }

        protected internal IPackageManagementService PackageManagementService {
            get {
                lock (_lockObject) {
                    if (!IsCanceled && !IsInitialized) {
                        try {
                            IsInitialized = PackageManager.Instance.Initialize(this);
                        } catch (Exception e) {
                            e.Dump();
                        }
                    }
                }
                return PackageManager.Instance;
            }
        }

        protected int TimeOut {
            get {
                if (IsInvocation) {
                    var t = GetDynamicParameterValue<int>("Timeout");
                    if (t > 0) {
                        return t;
                    }
                }

                return Constants.DefaultTimeout; // in a non-invocation, always default to one hour
            }
        }

        protected int Responsiveness {
            get {
                if (IsInvocation) {
                    var t = GetDynamicParameterValue<int>("Responsiveness");
                    if (t > 0) {
                        return t;
                    }
                }
                return Constants.DefaultResponsiveness; // in a non-invocation, always default to one hour
            }
        }

        public GetMessageString MessageResolver {
            get {
                return _messageResolver ?? (_messageResolver = GetDynamicParameterValue<GetMessageString>("MessageResolver"));
            }
        }

        #region Implementing IHostApi

        public Hashtable DynamicOptions {
            get {
                return _dynamicOptions;
            }
        }

        public virtual IEnumerable<string> Sources {
            get {
                return null;
            }
            set {
            }
        }

        public virtual IEnumerable<string> OptionKeys {
            get {
                return DynamicParameterDictionary.Values.OfType<CustomRuntimeDefinedParameter>().Where(each => each.IsSet).Select(each => each.Name).Concat(MyInvocation.BoundParameters.Keys);
            }
        }

        protected bool GenerateCommonDynamicParameters() {
            if (IsInvocation) {
                // only generate these parameters if there is an actual call happening.
                // this prevents get-help from showing them.
                DynamicParameterDictionary.Add("Timeout", new RuntimeDefinedParameter("Timeout", typeof (int), new Collection<Attribute> {
                    new ParameterAttribute()
                }));

                DynamicParameterDictionary.Add("Responsiveness", new RuntimeDefinedParameter("Responsiveness", typeof (int), new Collection<Attribute> {
                    new ParameterAttribute()
                }));

                DynamicParameterDictionary.Add("MessageResolver", new RuntimeDefinedParameter("MessageResolver", typeof (GetMessageString), new Collection<Attribute> {
                    new ParameterAttribute()
                }));
            }
            return true;
        }
        
        public override string GetMessageString(string messageText, string defaultText) {
            messageText = DropMsgPrefix(messageText);

            if (string.IsNullOrWhiteSpace(defaultText) || defaultText.IndexOf("MSG:", StringComparison.OrdinalIgnoreCase) == 0) {
                defaultText = Messages.ResourceManager.GetString(messageText);
            }

            string result = null;
            //when a user does find-module xjea, psreadline | install-module, the install-module starts executing before find-module completes. This
            //is expected. However when the install-module starts, the MessageResolver delegate stops responding. This is a PowerShell thing.
            //Once the delegate is blocked, it's blocked for the subsequence log message calls for the current cmdlet, find-module. Note that the
            //next cmdlet, install-module has no impact on the delegate blocking issue, meaning all messages are expected to be logged for install-module.
            //_messageResolverNotResponding is for avoiding continuously waiting for unresponsive MessageResolver delegate.
            if (MessageResolver != null && !_messageResolverNotResponding) {
                // if the consumer has specified a MessageResolver delegate, we need to call it on the main thread
                // because powershell won't let us use the default runspace from another thread.

                // if we are anywhere but the end of the pipeline, the delegate here would block on stuff later in the pipe
                // and since we're blocking *that* based on the the resolution of this, we're better off just skipping
                // the message resolution for things earlier in the pipeline.
                _messageResolverNotResponding = !ExecuteOnMainThread(() =>{
                    result = MessageResolver(messageText, defaultText);
                    if (string.IsNullOrWhiteSpace(result)) {
                        result = null;
                    }
                    return true;
                }).Wait(5000); // you don't get a lot of time to get back to me on this...
            }

            return result ?? Messages.ResourceManager.GetString(messageText) ?? defaultText;
        }

        public virtual bool AskPermission(string permission) {
#if DEBUG
            Message(Constants.Messages.NotImplemented, permission);
#endif
            return true;
        }

        public virtual IEnumerable<string> GetOptionValues(string key) {
            // try to see whether the key is a dynamic parameter first. if we cannot find it then go to bound parameters.
            var dynamicValues = DynamicParameterDictionary.Values.OfType<CustomRuntimeDefinedParameter>().Where(each => each.IsSet && each.Name == key).SelectMany(each => each.GetValues(this));
            if (dynamicValues.Any())
            {
                return dynamicValues;
            }

            if (MyInvocation.BoundParameters.ContainsKey(key)) {
                var value = MyInvocation.BoundParameters[key];
                if (value is string || value is int) {
                    return new[] {
                        MyInvocation.BoundParameters[key].ToString()
                    };
                }
                if (value is SwitchParameter) {
                    return new[] {
                        ((SwitchParameter)MyInvocation.BoundParameters[key]).IsPresent.ToString()
                    };
                }
                if (value is string[]) {
                    return ((string[])value);
                }
                return new[] {
                    MyInvocation.BoundParameters[key].ToString()
                };
            }

            return Enumerable.Empty<string>();
        }

        public virtual IWebProxy WebProxy
        {
            get
            {
                return null;
            }
        }

        public virtual string CredentialUsername {
            get {
                return null;
            }
        }

        public virtual SecureString CredentialPassword {
            get {
                return null;
            }
        }

        public virtual bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource) {
#if DEBUG
            Message(Constants.Messages.NotImplemented, packageSource);
#endif
            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Justification = "It's a PowerShell behavior.")]
        bool IHostApi.ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            var shouldContinueResult = base.ShouldContinue(query, caption, true).Result;
            if (shouldContinueResult != null)
            {
                yesToAll = shouldContinueResult.yesToAll;
                noToAll = shouldContinueResult.noToAll;
                return shouldContinueResult.result;
            }
                
            return false;
        }

        bool IHostApi.ShouldContinue(string query, string caption)
        {
            return base.ShouldContinue(query, caption).Result;
        }

        public virtual bool ShouldBootstrapProvider(string requestor, string providerName, string providerVersion, string providerType, string location, string destination) {
            try {
                if (Force || ForceBootstrap) {
                    return true;
                }

                return ShouldContinue(FormatMessageString(Constants.Messages.QueryBootstrap, providerName),
                    FormatMessageString(Constants.Messages.BootstrapProvider,
                        !string.IsNullOrWhiteSpace(requestor) ?
                            FormatMessageString(Constants.Messages.BootstrapProviderProviderRequested, requestor, providerName, providerVersion) :
                            FormatMessageString(Constants.Messages.BootstrapProviderUserRequested, providerName, providerVersion),
                        !string.IsNullOrWhiteSpace(providerType) && providerType.Equals(Constants.AssemblyProviderType) ?
                            FormatMessageString(Constants.Messages.BootstrapManualAssembly, providerName, location, destination) :
                            FormatMessageString(Constants.Messages.BootstrapManualInstall, providerName, location))).Result;
            } catch (Exception e) {
                e.Dump();
            }
            return false;
        }

        public virtual bool IsInteractive {
            get {
                return IsInvocation;
            }
        }

        public virtual int CallCount {
            get {
                return _callCount;
            }
        }

        #endregion

        protected override void Init() {
            if (!IsInitialized) {
                // get the service ( forces initialization )
                var x = PackageManagementService;
            }
        }

        protected virtual string BootstrapNuGet
        {
            get
            {
                return _bootstrapNuGet;
            }
            set
            {
                _bootstrapNuGet = value;
            }
        }

        protected virtual IHostApi PackageManagementHost
        {
            get
            {
                IHostApi parent = this;
                return new object[] {
                    new {
                        GetOptionValues = new Func<string, IEnumerable<string>>(key => {
                            if (key.EqualsIgnoreCase(Microsoft.PackageManagement.Internal.Constants.BootstrapNuGet)) {
                                return BootstrapNuGet.SingleItemAsEnumerable();
                            }

                            return this.GetOptionValues(key);
                        }),
                    },
                    parent,
                }.As<IHostApi>();
            }
        }

        protected IEnumerable<PackageProvider> SelectProviders(string[] names) {
            if (names.IsNullOrEmpty()) {
                return ShouldSelectAllProviders ? PackageManagementService.SelectProviders(null, PackageManagementHost.SuppressErrorsAndWarnings(IsProcessing))
                     : PackageManagementService.SelectProviders(null, PackageManagementHost.SuppressErrorsAndWarnings(IsProcessing)).Where(each => !each.Features.ContainsKey(Microsoft.PackageManagement.Internal.Constants.Features.AutomationOnly));               
                
            }
            // you can manually ask for any provider by name, if it is for automation only.
            if (IsInvocation) {
            
                return names.SelectMany(each => PackageManagementService.SelectProviders(each, this));
            }
            return names.SelectMany(each => PackageManagementService.SelectProviders(each, PackageManagementHost.SuppressErrorsAndWarnings(IsProcessing)));
        }

        protected IEnumerable<PackageProvider> SelectProviders(string name) {
            // you can manually ask for any provider by name, if it is for automation only.
            if (IsInvocation) {
                var result = PackageManagementService.SelectProviders(name, PackageManagementHost).ToArray();
                if ((result.Length == 0) && ShouldLogError) {
                    Error(Constants.Errors.UnknownProvider, name);
                }
                return result;
            }

            var r = PackageManagementService.SelectProviders(name, PackageManagementHost.SuppressErrorsAndWarnings(IsProcessing)).ToArray();
            if ((r.Length == 0) && ShouldLogError){
                Error(Constants.Errors.UnknownProvider, name);               
            }
            return r;
        }

        public override bool ConsumeDynamicParameters() {
            // pull data from dynamic parameters and place them into the DynamicOptions collection.
            foreach (var rdp in DynamicParameterDictionary.Keys.Select(d => DynamicParameterDictionary[d]).Where(rdp => rdp.IsSet)) {
                if (rdp.ParameterType == typeof (SwitchParameter)) {
                    _dynamicOptions[rdp.Name] = true;
                } else {
                    _dynamicOptions[rdp.Name] = rdp.Value;
                }
            }
            return true;
        }

        #region Event and telemetry stuff
        //Calling PowerShell Telemetry APIs
        protected void TraceMessage(string message, SoftwareIdentity swidObject) {
#if !UNIX
            TelemetryAPI.TraceMessage(message,
                new {
                    PackageName = swidObject.Name,
                    PackageVersion = swidObject.Version,
                    PackageProviderName = swidObject.ProviderName,
                    Repository = swidObject.Source,
                    ExecutionStatus = swidObject.Status,
                    ExecutionTime = DateTime.Today
                });
#endif
        }

        protected enum EventTask {
            None = 0,
            Install = 1,
            Uninstall = 2,
            Download = 3
        }

        
        //We use Microsoft-Windows-PowerShell event source to log PackageManagement events.
        //4101... is the eventid of Microsoft-Windows-PowerShell event provider
        //You can find the events in Microsoft-Windows-PowerShell event log
        protected enum EventId
        {
            Install = 4101,
            Uninstall = 4102,
            Save = 4103
        }

        protected void LogEvent(EventTask task, EventId id, string context, string name, string version, string providerName, string source, string status, string destinationPath)
        {
#if !UNIX
            var iis = InitialSessionState.CreateDefault2();

            using (Runspace rs = RunspaceFactory.CreateRunspace(iis))
            {
                using (PowerShell powershell = PowerShell.Create())
                {
                    try
                    {
                        rs.Open();
                        powershell.Runspace = rs;
                        powershell.AddCommand(Constants.NewWinEvent);
                        powershell.AddParameter("ProviderName", Constants.PowerShellProviderName);
                        powershell.AddParameter("Id", id);

                        powershell.AddParameter("Payload", new object[] {
                        task.ToString(),
                        string.Format(CultureInfo.InvariantCulture, "Package={0}, Version={1}, Provider={2}, Source={3}, Status={4}, DestinationPath={5}", name, version, providerName, source, status, destinationPath),
                        context});

                        powershell.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Verbose(ex.Message);
                    }
                }
            }
#endif
        }

        #endregion
    }
}
