using Microsoft.PackageManagement.Provider.Utility;

namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Management.Automation;

    using SemanticVersion = Microsoft.PackageManagement.Provider.Utility.SemanticVersion;

    /// <summary>
    /// A Package provider to the PackageManagement Platform.
    ///
    /// Important notes:
    ///    - Required Methods: Not all methods are required; some package providers do not support some features. If the methods isn't used or implemented it should be removed (or commented out)
    ///    - Error Handling: Avoid throwing exceptions from these methods. To properly return errors to the user, use the request.Error(...) method to notify the user of an error condition and then return.
    ///    - Communicating with the HOST and CORE: each method takes a Request (in reality, an alias for System.Object), which can be used in one of two ways:
    ///         - use the c# 'dynamic' keyword, and call functions on the object directly.
    ///         - use the <code><![CDATA[ .As<Request>() ]]></code> extension method to strongly-type it to the Request type (which calls upon the duck-typer to generate a strongly-typed wrapper). 
    ///           The strongly-typed wrapper also implements several helper functions to make using the request object easier.
    ///
    /// </summary>
    public class NuGetProvider
    { 
        /// <summary>
        /// The features that this package supports.
        /// </summary>
        internal static readonly Dictionary<string, string[]> Features = new Dictionary<string, string[]> {

            //Required by PowerShellGet provider
            { Constants.Features.SupportsPowerShellModules, Constants.FeaturePresent },

            // specify the extensions that your provider uses for its package files (if you have any)
            { Constants.Features.SupportedExtensions, new[]{"nupkg"}},

            // you can list the URL schemes that you support searching for packages with
            { Constants.Features.SupportedSchemes, new []{"http", "https", "file"}},

            // you can list the magic signatures (bytes at the beginning of a file) that we can use
            // to peek and see if a given file is yours.
            { Constants.Features.MagicSignatures, new[]{Constants.Signatures.Zip}},

        };

        /// <summary>
        /// Returns the name of the Provider.
        /// </summary>
        /// <returns>The name of this provider </returns>
        public string PackageProviderName {
            get { return NuGetConstant.ProviderName; }
        }

        /// <summary>
        /// Returns the version of the Provider.
        /// </summary>
        /// <returns>The version of this provider </returns>
        public string ProviderVersion {

            get{return NuGetConstant.ProviderVersion;}
        }

        /// <summary>
        /// This is just here as to give us some possibility of knowing when an exception happens...
        /// At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
        /// </summary>
        public void OnUnhandledException(string methodName, Exception exception) {
            if (exception == null){
                throw new ArgumentNullException("exception");
            }

            if (!string.IsNullOrWhiteSpace(methodName))
            {
                Debug.WriteLine("Unexpected Exception thrown in '{0}::{1}' -- {2}\\{3}\r\n{4}", PackageProviderName, methodName, "OnUnhandledException", exception.Message, exception.StackTrace);
            } else {
                Debug.WriteLine("Unexpected Exception thrown in '{0}' -- {1}\\{2}\r\n{3}", PackageProviderName, "OnUnhandledException", exception.Message, exception.StackTrace);
            }
        }

        /// <summary>
        /// Performs one-time initialization of the $provider.
        /// </summary>
        /// <param name="request">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void InitializeProvider(NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "InitializeProvider");
        }            

        /// <summary>
        /// Returns a collection of strings to the client advertizing features this provider supports.
        /// </summary>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void GetFeatures(Request request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "GetFeatures");

            request.Yield(Features);
        }

        /// <summary>
        /// Returns dynamic option definitions to the HOST
        ///
        /// example response:
        ///     request.YieldDynamicOption( "MySwitch", OptionType.String.ToString(), false);
        ///
        /// </summary>
        /// <param name="category">The category of dynamic options that the HOST is interested in</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void GetDynamicOptions(string category, Request request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            if (category == null)
            {
                request.Warning(Resources.Messages.UnkownCategory, PackageProviderName, "GetDynamicOptions", category);
                return;
            }

            switch ((category ?? string.Empty).ToLowerInvariant()) {
                case "install":
                    // put any options required for install/uninstall/getinstalledpackages

                    request.YieldDynamicOption("Destination", Constants.OptionType.Folder, false);
                    request.YieldDynamicOption("ExcludeVersion", Constants.OptionType.Switch, false);
                    request.YieldDynamicOption("Scope", Constants.OptionType.String, false, new[] { Constants.CurrentUser, Constants.AllUsers });
                    //request.YieldDynamicOption("SkipDependencies", Constants.OptionType.Switch, false);
                    //request.YieldDynamicOption("ContinueOnFailure", Constants.OptionType.Switch, false);
                    break;

                case "source":
                    // put any options for package sources
                    request.YieldDynamicOption("ConfigFile", Constants.OptionType.String, false);
                    request.YieldDynamicOption("SkipValidate", Constants.OptionType.Switch, false);
                    break;

                case "package":
                    // put any options used when searching for packages
                    request.YieldDynamicOption("Headers", Constants.OptionType.StringArray, false);
                    request.YieldDynamicOption("FilterOnTag", Constants.OptionType.StringArray, false);
                    request.YieldDynamicOption("Contains", Constants.OptionType.String, false);
                    request.YieldDynamicOption("AllowPrereleaseVersions", Constants.OptionType.Switch, false);
                    break;
                default:
                    request.Warning(Resources.Messages.UnkownCategory, PackageProviderName, "GetDynamicOptions", category);
                    break;
            }
        }

        /// <summary>
        /// Resolves and returns Package Sources to the client.
        ///
        /// Specified sources are passed in via the request object (<c>request.GetSources()</c>).
        ///
        /// Sources are returned using <c>request.YieldPackageSource(...)</c>
        /// </summary>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void ResolvePackageSources(NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "ResolvePackageSources");

            var selectedSources = request.SelectedSources;

            try
            {
                foreach (var source in selectedSources)
                {
                    request.Debug(Resources.Messages.YieldingPackageSource, PackageProviderName, source);
                    request.YieldPackageSource(source.Name, source.Location, source.Trusted, source.IsRegistered, source.IsValidated);
                }
            }
            catch (Exception e)
            {
                e.Dump(request);
            }

            request.Debug(Resources.Messages.DebugInfoReturnCall, PackageProviderName,  "ResolvePackageSources");
        }
            
        /// <summary>
        /// This is called when the user is adding (or updating) a package source
        ///
        /// If this PROVIDER doesn't support user-defined package sources, remove this method.
        /// </summary>
        /// <param name="name">The name of the package source. If this parameter is null or empty the PROVIDER should use the location as the name (if the PROVIDER actually stores names of package sources)</param>
        /// <param name="location">The location (ie, directory, URL, etc) of the package source. If this is null or empty, the PROVIDER should use the name as the location (if valid)</param>
        /// <param name="trusted">A boolean indicating that the user trusts this package source. Packages returned from this source should be marked as 'trusted'</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void AddPackageSource(string name, string location, bool trusted, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }
            try {
                request.Debug(string.Format(CultureInfo.InvariantCulture, "AddPackageSource - ProviderName = '{0}', name='{1}', location='{2}', trusted='{3}'", PackageProviderName, name, location, trusted));

                // Error out if a user does not provide package source Name
                if (string.IsNullOrWhiteSpace(name))
                {
                    request.WriteError(ErrorCategory.InvalidArgument, Constants.Parameters.Name, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Name);
                    return;
                }

                if (string.IsNullOrWhiteSpace(location))
                {
                    request.WriteError(ErrorCategory.InvalidArgument, Constants.Parameters.Location, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Location);
                    return;
                }

                request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "GetOptionValue");
                // if this is supposed to be an update, there will be a dynamic parameter set for IsUpdatePackageSource
                var isUpdate = request.GetOptionValue(Constants.Parameters.IsUpdate).IsTrue();

                request.Debug(Resources.Messages.VariableCheck, "IsUpdate", isUpdate);

                // if your source supports credentials you get get them too:
                // string username =request.Username;
                // SecureString password = request.Password;
                // feel free to send back an error here if your provider requires credentials for package sources.

                // check first that we're not clobbering an existing source, unless this is an update

                request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "FindRegisteredSource -name'{0}'", name));

                var src = request.FindRegisteredSource(name);

                if (src != null && !isUpdate)
                {
                    // tell the user that there's one here already
                    request.WriteError(ErrorCategory.InvalidArgument, name, Constants.Messages.PackageSourceExists, name);
                    return;
                }

                // conversely, if it didn't find one, and it is an update, that's bad too:
                if (src == null && isUpdate)
                {
                    // you can't find that package source? Tell that to the user
                    request.WriteError(ErrorCategory.ObjectNotFound, name, Constants.Messages.UnableToResolveSource, name);
                    return;
                }

                // ok, we know that we're ok to save this source
                // next we check if the location is valid (if we support that kind of thing)

                var validated = false;

                if (!request.SkipValidate.Value)
                {
                    // the user has not opted to skip validating the package source location, so check if the source is valid
                    validated = request.ValidateSourceLocation(location);

                    if (!validated)
                    {
                        request.WriteError(ErrorCategory.InvalidData, name, Constants.Messages.SourceLocationNotValid, location);
                        return;
                    }

                    request.Verbose(Resources.Messages.SuccessfullyValidated, name);
                }

                // it's good to check just before you actually write something to see if the user has cancelled the operation
                if (request.IsCanceled)
                {
                    return;
                }

                // looking good -- store the package source.
                request.AddPackageSource(name, location, trusted, validated);

                // Yield the package source back to the caller.
                request.YieldPackageSource(name, location, trusted, true /*since we just registered it*/, validated);
            }
            catch (Exception e)
            {
                e.Dump(request);
            }
        }

        /// <summary>
        /// Removes/Unregisters a package source
        /// </summary>
        /// <param name="name">The name or location of a package source to remove.</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void RemovePackageSource(string name, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }
            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, "RemovePackageSource");

            var src = request.FindRegisteredSource(name);
            if (src == null)
            {
                request.Warning(Constants.Messages.UnableToResolveSource, name);
                return;
            }

            request.RemovePackageSource(src.Name);
            request.YieldPackageSource(src.Name, src.Location, src.Trusted, false, src.IsValidated);
        }

        /// <summary>
        /// Searches package sources given name and version information
        ///
        /// Package information must be returned using <c>request.YieldPackage(...)</c> function.
        /// </summary>
        /// <param name="name">a name or partial name of the package(s) requested</param>
        /// <param name="requiredVersion">A specific version of the package. Null or empty if the user did not specify</param>
        /// <param name="minimumVersion">A minimum version of the package. Null or empty if the user did not specify</param>
        /// <param name="maximumVersion">A maximum version of the package. Null or empty if the user did not specify</param>
        /// <param name="id">if this is greater than zero (and the number should have been generated using <c>StartFind(...)</c>, the core is calling this multiple times to do a batch search request. The operation can be delayed until <c>CompleteFind(...)</c> is called</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            // true if we want to include the max and min version
            bool minInclusive = true;
            bool maxInclusive = true;

            // If finding by canonical id, then the version follows dependency version requirement
            if (request.GetOptionValue("FindByCanonicalId").IsTrue())
            {
                // Use the dependency version if no min and max is supplied
                if (String.IsNullOrWhiteSpace(maximumVersion) && String.IsNullOrWhiteSpace(minimumVersion))
                {
                    DependencyVersion depVers = DependencyVersion.ParseDependencyVersion(requiredVersion);
                    maximumVersion = depVers.MaxVersion.ToStringSafe();
                    minimumVersion = depVers.MinVersion.ToStringSafe();
                    minInclusive = depVers.IsMinInclusive;
                    maxInclusive = depVers.IsMaxInclusive;

                    // set required version if we have both min max as the same value.
                    if (depVers.MaxVersion != null && depVers.MinVersion != null
                        && depVers.MaxVersion == depVers.MinVersion && minInclusive && maxInclusive)
                    {
                        requiredVersion = maximumVersion;
                    }
                    else
                    {
                        requiredVersion = null;
                    }
                }                
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "FindPackage' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", name, requiredVersion, minimumVersion, maximumVersion));

            NormalizeVersion(request, ref requiredVersion, ref minimumVersion, ref maximumVersion);

            try {

            
            // If there are any packages, yield and return
            if (request.YieldPackages(request.GetPackageById(name, request, requiredVersion, minimumVersion, maximumVersion, minInclusive, maxInclusive), name))
            {
                return;
            }

            // Check if the name contains wildcards. If not, return. This matches the behavior as "Get-module xje" 
            if (!String.IsNullOrWhiteSpace(name) && !WildcardPattern.ContainsWildcardCharacters(name))
            {              
                return;
            }

            // In the case of the package name is null or contains wildcards, error out if a user puts version info
            if (!String.IsNullOrWhiteSpace(requiredVersion) || !String.IsNullOrWhiteSpace(minimumVersion) || !String.IsNullOrWhiteSpace(maximumVersion))
            {
                request.Warning( Constants.Messages.MissingRequiredParameter, "name");
                return;
            }
            
            
       
            // Have we been cancelled?
            if (request.IsCanceled) {
                request.Debug(Resources.Messages.RequestCanceled, PackageProviderName, "FindPackage");

                return;
            }

            // A user does not provide the package full Name at all Or used wildcard in the name. Let's try searching the entire repository for matches.
            request.YieldPackages(request.SearchForPackages(name), name);
            }
            catch (Exception ex)
            {
                ex.Dump(request);
            }
        }

        /// <summary>
        /// Finds packages given a locally-accessible filename
        ///
        /// Package information must be returned using <c>request.YieldPackage(...)</c> function.
        /// </summary>
        /// <param name="file">the full path to the file to determine if it is a package</param>
        /// <param name="id">if this is greater than zero (and the number should have been generated using <c>StartFind(...)</c>, the core is calling this multiple times to do a batch search request. The operation can be delayed until <c>CompleteFind(...)</c> is called</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void FindPackageByFile(string file, int id, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod3, PackageProviderName, file, id);

            var pkgItem = request.GetPackageByFilePath(Path.GetFullPath(file));
            if (pkgItem != null)
            {
                request.YieldPackage(pkgItem, file);
            }
        }
  
        /// <summary>
        /// Downloads a remote package file to a local location.
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="destLocation"></param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void DownloadPackage(string fastPackageReference, string destLocation, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod3, PackageProviderName, fastPackageReference, destLocation);

            try {
                var pkgItem = request.GetPackageByFastpath(fastPackageReference);
                if (pkgItem == null) {
                    request.WriteError(ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage);
                    return;
                }

                NuGetClient.InstallOrDownloadPackageHelper(pkgItem, request, Constants.Download,
                    (packageItem, progressTracker) => NuGetClient.DownloadSinglePackage(packageItem, request, destLocation, progressTracker));
            } catch (Exception ex) {
                ex.Dump(request);
                request.WriteError(ErrorCategory.InvalidOperation, fastPackageReference, ex.Message);
            }
        }
        
        /// <summary>
        /// Installs a given package.
        /// </summary>
        /// <param name="fastPackageReference">A provider supplied identifier that specifies an exact package</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void InstallPackage(string fastPackageReference, NuGetRequest request) 
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod3, PackageProviderName, "InstallPackage", fastPackageReference);

            try {

                var pkgItem = request.GetPackageByFastpath(fastPackageReference);
                if (pkgItem == null) {
                    request.WriteError(ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage);
                    return;
                }

                if ((pkgItem.PackageSource == null) || (pkgItem.PackageSource.Location == null) || (pkgItem.Package == null)) {
                    request.Debug(Resources.Messages.VariableCheck, "PackageSource or PackageSource.Location or Package object", "null");
                    request.WriteError(ErrorCategory.ObjectNotFound, fastPackageReference, Constants.Messages.UnableToResolvePackage, pkgItem.Id);
                    return;
                }

                request.Debug(Resources.Messages.VariableCheck, "Package version", pkgItem.Version);
                request.Debug(Resources.Messages.VariableCheck, "Request's Destination", request.Destination);

                // got this far, let's install the package we came here for.
                if (!NuGetClient.InstallOrDownloadPackageHelper(pkgItem, request, Constants.Install,
                    (packageItem, progressTracker) => NuGetClient.InstallSinglePackage(packageItem, request, progressTracker)))
                {
                    // package itself didn't install. Write error
                    request.WriteError(ErrorCategory.InvalidResult, pkgItem.Id, Constants.Messages.PackageFailedInstallOrDownload, pkgItem.Id, CultureInfo.CurrentCulture.TextInfo.ToLower(Constants.Install));

                    return;
                }
            } catch (Exception ex) {
                ex.Dump(request);
                request.WriteError(ErrorCategory.InvalidOperation, fastPackageReference, ex.Message);
            }
        }

        /// <summary>
        /// Uninstalls a package
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void UninstallPackage(string fastPackageReference, NuGetRequest request)
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod3, PackageProviderName, "UninstallPackage", fastPackageReference);

            var pkg = request.GetPackageByFastpath(fastPackageReference);

            NuGetClient.UninstallPackage(request, pkg);
        }

        /// <summary>
        /// Returns the packages that are installed. This method is called when a user type get-package, install-package and uninstall-package.
        /// </summary>
        /// <param name="name">the package name to match. Empty or null means match everything</param>
        /// <param name="requiredVersion">the specific version asked for. If this parameter is specified (ie, not null or empty string) then the minimum and maximum values are ignored</param>
        /// <param name="minimumVersion">the minimum version of packages to return . If the <code>requiredVersion</code> parameter is specified (ie, not null or empty string) this should be ignored</param>
        /// <param name="maximumVersion">the maximum version of packages to return . If the <code>requiredVersion</code> parameter is specified (ie, not null or empty string) this should be ignored</param>
        /// <param name="request">An object passed in from the PackageManagement that contains functions that can be used to interact with its Provider</param> 
        public void GetInstalledPackages(string name, string requiredVersion, string minimumVersion, string maximumVersion, NuGetRequest request) 
        {
            if (request == null){
                throw new ArgumentNullException("request");
            }

            request.Debug(Resources.Messages.DebugInfoCallMethod, PackageProviderName, string.Format(CultureInfo.InvariantCulture, "GetInstalledPackages' - name='{0}', requiredVersion='{1}',minimumVersion='{2}', maximumVersion='{3}'", name, requiredVersion, minimumVersion, maximumVersion));

            // In the case of the package name is null or contains wildcards, error out if a user puts version info
            if (!String.IsNullOrWhiteSpace(requiredVersion) || !String.IsNullOrWhiteSpace(minimumVersion) || !String.IsNullOrWhiteSpace(maximumVersion))
            {
                // A user provides the version info but missing name
                if (string.IsNullOrWhiteSpace(name))
                {
                    request.Warning(Constants.Messages.MissingRequiredParameter, "name");
                    return;
                }

                // A user provides the version info but the name containing wildcards
                if (WildcardPattern.ContainsWildcardCharacters(name)) {
                    return;
                }
            }

            NormalizeVersion(request, ref requiredVersion, ref minimumVersion, ref maximumVersion);

            request.GetInstalledPackages(name, requiredVersion, minimumVersion, maximumVersion);
        }

        private static void NormalizeVersion(NuGetRequest request, ref string requiredVersion, ref string minimumVersion, ref string maximumVersion) {
            if (!string.IsNullOrWhiteSpace(requiredVersion))
            {
                requiredVersion = requiredVersion.FixVersion();
                minimumVersion = null;
                maximumVersion = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(minimumVersion))
                {
                    minimumVersion = minimumVersion.FixVersion();
                }

                if (!string.IsNullOrWhiteSpace(maximumVersion))
                {
                    maximumVersion = maximumVersion.FixVersion();
                }
            }

            if (!string.IsNullOrWhiteSpace(minimumVersion) && !string.IsNullOrWhiteSpace(maximumVersion))
            {
                if(new SemanticVersion(minimumVersion) > new SemanticVersion(maximumVersion))
                {
                    request.Warning("Specified version range is invalid. minimumVersion = {0} maximumVersion ={1}", minimumVersion, maximumVersion);
                }
            }
        }

        /// <summary>
        /// True, if the package matches.
        /// </summary>
        /// <param name="packageName">Package name</param>
        /// <param name="filePath">Package file Path</param>
        /// <returns></returns>
        internal static bool IsNameMatch(string packageName, string filePath) 
        {
            // Get the file name
            var newName = Path.GetFileNameWithoutExtension(filePath);

            // Strip off the version info from the file name, e.g., Jquery.2.1.3
            newName = PackageUtility.GetPackageNameWithoutVersionInfo(newName);

            if (WildcardPattern.ContainsWildcardCharacters(packageName))
            {
                // Applying the wildcard pattern matching
                const WildcardOptions wildcardOptions = WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase;
                var wildcardPattern = new WildcardPattern(packageName, wildcardOptions);

                return wildcardPattern.IsMatch(newName);

            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                return false;
            }

            var reval = (newName.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            return reval;
        }
    }
}
