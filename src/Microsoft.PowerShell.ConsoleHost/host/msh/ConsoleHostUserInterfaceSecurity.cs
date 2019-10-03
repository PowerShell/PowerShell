// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Win32;

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
        /// <param name="confirmPassword">Prompt to confirm the password.</param>
        /// <returns>PSCredential object.</returns>

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            bool confirmPassword)
        {
            return PromptForCredential(caption,
                                         message,
                                         userName,
                                         targetName,
                                         confirmPassword,
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
        /// <param name="confirmPassword">Prompt to confirm the password.</param>
        /// <returns>PSCredential object, or null if input was cancelled (or if reading from stdin and stdin at EOF).</returns>

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            bool confirmPassword,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            PSCredential cred = null;
            SecureString password = null;
            SecureString retypedPassword = null;
            string userPrompt = null;
            string passwordPrompt = null;
            string retypePasswordPrompt = null;

            if (!string.IsNullOrEmpty(caption))
            {
                // Should be a skin lookup

                WriteLineToConsole();
                WriteLineToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(caption));
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

            passwordPrompt = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_Password, userName);

            //
            // now, prompt for the password
            //
            if (confirmPassword)
            {
                retypePasswordPrompt = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_ConfirmPassword, userName);
                bool passwordsMatch;
                do
                {
                    WriteToConsole(passwordPrompt, true);
                    password = ReadLineAsSecureString();
                    WriteToConsole(retypePasswordPrompt, true);
                    retypedPassword = ReadLineAsSecureString();

                    passwordsMatch = ComparePasswords(password, retypedPassword);
                } while (!passwordsMatch);
            }
            else
            {
                WriteToConsole(passwordPrompt, true);
                password = ReadLineAsSecureString();
                if (password == null)
                {
                    return null;
                }
            }

            WriteLineToConsole();

            cred = new PSCredential(userName, password);

            return cred;
        }

        private bool ComparePasswords(SecureString password, SecureString retypedPassword)
        {
            IntPtr passwordBstr = IntPtr.Zero;
            IntPtr retypedPasswordBstr = IntPtr.Zero;
            int match = 0;
            try
            {
                passwordBstr = Marshal.SecureStringToBSTR(password);
                retypedPasswordBstr = Marshal.SecureStringToBSTR(retypedPassword);
                int passwordLength = Marshal.ReadInt32(passwordBstr, -4);
                int confirmPasswordLength = Marshal.ReadInt32(retypedPasswordBstr, -4);
                if (passwordLength == confirmPasswordLength)
                {
                    for (int x = 0; x < passwordLength; ++x)
                    {
                        byte b1 = Marshal.ReadByte(passwordBstr, x);
                        byte b2 = Marshal.ReadByte(retypedPasswordBstr, x);
                        if (b1 != b2)
                        {
                            // byte mismatch
                            match++;
                        }
                    }
                }
                else
                {
                    // length mismatch
                    match++;
                }                
            }
            finally
            {
                if (retypedPasswordBstr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(retypedPasswordBstr);
                }
                    
                if (passwordBstr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(passwordBstr);
                }                 
            }

            if (match == 0)
            {
                // passwords match
                return true;
            }
            else
            {
                // passwords don't match
                WriteToConsole(StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PasswordMismatch), true);
                WriteLineToConsole();
                return false;
            }

        }
    }
}

