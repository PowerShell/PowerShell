// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Host;
using Microsoft.PowerShell;

namespace PSTests.Parallel
{
    /// <summary>
    /// Test helper class that implements PSHostRawUserInterface for progress bar testing.
    /// Delegates width calculations to ConsoleControl.LengthInBufferCells.
    /// </summary>
    internal sealed class TestProgressRawUI : PSHostRawUserInterface
    {
        public override ConsoleColor ForegroundColor { get; set; }

        public override ConsoleColor BackgroundColor { get; set; }

        public override Coordinates CursorPosition { get; set; }

        public override Coordinates WindowPosition { get; set; }

        public override int CursorSize { get; set; }

        public override Size BufferSize { get; set; }

        public override Size WindowSize { get; set; }

        public override Size MaxWindowSize => new Size(120, 50);

        public override Size MaxPhysicalWindowSize => new Size(120, 50);

        public override string WindowTitle { get; set; }

        public override bool KeyAvailable => false;

        public override BufferCell[,] GetBufferContents(Rectangle rectangle) => throw new NotImplementedException();

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) => throw new NotImplementedException();

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) => throw new NotImplementedException();

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) => throw new NotImplementedException();

        public override KeyInfo ReadKey(ReadKeyOptions options) => throw new NotImplementedException();

        public override void FlushInputBuffer() => throw new NotImplementedException();

        public override int LengthInBufferCells(string str) => ConsoleControl.LengthInBufferCells(str, 0, checkEscapeSequences: true);

        public override int LengthInBufferCells(string str, int offset) => ConsoleControl.LengthInBufferCells(str, offset, checkEscapeSequences: true);

        public override int LengthInBufferCells(char c) => ConsoleControl.LengthInBufferCells(c);
    }
}
