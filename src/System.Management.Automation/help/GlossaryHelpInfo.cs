/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Xml;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Class GlossaryHelpInfo keeps track of help information to be returned by 
    /// glossary help provider.
    /// 
    /// </summary>
    internal class GlossaryHelpInfo : HelpInfo
    {
        /// <summary>
        /// Constructor for GlossaryHelpInfo
        /// </summary>
        /// <remarks>
        /// This constructor is can be called only from constructors of derived class
        /// for GlossaryHelpInfo. The only way to to create a GlossaryHelpInfo is through 
        /// static function
        ///     Load(XmlNode node)
        /// where some sanity check is done.
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is internal code and is verified.")]
        protected GlossaryHelpInfo(XmlNode xmlNode)
        {
            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;
            this.Errors = mamlNode.Errors;

            Name = GetTerm();

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add(string.Format(Globalization.CultureInfo.InvariantCulture,
                "GlossaryHelpInfo#{0}", Name));
            _fullHelpObject.TypeNames.Add("GlossaryHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        #region Basic Help Properties

        /// <summary>
        /// Name of glossary. 
        /// </summary>
        /// <value>Name of glossary</value>
        internal override string Name { get; } = "";

        private string GetTerm()
        {
            if (_fullHelpObject == null)
                return "";

            if (_fullHelpObject.Properties["Terms"] == null)
                return "";

            if (_fullHelpObject.Properties["Terms"].Value == null)
                return "";

            PSObject terms = (PSObject)_fullHelpObject.Properties["Terms"].Value;

            if (terms.Properties["Term"] == null)
                return "";

            if (terms.Properties["Term"].Value == null)
                return "";

            if (terms.Properties["Term"].Value.GetType().Equals(typeof(PSObject)))
            {
                PSObject term = (PSObject)terms.Properties["Term"].Value;
                return term.ToString();
            }

            if (terms.Properties["Term"].Value.GetType().Equals(typeof(PSObject[])))
            {
                PSObject[] term = (PSObject[])terms.Properties["Term"].Value;

                StringBuilder result = new StringBuilder();
                for (int i = 0; i < term.Length; i++)
                {
                    string str = term[i].ToString();

                    if (str == null)
                        continue;

                    str = str.Trim();

                    if (String.IsNullOrEmpty(str))
                        continue;

                    if (result.Length > 0)
                        result.Append(", ");

                    result.Append(str);
                }

                return result.ToString();
            }

            return "";
        }

        /// <summary>
        /// Synopsis for this glossary help.
        /// </summary>
        /// <value>Synopsis for this glossary help</value>
        internal override string Synopsis
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Help category for this glossary help, which is constantly HelpCategory.Glossary.
        /// </summary>
        /// <value>Help category for this glossary help</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Glossary;
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

        #endregion

        #region Load 

        /// <summary>
        /// Create a GlossaryHelpInfo object from an XmlNode.
        /// </summary>
        /// <param name="xmlNode">xmlNode that contains help info</param>
        /// <returns>GlossaryHelpInfo object created</returns>
        internal static GlossaryHelpInfo Load(XmlNode xmlNode)
        {
            GlossaryHelpInfo glossaryHelpInfo = new GlossaryHelpInfo(xmlNode);

            if (String.IsNullOrEmpty(glossaryHelpInfo.Name))
                return null;

            glossaryHelpInfo.AddCommonHelpProperties();

            return glossaryHelpInfo;
        }

        #endregion
    }
}
