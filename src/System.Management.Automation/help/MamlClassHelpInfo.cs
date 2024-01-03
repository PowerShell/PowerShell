// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;
using System.Globalization;

namespace System.Management.Automation
{
    /// <summary>
    /// Class MamlClassHelpInfo keeps track of help information to be returned by
    /// class help provider.
    /// </summary>
    internal class MamlClassHelpInfo : HelpInfo
    {
        /// <summary>
        /// Constructor for custom HelpInfo object creation.
        /// </summary>
        /// <param name="helpObject"></param>
        /// <param name="helpCategory"></param>
        internal MamlClassHelpInfo(PSObject helpObject, HelpCategory helpCategory)
        {
            HelpCategory = helpCategory;
            _fullHelpObject = helpObject;
        }

        /// <summary>
        /// Convert a XMLNode to HelpInfo object.
        /// </summary>
        /// <param name="xmlNode"></param>
        /// <param name="helpCategory"></param>
        private MamlClassHelpInfo(XmlNode xmlNode, HelpCategory helpCategory)
        {
            HelpCategory = helpCategory;

            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;

            this.Errors = mamlNode.Errors;
            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add("PSClassHelpInfo");
        }

        /// <summary>
        /// PSObject representation on help.
        /// </summary>
        private readonly PSObject _fullHelpObject;

        #region Load

        /// <summary>
        /// Create a MamlClassHelpInfo object from an XmlNode.
        /// </summary>
        /// <param name="xmlNode">XmlNode that contains help info.</param>
        /// <param name="helpCategory">Help category this maml object fits into.</param>
        /// <returns>MamlCommandHelpInfo object created.</returns>
        internal static MamlClassHelpInfo Load(XmlNode xmlNode, HelpCategory helpCategory)
        {
            MamlClassHelpInfo mamlClassHelpInfo = new MamlClassHelpInfo(xmlNode, helpCategory);

            if (string.IsNullOrEmpty(mamlClassHelpInfo.Name))
                return null;

            mamlClassHelpInfo.AddCommonHelpProperties();

            return mamlClassHelpInfo;
        }

        #endregion

        #region Helper Methods and Overloads

        /// <summary>
        /// Clone the help info object.
        /// </summary>
        /// <returns>MamlClassHelpInfo object.</returns>
        internal MamlClassHelpInfo Copy()
        {
            MamlClassHelpInfo result = new MamlClassHelpInfo(_fullHelpObject.Copy(), this.HelpCategory);
            return result;
        }

        /// <summary>
        /// Clone the help object with a new category.
        /// </summary>
        /// <param name="newCategoryToUse"></param>
        /// <returns>MamlClassHelpInfo.</returns>
        internal MamlClassHelpInfo Copy(HelpCategory newCategoryToUse)
        {
            MamlClassHelpInfo result = new MamlClassHelpInfo(_fullHelpObject.Copy(), newCategoryToUse);
            result.FullHelp.Properties["Category"].Value = newCategoryToUse.ToString();
            return result;
        }

        internal override string Name
        {
            get
            {
                string tempName = string.Empty;
                var title = _fullHelpObject.Properties["title"];

                if (title != null && title.Value != null)
                {
                    tempName = title.Value.ToString();
                }

                return tempName;
            }
        }

        internal override string Synopsis
        {
            get
            {
                if (_fullHelpObject == null)
                {
                    return string.Empty;
                }

                if (_fullHelpObject.Properties["introduction"] == null ||
                    _fullHelpObject.Properties["introduction"].Value == null)
                {
                    return string.Empty;
                }

                object[] synopsisItems = (object[])LanguagePrimitives.ConvertTo(
                    _fullHelpObject.Properties["introduction"].Value,
                    typeof(object[]),
                    CultureInfo.InvariantCulture);
                if (synopsisItems == null || synopsisItems.Length == 0)
                {
                    return string.Empty;
                }

                PSObject firstSynopsisItem = synopsisItems[0] == null ? null : PSObject.AsPSObject(synopsisItems[0]);
                if (firstSynopsisItem == null ||
                    firstSynopsisItem.Properties["Text"] == null ||
                    firstSynopsisItem.Properties["Text"].Value == null)
                {
                    return string.Empty;
                }

                string synopsis = firstSynopsisItem.Properties["Text"].Value.ToString();
                if (synopsis == null)
                {
                    return string.Empty;
                }

                return synopsis.Trim();
            }
        }

        internal override HelpCategory HelpCategory { get; }

        internal override PSObject FullHelp
        {
            get { return _fullHelpObject; }
        }

        #endregion
    }
}
