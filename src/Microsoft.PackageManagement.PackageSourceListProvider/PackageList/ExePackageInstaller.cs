#if !UNIX

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

namespace Microsoft.PackageManagement.PackageSourceListProvider
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Security.AccessControl;
    using System.Threading;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Provider.Utility;
    using Microsoft.Win32;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;
    using SemanticVersion = Microsoft.PackageManagement.Provider.Utility.SemanticVersion;

    internal static class ExePackageInstaller
    {           
        internal static bool InstallExePackage(PackageJson package, string fastPath, PackageSourceListRequest request) {

            ProgressTracker tracker = new ProgressTracker(request.StartProgress(0, Resources.Messages.Installing));
         
            var exePackage = Path.ChangeExtension(Path.GetTempFileName(), "exe");
            WebDownloader.DownloadFile(package.Source, exePackage, request, tracker);

            if (File.Exists(exePackage)) {
                request.Verbose("Package: '{0}'", exePackage);

                // validate the file
                if (!WebDownloader.VerifyHash(exePackage,package, request))
                {                    
                    return false;
                }

                if (!package.IsTrustedSource)
                {
                    if (!request.ShouldContinueWithUntrustedPackageSource(package.Name, package.Source))
                    {
                        request.Warning(Constants.Messages.UserDeclinedUntrustedPackageInstall, package.Name);
                        return false;
                    }
                }

                // Prepare the process to run
                var processArguments = string.IsNullOrWhiteSpace(package.InstallArguments)
                    ? "/VERYSILENT /CLOSEAPPLICATIONS /NORESTART /NOCANCEL /SP /qn"
                    : package.InstallArguments;

                var start = new ProcessStartInfo {
                    FileName = exePackage,
                    Arguments = processArguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    //LoadUserProfile = true,
                };


                double percent = tracker.StartPercent;
                Timer timer = null;
                object timerLock = new object();
                bool cleanUp = false;

                Action cleanUpAction = () => {
                    lock (timerLock) {
                        // check whether clean up is already done before or not
                        if (!cleanUp) {
                            try {
                                if (timer != null) {
                                    // stop timer
                                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                                    // dispose it
                                    timer.Dispose();
                                }
                            } catch {
                            }

                            cleanUp = true;
                        }
                    }
                };

                // Run the external process & wait for it to finish
                using (var proc = Process.Start(start)) {
                   var timer1 = timer;
                    timer = new Timer(_ => {
                        percent += 0.025;

                        // percent between startProgress and endProgress
                        var progressPercent = tracker.ConvertPercentToProgress(percent);
                        if (progressPercent < 90) {
                            request.Progress(tracker.ProgressID, (int)progressPercent, Resources.Messages.InstallingPackage, package.Source);
                        }
                        if (request.IsCanceled) {
                            cleanUpAction();
                        }
                    }, null, 100, 3000);

                    proc.WaitForExit();

                    // Retrieve the app's exit code
                    var exitCode = proc.ExitCode;
                    if (exitCode != 0) {
                        request.WriteError(ErrorCategory.InvalidOperation, fastPath, Resources.Messages.InstallFailed, package.Name, proc.StandardError.ReadToEnd());
                        request.CompleteProgress(tracker.ProgressID, false);
                        return false;
                    }
                    else {
                        request.CompleteProgress(tracker.ProgressID, true);
                        request.YieldFromSwidtag(package, fastPath);
                        request.Verbose(Resources.Messages.SuccessfullyInstalled, package.Name);
                    }
                    cleanUpAction();
                }
                return true;
            }
            else
            {
                request.Error(ErrorCategory.InvalidOperation, Resources.Messages.FailedToDownload, Constants.ProviderName, package.Source, exePackage);
            }

            return false;
        }

        internal static void GetInstalledExePackages(PackageJson package, string requiredVersion, string minimumVersion, string maximumVersion, Request request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            request.Debug("Calling '{0}::GetInstalledPackages' '{1}','{2}','{3}','{4}'", Constants.ProviderName, package.Name, requiredVersion, minimumVersion, maximumVersion);

#if !CORECLR
            if (Environment.Is64BitOperatingSystem) {
                using (var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey)) {
#else
            if (System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64) {
                using (var hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")) {
#endif
                                                                                                                                                                                                                                                        if (!YieldPackages("hklm64", hklm64, package.Name, package.DisplayName, requiredVersion, minimumVersion, maximumVersion, package, request)) {
                        return;
                    }
                }

                using (var hkcu64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                    if (!YieldPackages("hkcu64", hkcu64, package.Name, package.DisplayName, requiredVersion, minimumVersion, maximumVersion, package, request)) {
                        return;
                    }
                }
            }

            using (var hklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                if (!YieldPackages("hklm32", hklm32, package.Name, package.DisplayName, requiredVersion, minimumVersion, maximumVersion, package, request)) {
                    return;
                }
            }

            using (var hkcu32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", false)) {
                if (!YieldPackages("hkcu32", hkcu32, package.Name, package.DisplayName, requiredVersion, minimumVersion, maximumVersion, package, request)) {
                }
            }
        }

        /// <summary>
        /// True if matched
        /// </summary>
        /// <param name="name">The package name from user's input</param>
        /// <param name="productName">The package name found from the system</param>
        /// <returns></returns>
        private static bool IsMatch(string name, string productName)
        {
            if ((name == null) || (productName == null))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (WildcardPattern.ContainsWildcardCharacters(name))
                {
                    var wildcardPattern = new WildcardPattern(name, PackageSourceListRequest.WildcardOptions);
                    return wildcardPattern.IsMatch(productName);
                }
                else
                {
                    // exactly match with the -name
                    return (productName.EqualsIgnoreCase(name));
                }
            }
            else
            {
                // Considered as matched if name is not specified
                return true;
            }
        }

        private static bool YieldPackages(string hive, RegistryKey regkey, string name, string displayname, string requiredVersion, string minimumVersion, string maximumVersion, PackageJson package, Request request)
        {
            //TODO make it wildcard match, follow the fastfrence format, get-package git no results, get-package git*

            if (regkey != null)
            {
                var includeSystemComponent = request.GetOptionValue("IncludeSystemComponent").IsTrue();

                foreach (var key in regkey.GetSubKeyNames())
                {
                    var subkey = regkey.OpenSubKey(key);
                    if (subkey != null)
                    {
                        var properties = subkey.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (subkey.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);

                        //if (!includeWindowsInstaller && properties.ContainsKey("WindowsInstaller") && properties["WindowsInstaller"] == "1")
                        //{
                        //    continue;
                        //}

                        if (!includeSystemComponent && properties.ContainsKey("SystemComponent") && properties["SystemComponent"] == "1")
                        {
                            continue;
                        }

                        var productName = "";

                        if (!properties.TryGetValue("DisplayName", out productName))
                        {
                            // no product name?
                            continue;
                        }

 
                        if (IsMatch(name, productName) || IsMatch(displayname, productName))
                        {
                            var productVersion = properties.Get("DisplayVersion") ?? "";
                            var publisher = properties.Get("Publisher") ?? "";
                            var uninstallString = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                            var comments = properties.Get("Comments") ?? "";

                            var fp = hive + @"\" + subkey;

                            if (!string.IsNullOrEmpty(requiredVersion))
                            {
                                if (new SemanticVersion(requiredVersion) != new SemanticVersion(productVersion))
                                {
                                    continue;
                                }
                            }
                            else {
                                if (!string.IsNullOrEmpty(minimumVersion) && new SemanticVersion(minimumVersion) > new SemanticVersion(productVersion))
                                {
                                    continue;
                                }
                                if (!string.IsNullOrEmpty(maximumVersion) && new SemanticVersion(maximumVersion) < new SemanticVersion(productVersion))
                                {
                                    continue;
                                }
                            }

                            fp = PackageSourceListRequest.MakeFastPathComplex(package.Destination ?? "", package.Name, package.DisplayName, productVersion, fp);
  
                            var source = properties.Get("InstallLocation") ?? "";
                            //we use name here because find-package uses name (not displayname) in the PSL.json, 

                            if (request.YieldSoftwareIdentity(fp, name, productVersion, "unknown", comments, source, name, "", "") != null)
                            {
                                if (properties.Keys.Where(each => !string.IsNullOrWhiteSpace(each)).Any(k => request.AddMetadata(fp, k.MakeSafeFileName(), properties[k]) == null))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
        private static bool YieldPackage(string path, string searchKey, Dictionary<string, string> properties, Request request)
        {
            var productName = properties.Get("DisplayName") ?? "";
            var productVersion = properties.Get("DisplayVersion") ?? "";
            var publisher = properties.Get("Publisher") ?? "";
            var uninstallString = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
            var comments = properties.Get("Comments") ?? "";
            var source = properties.Get("InstallLocation") ?? "";


            if (request.YieldSoftwareIdentity(path, productName, productVersion, "unknown", comments, source, searchKey, "", "") != null)
            {
                if (properties.Keys.Where(each => !string.IsNullOrWhiteSpace(each)).Any(k => request.AddMetadata(path, k.MakeSafeFileName(), properties[k]) == null))
                {
                    return false;
                }
            }
            return true;
        }
        internal static void UninstallExePackage(string fastPath, PackageSourceListRequest request)
        {
            if (string.IsNullOrWhiteSpace(fastPath)) {
                return;
            }
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            string sourceLocation;
            string id;
            string displayName;
            string version;
            string fastPackageReference;

            if (!request.TryParseFastPathComplex(fastPath: fastPath, regex: PackageSourceListRequest.RegexFastPathComplex, location: out sourceLocation, id: out id, displayname: out displayName, version: out version, fastpath: out fastPackageReference))
            {
                request.WriteError(ErrorCategory.InvalidData, fastPath, Resources.Messages.FailedToGetPackageObject, Constants.ProviderName, fastPath);
                return;
            }

            var ver = (new SemanticVersion(version)).ToString();
            var package =  request.GetPackage(id, ver) ?? request.GetPackage(displayName, ver);

            request.Debug("Calling '{0}::UninstallPackage' '{1}'", Constants.ProviderName, fastPackageReference);

            var path = fastPackageReference.Split(new[] { '\\' }, 3);
            var uninstallCommand = string.Empty;
            Dictionary<string, string> properties = null;
            if (path.Length == 3)
            {
                switch (path[0].ToLowerInvariant())
                {
                    case "hklm64":
#if !CORECLR
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
#else
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(path[2]))
#endif
                        {
                            if (product == null)
                            {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hkcu64":
#if !CORECLR
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
#else
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64).OpenSubKey(path[2]))
#endif
                        {
                            if (product == null)
                            {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hklm32":
#if !CORECLR
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
#else
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(path[2]))
#endif
                        {
                            if (product == null)
                            {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                    case "hkcu32":
#if !CORECLR
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(path[2], RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
#else
                        using (var product = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32).OpenSubKey(path[2]))
#endif
                        {
                            if (product == null)
                            {
                                return;
                            }
                            properties = product.GetValueNames().ToDictionaryNicely(each => each.ToString(), each => (product.GetValue(each) ?? string.Empty).ToString(), StringComparer.OrdinalIgnoreCase);
                            uninstallCommand = properties.Get("QuietUninstallString") ?? properties.Get("UninstallString") ?? "";
                        }
                        break;
                }

                if (properties == null)
                {
                    return;
                }

                var result = false;
                if (!string.IsNullOrWhiteSpace(uninstallCommand))
                {
                    do
                    {
                        if (File.Exists(uninstallCommand))
                        {
                            result = ExecuteUninstallCommand(fastPackageReference, request, uninstallCommand, package.UnInstallAdditionalArguments);
                            break;
                        }

                        // not a single file.
                        // check if it's just quoted.
                        var c = uninstallCommand.Trim('\"');
                        if (File.Exists(c))
                        {
                            result = ExecuteUninstallCommand(fastPackageReference, request, c, package.UnInstallAdditionalArguments);
                            break;
                        }
                     
                        if (uninstallCommand[0] == '"')
                        {
                            var p = uninstallCommand.IndexOf('"', 1);
                            if (p > 0)
                            {
                                var file = uninstallCommand.Substring(1, p - 1);
                                var args = uninstallCommand.Substring(p + 1);
                                if (File.Exists(file))
                                {
                                    args = string.Join(" ", args, package.UnInstallAdditionalArguments);
                                    result = ExecuteUninstallCommand(fastPackageReference, request, file, args);
                                }
                            }
                        }
                        else {
                            var p = uninstallCommand.IndexOf(' ');
                            if (p > 0)
                            {
                                var file = uninstallCommand.Substring(0, p);
                                var args = uninstallCommand.Substring(p + 1);
                                if (File.Exists(file))
                                {
                                    args = string.Join(" ", args, package.UnInstallAdditionalArguments);
                                    result = ExecuteUninstallCommand(fastPackageReference, request, file, args);
                                    continue;
                                }

                                var s = 0;
                                do
                                {
                                    s = uninstallCommand.IndexOf(' ', s + 1);
                                    if (s == -1)
                                    {
                                        break;
                                    }
                                    file = uninstallCommand.Substring(0, s);
                                    if (File.Exists(file))
                                    {
                                        args = uninstallCommand.Substring(s + 1);
                                        args = string.Join(" ", args, package.UnInstallAdditionalArguments);
                                        result = ExecuteUninstallCommand(fastPackageReference, request, file, args);
                                        break;
                                    }
                                } while (s > -1);

                                if (s == -1)
                                {
                                    // never found a way to parse the command :(
                                    request.WriteError(Internal.ErrorCategory.InvalidOperation, "DisplayName", properties["DisplayName"], Constants.Messages.UnableToUninstallPackage);
                                    return;
                                }
                            }
                        }
                    } while (false);


                    if (result)
                    {
                        YieldPackage(fastPackageReference, fastPackageReference, properties, request);
                        return;
                    }
                }
                request.WriteError(Internal.ErrorCategory.InvalidOperation, "DisplayName", properties["DisplayName"], Constants.Messages.UnableToUninstallPackage);
            }
        }

        private static bool ExecuteUninstallCommand(string fastPackageReference, Request request, string file, string args)
        {
            Timer timer = null;
            object timerLock = new object();
            bool cleanUp = false;

            ProgressTracker tracker = new ProgressTracker(request.StartProgress(0, Resources.Messages.Uninstalling));
            double percent = tracker.StartPercent;

            Action cleanUpAction = () => {
                lock (timerLock)
                {
                    // check whether clean up is already done before or not
                    if (!cleanUp)
                    {
                        try
                        {
                            if (timer != null)
                            {
                                // stop timer
                                timer.Change(Timeout.Infinite, Timeout.Infinite);
                                timer.Dispose();
                                timer = null;
                            }
                        }
                        catch
                        {
                        }

                        cleanUp = true;
                    }
                }
            };
            
            var start = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                //LoadUserProfile = true,
            };

           
            using (var proc = Process.Start(start))
            {
                // percent between startProgress and endProgress
                var progressPercent = tracker.ConvertPercentToProgress(percent += 0.01);

                request.Progress(tracker.ProgressID, (int)progressPercent, Resources.Messages.RunningCommand, file);

                if (proc == null)
                {
                    return false;
                }
              
                timer = new Timer(_ => {
                    percent += 0.025;
                   
                    if (progressPercent < 90)
                    {
                        request.Progress(tracker.ProgressID, (int) progressPercent, Resources.Messages.RunningCommand, file);
                    }
                    if (request.IsCanceled)
                    {
                        cleanUpAction();
                    }
                }, null, 0, 3000);


                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    request.Error(ErrorCategory.InvalidOperation, fastPackageReference, Resources.Messages.UninstallFailed, file, proc.StandardError.ReadToEnd());
                    request.CompleteProgress(tracker.ProgressID, false);

                    return false;
                }
                request.CompleteProgress(tracker.ProgressID, true);
                return true;
            }
        }

        internal static void DownloadExePackage(string fastPath, string location, PackageSourceListRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            // TODO do we need to support save-package for executable packages?
        }
    }
}

#endif