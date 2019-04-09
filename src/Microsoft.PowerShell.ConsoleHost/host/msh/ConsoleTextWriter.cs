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
        WriteLine(string value)
        {
            this.Write(value + ConsoleHostUserInterface.Crlf);
        }

        public override
        void
        Write(Boolean b)
        {
            this.Write(b.ToString());
        }

        public override
        void
        Write(char c)
        {
            this.Write(new String(c, 1));
        }

        public override
        void
        Write(char[] a)
        {
            this.Write(new String(a));
        }

        private ConsoleHostUserInterface _ui;
    }
}   // namespace

