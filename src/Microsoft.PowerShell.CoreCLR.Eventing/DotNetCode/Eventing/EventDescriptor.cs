//------------------------------------------------------------------------------
// <copyright file="etwprovider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct EventDescriptor
    {
        [FieldOffset(0)]
        private ushort m_id;
        [FieldOffset(2)]
        private byte m_version;
        [FieldOffset(3)]
        private byte m_channel;
        [FieldOffset(4)]
        private byte m_level;
        [FieldOffset(5)]
        private byte m_opcode;
        [FieldOffset(6)]
        private ushort m_task;
        [FieldOffset(8)]
        private long m_keywords;

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

            m_id = (ushort)id;
            m_version = version;
            m_channel = channel;
            m_level = level;
            m_opcode = opcode;
            m_keywords = keywords;

            if (task < 0)
            {
                throw new ArgumentOutOfRangeException("task", DotNetEventingStrings.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (task > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("task", string.Format(CultureInfo.CurrentCulture, DotNetEventingStrings.ArgumentOutOfRange_NeedValidId, 1, ushort.MaxValue));
            }

            m_task = (ushort)task;
        }

        public int EventId { 
            get {
                return m_id;
            }
        }

        public byte Version
        {
            get
            {
                return m_version;
            }
        }

        public byte Channel
        {
            get
            {
                return m_channel;
            }
        }
        public byte Level
        {
            get
            {
                return m_level;
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
        public byte Opcode
        {
            get
            {
                return m_opcode;
            }
        }

        public int Task
        {
            get
            {
                return m_task;
            }
        }

        public long Keywords
        {
            get
            {
                return m_keywords;
            }
        }
    }
}
