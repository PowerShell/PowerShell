// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Text;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Class BaseCommandHelpInfo provides common functionality for
    /// extracting information from FullHelp property.
    /// </summary>
    internal abstract class BaseCommandHelpInfo : HelpInfo
    {
        internal BaseCommandHelpInfo(HelpCategory helpCategory)
            : base()
        {
            HelpCategory = helpCategory;
        }

        #region Basic Help Properties

        internal PSObject Details
        {
            get
            {
                if (this.FullHelp == null)
                    return null;

                if (this.FullHelp.Properties["Details"] == null ||
                    this.FullHelp.Properties["Details"].Value == null)
                {
                    return null;
                }

                return PSObject.AsPSObject(this.FullHelp.Properties["Details"].Value);
            }
        }

        /// <summary>
        /// Name of command.
        /// </summary>
        /// <value>Name of command</value>
        internal override string Name
        {
            get
            {
                PSObject commandDetails = this.Details;
                if (commandDetails == null)
                {
                    return string.Empty;
                }

                if (commandDetails.Properties["Name"] == null ||
                    commandDetails.Properties["Name"].Value == null)
                {
                    return string.Empty;
                }

                string name = commandDetails.Properties["Name"].Value.ToString();
                if (name == null)
                    return string.Empty;

                return name.Trim();
            }
        }

        /// <summary>
        /// Synopsis for this command help.
        /// </summary>
        /// <value>Synopsis for this command help</value>
        internal override string Synopsis
        {
            get
            {
                PSObject commandDetails = this.Details;
                if (commandDetails == null)
                {
                    return string.Empty;
                }

                if (commandDetails.Properties["Description"] == null ||
                    commandDetails.Properties["Description"].Value == null)
                {
                    return string.Empty;
                }

                object[] synopsisItems = (object[])LanguagePrimitives.ConvertTo(
                    commandDetails.Properties["Description"].Value,
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

        /// <summary>
        /// Help category for this command help, which is constantly HelpCategory.Command.
        /// </summary>
        /// <value>Help category for this command help</value>
        internal override HelpCategory HelpCategory { get; }

        /// <summary>
        /// Returns the Uri used by get-help cmdlet to show help
        /// online. Returns only the first uri found under
        /// RelatedLinks.
        /// </summary>
        /// <returns>
        /// Null if no Uri is specified by the helpinfo or a
        /// valid Uri.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Specified Uri is not valid.
        /// </exception>
        internal override Uri GetUriForOnlineHelp()
        {
            Uri result = null;
            UriFormatException uriFormatException = null;

            try
            {
                result = GetUriFromCommandPSObject(this.FullHelp);
                if (result != null)
                {
                    return result;
                }
            }
            catch (UriFormatException urie)
            {
                uriFormatException = urie;
            }
            // else get uri from CommandInfo HelpUri attribute
            result = this.LookupUriFromCommandInfo();
            if (result != null)
            {
                return result;
            }
            else if (uriFormatException != null)
            {
                throw uriFormatException;
            }

            return base.GetUriForOnlineHelp();
        }

        internal Uri LookupUriFromCommandInfo()
        {
            CommandTypes cmdTypesToLookFor = CommandTypes.Cmdlet;
            switch (this.HelpCategory)
            {
                case Automation.HelpCategory.Cmdlet:
                    cmdTypesToLookFor = CommandTypes.Cmdlet;
                    break;

                case Automation.HelpCategory.Function:
                    cmdTypesToLookFor = CommandTypes.Function;
                    break;

                case Automation.HelpCategory.ScriptCommand:
                    cmdTypesToLookFor = CommandTypes.Script;
                    break;

                case Automation.HelpCategory.ExternalScript:
                    cmdTypesToLookFor = CommandTypes.ExternalScript;
                    break;

                case Automation.HelpCategory.Filter:
                    cmdTypesToLookFor = CommandTypes.Filter;
                    break;

                case Automation.HelpCategory.Configuration:
                    cmdTypesToLookFor = CommandTypes.Configuration;
                    break;

                default:
                    return null;
            }

            string commandName = this.Name;
            string moduleName = string.Empty;
            if (this.FullHelp.Properties["ModuleName"] != null)
            {
                PSNoteProperty moduleNameNP = this.FullHelp.Properties["ModuleName"] as PSNoteProperty;
                if (moduleNameNP != null)
                {
                    LanguagePrimitives.TryConvertTo<string>(moduleNameNP.Value, CultureInfo.InvariantCulture,
                                                            out moduleName);
                }
            }

            string commandToSearch = commandName;
            if (!string.IsNullOrEmpty(moduleName))
            {
                commandToSearch = string.Format(CultureInfo.InvariantCulture,
                    $"{moduleName}\\{commandName}");
            }

            ExecutionContext context = LocalPipeline.GetExecutionContextFromTLS();
            if (context == null)
            {
                return null;
            }

            try
            {
                CommandInfo cmdInfo = null;

                if (cmdTypesToLookFor == CommandTypes.Cmdlet)
                {
                    cmdInfo = context.SessionState.InvokeCommand.GetCmdlet(commandToSearch);
                }
                else
                {
                    cmdInfo = context.SessionState.InvokeCommand.GetCommands(commandToSearch, cmdTypesToLookFor, false).FirstOrDefault();
                }

                if ((cmdInfo == null) || (cmdInfo.CommandMetadata == null))
                {
                    return null;
                }

                string uriString = cmdInfo.CommandMetadata.HelpUri;
                if (!string.IsNullOrEmpty(uriString))
                {
                    if (!System.Uri.IsWellFormedUriString(uriString, UriKind.RelativeOrAbsolute))
                    {
                        // WinBlue: 545315 Online help links are broken with localized help
                        // Example: https://go.microsoft.com/fwlink/?LinkID=113324 (moglicherwei se auf Englisch)
                        // Split the string based on <s> (space). We decided to go with this approach as
                        // UX localization authors use spaces. Correctly extracting only the wellformed URI
                        // is out-of-scope for this fix.
                        string[] tempUriSplitArray = uriString.Split(' ');
                        uriString = tempUriSplitArray[0];
                    }

                    try
                    {
                        return new System.Uri(uriString);
                        // return only the first Uri (ignore other uris)
                    }
                    catch (UriFormatException)
                    {
                        throw PSTraceSource.NewInvalidOperationException(HelpErrors.InvalidURI,
                                                                         cmdInfo.CommandMetadata.HelpUri);
                    }
                }
            }
            catch (CommandNotFoundException)
            {
            }

            return null;
        }

        internal static Uri GetUriFromCommandPSObject(PSObject commandFullHelp)
        {
            // this object knows Maml format...
            // So retrieve Uri information as per the format..
            if ((commandFullHelp == null) ||
                (commandFullHelp.Properties["relatedLinks"] == null) ||
                (commandFullHelp.Properties["relatedLinks"].Value == null))
            {
                // return the default..
                return null;
            }

            PSObject relatedLinks = PSObject.AsPSObject(commandFullHelp.Properties["relatedLinks"].Value);
            if (relatedLinks.Properties["navigationLink"] == null)
            {
                return null;
            }

            object[] navigationLinks = (object[])LanguagePrimitives.ConvertTo(
                relatedLinks.Properties["navigationLink"].Value,
                typeof(object[]),
                CultureInfo.InvariantCulture);
            foreach (object navigationLinkAsObject in navigationLinks)
            {
                if (navigationLinkAsObject == null)
                {
                    continue;
                }

                PSObject navigationLink = PSObject.AsPSObject(navigationLinkAsObject);
                PSNoteProperty uriNP = navigationLink.Properties["uri"] as PSNoteProperty;
                if (uriNP != null)
                {
                    string uriString = string.Empty;
                    LanguagePrimitives.TryConvertTo<string>(uriNP.Value, CultureInfo.InvariantCulture, out uriString);
                    if (!string.IsNullOrEmpty(uriString))
                    {
                        if (!System.Uri.IsWellFormedUriString(uriString, UriKind.RelativeOrAbsolute))
                        {
                            // WinBlue: 545315 Online help links are broken with localized help
                            // Example: https://go.microsoft.com/fwlink/?LinkID=113324 (moglicherwei se auf Englisch)
                            // Split the string based on <s> (space). We decided to go with this approach as
                            // UX localization authors use spaces. Correctly extracting only the wellformed URI
                            // is out-of-scope for this fix.
                            string[] tempUriSplitArray = uriString.Split(' ');
                            uriString = tempUriSplitArray[0];
                        }

                        try
                        {
                            return new System.Uri(uriString);
                            // return only the first Uri (ignore other uris)
                        }
                        catch (UriFormatException)
                        {
                            throw PSTraceSource.NewInvalidOperationException(HelpErrors.InvalidURI, uriString);
                        }
                    }
                }
            }

            return null;
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
            Dbg.Assert(pattern != null, "pattern cannot be null");

            string synopsis = Synopsis;
            string detailedDescription = DetailedDescription;

            synopsis ??= string.Empty;

            detailedDescription ??= string.Empty;

            return pattern.IsMatch(synopsis) || pattern.IsMatch(detailedDescription);
        }

        /// <summary>
        /// Returns help information for a parameter(s) identified by pattern.
        /// </summary>
        /// <param name="pattern">Pattern to search for parameters.</param>
        /// <returns>A collection of parameters that match pattern.</returns>
        internal override PSObject[] GetParameter(string pattern)
        {
            // this object knows Maml format...
            // So retrieve parameter information as per the format..
            if ((this.FullHelp == null) ||
                (this.FullHelp.Properties["parameters"] == null) ||
                (this.FullHelp.Properties["parameters"].Value == null))
            {
                // return the default..
                return base.GetParameter(pattern);
            }

            PSObject prmts = PSObject.AsPSObject(this.FullHelp.Properties["parameters"].Value);

            if (prmts.Properties["parameter"] == null)
            {
                return base.GetParameter(pattern);
            }

            // The Maml format simplifies array fields containing only one object
            // by transforming them into the objects themselves. To ensure the consistency
            // of the help command result we change it back into an array.
            var param = prmts.Properties["parameter"].Value;
            PSObject[] paramAsPSObjArray = new PSObject[1];

            if (param is PSObject paramPSObj)
            {
                paramAsPSObjArray[0] = paramPSObj;
            }

            PSObject[] prmtArray = (PSObject[])LanguagePrimitives.ConvertTo(
                paramAsPSObjArray[0] != null ? paramAsPSObjArray : param,
                typeof(PSObject[]),
                CultureInfo.InvariantCulture);

            if (string.IsNullOrEmpty(pattern))
            {
                return prmtArray;
            }

            List<PSObject> returnList = new List<PSObject>();
            WildcardPattern matcher = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
            foreach (PSObject prmtr in prmtArray)
            {
                if ((prmtr.Properties["name"] == null) || (prmtr.Properties["name"].Value == null))
                {
                    continue;
                }

                string prmName = prmtr.Properties["name"].Value.ToString();
                if (matcher.IsMatch(prmName))
                {
                    returnList.Add(prmtr);
                }
            }

            return returnList.ToArray();
        }

        #endregion

        #region Cmdlet Help specific Properties

        /// <summary>
        /// Detailed Description string of this cmdlet help info.
        /// </summary>
        internal string DetailedDescription
        {
            get
            {
                if (this.FullHelp == null)
                    return string.Empty;

                if (this.FullHelp.Properties["Description"] == null ||
                    this.FullHelp.Properties["Description"].Value == null)
                {
                    return string.Empty;
                }

                object[] descriptionItems = (object[])LanguagePrimitives.ConvertTo(
                    this.FullHelp.Properties["Description"].Value,
                    typeof(object[]),
                    CultureInfo.InvariantCulture);
                if (descriptionItems == null || descriptionItems.Length == 0)
                {
                    return string.Empty;
                }

                // I think every cmdlet description should at least have 400 characters...
                // so starting with this assumption..I did an average of all the cmdlet
                // help content available at the time of writing this code and came up
                // with this number.
                StringBuilder result = new StringBuilder(400);
                foreach (object descriptionItem in descriptionItems)
                {
                    if (descriptionItem == null)
                    {
                        continue;
                    }

                    PSObject descriptionObject = PSObject.AsPSObject(descriptionItem);
                    if ((descriptionObject == null) ||
                        (descriptionObject.Properties["Text"] == null) ||
                        (descriptionObject.Properties["Text"].Value == null))
                    {
                        continue;
                    }

                    string text = descriptionObject.Properties["Text"].Value.ToString();
                    result.Append(text);
                    result.Append(Environment.NewLine);
                }

                return result.ToString().Trim();
            }
        }

        #endregion
    }
}
