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

namespace Microsoft.PackageManagement.Msi.Internal {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Deployment.WindowsInstaller;
    using Deployment.WindowsInstaller.Package;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Utility.Collections;
    using PackageManagement.Internal.Utility.Extensions;
    using PackageManagement.Internal.Utility.Versions;
    using System.Management.Automation;

    public class MsiProvider {
        /// <summary>
        ///     The name of this Package Provider
        /// </summary>
        internal const string ProviderName = "msi";

        private static readonly Dictionary<string, string[]> _features = new Dictionary<string, string[]> {
            {Constants.Features.SupportedExtensions, new[] {"msi", "msp"}},
            {Constants.Features.MagicSignatures, new[] {Constants.Signatures.OleCompoundDocument}}
        };

        private int _progressId;

        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <returns>The name of this provider (uses the constant declared at the top of the class)</returns>
        public string GetPackageProviderName() {
            return ProviderName;
        }

        /// <summary>
        ///     Performs one-time initialization of the PROVIDER.
        /// </summary>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void InitializeProvider(Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::InitializeProvider'", ProviderName);
        }

        /// <summary>
        ///     Returns a collection of strings to the client advertizing features this provider supports.
        /// </summary>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void GetFeatures(Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::GetFeatures' ", ProviderName);

            foreach (var feature in _features) {
                request.Yield(feature);
            }
        }

        /// <summary>
        ///     Returns dynamic option definitions to the HOST
        /// </summary>
        /// <param name="category">The category of dynamic options that the HOST is interested in</param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void GetDynamicOptions(string category, Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            category = category ?? string.Empty;

            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::GetDynamicOptions' '{1}'", ProviderName, category);

            switch (category.ToLowerInvariant()) {
                case "install":
                    // options required for install/uninstall/getinstalledpackages
                    request.YieldDynamicOption("AdditionalArguments", "StringArray", false);
                    break;

                case "provider":
                    // options used with this provider. Not currently used.
                    break;

                case "source":
                    // options for package sources
                    break;

                case "package":
                    // options used when searching for packages
                    break;
            }
        }

        /// <summary>
        ///     Finds packages given a locally-accessible filename
        ///     Package information must be returned using <c>request.YieldPackage(...)</c> function.
        /// </summary>
        /// <param name="file">the full path to the file to determine if it is a package</param>
        /// <param name="id">
        ///     if this is greater than zero (and the number should have been generated using <c>StartFind(...)</c>,
        ///     the core is calling this multiple times to do a batch search request. The operation can be delayed until
        ///     <c>CompleteFind(...)</c> is called
        /// </param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void FindPackageByFile(string file, int id, Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            if( string.IsNullOrWhiteSpace(file) ) {
                throw new ArgumentNullException("file");
            }

            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::FindPackageByFile' '{1}','{2}'", ProviderName, file, id);

            if (!file.FileExists()) {
                request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.ObjectNotFound, file, Constants.Messages.UnableToResolvePackage, file);
                return;
            }
            try {
                var package = new InstallPackage(file, DatabaseOpenMode.ReadOnly);
                YieldPackage(package, file, request);
                package.Close();
            } catch (Exception e) {
                e.Dump();
                // any exception at this point really just means that
                request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.OpenError, file, Constants.Messages.UnableToResolvePackage, file);
            }
        }

        /// <summary>
        /// Returns the packages that are installed
        /// </summary>
        /// <param name="name">the package name to match. Empty or null means match everything</param>
        /// <param name="requiredVersion">the specific version asked for. If this parameter is specified (ie, not null or empty string) then the minimum and maximum values are ignored</param>
        /// <param name="minimumVersion">the minimum version of packages to return . If the <code>requiredVersion</code> parameter is specified (ie, not null or empty string) this should be ignored</param>
        /// <param name="maximumVersion">the maximum version of packages to return . If the <code>requiredVersion</code> parameter is specified (ie, not null or empty string) this should be ignored</param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }

            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::GetInstalledPackages' '{1}','{2}','{3}','{4}'", ProviderName, name, requiredVersion, minimumVersion, maximumVersion);
            var products = ProductInstallation.AllProducts;
            WildcardPattern pattern = new WildcardPattern(name, WildcardOptions.IgnoreCase);
            var installed = string.IsNullOrWhiteSpace(name)
                ? products.Where(each => each.IsInstalled) : products.Where(each => each.IsInstalled && pattern.IsMatch(each.ProductName));

            if (!string.IsNullOrWhiteSpace(requiredVersion)) {
                // filter to just the exact version
                var rv = new Version(requiredVersion.FixVersion());
                installed = installed.Where(each => (FourPartVersion)each.ProductVersion == (FourPartVersion)rv);
            } else {
                if (!string.IsNullOrWhiteSpace(minimumVersion)) {
                    var min = new Version(minimumVersion.FixVersion());
                    installed = installed.Where(each => (FourPartVersion)each.ProductVersion >= (FourPartVersion)min);
                }
                if (!string.IsNullOrWhiteSpace(maximumVersion)) {
                    var max = new Version(maximumVersion.FixVersion());
                    installed = installed.Where(each => (FourPartVersion)each.ProductVersion <= (FourPartVersion)max);
                }
            }
            // make sure we don't enumerate more once
            installed = installed.ReEnumerable();

            // dump out results.
            if (installed.Any(p => !YieldPackage(p, name, request))) {
            }
        }

        /// <summary>
        ///     Installs a given package.
        /// </summary>
        /// <param name="fastPackageReference">A provider supplied identifier that specifies an exact package</param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void InstallPackage(string fastPackageReference, Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            if( string.IsNullOrWhiteSpace(fastPackageReference) ) {
                throw new ArgumentNullException("fastPackageReference");
            }
            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::InstallPackage' '{1}'", ProviderName, fastPackageReference);
            var file = fastPackageReference.CanonicalizePath(false);
            if (!file.FileExists()) {
                request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.OpenError, fastPackageReference, Constants.Messages.UnableToResolvePackage, fastPackageReference);
                return;
            }
            string errorLogFolder = Path.GetTempPath() + Guid.NewGuid();
            DirectoryInfo errorDir = Directory.CreateDirectory(errorLogFolder);
            string errorLogPath = errorLogFolder + "\\msi.log";       
            try {
                var package = new InstallPackage(file, DatabaseOpenMode.ReadOnly);

                Installer.SetInternalUI(InstallUIOptions.UacOnly | InstallUIOptions.Silent);

                // todo 1501: support additional parameters!

                if (request.Sources != null && request.Sources.Any()) {
                    // The 'file' can be from a temp location downloaded by a chained provider. In that case, we can show 
                    // the orignal package source specified in the request.Sources.
                    _progressId = request.StartProgress(0, Resources.Messages.InstallingMSIPackage, request.Sources.FirstOrDefault());

                } else {
                    _progressId = request.StartProgress(0, Resources.Messages.InstallingMSIPackage, file);
                }

                var handler = CreateProgressHandler(request, Resources.Messages.Installing);

                Installer.SetExternalUI(handler, InstallLogModes.Progress | InstallLogModes.Info);
                Installer.EnableLog(InstallLogModes.Error, errorLogPath);
                Installer.InstallProduct(file, "REBOOT=REALLYSUPPRESS");
                Installer.SetInternalUI(InstallUIOptions.Default);

                Installer.SetExternalUI(handler, InstallLogModes.None);

                if (request.Sources != null && request.Sources.Any()) {

                    // The 'file' can be from a temp location downloaded by a chained provider. In that case, we can show 
                    // the orignal package source specified in the request.Sources.
                    YieldPackage(package, request.Sources.FirstOrDefault(), request);
                } else {
                    YieldPackage(package, file, request);
                }

                package.Close();

                if (Installer.RebootRequired) {
                    request.Warning(Resources.Messages.InstallRequireReboot);
                }

                if (errorDir.Exists)
                    errorDir.Delete(true);
            } catch (Exception e) {
                e.Dump();
                request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.InvalidOperation, file, Constants.Messages.PackageFailedInstallErrorLog, file, errorLogPath);
            }

            request.CompleteProgress(_progressId, true);
        }

        /// <summary>
        ///     Uninstalls a package
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void UninstallPackage(string fastPackageReference, Request request) {
            if( request == null ) {
                throw new ArgumentNullException("request");
            }
            if( string.IsNullOrWhiteSpace(fastPackageReference) ) {
                throw new ArgumentNullException("fastPackageReference");
            }
            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::UninstallPackage' '{1}'", ProviderName, fastPackageReference);

            try {
                Guid guid;
                if (!Guid.TryParse(fastPackageReference, out guid)) {
                    request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage, fastPackageReference);
                    return;
                }
                var product = ProductInstallation.GetProducts(fastPackageReference, null, UserContexts.All).FirstOrDefault();
                if (product == null) {
                    request.Error(Microsoft.PackageManagement.Internal.ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage, fastPackageReference);
                    return;
                }
                var productVersion = product.ProductVersion.ToString();
                var productName = product.ProductName;
                var summary = product["Summary"];

                Installer.SetInternalUI(InstallUIOptions.UacOnly | InstallUIOptions.Silent);
                _progressId = request.StartProgress(0, Resources.Messages.UninstallingMSIPackage, productName);
                var handler = CreateProgressHandler(request, Resources.Messages.UnInstalling);

                Installer.SetExternalUI(handler, InstallLogModes.Progress | InstallLogModes.Info);
                Installer.InstallProduct(product.LocalPackage, "REMOVE=ALL REBOOT=REALLYSUPPRESS");
                Installer.SetInternalUI(InstallUIOptions.Default);

                Installer.SetExternalUI(handler, InstallLogModes.None);

                // YieldPackage(product,fastPackageReference, request);
                if (request.YieldSoftwareIdentity(fastPackageReference, productName, productVersion, "multipartnumeric", summary, "", fastPackageReference, "", "") != null) {
                    request.AddMetadata(fastPackageReference, "ProductCode", fastPackageReference);
                    request.AddTagId(fastPackageReference.Trim(new char[] { '{', '}' }));
                }


                request.Warning(Resources.Messages.UninstallRequireReboot);
            } catch (Exception e) {
                e.Dump();
            }
            request.CompleteProgress(_progressId, true);
            _progressId = 0;
        }

        private ExternalUIHandler CreateProgressHandler(Request request, string showMessage) {
            var currentTotalTicks = -1;
            var currentProgress = 0;
            var progressDirection = 1;
            var actualPercent = 0;

            ExternalUIHandler handler = (type, message, buttons, icon, button) => {
                if (request.IsCanceled) {
                    return MessageResult.Cancel;
                }

                switch (type) {
                    case InstallMessage.Progress:
                        if (message.Length >= 2) {
                            var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(m => m.ToInt32(0)).ToArray();

                            switch (msg[1]) {
                                // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                                case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                                    currentTotalTicks = msg[3];
                                    currentProgress = 0;
                                    if (msg.Length >= 6) {
                                        progressDirection = msg[5] == 0 ? 1 : -1;
                                    }
                                    break;
                                case 1:
                                    //Provides information related to progress messages to be sent by the current action.
                                    break;
                                case 2: //Increments the progress bar.
                                    if (currentTotalTicks == -1) {
                                        break;
                                    }
                                    currentProgress += msg[3]*progressDirection;
                                    break;
                                case 3:
                                    //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                                    break;
                            }
                        }

                        if (currentTotalTicks > 0) {
                            var newPercent = (currentProgress*100/currentTotalTicks);
                            if (actualPercent < newPercent) {
                                actualPercent = newPercent;
                                // request.Debug("Progress : {0}", newPercent);
                                request.Progress(_progressId, actualPercent, showMessage);
                            }
                        }
                        break;
                }

                return MessageResult.OK;
            };

            return handler;
        }

        private bool YieldPackage(InstallPackage package, string filename, Request request) {
            /*
                       var properties = package.ExecuteStringQuery("SELECT `Property` FROM `Property` ");
                       foreach (var i in properties) {
                           Debug.WriteLine("Property {0} = {1}", i, package.Property[i]);
                       }
                       */
            if (request.YieldSoftwareIdentity(filename, package.Property["ProductName"], package.Property["ProductVersion"], "multipartnumeric", package.Property["Summary"], filename, filename, filename, Path.GetFileName(filename)) != null) {
                var trusted = request.ProviderServices.IsSignedAndTrusted(filename, request);
                                
                if (request.AddMetadata(filename, "FromTrustedSource", trusted.ToString()) == null ) {
                    return false;
                }

                if (request.AddMetadata(filename, "ProductCode", package.Property["ProductCode"]) == null) {
                    return false;
                }

                if (request.AddTagId(package.Property["ProductCode"].Trim(new char[] { '{', '}' })) == null)
                {
                    return false;
                }

                if (request.AddMetadata(filename, "UpgradeCode", package.Property["UpgradeCode"]) == null) {
                    return false;
                }

                return true;
            }

            return false;
        }

        private bool YieldPackage(ProductInstallation package, string searchKey, Request request) {
            if (request.YieldSoftwareIdentity(package.ProductCode, package.ProductName, package.ProductVersion.ToString(), "multipartnumeric", package["Summary"], package.InstallLocation, searchKey, package.InstallLocation, "?") != null)
            {
                if (request.AddMetadata(package.ProductCode, "ProductCode", package.ProductCode) == null) {
                    return false;
                }

                if (request.AddTagId(package.ProductCode.Trim(new char[] {'{','}'})) == null)
                {
                    return false;
                }

                if (request.AddMetadata(package.ProductCode, "UpgradeCode", package["UpgradeCode"]) == null) {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
