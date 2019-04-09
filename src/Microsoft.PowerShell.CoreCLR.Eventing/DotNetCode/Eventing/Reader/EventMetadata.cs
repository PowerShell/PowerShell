// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class describes the metadata for a specific event
** raised by Provider. An instance of this class is obtained from
** ProviderMetadata class.
**
============================================================*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Event Metadata.
    /// </summary>
    public sealed class EventMetadata
    {
        private long _id;
        private byte _version;
        private byte _channelId;
        private byte _level;
        private short _opcode;
        private int _task;
        private long _keywords;
        private string _template;
        private string _description;

        private ProviderMetadata _pmReference;

        internal EventMetadata(uint id, byte version, byte channelId,
                 byte level, byte opcode, short task, long keywords,
                 string template, string description, ProviderMetadata pmReference)
        {
            _id = id;
            _version = version;
            _channelId = channelId;
            _level = level;
            _opcode = opcode;
            _task = task;
            _keywords = keywords;
            _template = template;
            _description = description;
            _pmReference = pmReference;
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
        public long Id
        {
            get
            {
                return _id;
            }
        }

        public byte Version
        {
            get
            {
                return _version;
            }
        }

        public EventLogLink LogLink
        {
            get
            {
                return new EventLogLink((uint)_channelId, _pmReference);
            }
        }

        public EventLevel Level
        {
            get
            {
                return new EventLevel(_level, _pmReference);
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public EventOpcode Opcode
        {
            get
            {
                return new EventOpcode(_opcode, _pmReference);
            }
        }

        public EventTask Task
        {
            get
            {
                return new EventTask(_task, _pmReference);
            }
        }

        public IEnumerable<EventKeyword> Keywords
        {
            get
            {
                List<EventKeyword> list = new List<EventKeyword>();

                ulong theKeywords = unchecked((ulong)_keywords);
                ulong mask = 0x8000000000000000;

                // for every bit
                // for (int i = 0; i < 64 && theKeywords != 0; i++)
                for (int i = 0; i < 64; i++)
                {
                    // if this bit is set
                    if ((theKeywords & mask) > 0)
                    {
                        // the mask is the keyword we will be searching for.
                        list.Add(new EventKeyword(unchecked((long)mask), _pmReference));
                        // theKeywords = theKeywords - mask;
                    }
                    // modify the mask to check next bit.
                    mask = mask >> 1;
                }

                return list;
            }
        }

        public string Template
        {
            get
            {
                return _template;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
        }
    }
}
