// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    /// <summary>
    /// Class HelpInfo keeps track of help information to be returned by help system.
    ///
    /// HelpInfo includes information in following aspect,
    ///
    ///     a. Name: the target name for help
    ///     b. Category: what category the help belongs to
    /// This class will be derived to track help info for different help categories like,
    ///     AliasHelpInfo
    ///     CommandHelpInfo
    ///     ProviderHelpInfo
    ///
    /// etc.
    ///
    /// In general, there will be a specific helpInfo child class for each kind of help provider.
    /// </summary>
    internal abstract class HelpInfo
    {
        /// <summary>
        /// Constructor for HelpInfo.
        /// </summary>
        internal HelpInfo()
        {
        }

        /// <summary>
        /// Name for help info.
        /// </summary>
        /// <value>Name for help info</value>
        internal abstract string Name
        {
            get;
        }

        /// <summary>
        /// Synopsis for help info.
        /// </summary>
        /// <value>Synopsis for help info</value>
        internal abstract string Synopsis
        {
            get;
        }

        /// <summary>
        /// Component for help info.
        /// </summary>
        /// <value>Component for help info</value>
        internal virtual string Component
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Role for help info.
        /// </summary>
        /// <value>Role for help ino</value>
        internal virtual string Role
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Functionality for help info.
        /// </summary>
        /// <value>Functionality for help info</value>
        internal virtual string Functionality
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Help category for help info.
        /// </summary>
        /// <value>Help category for help info</value>
        internal abstract HelpCategory HelpCategory
        {
            get;
        }

        /// <summary>
        /// Forward help category for this help info.
        /// </summary>
        /// <remarks>
        /// If this is not HelpCategory.None, then some other help provider
        /// (as specified in the HelpCategory bit pattern) need
        /// to process this helpInfo before it can be returned to end user.
        /// </remarks>
        /// <value>Help category to forward this helpInfo to</value>
        internal HelpCategory ForwardHelpCategory { get; set; } = HelpCategory.None;

        /// <summary>
        /// Target object in forward-help-provider that should process this HelpInfo.
        /// This will serve as auxiliary information to be passed to forward help provider.
        ///
        /// In the case of AliasHelpInfo, for example, it needs to be forwarded to
        /// CommandHelpProvider to fill in detailed helpInfo. In that case, ForwardHelpCategory
        /// will be HelpCategory.Command and the help target is the cmdlet name that matches this
        /// alias.
        /// </summary>
        /// <value>forward target object name</value>
        internal string ForwardTarget { get; set; } = string.Empty;

        /// <summary>
        /// Full help object for this help item.
        /// </summary>
        /// <value>Full help object for this help item</value>
        internal abstract PSObject FullHelp
        {
            get;
        }

        /// <summary>
        /// Short help object for this help item.
        /// </summary>
        /// <value>Short help object for this help item</value>
        internal PSObject ShortHelp
        {
            get
            {
                if (this.FullHelp == null)
                    return null;

                PSObject shortHelpObject = new PSObject(this.FullHelp);

                shortHelpObject.TypeNames.Clear();
                shortHelpObject.TypeNames.Add("HelpInfoShort");

                return shortHelpObject;
            }
        }

        /// <summary>
        /// Returns help information for a parameter(s) identified by pattern.
        /// </summary>
        /// <param name="pattern">Pattern to search for parameters.</param>
        /// <returns>A collection of parameters that match pattern.</returns>
        /// <remarks>
        /// The base method returns an empty list.
        /// </remarks>
        internal virtual PSObject[] GetParameter(string pattern)
        {
            return Array.Empty<PSObject>();
        }

        /// <summary>
        /// Returns the Uri used by get-help cmdlet to show help
        /// online.
        /// </summary>
        /// <returns>
        /// Null if no Uri is specified by the helpinfo or a
        /// valid Uri.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Specified Uri is not valid.
        /// </exception>
        internal virtual Uri GetUriForOnlineHelp()
        {
            return null;
        }

        /// <summary>
        /// Returns true if help content in help info matches the
        /// pattern contained in <paramref name="pattern"/>.
        /// The underlying code will usually run pattern.IsMatch() on
        /// content it wants to search.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        internal virtual bool MatchPatternInContent(WildcardPattern pattern)
        {
            // this is base class implementation..derived classes can choose
            // what is best to them.
            return false;
        }

        /// <summary>
        /// Add common help properties to the helpObject which is in PSObject format.
        ///
        /// Intrinsic help properties include properties like,
        ///     Name,
        ///     Synopsis
        ///     HelpCategory
        /// etc.
        ///
        /// Since help object from different help category has different format, it is
        /// needed that we generate these basic information uniformly in the help object
        /// itself.
        ///
        /// This function is normally called at the end of each child class constructor.
        /// </summary>
        /// <returns></returns>
        protected void AddCommonHelpProperties()
        {
            if (this.FullHelp == null)
                return;

            if (this.FullHelp.Properties["Name"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Name", this.Name.ToString()));
            }

            if (this.FullHelp.Properties["Category"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Category", this.HelpCategory.ToString()));
            }

            if (this.FullHelp.Properties["Synopsis"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Synopsis", this.Synopsis.ToString()));
            }

            if (this.FullHelp.Properties["Component"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Component", this.Component));
            }

            if (this.FullHelp.Properties["Role"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Role", this.Role));
            }

            if (this.FullHelp.Properties["Functionality"] == null)
            {
                this.FullHelp.Properties.Add(new PSNoteProperty("Functionality", this.Functionality));
            }
        }

        /// <summary>
        /// Update common help user-defined properties of the help object which is in PSObject format.
        /// Call this function to update Mshobject after it is created.
        /// </summary>
        /// <remarks>
        /// This function wont create new properties.This will update only user-defined properties created in
        /// <paramref name="AddCommonHelpProperties"/>
        /// </remarks>
        protected void UpdateUserDefinedDataProperties()
        {
            if (this.FullHelp == null)
                return;

            this.FullHelp.Properties.Remove("Component");
            this.FullHelp.Properties.Add(new PSNoteProperty("Component", this.Component));

            this.FullHelp.Properties.Remove("Role");
            this.FullHelp.Properties.Add(new PSNoteProperty("Role", this.Role));

            this.FullHelp.Properties.Remove("Functionality");
            this.FullHelp.Properties.Add(new PSNoteProperty("Functionality", this.Functionality));
        }

        #region Error handling

        /// <summary>
        /// This is for tracking the set of errors happened during the parsing of
        /// of this helpinfo.
        /// </summary>
        /// <value></value>
        internal Collection<ErrorRecord> Errors { get; set; }

        #endregion
    }
}
