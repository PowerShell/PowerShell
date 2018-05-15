// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.IO;
using Dbg = System.Management.Automation.Diagnostics;
using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
using HRESULT = System.UInt32;
using DWORD = System.UInt32;
using NakedWin32Handle = System.IntPtr;

namespace Microsoft.PowerShell
{
    internal
    class ConsoleTextWriter : TextWriter
    {
        internal
        ConsoleTextWriter(ConsoleHostUserInterface ui)
            :
            base(System.Globalization.CultureInfo.CurrentCulture)
        {
            Dbg.Assert(ui != null, "ui needs a value");

            _ui = ui;
        }

        public override
        Encoding
        Encoding
        {
            get
            {
                return null;
            }
        }

        public override
        void
        Write(string value)
        {
            _ui.WriteToConsole(value, true);
        }

        public override
        void
        Write(ReadOnlySpan<char> value)
        {
            _ui.WriteToConsole(value, true);
        }

        public override
        void
        WriteLine(string value)
        {
            _ui.WriteLineToConsole(value, true);
        }

        public override
        void
        WriteLine(ReadOnlySpan<char> value)
        {
            _ui.WriteLineToConsole(value, true);
        }

        public override
        void
        Write(Boolean b)
        {
            if (b)
            {
                _ui.WriteToConsole(Boolean.TrueString.AsSpan(), true);
            }
            else
            {
                _ui.WriteToConsole(Boolean.FalseString.AsSpan(), true);
            }

        }

        public override
        void
        Write(Char c)
        {
            Span<char> c1 = stackalloc char[1];
            c1[0] = c;
            _ui.WriteToConsole(c1, true);
        }

        public override
        void
        Write(Char[] a)
        {
            _ui.WriteToConsole(a, true);
        }

        private ConsoleHostUserInterface _ui;
    }
}   // namespace

