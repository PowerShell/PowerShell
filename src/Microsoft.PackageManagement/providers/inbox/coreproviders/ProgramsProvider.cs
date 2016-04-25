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

namespace Microsoft.PackageManagement.Providers.Internal {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.AccessControl;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Packaging;
    using PackageManagement.Internal.Utility.Platform;
    using PackageManagement.Internal.Utility.Extensions;
    using Win32;
    using File = System.IO.File;

    public class ProgramsProvider {
        /// <summary>
        ///     The name of this Package Provider
        /// </summary>
        internal const string ProviderName = "Programs";

        private static readonly Dictionary<string, string[]> _features = new Dictionary<string, string[]>();

        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <returns>The name of this proivder (uses the constant declared at the top of the class)</returns>
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
                    request.YieldDynamicOption("IncludeWindowsInstaller", "Switch", false);
                    request.YieldDynamicOption("IncludeSystemComponent", "Switch", false);
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

            // dump out results.

            if (Environment.Is64BitOperatingSystem) {
                using (var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
                    if (!YieldPackages("hklm64", hklm64, name,requiredVersion, minimumVersion,maximumVersion, request)) {
                        return;
                    }
                }

                using (var hkcu64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                    if (!YieldPackages("hkcu64", hkcu64, name, requiredVersion, minimumVersion, maximumVersion, request)) {
                        return;
                    }
                }
            }

            using (var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                if (!YieldPackages("hklm32", hklm32, name, requiredVersion, minimumVersion, maximumVersion, request)) {
                    return;
                }
            }

            using (var hkcu32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                if (!YieldPackages("hkcu32", hkcu32, name, requiredVersion, minimumVersion,maximumVersion, request)) {
                }
            }
        }


