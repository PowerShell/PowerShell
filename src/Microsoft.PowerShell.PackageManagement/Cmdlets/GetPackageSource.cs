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
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.PackageManagement.Internal.Packaging;
    using Microsoft.PackageManagement.Internal.Utility.Async;
    using Microsoft.PackageManagement.Internal.Utility.Extensions;
    using Microsoft.PackageManagement.Packaging;
    using Utility;

    [Cmdlet(VerbsCommon.Get, Constants.Nouns.PackageSourceNoun, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=517137")]
    public sealed class GetPackageSource : CmdletWithProvider {
        private readonly List<PackageSource> _unregistered = new List<PackageSource>();
        private bool _found;
        private bool _noLocation;
        private bool _noName;
        private string _name;
        private string _originalName;

        public GetPackageSource()
            : base(new[] {
                OptionCategory.Provider, OptionCategory.Source
            }) {
        }

        protected override IEnumerable<string> ParameterSets {
            get {
                return new[] {""};
            }
        }

        [Parameter(Position = 0)]
        public string Name {
            get {
                return _name;
            }
            set {
                _originalName = value;
                if (!string.IsNullOrWhiteSpace(_originalName) && _originalName.ContainsWildcards()){
                    //'Name' means package Source Name here. if we pass down the source name with any wildcard characters, providers cannot resolve them.
                    //set it to "" here just as a user does not provide -Name. With that Get-PackageSource returns all and we'll filter on results.
                    _name = string.Empty;
                } else {
                    _name = _originalName;                    
                }
            }
        }

        [Parameter]
        public string Location {get; set;}

        private IEnumerable<string> _sources {
            get {
                if (!string.IsNullOrWhiteSpace(Name)) {
                    yield return Name;
                }

                if (!string.IsNullOrWhiteSpace(Location)) {
                    yield return Location;
                }
            }
        }

        public override IEnumerable<string> Sources {
            get {
                return _sources;
            }
        }

        private bool WriteSources(IEnumerable<PackageSource> sources) {
            foreach (var source in sources) {
                _found = true;
                WriteObject(source);
            }
            return _found;
        }

        public override bool ProcessRecordAsync() {
            var noName = string.IsNullOrWhiteSpace(Name);
            var noLocation = string.IsNullOrWhiteSpace(Location);
            var noCriteria = noName && noLocation;

            // store the information if we've ever had a name or location
            _noName = _noName || noName;
            _noLocation = _noLocation || noLocation;

            foreach (var provider in SelectedProviders) {
                if (Stopping) {
                    return false;
                }

                using (var src = provider.ResolvePackageSources(this.SuppressErrorsAndWarnings(IsProcessing)).CancelWhen(CancellationEvent.Token)) {
                    var sources = src.Distinct();
                    if (!string.IsNullOrWhiteSpace(_originalName) && _originalName.ContainsWildcards()) {
                        WriteSources(sources.Where(each => each.Name.IsWildcardMatch(_originalName)));
                        continue;
                    }
                    if (noCriteria) {
                        // no criteria means just return whatever we found
                        if (WriteSources(sources)) {
                        }
                    } else {
                        var all = sources.ToArray();
                        var registered = all.Where(each => each.IsRegistered);

                        if (noName)
                        {
                            // just location was specified
                            if (WriteSources(registered.Where(each => each.Location.EqualsIgnoreCase(Location)))) {
                                continue;
                            }
                        }
                        else if (noLocation)
                        {
                            // just name was specified
                            if (WriteSources(registered.Where(each => each.Name.EqualsIgnoreCase(Name))))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // name and location were specified
                            if (WriteSources(registered.Where(each => each.Name.EqualsIgnoreCase(Name) && each.Location.EqualsIgnoreCase(Location)))) {
                                continue;
                            }
                        }
                        // we haven't returned anything to the user yet...
                        // hold on to the unregistered ones. Might need these at the end.
                        _unregistered.AddRangeLocked(all.Where(each => !each.IsRegistered));
                    }
                }
            }

            return true;
        }

        public override bool EndProcessingAsync() {
            if (!_found) {
                if (_noName && _noLocation) {
                    // no criteria means just return whatever we found
                    if (WriteSources(_unregistered)) {
                        return true;
                    }
                    Warning(Constants.Messages.SourceNotFoundNoCriteria);
                    return true;
                }

                if (_noName) {
                    // just location was specified
                    if (WriteSources(_unregistered.Where(each => each.Location.EqualsIgnoreCase(Location)))) {
                        return true;
                    }
                    Warning(Constants.Messages.SourceNotFoundForLocation, Location);
                    return true;
                }

                if (_noLocation)
                {
                    // just name was specified
                    if (WriteSources(_unregistered.Where(each => each.Name.EqualsIgnoreCase(Name))))
                    {
                    return true;
                }
                Warning(Constants.Messages.SourceNotFound, Name);
                return true;
            }

                // both Name and Location were specified
                if (WriteSources(_unregistered.Where(each => each.Name.EqualsIgnoreCase(Name) && each.Location.EqualsIgnoreCase(Location)))) {
                    return true;
                }

                Warning(Constants.Messages.SourceNotFoundForNameAndLocation, Name, Location);
                return true;
            }
            return true;
        }
    }
}
