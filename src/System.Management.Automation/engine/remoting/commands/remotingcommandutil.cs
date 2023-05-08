// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;

using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This enum is used to distinguish two sets of parameters on some of the remoting cmdlets.
    /// </summary>
    internal enum RunspaceParameterSet
    {
        /// <summary>
        /// Use ComputerName parameter set.
        /// </summary>
        ComputerName,
        /// <summary>
        /// Use Runspace Parameter set.
        /// </summary>
        Runspace
    }

    /// <summary>
    /// This is a static utility class that performs some of the common chore work for the
    /// the remoting cmdlets.
    /// </summary>
    internal static class RemotingCommandUtil
    {
        internal static bool HasRepeatingRunspaces(PSSession[] runspaceInfos)
        {
            if (runspaceInfos == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runspaceInfos));
            }

            if (runspaceInfos.GetLength(0) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(runspaceInfos));
            }

            for (int i = 0; i < runspaceInfos.GetLength(0); i++)
            {
                for (int k = 0; k < runspaceInfos.GetLength(0); k++)
                {
                    if (i != k)
                    {
                        if (runspaceInfos[i].Runspace.InstanceId == runspaceInfos[k].Runspace.InstanceId)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool ExceedMaximumAllowableRunspaces(PSSession[] runspaceInfos)
        {
            if (runspaceInfos == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(runspaceInfos));
            }

            if (runspaceInfos.GetLength(0) == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(runspaceInfos));
            }

            return false;
        }

        /// <summary>
        /// Checks the prerequisites for a cmdlet and terminates if the cmdlet
        /// is not valid.
        /// </summary>
        internal static void CheckRemotingCmdletPrerequisites()
        {
#if UNIX
            // TODO: check that PSRP requirements are installed
            return;
#else
            bool notSupported = true;
            const string WSManKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\WSMAN\\";

            CheckHostRemotingPrerequisites();

            try
            {
                // the following registry key defines WSMan compatibility
                // HKLM\Software\Microsoft\Windows\CurrentVersion\WSMAN\ServiceStackVersion
                string wsManStackValue = null;
                RegistryKey wsManKey = Registry.LocalMachine.OpenSubKey(WSManKeyPath);
                if (wsManKey != null)
                {
                    wsManStackValue = (string)wsManKey.GetValue("ServiceStackVersion");
                }

                Version wsManStackVersion = !string.IsNullOrEmpty(wsManStackValue) ?
                    new Version(wsManStackValue.Trim()) :
                    System.Management.Automation.Remoting.Client.WSManNativeApi.WSMAN_STACK_VERSION;

                // WSMan stack version must be 2.0 or later.
                if (wsManStackVersion >= new Version(2, 0))
                {
                    notSupported = false;
                }
            }
            catch (FormatException)
            {
                notSupported = true;
            }
            catch (OverflowException)
            {
                notSupported = true;
            }
            catch (ArgumentException)
            {
                notSupported = true;
            }
            catch (System.Security.SecurityException)
            {
                notSupported = true;
            }
            catch (ObjectDisposedException)
            {
                notSupported = true;
            }

            if (notSupported)
            {
                // WSMan is not supported on this platform
                throw new InvalidOperationException(
                     "Windows PowerShell remoting features are not enabled or not supported on this machine.\nThis may be because you do not have the correct version of WS-Management installed or this version of Windows does not support remoting currently.\n For more information, type 'get-help about_remote_requirements'.");
            }
#endif
        }

        /// <summary>
        /// Facilitates to check if remoting is supported on the host machine.
        /// PowerShell remoting is supported on all Windows SQU's except WinPE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When PowerShell is hosted on a WinPE machine, the execution
        /// of this API would result in an InvalidOperationException being
        /// thrown, indicating that remoting is not supported on a WinPE machine.
        /// </exception>
        internal static void CheckHostRemotingPrerequisites()
        {
            // A registry key indicates if the SKU is WINPE. If this turns out to be true,
            // then an InValidOperation exception is thrown.
            bool isWinPEHost = Utils.IsWinPEHost();
            if (isWinPEHost)
            {
                // WSMan is not supported on this platform
                // throw new InvalidOperationException(
                //     "WinPE does not support Windows PowerShell remoting");
                ErrorRecord errorRecord = new ErrorRecord(new InvalidOperationException(StringUtil.Format(RemotingErrorIdStrings.WinPERemotingNotSupported)), null, ErrorCategory.InvalidOperation, null);
                throw new InvalidOperationException(errorRecord.ToString());
            }
        }
    }
}
