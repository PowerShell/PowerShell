using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.PowerShell.Commands;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
#if CORECLR
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#endif

namespace System.Management.Automation
{
    internal enum SuggestionMatchType
    {
        Command = 0,
        Error = 1,
        Dynamic = 2
    }

    #region Public HostUtilities Class

    /// <summary>
    /// Implements utility methods that might be used by Hosts.
    /// </summary>
    public static class HostUtilities
    {
        #region Internal Access

        private static string s_checkForCommandInCurrentDirectoryScript = @"
            [System.Diagnostics.DebuggerHidden()]
            param()

            $foundSuggestion = $false
        
            if($lastError -and
                ($lastError.Exception -is ""System.Management.Automation.CommandNotFoundException""))
            {
                $escapedCommand = [System.Management.Automation.WildcardPattern]::Escape($lastError.TargetObject)
                $foundSuggestion = @(Get-Command ($ExecutionContext.SessionState.Path.Combine(""."", $escapedCommand)) -ErrorAction Ignore).Count -gt 0
            }

            $foundSuggestion
        ";

        private static string s_createCommandExistsInCurrentDirectoryScript = @"
            [System.Diagnostics.DebuggerHidden()]
            param([string] $formatString)

            $formatString -f $lastError.TargetObject,"".\$($lastError.TargetObject)""
        ";

        private static ArrayList s_suggestions = new ArrayList(
            new Hashtable[] {
                NewSuggestion(1, "Transactions", SuggestionMatchType.Command, "^Start-Transaction",
                    SuggestionStrings.Suggestion_StartTransaction, true),
                NewSuggestion(2, "Transactions", SuggestionMatchType.Command, "^Use-Transaction",
                    SuggestionStrings.Suggestion_UseTransaction, true),
                NewSuggestion(3, "General", SuggestionMatchType.Dynamic,
                    ScriptBlock.CreateDelayParsedScriptBlock(s_checkForCommandInCurrentDirectoryScript, isProductCode: true),
                    ScriptBlock.CreateDelayParsedScriptBlock(s_createCommandExistsInCurrentDirectoryScript, isProductCode: true),
                    new object[] { CodeGeneration.EscapeSingleQuotedStringContent(SuggestionStrings.Suggestion_CommandExistsInCurrentDirectory) },
                    true)
            }
        );

        #region GetProfileCommands
        /// <summary>
        /// Gets a PSObject whose base object is currentUserCurrentHost and with notes for the other 4 parameters.
        /// </summary>
        /// <param name="allUsersAllHosts">The profile file name for all users and all hosts.</param>
        /// <param name="allUsersCurrentHost">The profile file name for all users and current host.</param>
        /// <param name="currentUserAllHosts">The profile file name for current user and all hosts.</param>
        /// <param name="currentUserCurrentHost">The profile  name for current user and current host.</param>
        /// <returns>A PSObject whose base object is currentUserCurrentHost and with notes for the other 4 parameters.</returns>
        internal static PSObject GetDollarProfile(string allUsersAllHosts, string allUsersCurrentHost, string currentUserAllHosts, string currentUserCurrentHost)
        {
            PSObject returnValue = new PSObject(currentUserCurrentHost);
            returnValue.Properties.Add(new PSNoteProperty("AllUsersAllHosts", allUsersAllHosts));
            returnValue.Properties.Add(new PSNoteProperty("AllUsersCurrentHost", allUsersCurrentHost));
            returnValue.Properties.Add(new PSNoteProperty("CurrentUserAllHosts", currentUserAllHosts));
            returnValue.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", currentUserCurrentHost));
            return returnValue;
        }


        /// <summary>
        /// Gets an array of commands that can be run sequentially to set $profile and run the profile commands.
        /// </summary>
        /// <param name="shellId">The id identifying the host or shell used in profile file names.</param>
        /// <returns></returns>
        internal static PSCommand[] GetProfileCommands(string shellId)
        {
            return HostUtilities.GetProfileCommands(shellId, false);
        }

