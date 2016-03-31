// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLogLink
**
** Purpose: 
** This public class describes the metadata for a specific Log 
** Reference defined by a Provider. An instance of this class is obtained from 
** a ProviderMetadata object.
**
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Describes the metadata for a specific Log Reference defined
    /// by a Provider. An instance of this class is obtained from 
    /// a ProviderMetadata object.
    /// </summary>
    public sealed class EventLogLink {
        private string channelName;
        private bool isImported;
        private string displayName;
        private uint channelId;

        private bool dataReady;
        ProviderMetadata pmReference;
        object syncObject;

        internal EventLogLink(uint channelId, ProviderMetadata pmReference) {
            this.channelId = channelId;
            this.pmReference = pmReference;
            this.syncObject = new object();
        }

        internal EventLogLink(string channelName, bool isImported, string displayName, uint channelId) {
            this.channelName = channelName;
            this.isImported = isImported;
            this.displayName = displayName;
            this.channelId = channelId;

            this.dataReady = true;
            this.syncObject = new object();
        }

        private void PrepareData() {
            if (dataReady == true) return;

            lock (syncObject) {
                if (dataReady == true) return;

                IEnumerable<EventLogLink> result = pmReference.LogLinks;

                this.channelName = null;
                this.isImported = false;
                this.displayName = null;
                this.dataReady = true;

                foreach (EventLogLink ch in result) {
                    if (ch.ChannelId == this.channelId) {
                        this.channelName = ch.LogName;
                        this.isImported = ch.IsImported;
                        this.displayName = ch.DisplayName;

                        this.dataReady = true;

                        break;
                    }
                }
            }
        }


        public string LogName {
            get {
                this.PrepareData();
                return this.channelName;
            }
        }

        public bool IsImported {
            get {
                this.PrepareData();
                return this.isImported;
            }
        }

        public string DisplayName {
            get {
                this.PrepareData();
                return this.displayName;
            }
        }

        internal uint ChannelId {
            get {
                return channelId;
            }
        }
    }

}
