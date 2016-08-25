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
    using System.Net;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security;
    using Internal.Api;

    public abstract class Request : IRequest {
        #region core-apis

        public abstract IPackageManagementService PackageManagementService {get;}

        public abstract IProviderServices ProviderServices {get;}

        #endregion

        #region copy host-apis

        /* Synced/Generated code =================================================== */
        public abstract bool IsCanceled { get; }

        public abstract string GetMessageString(string messageText, string defaultText);

        public abstract bool Warning(string messageText);

        public abstract bool Error(string id, string category, string targetObjectValue, string messageText);

        public abstract bool Message(string messageText);

        public abstract bool Verbose(string messageText);

        public abstract bool Debug(string messageText);

        public abstract int StartProgress(int parentActivityId, string messageText);

        public abstract bool Progress(int activityId, int progressPercentage, string messageText);

        public abstract bool Progress(string activity, string messageText, int activityId, int progressPercentage, int secondsRemaining, string currentOperation, int parentActivityId, bool completed);

        public abstract bool CompleteProgress(int activityId, bool isSuccessful);

        /// <summary>
        ///     Used by a provider to request what metadata keys were passed from the user
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerable<string> OptionKeys {get;}

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract IEnumerable<string> GetOptionValues(string key);

        public abstract IEnumerable<string> Sources {get;}

        public abstract IWebProxy WebProxy { get; }

        public abstract string CredentialUsername { get; }

        public abstract SecureString CredentialPassword { get; }

        public abstract bool ShouldBootstrapProvider(string requestor, string providerName, string providerVersion, string providerType, string location, string destination);

        public abstract bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource);

        public abstract bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);

        public abstract bool ShouldContinue(string query, string caption);

        public abstract bool AskPermission(string permission);

        public abstract bool IsInteractive {get;}

        public abstract int CallCount {get;}

        #endregion

        #region copy response-apis

        /* Synced/Generated code =================================================== */

        /// <summary>
        ///     Used by a provider to return fields for a SoftwareIdentity.
        /// </summary>
        /// <param name="fastPath"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="versionScheme"></param>
        /// <param name="summary"></param>
        /// <param name="source"></param>
        /// <param name="searchKey"></param>
        /// <param name="fullPath"></param>
        /// <param name="packageFileName"></param>
        /// <returns></returns>
        public abstract string YieldSoftwareIdentity(string fastPath, string name, string version, string versionScheme, string summary, string source, string searchKey, string fullPath, string packageFileName);

        public abstract string YieldSoftwareIdentityXml(string xmlSwidTag, bool commitImmediately);

        public abstract bool IsSwidTagXml(string xmlSwidTag);

        public abstract string AddTagId(string tagId);

        public abstract string AddCulture(string xmlLang);

        public abstract string AddMetadata(string name, string value);

        public abstract string AddMetadata(string elementPath, string name, string value);

        public abstract string AddMetadata(string elementPath, Uri @namespace, string name, string value);

        public abstract string AddMeta(string elementPath);

        public abstract string AddEntity(string name, string regid, string role, string thumbprint);

        public abstract string AddLink(Uri referenceUri, string relationship, string mediaType, string ownership, string use, string appliesToMedia, string artifact);

        public abstract string AddDependency(string providerName, string packageName, string version, string source, string appliesTo);

        public abstract string AddPayload();

        public abstract string AddEvidence(DateTime date, string deviceId);

        public abstract string AddDirectory(string elementPath, string directoryName, string location, string root, bool isKey);

        public abstract string AddFile(string elementPath, string fileName, string location, string root, bool isKey, long size, string version);

        public abstract string AddProcess(string elementPath, string processName, int pid);

        public abstract string AddResource(string elementPath, string type);

        /// <summary>
        ///     Used by a provider to return fields for a package source (repository)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="location"></param>
        /// <param name="isTrusted"></param>
        /// <param name="isRegistered"></param>
        /// <param name="isValidated"></param>
        /// <returns></returns>
        public abstract bool YieldPackageSource(string name, string location, bool isTrusted, bool isRegistered, bool isValidated);

        /// <summary>
        ///     Used by a provider to return the fields for a Metadata Definition
        ///     The cmdlets can use this to supply tab-completion for metadata to the user.
        /// </summary>
        /// <param name="name">the provider-defined name of the option</param>
        /// <param name="expectedType"> one of ['string','int','path','switch']</param>
        /// <param name="isRequired">if the parameter is mandatory</param>
        /// <returns></returns>
        public abstract bool YieldDynamicOption(string name, string expectedType, bool isRequired);

        public bool YieldDynamicOption(string name, string expectedType, bool isRequired, IEnumerable<string> permittedValues) {
            return YieldDynamicOption(name, expectedType, isRequired) && (permittedValues ?? Enumerable.Empty<string>()).All(each => YieldKeyValuePair(name, each));
        }

        public abstract bool YieldKeyValuePair(string key, string value);

        public abstract bool YieldValue(string value);

        #endregion


        #region declare Request-implementation
        /// <summary>
        ///     Yield values in a dictionary as key/value pairs. (one pair for each value in each key)
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public bool Yield(Dictionary<string, string[]> dictionary) {
            if( dictionary != null ) {
                return dictionary.All(Yield);
            }
            return true;
        }

        public bool Yield(KeyValuePair<string, string[]> pair) {
            if (pair.Value.Length == 0) {
                return YieldKeyValuePair(pair.Key, null);
            }
            return pair.Value.All(each => YieldKeyValuePair(pair.Key, each));
        }

        public bool Error(ErrorCategory category, string targetObjectValue, string messageText, params object[] args) {
            return Error(messageText, category.ToString(), targetObjectValue, FormatMessageString(messageText, args));
        }

        public bool Warning(string messageText, params object[] args) {
            return Warning(FormatMessageString(messageText, args));
        }

        public bool Message(string messageText, params object[] args) {
            return Message(FormatMessageString(messageText, args));
        }

        public bool Verbose(string messageText, params object[] args) {
            return Verbose(FormatMessageString(messageText, args));
        }

        public bool Debug(string messageText, params object[] args) {
            return Debug(FormatMessageString(messageText, args));
        }

        public int StartProgress(int parentActivityId, string messageText, params object[] args) {
            return StartProgress(parentActivityId, FormatMessageString(messageText, args));
        }

        public bool Progress(string activity, string messageText, int activityId, int progressPercentage, int secondsRemaining, string currentOperation, int parentActivityId, bool completed, params object[] args)
        {
            return Progress(activity, FormatMessageString(messageText, args), activityId, progressPercentage, secondsRemaining, currentOperation, parentActivityId, completed);
        }

        public bool Progress(int activityId, int progressPercentage, string messageText, params object[] args) {
            return Progress(activityId, progressPercentage, FormatMessageString(messageText, args));
        }

        public string GetOptionValue(string name) {
            // get the value from the request
            return (GetOptionValues(name) ?? Enumerable.Empty<string>()).LastOrDefault();
        }

        private static string FixMeFormat(string formatString, object[] args) {
            if (args == null || args.Length == 0) {
                // not really any args, and not really expecting any
                return formatString.Replace('{', '\u00ab').Replace('}', '\u00bb');
            }
            return args.Aggregate(formatString.Replace('{', '\u00ab').Replace('}', '\u00bb'), (current, arg) => current + string.Format(CultureInfo.CurrentCulture, " \u00ab{0}\u00bb", arg));
        }

        #endregion

        protected string FormatMessageString(string messageText, params object[] args) {
            if (string.IsNullOrWhiteSpace(messageText)) {
                return string.Empty;
            }

            if (messageText.IndexOf(Constants.MSGPrefix, StringComparison.CurrentCultureIgnoreCase) == 0) {
                // check with the caller first, then with the local resources, and fallback to using the messageText itself.
                messageText = GetMessageString(messageText.Substring(Constants.MSGPrefix.Length),messageText) ?? messageText;
            }

            // if it doesn't look like we have the correct number of parameters
            // let's return a fix-me-format string.
            var c = messageText.ToCharArray().Where(each => each == '{').Count();
            if (c < args.Length) {
                return FixMeFormat(messageText, args);
            }
            return string.Format(CultureInfo.CurrentCulture, messageText, args);
        }
    }
}