        /// <summary>
        /// Gets the object that serves as a value to $profile and the paths on it
        /// </summary>
        /// <param name="shellId">The id identifying the host or shell used in profile file names.</param>
        /// <param name="useTestProfile">used from test not to overwrite the profile file names from development boxes</param>
        /// <param name="allUsersAllHosts">path for all users and all hosts</param>
        /// <param name="currentUserAllHosts">path for current user and all hosts</param>
        /// <param name="allUsersCurrentHost">path for all users current host</param>
        /// <param name="currentUserCurrentHost">path for current user and current host</param>
        /// <param name="dollarProfile">the object that serves as a value to $profile</param>
        /// <returns></returns>
        internal static void GetProfileObjectData(string shellId, bool useTestProfile, out string allUsersAllHosts, out string allUsersCurrentHost, out string currentUserAllHosts, out string currentUserCurrentHost, out PSObject dollarProfile)
        {
            allUsersAllHosts = HostUtilities.GetFullProfileFileName(null, false, useTestProfile);
            allUsersCurrentHost = HostUtilities.GetFullProfileFileName(shellId, false, useTestProfile);
            currentUserAllHosts = HostUtilities.GetFullProfileFileName(null, true, useTestProfile);
            currentUserCurrentHost = HostUtilities.GetFullProfileFileName(shellId, true, useTestProfile);
            dollarProfile = HostUtilities.GetDollarProfile(allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost);
        }

        /// <summary>
        /// Gets an array of commands that can be run sequentially to set $profile and run the profile commands.
        /// </summary>
        /// <param name="shellId">The id identifying the host or shell used in profile file names.</param>
        /// <param name="useTestProfile">used from test not to overwrite the profile file names from development boxes</param>
        /// <returns></returns>
        internal static PSCommand[] GetProfileCommands(string shellId, bool useTestProfile)
        {
            List<PSCommand> commands = new List<PSCommand>();
            string allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost;
            PSObject dollarProfile;
            HostUtilities.GetProfileObjectData(shellId, useTestProfile, out allUsersAllHosts, out allUsersCurrentHost, out currentUserAllHosts, out currentUserCurrentHost, out dollarProfile);

            PSCommand command = new PSCommand();
            command.AddCommand("set-variable");
            command.AddParameter("Name", "profile");
            command.AddParameter("Value", dollarProfile);
            command.AddParameter("Option", ScopedItemOptions.None);
            commands.Add(command);

            string[] profilePaths = new string[] { allUsersAllHosts, allUsersCurrentHost, currentUserAllHosts, currentUserCurrentHost };
            foreach (string profilePath in profilePaths)
            {
                if (!System.IO.File.Exists(profilePath))
                {
                    continue;
                }
                command = new PSCommand();
                command.AddCommand(profilePath, false);
                commands.Add(command);
            }

            return commands.ToArray();
        }

        /// <summary>
        /// Used to get all profile file names for the current or all hosts and for the current or all users.
        /// </summary>
        /// <param name="shellId">null for all hosts, not null for the specified host</param>
        /// <param name="forCurrentUser">false for all users, true for the current user.</param>
        /// <returns>The profile file name matching the parameters.</returns>
        internal static string GetFullProfileFileName(string shellId, bool forCurrentUser)
        {
            return HostUtilities.GetFullProfileFileName(shellId, forCurrentUser, false);
        }

        /// <summary>
        /// Used to get all profile file names for the current or all hosts and for the current or all users.
        /// </summary>
        /// <param name="shellId">null for all hosts, not null for the specified host</param>
        /// <param name="forCurrentUser">false for all users, true for the current user.</param>
        /// <param name="useTestProfile">used from test not to overwrite the profile file names from development boxes</param>
        /// <returns>The profile file name matching the parameters.</returns>
        internal static string GetFullProfileFileName(string shellId, bool forCurrentUser, bool useTestProfile)
        {
            string basePath = null;

            if (forCurrentUser)
            {
                basePath = Utils.GetUserConfigurationDirectory();
            }
            else
            {
                basePath = GetAllUsersFolderPath(shellId);
                if (string.IsNullOrEmpty(basePath))
                {
                    return "";
                }
            }

            string profileName = useTestProfile ? "profile_test.ps1" : "profile.ps1";

            if (!string.IsNullOrEmpty(shellId))
            {
                profileName = shellId + "_" + profileName;
            }

            string fullPath = basePath = IO.Path.Combine(basePath, profileName);

            return fullPath;
        }

