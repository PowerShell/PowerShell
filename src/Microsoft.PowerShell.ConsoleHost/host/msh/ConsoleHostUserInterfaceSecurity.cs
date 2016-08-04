/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Security;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Microsoft.Win32;
using System.Globalization;

namespace Microsoft.PowerShell
{
    /// <summary> 
    /// 
    /// ConsoleHostUserInterface implements console-mode user interface for powershell.exe
    /// 
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
        /// 
        /// </summary>
        /// <param name="userName"> name of the user whose creds are to be prompted for. If set to null or empty string, the function will prompt for user name first. </param>
        /// 
        /// <param name="targetName"> name of the target for which creds are being collected </param>
        /// 
        /// <param name="message"> message to be displayed. </param>
        /// 
        /// <param name="caption"> caption for the message. </param>
        /// 
        /// <returns> PSCredential object</returns>
        ///

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
        /// <param name="userName"> name of the user whose creds are to be prompted for. If set to null or empty string, the function will prompt for user name first. </param>
        /// 
        /// <param name="targetName"> name of the target for which creds are being collected </param>
        /// 
        /// <param name="message"> message to be displayed. </param>
        /// 
        /// <param name="caption"> caption for the message. </param>
        /// 
        /// <param name="allowedCredentialTypes"> what type of creds can be supplied by the user </param>
        /// 
        /// <param name="options"> options that control the cred gathering UI behavior </param>
        /// 
        /// <returns> PSCredential object, or null if input was cancelled (or if reading from stdin and stdin at EOF)</returns>
        ///

        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            if (!PromptUsingConsole())
            {
                IntPtr mainWindowHandle = GetMainWindowHandle();
                return HostUtilities.CredUIPromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options, mainWindowHandle);
            }
            else
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

        private IntPtr GetMainWindowHandle()
        {
#if CORECLR // No System.Diagnostics.Process.MainWindowHandle on CoreCLR;
            // Returned WindowHandle is used only in 1 case - prompting for credential using GUI dialog, which is not used on Nano,
            // because on Nano we prompt for credential using console (different code path in 'PromptForCredential' function)
            return IntPtr.Zero;
#else
            System.Diagnostics.Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr mainWindowHandle = currentProcess.MainWindowHandle;

            while ((mainWindowHandle == IntPtr.Zero) && (currentProcess != null))
            {
                currentProcess = PsUtils.GetParentProcess(currentProcess);
                if (currentProcess != null)
                {
                    mainWindowHandle = currentProcess.MainWindowHandle;
                }
            }

            return mainWindowHandle;
#endif
        }

        // Determines whether we should prompt using the Console prompting
        // APIs
        private bool PromptUsingConsole()
        {
#if CORECLR
            // on Nano there is no other way to prompt except by using console
            return true;
#else
            bool promptUsingConsole = false;
            // Get the configuration setting
            try
            {
                promptUsingConsole = ConfigPropertyAccessor.Instance.GetConsolePrompting();
            }
            catch (System.Security.SecurityException e)
            {
                s_tracer.TraceError("Could not read CredUI registry key: " + e.Message);
                return promptUsingConsole;
            }
            catch (InvalidCastException e)
            {
                s_tracer.TraceError("Could not parse CredUI registry key: " + e.Message);
                return promptUsingConsole;
            }
            catch (FormatException e)
            {
                s_tracer.TraceError("Could not parse CredUI registry key: " + e.Message);
                return promptUsingConsole;
            }

            s_tracer.WriteLine("DetermineCredUIPolicy: policy == {0}", promptUsingConsole);
            return promptUsingConsole;
#endif
        }
    }
}

