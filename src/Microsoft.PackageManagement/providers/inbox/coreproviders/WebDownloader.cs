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

namespace Microsoft.PackageManagement.Providers.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Net;
    using System.Globalization;
#if CORECLR
    using System.Net.Http;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using PackageManagement.Internal;
    using PackageManagement.Internal.Implementation;
    using PackageManagement.Internal.Utility.Extensions;

    public class WebDownloader {
        internal static string ProviderName = "WebDownloader";

        private static readonly Dictionary<string, string[]> _features = new Dictionary<string, string[]> {
            {Constants.Features.SupportedSchemes, new[] {"http", "https", "ftp", "file"}},
        };

        /// <summary>
        ///     Returns a collection of strings to the client advertizing features this provider supports.
        /// </summary>
        /// <param name="request">
        ///     An object passed in from the CORE that contains functions that can be used to interact with
        ///     the CORE and HOST
        /// </param>
        public void GetFeatures(Request request) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            // Nice-to-have put a debug message in that tells what's going on.
            request.Debug("Calling '{0}::GetFeatures' ", ProviderName);
            foreach (var feature in _features) {
                request.Yield(feature);
            }
        }

        public void InitializeProvider(Request request) {
        }

        public string GetDownloaderName() {
            return ProviderName;
        }

        public string DownloadFile(Uri remoteLocation, string localFilename, int timeoutMilliseconds, bool showProgress, Request request) {

            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (remoteLocation == null) {
                throw new ArgumentNullException("remoteLocation");
            }

            request.Debug("Calling 'WebDownloader::DownloadFile' '{0}','{1}','{2}','{3}'", remoteLocation, localFilename, timeoutMilliseconds, showProgress);

            if (remoteLocation.Scheme.ToLowerInvariant() != "http" && remoteLocation.Scheme.ToLowerInvariant() != "https" && remoteLocation.Scheme.ToLowerInvariant() != "ftp") {
                request.Error(ErrorCategory.InvalidResult, remoteLocation.ToString(), Constants.Messages.SchemeNotSupported, remoteLocation.Scheme);
                return null;
            }

            if (localFilename == null) {
                localFilename = "downloadedFile.tmp".GenerateTemporaryFilename();
            }

            localFilename = Path.GetFullPath(localFilename);

            // did the caller pass us a directory name?
            if (Directory.Exists(localFilename)) {
                localFilename = Path.Combine(localFilename, "downloadedFile.tmp");
            }

            // make sure that the parent folder is created first.
            var folder = Path.GetDirectoryName(localFilename);
            if (!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            // clobber an existing file if it's already there.
            // todo: in the future, we could check the md5 of the file and if the remote server supports it
            // todo: we could skip the download.
            if (File.Exists(localFilename)) {
                localFilename.TryHardToDelete();
            }

            // setup the progress tracker if the caller wanted one.
            int pid = 0;
            if (showProgress) {
                pid = request.StartProgress(0, "Downloading '{0}'", remoteLocation);
            }

#if CORECLR
            var task = Download(remoteLocation, localFilename, showProgress, request, pid);

            task.Wait(timeoutMilliseconds);

            if (!task.IsCompleted)
            {
                request.Warning(Constants.Status.TimedOut);
                request.Debug("Timed out downloading '{0}'", remoteLocation.AbsoluteUri);
            }
#else
            var webClient = new WebClient();

            // Mozilla/5.0 is the general token that says the browser is Mozilla compatible, and is common to almost every browser today.
            webClient.Headers.Add("User-Agent", "Mozilla/5.0 PackageManagement");

            // get ie settings
            webClient.Proxy = WebRequest.GetSystemWebProxy();

            // set credentials to be user credentials
            webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

            var done = new ManualResetEvent(false);

            webClient.DownloadFileCompleted += (sender, args) => {
                if (args.Cancelled || args.Error != null) {
                    localFilename = null;
                }
                done.Set();
            };

            var lastPercent = 0;

            if (showProgress) {
                webClient.DownloadProgressChanged += (sender, args) => {
                    // Progress(requestObject, 2, (int)percent, "Downloading {0} of {1} bytes", args.BytesReceived, args.TotalBytesToReceive);
                    var percent = (int)((args.BytesReceived*100)/args.TotalBytesToReceive);
                    if (percent > lastPercent) {
                        lastPercent = percent;
                        request.Progress(pid, (int)((args.BytesReceived*100)/args.TotalBytesToReceive), "To {0}", localFilename);
                    }
                };
            }

            // start the download 
            webClient.DownloadFileAsync(remoteLocation, localFilename);

            // wait for the completion 
            if (timeoutMilliseconds > 0) {
                if (!done.WaitOne(timeoutMilliseconds))
                {
                    webClient.CancelAsync();
                    request.Warning(Constants.Status.TimedOut);
                    request.Debug("Timed out downloading '{0}'", remoteLocation.AbsoluteUri);
                    return null;
                }
            } else {
                // wait until it completes or fails on it's own
                done.WaitOne();
            }

#endif
            
            // if we don't have the file by this point, we've failed.
            if (localFilename == null || !File.Exists(localFilename)) {
                request.CompleteProgress(pid, false);
                request.Error(ErrorCategory.InvalidResult, remoteLocation.ToString(), Constants.Messages.UnableToDownload, remoteLocation.ToString(), localFilename);
                return null;
            }

            if (showProgress) {
                request.CompleteProgress(pid, true);
            }

            return localFilename;
        }

#if CORECLR
        private async Task<string> Download(Uri remoteLocation, string localFilename, bool showProgress, Request request, int pid)
        {
            var clientHandler = new HttpClientHandler();
            
            clientHandler.UseDefaultCredentials = true;

            // defaultwebproxy will use default ie settings
            clientHandler.Proxy = System.Net.WebRequest.DefaultWebProxy;
        
            // set credential of user to the proxy
            clientHandler.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

            var httpClient = new HttpClient();

            request.Debug("Calling httpclient with remotelocation {0}", remoteLocation.AbsoluteUri);

            // Mozilla/5.0 is the general token that says the browser is Mozilla compatible, and is common to almost every browser today.
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 PackageManagement");
            httpClient.DefaultRequestHeaders.ExpectContinue = false;

            long totalBytesToReceive = 0L;

            HttpResponseMessage response = await httpClient.GetAsync(remoteLocation, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content != null && response.Content.Headers != null)
            {
                totalBytesToReceive = response.Content.Headers.ContentLength ?? 0;
            }

            try
            {
                Stream input = await response.Content.ReadAsStreamAsync();
                byte[] bytes = new byte[1024 * 4];
                FileStream output = File.Open(localFilename, FileMode.OpenOrCreate);

                long totalDownloaded = 0;
                int current = 0;
                int lastPercent = 0;

                current = await input.ReadAsync(bytes, 0, bytes.Length);

                while (current > 0)
                {
                    totalDownloaded += current;

                    await output.WriteAsync(bytes, 0, current);

                    if (showProgress && totalBytesToReceive != 0)
                    {
                        int percent = (int)((totalDownloaded * 100) / totalBytesToReceive);

                        if (percent > lastPercent)
                        {
                            lastPercent = percent;
                            request.Progress(pid, (int)percent, "To {0}", localFilename);
                        }
                    }

                    current = await input.ReadAsync(bytes, 0, bytes.Length);
                }

                input.Dispose();
                output.Dispose();
            }
            catch (Exception e)
            {
                request.Debug(e.Message);
                localFilename = null;
            }

            return localFilename;
        }

#endif
    }
}