        /// <summary>
        /// Used internally in GetFullProfileFileName to get the base path for all users profiles.
        /// </summary>
        /// <param name="shellId">The shellId to use.</param>
        /// <returns>the base path for all users profiles.</returns>
        private static string GetAllUsersFolderPath(string shellId)
        {
            string folderPath = string.Empty;
            try
            {
                folderPath = Utils.GetApplicationBase(shellId);
            }
            catch (System.Security.SecurityException)
            {
            }

            return folderPath;
        }
        #endregion GetProfileCommands

        /// <summary>
        /// Gets the first <paramref name="maxLines"/> lines of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">string we want to limit the number of lines</param>
        /// <param name="maxLines"> maximum number of lines to be returned</param>
        /// <returns>The first lines of <paramref name="source"/>.</returns>
        internal static string GetMaxLines(string source, int maxLines)
        {
            if (String.IsNullOrEmpty(source))
            {
                return String.Empty;
            }

            StringBuilder returnValue = new StringBuilder();

            for (int i = 0, lineCount = 1; i < source.Length; i++)
            {
                char c = source[i];

                if (c == '\n')
                {
                    lineCount++;
                }

                returnValue.Append(c);

                if (lineCount == maxLines)
                {
                    returnValue.Append("...");
                    break;
                }
            }

            return returnValue.ToString();
        }

