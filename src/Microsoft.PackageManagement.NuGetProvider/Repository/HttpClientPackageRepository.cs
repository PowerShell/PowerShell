namespace Microsoft.PackageManagement.NuGetProvider  {
    using System;
    using System.Net;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Resources;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Security;
    using Microsoft.PackageManagement.Provider.Utility;

    /// <summary>
    /// Package repository for downloading data from remote galleries
    /// </summary>
    internal class HttpClientPackageRepository : IPackageRepository
    {
        private readonly string _nugetFindPackageIdQueryFormat;
        private readonly string _queryUri;

        /// <summary>
        /// Ctor's
        /// </summary>
        /// <param name="request">The nuget request object</param>
        /// <param name="queryUrl">Packagesource location</param>
        internal HttpClientPackageRepository(string queryUrl, NuGetRequest request) 
        {
            // Validate the url

            Uri newUri;
            Uri validatedUri = null;

            if (Uri.TryCreate(queryUrl, UriKind.Absolute, out newUri))
            {
                validatedUri = NuGetPathUtility.ValidateUri(newUri, request);
            }

            if (validatedUri == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Messages.InvalidQueryUrl, queryUrl));
            }

            queryUrl = validatedUri.AbsoluteUri;

            // if a query is http://www.nuget.org/api/v2 then we add / to the end
            if (!queryUrl.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                queryUrl = String.Concat(queryUrl, "/");
            }

            _queryUri = queryUrl;

            //we are constructing the url query like http://www.nuget.org/api/v2/FindPackagesById()?id='JQuery'           
            _nugetFindPackageIdQueryFormat = PathUtility.UriCombine(_queryUri, NuGetConstant.FindPackagesById);
        }

        /// <summary>
        /// Package source location
        /// </summary>
        public string Source {
            get {
                // Package source Uri 
                return _queryUri;
            }
        }

        /// <summary>
        /// True if the packagesource is a file repository
        /// </summary>
        public bool IsFile
        {
            get
            {
                //false because this is not a local file repository
                return false;
            }
        }

        /// <summary>
        /// Find-Package
        /// </summary>
        /// <param name="packageId">package Id</param>
        /// <param name="version">package version</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public IPackage FindPackage(string packageId, SemanticVersion version, NuGetRequest request) 
        {
            if (string.IsNullOrWhiteSpace(packageId)) {
                return null;
            }

            request.Debug(Messages.DebugInfoCallMethod3, "HttpClientPackageRepository", "FindPackage", packageId);

            var query = packageId.MakeFindPackageByIdQuery(_nugetFindPackageIdQueryFormat);          

            var packages = NuGetClient.FindPackage(query, request);

            //Usually versions has a limited number, ToArray should be ok. 
            var versions = version.GetComparableVersionStrings().ToArray();

            //Will only enumerate packages once
            return packages.FirstOrDefault(package => packageId.Equals(package.Id, StringComparison.OrdinalIgnoreCase) && versions.Contains(package.Version,StringComparer.OrdinalIgnoreCase));  
        }

        /// <summary>
        /// Find-Package bases on the given package Id
        /// </summary>
        /// <param name="packageId">Package Id</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public IEnumerable<IPackage> FindPackagesById(string packageId, NuGetRequest request){

            request.Debug(Messages.DebugInfoCallMethod3, "HttpClientPackageRepository", "FindPackagesById", packageId);

            var query = packageId.MakeFindPackageByIdQuery(_nugetFindPackageIdQueryFormat);

            //request.Verbose(query.ToString());

            return NuGetClient.FindPackage(query, request);
        }

        /// <summary>
        /// Search the entire repository for the case when a user does not provider package name or uses wildcards in the name.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <param name="nugetRequest"></param>
        /// <returns></returns>
        public IEnumerable<IPackage> Search(string searchTerm, NuGetRequest nugetRequest)
        {
            if (nugetRequest == null)
            {
                yield break;
            }

            nugetRequest.Debug(Messages.DebugInfoCallMethod3, "HttpClientPackageRepository", "Search", searchTerm);

            var searchQuery = searchTerm.MakeSearchQuery(_queryUri, nugetRequest.AllowPrereleaseVersions.Value, nugetRequest.AllVersions.Value);

            foreach (var pkg in SendRequest(searchQuery, nugetRequest))
            {
                yield return pkg;
            }
        }

        /// <summary>
        /// Send the request to the server with buffer size to account for the case where there are more data
        /// that we need to fetch
        /// </summary>
        /// <param name="query"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IEnumerable<PackageBase> SendRequest(string query, NuGetRequest request)
        {
            const int bufferSize = 40;
            // number of threads sending the requests
            const int numberOfSenders = 4;

            var startPoint = 0;
            var tasks = new List<Task<Stream>>();

            bool stopSending = false;
            object stopLock = new Object();

            // Send one request first

            // this initial query is of the form http://www.nuget.org/api/v2/FindPackagesById()?id='jquery'&$skip={0}&$top={1}
            UriBuilder initialQuery = new UriBuilder(query.InsertSkipAndTop());

            PackageBase firstPackage = null;

            // Send out an initial request
            // we send out 1 initial request first to check for redirection and check whether repository supports odata
            using (Stream stream = NuGetClient.InitialDownloadDataToStream(initialQuery, startPoint, bufferSize, request))
            {
                if (stream == null)
                {
                    yield break;
                }

                XDocument document = XmlUtility.LoadSafe(stream, ignoreWhiteSpace: true);

                var entries = document.Root.ElementsNoNamespace("entry").ToList();

                // If the initial request has different number of entries than the buffer size, return it because this means the server
                // does not understand odata request or there is no more data. in the former case, we have to stop to prevent infinite loop
                if (entries.Count != bufferSize)
                {
                    request.Debug(Messages.PackagesReceived, entries.Count);
                    stopSending = true;
                }

                foreach (XElement entry in entries)
                {
                    var package = new PackageBase();

                    // set the first package of the request. this is used later to verify that the case when the number of packages in the repository
                    // is the same as the buffer size and the repository does not support odata query. in that case, we want to check whether the first package
                    // exists anywhere in the second call. if it is, then we cancel the request (this is to prevent infinite loop)
                    if (firstPackage == null)
                    {
                        firstPackage = package;
                    }

                    PackageUtility.ReadEntryElement(ref package, entry);
                    yield return package;
                }
            }

            if (stopSending || request.IsCanceled)
            {
                yield break;
            }

            // To avoid more redirection (for example, if the initial query is nuget.org, it will be changed to www.nuget.org

            query = initialQuery.Uri.ToString();

            // Sending the initial requests
            for (var i = 0; i < numberOfSenders; i++)
            {
                // Update the start point to fetch the packages
                startPoint += bufferSize;

                // Get the query
                var newQuery = string.Format(query, startPoint, bufferSize);

                // Send it 
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    Stream items = NuGetClient.DownloadDataToStream(newQuery, request);
                    return items;
                }));
            }

            //Wait for the responses, parse the data, and send to the user
            while (tasks.Count > 0)
            {

                //Cast because the compiler warning: Co-variant array conversion from Task[] to Task[] can cause run-time exception on write operation.
                var index = Task.WaitAny(tasks.Cast<Task>().ToArray());

                using (Stream stream = tasks[index].Result)
                {
                    if (stream == null)
                    {
                        yield break;
                    }

                    XDocument document = XmlUtility.LoadSafe(stream, ignoreWhiteSpace: true);

                    var entries = document.Root.ElementsNoNamespace("entry").ToList();

                    if (entries.Count < bufferSize)
                    {
                        request.Debug(Messages.PackagesReceived, entries.Count);
                        lock (stopLock)
                        {
                            stopSending = true;
                        }
                    }

                    foreach (XElement entry in entries)
                    {
                        var package = new PackageBase();

                        PackageUtility.ReadEntryElement(ref package, entry);

                        if (firstPackage != null)
                        {
                            // check whether first package in the first request exists anywhere in the second request
                            if (string.Equals(firstPackage.GetFullName(), package.GetFullName(), StringComparison.OrdinalIgnoreCase)
                                && string.Equals(firstPackage.Version, package.Version, StringComparison.OrdinalIgnoreCase))
                            {
                                lock (stopLock)
                                {
                                    stopSending = true;
                                }

                                break;
                            }
                        }

                        yield return package;
                    }

                    // we only needs to check for the existence of the first package in the second request. don't need to do for subsequent request
                    if (firstPackage != null)
                    {
                        firstPackage = null;
                    }
                }

                // checks whether we should stop sending requests
                if (!stopSending && !request.IsCanceled)
                {
                    // Make sure nobody else is updating the startPoint
                    lock (stopLock)
                    {
                        // update the startPoint
                        startPoint += bufferSize;
                    }
                    // Make a new request with the new startPoint
                    var newQuery = string.Format(query, startPoint, bufferSize);

                    //Keep sending a request 
                    tasks[index] = (Task.Factory.StartNew(searchQuery =>
                    {
                        var items = NuGetClient.DownloadDataToStream(searchQuery.ToStringSafe(), request);
                        return items;
                    }, newQuery));

                }
                else
                {
                    if (request.IsCanceled)
                    {
                        request.Warning(Messages.RequestCanceled, "HttpClientPackageRepository", "SendRequest");
                        //stop sending request to the remote server
                        stopSending = true;
                    }

                    tasks.RemoveAt(index);
                }
            }

        }

        #region SkipTokenCode
        // This code is commented out because powershellgallery has a bug with skip token. We can uncomment it once that is fixed.
        ///// <summary>
        ///// Create a new task that will automatically create a task and add it to taskCollection
        ///// if there are more links to be downloaded. Otherwise, it will signal to the taskCollection that
        ///// we are not expecting any more results.
        ///// The task will also add any packages that it produced to packageCollection
        ///// </summary>
        ///// <param name="query"></param>
        ///// <param name="taskCollection"></param>
        ///// <param name="packageCollection"></param>
        ///// <returns></returns>
        //private static Task<Stream> CreateDownloadTask(string query, BlockingCollection<Task<Stream>> taskCollection, BlockingCollection<PackageBase> packageCollection,Request request)
        //{
        //    Task<Stream> taskStream = Task.Factory.StartNew(searchQuery =>
        //    {
        //        var items = NuGetClient.DownloadDataToStream(searchQuery.ToStringSafe(), request);
        //        return items;
        //    }, query);

        //    // After the task is done, we check whether we should create a new task
        //    taskStream.ContinueWith(streamTask =>
        //    {
        //        using (Stream stream = streamTask.Result)
        //        {
        //            XDocument document = XmlUtility.LoadSafe(stream, ignoreWhiteSpace: true);

        //            // find the xelement of the form <link rel="next" href="<next link>">
        //            XElement next = document.Root.Elements().FirstOrDefault(e => String.Equals(e.Name.LocalName, "link", StringComparison.OrdinalIgnoreCase)
        //                && e.Attribute("rel") != null
        //                && String.Equals(e.Attribute("rel").Value, "next", StringComparison.OrdinalIgnoreCase)
        //                && e.Attribute("href") != null);

        //            // If there is no next link or the request is cancelled, stop sending the request
        //            if (next == null || request.IsCanceled)
        //            {
        //                // call completeadding to signal that there won't be anymore request
        //                taskCollection.CompleteAdding();
        //            }
        //            else
        //            {
        //                var newQuery = next.Attribute("href").Value;
        //                taskCollection.Add(CreateDownloadTask(newQuery, taskCollection, packageCollection, request));
        //            }

        //            foreach (XElement entry in document.Root.ElementsNoNamespace("entry"))
        //            {
        //                var package = new PackageBase();

        //                PackageUtility.ReadEntryElement(ref package, entry);
        //                packageCollection.Add(package);
        //            }

        //            if (next == null || request.IsCanceled)
        //            {
        //                packageCollection.CompleteAdding();
        //            }
        //        }
        //    });

        //    return taskStream;
        //}

        ///// <summary>
        ///// Send the request to the server. We check whether the response has a next link
        ///// to account for the case where there are more data that we need to fetch
        ///// </summary>
        ///// <param name="query"></param>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //public static IEnumerable<PackageBase> SendRequest(string query, Request request)
        //{
        //    var tasks = new List<Task<Stream>>();

        //    // A blocking collection of task stream.
        //    BlockingCollection<Task<Stream>> taskCollection = new BlockingCollection<Task<Stream>>();
        //    // A blocking collection of packages.
        //    BlockingCollection<PackageBase> packageCollection = new BlockingCollection<PackageBase>();

        //    // Populate the first task
        //    Task<Stream> firstTask = CreateDownloadTask(query, taskCollection, packageCollection, request);

        //    while (!taskCollection.IsCompleted)
        //    {
        //        Task<Stream> streamTask = null;

        //        try
        //        {
        //            streamTask = taskCollection.Take();
        //        }
        //        catch (InvalidOperationException) { }

        //        // Try to yield package from packageCollection
        //        while (!packageCollection.IsCompleted)
        //        {
        //            PackageBase package = null;

        //            try
        //            {
        //                package = packageCollection.Take();
        //            }
        //            catch (InvalidOperationException) { }

        //            if (package != null)
        //            {
        //                yield return package;
        //            }
        //        }
        //    }

        //    // There may be packages in packageCollection
        //    // Try to yield package from packageCollection
        //    while (!packageCollection.IsCompleted)
        //    {
        //        PackageBase package = null;

        //        try
        //        {
        //            package = packageCollection.Take();
        //        }
        //        catch (InvalidOperationException) { }

        //        if (package != null)
        //        {
        //            yield return package;
        //        }
        //    }
        //}
        #endregion
    }
}

