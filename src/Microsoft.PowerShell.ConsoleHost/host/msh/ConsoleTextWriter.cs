// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;

using ConsoleHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
using Dbg = System.Management.Automation.Diagnostics;
using DWORD = System.UInt32;
using HRESULT = System.UInt32;
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
            _ui.WriteToConsole(value, transcribeResult: true);
        }

        public override
        void
        Write(ReadOnlySpan<char> value)
        {
            _ui.WriteToConsole(value, transcribeResult: true);
        }

        public override
        void
        WriteLine(string value)
        {
            _ui.WriteLineToConsole(value, transcribeResult: true);
        }

        public override
        void
        WriteLine(ReadOnlySpan<char> value)
        {
            _ui.WriteLineToConsole(value, transcribeResult: true);
        }

        public override
        void
        Write(bool b)
        {
            if (b)
            {
                _ui.WriteToConsole(bool.TrueString, transcribeResult: true);
            }
            else
            {
                _ui.WriteToConsole(bool.FalseString, transcribeResult: true);
            }
        }

        public override
        void
        Write(char c)
        {
            ReadOnlySpan<char> c1 = stackalloc char[1] { c };
            _ui.WriteToConsole(c1, transcribeResult: true);
        }

        public override
        void
        Write(char[] a)
        {
            _ui.WriteToConsole(a, transcribeResult: true);
        }

        private ConsoleHostUserInterface _ui;
    }
}
