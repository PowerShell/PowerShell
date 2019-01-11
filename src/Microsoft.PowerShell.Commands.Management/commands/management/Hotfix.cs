// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics; // Process class
using System.ComponentModel; // Win32Exception
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Security.AccessControl;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    #region Get-HotFix

    /// <summary>
    /// Cmdlet for Get-Hotfix Proxy.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "HotFix", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135217", RemotingCapability = RemotingCapability.SupportedByCommand)]
    [OutputType(@"System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering")]
    public sealed class GetHotFixCommand : PSCmdlet, IDisposable
    {
        #region Parameters

        /// <summary>
        /// Specifies the HotFixID. Unique identifier associated with a particular update.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        [Alias("HFID")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Id { get; set; }

        /// <summary>
        /// To search on description of Hotfixes.
        /// </summary>
        [Parameter(ParameterSetName = "Description")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Description { get; set; }

        /// <summary>
        /// Parameter to pass the Computer Name.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("CN", "__Server", "IPAddress")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ComputerName { get; set; } = new string[] { "localhost" };

        /// <summary>
        /// Parameter to pass the Credentials.
        /// </summary>
        [Parameter]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential { get; set; }

        #endregion Parameters

        #region Overrides

        private ManagementObjectSearcher _searchProcess;

        private bool _inputContainsWildcard = false;
        /// <summary>
        /// Get the List of HotFixes installed on the Local Machine.
        /// </summary>
        protected override void BeginProcessing()
        {
            foreach (string computer in ComputerName)
            {
                bool foundRecord = false;
                StringBuilder QueryString = new StringBuilder();
                ConnectionOptions conOptions = ComputerWMIHelper.GetConnectionOptions(AuthenticationLevel.Packet, ImpersonationLevel.Impersonate, this.Credential);
                ManagementScope scope = new ManagementScope(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), conOptions);
                scope.Connect();
                if (Id != null)
                {
                    QueryString.Append("Select * from Win32_QuickFixEngineering where (");
                    for (int i = 0; i <= Id.Length - 1; i++)
                    {
                        QueryString.Append("HotFixID= '");
                        QueryString.Append(Id[i].ToString().Replace("'", "\\'"));
                        QueryString.Append("'");
                        if (i < Id.Length - 1)
                            QueryString.Append(" Or ");
                    }

                    QueryString.Append(")");
                }
                else
                {
                    QueryString.Append("Select * from Win32_QuickFixEngineering");
                    foundRecord = true;
                }

                _searchProcess = new ManagementObjectSearcher(scope, new ObjectQuery(QueryString.ToString()));
                foreach (ManagementObject obj in _searchProcess.Get())
                {
                    if (Description != null)
                    {
                        if (!FilterMatch(obj))
                            continue;
                    }
                    else
                    {
                        _inputContainsWildcard = true;
                    }

                    // try to translate the SID to a more friendly username
                    // just stick with the SID if anything goes wrong
                    string installed = (string)obj["InstalledBy"];
                    if (!string.IsNullOrEmpty(installed))
                    {
                        try
                        {
                            SecurityIdentifier secObj = new SecurityIdentifier(installed);
                            obj["InstalledBy"] = secObj.Translate(typeof(NTAccount)); ;
                        }
                        catch (IdentityNotMappedException) // thrown by SecurityIdentifier.Translate
                        {
                        }
                        catch (SystemException) // thrown by SecurityIdentifier.constr
                        {
                        }
                        // catch (ArgumentException) // thrown (indirectly) by SecurityIdentifier.constr (on XP only?)
                        // { catch not needed - this is already caught as SystemException
                        // }
                        // catch (PlatformNotSupportedException) // thrown (indirectly) by SecurityIdentifier.Translate (on Win95 only?)
                        // { catch not needed - this is already caught as SystemException
                        // }
                        // catch (UnauthorizedAccessException) // thrown (indirectly) by SecurityIdentifier.Translate
                        // { catch not needed - this is already caught as SystemException
                        // }
                    }

                    WriteObject(obj);
                    foundRecord = true;
                }

                if (!foundRecord && !_inputContainsWildcard)
                {
                    Exception Ex = new ArgumentException(StringUtil.Format(HotFixResources.NoEntriesFound, computer));
                    WriteError(new ErrorRecord(Ex, "GetHotFixNoEntriesFound", ErrorCategory.ObjectNotFound, null));
                }

                if (_searchProcess != null)
                {
                    this.Dispose();
                }
            }
        }

        /// <summary>
        /// To implement ^C.
        /// </summary>
        protected override void StopProcessing()
        {
            if (_searchProcess != null)
            {
                _searchProcess.Dispose();
            }
        }
        #endregion Overrides

        #region "Private Methods"

        private bool FilterMatch(ManagementObject obj)
        {
            try
            {
                foreach (string desc in Description)
                {
                    WildcardPattern wildcardpattern = WildcardPattern.Get(desc, WildcardOptions.IgnoreCase);
                    if (wildcardpattern.IsMatch((string)obj["Description"]))
                    {
                        return true;
                    }

                    if (WildcardPattern.ContainsWildcardCharacters(desc))
                    {
                        _inputContainsWildcard = true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        #endregion "Private Methods"

        #region "IDisposable Members"

        /// <summary>
        /// Dispose Method.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            // Use SuppressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose Method.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_searchProcess != null)
                {
                    _searchProcess.Dispose();
                }
            }
        }

        #endregion "IDisposable Members"
    }
    #endregion
}
