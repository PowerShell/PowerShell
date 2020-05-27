// Copyright (c) Microsoft Corporation.
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
        /// <param name="caption">Caption for the message.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="userName">Name of the user whose credentials are to be prompted for. If set to null or empty string, the function will prompt for user name first.</param>
        /// <param name="targetName">Name of the target for which credentials are being collected.</param>
        /// <returns>PSCredential object.</returns>
        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return PromptForCredential(caption,
                                         message,
                                         userName,
                                         reenterPassword: false,
                                         targetName,
                                         PSCredentialTypes.Default,
                                         PSCredentialUIOptions.Default);
        }

        /// <summary>
        /// Prompt for credentials.
        /// </summary>
        /// <param name="caption">Caption for the message.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="userName">Name of the user whose credentials are to be prompted for. If set to null or empty string, the function will prompt for user name first.</param>
        /// <param name="targetName">Name of the target for which credentials are being collected.</param>
        /// <param name="allowedCredentialTypes">What type of credentials can be supplied by the user.</param>
        /// <param name="options">Options that control the credential gathering UI behavior.</param>
        /// <returns>PSCredential object, or null if input was cancelled (or if reading from stdin and stdin at EOF).</returns>
        public override PSCredential PromptForCredential(
            string caption, 
            string message, 
            string userName, 
            string targetName, 
            PSCredentialTypes allowedCredentialTypes, 
            PSCredentialUIOptions options)
        {
            return PromptForCredential(
                caption,
                message,
                userName,
                reenterPassword: false,
                targetName,
                allowedCredentialTypes,
                options);
        }

        /// <summary>
        /// Prompt for credentials.
        /// </summary>
        /// <param name="caption">Caption for the message.</param>
        /// <param name="message">Message to be displayed.</param>
        /// <param name="userName">Name of the user whose credentials are to be prompted for. If set to null or empty string, the function will prompt for user name first.</param>
        /// <param name="reenterPassword">Prompts user to re-enter the password for confirmation.</param>
        /// <param name="targetName">Name of the target for which credentials are being collected.</param>
        /// <param name="allowedCredentialTypes">What type of credentials can be supplied by the user.</param>
        /// <param name="options">Options that control the credential gathering UI behavior.</param>
        /// <returns>PSCredential object, or null if input was cancelled (or if reading from stdin and stdin at EOF).</returns>
        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            bool reenterPassword,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            PSCredential cred = null;
            SecureString password = null;
            SecureString confirmPassword = null;
            string userPrompt = null;
            string passwordPrompt = null;
            string reenterPasswordPrompt = null;
            string passwordMismatch = null;

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

                // need to prompt for user name first
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

            // now, prompt for the password
            do
            {
                WriteToConsole(passwordPrompt, true);
                password = ReadLineAsSecureString();
                if (password == null)
                {
                    return null;
                }
            }
            while (password.Length == 0);

            if (reenterPassword)
            {
                reenterPasswordPrompt = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_ReenterPassword, userName);
                passwordMismatch = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_PasswordMismatch);
                do
                {
                    // now, prompt to re-enter the password.
                    WriteToConsole(reenterPasswordPrompt, true);
                    confirmPassword = ReadLineAsSecureString();
                    if (confirmPassword == null)
                    {
                        return null;
                    }

                    // If the string is not equal print message and loop.
                    if(SecureStringEquals(password, confirmPassword))
                    {
                        break;
                    }
                    WriteToConsole(ConsoleColor.Red, ConsoleColor.Black, passwordMismatch, true);
                }
                while (true);
            }

            WriteLineToConsole();
            cred = new PSCredential(userName, password);
            return cred;
        }

        private static bool SecureStringEquals(SecureString password, SecureString confirmPassword)
        {
            IntPtr pwd_ptr = IntPtr.Zero;
            IntPtr confirmPwd_ptr = IntPtr.Zero;
            try
            {
                pwd_ptr = Marshal.SecureStringToBSTR(password);
                string pwd = Marshal.PtrToStringBSTR(pwd_ptr);

                confirmPwd_ptr = Marshal.SecureStringToBSTR(confirmPassword);
                string confirmPwd = Marshal.PtrToStringBSTR(confirmPwd_ptr);

                return pwd.Equals(confirmPwd);
            }
            finally
            {
                if (pwd_ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(pwd_ptr);
                }

                if (confirmPwd_ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(confirmPwd_ptr);
                }
            }
        }
    }
}

