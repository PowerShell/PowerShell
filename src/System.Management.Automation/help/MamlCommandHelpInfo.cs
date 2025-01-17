// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using System.Xml;

namespace System.Management.Automation
{
    /// <summary>
    /// Class MamlCommandHelpInfo keeps track of help information to be returned by
    /// command help provider.
    /// </summary>
    internal class MamlCommandHelpInfo : BaseCommandHelpInfo
    {
        /// <summary>
        /// Constructor for custom HelpInfo object construction
        ///
        /// This is used by the CommandHelpProvider class to generate the
        /// default help UX when no help content is present.
        /// </summary>
        /// <param name="helpObject"></param>
        /// <param name="helpCategory"></param>
        internal MamlCommandHelpInfo(PSObject helpObject, HelpCategory helpCategory)
            : base(helpCategory)
        {
            _fullHelpObject = helpObject;

            this.ForwardHelpCategory = HelpCategory.Provider;

            this.AddCommonHelpProperties();
            // set user defined data
            if (helpObject.Properties["Component"] != null)
            {
                _component = helpObject.Properties["Component"].Value as string;
            }

            if (helpObject.Properties["Role"] != null)
            {
                _role = helpObject.Properties["Role"].Value as string;
            }

            if (helpObject.Properties["Functionality"] != null)
            {
                _functionality = helpObject.Properties["Functionality"].Value as string;
            }
        }

        /// <summary>
        /// Constructor for MamlCommandHelpInfo. This constructor will call the corresponding
        /// constructor in CommandHelpInfo so that xmlNode will be converted a mamlNode.
        /// </summary>
        /// <remarks>
        /// This constructor is intentionally made private so that the only way to create
        /// MamlCommandHelpInfo is through static function
        ///     Load(XmlNode node)
        /// where some sanity check is done.
        /// </remarks>
        private MamlCommandHelpInfo(XmlNode xmlNode, HelpCategory helpCategory) : base(helpCategory)
        {
            MamlNode mamlNode = new MamlNode(xmlNode);
            _fullHelpObject = mamlNode.PSObject;

            this.Errors = mamlNode.Errors;

            // The type name hierarchy for mshObject doesn't necessary
            // reflect the hierarchy in source code. From display's point of
            // view MamlCommandHelpInfo is derived from HelpInfo.

            _fullHelpObject.TypeNames.Clear();
            if (helpCategory == HelpCategory.DscResource)
            {
                _fullHelpObject.TypeNames.Add("DscResourceHelpInfo");
            }
            else
            {
                _fullHelpObject.TypeNames.Add("MamlCommandHelpInfo");
                _fullHelpObject.TypeNames.Add("HelpInfo");
            }

            this.ForwardHelpCategory = HelpCategory.Provider;
        }

        /// <summary>
        /// Override the FullHelp PSObject of this provider-specific HelpInfo with generic help.
        /// </summary>
        internal void OverrideProviderSpecificHelpWithGenericHelp(HelpInfo genericHelpInfo)
        {
            PSObject genericHelpMaml = genericHelpInfo.FullHelp;
            MamlUtil.OverrideName(_fullHelpObject, genericHelpMaml);
            MamlUtil.OverridePSTypeNames(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependSyntax(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependDetailedDescription(_fullHelpObject, genericHelpMaml);
            MamlUtil.OverrideParameters(_fullHelpObject, genericHelpMaml);
            MamlUtil.PrependNotes(_fullHelpObject, genericHelpMaml);
            MamlUtil.AddCommonProperties(_fullHelpObject, genericHelpMaml);
        }

        #region Basic Help Properties

        private readonly PSObject _fullHelpObject;

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
        /// Examples string of this cmdlet help info.
        /// </summary>
        private string Examples
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "Examples");
            }
        }

