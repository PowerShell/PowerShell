// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis; // for fxcop

namespace System.Management.Automation
{
    /// <summary>
    /// Stores help information related to Alias Commands.
    /// </summary>
    internal sealed class AliasHelpInfo : HelpInfo
    {
        /// <summary>
        /// Initializes a new instance of the AliasHelpInfo class.
        /// </summary>
        /// <remarks>
        /// The constructor is private. The only way to create an
        /// AliasHelpInfo object is through static method <see cref="GetHelpInfo"/>
        /// </remarks>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        private AliasHelpInfo(AliasInfo aliasInfo)
        {
            _fullHelpObject = new PSObject();

            string name = (aliasInfo.ResolvedCommand == null) ? aliasInfo.UnresolvedCommandName : aliasInfo.ResolvedCommand.Name;

            this.ForwardTarget = name;
            // A Cmdlet/Function/Script etc can have alias.
            this.ForwardHelpCategory = HelpCategory.Cmdlet |
                HelpCategory.Function | HelpCategory.ExternalScript | HelpCategory.ScriptCommand | HelpCategory.Filter;

            if (!string.IsNullOrEmpty(aliasInfo.Name))
            {
                Name = aliasInfo.Name.Trim();
            }

            if (!string.IsNullOrEmpty(name))
            {
                Synopsis = name.Trim();
            }

            _fullHelpObject.TypeNames.Clear();
            _fullHelpObject.TypeNames.Add(string.Format(Globalization.CultureInfo.InvariantCulture,
                $"AliasHelpInfo#{Name}"));
            _fullHelpObject.TypeNames.Add("AliasHelpInfo");
            _fullHelpObject.TypeNames.Add("HelpInfo");
        }

        /// <summary>
        /// Returns the name of alias help.
        /// </summary>
        /// <value>Name of alias help.</value>
        internal override string Name { get; } = string.Empty;

        /// <summary>
        /// Returns synopsis of alias help.
        /// </summary>
        /// <value>Synopsis of alias help.</value>
        internal override string Synopsis { get; } = string.Empty;

        /// <summary>
        /// Help category for alias help. This is always HelpCategory.Alias.
        /// </summary>
        /// <value>Help category for alias help</value>
        internal override HelpCategory HelpCategory
        {
            get
            {
                return HelpCategory.Alias;
            }
        }

        private readonly PSObject _fullHelpObject;

        /// <summary>
        /// Returns full help object for alias help.
        /// </summary>
        /// <value>Full help object of alias help.</value>
        internal override PSObject FullHelp
        {
            get
            {
                return _fullHelpObject;
            }
        }

        /// <summary>
        /// Creates an AliasHelpInfo instance based on an AliasInfo object.
        /// This is the only way to create AliasHelpInfo object from outside this class.
        /// </summary>
        /// <param name="aliasInfo">AliasInfo object for which to create AliasHelpInfo object.</param>
        /// <returns>AliasHelpInfo object.</returns>
        internal static AliasHelpInfo GetHelpInfo(AliasInfo aliasInfo)
        {
            if (aliasInfo == null)
                return null;

            if (aliasInfo.ResolvedCommand == null && aliasInfo.UnresolvedCommandName == null)
                return null;

            AliasHelpInfo aliasHelpInfo = new AliasHelpInfo(aliasInfo);

            if (string.IsNullOrEmpty(aliasHelpInfo.Name))
                return null;

            aliasHelpInfo.AddCommonHelpProperties();

            return aliasHelpInfo;
        }
    }
}
