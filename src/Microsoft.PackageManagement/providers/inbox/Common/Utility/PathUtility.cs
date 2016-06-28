
using System.Security;

namespace Microsoft.PackageManagement.Provider.Utility 
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using System.Globalization;

    internal static class PathUtility
    {      

        internal static string EnsureTrailingSlash(string path)
        {
            //The value of DirectorySeparatorChar is a slash ("/") on UNIX, and a backslash ("\") on the Windows and Macintosh.
            return EnsureTrailingCharacter(path, Path.DirectorySeparatorChar);
        }

        private static string EnsureTrailingCharacter(string path, char trailingCharacter)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            // if the path is empty, we want to return the original string instead of a single trailing character.
            if (path.Length == 0 || path[path.Length - 1] == trailingCharacter)
            {
                return path;
            }
           
            return path + trailingCharacter;
        }

        internal static HttpResponseMessage GetHttpResponse(HttpClient httpClient, string query, Func<bool> isCanceled, Action<string, int>logRetry, Action<string>verbose, Action<string>debug)
        {
            // try downloading for 3 times
            int remainingTry = 3;
            object timerLock = new object();
            Timer timer = null;
            CancellationTokenSource cts = null;
            bool cleanUp = true;

            Action cleanUpAction = () =>
            {
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
                                // dispose it
                                timer.Dispose();
                            }

                            // dispose the token
                            if (cts != null)
                            {
                                cts.Cancel();
                                cts.Dispose();
                            }
                        }
                        catch { }

                        cleanUp = true;
                    }
                }
            };

            while (remainingTry > 0)
            {
                // if user cancel the request, no need to do anything
                if (isCanceled())
                {
                    break;
                }

                try
                {
                    // decrease try by 1
                    remainingTry -= 1;

                    // create new timer and cancellation token source
                    lock (timerLock)
                    {
                        // check every second to see whether request is cancelled
                        timer = new Timer(_ =>
                        {
                            if (isCanceled())
                            {
                                cleanUpAction();
                            }
                        }, null, 500, 1000);

                        cts = new CancellationTokenSource();

                        cleanUp = false;
                    }

                    Task task = httpClient.GetAsync(query, cts.Token);

                    // start the task
                    task.Wait();

                    if (task.IsCompleted && task is Task<HttpResponseMessage>)
                    {
                        var result = (task as Task<HttpResponseMessage>).Result;

                        // if success, returns result
                        if (result.IsSuccessStatusCode)
                        {
                            return result;
                        }

                        // otherwise, we have to retry again
                    }

                    // if request is canceled, don't retry
                    if (isCanceled())
                    {
                        break;
                    }

                    logRetry(query, remainingTry);
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException)
                    {
                        (ex as AggregateException).Handle(singleException =>
                        {
                            // report each of the exception
                            verbose(singleException.Message);
                            debug(singleException.StackTrace);
                            return true;
                        });
                    }
                    else
                    {
                        // single exception, just report the message and stacktrace
                        verbose(ex.Message);
                        debug(ex.StackTrace);
                    }

                    // if there is exception, we will retry too
                    logRetry(query, remainingTry);
                }
                finally
                {
                    cleanUpAction();
                }
            }

            return null;
        }

        
        internal static string UriCombine(string query, string append)
        {
            if (String.IsNullOrWhiteSpace(query)) return append;
            if (String.IsNullOrWhiteSpace(append)) return query;

            return query.TrimEnd('/') + "/" + append.TrimStart('/');
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "It's used in Nano")]
        internal static string SecureStringToString(SecureString secure)
        {
            IntPtr value = IntPtr.Zero;
            try
            {
#if !CORECLR
                value = Marshal.SecureStringToCoTaskMemUnicode(secure);
#else
                value = SecureStringMarshal.SecureStringToCoTaskMemUnicode(secure);
#endif
                return Marshal.PtrToStringUni(value);
            }
            finally
            {
#if !CORECLR
                Marshal.ZeroFreeGlobalAllocUnicode(value);
#else
                Marshal.ZeroFreeCoTaskMemUnicode(value);
#endif
            }
        }

        internal static NetworkCredential GetNetworkCredential(string username, SecureString password)
        {
            // if request has username and password, use that
            if (!string.IsNullOrWhiteSpace(username) && password != null)
            {
#if CORECLR
                // networkcredential class on coreclr does not accept securestring so we have to convert
                return new NetworkCredential(username, SecureStringToString(password));
#else
                return new NetworkCredential(username, password);
#endif
            }

            // if no user name and password, returns null
            return null;
        }

        internal static HttpClient GetHttpClientHelper(string username, SecureString password, IWebProxy webProxy)
        {
            var clientHandler = new HttpClientHandler();

            var networkCredential = GetNetworkCredential(username, password);

            // if we are given a network credential, use that
            if (networkCredential != null)
            {
                // else use the one given to us
                clientHandler.Credentials = networkCredential;
                clientHandler.PreAuthenticate = true;
            }
            else
            {
                clientHandler.UseDefaultCredentials = true;
            }

            // do not need to set proxy of httpClient or httpClientHandler because it will use system proxy setting by default
            // discussion here (https://github.com/dotnet/corefx/issues/7037)

            if (webProxy != null)
            {
                // if webproxy is not null, use that
                clientHandler.Proxy = webProxy;
            }

            var httpClient = new HttpClient(clientHandler);

            // Mozilla/5.0 is the general token that says the browser is Mozilla compatible, and is common to almost every browser today.
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 NuGet");

            return httpClient;
        }

    }
}
