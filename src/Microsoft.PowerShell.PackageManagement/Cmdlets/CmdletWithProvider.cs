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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Implementation;
    using Microsoft.PackageManagement.Internal.Implementation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Collections;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Utility;
    using Microsoft.PackageManagement.Packaging;

    public abstract class CmdletWithProvider : CmdletBase {
        private readonly OptionCategory[] _optionCategories;
        protected CmdletWithProvider(OptionCategory[] categories) {
            _optionCategories = categories;
        }

        private string[] _providerName;
        private bool _initializedTypeName;
        private string _softwareIdentityTypeName = "Microsoft.PackageManagement.Packaging.SoftwareIdentity";
        private bool _isDisplayCulture;
        private bool _initializedCulture;
        private bool _isUserSpecifyOneProviderName;
        private bool _hasTypeNameChanged;
        private bool _useDefaultSourceFormat = true;
        private bool _initializedSource;
        private IEnumerable<PackageSource> _resolvedUserSpecifiedSource;

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Used in a powershell parameter.")]
        public string[] ProviderName {
            get {
                if (!_providerName.IsNullOrEmpty()) {
                    //meaning a user specifies -ProviderName, we will use it
                    return _providerName;
                }
                //need to call it so that cases like get-packagesource | find-package -name Jquery will work 
                return GetDynamicParameterValue<string[]>("ProviderName");
            }
            set {
                _providerName = value;
            }
        }

        protected bool IsFailingEarly;
        protected Dictionary<string, string> UserSpecifiedSourcesList = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected virtual IEnumerable<PackageProvider> SelectedProviders {
            get {

                var didUserSpecifyProviders = !ProviderName.IsNullOrEmpty();
                var registeredSources = Enumerable.Empty<PackageSource>();


                // filter on provider names  - if they specify a provider name, narrow to only those provider names.
                // if this is an actual invocation, this will attempt to bootstrap a provider that the user specified
                // (which will require a prompt or -force or -forcebootstrap )
                var providers = SelectProviders(ProviderName).ReEnumerable();

                if (!providers.Any()) {
                    // the user gave us provider names that we're not able to resolve.

                    if (IsInvocation) {
                        // and we're in an actual cmdlet invocation.
                        QueueHeldMessage(() => Error(Constants.Errors.UnknownProviders, ProviderName.JoinWithComma()));
                        IsFailingEarly = true;
                    }
                    // return the empty collection, for all the good it's doing.
                    return providers;
                }

                // fyi, re-enumerable insulates us against null sets.
                var userSpecifiedSources = Sources.ReEnumerable().ToArray();
                var didUserSpecifySources = userSpecifiedSources.Any();

                // filter out providers that don't have the sources that have been specified (only if we have specified a source!)
                if (didUserSpecifySources) {
                    // sources must actually match a name or location. Keeps providers from being a bit dishonest

                    var potentialSources = providers.SelectMany(each => each.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing))
                        .Where(source => userSpecifiedSources.Any(
                            // check whether location of the resolved source contains the userspecified source
                            userSpecifiedSource => source.Location.EqualsIgnoreEndSlash(userSpecifiedSource)
                            // or the name equals to the name provided by the user
                            || source.Name.EqualsIgnoreCase(userSpecifiedSource)))).ReEnumerable();

                    // save the resolved package source name and location
                    potentialSources.SerialForEach(src => UserSpecifiedSourcesList.AddOrSet(src.Name, src.Location));
                
                    // prefer registered sources
                    registeredSources = potentialSources.Where(source => source.IsRegistered).ReEnumerable();

                    _resolvedUserSpecifiedSource = registeredSources.Any() ? registeredSources : potentialSources;
                    var filteredproviders = registeredSources.Any() ? registeredSources.Select(source => source.Provider).Distinct().ReEnumerable() : potentialSources.Select(source => source.Provider).Distinct().ReEnumerable();

                    if (!filteredproviders.Any()) {
                        // we've filtered out everything!

                        if (!didUserSpecifyProviders) {
                            // if cmdlet is generating parameter, we have to log error that source is wrong
                            if (IsInvocation && CmdletState >= AsyncCmdletState.GenerateParameters ) {
                                
                                // user didn't actually specify provider(s), the sources can't be tied to any particular provider
                                QueueHeldMessage(() => Error(Constants.Errors.SourceNotFound, userSpecifiedSources.JoinWithComma()));
                                IsFailingEarly = true;
                            }
                            // return the empty set.
                            return filteredproviders;
                        }

                        // they gave us both provider name(s) and source(s)
                        // and the source(s) aren't found in the providers they listed
                        // we should log the correct error about the source. if not we will get wrong error about dynamic parameters.
                        if (IsInvocation &&  CmdletState >= AsyncCmdletState.GenerateParameters ) {
                            if (providers.Count() < 2) {
                                QueueHeldMessage(() => Error(Constants.Errors.SourceNotFound, userSpecifiedSources.JoinWithComma()));
                                IsFailingEarly = true;
                            } else {
                                var providerNames = providers.Select(each => each.Name).JoinWithComma();
                                QueueHeldMessage(() => Error(Constants.Errors.NoMatchForProvidersAndSources, providerNames, userSpecifiedSources.JoinWithComma()));
                                IsFailingEarly = true;
                            }
                        }

                        return filteredproviders;
                    }

                    // make this the new subset.
                    providers = filteredproviders;
                }


                // filter on: dynamic options - if they specify any dynamic options, limit the provider set to providers with those options.
                var result = FilterProvidersUsingDynamicParameters(providers, registeredSources, didUserSpecifyProviders, didUserSpecifySources).ToArray();

                /* todo : return error messages when dynamic parameters filter everything out. Either here or in the FPUDP fn.

                if (!result.Any()) {
                    // they specified dynamic parameters that implicitly select providers
                    // that don't fit with the providers and sources that they initially asked for.

                    if (didUserSpecifyProviders) {

                        if (didUserSpecifySources) {
                            // user said provider and source and the dynamic parameters imply providers they didn't select
                            if (IsInvocation) {
                                QueueHeldMessage(() => Error(Errors.DynamicParameters, providerNames, userSpecifiedSources.JoinWithComma()));
                            }
                            // return empty set
                            return result;
                        }

                        // user said provider and then the dynamic parameters imply providers they didn't select
                        if (IsInvocation) {
                            // error
                        }
                        // return empty set
                        return result;

                    }

                    if (didUserSpecifySources) {
                        // user gave sources which implied some providers but the dynamic parameters implied different providers
                        if (IsInvocation) {
                            // error
                        }
                        // return empty set
                        return result;
                    }

                    // well, this is silly.
                    // if the user didn't specify a source or a provider
                    // but the FilterProvidersUsingDynamicParameters came back empty
                    // that means that they user specified parameters from two conflicting providers
                    // and they forced each other out!

                    if (IsInvocation) {
                        // error

                    }

                }
                */
                return result;
            }
        }


        private IEnumerable<PackageProvider> FilterProvidersUsingDynamicParameters(MutableEnumerable<PackageProvider> providers, IEnumerable<PackageSource> userSpecifiedRegisteredSources,  bool didUserSpecifyProviders, bool didUserSpecifySources) {
            var excluded = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

            var setparameters = DynamicParameterDictionary.Values.OfType<CustomRuntimeDefinedParameter>().Where(each => each.IsSet).ReEnumerable();

            var matchedProviders = (setparameters.Any() ? providers.Where(p => setparameters.All(each => each.Options.Any(opt => opt.ProviderName == p.ProviderName))) : providers).ReEnumerable();

            foreach (var provider in matchedProviders) {
                // if a 'required' parameter is not filled in, the provider should not be returned.
                // we'll collect these for warnings at the end of the filter.
                var missingRequiredParameters = DynamicParameterDictionary.Values.OfType<CustomRuntimeDefinedParameter>().Where(each => !each.IsSet && each.IsRequiredForProvider(provider.ProviderName)).ReEnumerable();
                if (!missingRequiredParameters.Any()) {
                    yield return provider;
                } else {
                    Collection<string> missingOptions = new Collection<string>();
                    foreach (var missingRequiredParameter in missingRequiredParameters)
                    {
                        foreach (var option in missingRequiredParameter.Options)
                        {
                    // remember these so we can warn later.
                            missingOptions.Add(option.Name);
                        }
                    }
                    excluded.Add(provider.ProviderName, missingOptions);
                }
            }

            /* TODO: provide errors in the case where everything got filtered out. Or maybe warnings?
             *
            var mismatchedProviders = (setparameters.Any() ? providers.Where(each => !matchedProviders.Contains(each)).Where(p => setparameters.Any(each => each.Options.Any(opt => opt.ProviderName == p.ProviderName))) : Enumerable.Empty<PackageProvider>()).ReEnumerable();

            if (!found) {
                // we didn't find anything that matched
                // they specified dynamic parameters that implicitly select providers
                // that don't fit with the providers and sources that they initially asked for.

                if (didUserSpecifyProviders || didUserSpecifySources) {

                    if (IsInvocation) {
                        QueueHeldMessage(() => Error(Errors.ExcludedProvidersDueToMissingRequiredParameter, excluded.Keys, userSpecifiedSources.JoinWithComma()));
                    }

                    yield break;

                }

                if (didUserSpecifySources) {
                    // user gave sources which implied some providers but the dynamic parameters implied different providers
                    if (IsInvocation) {
                        // error
                    }
                    // return empty set
                    return result;
                }

                // well, this is silly.
                // if the user didn't specify a source or a provider
                // but the FilterProvidersUsingDynamicParameters came back empty
                // that means that they user specified parameters from two conflicting providers
                // and they forced each other out!

                if (IsInvocation) {
                    // error

                }

            }
            */

            if (ProviderName != null && ProviderName.Any()) {
                foreach (var providerName in ProviderName) {
                    if (excluded.ContainsKey(providerName)) {
                        Error(Constants.Errors.SpecifiedProviderMissingRequiredOption, providerName, excluded[providerName].JoinWithComma());
                    }
                }
            }

            // these warnings only show for providers that would have otherwise be selected.
            // if not for the missing required parameter.
            foreach (var mp in excluded.OrderBy(each => each.Key)) {
                string optionsValue = mp.Value.JoinWithComma();

                if (userSpecifiedRegisteredSources.Any())
                {
                    var mp1 = mp;

                    //Check if the provider with missing dynamic parameters has been registered with the source provided by a user
                    var sources = userSpecifiedRegisteredSources.Where(source => source.ProviderName != null && source.ProviderName.EqualsIgnoreCase(mp1.Key));

                    if (didUserSpecifySources && sources.Any())
                    {
                        //Error out if the provider associated with the -source matches
                        Error(Constants.Errors.SpecifiedProviderMissingRequiredOption, mp.Key, optionsValue);
                    }
                }

                Verbose(Constants.Messages.SkippedProviderMissingRequiredOption, mp.Key, optionsValue);
            }
        }


        protected virtual void GenerateCmdletSpecificParameters(Dictionary<string, object> unboundArguments) {
            if (!IsInvocation) {
                var providerNames = PackageManagementService.AllProviderNames;
                var whatsOnCmdline = GetDynamicParameterValue<string[]>("ProviderName");
                if (whatsOnCmdline != null) {
                    providerNames = providerNames.Concat(whatsOnCmdline).Distinct();
                }

                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof (string[]), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true
                    },
                    new AliasAttribute("Provider"),
                    new ValidateSetAttribute(providerNames.ToArray())
                }));
            } else {
                DynamicParameterDictionary.AddOrSet("ProviderName", new RuntimeDefinedParameter("ProviderName", typeof(string[]), new Collection<Attribute> {
                    new ParameterAttribute {
                        ValueFromPipelineByPropertyName = true
                    },
                    new AliasAttribute("Provider")
                }));
            }
        }

        protected bool ActualGenerateDynamicParameters(Dictionary<string,object> unboundArguments ) {
            if (CachedStaticParameters == null) {
                // we're in the second call, we're just looking to find out what the static parameters actually are.
                // we're gonna just skip generating the dynamic parameters on this call.
                return true;
            }

            try {

                unboundArguments = unboundArguments ?? new Dictionary<string, object>();

                // if there are unbound arguments that are owned by a provider, we can narrow the rest of the
                // arguments to just ones that are connected with that provider
                var dynamicOptions = CachedDynamicOptions;

                var keys = unboundArguments.Keys.ToArray();
                if (keys.Length > 0) {
                    var acceptableProviders = CachedDynamicOptions.Where(option => keys.ContainsAnyOfIgnoreCase(option.Name)).Select(option => option.ProviderName).Distinct().ToArray();
                    if (acceptableProviders.Length > 0) {
                        dynamicOptions = dynamicOptions.Where(option => acceptableProviders.Contains(option.ProviderName)).ToArray();
                    }
                }
                // generate the common parameters for our cmdlets (timeout, messagehandler, etc)
                GenerateCommonDynamicParameters();

                // generate parameters that are specific to the cmdlet being implemented.
                GenerateCmdletSpecificParameters(unboundArguments);

                var staticParameters = GetType().Get<Dictionary<string, ParameterMetadata>>("MyInvocation.MyCommand.Parameters");

                foreach (var md in dynamicOptions) {
                    if (string.IsNullOrWhiteSpace(md.Name)) {
                        continue;
                    }

                    if (DynamicParameterDictionary.ContainsKey(md.Name)) {
                        // todo: if the dynamic parameters from two providers aren't compatible, then what?

                        // for now, we're just going to mark the existing parameter as also used by the second provider to specify it.
                        var crdp = DynamicParameterDictionary[md.Name] as CustomRuntimeDefinedParameter;

                        if (crdp == null) {
                            // the package provider is trying to overwrite a parameter that is already dynamically defined by the BaseCmdlet.
                            // just ignore it.
                            continue;
                        }

                        if (IsInvocation) {
                            // this is during an actual execution
                            crdp.Options.Add(md);
                        } else {
                            // this is for metadata sake. (get-help, etc)
                            crdp.IncludeInParameterSet(md, IsInvocation, ParameterSets);
                        }
                    } else {
                        // check if the dynamic parameter is a static parameter first.

                        // this can happen if we make a common dynamic parameter into a proper static one
                        // and a provider didn't know that yet.

                        if (staticParameters != null && staticParameters.ContainsKey(md.Name)) {
                            // don't add it.
                            continue;
                        }

                        DynamicParameterDictionary.Add(md.Name, new CustomRuntimeDefinedParameter(md, IsInvocation, ParameterSets));
                    }
                }
            } catch (Exception e) {
                e.Dump();
            }
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's a performance thing.")]
        protected PackageProvider[] CachedSelectedProviders {
            get {
                return GetType().GetOrAdd(() => SelectedProviders.ToArray(), "CachedSelectedProviders");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "It's a performance thing.")]
        protected virtual DynamicOption[] CachedDynamicOptions {
            get {
                return GetType().GetOrAdd(() => CachedSelectedProviders.SelectMany(provider => _optionCategories.SelectMany(category => provider.GetDynamicOptions(category, this.SuppressErrorsAndWarnings(IsProcessing)))).ToArray(), "CachedDynamicOptions");
            }
        }

        protected Dictionary<string, ParameterMetadata> CachedStaticParameters {
            get {
                return GetType().Get<Dictionary<string, ParameterMetadata>>("MyInvocation.MyCommand.Parameters");
            }
        }

        public override bool GenerateDynamicParameters() {
            var thisIsFirstObject = false;
            try {
                if (!IsReentrantLocked) {
                    // we're not locked at this point. Let's turn on the lock.
                    IsReentrantLocked = true;
                    thisIsFirstObject = true;

                    try {
                        // do all the work that we need to during the lock
                        // this includes:
                        //      one-time-per-call work
                        //      any access to MyInvocation.MyCommand.*
                        //      modifying parameter validation sets
                        //

                        if (MyInvocation != null && MyInvocation.MyCommand != null && MyInvocation.MyCommand.Parameters != null) {
                            GetType().AddOrSet(MyInvocation.MyCommand.Parameters, "MyInvocation.MyCommand.Parameters");
                        }
#if DEEP_DEBUG
                        else {
                            if (MyInvocation == null) {
                                Console.WriteLine("»»» Attempt to get parameters MyInvocation == NULL");
                            } else {
                                if (MyInvocation.MyCommand == null) {
                                    Console.WriteLine("»»» Attempt to get parameters MyCommand == NULL");
                                } else {
                                    Console.WriteLine("»»» Attempt to get parameters Parameters == NULL");
                                }
                            }
                        }
#endif


                        // the second time, it will generate all the parameters, including the dynamic ones.
                        // (not that we currently need it, but if you do, you gotta do it here!)
                        // var all_parameters = MyInvocation.MyCommand.Parameters;

                        // ask for the unbound arguments.
                          var unbound = UnboundArguments;
                          if (unbound.ContainsKey("ProviderName") || unbound.ContainsKey("Provider"))
                          {
                            var pName = unbound.ContainsKey("ProviderName")?unbound["ProviderName"]:unbound["Provider"] ;
                            if (pName != null)
                            {

                                if (pName.GetType().IsArray)
                                {
                                    ProviderName = pName as string[] ?? (((object[])pName).Select(p => p.ToString()).ToArray());
                                }
                                else
                                {
                                    ProviderName = new[] { pName.ToString() };
                                }
                             
                                // a user specifies -providerName
                                _isUserSpecifyOneProviderName = (ProviderName.Count() == 1);
                                
                            }
                        }

                        // we've now got a copy of the arguments that aren't bound
                        // and we can potentially narrow the provider selection based
                        // on arguments the user specified.

                        if (null== CachedSelectedProviders || IsFailingEarly || IsCanceled ) {
#if DEEP_DEBUG
                            Console.WriteLine("»»» Cancelled before we got finished doing dynamic parameters");
#endif
                            // this happens if there is a serious failure early in the cmdlet
                            // i.e. - if the SelectedProviders comes back empty (due to aggressive filtering)

                            // in this case, we just want to provide a catch-all for remaining arguments so that we can make
                            // output the error that we really want to (that the user specified conditions that filtered them all out)

                            DynamicParameterDictionary.Add("RemainingArguments", new RuntimeDefinedParameter("RemainingArguments", typeof(object), new Collection<Attribute> {
                                new ParameterAttribute() {   ValueFromRemainingArguments =  true},
                            }));
                        }

                        // at this point, we're actually calling to have the dynamic parameters generated
                        // that are expected to be used.
                        return ActualGenerateDynamicParameters(unbound);

                    } finally {
                        IsReentrantLocked = false;
                    }
                }

                // otherwise just call the AGDP because we're in a reentrant call.
                // and this might be needed if the original call had some strange need
                // to know what the parameters that it's about to generate would be.
                // Yeah, you heard me.
                return ActualGenerateDynamicParameters(null);

            } finally
            {
                if (thisIsFirstObject) {
                    // clean up our once-per-call data.
                    GetType().Remove<PackageProvider[]>( "CachedSelectedProviders");
                    GetType().Remove<Dictionary<string, ParameterMetadata>>("MyInvocation.MyCommand.Parameters");
                    GetType().Remove<DynamicOption[]>("CachedDynamicOptions");
                }
            }
            // return true;
        }

        // true if a user specifies -displayCulture and provider has the "DisplayCulture" dynamic option
        protected bool IsDisplayCulture
        {
            get
            {
                if (!_initializedCulture)
                {
                    var displayCulture = GetDynamicParameterValue<string>("DisplayCulture");
                    _isDisplayCulture = !(string.IsNullOrWhiteSpace(displayCulture));
                    _initializedCulture = true;
                    return _isDisplayCulture;
                }
                else
                {
                    return _isDisplayCulture;
                }
            }
        }

        protected virtual bool UseDefaultSourceFormat
        {
            get
            {
                if (_initializedSource)
                {
                    return _useDefaultSourceFormat;
                }
                // check if a user specifies -source and its source name >= the default width. if so, we will widen the source column
                if (!_resolvedUserSpecifiedSource.IsNullOrEmpty())
                {
                    //the default width of the Source column is 16
                    _useDefaultSourceFormat = _resolvedUserSpecifiedSource.Any(each => each.Source.Length <= 16);
                }
                _initializedSource = true;
                return _useDefaultSourceFormat;
            }
        }
        private string GetSoftwareIdentityTypeName(SoftwareIdentity package)
        {
            if(_initializedTypeName)
            {
                return _softwareIdentityTypeName;
            }

            //check if a user specifies -source with a long source name such as http://www.powershellgallery.com/api/v2, 
            // if so we will choose the longsource column format.
            if (!UseDefaultSourceFormat)
            {
                _softwareIdentityTypeName += "#DisplayLongSourceName";
                _hasTypeNameChanged = true;
            }
            //provider has the "DisplayCulture" in the Get-DynamicOption()
            if (IsDisplayCulture)
            {
                _softwareIdentityTypeName += "#DisplayCulture";
                _hasTypeNameChanged = true;
            }

            //provider defines the 'DisplayLongName' feature in the Get-Feature()
            if (package != null && (package.Provider != null && (package.Provider.Features != null && package.Provider.Features.ContainsKey("DisplayLongName"))))
            {
                _softwareIdentityTypeName += "#DisplayLongName";
                _hasTypeNameChanged = true;
            }

            _initializedTypeName = true;

            return _softwareIdentityTypeName;
        }

        /// <summary>
        /// This method is used for customizing the format of the OneGet *-Package output display.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        protected object AddPropertyToSoftwareIdentity(SoftwareIdentity package)
        {
            // Use the default output format if a user does not provide the -providername property, e.g. find-package
            if (package == null || (!_isUserSpecifyOneProviderName && !IsDisplayCulture && UseDefaultSourceFormat))
            {
                return package;
            }

            // Customize the output format
            var typeName = GetSoftwareIdentityTypeName(package);

            // For the find-package -providername nuget case, we can return right away.
            if (!_hasTypeNameChanged) {
                return package;
            }

            var swidTagAsPsobj = PSObject.AsPSObject(package);
            var noteProperty = new PSNoteProperty("PropertyOfSoftwareIdentity", "PropertyOfSoftwareIdentity");
            swidTagAsPsobj.Properties.Add(noteProperty, true);
            swidTagAsPsobj.TypeNames.Insert(0, typeName);
            return swidTagAsPsobj;
        }
    }
}