        internal static ArrayList GetSuggestion(Runspace runspace)
        {
            LocalRunspace localRunspace = runspace as LocalRunspace;
            if (localRunspace == null) { return new ArrayList(); }

            // Get the last value of $?
            bool questionMarkVariableValue = localRunspace.ExecutionContext.QuestionMarkVariableValue;

            // Get the last history item
            History history = localRunspace.History;
            HistoryInfo[] entries = history.GetEntries(-1, 1, true);

            if (entries.Length == 0)
                return new ArrayList();

            HistoryInfo lastHistory = entries[0];

            // Get the last error
            ArrayList errorList = (ArrayList)localRunspace.GetExecutionContext.DollarErrorVariable;
            Object lastError = null;

            if (errorList.Count > 0)
            {
                lastError = errorList[0] as Exception;
                ErrorRecord lastErrorRecord = null;

                // The error was an actual ErrorRecord
                if (lastError == null)
                {
                    lastErrorRecord = errorList[0] as ErrorRecord;
                }
                else if (lastError is RuntimeException)
                {
                    lastErrorRecord = ((RuntimeException)lastError).ErrorRecord;
                }

                // If we got information about the error invocation,
                // we can be more careful with the errors we pass along
                if ((lastErrorRecord != null) && (lastErrorRecord.InvocationInfo != null))
                {
                    if (lastErrorRecord.InvocationInfo.HistoryId == lastHistory.Id)
                        lastError = lastErrorRecord;
                    else
                        lastError = null;
                }
            }

            Runspace oldDefault = null;
            bool changedDefault = false;
            if (Runspace.DefaultRunspace != runspace)
            {
                oldDefault = Runspace.DefaultRunspace;
                changedDefault = true;
                Runspace.DefaultRunspace = runspace;
            }

            ArrayList suggestions = null;

            try
            {
                suggestions = GetSuggestion(lastHistory, lastError, errorList);
            }
            finally
            {
                if (changedDefault)
                {
                    Runspace.DefaultRunspace = oldDefault;
                }
            }

            // Restore $?
            localRunspace.ExecutionContext.QuestionMarkVariableValue = questionMarkVariableValue;
            return suggestions;
        }

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        internal static ArrayList GetSuggestion(HistoryInfo lastHistory, Object lastError, ArrayList errorList)
        {
            ArrayList returnSuggestions = new ArrayList();

            PSModuleInfo invocationModule = new PSModuleInfo(true);
            invocationModule.SessionState.PSVariable.Set("lastHistory", lastHistory);
            invocationModule.SessionState.PSVariable.Set("lastError", lastError);

            int initialErrorCount = 0;

            // Go through all of the suggestions
            foreach (Hashtable suggestion in s_suggestions)
            {
                initialErrorCount = errorList.Count;

                // Make sure the rule is enabled
                if (!LanguagePrimitives.IsTrue(suggestion["Enabled"]))
                    continue;

                SuggestionMatchType matchType = (SuggestionMatchType)LanguagePrimitives.ConvertTo(
                    suggestion["MatchType"],
                    typeof(SuggestionMatchType),
                    CultureInfo.InvariantCulture);

                // If this is a dynamic match, evaluate the ScriptBlock
                if (matchType == SuggestionMatchType.Dynamic)
                {
                    object result = null;

                    ScriptBlock evaluator = suggestion["Rule"] as ScriptBlock;
                    if (evaluator == null)
                    {
                        suggestion["Enabled"] = false;

                        throw new ArgumentException(
                            SuggestionStrings.RuleMustBeScriptBlock, "Rule");
                    }

                    try
                    {
                        result = invocationModule.Invoke(evaluator, null);
                    }
                    catch (Exception e)
                    {
                        // Catch-all OK. This is a third-party call-out.
                        CommandProcessorBase.CheckForSevereException(e);

                        suggestion["Enabled"] = false;
                        continue;
                    }

                    // If it returned results, evaluate its suggestion
                    if (LanguagePrimitives.IsTrue(result))
                    {
                        string suggestionText = GetSuggestionText(suggestion["Suggestion"], (object[])suggestion["SuggestionArgs"], invocationModule);

                        if (!String.IsNullOrEmpty(suggestionText))
                        {
                            string returnString = String.Format(
                                CultureInfo.CurrentCulture,
                                "Suggestion [{0},{1}]: {2}",
                                (int)suggestion["Id"],
                                (string)suggestion["Category"],
                                suggestionText);

                            returnSuggestions.Add(returnString);
                        }
                    }
                }
                else
                {
                    string matchText = String.Empty;

                    // Otherwise, this is a Regex match against the
                    // command or error
                    if (matchType == SuggestionMatchType.Command)
                    {
                        matchText = lastHistory.CommandLine;
                    }
                    else if (matchType == SuggestionMatchType.Error)
                    {
                        if (lastError != null)
                        {
                            Exception lastException = lastError as Exception;
                            if (lastException != null)
                            {
                                matchText = lastException.Message;
                            }
                            else
                            {
                                matchText = lastError.ToString();
                            }
                        }
                    }
                    else
                    {
                        suggestion["Enabled"] = false;

                        throw new ArgumentException(
                            SuggestionStrings.InvalidMatchType,
                            "MatchType");
                    }

                    // If the text matches, evaluate the suggestion
                    if (Regex.IsMatch(matchText, (string)suggestion["Rule"], RegexOptions.IgnoreCase))
                    {
                        string suggestionText = GetSuggestionText(suggestion["Suggestion"], (object[])suggestion["SuggestionArgs"], invocationModule);

                        if (!String.IsNullOrEmpty(suggestionText))
                        {
                            string returnString = String.Format(
                                CultureInfo.CurrentCulture,
                                "Suggestion [{0},{1}]: {2}",
                                (int)suggestion["Id"],
                                (string)suggestion["Category"],
                                suggestionText);

                            returnSuggestions.Add(returnString);
                        }
                    }
                }

                // If the rule generated an error, disable it
                if (errorList.Count != initialErrorCount)
                {
                    suggestion["Enabled"] = false;
                }
            }


            return returnSuggestions;
        }

        /// <summary>
        /// Remove the GUID from the message if the message is in the pre-defined format
        /// </summary>
        /// <param name="message"></param>
        /// <param name="matchPattern"></param>
        /// <returns></returns>
        internal static string RemoveGuidFromMessage(string message, out bool matchPattern)
        {
            matchPattern = false;
            if (String.IsNullOrEmpty(message))
                return message;

            const string pattern = @"^([\d\w]{8}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{12}:).*";
            Match matchResult = Regex.Match(message, pattern);
            if (matchResult.Success)
            {
                string partToRemove = matchResult.Groups[1].Captures[0].Value;
                message = message.Remove(0, partToRemove.Length);
                matchPattern = true;
            }
            return message;
        }

        internal static string RemoveIdentifierInfoFromMessage(string message, out bool matchPattern)
        {
            matchPattern = false;
            if (String.IsNullOrEmpty(message))
                return message;

            const string pattern = @"^([\d\w]{8}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{4}\-[\d\w]{12}:\[.*\]:).*";
            Match matchResult = Regex.Match(message, pattern);
            if (matchResult.Success)
            {
                string partToRemove = matchResult.Groups[1].Captures[0].Value;
                message = message.Remove(0, partToRemove.Length);
                matchPattern = true;
            }
            return message;
        }

