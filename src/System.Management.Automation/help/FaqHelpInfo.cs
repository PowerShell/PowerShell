/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections;
using System.Xml;
using System.Diagnostics.CodeAnalysis; // for fxcop

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Class FaqHelpInfo keeps track of help information to be returned by 
    /// faq help provider.
    /// 
    /// </summary>
    internal class FaqHelpInfo : HelpInfo
    {
        /// <summary>
        /// Constructor for FaqHelpInfo
        /// </summary>
        /// <remarks>
        /// This constructor is can be called only from constructors of derived class
        /// for FaqHelpInfo. The only way to to create a FaqHelpInfo is through 
        /// static function
        ///     Load(XmlNode node)
        /// where some sanity check is done.
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is internal code and is verified.")]
        protected FaqHelpInfo(XmlNode xmlNode)
        {
            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;
            this.Errors = mamlNode.Errors;

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add(string.Format(Globalization.CultureInfo.InvariantCulture,
                "FaqHelpInfo#{0}", Name));
            _fullHelpObject.TypeNames.Add("FaqHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        #region Basic Help Properties / Methods

        /// <summary>
        /// Name of faq. 
        /// </summary>
        /// <value>Name of faq</value>
        internal override string Name
        {
            get
            {
                if (_fullHelpObject == null)
                    return "";

                if (_fullHelpObject.Properties["Title"] == null)
                    return "";

                if (_fullHelpObject.Properties["Title"].Value == null)
                    return "";

                string name = _fullHelpObject.Properties["Title"].Value.ToString();
                if (name == null)
                    return "";

                return name.Trim();
            }
        }

        /// <summary>
        /// Synopsis for this faq help.
        /// </summary>
        /// <value>Synopsis for this faq help</value>
        internal override string Synopsis
        {
            get
            {
                if (_fullHelpObject == null)
                    return "";

                if (_fullHelpObject.Properties["question"] == null)
                    return "";

                if (_fullHelpObject.Properties["question"].Value == null)
                    return "";

                string synopsis = _fullHelpObject.Properties["question"].Value.ToString();
                if (synopsis == null)
                    return "";

                return synopsis.Trim();
            }
        }

        /// <summary>
        /// Help category for this faq help, which is constantly HelpCategory.FAQ.
        /// </summary>
        /// <value>Help category for this faq help</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.FAQ;
            }
        }

        private PSObject _fullHelpObject;

        /// <summary>
        /// Full help object for this help item.
        /// </summary>
        /// <value>Full help object for this help item.</value>
        internal override PSObject FullHelp
        {
            get
            {
                return _fullHelpObject;
            }
        }

        /// <summary>
        /// Returns true if help content in help info matches the
        /// pattern contained in <paramref name="pattern"/>. 
        /// The underlying code will usually run pattern.IsMatch() on
        /// content it wants to search.
        /// FAQ help info looks for pattern in Synopsis and 
        /// Answers
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal override bool MatchPatternInContent(WildcardPattern pattern)
        {
            Diagnostics.Assert(null != pattern, "pattern cannot be null");

            string synopsis = Synopsis;
            string answers = Answers;

            if (null == synopsis)
            {
                synopsis = string.Empty;
            }

            if (null == Answers)
            {
                answers = string.Empty;
            }

            return pattern.IsMatch(synopsis) || pattern.IsMatch(answers);
        }

        #endregion

        #region Private Methods / Properties


        /// <summary>
        /// Answers string of this FAQ help info.
        /// </summary>
        private string Answers
        {
            get
            {
                if (this.FullHelp == null)
                    return "";

                if (this.FullHelp.Properties["answer"] == null ||
                    this.FullHelp.Properties["answer"].Value == null)
                {
                    return "";
                }

                IList answerItems = FullHelp.Properties["answer"].Value as IList;
                if (answerItems == null || answerItems.Count == 0)
                {
                    return "";
                }

                Text.StringBuilder result = new Text.StringBuilder();
                foreach (object answerItem in answerItems)
                {
                    PSObject answerObject = PSObject.AsPSObject(answerItem);
                    if ((null == answerObject) ||
                        (null == answerObject.Properties["Text"]) ||
                        (null == answerObject.Properties["Text"].Value))
                    {
                        continue;
                    }

                    string text = answerObject.Properties["Text"].Value.ToString();
                    result.Append(text);
                    result.Append(Environment.NewLine);
                }

                return result.ToString().Trim();
            }
        }

        #endregion

        #region Load

        /// <summary>
        /// Create a FaqHelpInfo object from an XmlNode.
        /// </summary>
        /// <param name="xmlNode">xmlNode that contains help info</param>
        /// <returns>FaqHelpInfo object created</returns>
        internal static FaqHelpInfo Load(XmlNode xmlNode)
        {
            FaqHelpInfo faqHelpInfo = new FaqHelpInfo(xmlNode);

            if (String.IsNullOrEmpty(faqHelpInfo.Name))
                return null;

            faqHelpInfo.AddCommonHelpProperties();

            return faqHelpInfo;
        }

        #endregion
    }
}
