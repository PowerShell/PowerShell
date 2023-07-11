// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Queue to provide sliding window capabilities for auto size functionality
    /// It provides caching capabilities (either the first N objects in a group
    /// or all the objects in a group)
    /// </summary>
    internal sealed class OutputGroupQueue
    {
        /// <summary>
        /// Create a grouping cache.
        /// </summary>
        /// <param name="callBack">Notification callback to be called when the desired number of objects is reached.</param>
        /// <param name="objectCount">Max number of objects to be cached.</param>
        internal OutputGroupQueue(FormattedObjectsCache.ProcessCachedGroupNotification callBack, int objectCount)
        {
            _notificationCallBack = callBack;
            _objectCount = objectCount;
        }

        /// <summary>
        /// Create a time-bounded grouping cache.
        /// </summary>
        /// <param name="callBack">Notification callback to be called when the desired number of objects is reached.</param>
        /// <param name="groupingDuration">Max amount of time to cache of objects.</param>
        internal OutputGroupQueue(FormattedObjectsCache.ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            _notificationCallBack = callBack;
            _groupingDuration = groupingDuration;
        }

        /// <summary>
        /// Add an object to the cache.
        /// </summary>
        /// <param name="o">Object to add.</param>
        /// <returns>Objects the cache needs to return. It can be null.</returns>
        internal List<PacketInfoData> Add(PacketInfoData o)
        {
            if (o is FormatStartData fsd)
            {
                // just cache the reference (used during the notification call)
                _formatStartData = fsd;
            }

            UpdateObjectCount(o);

            // STATE TRANSITION: we are not processing and we start
            if (!_processingGroup && (o is GroupStartData))
            {
                // just set the flag and start caching
                _processingGroup = true;
                _currentObjectCount = 0;

                if (_groupingDuration > TimeSpan.MinValue)
                {
                    _groupingTimer = Stopwatch.StartNew();
                }

                _queue.Enqueue(o);
                return null;
            }

            // STATE TRANSITION: we are processing and we stop
            if (_processingGroup &&
                ((o is GroupEndData) ||
                (_objectCount > 0) && (_currentObjectCount >= _objectCount)) ||
                ((_groupingTimer != null) && (_groupingTimer.Elapsed > _groupingDuration))
                )
            {
                // reset the object count
                _currentObjectCount = 0;

                if (_groupingTimer != null)
                {
                    _groupingTimer.Stop();
                    _groupingTimer = null;
                }

                // add object to queue, to be picked up
                _queue.Enqueue(o);

                // we are at the end of a group, drain the queue
                Notify();
                _processingGroup = false;

                List<PacketInfoData> retVal = new List<PacketInfoData>();

                while (_queue.Count > 0)
                {
                    retVal.Add(_queue.Dequeue());
                }

                return retVal;
            }

            // NO STATE TRANSITION: check the state we are in
            if (_processingGroup)
            {
                // we are in the caching state
                _queue.Enqueue(o);
                return null;
            }

            // we are not processing, so just return it
            List<PacketInfoData> ret = new List<PacketInfoData>();

            ret.Add(o);
            return ret;
        }

        private void UpdateObjectCount(PacketInfoData o)
        {
            // add only of it's not a control message
            // and it's not out of band
            if (o is FormatEntryData fed && !fed.outOfBand)
            {
                _currentObjectCount++;
            }
        }

        private void Notify()
        {
            if (_notificationCallBack == null)
                return;

            // filter out the out of band data, since they do not participate in the
            // auto resize algorithm
            List<PacketInfoData> validObjects = new List<PacketInfoData>();

            foreach (PacketInfoData x in _queue)
            {
                if (x is FormatEntryData fed && fed.outOfBand)
                    continue;

                validObjects.Add(x);
            }

            _notificationCallBack(_formatStartData, validObjects);
        }

        /// <summary>
        /// Remove a single object from the queue.
        /// </summary>
        /// <returns>Object retrieved, null if queue is empty.</returns>
        internal PacketInfoData Dequeue()
        {
            if (_queue.Count == 0)
                return null;

            return _queue.Dequeue();
        }

        /// <summary>
        /// Queue to store the currently cached objects.
        /// </summary>
        private readonly Queue<PacketInfoData> _queue = new Queue<PacketInfoData>();

        /// <summary>
        /// Number of objects to compute the best fit.
        /// Zero: all the objects
        /// a positive number N: use the first N.
        /// </summary>
        private readonly int _objectCount = 0;

        /// <summary>
        /// Maximum amount of time for record processing to compute the best fit.
        /// MaxValue: all the objects.
        /// A positive timespan: use all objects that have been processed within the timeframe.
        /// </summary>
        private readonly TimeSpan _groupingDuration = TimeSpan.MinValue;
        private Stopwatch _groupingTimer = null;

        /// <summary>
        /// Notification callback to be called when we have accumulated enough
        /// data to compute a hint.
        /// </summary>
        private readonly FormattedObjectsCache.ProcessCachedGroupNotification _notificationCallBack = null;

        /// <summary>
        /// Reference kept to be used during notification.
        /// </summary>
        private FormatStartData _formatStartData = null;

        /// <summary>
        /// State flag to signal we are queuing.
        /// </summary>
        private bool _processingGroup = false;

        /// <summary>
        /// Current object count.
        /// </summary>
        private int _currentObjectCount = 0;
    }

    /// <summary>
    /// Facade class managing the front end and the autosize cache.
    /// </summary>
    internal sealed class FormattedObjectsCache
    {
        /// <summary>
        /// Delegate to allow notifications when the autosize queue is about to be drained.
        /// </summary>
        /// <param name="formatStartData">Current Fs control message.</param>
        /// <param name="objects">Enumeration of PacketInfoData objects.</param>
        internal delegate void ProcessCachedGroupNotification(FormatStartData formatStartData, List<PacketInfoData> objects);

        /// <summary>
        /// Decide right away if we need a front end cache (e.g. printing)
        /// </summary>
        /// <param name="cacheFrontEnd">If true, create a front end cache object.</param>
        internal FormattedObjectsCache(bool cacheFrontEnd)
        {
            if (cacheFrontEnd)
                _frontEndQueue = new Queue<PacketInfoData>();
        }

        /// <summary>
        /// If needed, add a back end autosize (grouping) cache.
        /// </summary>
        /// <param name="callBack">Notification callback to be called when the desired number of objects is reached.</param>
        /// <param name="objectCount">Max number of objects to be cached.</param>
        internal void EnableGroupCaching(ProcessCachedGroupNotification callBack, int objectCount)
        {
            if (callBack != null)
                _groupQueue = new OutputGroupQueue(callBack, objectCount);
        }

        /// <summary>
        /// If needed, add a back end autosize (grouping) cache.
        /// </summary>
        /// <param name="callBack">Notification callback to be called when the desired number of objects is reached.</param>
        /// <param name="groupingDuration">Max amount of time to cache of objects.</param>
        internal void EnableGroupCaching(ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            if (callBack != null)
                _groupQueue = new OutputGroupQueue(callBack, groupingDuration);
        }

        /// <summary>
        /// Add an object to the cache. the behavior depends on the object added, the
        /// objects already in the cache and the cache settings.
        /// </summary>
        /// <param name="o">Object to add.</param>
        /// <returns>List of objects the cache is flushing.</returns>
        internal List<PacketInfoData> Add(PacketInfoData o)
        {
            // if neither there, pass thru
            if (_frontEndQueue == null && _groupQueue == null)
            {
                List<PacketInfoData> retVal = new List<PacketInfoData>();
                retVal.Add(o);
                return retVal;
            }

            // if front present, add to front
            if (_frontEndQueue != null)
            {
                _frontEndQueue.Enqueue(o);
                return null;
            }

            // if back only, add to back
            return _groupQueue.Add(o);
        }

        /// <summary>
        /// Remove all the objects from the cache.
        /// </summary>
        /// <returns>All the objects that were in the cache.</returns>
        internal List<PacketInfoData> Drain()
        {
            // if neither there,we did not cache at all
            if (_frontEndQueue == null && _groupQueue == null)
            {
                return null;
            }

            List<PacketInfoData> retVal = new List<PacketInfoData>();

            if (_frontEndQueue != null)
            {
                if (_groupQueue == null)
                {
                    // drain the front queue and return the data
                    while (_frontEndQueue.Count > 0)
                        retVal.Add(_frontEndQueue.Dequeue());

                    return retVal;
                }

                // move from the front to the back queue
                while (_frontEndQueue.Count > 0)
                {
                    List<PacketInfoData> groupQueueOut = _groupQueue.Add(_frontEndQueue.Dequeue());

                    if (groupQueueOut != null)
                        foreach (PacketInfoData x in groupQueueOut)
                            retVal.Add(x);
                }
            }

            // drain the back queue
            while (true)
            {
                PacketInfoData obj = _groupQueue.Dequeue();

                if (obj == null)
                    break;

                retVal.Add(obj);
            }

            return retVal;
        }

        /// <summary>
        /// Front end queue (if present, cache ALL, if not, bypass)
        /// </summary>
        private readonly Queue<PacketInfoData> _frontEndQueue;

        /// <summary>
        /// Back end grouping queue.
        /// </summary>
        private OutputGroupQueue _groupQueue = null;
    }
}
