// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Text.RegularExpressions;
using Microsoft.PowerShell;
using Xunit;

namespace PSTests.Parallel
{
    /// <summary>
    /// Test helper class that implements PSHostRawUserInterface for progress bar testing.
    /// This implementation properly handles VT sequences and double-width characters.
    /// </summary>
    internal class TestProgressRawUI : PSHostRawUserInterface
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

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException();
        }

        public override void FlushInputBuffer()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculate the display width of a string in buffer cells, properly handling:
        /// - VT sequences (ANSI escape codes) which have zero width
        /// - Double-width characters (CJK, emoji) which occupy 2 cells
        /// - Regular characters which occupy 1 cell
        /// </summary>
        public override int LengthInBufferCells(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            // First, strip VT sequences as they don't contribute to display width
            string stripped = Regex.Replace(str, @"\x1b\[[0-9;]*m", string.Empty);

            int width = 0;
            foreach (char c in stripped)
            {
                // Check if character is double-width
                // This includes CJK characters, full-width punctuation, and emoji
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                // Characters in these ranges are typically double-width:
                // - East Asian ideographs (CJK)
                // - Hangul syllables (Korean)
                // - Full-width forms
                // - Emoji and symbols
                bool isDoubleWidth =
                    (c >= 0x1100 && c <= 0x115F) ||  // Hangul Jamo
                    (c >= 0x2E80 && c <= 0x9FFF) ||  // CJK
                    (c >= 0xAC00 && c <= 0xD7AF) ||  // Hangul Syllables
                    (c >= 0xF900 && c <= 0xFAFF) ||  // CJK Compatibility
                    (c >= 0xFE10 && c <= 0xFE19) ||  // Vertical forms
                    (c >= 0xFE30 && c <= 0xFE6F) ||  // CJK Compatibility Forms
                    (c >= 0xFF00 && c <= 0xFF60) ||  // Full-width forms
                    (c >= 0xFFE0 && c <= 0xFFE6) ||  // Full-width forms
                    (c >= 0x1F300 && c <= 0x1F9FF);  // Emoji and symbols

                width += isDoubleWidth ? 2 : 1;
            }

            return width;
        }

        public override int LengthInBufferCells(string str, int offset)
        {
            if (string.IsNullOrEmpty(str) || offset >= str.Length)
                return 0;

            return LengthInBufferCells(str.Substring(offset));
        }

        public override int LengthInBufferCells(char c)
        {
            return LengthInBufferCells(c.ToString());
        }
    }

    /// <summary>
    /// Tests for ProgressNode rendering with double-width unicode characters.
    /// These tests verify the fix for Issue #21293 where progress bars with
    /// double-width characters (Japanese, Chinese, Korean, emoji) exceeded maxWidth.
    /// </summary>
    public static class ProgressNodeTests
    {
        private static string StripVTSequences(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, @"\x1b\[[0-9;]*m", string.Empty);
        }

        /// <summary>
        /// Verify Issue #21293 scenario - the original bug report
        /// This test reproduces the exact scenario from the issue
        /// Tests with various window widths to ensure the fix works across different terminal sizes
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_OriginalBugScenario_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "My Status", "1/6 次の段階");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.SecondsRemaining = 120;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Issue #21293 (width {maxWidth}): Progress bar ({actualWidth} cells) exceeds maxWidth ({maxWidth}). Stripped: {stripped}");
        }

        /// <summary>
        /// Verify that progress bar respects maxWidth for Japanese text
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_WithDoubleWidthChars_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "Activity", "日本語テキスト");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Progress bar width {maxWidth}: ({actualWidth}) exceeds maxWidth ({maxWidth}). Stripped: {stripped}");
        }

        /// <summary>
        /// Verify emoji handling with sufficient text length
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_WithEmoji_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "Upload", "📁ファイル転送中🔄処理中📊");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Emoji progress bar width {maxWidth}: ({actualWidth} cells) exceeds maxWidth ({maxWidth}). Stripped: {stripped}");
        }

        /// <summary>
        /// Test various lengths of double-width text
        /// All test cases have sufficient length to detect the bug
        /// </summary>
        [Theory]
        [InlineData("あいうえお", 20)]
        [InlineData("あいうえお", 25)]
        [InlineData("あいうえお", 37)]
        [InlineData("あいうえお", 40)]
        [InlineData("あいうえお", 60)]
        [InlineData("あいうえお", 73)]
        [InlineData("あいうえお", 80)]
        [InlineData("あいうえお", 93)]
        [InlineData("あいうえお", 100)]
        [InlineData("あいうえお", 120)]
        [InlineData("あいうえお", 155)]
        [InlineData("あいうえお", 200)]
        [InlineData("データ処理中", 20)]
        [InlineData("データ処理中", 25)]
        [InlineData("データ処理中", 37)]
        [InlineData("データ処理中", 40)]
        [InlineData("データ処理中", 60)]
        [InlineData("データ処理中", 73)]
        [InlineData("データ処理中", 80)]
        [InlineData("データ処理中", 93)]
        [InlineData("データ処理中", 100)]
        [InlineData("データ処理中", 120)]
        [InlineData("データ処理中", 155)]
        [InlineData("データ処理中", 200)]
        [InlineData("ファイルをアップロード中", 20)]
        [InlineData("ファイルをアップロード中", 25)]
        [InlineData("ファイルをアップロード中", 37)]
        [InlineData("ファイルをアップロード中", 40)]
        [InlineData("ファイルをアップロード中", 60)]
        [InlineData("ファイルをアップロード中", 73)]
        [InlineData("ファイルをアップロード中", 80)]
        [InlineData("ファイルをアップロード中", 93)]
        [InlineData("ファイルをアップロード中", 100)]
        [InlineData("ファイルをアップロード中", 120)]
        [InlineData("ファイルをアップロード中", 155)]
        [InlineData("ファイルをアップロード中", 200)]
        public static void ProgressBar_VariousDoubleWidthLengths_MustNotExceedMaxWidth(string statusText, int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "Test", statusText);
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Status '{statusText}' width {maxWidth}: resulted in width {actualWidth} exceeding maxWidth {maxWidth}. Stripped: {stripped}");
        }

        /// <summary>
        /// Verify mixed ASCII and double-width characters
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_MixedWidthText_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "Process", "File_日本語_document.txt");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Mixed-width text width {maxWidth}: resulted in width {actualWidth} exceeding maxWidth {maxWidth}. Stripped: {stripped}");
        }

        /// <summary>
        /// Verify long double-width string handling and truncation
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_LongDoubleWidthString_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "処理", "ファイルを処理中です今しばらくお待ちください");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Long double-width text width {maxWidth}: resulted in width {actualWidth} exceeding maxWidth {maxWidth}. Stripped: {stripped}");
        }

        /// <summary>
        /// Verify that truncation respects character boundaries
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_Truncation_MustRespectCharacterBoundaries(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();
            var record = new ProgressRecord(0, "Downloading", "ファイル処理中123456789012345678901234567890");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Truncated progress bar width {maxWidth}: ({actualWidth} cells) exceeds maxWidth ({maxWidth}). Stripped: {stripped}");
        }

        /// <summary>
        /// Test for Issue: Double closing bracket when truncating with double-width characters
        /// Verifies that statusPartDisplayWidth is calculated from actual statusPart length,
        /// not from estimated value, to prevent progress bar from exceeding maxWidth
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ProgressBar_LongStatusWithDoubleWidth_MustNotExceedMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();

            // Very long status with emojis and mixed width characters that will be truncated
            var record = new ProgressRecord(0, "Long Text Test", "🚀🎯⚡💾✅ 絵文字も含めた超長文テストケース with emojis and very long text to test the truncation feature properly");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 100;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            // The progress bar must not exceed maxWidth
            Assert.True(actualWidth <= maxWidth,
                $"Progress bar with long truncated status width {maxWidth}: ({actualWidth} cells) exceeds maxWidth ({maxWidth}). Stripped: {stripped}");

            // Verify no double closing brackets - count ']' occurrences at end
            int closingBracketCount = 0;
            for (int i = stripped.Length - 1; i >= 0 && stripped[i] == ']'; i--)
            {
                closingBracketCount++;
            }

            Assert.True(closingBracketCount <= 1,
                $"Found {closingBracketCount} consecutive closing brackets at end (width {maxWidth}), expected 1. Output: {stripped}");
        }

        /// <summary>
        /// Test activityDisplayWidth bug with small maxWidth to amplify error
        /// Bug: After truncation, activityDisplayWidth is set to maxWidth/2 but actual width differs
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ExtremeTest_ActivityWidthBug_SmallMaxWidth(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();

            string longActivity = "日本語日本語日本語日本語日本語"; // 20 cells
            var record = new ProgressRecord(0, longActivity, "St");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 50;
            node.SecondsRemaining = 30;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"maxWidth {maxWidth} test: width {actualWidth} exceeds {maxWidth}. Output: [{stripped}]");
        }

        /// <summary>
        /// Test surrogate pair handling with strategically positioned emojis
        /// Bug: Substring(i,1) splits surrogate pairs causing width calculation errors
        /// </summary>
        [Theory]
        [InlineData(20)]
        [InlineData(25)]
        [InlineData(37)]
        [InlineData(40)]
        [InlineData(60)]
        [InlineData(73)]
        [InlineData(80)]
        [InlineData(93)]
        [InlineData(100)]
        [InlineData(120)]
        [InlineData(155)]
        [InlineData(200)]
        public static void ExtremeTest_SurrogatePairAtVTBoundary(int maxWidth)
        {
            var rawUI = new TestProgressRawUI();

            var record = new ProgressRecord(0, "A", "X🚀Y🚀Z🚀");
            var node = new ProgressNode(1, record);
            node.PercentComplete = 33;
            node.Style = ProgressNode.RenderStyle.Ansi;

            var strCollection = new ArrayList();

            node.Render(strCollection, 0, maxWidth, rawUI);

            var output = strCollection[0] as string;
            var stripped = StripVTSequences(output);
            int actualWidth = rawUI.LengthInBufferCells(stripped);

            Assert.True(actualWidth <= maxWidth,
                $"Surrogate pair test width {maxWidth}: width {actualWidth} exceeds {maxWidth}. Output: [{stripped}]");
        }
    }
}
