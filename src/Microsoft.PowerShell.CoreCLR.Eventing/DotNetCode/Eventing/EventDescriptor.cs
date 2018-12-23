// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct EventDescriptor
    {
        [FieldOffset(0)]
        private ushort _id;
        [FieldOffset(2)]
        private byte _version;
        [FieldOffset(3)]
        private byte _channel;
        [FieldOffset(4)]
        private byte _level;
        [FieldOffset(5)]
        private byte _opcode;
        [FieldOffset(6)]
        private ushort _task;
        [FieldOffset(8)]
        private long _keywords;

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public EventDescriptor(
                int id,
                byte version,
                byte channel,
                byte level,
                byte opcode,
                int task,
                long keywords
                )
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException("id", DotNetEventingStrings.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (id > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("id", string.Format(CultureInfo.CurrentCulture, DotNetEventingStrings.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
            }

            _id = (ushort)id;
            _version = version;
            _channel = channel;
            _level = level;
            _opcode = opcode;
            _keywords = keywords;

            if (task < 0)
            {
                throw new ArgumentOutOfRangeException("task", DotNetEventingStrings.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (task > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("task", string.Format(CultureInfo.CurrentCulture, DotNetEventingStrings.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
            }

            _task = (ushort)task;
        }

        public int EventId
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

        public byte Channel
        {
            get
            {
                return _channel;
            }
        }

        public byte Level
        {
            get
            {
                return _level;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public byte Opcode
        {
            get
            {
                return _opcode;
            }
        }

        public int Task
        {
            get
            {
                return _task;
            }
        }

        public long Keywords
        {
            get
            {
                return _keywords;
            }
        }
    }
}
