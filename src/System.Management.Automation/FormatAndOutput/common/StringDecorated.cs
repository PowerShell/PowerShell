// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

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
        private string? _plaintextcontent;

        private string PlainText
        {
            get
            {
                if (_plaintextcontent == null)
                {
                    _plaintextcontent = ValueStringDecorated.s_ansiRegex.Replace(_text, string.Empty);
                }

                return _plaintextcontent;
            }
        }

        /// <summary>
        /// Initializes a new instance of the StringDecorated class.
        /// </summary>
        /// <param name="text">The input string.</param>
        public StringDecorated(string text)
        {
            _text = text;
            _isDecorated = text.Contains(ESC);
        }

        /// <summary>
        /// Gets a value indicating whether the string contains decoration.
        /// </summary>
        /// <returns>Boolean if the string contains decoration.</returns>
        public bool IsDecorated => _isDecorated;

        /// <summary>
        /// Gets the length of content sans escape sequences.
        /// </summary>
        /// <returns>Length of content sans escape sequences.</returns>
        public int ContentLength => PlainText.Length;

        /// <summary>
        /// Render the decorarted string using automatic output rendering.
        /// </summary>
        /// <returns>Rendered string based on automatic output rendering.</returns>
        public override string ToString() => _isDecorated ? ToString(OutputRendering.Automatic) : _text;

        /// <summary>
        /// Return string representation of content depending on output rendering mode.
        /// </summary>
        /// <param name="outputRendering">Specify how to render the text content.</param>
        /// <returns>Rendered string based on outputRendering.</returns>
        public string ToString(OutputRendering outputRendering)
        {
            if (!_isDecorated)
            {
                return _text;
            }

            if (outputRendering == OutputRendering.Automatic)
            {
                outputRendering = OutputRendering.Ansi;
                if (PSStyle.Instance.OutputRendering == OutputRendering.PlainText)
                {
                    outputRendering = OutputRendering.PlainText;
                }
            }

            if (outputRendering == OutputRendering.PlainText)
            {
                return PlainText;
            }
            else
            {
                return _text;
            }
        }
    }

    internal struct ValueStringDecorated
    {
        private const char ESC = '\x1b';
        private readonly bool _isDecorated;
        private readonly string _text;
        private string _plaintextcontent;
        private bool _isInitialized;

        private string PlainText
        {
            get
            {
                if (!_isInitialized)
                {
                    _plaintextcontent = s_ansiRegex.Replace(_text, string.Empty);
                    _isInitialized = true;

                }

                return _plaintextcontent;
            }
        }

        // replace regex with .NET 6 API once available
        public static readonly Regex s_ansiRegex = new Regex(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the StringDecorated class.
        /// </summary>
        /// <param name="text">The input string.</param>
        public ValueStringDecorated(string text)
        {
            _text = text;
            _isDecorated = text.Contains(ESC);
            _isInitialized = false;
            _plaintextcontent = string.Empty;
        }

        /// <summary>
        /// Gets a value indicating whether the string contains decoration.
        /// </summary>
        /// <returns>Boolean if the string contains decoration.</returns>
        public bool IsDecorated => _isDecorated;

        /// <summary>
        /// Gets the length of content sans escape sequences.
        /// </summary>
        /// <returns>Length of content sans escape sequences.</returns>
        public int ContentLength => PlainText.Length;

        /// <summary>
        /// Render the decorarted string using automatic output rendering.
        /// </summary>
        /// <returns>Rendered string based on automatic output rendering.</returns>
        public override string ToString() => _isDecorated ? ToString(OutputRendering.Automatic) : _text;

        /// <summary>
        /// Return string representation of content depending on output rendering mode.
        /// </summary>
        /// <param name="outputRendering">Specify how to render the text content.</param>
        /// <returns>Rendered string based on outputRendering.</returns>
        public string ToString(OutputRendering outputRendering)
        {
            if (!_isDecorated)
            {
                return _text;
            }

            if (outputRendering == OutputRendering.Automatic)
            {
                outputRendering = OutputRendering.Ansi;
                if (PSStyle.Instance.OutputRendering == OutputRendering.PlainText)
                {
                    outputRendering = OutputRendering.PlainText;
                }
            }

            if (outputRendering == OutputRendering.PlainText)
            {
                return PlainText;
            }
            else
            {
                return _text;
            }
        }
    }
}
