// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventTask
**
** Purpose: 
** This public class describes the metadata for a specific Task 
** defined by a Provider. An instance of this class is obtained  
** from a ProviderMetadata object.
** 
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader {

    /// <summary>
    /// Describes the metadata for a specific Task defined by a Provider. 
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventTask {
        private int value;
        private string name;
        private string displayName;
        private Guid guid;
        private bool dataReady;
        ProviderMetadata pmReference;
        object syncObject;


        //called from EventMetadata
        internal EventTask(int value, ProviderMetadata pmReference) {
            this.value = value;
            this.pmReference = pmReference;
            this.syncObject = new object();
        }

        //called from ProviderMetadata
        internal EventTask(string name, int value, string displayName, Guid guid) {
            this.value = value;
            this.name = name;
            this.displayName = displayName;
            this.guid = guid;
            this.dataReady = true;
            this.syncObject = new object();
        }

        internal void PrepareData() {
            lock (syncObject) {
                if (dataReady == true) return;

                IEnumerable<EventTask> result = pmReference.Tasks;

                this.name = null;
                this.displayName = null;
                this.guid = Guid.Empty;
                this.dataReady = true;

                foreach (EventTask task in result) {
                    if (task.Value == this.value) {
                        this.name = task.Name;
                        this.displayName = task.DisplayName;
                        this.guid = task.EventGuid;
                        this.dataReady = true;
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

        public Guid EventGuid {
            get {
                PrepareData();
                return this.guid;
            }
        }
    }

}
