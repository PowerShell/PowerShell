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
    using System.Linq;
    using System.Text.RegularExpressions;
    using Api;
    using PackageManagement.Packaging;
    using Utility.Extensions;
    using Utility.Platform;
    using Utility.Plugin;
    using Process = System.Diagnostics.Process;

    internal class ProviderServicesImpl : IProviderServices {
        internal static IProviderServices Instance = new ProviderServicesImpl();
        private static readonly Regex _canonicalPackageRegex = new Regex(@"([^:]*?):([^/\#]*)/?([^#]*)\#?(.*)");

        private PackageManagementService PackageManagementService {
            get {
                return PackageManager.Instance as PackageManagementService;
            }
        }

        public bool IsElevated {
            get {
                return AdminPrivilege.IsElevated;
            }
        }

        public IEnumerable<SoftwareIdentity> FindPackageByCanonicalId(string canonicalId, IRequest requestObject) {
            if (requestObject == null) {
                throw new ArgumentNullException("requestObject");
            }

            return PackageManagementService.FindPackageByCanonicalId(canonicalId, requestObject);
        }

        public string GetCanonicalPackageId(string providerName, string packageName, string version, string source) {
            return SoftwareIdentity.CreateCanonicalId(providerName, packageName, version, source);
        }

        public string ParseProviderName(string canonicalPackageId) {
            return _canonicalPackageRegex.Match(canonicalPackageId).Groups[1].Value;
        }

        public string ParsePackageName(string canonicalPackageId) {
            return _canonicalPackageRegex.Match(canonicalPackageId).Groups[2].Value;
        }

        public string ParsePackageVersion(string canonicalPackageId) {
            return _canonicalPackageRegex.Match(canonicalPackageId).Groups[3].Value;
        }

        public string ParsePackageSource(string canonicalPackageId) {
            return _canonicalPackageRegex.Match(canonicalPackageId).Groups[4].Value;
        }

        public bool IsSupportedArchive(string localFilename, IRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }
            if (!request.IsCanceled) {
                return PackageManagementService.Archivers.Values.Any(archiver => archiver.IsSupportedFile(localFilename));
            }
            return false;
        }

        public string DownloadFile(Uri remoteLocation, string localFilename, IRequest request) {
            return DownloadFile(remoteLocation, localFilename, -1, true, request);
        }

        public string DownloadFile(Uri remoteLocation, string localFilename, int timeoutMilliseconds, bool showProgress, IRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!request.IsCanceled) {
                // check the Uri type, see if we have anyone who can handle that
                // if so, call that provider's download file
                if (remoteLocation == null) {
                    throw new ArgumentNullException("remoteLocation");
                }

                foreach (var downloader in PackageManagementService.Downloaders.Values) {
                    if (downloader.SupportedUriSchemes.Contains(remoteLocation.Scheme, StringComparer.OrdinalIgnoreCase)) {
                        return downloader.DownloadFile(remoteLocation, localFilename,timeoutMilliseconds, showProgress, request);
                    }
                }

                Error(request, ErrorCategory.NotImplemented, remoteLocation.Scheme, Constants.Messages.ProtocolNotSupported, remoteLocation.Scheme);
            }
            return null;
        }

        public IEnumerable<string> UnpackArchive(string localFilename, string destinationFolder, IRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!request.IsCanceled) {
                // check who supports the archive type
                // and call that provider.
                if (request == null) {
                    throw new ArgumentNullException("request");
                }

                foreach (var archiver in PackageManagementService.Archivers.Values) {
                    if (archiver.IsSupportedFile(localFilename)) {
                        return archiver.UnpackArchive(localFilename, destinationFolder, request);
                    }
                }
                Error(request, ErrorCategory.NotImplemented, localFilename, Constants.Messages.UnsupportedArchive);
            }
            return Enumerable.Empty<string>();
        }

        public bool Install(string fileName, string additionalArgs, IRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!request.IsCanceled) {
                if (String.IsNullOrWhiteSpace(fileName)) {
                    return false;
                }

                // high-level api for simply installing a file
                // returns false if unsuccessful.
                foreach (var provider in PackageManager.Instance.PackageProviders) {
                    var packages = provider.FindPackageByFile(fileName, request).ToArray();
                    if (packages.Length > 0) {
                        // found a provider that can handle this package.
                        // install with this provider
                        // ToDo: @FutureGarrett -- we need to be able to handle priorities and who wins...
                        foreach (var package in packages) {
                            foreach (var installedPackage in provider.InstallPackage(package, request.Extend<IRequest>(new {
                                GetOptionValues = new Func<string, IEnumerable<string>>(key => {
                                    if (key.EqualsIgnoreCase("additionalArguments")) {
                                        return new[] {additionalArgs};
                                    }
                                    return request.GetOptionValues(key);
                                })
                            }))) {
                                Debug(request, "Installed internal package {0}", installedPackage.Name);
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsSignedAndTrusted(string filename, IRequest request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (!request.IsCanceled) {
                if (String.IsNullOrWhiteSpace(filename) || !filename.FileExists()) {
                    return false;
                }

                Debug(request, "Calling 'ProviderService::IsSignedAndTrusted, '{0}'", filename);

                // we are not using this function anywhere
#if !LINUX
                var wtd = new WinTrustData(filename);

                var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                return result == WinVerifyTrustResult.Success;
#endif
            }
            return false;
        }

        public int StartProcess(string filename, string arguments, bool requiresElevation, out string standardOutput, IRequest requestObject) {
            Process p = new Process();

            if (requiresElevation) {
                p.StartInfo.UseShellExecute = true;
            } else {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
            }

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = filename;

            if (!String.IsNullOrEmpty(arguments)) {
                p.StartInfo.Arguments = arguments;
            }

            p.Start();

            if (p.StartInfo.RedirectStandardOutput) {
                standardOutput = p.StandardOutput.ReadToEnd();
            } else {
                standardOutput = String.Empty;
            }

            p.WaitForExit();

            return p.ExitCode;
        }

        public bool Error(IRequest request, ErrorCategory category, string targetObjectValue, string messageText, params object[] args) {
            return request.Error(messageText, category.ToString(), targetObjectValue, request.FormatMessageString(messageText, args));
        }

        public bool Warning(IRequest request, string messageText, params object[] args) {
            return request.Warning(request.FormatMessageString(messageText, args));
        }

        public bool Message(IRequest request, string messageText, params object[] args) {
            return request.Message(request.FormatMessageString(messageText, args));
        }

        public bool Verbose(IRequest request, string messageText, params object[] args) {
            return request.Verbose(request.FormatMessageString(messageText, args));
        }

        public bool Debug(IRequest request, string messageText, params object[] args) {
            return request.Debug(request.FormatMessageString(messageText, args));
        }
    }
}
