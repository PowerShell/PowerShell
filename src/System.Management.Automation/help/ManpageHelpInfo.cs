// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;      // for fxcop
using System.Management.Automation.Help;    // for DefaultCommandHelpObjectBuilder

namespace System.Management.Automation
{
    /// <summary>
    /// Stores help information related to commands Manpage.
    /// </summary>
    internal class ManpageHelpInfo : BaseCommandHelpInfo
    {
        /// <summary>
        /// Initializes a new instance of the ManpagesHelpInfo class.
        /// </summary>
        /// <remarks>
        /// The constructor is private. The only way to create an
        /// ManpageHelpInfo object is through static method <see cref="GetHelpInfo"/>
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        private ManpageHelpInfo(
            ManpageInfo manpageInfo,
            HelpCategory localHelpCategory) : base(localHelpCategory)
        {
            _fullHelpObject = new PSObject();

            Name = manpageInfo.Name;
            if (!string.IsNullOrEmpty(manpageInfo.ManSectionNum))
            {
                Name += " (" + manpageInfo.ManSectionNum + ")";
            }

            Synopsis = manpageInfo.ShortDescription;

            _fullHelpObject.Properties.Add(new PSNoteProperty("Name", Name));
            _fullHelpObject.Properties.Add(new PSNoteProperty("ManNameWithoutSection", manpageInfo.Name));
            _fullHelpObject.Properties.Add(new PSNoteProperty("ManSectionNum", manpageInfo.ManSectionNum));
            _fullHelpObject.Properties.Add(new PSNoteProperty("ShortDescription", manpageInfo.ShortDescription));
            _fullHelpObject.Properties.Add(new PSNoteProperty("Remarks", "For full help use `man " + manpageInfo.Name + "`"));

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add("ManpageHelpInfo");
            _fullHelpObject.TypeNames.Add("ExtendedManpageHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        /// <summary>
        /// Returns the name of manpage help.
        /// </summary>
        /// <value>Name of manpage help.</value>
        internal override string Name { get; } = string.Empty;

        /// <summary>
        /// Returns synopsis of manpage help.
        /// </summary>
        /// <value>Synopsis of manpage help.</value>
        internal override string Synopsis { get; } = string.Empty;

        /// <summary>
        /// Help category for manpage help. This is always HelpCategory.Manpage.
        /// </summary>
        /// <value>Help category for manpage help.</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Manpage;
            }
        }

        private readonly PSObject _fullHelpObject;

        /// <summary>
        /// Returns full help object for manpage help.
        /// </summary>
        /// <value>Full help object of manpage help.</value>
        internal override PSObject FullHelp
        {
            get
            {
                return _fullHelpObject;
            }
        }

        /// <summary>
        /// Creates a ManpagesHelpInfo instance based on an ManpageInfo object.
        /// This is the only way to create ManpageHelpInfo object from outside this class.
        /// </summary>
        /// <param name="manpageInfo">ManpageInfo object for which to create ManpageHelpInfo object.</param>
        /// <returns>ManpageHelpInfo object.</returns>
        internal static ManpageHelpInfo GetHelpInfo(ManpageInfo manpageInfo)
        {
            if (manpageInfo == null)
            {
                return null;
            }

            ManpageHelpInfo manpageHelpInfo = new ManpageHelpInfo(manpageInfo, HelpCategory.Manpage);

            if (string.IsNullOrEmpty(manpageHelpInfo.Name))
            {
                return null;
            }

            manpageHelpInfo.AddCommonHelpProperties();

            return manpageHelpInfo;
        }
    }
}
