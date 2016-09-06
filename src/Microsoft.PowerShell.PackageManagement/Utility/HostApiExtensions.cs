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
namespace Microsoft.PowerShell.PackageManagement.Utility {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Api;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Internal.Utility.Plugin;

    /// <summary>
    ///     This can be used when we want to override some of the functions that are passed
    ///     in as the implementation of the IHostApi (ie, 'request object').
    ///     Because the DynamicInterface DuckTyper will use all the objects passed in in order
    ///     to implement a given API, if we put in delegates to handle some of the functions
    ///     they will get called instead of the implementation in the current class. ('this')
    /// </summary>

    internal static class HostApiExtensions {
        internal static IHostApi ProviderSpecific(this IHostApi parent, PackageProvider provider) {
            var thisProviderIsCanceled = false;
            return new object[] {
                new {
                    Error = new Func<string, string, string, string, bool>((id, cat, targetobjectvalue, messageText) => {
                        // turn errors into warnings when we're working in parallel
                        parent.Warning(messageText);

                        thisProviderIsCanceled = true;
                        // tell the provider that yeah, this request is canceled.
                        return thisProviderIsCanceled;
                    }),

                    GetIsCanceled = new Func<bool>(() => parent.IsCanceled || thisProviderIsCanceled)
                },
                parent,
            }.As<IHostApi>();
        }

        internal static IHostApi SuppressErrorsAndWarnings(this IHostApi parent, bool isProcessing) {

            return new object[] {
                new {
                    Error = new Func<string, string, string, string, bool>((id, cat, targetobjectvalue, messageText) => {
#if DEBUG
                        parent.Verbose("Suppressed Error {0}".format(messageText));
#endif
                        return false;
                    }),
                    Warning = new Func<string, bool>((messageText) => {
#if DEBUG
                        parent.Verbose("Suppressed Warning {0}".format(messageText));
#endif
                        return true;
                    }),
                    Verbose = new Func<string, bool>((messageText) => {
                        if (isProcessing) {
                            parent.Verbose(messageText);
                        }
#if DEBUG
                        else {
                            parent.Verbose("Suppressed Verbose {0}".format(messageText));
                        }
#endif
                        return true;
                    }),

                },
                parent,
            }.As<IHostApi>();
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        internal static IHostApi SuppressBootstrapping(this IHostApi parent, bool isProcessing) {
            return new object[] {
                new {
                    IsInvocation = new Func<bool>(() => false),
                    ShouldBootstrapProvider = new Func<string, string, string, string, string, string, bool>((s1, s2, s3, s4, s5, s6) => false),
                    IsInteractive = new Func<bool>(() => false),
                },
                parent,
            }.As<IHostApi>();
        }
    }
}
