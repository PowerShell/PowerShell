// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpFileHelpInfo keeps track of help information to be returned by
    /// command help provider.
    /// </summary>
    internal class SyntaxHelpInfo : BaseCommandHelpInfo
    {
        /// <summary>
        /// Constructor for SyntaxHelpInfo.
        /// </summary>
        private SyntaxHelpInfo(string name, string text, HelpCategory category)
            : base(category)
        {
            FullHelp = PSObject.AsPSObject(text);
            Name = name;
            Synopsis = text;
        }

        /// <summary>
        /// Name for the help info.
        /// </summary>
        /// <value>Name for the help info</value>
        internal override string Name { get; } = string.Empty;

        /// <summary>
        /// Synopsis for the help info.
        /// </summary>
        /// <value>Synopsis for the help info</value>
        internal override string Synopsis { get; } = string.Empty;

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
        /// <param name="category">Help category.</param>
        /// <returns>SyntaxHelpInfo object created based on information provided.</returns>
        internal static SyntaxHelpInfo GetHelpInfo(string name, string text, HelpCategory category)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            SyntaxHelpInfo syntaxHelpInfo = new SyntaxHelpInfo(name, text, category);

            if (string.IsNullOrEmpty(syntaxHelpInfo.Name))
                return null;

            syntaxHelpInfo.AddCommonHelpProperties();

            return syntaxHelpInfo;
        }
    }
}
