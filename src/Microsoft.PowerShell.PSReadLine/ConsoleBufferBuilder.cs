/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using Microsoft.PowerShell.Internal;

namespace Microsoft.PowerShell
{
    internal class ConsoleBufferBuilder
    {
        private List<BufferChar> buffer;
        private IConsole _console;

        public ConsoleBufferBuilder(int capacity, IConsole console)
        {
            buffer = new List<BufferChar>(capacity);
            _console = console;
        }

        BufferChar NewCharInfo(char c)
        {
            return new BufferChar
            {
                UnicodeChar = c,
                BackgroundColor = _console.BackgroundColor,
                ForegroundColor = _console.ForegroundColor
            };
        }

        public void Append(string s)
        {
            foreach (char c in s)
            {
                buffer.Add(NewCharInfo(c));
            }
        }

        public void Append(char c, int count)
        {
            while (count-- > 0)
            {
                buffer.Add(NewCharInfo(c));
            }
        }

        public BufferChar[] ToArray()
        {
            return buffer.ToArray();
        }

        public int Length
        {
            get { return buffer.Count; }
        }
    }
}