        private bool YieldPackages(string hive, RegistryKey regkey, string name,string requiredVersion, string minimumVersion, string maximumVersion, Request request) {
            if (regkey != null) {
                var includeWindowsInstaller = request.GetOptionValue("IncludeWindowsInstaller").IsTrue();
                var includeSystemComponent = request.GetOptionValue("IncludeSystemComponent").IsTrue();

                foreach (var key in regkey.GetSubKeyNames()) {
                    var subkey = regkey.OpenSubKey(key);
                    if (subkey != null) {
                        var properties = subkey.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (subkey.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);

                        if (!includeWindowsInstaller && properties.ContainsKey("WindowsInstaller") && properties["WindowsInstaller"] == "1") {
                            continue;
                        }

                        if (!includeSystemComponent && properties.ContainsKey("SystemComponent") && properties["SystemComponent"] == "1") {
                            continue;
                        }

                        var productName = "";

                        if (!properties.TryGetValue("DisplayName", out productName)) {
                            // no product name?
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(name) || productName.IndexOf(name, StringComparison.OrdinalIgnoreCase) > -1) {
                            var productVersion = properties.Get("DisplayVersion") ?? "";
                            var publisher = properties.Get("Publisher") ?? "";
                            var uninstallString = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                            var comments = properties.Get("Comments") ?? "";

                            var fp = hive + @"\" + subkey;

                            if (!string.IsNullOrEmpty(requiredVersion)) {
                                if (SoftwareIdentityVersionComparer.CompareVersions("unknown", requiredVersion, productVersion) != 0) {
                                    continue;
                                }
                            } else {
                                if (!string.IsNullOrEmpty(minimumVersion) && SoftwareIdentityVersionComparer.CompareVersions("unknown", productVersion, minimumVersion) < 0) {
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(maximumVersion) && SoftwareIdentityVersionComparer.CompareVersions("unknown", productVersion, maximumVersion) > 0) {
                                    continue;
                                }
                            }

                            if (request.YieldSoftwareIdentity(fp, productName, productVersion, "unknown", comments, "", name, "", "") != null) {
                                if (properties.Keys.Where(each => !string.IsNullOrWhiteSpace(each)).Any(k => request.AddMetadata(fp, k.MakeSafeFileName(), properties[k]) == null)) {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool YieldPackage(string path, string searchKey, Dictionary<string, string> properties, Request request) {
            var productName = properties.Get("DisplayName") ?? "";
            var productVersion = properties.Get("DisplayVersion") ?? "";
            var publisher = properties.Get("Publisher") ?? "";
            var uninstallString = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
            var comments = properties.Get("Comments") ?? "";

            if (request.YieldSoftwareIdentity(path, productName, productVersion, "unknown", comments, "", searchKey, "", "") != null) {
                if (properties.Keys.Where(each => !string.IsNullOrWhiteSpace(each)).Any(k => request.AddMetadata(path, k.MakeSafeFileName(), properties[k]) == null)) {
                    return false;
                }
            }
            return true;
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
            if( string.IsNullOrWhiteSpace( fastPackageReference ) ) {
              return;
            }

            if( request == null ) {
                throw new ArgumentNullException("request");
            }


            request.Debug("Calling '{0}::UninstallPackage' '{1}'", ProviderName, fastPackageReference);
            // Nice-to-have put a debug message in that tells what's going on.

            var path = fastPackageReference.Split(new[] {'\\'}, 3);
            var uninstallCommand = string.Empty;
            Dictionary<string, string> properties = null;
            if (path.Length == 3) {
                switch (path[0].ToLowerInvariant()) {
                    case "hklm64":
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
                            if (product == null) {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hkcu64":
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
                            if (product == null) {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hklm32":
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
                            if (product == null) {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hkcu32":
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
                            if (product == null) {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                }

                if (properties == null) {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(uninstallCommand)) {
                    do {
                        if (File.Exists(uninstallCommand)) {
                            ExecStandalone(request, uninstallCommand);
                            break;
                        }

                        // not a single file.
                        // check if it's just quoted.
                        var c = uninstallCommand.Trim('\"');
                        if (File.Exists(c)) {
                            ExecStandalone(request, c);
                            break;
                        }

                        if (uninstallCommand.IndexOf("msiexec", StringComparison.OrdinalIgnoreCase) > -1) {
                            MsiUninstall(request, uninstallCommand);
                            break;
                        }

                        if (uninstallCommand.IndexOf("rundll32", StringComparison.OrdinalIgnoreCase) > -1) {
                            RunDll32(request, uninstallCommand);
                            break;
                        }

                        if (uninstallCommand.IndexOf("cmd.exe", StringComparison.OrdinalIgnoreCase) == 0) {
                            CmdCommand(request, uninstallCommand);
                            continue;
                        }

                        if (uninstallCommand.IndexOf("cmd ", StringComparison.OrdinalIgnoreCase) == 0) {
                            CmdCommand(request, uninstallCommand);
                            continue;
                        }

                        if (uninstallCommand[0] == '"') {
                            var p = uninstallCommand.IndexOf('"', 1);
                            if (p > 0) {
                                var file = uninstallCommand.Substring(1, p - 1);
                                var args = uninstallCommand.Substring(p + 1);
                                if (File.Exists(file)) {
                                    CommandWithParameters(request, file, args);
                                }
                            }
                        } else {
                            var p = uninstallCommand.IndexOf(' ');
                            if (p > 0) {
                                var file = uninstallCommand.Substring(0, p);
                                var args = uninstallCommand.Substring(p + 1);
                                if (File.Exists(file)) {
                                    CommandWithParameters(request, file, args);
                                    continue;
                                }

                                var s = 0;
                                do {
                                    s = uninstallCommand.IndexOf(' ', s + 1);
                                    if (s == -1) {
                                        break;
                                    }
                                    file = uninstallCommand.Substring(0, s);
                                    if (File.Exists(file)) {
                                        args = uninstallCommand.Substring(s + 1);
                                        CommandWithParameters(request, file, args);
                                        break;
                                    }
                                } while (s > -1);

                                if (s == -1) {
                                    // never found a way to parse the command :(
                                    request.Error(ErrorCategory.InvalidOperation, properties["DisplayName"], Constants.Messages.UnableToUninstallPackage);
                                    return;
                                }
                            }
                        }
                    } while (false);
                    YieldPackage(fastPackageReference, fastPackageReference, properties, request);
                    return;
                }
                request.Error(ErrorCategory.InvalidOperation, properties["DisplayName"], Constants.Messages.UnableToUninstallPackage);

            }
        }

        private void RunDll32(Request request,string uninstallCommand) {

        }

        private void MsiUninstall(Request request, string uninstallCommand) {
        }

        private void CommandWithParameters(Request request, string file, string args) {
        }

        private void CmdCommand(Request request, string args) {
        }

        private void ExecStandalone(Request request, string uninstallCommand) {
            // we could examine the EXE a bit here to see if it's a NSIS installer and if it is, tack on a /S to get it to go silently.

            // uninstall via standalone EXE
            var proc = AsyncProcess.Start(new ProcessStartInfo {
                FileName = uninstallCommand,
            });

            proc.WaitForExit();
        }
    }
}
