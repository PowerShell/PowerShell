// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: ProviderMetadataCachedInformation
**
** Purpose: 
** This internal class exposes a limited set of cached Provider 
** metadata information.   It is meant to support the Metadata 
** 
============================================================*/

using System.Globalization;
using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    // 
    // this class does not expose underlying Provider metadata objects.  Instead it 
    // exposes a limited set of Provider metadata information from the cache.  The reason
    // for this is so the cache can easily Dispose the metadata object without worrying 
    // about who is using it.
    //
    internal class ProviderMetadataCachedInformation {

        private Dictionary<ProviderMetadataId, CacheItem> cache;
        private int maximumCacheSize;
        private EventLogSession session;
        private string logfile;

        private class ProviderMetadataId {
            private string providerName;
            private CultureInfo cultureInfo;

            public ProviderMetadataId(string providerName, CultureInfo cultureInfo) {
                this.providerName = providerName;
                this.cultureInfo = cultureInfo;
            }

            public override bool Equals(object obj) {
                ProviderMetadataId rhs = obj as ProviderMetadataId;
                if (rhs == null) return false;
                if (this.providerName.Equals(rhs.providerName) && (cultureInfo == rhs.cultureInfo))
                    return true;
                return false;
            }

            public override int GetHashCode() {

                return this.providerName.GetHashCode() ^ cultureInfo.GetHashCode();

            }

            public string ProviderName {
                get {
                    return providerName;
                }
            }
            public CultureInfo TheCultureInfo {
                get {
                    return cultureInfo;
                }
            }
        }

        private class CacheItem {
            private ProviderMetadata pm;
            private DateTime theTime;

            public CacheItem(ProviderMetadata pm) {
                this.pm = pm;
                theTime = DateTime.Now;
            }

            public DateTime TheTime {
                get {
                    return theTime;
                }
                set {
                    theTime = value;
                }
            }

            public ProviderMetadata ProviderMetadata {
                get {
                    return pm;
                }
            }
        }

        public ProviderMetadataCachedInformation(EventLogSession session, string logfile, int maximumCacheSize) {
            Debug.Assert(session != null);
            this.session = session;
            this.logfile = logfile;
            cache = new Dictionary<ProviderMetadataId, CacheItem>();
            this.maximumCacheSize = maximumCacheSize;
        }

        private bool IsCacheFull() {
            return cache.Count == maximumCacheSize;
        }

        private bool IsProviderinCache(ProviderMetadataId key) {
            return cache.ContainsKey(key);
        }

        private void DeleteCacheEntry(ProviderMetadataId key) {
            if (!IsProviderinCache(key))
                return;

            CacheItem value = cache[key];
            cache.Remove(key);

            value.ProviderMetadata.Dispose();
        }


        private void AddCacheEntry(ProviderMetadataId key, ProviderMetadata pm) {
            if (IsCacheFull())
                FlushOldestEntry();

            CacheItem value = new CacheItem(pm);
            cache.Add(key, value);
            return;
        }

        private void FlushOldestEntry() {
            double maxPassedTime = -10;
            DateTime timeNow = DateTime.Now;
            ProviderMetadataId keyToDelete = null;

            //get the entry in the cache which was not accessed for the longest time.
            foreach (KeyValuePair<ProviderMetadataId, CacheItem> kvp in cache) {
                //the time difference (in ms) between the timeNow and the last used time of each entry
                TimeSpan timeDifference = timeNow.Subtract(kvp.Value.TheTime);

                //for the "unused" items (with ReferenceCount == 0)   -> can possible be deleted.
                if (timeDifference.TotalMilliseconds >= maxPassedTime) {
                    maxPassedTime = timeDifference.TotalMilliseconds;
                    keyToDelete = kvp.Key;
                }
            }

            if (keyToDelete != null)
                DeleteCacheEntry(keyToDelete);
        }

        private static void UpdateCacheValueInfoForHit(CacheItem cacheItem) {
            cacheItem.TheTime = DateTime.Now;
        }

        private ProviderMetadata GetProviderMetadata(ProviderMetadataId key) {
            if (!IsProviderinCache(key)) {
                ProviderMetadata pm;
                try {
                    pm = new ProviderMetadata(key.ProviderName, this.session, key.TheCultureInfo, this.logfile);
                }
                catch (EventLogNotFoundException) {
                    pm = new ProviderMetadata(key.ProviderName, this.session, key.TheCultureInfo);
                }
                AddCacheEntry(key, pm);
                return pm;
            }
            else {
                CacheItem cacheItem = cache[key];

                ProviderMetadata pm = cacheItem.ProviderMetadata;

                //
                // check Provider metadata to be sure it's hasn't been
                // uninstalled since last time it was used.
                //

                try {
                    pm.CheckReleased();
                    UpdateCacheValueInfoForHit(cacheItem);
                }
                catch (EventLogException) {
                    DeleteCacheEntry(key);
                    try {
                        pm = new ProviderMetadata(key.ProviderName, this.session, key.TheCultureInfo, this.logfile);
                    }
                    catch (EventLogNotFoundException) {
                        pm = new ProviderMetadata(key.ProviderName, this.session, key.TheCultureInfo);
                    }
                    AddCacheEntry(key, pm);
                }

                return pm;
            }
        }

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        public string GetFormatDescription(string ProviderName,  EventLogHandle eventHandle) {

            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);

                try {
                    ProviderMetadata pm = GetProviderMetadata(key);
                    return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle,  UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageEvent);
                }
                catch (EventLogNotFoundException) {
                    return null;
                }
            }
        }

        public string GetFormatDescription(string ProviderName, EventLogHandle eventHandle, string[] values) {

            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                try {
                    return NativeWrapper.EvtFormatMessageFormatDescription(pm.Handle, eventHandle, values);
                }
                catch (EventLogNotFoundException) {
                    return null;
                }
            }
        }

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        public string GetLevelDisplayName(string ProviderName, EventLogHandle eventHandle) {
            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle,  UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageLevel);
            }
        }

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        public string GetOpcodeDisplayName(string ProviderName, EventLogHandle eventHandle) {
            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle,  UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageOpcode);
            }
        }

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        public string GetTaskDisplayName(string ProviderName, EventLogHandle eventHandle) {
            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderName(pm.Handle, eventHandle,  UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageTask);
            }
        }

        // marking as TreatAsSafe because just passing around a reference to an EventLogHandle is safe.
        [System.Security.SecuritySafeCritical]
        public IEnumerable<string> GetKeywordDisplayNames(string ProviderName, EventLogHandle eventHandle) {
            lock (this) {
                ProviderMetadataId key = new ProviderMetadataId(ProviderName, CultureInfo.CurrentCulture);
                ProviderMetadata pm = GetProviderMetadata(key);
                return NativeWrapper.EvtFormatMessageRenderKeywords(pm.Handle, eventHandle,  UnsafeNativeMethods.EvtFormatMessageFlags.EvtFormatMessageKeyword);
            }
        }
    }

}