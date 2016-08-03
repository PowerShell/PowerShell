/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Xml;
using System.Diagnostics.CodeAnalysis; // for fxcop

namespace System.Management.Automation
{
    /// <summary>
    /// 
    /// Class GeneralHelpInfo keeps track of help information to be returned by 
    /// general help provider.
    /// 
    /// </summary>
    internal class GeneralHelpInfo : HelpInfo
    {
        /// <summary>
        /// Constructor for GeneralHelpInfo
        /// </summary>
        /// <remarks>
        /// This constructor is can be called only from constructors of derived class
        /// for GeneralHelpInfo. The only way to to create a GeneralHelpInfo is through 
        /// static function
        ///     Load(XmlNode node)
        /// where some sanity check is done.
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is internal and the code path is verified.")]
        protected GeneralHelpInfo(XmlNode xmlNode)
        {
            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;
            this.Errors = mamlNode.Errors;

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add(string.Format(Globalization.CultureInfo.InvariantCulture,
                "GeneralHelpInfo#{0}", Name));
            _fullHelpObject.TypeNames.Add("GeneralHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        #region Basic Help Properties

        /// <summary>
        /// Name of help content. 
        /// </summary>
        /// <value>Name of help content</value>
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
        /// Synopsis for this general help.
        /// </summary>
        /// <value>Synopsis for this general help</value>
        internal override string Synopsis
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Help category for this general help, which is constantly HelpCategory.General.
        /// </summary>
        /// <value>Help category for general help</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.General;
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
        /// Create a GeneralHelpInfo object from an XmlNode.
        /// </summary>
        /// <param name="xmlNode">xmlNode that contains help info</param>
        /// <returns>GeneralHelpInfo object created</returns>
        internal static GeneralHelpInfo Load(XmlNode xmlNode)
        {
            GeneralHelpInfo generalHelpInfo = new GeneralHelpInfo(xmlNode);

            if (String.IsNullOrEmpty(generalHelpInfo.Name))
                return null;

            generalHelpInfo.AddCommonHelpProperties();

            return generalHelpInfo;
        }

        #endregion
    }
}
