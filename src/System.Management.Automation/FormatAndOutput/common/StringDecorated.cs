// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Extensions to String type to calculate and render decorated content.
    /// </summary>
    public class StringDecorated
    {
        private const char ESC = '\x1b';
        private readonly bool _isDecorated;
        private readonly string _text;
        private readonly string _plaintext;
        private static readonly Regex _ansiRegex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the StringDecorated class.
        /// </summary>
        /// <param name="text">The input string.</param>
        public StringDecorated(string text)
        {
            _text = text;
            _isDecorated = text.Contains(ESC);
            _plaintext = _ansiRegex.Replace(text, string.Empty);
        }

        /// <summary>
        /// Gets a value indicating whether the string contains decoration.
        /// </summary>
        /// <returns>Boolean if the string contains decoration.</returns>
        public bool IsDecorated
        {
            get
            {
                return _isDecorated;
            }
        }

        /// <summary>
        /// Gets the length of content sans escape sequences.
        /// </summary>
        /// <returns>Length of content sans escape sequences.</returns>
        public int ContentLength
        {
            get
            {
                return _plaintext.Length;
            }
        }

        /// <summary>
        /// Return a substring based on content length starting from index 0.
        /// </summary>
        /// <param name="length">Number of characters from content to return.</param>
        /// <returns>A string up to the number of specified characters including decoration up to that length.</returns>
        public string Substring(int length)
        {
            return _text;
        }

        /// <summary>
        /// Render the decorarted string using automatic output rendering.
        /// </summary>
        /// <returns>Rendered string based on automatic output rendering.</returns>
        public override string ToString()
        {
            if (_isDecorated)
            {
                return ToString(OutputRendering.Automatic);
            }
            else
            {
                return _text;
            }
        }

        /// <summary>
        /// Return string representation of content depending on output rendering mode.
        /// </summary>
        /// <param name="outputRendering">Specify how to render the text content.</param>
        /// <returns>Rendered string based on outputRendering.</returns>
        public string ToString(OutputRendering outputRendering)
        {
            if (!_isDecorated)
            {
                return _plaintext;
            }

            if (outputRendering == OutputRendering.Automatic)
            {
                outputRendering = OutputRendering.Ansi;
                if (Utils.OutputRenderingSetting == OutputRendering.PlainText)
                {
                    outputRendering = OutputRendering.PlainText;
                }
            }

            if (outputRendering == OutputRendering.PlainText)
            {
                return _plaintext;
            }
            else
            {
                return _text;
            }
        }
    }
}
