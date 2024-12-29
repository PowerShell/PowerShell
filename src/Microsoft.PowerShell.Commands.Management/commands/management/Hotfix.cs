// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security.Principal;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    #region Get-HotFix

    /// <summary>
    /// Cmdlet for Get-Hotfix Proxy.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "HotFix", DefaultParameterSetName = "Default",
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2109716", RemotingCapability = RemotingCapability.SupportedByCommand)]
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
        private readonly ConnectionOptions _connectionOptions = new();

        /// <summary>
        /// Sets connection options.
        /// </summary>
        protected override void BeginProcessing()
        {
            _connectionOptions.Authentication = AuthenticationLevel.Packet;
            _connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
            _connectionOptions.Username = Credential?.UserName;
            _connectionOptions.SecurePassword = Credential?.Password;
        }

        /// <summary>
        /// Get the List of HotFixes installed on the Local Machine.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string computer in ComputerName)
            {
                bool foundRecord = false;
                StringBuilder queryString = new();
                ManagementScope scope = new(ComputerWMIHelper.GetScopeString(computer, ComputerWMIHelper.WMI_Path_CIM), _connectionOptions);
                scope.Connect();
                if (Id != null)
                {
                    queryString.Append("Select * from Win32_QuickFixEngineering where (");
                    for (int i = 0; i <= Id.Length - 1; i++)
                    {
                        queryString.Append("HotFixID= '");
                        queryString.Append(Id[i].Replace("'", "\\'"));
                        queryString.Append('\'');
                        if (i < Id.Length - 1)
                        {
                            queryString.Append(" Or ");
                        }
                    }

                    queryString.Append(')');
                }
                else
                {
                    queryString.Append("Select * from Win32_QuickFixEngineering");
                    foundRecord = true;
                }

                _searchProcess = new ManagementObjectSearcher(scope, new ObjectQuery(queryString.ToString()));
                foreach (ManagementObject obj in _searchProcess.Get())
                {
                    if (Description != null)
                    {
                        if (!FilterMatch(obj))
                        {
                            continue;
                        }
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
                            SecurityIdentifier secObj = new(installed);
                            obj["InstalledBy"] = secObj.Translate(typeof(NTAccount));
                        }
                        catch (IdentityNotMappedException)
                        {
                            // thrown by SecurityIdentifier.Translate
                        }
                        catch (SystemException)
                        {
                            // thrown by SecurityIdentifier.constr
                        }
                    }

                    WriteObject(obj);
                    foundRecord = true;
                }

                if (!foundRecord && !_inputContainsWildcard)
                {
                    Exception ex = new ArgumentException(StringUtil.Format(HotFixResources.NoEntriesFound, computer));
                    WriteError(new ErrorRecord(ex, "GetHotFixNoEntriesFound", ErrorCategory.ObjectNotFound, null));
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
            _searchProcess?.Dispose();
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
                _searchProcess?.Dispose();
            }
        }

        #endregion "IDisposable Members"
    }
    #endregion
}

#endif
