// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventKeyword
**
** Purpose: 
** This public class describes the metadata for a specific Keyword 
** defined by a Provider. An instance of this class is obtained from 
** a ProviderMetadata object.
** 
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Describes the metadata for a specific Keyword defined by a Provider. 
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventKeyword {
        private long value;
        private string name;
        private string displayName;
        private bool dataReady;
        ProviderMetadata pmReference;
        object syncObject;

        //called from EventMetadata
        internal EventKeyword(long value, ProviderMetadata pmReference) {
            this.value = value;
            this.pmReference = pmReference;
            this.syncObject = new object();
        }

        //called from ProviderMetadata
        internal EventKeyword(string name, long value, string displayName) {
            this.value = value;
            this.name = name;
            this.displayName = displayName;
            this.dataReady = true;
            this.syncObject = new object();
        }

        internal void PrepareData() {
            if (dataReady == true) return;

            lock (syncObject) {
                if (dataReady == true) return;

                IEnumerable<EventKeyword> result = pmReference.Keywords;

                this.name = null;
                this.displayName = null;
                this.dataReady = true;

                foreach (EventKeyword key in result) {
                    if (key.Value == this.value) {
                        this.name = key.Name;
                        this.displayName = key.DisplayName;
                        break;
                    }
                }
            }
        }

        public string Name {
            get {
                PrepareData();
                return this.name;
            }
        }

        public long Value {
            get {
                return this.value;
            }
        }

        public string DisplayName {
            get {
                PrepareData();
                return this.displayName;
            }
        }
    }
}
