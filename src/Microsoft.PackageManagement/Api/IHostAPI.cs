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

namespace Microsoft.PackageManagement.Internal.Api {
    using System.Collections.Generic;
    using System.Security;
    using System.Net;

    /// <summary>
    /// Functions implemented by the HOST to provide contextual information and control to for the current request.
    /// </summary>
    public interface IHostApi {

        /// <summary>
        ///  The HOST should return true if the current request in progress should be cancelled.
        /// </summary>
        bool IsCanceled {get;}

        /// <summary>
        /// the HOST should return a localized string for the given messageText, or null if not localizable.
        /// </summary>
        /// <param name="messageText">
        ///     The message ID or text string to resolve.
        /// </param>
        /// <param name="defaultText">
        ///     a default message text that would be used if there is no match in local resources. 
        ///     This provides the HOST the opportunity to reformat the actual message even if they don't match it. 
        ///     (PSGet uses this)
        /// </param>
        /// <returns></returns>
        string GetMessageString(string messageText, string defaultText);

        /// <summary>
        /// Sends a formatted warning message to the HOST.
        /// </summary>
        /// <param name="messageText">
        ///     The fully formatted warning message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool Warning(string messageText);

        /// <summary>
        /// Sends a complex Error message to the HOST.
        /// </summary>
        /// <param name="id">An identifier that can be used to uniquely id the Error</param>
        /// <param name="category">A category for the message (should map to a PowerShell ErrorCategory enumeration)</param>
        /// <param name="targetObjectValue">a target object the operation has failed for.</param>
        /// <param name="messageText">
        ///     The fully formatted error message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>        /// <returns></returns>
        bool Error(string id, string category, string targetObjectValue, string messageText);

        /// <summary>
        /// Sends a status message to the HOST
        /// </summary>
        /// <param name="messageText">
        ///     The fully formatted message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool Message(string messageText);

        /// <summary>
        /// Sends a message to the verbose channel. 
        /// </summary>
        /// <param name="messageText">
        ///     The fully formatted verbose message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool Verbose(string messageText);

        /// <summary>
        /// Sends a message to the debug channel
        /// </summary>
        /// <param name="messageText">
        ///     The fully formatted debug message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool Debug(string messageText);

        /// <summary>
        /// Starts a progress indicator
        /// </summary>
        /// <param name="parentActivityId">the number of a parent progress indicator. Should be zero if there is no parent.</param>
        /// <param name="messageText">
        ///     The fully formatted progress message to display to the user
        /// </param>
        /// <returns>
        ///     The progress indicator handle for the new progress message
        /// </returns>
        int StartProgress(int parentActivityId, string messageText);

        /// <summary>
        /// Write progress using powershell write progress directly
        /// </summary>
        /// <param name="activity">Specifies the first line of text in the heading above the status bar.</param>
        /// <param name="messageText">Corresponds to status on write-progress. Specifies the second line of text in the heading above the status bar. This text describes current state of the activity.</param>
        /// <param name="activityId">
        /// Corresponds to id on write-progress. Specifies an ID that distinguishes each progress bar from the others.
        /// Use this parameter when you are creating more than one progress bar in a single command.
        /// If the progress bars do not have different IDs, they are superimposed instead of being displayed in a series.
        /// </param>
        /// <param name="progressPercentage">Specifies the percentage of the activity that is completed. Use the value -1 if the percentage complete is unknown or not applicable.</param>
        /// <param name="secondsRemaining">Specifies the projected number of seconds remaining until the activity is completed. Use the value -1 if the number of seconds remaining is unknown or not applicable.</param>
        /// <param name="currentOperation">Specifies the line of text below the progress bar. This text describes the operation that is currently taking place.</param>
        /// <param name="parentActivityId">Identifies the parent activity of the current activity. Use the value -1 if the current activity has no parent activity.</param>
        /// <param name="completed">Indicates whether the progress bar is visible</param>
        /// <returns></returns>
        bool Progress(string activity, string messageText, int activityId, int progressPercentage, int secondsRemaining, string currentOperation, int parentActivityId, bool completed);

