// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Microsoft.Win32;
using System.Globalization;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// ConsoleHostUserInterface implements console-mode user interface for powershell.
    /// </summary>
    internal partial
    class ConsoleHostUserInterface : System.Management.Automation.Host.PSHostUserInterface
    {
        /// <summary>
        /// Prompt for credentials.
        ///
        /// In future, when we have Credential object from the security team,
        /// this function will be modified to prompt using secure-path
        /// if so configured.
        /// </summary>
        /// <param name="userName">Name of the user whose creds are to be prompted for. If set to null or empty string, the function will prompt for user name first.</param>
        /// <param name="targetName">Name of the target for which creds are being collected.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="caption">Caption for the message.</param>
        /// <returns>PSCredential object.</returns>

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName)
        {
            return PromptForCredential(caption,
                                         message,
                                         userName,
                                         targetName,
                                         PSCredentialTypes.Default,
                                         PSCredentialUIOptions.Default);
        }

        /// <summary>
        /// Prompt for credentials.
        /// </summary>
        /// <param name="userName">Name of the user whose creds are to be prompted for. If set to null or empty string, the function will prompt for user name first.</param>
        /// <param name="targetName">Name of the target for which creds are being collected.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="caption">Caption for the message.</param>
        /// <param name="allowedCredentialTypes">What type of creds can be supplied by the user.</param>
        /// <param name="options">Options that control the cred gathering UI behavior.</param>
        /// <returns>PSCredential object, or null if input was cancelled (or if reading from stdin and stdin at EOF).</returns>

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            PSCredential cred = null;
            SecureString password = null;
            string userPrompt = null;
            string passwordPrompt = null;

            if (!string.IsNullOrEmpty(caption))
            {
                // Should be a skin lookup

                WriteLineToConsole();
                WriteToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(caption));
                WriteLineToConsole();
            }

            if (!string.IsNullOrEmpty(message))
            {
                WriteLineToConsole(WrapToCurrentWindowWidth(message));
            }

            if (string.IsNullOrEmpty(userName))
            {
                userPrompt = ConsoleHostUserInterfaceSecurityResources.PromptForCredential_User;

                //
                // need to prompt for user name first
                //
                do
                {
                    WriteToConsole(userPrompt, true);
                    userName = ReadLine();
                    if (userName == null)
                    {
                        return null;
                    }
                }
                while (userName.Length == 0);
            }

            passwordPrompt = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_Password, userName
            );

            //
            // now, prompt for the password
            //
            WriteToConsole(passwordPrompt, true);
            password = ReadLineAsSecureString();
            if (password == null)
            {
                return null;
            }

            WriteLineToConsole();

            cred = new PSCredential(userName, password);

            return cred;
        }
    }
}