        /// <summary>
        /// Parameters string of this cmdlet help info.
        /// </summary>
        private string Parameters
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "Parameters");
            }
        }

        /// <summary>
        /// Notes string of this cmdlet help info.
        /// </summary>
        private string Notes
        {
            get
            {
                return ExtractTextForHelpProperty(this.FullHelp, "alertset");
            }
        }

        #endregion

        #region Component, Role, Features

        // Component, Role, Functionality are required by exchange for filtering
        // help contents to be returned from help system.
        //
        // Following is how this is going to work,
        //    1. Each command will optionally include component, role and functionality
        //       information. This information is discovered from help content
        //       from xml tags <component>, <role>, <functionality> respectively
        //       as part of command metadata.
        //    2. From command line, end user can request help for commands for
        //       particular component, role and functionality using parameters like
        //       -component, -role, -functionality.
        //    3. At runtime, help engine will match against component/role/functionality
        //       criteria before returning help results.
        //

        private string _component = null;
        /// <summary>
        /// Component for this command.
        /// </summary>
        /// <value></value>
        internal override string Component
        {
            get
            {
                return _component;
            }
        }

        private string _role = null;
        /// <summary>
        /// Role for this command.
        /// </summary>
        /// <value></value>
        internal override string Role
        {
            get
            {
                return _role;
            }
        }

        private string _functionality = null;
        /// <summary>
        /// Functionality for this command.
        /// </summary>
        /// <value></value>
        internal override string Functionality
        {
            get
            {
                return _functionality;
            }
        }

        internal void SetAdditionalDataFromHelpComment(string component, string functionality, string role)
        {
            _component = component;
            _functionality = functionality;
            _role = role;

            // component,role,functionality is part of common help..
            // Update these properties as we have new data now..
            this.UpdateUserDefinedDataProperties();
        }

        /// <summary>
        /// Add user-defined command help data to command help.
        /// </summary>
        /// <param name="userDefinedData">User defined data object.</param>
        internal void AddUserDefinedData(UserDefinedHelpData userDefinedData)
        {
            if (userDefinedData == null)
                return;

            string propertyValue;
            if (userDefinedData.Properties.TryGetValue("component", out propertyValue))
            {
                _component = propertyValue;
            }

            if (userDefinedData.Properties.TryGetValue("role", out propertyValue))
            {
                _role = propertyValue;
            }

            if (userDefinedData.Properties.TryGetValue("functionality", out propertyValue))
            {
                _functionality = propertyValue;
            }

            // component,role,functionality is part of common help..
            // Update these properties as we have new data now..
            this.UpdateUserDefinedDataProperties();
        }

        #endregion

        #region Load

        /// <summary>
        /// Create a MamlCommandHelpInfo object from an XmlNode.
        /// </summary>
        /// <param name="xmlNode">XmlNode that contains help info.</param>
        /// <param name="helpCategory">Help category this maml object fits into.</param>
        /// <returns>MamlCommandHelpInfo object created.</returns>
        internal static MamlCommandHelpInfo Load(XmlNode xmlNode, HelpCategory helpCategory)
        {
            MamlCommandHelpInfo mamlCommandHelpInfo = new MamlCommandHelpInfo(xmlNode, helpCategory);

            if (string.IsNullOrEmpty(mamlCommandHelpInfo.Name))
                return null;

            mamlCommandHelpInfo.AddCommonHelpProperties();

            return mamlCommandHelpInfo;
        }

        #endregion

        #region Provider specific help

#if V2
        /// <summary>
        /// Merge the provider specific help with current command help.
        ///
        /// The cmdletHelp and dynamicParameterHelp is normally retrieved from ProviderHelpProvider.
        /// </summary>
        /// <remarks>
        /// A new MamlCommandHelpInfo is created to avoid polluting the provider help cache.
        /// </remarks>
        /// <param name="cmdletHelp">Provider-specific cmdletHelp to merge into current MamlCommandHelpInfo object.</param>
        /// <param name="dynamicParameterHelp">Provider-specific dynamic parameter help to merge into current MamlCommandHelpInfo object.</param>
        /// <returns>Merged command help info object.</returns>
        internal MamlCommandHelpInfo MergeProviderSpecificHelp(PSObject cmdletHelp, PSObject[] dynamicParameterHelp)
        {
            if (this._fullHelpObject == null)
                return null;

            MamlCommandHelpInfo result = (MamlCommandHelpInfo)this.MemberwiseClone();

            // We will need to use a deep clone of _fullHelpObject
            // to avoid _fullHelpObject being get terminated.
            result._fullHelpObject = this._fullHelpObject.Copy();

            if (cmdletHelp != null)
                result._fullHelpObject.Properties.Add(new PSNoteProperty("PS_Cmdlet", cmdletHelp));

            if (dynamicParameterHelp != null)
                result._fullHelpObject.Properties.Add(new PSNoteProperty("PS_DynamicParameters", dynamicParameterHelp));

            return result;
        }