        /// <summary>
        /// Create suggestion with string rule and suggestion.
        /// </summary>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, string rule, string suggestion, bool enabled)
        {
            Hashtable result = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            result["Id"] = id;
            result["Category"] = category;
            result["MatchType"] = matchType;
            result["Rule"] = rule;
            result["Suggestion"] = suggestion;
            result["Enabled"] = enabled;

            return result;
        }

        /// <summary>
        /// Create suggestion with scriptblock rule and suggestion.
        /// </summary>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, ScriptBlock rule, ScriptBlock suggestion, bool enabled)
        {
            Hashtable result = new Hashtable(StringComparer.CurrentCultureIgnoreCase);

            result["Id"] = id;
            result["Category"] = category;
            result["MatchType"] = matchType;
            result["Rule"] = rule;
            result["Suggestion"] = suggestion;
            result["Enabled"] = enabled;

            return result;
        }

        /// <summary>
        /// Create suggestion with scriptblock rule and scriptblock suggestion with arguments.
        /// </summary>
        private static Hashtable NewSuggestion(int id, string category, SuggestionMatchType matchType, ScriptBlock rule, ScriptBlock suggestion, object[] suggestionArgs, bool enabled)
        {
            Hashtable result = NewSuggestion(id, category, matchType, rule, suggestion, enabled);
            result.Add("SuggestionArgs", suggestionArgs);

            return result;
        }

        /// <summary>
        /// Get suggestion text from suggestion scriptblock
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Need to keep this for legacy reflection based use")]
        private static string GetSuggestionText(Object suggestion, PSModuleInfo invocationModule)
        {
            return GetSuggestionText(suggestion, null, invocationModule);
        }

        /// <summary>
        /// Get suggestion text from suggestion scriptblock with arguments.
        /// </summary>
        private static string GetSuggestionText(Object suggestion, object[] suggestionArgs, PSModuleInfo invocationModule)
        {
            if (suggestion is ScriptBlock)
            {
                ScriptBlock suggestionScript = (ScriptBlock)suggestion;

                object result = null;
                try
                {
                    result = invocationModule.Invoke(suggestionScript, suggestionArgs);
                }
                catch (Exception e)
                {
                    // Catch-all OK. This is a third-party call-out.
                    CommandProcessorBase.CheckForSevereException(e);

                    return String.Empty;
                }

                return (string)LanguagePrimitives.ConvertTo(result, typeof(string), CultureInfo.CurrentCulture);
            }
            else
            {
                return (string)LanguagePrimitives.ConvertTo(suggestion, typeof(string), CultureInfo.CurrentCulture);
            }
        }

