// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class: EventMetadata
**
** Purpose: 
** This public class describes the metadata for a specific event 
** raised by Provider. An instance of this class is obtained from 
** ProviderMetadata class.
** 
============================================================*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader {
    /// <summary>
    /// Event Metadata
    /// </summary>
    public sealed class EventMetadata {
        private long id;
        private byte version;
        private byte channelId;
        private byte level;
        private short opcode;
        private int task;
        private long keywords;
        private string template;
        private string description;

        ProviderMetadata pmReference;

        internal EventMetadata(uint id, byte version, byte channelId,
                 byte level, byte opcode, short task, long keywords,
                 string template, string description, ProviderMetadata pmReference) {
            this.id = id;
            this.version = version;
            this.channelId = channelId;
            this.level = level;
            this.opcode = opcode;
            this.task = task;
            this.keywords = keywords;
            this.template = template;
            this.description = description;
            this.pmReference = pmReference;
        }

        //
        // Max value will be UINT32.MaxValue - it is a long because this property
        // is really a UINT32.  The legacy API allows event message ids to be declared 
        // as UINT32 and these event/messages may be migrated into a Provider's 
        // manifest as UINT32.  Note that EventRecord ids are 
        // still declared as int, because those ids max value is UINT16.MaxValue
        // and rest of the bits of the legacy event id would be stored in 
        // Qualifiers property.
        // 
        public long Id {
            get {
                return this.id;
            }
        }

        public byte Version {
            get {
                return this.version;
            }
        }

        public EventLogLink LogLink {
            get {
                return new EventLogLink((uint)this.channelId, this.pmReference);
            }
        }

        public EventLevel Level {
            get {
                return new EventLevel(this.level, this.pmReference);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public EventOpcode Opcode {
            get {
                return new EventOpcode(this.opcode, this.pmReference);
            }
        }

        public EventTask Task {
            get {
                return new EventTask(this.task, this.pmReference);
            }
        }


        public IEnumerable<EventKeyword> Keywords {
            get {
                List<EventKeyword> list = new List<EventKeyword>();

                ulong theKeywords = unchecked((ulong)this.keywords);
                ulong mask = 0x8000000000000000;

                //for every bit
                //for (int i = 0; i < 64 && theKeywords != 0; i++)
                for (int i = 0; i < 64; i++) {
                    //if this bit is set
                    if ((theKeywords & mask) > 0) {
                        //the mask is the keyword we will be searching for.
                        list.Add(new EventKeyword(unchecked((long)mask), this.pmReference));
                        //theKeywords = theKeywords - mask;
                    }
                    //modify the mask to check next bit.
                    mask = mask >> 1;
                }

                return list;
            }
        }

        public string Template {
            get {
                return this.template;
            }
        }

        public string Description {
            get {
                return this.description;
            }
        }
    }
}
