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

namespace Microsoft.PackageManagement.Providers.Internal.Bootstrap {
    using System;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Api;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Packaging;
    using PackageManagement.Internal.Utility.Extensions;
    using PackageManagement.Internal.Utility.Plugin;

    internal class Swid {
        private const int SwidDownloadTimeout = 40000;
        internal readonly BootstrapRequest _request;
        internal readonly Swidtag _swidtag;
        private bool _timedOut;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Swid(BootstrapRequest request, Swidtag swidtag) {
            _request = request;
            _swidtag = swidtag;
        }

        internal Swid(BootstrapRequest request, IEnumerable<Link> mirrors) {
            _request = request;
            _swidtag = DownloadSwidtag(mirrors);
        }

        internal Swid(BootstrapRequest request, IEnumerable<Uri> mirrors) {
            _request = request;
            _swidtag = DownloadSwidtag(mirrors);
        }

        internal Uri Location {get;  set;}

        private IRequest DownloadRequest {
            get {
                _timedOut = false;
                // overrides the Warning to check for timed out messages.
                return new object[] {
                    new {
                        Warning = new Func<string, bool>((messageText) => {
                            if (messageText == Constants.Status.TimedOut) {
                                _timedOut = true;
                                return true;
                            }
                            return _request.Warning(messageText);
                        }),
                        Error = new Func<string,string,string,string, bool>((category, id, target, messageText ) => {
                            // tag download errors don't count as "errors", at best, just a warning.
                            return _request.Verbose(messageText);
                        })
                    },
                    _request
                }.As<IRequest>();
            }
        }

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        protected IEnumerable<IGrouping<string, Link>> Artifacts {
            get {
                return IsValid ? _swidtag.Links.GroupBy(link => string.IsNullOrEmpty(link.Artifact) ? FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory() : link.Artifact) : Enumerable.Empty<IGrouping<string, Link>>();
            }
        }

        protected IEnumerable<IGrouping<string, Link>> Feeds {
            get {
                return IsValid
                    ? _swidtag.Links.Where(link => link.Relationship == Iso19770_2.Relationship.Feed).GroupBy(link => string.IsNullOrEmpty(link.Artifact) ? FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory() : link.Artifact) : Enumerable.Empty<IGrouping<string, Link>>();
            }
        }

        protected IEnumerable<IGrouping<string, Link>> Packages {
            get {
                return IsValid
                    ? _swidtag.Links.Where(link => link.Relationship == Iso19770_2.Relationship.Package).GroupBy(link => string.IsNullOrEmpty(link.Artifact) ? FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory() : link.Artifact)
                    : Enumerable.Empty<IGrouping<string, Link>>();
            }
        }

        protected IEnumerable<IGrouping<string, Link>> More {
            get {
                return _swidtag.Links.Where(link => link.Relationship == Iso19770_2.Relationship.Supplemental).GroupBy(link => string.IsNullOrEmpty(link.Artifact) ? FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory() : link.Artifact);
            }
        }

        internal virtual bool IsValid {
            get {
                return _swidtag != null;
            }
        }

        private Swidtag DownloadSwidtag(IEnumerable<Uri> locations) {            
            foreach (var location in locations.WhereNotNull()) {
                try {
                    var filename = FilesystemExtensions.GenerateTemporaryFileOrDirectoryNameInTempDirectory();
                    DownloadSwidtagToFile(filename, location);

                    if (_timedOut) {
                        // try one more time...
                        DownloadSwidtagToFile(filename, location);
                    }

                    if (filename.FileExists()) {
                        try
                        {
                            var document = XDocument.Load(filename);
                            if (Swidtag.IsSwidtag(document.Root))
                            {
                                Location = location;
                                return new Swidtag(document);
                            }
                        }
                        finally
                        {
                            System.IO.File.Delete(filename);
                        }
                    }
                } catch (Exception e) {
                    e.Dump();
                }
            }
            return null;
        }

        private string DownloadSwidtagToFile(string filename, Uri location) {
            return _request.RetryDownload(
                    (uri) => 
                        {
                            if (_request.ProviderServices == null)
                            {
                                // during initialization, the pluggable downloader isn't available.
                                // luckily, it's built into this assembly, so we'll create and use it directly.
                                return new WebDownloader().DownloadFile(location, filename, SwidDownloadTimeout, false, DownloadRequest.As<Request>());
                            }

                            // otherwise, we can just use the pluggable one.
                            return _request.ProviderServices.DownloadFile(location, filename, SwidDownloadTimeout, false, DownloadRequest);
                        },
                    location
                );
        }

        internal Swidtag DownloadSwidtag(IEnumerable<Link> locations) {
            return DownloadSwidtag(locations.Where(link => link != null && link.HRef != null).Select(link => link.HRef));
        }

        protected IEnumerable<IGrouping<string, Link>> PackagesFilteredByName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return Packages;
            }

            return Packages.Where(packageGroup => {
                var n = packageGroup.FirstOrDefault().Attributes[Iso19770_2.Discovery.Name];
                return (string.IsNullOrEmpty(n) || name.EqualsIgnoreCase(n));
            });
        }
    }
}