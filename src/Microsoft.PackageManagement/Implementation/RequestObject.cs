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

namespace Microsoft.PackageManagement.Internal.Implementation {
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using Internal.Api;
    using Resources;
    using Utility.Async;
    using Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Packaging;
    using System.Net;

    public abstract class RequestObject : AsyncAction, IRequest, IHostApi {
        private static int _c;
        protected Action<RequestObject> _action;
        private IHostApi _hostApi;
        protected Task _invocationTask;
        protected readonly ProviderBase Provider;

        internal RequestObject(ProviderBase provider, IHostApi hostApi, Action<RequestObject> action) {
            // construct request object
            _hostApi = hostApi;
            Provider = provider;
            _action = action;
        }

        internal RequestObject(ProviderBase provider, IHostApi hostApi) {
            // construct request object
            _hostApi = hostApi;
            Provider = provider;
        }

        private bool CanCallHost {
            get {
                if (IsCompleted || IsAborted) {
                    return false;
                }
                Activity();
                return _hostApi != null;
            }
        }

        public override bool IsCanceled {
            get {
                return base.IsCanceled || (CanCallHost && _hostApi.IsCanceled);
            }
        }

        public override void WarnBeforeResponsivenessCancellation() {
            if (CanCallHost) {
                _hostApi.Warning((GetMessageString(Constants.Messages.ProviderNotResponsive, null) ?? "Provider '{0}' is not respecting the responsiveness threshold in a timely fashion; Canceling request.").format(Provider.ProviderName));
            }
        }
        public override void WarnBeforeTimeoutCancellation() {
            if (CanCallHost) {
                _hostApi.Warning((GetMessageString(Constants.Messages.ProviderTimeoutExceeded, null) ?? "Provider '{0}' is not completing the request in the time allowed; Canceling request.").format(Provider.ProviderName));
            }
        }


        protected void InvokeImpl() {
            _invocationTask = Task.Factory.StartNew(() => {
                _invocationThread = Thread.CurrentThread;
                _invocationThread.Name = Provider.ProviderName + ":" + _c++;

                try {
                    _action(this);
#if !CORECLR
                } catch (ThreadAbortException) {
#if DEEP_DEBUG
                    Console.WriteLine("Thread Aborted for {0} : {1}", _invocationThread.Name, DateTime.Now.Subtract(_callStart).TotalSeconds);
#endif
                    Thread.ResetAbort();
#endif
                } catch (Exception e) {
                    e.Dump();
                } finally {
                    try {
                        Complete();
                    }
                    catch (Exception e)
                    {
                        e.Dump();
                    } 
                }
            }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default); // .ContinueWith(antecedent => Complete());

            // start thread, call function
            StartCall();
        }

        #region HostApi Wrapper
        public string DropMsgPrefix(string messageText) {
            if (string.IsNullOrWhiteSpace(messageText)) {
                return messageText;
            }
            return messageText.StartsWith("MSG:", StringComparison.OrdinalIgnoreCase) ? messageText.Substring(4) : messageText;
        }

        public string GetMessageString(string messageText, string defaultText) {
            if (CanCallHost) {
                if (string.IsNullOrWhiteSpace(defaultText) || defaultText.StartsWith("MSG:", StringComparison.OrdinalIgnoreCase)) {
                    defaultText = Messages.ResourceManager.GetString(DropMsgPrefix(messageText));
                }

                return _hostApi.GetMessageString(messageText, defaultText);
            }
            return null;
        }