#endif

        #endregion

        #region Helper Methods and Overloads

        /// <summary>
        /// Extracts text for a given property from the full help object.
        /// </summary>
        /// <param name="psObject">FullHelp object.</param>
        /// <param name="propertyName">
        /// Name of the property for which text needs to be extracted.
        /// </param>
        /// <returns></returns>
        private static string ExtractTextForHelpProperty(PSObject psObject, string propertyName)
        {
            if (psObject == null)
                return string.Empty;

            if (psObject.Properties[propertyName] == null ||
                psObject.Properties[propertyName].Value == null)
            {
                return string.Empty;
            }

            return ExtractText(PSObject.AsPSObject(psObject.Properties[propertyName].Value));
        }

        /// <summary>
        /// Given a PSObject, this method will traverse through the objects properties,
        /// extracts content from properties that are of type System.String, appends them
        /// together and returns.
        /// </summary>
        /// <param name="psObject"></param>
        /// <returns></returns>
        private static string ExtractText(PSObject psObject)
        {
            if (psObject == null)
            {
                return string.Empty;
            }

            // I think every cmdlet description should at least have 400 characters...
            // so starting with this assumption..I did an average of all the cmdlet
            // help content available at the time of writing this code and came up
            // with this number.
            StringBuilder result = new StringBuilder(400);
            foreach (PSPropertyInfo propertyInfo in psObject.Properties)
            {
                string typeNameOfValue = propertyInfo.TypeNameOfValue;
                switch (typeNameOfValue.ToLowerInvariant())
                {
                    case "system.boolean":
                    case "system.int32":
                    case "system.object":
                    case "system.object[]":
                        continue;
                    case "system.string":
                        result.Append((string)LanguagePrimitives.ConvertTo(propertyInfo.Value,
                            typeof(string), CultureInfo.InvariantCulture));
                        break;
                    case "system.management.automation.psobject[]":
                        PSObject[] items = (PSObject[])LanguagePrimitives.ConvertTo(
                                propertyInfo.Value,
                                typeof(PSObject[]),
                                CultureInfo.InvariantCulture);
                        foreach (PSObject item in items)
                        {
                            result.Append(ExtractText(item));
                        }

                        break;
                    case "system.management.automation.psobject":
                        result.Append(ExtractText(PSObject.AsPSObject(propertyInfo.Value)));
                        break;
                    default:
                        result.Append(ExtractText(PSObject.AsPSObject(propertyInfo.Value)));
                        break;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns true if help content in help info matches the
        /// pattern contained in <paramref name="pattern"/>.
        /// The underlying code will usually run pattern.IsMatch() on
        /// content it wants to search.
        /// Cmdlet help info looks for pattern in Synopsis and
        /// DetailedDescription.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal override bool MatchPatternInContent(WildcardPattern pattern)
        {
            System.Management.Automation.Diagnostics.Assert(pattern != null, "pattern cannot be null");

            string synopsis = Synopsis;
            if ((!string.IsNullOrEmpty(synopsis)) && (pattern.IsMatch(synopsis)))
            {
                return true;
            }

            string detailedDescription = DetailedDescription;
            if ((!string.IsNullOrEmpty(detailedDescription)) && (pattern.IsMatch(detailedDescription)))
            {
                return true;
            }

            string examples = Examples;
            if ((!string.IsNullOrEmpty(examples)) && (pattern.IsMatch(examples)))
            {
                return true;
            }

            string notes = Notes;
            if ((!string.IsNullOrEmpty(notes)) && (pattern.IsMatch(notes)))
            {
                return true;
            }

            string parameters = Parameters;
            if ((!string.IsNullOrEmpty(parameters)) && (pattern.IsMatch(parameters)))
            {
                return true;
            }

            return false;
        }

        internal MamlCommandHelpInfo Copy()
        {
            MamlCommandHelpInfo result = new MamlCommandHelpInfo(_fullHelpObject.Copy(), this.HelpCategory);
            return result;
        }

        internal MamlCommandHelpInfo Copy(HelpCategory newCategoryToUse)
        {
            MamlCommandHelpInfo result = new MamlCommandHelpInfo(_fullHelpObject.Copy(), newCategoryToUse);
            result.FullHelp.Properties["Category"].Value = newCategoryToUse.ToString();
            return result;
        }

        #endregion
    }
}
