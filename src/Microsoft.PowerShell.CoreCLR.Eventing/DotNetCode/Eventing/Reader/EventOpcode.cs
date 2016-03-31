// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventOpcode
**
** Purpose: 
** This public class describes the metadata for a specific Opcode 
** defined by a Provider. An instance of this class is obtained from 
** a ProviderMetadata object.
** 
============================================================*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader {
    
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
    public sealed class EventOpcode {
        private int value;
        private string name;
        private string displayName;
        private bool dataReady;
        ProviderMetadata pmReference;
        object syncObject;

        //call from EventMetadata 
        internal EventOpcode(int value, ProviderMetadata pmReference) {
            this.value = value;
            this.pmReference = pmReference;
            this.syncObject = new object();
        }

        //call from ProviderMetadata
        internal EventOpcode(string name, int value, string displayName) {
            this.value = value;
            this.name = name;
            this.displayName = displayName;
            this.dataReady = true;
            this.syncObject = new object();
        }

        internal void PrepareData() {
            lock (syncObject) {
                if (dataReady == true) return;

                // get the data
                IEnumerable<EventOpcode> result = pmReference.Opcodes;
                //set the names and display names to null
                this.name = null;
                this.displayName = null;
                this.dataReady = true;
                foreach (EventOpcode op in result) {
                    if (op.Value == this.value) {
                        this.name = op.Name;
                        this.displayName = op.DisplayName;
                        this.dataReady = true;
                        break;
                    }
                }
            }
        }//End Prepare Data

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
