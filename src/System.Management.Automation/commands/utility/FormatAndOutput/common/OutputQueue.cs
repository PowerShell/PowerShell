/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Internal.Format
{

    /// <summary>
    /// queue to provide sliding window capabilities for auto size functionality
    /// It provides caching capabilities (either the first N objects in a group
    /// or all the objects in a group)
    /// </summary>
    internal sealed class OutputGroupQueue
    {
        /// <summary>
        /// create a grouping cache
        /// </summary>
        /// <param name="callBack">notification callback to be called when the desired number of objects is reached</param>
        /// <param name="objectCount">max number of objects to be cached</param>
        internal OutputGroupQueue (FormattedObjectsCache.ProcessCachedGroupNotification callBack, int objectCount)
        {
            this.notificationCallBack = callBack;
            this.objectCount = objectCount;
        }

        /// <summary>
        /// create a time-bounded grouping cache
        /// </summary>
        /// <param name="callBack">notification callback to be called when the desired number of objects is reached</param>
        /// <param name="groupingDuration">max amount of time to cache of objects</param>
        internal OutputGroupQueue(FormattedObjectsCache.ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            this.notificationCallBack = callBack;
            this.groupingDuration = groupingDuration;
        }


        /// <summary>
        /// add an object to the cache
        /// </summary>
        /// <param name="o">object to add</param>
        /// <returns>objects the cache needs to return. It can be null</returns>
        internal List<PacketInfoData> Add (PacketInfoData o)
        {
            FormatStartData fsd = o as FormatStartData;
            if (fsd != null)
            {
                // just cache the reference (used during the notification call)
                formatStartData = fsd;
            }

            UpdateObjectCount (o);

            // STATE TRANSITION: we are not processing and we start
            if (!processingGroup && (o is GroupStartData))
            {
                // just set the flag and start caching
                processingGroup = true;
                this.currentObjectCount = 0;

                if (this.groupingDuration > TimeSpan.MinValue)
                {
                    this.groupingTimer = Stopwatch.StartNew();
                }

                queue.Enqueue (o);
                return null;
            }

            // STATE TRANSITION: we are processing and we stop
            if (processingGroup &&
                ((o is GroupEndData) ||
                (this.objectCount > 0) && (this.currentObjectCount >= this.objectCount)) ||
                ((this.groupingTimer != null) && (this.groupingTimer.Elapsed > this.groupingDuration))
                )
            {
                // reset the object count
                this.currentObjectCount = 0;

                if(this.groupingTimer != null)
                {
                    this.groupingTimer.Stop();
                    this.groupingTimer = null;
                }

                // add object to queue, to be picked up
                queue.Enqueue (o);

                // we are at the end of a group, drain the queue
                Notify ();
                processingGroup = false;

                List<PacketInfoData> retVal = new List<PacketInfoData> ();

                while (this.queue.Count > 0)
                {
                    retVal.Add (queue.Dequeue ());
                }

                return retVal;
            }

            // NO STATE TRANSITION: check the state we are in
            if (processingGroup)
            {
                // we are in the caching state
                queue.Enqueue (o);
                return null;
            }

            // we are not processing, so just return it
            List<PacketInfoData> ret = new List<PacketInfoData> ();

            ret.Add (o);
            return ret;
        }

        private void UpdateObjectCount (PacketInfoData o)
        {
            // add only of it's not a control message
            // and it's not out of band
            FormatEntryData fed = o as FormatEntryData;

            if (fed == null || fed.outOfBand)
                return;

            this.currentObjectCount++;
        }

        private void Notify ()
        {
            if (notificationCallBack == null)
                return;

            // filter out the out of band data, since they do not participate in the
            // auto resize algorithm
            List<PacketInfoData> validObjects = new List<PacketInfoData> ();

            foreach (PacketInfoData x in this.queue)
            {
                FormatEntryData fed = x as FormatEntryData;
                if (fed != null && fed.outOfBand)
                    continue;

                validObjects.Add (x);
            }

            notificationCallBack (this.formatStartData, validObjects);
        }

        /// <summary>
        /// remove a single object from the queue
        /// </summary>
        /// <returns>object retrieved, null if queue is empty</returns>
        internal PacketInfoData Dequeue ()
        {
            if (queue.Count == 0)
                return null;

            return queue.Dequeue ();
        }

        /// <summary>
        /// queue to store the currently cached objects
        /// </summary>
        private Queue<PacketInfoData> queue = new Queue<PacketInfoData> ();

        /// <summary>
        /// number of objects to compute the best fit.
        /// Zero: all the objects
        /// a positive number N: use the first N
        /// </summary>
        private int objectCount = 0;

        /// <summary>
        /// Maximum amount of time for record processing to compute the best fit.
        /// MaxValue: all the objects.
        /// A positive timespan: use all objects that have been processed within the timeframe.
        /// </summary>
        private TimeSpan groupingDuration = TimeSpan.MinValue;
        private Stopwatch groupingTimer = null;

        /// <summary>
        /// notification callback to be called when we have accumulated enough
        /// data to compute a hint
        /// </summary>
        private FormattedObjectsCache.ProcessCachedGroupNotification notificationCallBack = null;

        /// <summary>
        /// reference kept to be used during notification
        /// </summary>
        private FormatStartData formatStartData = null;

        /// <summary>
        /// state flag to signal we are queuing
        /// </summary>
        private bool processingGroup = false;

        /// <summary>
        /// current object count
        /// </summary>
        private int currentObjectCount = 0;
    }

    /// <summary>
    /// facade class managing the front end and the autosize cache
    /// </summary>
    internal sealed class FormattedObjectsCache
    {
        /// <summary>
        /// delegate to allow notifications when the autosize queue is about to be drained
        /// </summary>
        /// <param name="formatStartData"> current Fs control message</param>
        /// <param name="objects">enumeration of PacketInfoData objects </param>
        internal delegate void ProcessCachedGroupNotification (FormatStartData formatStartData, List<PacketInfoData> objects);

        /// <summary>
        /// decide right away if we need a front end cache (e.g. printing)
        /// </summary>
        /// <param name="cacheFrontEnd">if true, create a front end cache object</param>
        internal FormattedObjectsCache (bool cacheFrontEnd)
        {
            if (cacheFrontEnd)
                this.frontEndQueue = new Queue<PacketInfoData> ();
        }

        /// <summary>
        /// if needed, add a back end autosize (grouping) cache
        /// </summary>
        /// <param name="callBack">notification callback to be called when the desired number of objects is reached</param>
        /// <param name="objectCount">max number of objects to be cached</param>
        internal void EnableGroupCaching (ProcessCachedGroupNotification callBack, int objectCount)
        {
            if (callBack != null)
                this.groupQueue = new OutputGroupQueue (callBack, objectCount);
        }

        /// <summary>
        /// if needed, add a back end autosize (grouping) cache
        /// </summary>
        /// <param name="callBack">notification callback to be called when the desired number of objects is reached</param>
        /// <param name="groupingDuration">max amount of time to cache of objects</param>
        internal void EnableGroupCaching(ProcessCachedGroupNotification callBack, TimeSpan groupingDuration)
        {
            if (callBack != null)
                this.groupQueue = new OutputGroupQueue(callBack, groupingDuration);
        }

        /// <summary>
        /// add an object to the cache. the behavior depends on the object added, the 
        /// objects already in the cache and the cache settings
        /// </summary>
        /// <param name="o">object to add</param>
        /// <returns>list of objects the cache is flushing</returns>
        internal List<PacketInfoData> Add (PacketInfoData o)
        {
            // if neither there, pass thru
            if (this.frontEndQueue == null && this.groupQueue == null)
            {
                List<PacketInfoData> retVal = new List<PacketInfoData> ();
                retVal.Add (o);
                return retVal;
            }

            // if front present, add to front
            if (this.frontEndQueue != null)
            {
                this.frontEndQueue.Enqueue (o);
                return null;
            }

            // if back only, add to back
            return this.groupQueue.Add (o);
        }

        /// <summary>
        /// remove all the objects from the cache
        /// </summary>
        /// <returns>all the objects that were in the cache</returns>
        internal List<PacketInfoData> Drain ()
        {
            // if neither there,we did not cache at all
            if (this.frontEndQueue == null && this.groupQueue == null)
            {
                return null;
            }

            List<PacketInfoData> retVal = new List<PacketInfoData> ();

            if (this.frontEndQueue != null)
            {
                if (this.groupQueue == null)
                {
                    // drain the front queue and return the data
                    while (this.frontEndQueue.Count > 0)
                        retVal.Add (this.frontEndQueue.Dequeue ());

                    return retVal;
                }

                // move from the front to the back queue
                while (this.frontEndQueue.Count > 0)
                {
                    List<PacketInfoData> groupQueueOut = this.groupQueue.Add (this.frontEndQueue.Dequeue ());

                    if (groupQueueOut != null)
                        foreach (PacketInfoData x in groupQueueOut)
                            retVal.Add (x);
                }
            }

            // drain the back queue
            while (true)
            {
                PacketInfoData obj = this.groupQueue.Dequeue ();

                if (obj == null)
                    break;

                retVal.Add (obj);
            }

            return retVal;
        }


        /// <summary>
        /// front end queue (if present, cache ALL, if not, bypass)
        /// </summary>
        private Queue<PacketInfoData> frontEndQueue;

        /// <summary>
        /// back end grouping queue
        /// </summary>
        private OutputGroupQueue groupQueue = null;
    }
}