        public bool Warning(string messageText) {
            if (CanCallHost) {
                return _hostApi.Warning(GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public bool Error(string id, string category, string targetObjectValue, string messageText) {
            if (CanCallHost) {
                return _hostApi.Error(id, category, targetObjectValue, GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public bool Message(string messageText) {
            if (CanCallHost) {
                return _hostApi.Message(GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public bool Verbose(string messageText) {
            if (CanCallHost) {
                return _hostApi.Verbose(GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public bool Debug(string messageText) {
            if (CanCallHost) {
                return _hostApi.Debug(GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public int StartProgress(int parentActivityId, string messageText) {
            if (CanCallHost) {
                return _hostApi.StartProgress(parentActivityId, messageText);
            }
            return 0;
        }

        public bool Progress(int activityId, int progressPercentage, string messageText) {
            if (CanCallHost) {
                return _hostApi.Progress(activityId, progressPercentage, GetMessageString(messageText, null) ?? messageText);
            }
            return true;
        }

        public bool Progress(string activity, string status, int id, int percentcomplete, int secondsremaining, string currentoperation, int parentid, bool completed)
        {
            if (CanCallHost)
            {
                return _hostApi.Progress(activity, status, id, percentcomplete, secondsremaining, currentoperation, parentid, completed);
            }

            return true;
        }

        public bool CompleteProgress(int activityId, bool isSuccessful) {
            if (CanCallHost) {
                return _hostApi.CompleteProgress(activityId, isSuccessful);
            }
            return true;
        }

        public IEnumerable<string> OptionKeys {
            get {
                if (CanCallHost) {
                    return _hostApi.OptionKeys;
                }
                return new string[0];
            }
        }

        public IEnumerable<string> GetOptionValues(string key) {
            if (CanCallHost) {
                return _hostApi.GetOptionValues(key);
            }
            return new string[0];
        }

        public IEnumerable<string> Sources {
            get {
                if (CanCallHost) {
                    return _hostApi.Sources;
                }
                return new string[0];
            }
        }

        public IWebProxy WebProxy
        {
            get
            {
                return CanCallHost ? _hostApi.WebProxy : null;
            }
        }

        public string CredentialUsername {
            get {
                return CanCallHost ? _hostApi.CredentialUsername : null;
            }
        }

        public SecureString CredentialPassword {
            get {
                return CanCallHost ? _hostApi.CredentialPassword : null;
            }
        }

        public bool ShouldBootstrapProvider(string requestor, string providerName, string providerVersion, string providerType, string location, string destination) {
            if (CanCallHost) {
                return _hostApi.ShouldBootstrapProvider(requestor, providerName, providerVersion, providerType, location, destination);
            }
            return false;
        }

        public bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource) {
            if (CanCallHost) {
                return _hostApi.ShouldContinueWithUntrustedPackageSource(package, packageSource);
            }
            return false;
        }

        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll) {
            if (CanCallHost) {
                return _hostApi.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
            }
            return false;
        }

        public bool ShouldContinue(string query, string caption)
        {
            if (CanCallHost) {
                return _hostApi.ShouldContinue(query, caption);
            }
            return false;
        }
        public bool AskPermission(string permission) {
            if (CanCallHost) {
                return _hostApi.AskPermission(permission);
            }
            return false;
        }

        public bool IsInteractive {
            get {
                if (CanCallHost) {
                    return _hostApi.IsInteractive;
                }
                return false;
            }
        }

        public int CallCount {
            get {
                if (CanCallHost) {
                    return _hostApi.CallCount;
                }
                return 0;
            }
        }

        #endregion

        #region CoreApi implementation

        public IPackageManagementService PackageManagementService {
            get {
                Activity();
                return PackageManager.Instance;
            }
        }

        public IProviderServices ProviderServices {
            get {
                Activity();
                return ProviderServicesImpl.Instance;
            }
        }



        #endregion

        #region response api implementation

        public virtual string YieldSoftwareIdentity(string fastPath, string name, string version, string versionScheme, string summary, string source, string searchKey, string fullPath, string packageFileName) {
            Debug("Unexpected call to YieldSoftwareIdentity in RequestObject");
            return null;
        }

        public virtual string YieldSoftwareIdentityXml(string xmlSwidTag, bool commitImmediately)
        {
            Debug("Unexpected call to YieldSoftwareIdentityXml in RequestObject");
            return null;
        }

        public virtual string AddMetadata(string name, string value) {
            Debug("Unexpected call to AddMetaData in RequestObject");
            return null;
        }

        public virtual string AddTagId(string tagId) {
            Debug("Unexpected call to AddMetaData in RequestObject");
            return null;
        }

        public virtual string AddCulture(string xmlLang)
        {
            Debug("Unexpected call to Culture in RequestObject");
            return null;
        }

        public virtual string AddMetadata(string elementPath, string name, string value) {
            Debug("Unexpected call to AddMetaData in RequestObject");
            return null;
        }

        public virtual string AddMetadata(string elementPath, Uri @namespace, string name, string value) {
            Debug("Unexpected call to AddMetaData in RequestObject");
            return null;
        }

        public virtual string AddMeta(string elementPath) {
            Debug("Unexpected call to AddMeta in RequestObject");
            return null;
        }

        public virtual string AddPayload() {
            Debug("Unexpected call to AddPayload in RequestObject");
            return null;
        }

        public virtual string AddEvidence(DateTime date, string deviceId) {
            Debug("Unexpected call to AddEvidence in RequestObject");
            return null;
        }
        public virtual string AddDirectory(string elementPath, string directoryName, string location, string root, bool isKey) {
            Debug("Unexpected call to AddDirectory in RequestObject");
            return null;
        }

        public virtual string AddFile(string elementPath, string fileName, string location, string root, bool isKey, long size, string version) {
            Debug("Unexpected call to AddFile in RequestObject");
            return null;
        }

        public virtual string AddProcess(string elementPath, string processName, int pid) {
            Debug("Unexpected call to AddProcess in RequestObject");
            return null;
        }

        public virtual string AddResource(string elementPath, string type) {
            Debug("Unexpected call to AddResource in RequestObject");
            return null;
        }

        public virtual string AddEntity(string name, string regid, string role, string thumbprint) {
            Debug("Unexpected call to AddEntity in RequestObject");
            return null;
        }

        public virtual string AddLink(Uri referenceUri, string relationship, string mediaType, string ownership, string use, string appliesToMedia, string artifact) {
            Debug("Unexpected call to AddLink in RequestObject");
            return null;
        }

        public virtual string AddDependency(string providerName, string packageName, string version, string source, string appliesTo) {
            Debug("Unexpected call to AddDependency in RequestObject");
            return null;
        }

        public virtual bool YieldPackageSource(string name, string location, bool isTrusted, bool isRegistered, bool isValidated) {
            Console.WriteLine("SHOULD NOT GET HERE [YieldSoftwareIdentity] ================================================");
            // todo: give an actual error here
            return true; // cancel
        }

        public virtual bool YieldDynamicOption(string name, string expectedType, bool isRequired) {
            Console.WriteLine("SHOULD NOT GET HERE [YieldSoftwareIdentity] ================================================");
            // todo: give an actual error here
            return true; // cancel
        }

        public virtual bool YieldKeyValuePair(string key, string value) {
            Console.WriteLine("SHOULD NOT GET HERE [YieldSoftwareIdentity] ================================================");
            // todo: give an actual error here
            return true; // cancel
        }

        public virtual bool YieldValue(string value) {
            Console.WriteLine("SHOULD NOT GET HERE [YieldSoftwareIdentity] ================================================");
            // todo: give an actual error here
            return true; // cancel
        }

        #endregion


    }
}