        internal static PSCredential CredUIPromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options,
            IntPtr parentHWND)
        {
            PSCredential cred = null;

            // From WinCred.h
            const int CRED_MAX_USERNAME_LENGTH = (256 + 1 + 256);
            const int CRED_MAX_CREDENTIAL_BLOB_SIZE = 512;
            const int CRED_MAX_PASSWORD_LENGTH = CRED_MAX_CREDENTIAL_BLOB_SIZE / 2;
            const int CREDUI_MAX_MESSAGE_LENGTH = 1024;
            const int CREDUI_MAX_CAPTION_LENGTH = 128;

            // Populate the UI text with defaults, if required
            if (string.IsNullOrEmpty(caption))
            {
                caption = CredUI.PromptForCredential_DefaultCaption;
            }

            if (string.IsNullOrEmpty(message))
            {
                message = CredUI.PromptForCredential_DefaultMessage;
            }

            if (caption.Length > CREDUI_MAX_CAPTION_LENGTH)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, CredUI.PromptForCredential_InvalidCaption, CREDUI_MAX_CAPTION_LENGTH));
            }

            if (message.Length > CREDUI_MAX_MESSAGE_LENGTH)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, CredUI.PromptForCredential_InvalidMessage, CREDUI_MAX_MESSAGE_LENGTH));
            }

            if (userName != null && userName.Length > CRED_MAX_USERNAME_LENGTH)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, CredUI.PromptForCredential_InvalidUserName, CRED_MAX_USERNAME_LENGTH));
            }

            CREDUI_INFO credUiInfo = new CREDUI_INFO();
            credUiInfo.pszCaptionText = caption;
            credUiInfo.pszMessageText = message;

            StringBuilder usernameBuilder = new StringBuilder(userName, CRED_MAX_USERNAME_LENGTH);
            StringBuilder passwordBuilder = new StringBuilder(CRED_MAX_PASSWORD_LENGTH);

            bool save = false;
            int saveCredentials = Convert.ToInt32(save);
            credUiInfo.cbSize = Marshal.SizeOf(credUiInfo);
            credUiInfo.hwndParent = parentHWND;


            CREDUI_FLAGS flags = CREDUI_FLAGS.DO_NOT_PERSIST;

            // Set some of the flags if they have not requested a domain credential
            if ((allowedCredentialTypes & PSCredentialTypes.Domain) != PSCredentialTypes.Domain)
            {
                flags |= CREDUI_FLAGS.GENERIC_CREDENTIALS;

                // If they've asked to always prompt, do so.
                if ((options & PSCredentialUIOptions.AlwaysPrompt) == PSCredentialUIOptions.AlwaysPrompt)
                    flags |= CREDUI_FLAGS.ALWAYS_SHOW_UI;
            }

            // To prevent buffer overrun attack, only attempt call if buffer lengths are within bounds.
            CredUIReturnCodes result = CredUIReturnCodes.ERROR_INVALID_PARAMETER;
            if (usernameBuilder.Length <= CRED_MAX_USERNAME_LENGTH && passwordBuilder.Length <= CRED_MAX_PASSWORD_LENGTH)
            {
                result = CredUIPromptForCredentials(
                    ref credUiInfo,
                    targetName,
                    IntPtr.Zero,
                    0,
                    usernameBuilder,
                    CRED_MAX_USERNAME_LENGTH,
                    passwordBuilder,
                    CRED_MAX_PASSWORD_LENGTH,
                    ref saveCredentials,
                    flags);
            }

            if (result == CredUIReturnCodes.NO_ERROR)
            {
                // Extract the username
                string credentialUsername = null;
                if (usernameBuilder != null)
                    credentialUsername = usernameBuilder.ToString();

                // Trim the leading '\' from the username, which CredUI automatically adds
                // if you don't specify a domain.
                // This is a really common bug in V1 and V2, causing everybody to have to do
                // it themselves.
                // This could be a breaking change for hosts that do hard-coded hacking:
                // $cred.UserName.SubString(1, $cred.Username.Length - 1)
                // But that's OK, because they would have an even worse bug when you've
                // set the host (ConsolePrompting = true) configuration (which does not do this).
                credentialUsername = credentialUsername.TrimStart('\\');

                // Extract the password into a SecureString, zeroing out the memory
                // as soon as possible.
                SecureString password = new SecureString();
                for (int counter = 0; counter < passwordBuilder.Length; counter++)
                {
                    password.AppendChar(passwordBuilder[counter]);
                    passwordBuilder[counter] = (char)0;
                }

                if (!String.IsNullOrEmpty(credentialUsername))
                    cred = new PSCredential(credentialUsername, password);
                else
                    cred = null;
            }
            else // result is not CredUIReturnCodes.NO_ERROR
            {
                cred = null;
            }

            return cred;
        }

        [DllImport("credui", EntryPoint = "CredUIPromptForCredentialsW", CharSet = CharSet.Unicode)]
        private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO pUiInfo,
                  string pszTargetName, IntPtr Reserved, int dwAuthError, StringBuilder pszUserName,
                  int ulUserNameMaxChars, StringBuilder pszPassword, int ulPasswordMaxChars, ref int pfSave, CREDUI_FLAGS dwFlags);

        [Flags]
        private enum CREDUI_FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        private enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
        }

        /// <summary>
        /// Returns the prompt used in remote sessions: "[machine]: basePrompt"
        /// </summary>
        internal static string GetRemotePrompt(RemoteRunspace runspace, string basePrompt, bool configuredSession = false)
        {
            if (configuredSession ||
                runspace.ConnectionInfo is NamedPipeConnectionInfo ||
                runspace.ConnectionInfo is VMConnectionInfo ||
                runspace.ConnectionInfo is ContainerConnectionInfo)
            {
                return basePrompt;
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "[{0}]: {1}", runspace.ConnectionInfo.ComputerName, basePrompt);
            }
        }

        internal static bool IsProcessInteractive(InvocationInfo invocationInfo)
        {
#if CORECLR
            return false;
#else
            // CommandOrigin != Runspace means it is in a script
            if (invocationInfo.CommandOrigin != CommandOrigin.Runspace)
                return false;

            // If we don't own the window handle, we've been invoked
            // from another process that just calls "PowerShell -Command"
            if (System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle == IntPtr.Zero)
                return false;

            // If the window has been idle for less than two seconds,
            // they're probably still calling "PowerShell -Command"
            // but from Start-Process, or the StartProcess API
            try
            {
                System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                TimeSpan timeSinceStart = DateTime.Now - currentProcess.StartTime;
                TimeSpan idleTime = timeSinceStart - currentProcess.TotalProcessorTime;

                // Making it 2 seconds because of things like delayed prompt
                if (idleTime.TotalSeconds > 2)
                    return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Don't have access to the properties
                return false;
            }

            return false;
#endif
        }

        /// <summary>
        /// Create a configured remote runspace from provided name.
        /// </summary>
        /// <param name="configurationName"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        internal static RemoteRunspace CreateConfiguredRunspace(
            string configurationName,
            PSHost host)
        {
            // Create a loop-back remote runspace with network access enabled, and 
            // with the provided endpoint configurationname.
            TypeTable typeTable = TypeTable.LoadDefaultTypeFiles();
            var connectInfo = new WSManConnectionInfo();
            connectInfo.ShellUri = configurationName.Trim();
            connectInfo.EnableNetworkAccess = true;

            RemoteRunspace remoteRunspace = null;
            try
            {
                remoteRunspace = (RemoteRunspace)RunspaceFactory.CreateRunspace(connectInfo, host, typeTable);
                remoteRunspace.Open();
            }
            catch (Exception e)
            {
                CommandProcessorBase.CheckForSevereException(e);

                throw new PSInvalidOperationException(
                    StringUtil.Format(RemotingErrorIdStrings.CannotCreateConfiguredRunspace, configurationName),
                    e);
            }

            return remoteRunspace;
        }

        #endregion

        #region Public Access

        #region PSEdit Support

        /// <summary>
        /// PSEditFunction script string.
        /// </summary>
        public const string PSEditFunction = @"
            param (
                [Parameter(Mandatory=$true)] [String[]] $FileName
            )

            foreach ($file in $FileName)
            {
                dir $file -File | foreach {
                    $filePathName = $_.FullName

# Get file contents
                    $contentBytes = Get-Content -Path $filePathName -Raw -Encoding Byte

# Notify client for file open.
                    New-Event -SourceIdentifier PSISERemoteSessionOpenFile -EventArguments @($filePathName, $contentBytes) > $null
                }
            }
        ";

        /// <summary>
        /// CreatePSEditFunction script string.
        /// </summary>
        public const string CreatePSEditFunction = @"
            param (
                [string] $PSEditFunction
            )

            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))
            {
                throw (new-object System.NotSupportedException)
            }

            Register-EngineEvent -SourceIdentifier PSISERemoteSessionOpenFile -Forward

            if ((Test-Path -Path 'function:\global:PSEdit') -eq $false)
            {
                Set-Item -Path 'function:\global:PSEdit' -Value $PSEditFunction
            }
        ";

        /// <summary>
        /// RemovePSEditFunction script string.
        /// </summary>
        public const string RemovePSEditFunction = @"
            if ($PSVersionTable.PSVersion -lt ([version] '3.0'))
            {
                throw (new-object System.NotSupportedException)
            }

            if ((Test-Path -Path 'function:\global:PSEdit') -eq $true)
            {
                Remove-Item -Path 'function:\global:PSEdit' -Force
            }

            Get-EventSubscriber -SourceIdentifier PSISERemoteSessionOpenFile -EA Ignore | Remove-Event
        ";

        /// <summary>
        /// Open file event.
        /// </summary>
        public const string RemoteSessionOpenFileEvent = "PSISERemoteSessionOpenFile";

        #endregion

        #endregion
    }

    #endregion
}
