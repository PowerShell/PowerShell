// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpFileHelpInfo keeps track of help information to be returned by
    /// command help provider.
    /// </summary>
    internal sealed class HelpFileHelpInfo : HelpInfo
    {
        /// <summary>
        /// Constructor for HelpFileHelpInfo.
        /// </summary>
        /// <remarks>
        /// This is made private intentionally so that the only way to create object of this type
        /// is through
        ///     GetHelpInfo(string name, string text, string filename)
        /// </remarks>
        /// <param name="name">Help topic name.</param>
        /// <param name="text">Help text.</param>
        /// <param name="filename">File name that contains the help text.</param>
        private HelpFileHelpInfo(string name, string text, string filename)
        {
            FullHelp = PSObject.AsPSObject(text);

            Name = name;

            // Take the 5th line as synopsis. This may not be true if
            // format of help file is changed later on.
            _synopsis = GetLine(text, 5);
            if (_synopsis != null)
            {
                _synopsis = _synopsis.Trim();
            }
            else
            {
                // make sure _synopsis is never null
                _synopsis = string.Empty;
            }

            _filename = filename;
        }

        /// <summary>
        /// Name for the help info.
        /// </summary>
        /// <value>Name for the help info</value>
        internal override string Name { get; } = string.Empty;

        private readonly string _filename = string.Empty;
        private readonly string _synopsis = string.Empty;
        /// <summary>
        /// Synopsis for the help info.
        /// </summary>
        /// <value>Synopsis for the help info</value>
        internal override string Synopsis
        {
            get
            {
                return _synopsis;
            }
        }

        /// <summary>
        /// Help category for the help info.
        /// </summary>
        /// <value>Help category for the help info</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.HelpFile;
            }
        }

        /// <summary>
        /// Full help object for this help info.
        /// </summary>
        /// <value>Full help object for this help info</value>
        internal override PSObject FullHelp { get; }

        /// <summary>
        /// Get help info based on name, text and filename.
        /// </summary>
        /// <param name="name">Help topic name.</param>
        /// <param name="text">Help text.</param>
        /// <param name="filename">File name that contains the help text.</param>
        /// <returns>HelpFileHelpInfo object created based on information provided.</returns>
        internal static HelpFileHelpInfo GetHelpInfo(string name, string text, string filename)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            HelpFileHelpInfo helpfileHelpInfo = new HelpFileHelpInfo(name, text, filename);

            if (string.IsNullOrEmpty(helpfileHelpInfo.Name))
                return null;

            helpfileHelpInfo.AddCommonHelpProperties();

            return helpfileHelpInfo;
        }

        /// <summary>
        /// Get the text corresponding to a line in input text.
        /// </summary>
        /// <param name="text">Text to get the line for.</param>
        /// <param name="line">Line number.</param>
        /// <returns>The part of string in text that is in specified line.</returns>
        private static string GetLine(string text, int line)
        {
            StringReader reader = new StringReader(text);

            string result = null;

            for (int i = 0; i < line; i++)
            {
                result = reader.ReadLine();

                if (result == null)
                    return null;
            }

            return result;
        }

        internal override bool MatchPatternInContent(WildcardPattern pattern)
        {
            Diagnostics.Assert(pattern != null, "pattern cannot be null.");

            string helpContent = string.Empty;
            LanguagePrimitives.TryConvertTo<string>(FullHelp, out helpContent);
            return pattern.IsMatch(helpContent);
        }
    }
}
