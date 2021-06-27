// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The ServerDispatchTable class.
    /// </summary>
    internal class ServerDispatchTable : DispatchTable<RemoteHostResponse>
    {
        // DispatchTable specialized for RemoteHostResponse.
    }

    /// <summary>
    /// Provides a thread-safe dictionary that maps call-ids to AsyncData objects.
    /// When a thread tries to do a get on a hashtable key (callId) that has not been
    /// set it is blocked. Once the key's value is set the thread is released. This is
    /// used to synchronize server calls with their responses.
    ///
    /// This code needs to be thread-safe. The locking convention is that only the
    /// internal or public methods use locks and are thread-safe. The private methods
    /// do not use locks and are not thread-safe (unless called by the internal and
    /// public methods). If the private methods becomes internal or public
    /// please review the locking.
    /// </summary>
    internal class DispatchTable<T> where T : class
    {
        /// <summary>
        /// Response async objects.
        /// </summary>
        private readonly Dictionary<long, AsyncObject<T>> _responseAsyncObjects = new Dictionary<long, AsyncObject<T>>();

        /// <summary>
        /// Next call id.
        /// </summary>
        private long _nextCallId = 0;

        /// <summary>
        /// Void call id.
        /// </summary>
        internal const long VoidCallId = -100;

        /// <summary>
        /// Create new call id.
        /// </summary>
        internal long CreateNewCallId()
        {
            // Note: Only CreateNewCallId adds new records.

            long callId = Interlocked.Increment(ref _nextCallId);
            AsyncObject<T> responseAsyncObject = new AsyncObject<T>();
            lock (_responseAsyncObjects)
            {
                _responseAsyncObjects[callId] = responseAsyncObject;
            }

            return callId;
        }

        /// <summary>
        /// Get response async object.
        /// </summary>
        private AsyncObject<T> GetResponseAsyncObject(long callId)
        {
            AsyncObject<T> responseAsyncObject = null;
            Dbg.Assert(_responseAsyncObjects.ContainsKey(callId), "Expected _responseAsyncObjects.ContainsKey(callId)");
            responseAsyncObject = _responseAsyncObjects[callId];
            Dbg.Assert(responseAsyncObject != null, "Expected responseAsyncObject != null");
            return responseAsyncObject;
        }

        /// <summary>
        /// Waits for response PSObject to be set and then returns it. Returns null
        /// if wait was aborted.
        /// </summary>
        /// <param name="callId">
        /// </param>
        /// <param name="defaultValue">
        /// default return value (in case the remote end did not send response).
        /// </param>
        internal T GetResponse(long callId, T defaultValue)
        {
            // Note: Only GetResponse removes records.

            AsyncObject<T> responseAsyncObject = null;
            lock (_responseAsyncObjects)
            {
                responseAsyncObject = GetResponseAsyncObject(callId);
            }

            // This will block until Value is set on this AsyncObject.
            T remoteHostResponse = responseAsyncObject.Value;

            // Remove table entry to conserve memory: this table could be alive for a long time.
            lock (_responseAsyncObjects)
            {
                _responseAsyncObjects.Remove(callId);
            }

            // return caller specified value in case there is no response
            // from remote end.
            if (remoteHostResponse == null)
            {
                return defaultValue;
            }

            return remoteHostResponse;
        }

        /// <summary>
        /// Set response.
        /// </summary>
        internal void SetResponse(long callId, T remoteHostResponse)
        {
            Dbg.Assert(remoteHostResponse != null, "Expected remoteHostResponse != null");
            lock (_responseAsyncObjects)
            {
                // The response-async-object might not exist if the call was aborted by Ctrl-C or if
                // the call had a void return and no return value was expected.
                if (!_responseAsyncObjects.ContainsKey(callId))
                {
                    return;
                }

                // Unblock the AsyncObject by setting its value.
                AsyncObject<T> responseAsyncObject = GetResponseAsyncObject(callId);
                responseAsyncObject.Value = remoteHostResponse;
            }
        }

        /// <summary>
        /// Abort call.
        /// </summary>
        private void AbortCall(long callId)
        {
            // The response-async-object might not exist if the call was already aborted.
            if (!_responseAsyncObjects.ContainsKey(callId))
            {
                return;
            }

            // Releases blocked thread by setting null as return value, which should be detected by caller of GetResponse.
            AsyncObject<T> responseAsyncObject = GetResponseAsyncObject(callId);
            responseAsyncObject.Value = null;
        }

        /// <summary>
        /// Abort calls.
        /// </summary>
        private void AbortCalls(List<long> callIds)
        {
            // Releases blocked thread by setting null as return value, which should be detected by caller of GetResponse.
            foreach (long callId in callIds)
            {
                AbortCall(callId);
            }
        }

        /// <summary>
        /// Get all calls.
        /// </summary>
        private List<long> GetAllCalls()
        {
            // Gets all the callIds that are waiting on calls.
            List<long> callIds = new List<long>();
            foreach (KeyValuePair<long, AsyncObject<T>> callIdResponseAsyncObjectPair in _responseAsyncObjects)
            {
                callIds.Add(callIdResponseAsyncObjectPair.Key);
            }

            return callIds;
        }

        /// <summary>
        /// Abort all calls.
        /// </summary>
        internal void AbortAllCalls()
        {
            lock (_responseAsyncObjects)
            {
                List<long> callIds = GetAllCalls();
                AbortCalls(callIds);
            }
        }
    }
}
