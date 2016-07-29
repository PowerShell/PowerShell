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
    using System.IO;
    using System.Net;
    using System.Threading;
    using Directory = System.IO.Directory;
    using File = System.IO.File;
    using System.Threading.Tasks;
    using Microsoft.PackageManagement.Provider.Utility;
    using System.Security.Cryptography;
    using System.Linq;
    using ErrorCategory = PackageManagement.Internal.ErrorCategory;

    public class WebDownloader
    {

        /// <summary>
        /// Download data from remote via uri query.
        /// </summary>
        /// <param name="fileName">A file to store the downloaded data.</param>
        /// <param name="query">Uri query</param>
        /// <param name="request">An object passed in from the PackageManagement platform that contains APIs that can be used to interact with it </param>   
        /// <param name="networkCredential">Credential to pass along to get httpclient</param>
        /// <param name="progressTracker">Utility class to help track progress</param>
        /// <returns></returns>
        internal static async Task<long> DownloadDataToFileAsync(string fileName, string query, PackageSourceListRequest request, NetworkCredential networkCredential, ProgressTracker progressTracker)
        {
            request.Verbose(Resources.Messages.DownloadingPackage, query);

            // try downloading for 3 times
            int remainingTry = 3;
            long totalDownloaded = 0;

            CancellationTokenSource cts;
            Stream input = null;
            FileStream output = null;

            while (remainingTry > 0)
            {
                // if user cancel the request, no need to do anything
                if (request.IsCanceled)
                {
                    break;
                }

                input = null;
                output = null;
                cts = new CancellationTokenSource();
                totalDownloaded = 0;

                try
                {
                    // decrease try by 1
                    remainingTry -= 1;

                    var httpClient = request.Client;

                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "text/html; charset=iso-8859-1");

                    input = await httpClient.GetStreamAsync(query);


                    // buffer size of 64 KB, this seems to be preferable buffer size, not too small and not too big
                    byte[] bytes = new byte[1024 * 64];
                    output = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

                    int current = 0;

                    // here we read content that we got from the http response stream into the bytes array
                    current = await input.ReadAsync(bytes, 0, bytes.Length, cts.Token);

                    int progressPercentage = progressTracker.StartPercent;
                    // report initial progress
                    request.Progress(progressTracker.ProgressID, progressPercentage, Resources.Messages.BytesRead, current);

                    int i = progressTracker.StartPercent;

                    while (current > 0)
                    {
                        totalDownloaded += current;

                        // here we write out the bytes array into the file
                        await output.WriteAsync(bytes, 0, current, cts.Token);

                        // report the progress
                        request.Progress(progressTracker.ProgressID, progressPercentage<progressTracker.EndPercent?progressPercentage++:progressTracker.EndPercent, Resources.Messages.BytesRead, totalDownloaded);

                        // continue reading from the stream
                        current = await input.ReadAsync(bytes, 0, bytes.Length, cts.Token);
                    }

                    if (totalDownloaded > 0)
                    {
                        // report that we finished the download
                        request.Progress(progressTracker.ProgressID, progressTracker.EndPercent, Resources.Messages.BytesRead, totalDownloaded);
                        request.CompleteProgress(progressTracker.ProgressID, true);

                        return totalDownloaded;
                    }

                    // if request is canceled, don't retry
                    if (request.IsCanceled)
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    request.CompleteProgress(progressTracker.ProgressID, true);

                    request.Verbose(ex.Message);
                    request.Debug(ex.StackTrace);
                }
                finally
                {
                    // dispose input and output stream
                    if (input != null)
                    {
                        input.Dispose();
                    }

                    // dispose it
                    if (output != null)
                    {
                        output.Dispose();
                    }
                    // delete the file if created and nothing has downloaded
                    if (totalDownloaded == 0 && File.Exists(fileName))
                    {
                        fileName.TryHardToDelete();
                    }

                    if (cts != null)
                    {
                        cts.Dispose();
                    }
                }

                // we have to retry again
                request.Verbose(Resources.Messages.RetryingDownload, query, remainingTry);
            }

            return totalDownloaded;
        }

        internal static string DownloadFile(string queryUrl, string destination, PackageSourceListRequest request, ProgressTracker progressTracker)
        {
            try
            {
                request.Debug(Resources.Messages.DebugInfoCallMethod, Constants.ProviderName, string.Format(System.Globalization.CultureInfo.InvariantCulture, "DownloadFile - url='{0}', destination='{1}'", queryUrl, destination));

                if (string.IsNullOrWhiteSpace(destination))
                {
                    throw new ArgumentNullException("destination");
                }

                // make sure that the parent folder is created first.
                var folder = Path.GetDirectoryName(destination);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                if (File.Exists(destination))
                {
                    destination.TryHardToDelete();
                }

                
                if (progressTracker == null)
                {
                    progressTracker = new ProgressTracker(request.StartProgress(0, Resources.Messages.DownloadingPackage, queryUrl));
                } 

                Uri uri;

                if (!Uri.TryCreate(queryUrl, UriKind.Absolute, out uri))
                {
                    request.Error(Internal.ErrorCategory.InvalidOperation, Resources.Messages.UnsuportedUriFormat, Constants.ProviderName, queryUrl);
                    return null;
                }

                if (uri.IsFile)
                {
                    // downloading from a file share
                    using (var input = File.OpenRead(queryUrl))
                    {
                        using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            request.Progress(progressTracker.ProgressID, progressTracker.StartPercent, Resources.Messages.Downloading);

                            input.CopyTo(output);

                        }
                    }

                    request.CompleteProgress(progressTracker.ProgressID, true);
                }
                else
                {
                    //Downloading from url
                    var result = DownloadDataToFileAsync(destination, queryUrl, request, PathUtility.GetNetworkCredential(request.CredentialUsername, request.CredentialPassword), progressTracker).Result;
                }

                if (File.Exists(destination))
                {
                    request.Verbose(Resources.Messages.CompletedDownload, queryUrl);                    
                    return destination;
                }
                else
                {
                    request.Error(Internal.ErrorCategory.InvalidOperation, Resources.Messages.FailedToDownload, Constants.ProviderName, queryUrl, destination);
                    return null;
                }                
            }
            catch (Exception ex)
            {
                ex.Dump(request);
                request.Warning(ex.Message);
                return null;
            }
        }

        internal static bool VerifyHash(string fileFullPath,PackageJson package, PackageSourceListRequest request)
        {
            //skip in case the skip switch is specified
            if (request.SkipHashValidation.Value)
            {
                request.Verbose(Resources.Messages.SkipHashValidation);
                return true;                   
            }
            PackageHash packageHash = package.Hash;
            if (packageHash==null || string.IsNullOrWhiteSpace(packageHash.algorithm) || string.IsNullOrWhiteSpace(packageHash.hashCode))
            {
                request.WriteError(ErrorCategory.InvalidArgument, Constants.ProviderName, Resources.Messages.HashNotSpecified, package.Name);
                return false;
            }
            try
            {
                HashAlgorithm hashAlgorithm = null;
                switch (packageHash.algorithm.ToLowerInvariant())
                {
                    case "sha256":
                        hashAlgorithm = SHA256.Create();
                        break;

                    case "md5":
                        hashAlgorithm = MD5.Create();
                        break;

                    case "sha512":
                        hashAlgorithm = SHA512.Create();
                        break;                    
                    default:
                        request.WriteError(ErrorCategory.InvalidArgument, Constants.ProviderName, Resources.Messages.InvalidHashAlgorithm, packageHash.algorithm);
                        return false;
                }

                using (FileStream stream = File.OpenRead(fileFullPath))
                {
                    // compute the hash
                    byte[] computedHash = hashAlgorithm.ComputeHash(stream);
                    // convert the original hash we got from json
                    byte[] hashFromJSON = Convert.FromBase64String(package.Hash.hashCode);
                    if (!Enumerable.SequenceEqual(computedHash, hashFromJSON))
                    {
                        request.WriteError(ErrorCategory.InvalidOperation, Constants.ProviderName, Resources.Messages.HashVerificationFailed, package.Name, package.Source);
                        return false;
                    }
                    else
                    {
                        request.Verbose(Resources.Messages.HashValidationSuccessfull);
                    }
                }
            }
            catch
            {
                request.WriteError(ErrorCategory.InvalidOperation, Constants.ProviderName, Resources.Messages.HashVerificationFailed, package.Name, package.Source);
                return false;
            }
            
           return true;
        }
    }
}

#endif