        /// <summary>
        ///     Sends a progress update
        /// </summary>
        /// <param name="activityId">
        ///     The Progress indicator ID (from StartProgress)
        /// </param>
        /// <param name="progressPercentage">
        ///     The Percentage for the progress (0-100)
        /// </param>
        /// <param name="messageText">
        ///     The fully formatted progress message to display to the user
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool Progress(int activityId, int progressPercentage, string messageText);

        /// <summary>
        ///     Ends a progress notification
        /// </summary>
        /// <param name="activityId">
        ///     The Progress indicator ID (from StartProgress)
        /// </param>
        /// <param name="isSuccessful">
        ///     true if the operation was successful.
        /// </param>
        /// <returns>
        ///     This should return true if the request is cancelled.
        /// </returns>
        bool CompleteProgress(int activityId, bool isSuccessful);

        /// <summary>
        ///     Used by a provider to request what metadata keys were passed from the user
        /// </summary>
        /// <returns>an collection of the keys for the specified dynamic options</returns>
        IEnumerable<string> OptionKeys {get;}

        /// <summary>
        ///     Used by a provider to request the values for a given dynamic option
        /// </summary>
        /// <param name="key">the dynamic option Key (should be present in OptionKeys)</param>
        /// <returns>an collection of the value for the specified dynamic option</returns>
        IEnumerable<string> GetOptionValues(string key);

        /// <summary>
        /// Proxy used by provider
        /// </summary>
        IWebProxy WebProxy { get; }

        /// <summary>
        /// A collection of sources specified by the user. If this is null or empty, the provider should assume 'all the registered sources'
        /// </summary>
        IEnumerable<string> Sources {get;}

        /// <summary>
        /// A credential username specified by the user 
        /// </summary>
        string CredentialUsername {get;}

        /// <summary>
        /// A credential password specified by the user 
        /// </summary>
        SecureString CredentialPassword {get;}

        /// <summary>
        /// The CORE may ask the HOST if a given provider should be bootstrapped during an operation.
        /// </summary>
        /// <param name="requestor">the name of the provider or component requesting the provider.</param>
        /// <param name="providerName">the name of the requested provider</param>
        /// <param name="providerVersion">the minimum version of the provider required</param>
        /// <param name="providerType"></param>
        /// <param name="location">the remote location that the provider is being bootstrapped from</param>
        /// <param name="destination">the target folder where the provider is to be installed.</param>
        /// <returns></returns>
        bool ShouldBootstrapProvider(string requestor, string providerName, string providerVersion, string providerType, string location, string destination);

        /// <summary>
        /// the CORE may aske the user if a given package should be allowed to install.
        /// </summary>
        /// <param name="package">the name of the package</param>
        /// <param name="packageSource">the name of the source of the package</param>
        /// <returns></returns>
        bool ShouldContinueWithUntrustedPackageSource(string package, string packageSource);

        /// <summary>
        /// Allow a package provider to confirm a user whether the process should continue
        /// </summary>
        /// <param name="query">Query that inquires whether the cmdlet should continue.</param>
        /// <param name="caption">Caption of the window that might be displayed when the user is prompted whether or not to perform the action.</param>
        /// <param name="yesToAll">True if and only if the user selects the yesToall option. If this is already True, ShouldContinue will bypass the prompt and return True.</param>
        /// <param name="noToAll">True if and only if the user selects the noToall option. If this is already True, ShouldContinue will bypass the prompt and return False.</param>
        /// <returns></returns>
        bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll);

        /// <summary>
        /// Allow a package provider to confirm a user whether the process should continue
        /// </summary>
        /// <param name="query">Query that inquires whether the cmdlet should continue.</param>
        /// <param name="caption">Caption of the window that might be displayed when the user is prompted whether or not to perform the action.</param>
        /// <returns></returns>
        bool ShouldContinue(string query, string caption);

        /// <summary>
        /// Asks an arbitrary true/false question of the user.
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        bool AskPermission(string permission);

        /// <summary>
        /// The HOST should return 'True' if the current operation is executed in an interactive environment
        /// and the user should be able to respond to queries.
        /// </summary>
        bool IsInteractive {get;}

        /// <summary>
        /// The HOST should give each individual request a unique value (used to track if a particular operation has been tried before)
        /// </summary>
        int CallCount {get;}
    }
}