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

namespace Microsoft.PackageManagement.Archivers.Internal {
    using System;
    using System.Collections.Generic;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;

    public class CabArchiver {
        private static readonly Dictionary<string, string[]> _features = new Dictionary<string, string[]> {
            {Constants.Features.SupportedExtensions, new[] {"cab", "msu"}},
            {Constants.Features.MagicSignatures, new[] {Constants.Signatures.Cab}}
        };

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
            request.Debug("Calling '{0}::GetFeatures' ", ArchiverName);
            foreach (var feature in _features) {
                request.Yield(feature);
            }
        }

        /// <summary>
        ///     Returns the name of the Provider.
        /// </summary>
        /// <returns></returns>
        public string ArchiverName {
            get { return "cabfile"; }
        }

        /// <summary>
        /// Returns the version of the Provider.
        /// </summary>
        /// <returns>The version of this provider </returns>
        public string ProviderVersion {
            get {
                return "1.0.0.0";
            }
        }

        public IEnumerable<string> UnpackArchive(string localFilename, string destinationFolder, Request request) {
            return null;
        }

        public bool IsSupportedFile(string localFilename) {
            return false;
        }
    }
}

