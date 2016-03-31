// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventLevel
**
** Purpose: 
** This public class describes the metadata for a specific Level 
** defined by a Provider. An instance of this class is obtained from 
** a ProviderMetadata object.
** 
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Describes the metadata for a specific Level defined by a Provider. 
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventLevel {

        private int value;
        private string name;
        private string displayName;
        private bool dataReady;
        ProviderMetadata pmReference;
        object syncObject;

        //called from EventMetadata 
        internal EventLevel(int value, ProviderMetadata pmReference) {
            this.value = value;
            this.pmReference = pmReference;
            this.syncObject = new object();
        }

        //called from ProviderMetadata
        internal EventLevel(string name, int value, string displayName) {
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

                IEnumerable<EventLevel> result = pmReference.Levels;
                this.name = null;
                this.displayName = null;
                this.dataReady = true;
                foreach (EventLevel lev in result) {
                    if (lev.Value == this.value) {
                        this.name = lev.Name;
                        this.displayName = lev.DisplayName;
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

        public int Value {
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
