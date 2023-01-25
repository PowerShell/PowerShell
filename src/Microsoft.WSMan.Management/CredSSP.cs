// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Xml;

using Microsoft.Win32;

using Dbg = System.Management.Automation;

namespace Microsoft.WSMan.Management
{
    #region WSManCredSSP cmdlet base

    /// <summary>
    /// Base class used *-WSManCredSSP cmdlets (Enable-WSManCredSSP, Disable-WSManCredSSP)
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSP")]
    public class WSManCredSSPCommandBase : PSCmdlet
    {
        #region Protected / Internal Data

        internal const string Server = "Server";
        internal const string Client = "Client";

        #endregion

        #region Parameters

        /// <summary>
        /// Role can either "Client" or "Server".
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateSet(Client, Server)]
        public string Role
        {
            get { return role; }

            set { role = value; }
        }

        private string role;
        #endregion

        #region Utilities

        /// <summary>
        /// </summary>
        /// <returns>
        /// Returns a session object upon successful creation..otherwise
        /// writes an error using WriteError and returns null.
        /// </returns>
        internal IWSManSession CreateWSManSession()
        {
            IWSManEx wsmanObject = (IWSManEx)new WSManClass();
            IWSManSession m_SessionObj = null;

            try
            {
                m_SessionObj = (IWSManSession)wsmanObject.CreateSession(null, 0, null);
                return m_SessionObj;
            }
            catch (COMException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "COMException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }

            return null;
        }

        #endregion
    }

    #endregion

    #region DisableWsManCredSsp

    /// <summary>
    /// Disables CredSSP authentication on the client. CredSSP authentication
    /// enables an application to delegate the user's credentials from the client to
    /// the server, hence allowing the user to perform management operations that
    /// access a second hop.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "WSManCredSSP", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096628")]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSP")]
    public class DisableWSManCredSSPCommand : WSManCredSSPCommandBase, IDisposable
    {
        #region private

        // The application name MUST be "wsman" as wsman got approval from security
        // folks who suggested to register the SPN with name "wsman".
        private const string applicationname = "wsman";

        private void DisableClientSideSettings()
        {
            WSManHelper helper = new WSManHelper(this);
            IWSManSession m_SessionObj = CreateWSManSession();
            if (m_SessionObj == null)
            {
                return;
            }

            try
            {
                string result = m_SessionObj.Get(helper.CredSSP_RUri, 0);
                XmlDocument resultopxml = new XmlDocument();
                resultopxml.LoadXml(result);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(resultopxml.NameTable);
                nsmgr.AddNamespace("cfg", helper.CredSSP_XMLNmsp);
                XmlNode xNode = resultopxml.SelectSingleNode(helper.CredSSP_SNode, nsmgr);
                if (xNode is null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex, helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"), ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                string inputXml = @"<cfg:Auth xmlns:cfg=""http://schemas.microsoft.com/wbem/wsman/1/config/client/auth""><cfg:CredSSP>false</cfg:CredSSP></cfg:Auth>";

                m_SessionObj.Put(helper.CredSSP_RUri, inputXml, 0);

                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                {
                    this.DeleteUserDelegateSettings();
                }
                else
                {
                    ThreadStart start = new ThreadStart(this.DeleteUserDelegateSettings);
                    Thread thread = new Thread(start);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }

                if (!helper.ValidateCreadSSPRegistryRetry(false, null, applicationname))
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("DisableCredSSPPolicyValidateError"), false, null);
                }
            }
            catch (System.Xml.XPath.XPathException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "XpathException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_SessionObj.Error))
                {
                    helper.AssertError(m_SessionObj.Error, true, null);
                }

                if (m_SessionObj != null)
                    Dispose(m_SessionObj);
            }
        }

        private void DisableServerSideSettings()
        {
            WSManHelper helper = new WSManHelper(this);
            IWSManSession m_SessionObj = CreateWSManSession();
            if (m_SessionObj == null)
            {
                return;
            }

            try
            {
                string result = m_SessionObj.Get(helper.Service_CredSSP_Uri, 0);
                XmlDocument resultopxml = new XmlDocument();
                resultopxml.LoadXml(result);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(resultopxml.NameTable);
                nsmgr.AddNamespace("cfg", helper.Service_CredSSP_XMLNmsp);
                XmlNode xNode = resultopxml.SelectSingleNode(helper.CredSSP_SNode, nsmgr);
                if (xNode is null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex,
                        helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"),
                        ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                string inputXml = string.Format(
                    CultureInfo.InvariantCulture,
                    @"<cfg:Auth xmlns:cfg=""{0}""><cfg:CredSSP>false</cfg:CredSSP></cfg:Auth>",
                    helper.Service_CredSSP_XMLNmsp);

                m_SessionObj.Put(helper.Service_CredSSP_Uri, inputXml, 0);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_SessionObj.Error))
                {
                    helper.AssertError(m_SessionObj.Error, true, null);
                }

                if (m_SessionObj != null)
                {
                    Dispose(m_SessionObj);
                }
            }
        }

        private void DeleteUserDelegateSettings()
        {
            System.IntPtr KeyHandle = System.IntPtr.Zero;
            IGroupPolicyObject GPO = (IGroupPolicyObject)new GPClass();
            GPO.OpenLocalMachineGPO(1);
            KeyHandle = GPO.GetRegistryKey(2);
            RegistryKey rootKey = Registry.CurrentUser;
            const string GPOpath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy Objects";
            RegistryKey GPOKey = rootKey.OpenSubKey(GPOpath, true);
            foreach (string keyname in GPOKey.GetSubKeyNames())
            {
                if (keyname.EndsWith("Machine", StringComparison.OrdinalIgnoreCase))
                {
                    string key = GPOpath + "\\" + keyname + "\\" + @"Software\Policies\Microsoft\Windows";
                    DeleteDelegateSettings(applicationname, Registry.CurrentUser, key, GPO);
                }
            }

            KeyHandle = System.IntPtr.Zero;
        }

        private void DeleteDelegateSettings(string applicationname, RegistryKey rootKey, string Registry_Path, IGroupPolicyObject GPO)
        {
            WSManHelper helper = new WSManHelper(this);
            RegistryKey rKey;
            int i = 0;
            bool otherkeys = false;
            try
            {
                string Registry_Path_Credentials_Delegation = Registry_Path + @"\CredentialsDelegation";
                RegistryKey Allow_Fresh_Credential_Key = rootKey.OpenSubKey(Registry_Path_Credentials_Delegation + @"\" + helper.Key_Allow_Fresh_Credentials, true);
                if (Allow_Fresh_Credential_Key != null)
                {
                    string[] valuenames = Allow_Fresh_Credential_Key.GetValueNames();
                    if (valuenames.Length > 0)
                    {
                        Collection<string> KeyCollection = new Collection<string>();
                        foreach (string value in valuenames)
                        {
                            object keyvalue = Allow_Fresh_Credential_Key.GetValue(value);
                            if (keyvalue != null)
                            {
                                if (!keyvalue.ToString().StartsWith(applicationname, StringComparison.OrdinalIgnoreCase))
                                {
                                    KeyCollection.Add(keyvalue.ToString());
                                    otherkeys = true;
                                }
                            }

                            Allow_Fresh_Credential_Key.DeleteValue(value);
                        }

                        foreach (string keyvalue in KeyCollection)
                        {
                            Allow_Fresh_Credential_Key.SetValue(Convert.ToString(i + 1, CultureInfo.InvariantCulture), keyvalue, RegistryValueKind.String);
                            i++;
                        }
                    }
                }

                if (!otherkeys)
                {
                    rKey = rootKey.OpenSubKey(Registry_Path_Credentials_Delegation, true);
                    if (rKey != null)
                    {
                        object regval1 = rKey.GetValue(helper.Key_Allow_Fresh_Credentials);
                        if (regval1 != null)
                        {
                            rKey.DeleteValue(helper.Key_Allow_Fresh_Credentials, false);
                        }

                        object regval2 = rKey.GetValue(helper.Key_Concatenate_Defaults_AllowFresh);
                        if (regval2 != null)
                        {
                            rKey.DeleteValue(helper.Key_Concatenate_Defaults_AllowFresh, false);
                        }

                        if (rKey.OpenSubKey(helper.Key_Allow_Fresh_Credentials) != null)
                        {
                            rKey.DeleteSubKeyTree(helper.Key_Allow_Fresh_Credentials);
                        }
                    }
                }

                GPO.Save(true, true, new Guid("35378EAC-683F-11D2-A89A-00C04FBBCFA2"), new Guid("6AD20875-336C-4e22-968F-C709ACB15814"));
            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperation", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            catch (ArgumentException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidArgument", ErrorCategory.InvalidArgument, null);
                WriteError(er);
            }
            catch (SecurityException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SecurityException", ErrorCategory.SecurityError, null);
                WriteError(er);
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "UnauthorizedAccess", ErrorCategory.SecurityError, null);
                WriteError(er);
            }
        }
        #endregion private
        /// <summary>
        /// Begin processing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            // If not running elevated, then throw an "elevation required" error message.
            WSManHelper.ThrowIfNotAdministrator();

            if (Role.Equals(Client, StringComparison.OrdinalIgnoreCase))
            {
                DisableClientSideSettings();
            }

            if (Role.Equals(Server, StringComparison.OrdinalIgnoreCase))
            {
                DisableServerSideSettings();
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose()
        {
            // CleanUp();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose(IWSManSession sessionObject)
        {
            sessionObject = null;
            this.Dispose();
        }

        #endregion IDisposable Members
    }
    #endregion DisableWsManCredSSP

    #region EnableCredSSP
    /// <summary>
    /// Enables CredSSP authentication on the client. CredSSP authentication enables
    /// an application to delegate the user's credentials from the client to the
    /// server, hence allowing the user to perform management operations that access
    /// a second hop.
    /// This cmdlet performs the following:
    ///
    /// On the client:
    /// 1. Enables WSMan local configuration on client to enable CredSSP
    /// 2. Sets CredSSP policy AllowFreshCredentials to wsman/Delegate. This policy
    /// allows delegating explicit credentials to a server when server
    /// authentication is achieved via a trusted X509 certificate or Kerberos.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Enable, "WSManCredSSP", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096719")]
    [OutputType(typeof(XmlElement))]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSP")]
    public class EnableWSManCredSSPCommand : WSManCredSSPCommandBase, IDisposable/*, IDynamicParameters*/
    {
        /// <summary>
        /// Delegate parameter.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] DelegateComputer
        {
            get { return delegatecomputer; }

            set { delegatecomputer = value; }
        }

        private string[] delegatecomputer;

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get { return force; }

            set { force = value; }
        }

        private bool force = false;

        // helper variable
        private WSManHelper helper;

        // The application name MUST be "wsman" as wsman got approval from security
        // folks who suggested to register the SPN with name "wsman".
        private const string applicationname = "wsman";

        #region Cmdlet Overloads

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            // If not running elevated, then throw an "elevation required" error message.
            WSManHelper.ThrowIfNotAdministrator();
            helper = new WSManHelper(this);

            // DelegateComputer cannot be specified when Role is other than client
            if ((delegatecomputer != null) && !Role.Equals(Client, StringComparison.OrdinalIgnoreCase))
            {
                string message = helper.FormatResourceMsgFromResourcetext("CredSSPRoleAndDelegateCannotBeSpecified",
                    "DelegateComputer",
                    "Role",
                    Role,
                    Client);

                throw new InvalidOperationException(message);
            }

            // DelegateComputer must be specified when Role is client
            if (Role.Equals(Client, StringComparison.OrdinalIgnoreCase) && (delegatecomputer == null))
            {
                string message = helper.FormatResourceMsgFromResourcetext("CredSSPClientAndDelegateMustBeSpecified",
                    "DelegateComputer",
                    "Role",
                    Client);

                throw new InvalidOperationException(message);
            }

            if (Role.Equals(Client, StringComparison.OrdinalIgnoreCase))
            {
                EnableClientSideSettings();
            }

            if (Role.Equals(Server, StringComparison.OrdinalIgnoreCase))
            {
                EnableServerSideSettings();
            }
        }

        #endregion

        /// <summary>
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        private void EnableClientSideSettings()
        {
            string query = helper.GetResourceMsgFromResourcetext("CredSSPContinueQuery");
            string caption = helper.GetResourceMsgFromResourcetext("CredSSPContinueCaption");
            if (!force && !ShouldContinue(query, caption))
            {
                return;
            }

            IWSManSession m_SessionObj = CreateWSManSession();
            if (m_SessionObj == null)
            {
                return;
            }

            try
            {
                // get the credssp node to check if wsman is configured on this machine
                string result = m_SessionObj.Get(helper.CredSSP_RUri, 0);
                XmlNode node = helper.GetXmlNode(result, helper.CredSSP_SNode, helper.CredSSP_XMLNmsp);

                if (node == null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex, helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"), ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                const string newxmlcontent = @"<cfg:Auth xmlns:cfg=""http://schemas.microsoft.com/wbem/wsman/1/config/client/auth""><cfg:CredSSP>true</cfg:CredSSP></cfg:Auth>";
                try
                {
                    XmlDocument xmldoc = new XmlDocument();
                    
                    // push the xml string with credssp enabled
                    xmldoc.LoadXml(m_SessionObj.Put(helper.CredSSP_RUri, newxmlcontent, 0));

                    // set the Registry using GroupPolicyObject
                    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    {
                        this.UpdateCurrentUserRegistrySettings();
                    }
                    else
                    {
                        ThreadStart start = new ThreadStart(this.UpdateCurrentUserRegistrySettings);
                        Thread thread = new Thread(start);
                        thread.SetApartmentState(ApartmentState.STA);
                        thread.Start();
                        thread.Join();
                    }

                    if (helper.ValidateCreadSSPRegistryRetry(true, delegatecomputer, applicationname))
                    {
                        WriteObject(xmldoc.FirstChild);
                    }
                    else
                    {
                        helper.AssertError(helper.GetResourceMsgFromResourcetext("EnableCredSSPPolicyValidateError"), false, delegatecomputer);
                    }
                }
                catch (COMException)
                {
                    helper.AssertError(m_SessionObj.Error, true, delegatecomputer);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_SessionObj.Error))
                {
                    helper.AssertError(m_SessionObj.Error, true, delegatecomputer);
                }

                if (m_SessionObj != null)
                {
                    Dispose(m_SessionObj);
                }
            }
        }

        private void EnableServerSideSettings()
        {
            string query = helper.GetResourceMsgFromResourcetext("CredSSPServerContinueQuery");
            string caption = helper.GetResourceMsgFromResourcetext("CredSSPContinueCaption");
            if (!force && !ShouldContinue(query, caption))
            {
                return;
            }

            IWSManSession m_SessionObj = CreateWSManSession();
            if (m_SessionObj == null)
            {
                return;
            }

            try
            {
                // get the credssp node to check if wsman is configured on this machine
                string result = m_SessionObj.Get(helper.Service_CredSSP_Uri, 0);
                XmlNode node = helper.GetXmlNode(result,
                    helper.CredSSP_SNode,
                    helper.Service_CredSSP_XMLNmsp);

                if (node == null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex, helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"), ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                try
                {
                    XmlDocument xmldoc = new XmlDocument();
                    string newxmlcontent = string.Format(
                        CultureInfo.InvariantCulture,
                        @"<cfg:Auth xmlns:cfg=""{0}""><cfg:CredSSP>true</cfg:CredSSP></cfg:Auth>",
                        helper.Service_CredSSP_XMLNmsp);

                    // push the xml string with credssp enabled
                    xmldoc.LoadXml(m_SessionObj.Put(helper.Service_CredSSP_Uri, newxmlcontent, 0));
                    WriteObject(xmldoc.FirstChild);
                }
                catch (COMException)
                {
                    helper.AssertError(m_SessionObj.Error, true, delegatecomputer);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_SessionObj.Error))
                {
                    helper.AssertError(m_SessionObj.Error, true, delegatecomputer);
                }

                if (m_SessionObj != null)
                {
                    Dispose(m_SessionObj);
                }
            }
        }

        /// <summary>
        /// </summary>
        private void UpdateCurrentUserRegistrySettings()
        {
            System.IntPtr KeyHandle = System.IntPtr.Zero;
            IGroupPolicyObject GPO = (IGroupPolicyObject)new GPClass();
            GPO.OpenLocalMachineGPO(1);
            KeyHandle = GPO.GetRegistryKey(2);
            RegistryKey rootKey = Registry.CurrentUser;
            const string GPOpath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Group Policy Objects";
            RegistryKey GPOKey = rootKey.OpenSubKey(GPOpath, true);
            foreach (string keyname in GPOKey.GetSubKeyNames())
            {
                if (keyname.EndsWith("Machine", StringComparison.OrdinalIgnoreCase))
                {
                    string key = GPOpath + "\\" + keyname + "\\" + @"Software\Policies\Microsoft\Windows";
                    UpdateGPORegistrySettings(applicationname, this.delegatecomputer, Registry.CurrentUser, key);
                }
            }
            // saving gpo settings
            GPO.Save(true, true, new Guid("35378EAC-683F-11D2-A89A-00C04FBBCFA2"), new Guid("7A9206BD-33AF-47af-B832-D4128730E990"));
        }

        /// <summary>
        /// Updates the grouppolicy registry settings.
        /// </summary>
        /// <param name="applicationname"></param>
        /// <param name="delegatestring"></param>
        /// <param name="rootKey"></param>
        /// <param name="Registry_Path"></param>
        private void UpdateGPORegistrySettings(string applicationname, string[] delegatestring, RegistryKey rootKey, string Registry_Path)
        {
            // RegistryKey rootKey = Registry.LocalMachine;
            RegistryKey Credential_Delegation_Key;
            RegistryKey Allow_Fresh_Credential_Key;
            int i = 0;
            try
            {
                string Registry_Path_Credentials_Delegation = Registry_Path + @"\CredentialsDelegation";
                // open the registry key.If key is not present,create a new one
                Credential_Delegation_Key = rootKey.OpenSubKey(Registry_Path_Credentials_Delegation, true) ?? rootKey.CreateSubKey(Registry_Path_Credentials_Delegation, RegistryKeyPermissionCheck.ReadWriteSubTree);

                Credential_Delegation_Key.SetValue(helper.Key_Allow_Fresh_Credentials, 1, RegistryValueKind.DWord);
                Credential_Delegation_Key.SetValue(helper.Key_Concatenate_Defaults_AllowFresh, 1, RegistryValueKind.DWord);

                // add the delegate value
                Allow_Fresh_Credential_Key = rootKey.OpenSubKey(Registry_Path_Credentials_Delegation + @"\" + helper.Key_Allow_Fresh_Credentials, true) ?? rootKey.CreateSubKey(Registry_Path_Credentials_Delegation + @"\" + helper.Key_Allow_Fresh_Credentials, RegistryKeyPermissionCheck.ReadWriteSubTree);

                if (Allow_Fresh_Credential_Key != null)
                {
                    i = Allow_Fresh_Credential_Key.ValueCount;
                    foreach (string del in delegatestring)
                    {
                        Allow_Fresh_Credential_Key.SetValue(Convert.ToString(i + 1, CultureInfo.InvariantCulture), applicationname + @"/" + del, RegistryValueKind.String);
                        i++;
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "UnauthorizedAccessException", ErrorCategory.PermissionDenied, null);
                WriteError(er);
            }
            catch (SecurityException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SecurityException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            catch (ArgumentException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose()
        {
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose(IWSManSession sessionObject)
        {
            sessionObject = null;
            this.Dispose();
        }

        #endregion IDisposable Members
    }
    #endregion EnableCredSSP

    #region Get-CredSSP

    /// <summary>
    /// Gets the CredSSP related configuration on the client. CredSSP authentication
    /// enables an application to delegate the user's credentials from the client to
    /// the server, hence allowing the user to perform management operations that
    /// access a second hop.
    /// This cmdlet performs the following:
    /// 1. Gets the configuration for WSMan policy on client to enable/disable
    /// CredSSP
    /// 2. Gets the configuration information for the CredSSP policy
    /// AllowFreshCredentials . This policy allows delegating explicit credentials
    /// to a server when server authentication is achieved via a trusted X509
    /// certificate or Kerberos.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cred")]
    [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSP")]
    [Cmdlet(VerbsCommon.Get, "WSManCredSSP", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096838")]
    [OutputType(typeof(string))]
    public class GetWSManCredSSPCommand : PSCmdlet, IDisposable
    {
        #region private
        private WSManHelper helper = null;
        /// <summary>
        /// Method to get the values.
        /// </summary>
        private string GetDelegateSettings(string applicationname)
        {
            RegistryKey rootKey = Registry.LocalMachine;
            RegistryKey rKey;
            string result = string.Empty;
            string[] valuenames = null;
            try
            {
                string Reg_key = helper.Registry_Path_Credentials_Delegation + @"\CredentialsDelegation";
                rKey = rootKey.OpenSubKey(Reg_key);
                if (rKey != null)
                {
                    rKey = rKey.OpenSubKey(helper.Key_Allow_Fresh_Credentials);
                    if (rKey != null)
                    {
                        valuenames = rKey.GetValueNames();
                        if (valuenames.Length > 0)
                        {
                            string listvalue = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                            foreach (string value in valuenames)
                            {
                                object keyvalue = rKey.GetValue(value);
                                if (keyvalue != null)
                                {
                                    if (keyvalue.ToString().StartsWith(applicationname, StringComparison.OrdinalIgnoreCase))
                                    {
                                        result = keyvalue.ToString() + listvalue + result;
                                    }
                                }
                            }

                            if (result.EndsWith(listvalue, StringComparison.OrdinalIgnoreCase))
                            {
                                result = result.Remove(result.Length - 1);
                            }
                        }
                    }
                }
            }
            catch (ArgumentException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.PermissionDenied, null);
                WriteError(er);
            }
            catch (SecurityException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SecurityException", ErrorCategory.PermissionDenied, null);
                WriteError(er);
            }
            catch (ObjectDisposedException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "ObjectDisposedException", ErrorCategory.PermissionDenied, null);
                WriteError(er);
            }

            return result;
        }
        #endregion private

        #region overrides
        /// <summary>
        /// Method to begin processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            // If not running elevated, then throw an "elevation required" error message.
            WSManHelper.ThrowIfNotAdministrator();
            helper = new WSManHelper(this);
            IWSManSession m_SessionObj = null;
            try
            {
                IWSManEx wsmanObject = (IWSManEx)new WSManClass();
                m_SessionObj = (IWSManSession)wsmanObject.CreateSession(null, 0, null);
                string result = m_SessionObj.Get(helper.CredSSP_RUri, 0);
                XmlNode node = helper.GetXmlNode(result, helper.CredSSP_SNode, helper.CredSSP_XMLNmsp);
                if (node == null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex, helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"), ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }
                // The application name MUST be "wsman" as wsman got approval from security
                // folks who suggested to register the SPN with name "wsman".
                const string applicationname = "wsman";
                string credsspResult = GetDelegateSettings(applicationname);
                if (string.IsNullOrEmpty(credsspResult))
                {
                    WriteObject(helper.GetResourceMsgFromResourcetext("NoDelegateFreshCred"));
                }
                else
                {
                    WriteObject(helper.GetResourceMsgFromResourcetext("DelegateFreshCred") + credsspResult);
                }

                // Get the server side settings
                result = m_SessionObj.Get(helper.Service_CredSSP_Uri, 0);
                node = helper.GetXmlNode(result, helper.CredSSP_SNode, helper.Service_CredSSP_XMLNmsp);
                if (node == null)
                {
                    InvalidOperationException ex = new InvalidOperationException();
                    ErrorRecord er = new ErrorRecord(ex, helper.GetResourceMsgFromResourcetext("WinrmNotConfigured"), ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                if (node.InnerText.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    WriteObject(helper.GetResourceMsgFromResourcetext("CredSSPServiceConfigured"));
                }
                else
                {
                    WriteObject(helper.GetResourceMsgFromResourcetext("CredSSPServiceNotConfigured"));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "UnauthorizedAccess", ErrorCategory.PermissionDenied, null);
                WriteError(er);
            }
            catch (SecurityException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SecurityException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            catch (ArgumentException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidArgument", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            catch (System.Xml.XPath.XPathException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "XPathException", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_SessionObj.Error))
                {
                    helper.AssertError(m_SessionObj.Error, true, null);
                }

                if (m_SessionObj != null)
                {
                    Dispose(m_SessionObj);
                }
            }
        }
        #endregion overrides
        #region IDisposable Members

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose()
        {
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Public dispose method.
        /// </summary>
        public
        void
        Dispose(IWSManSession sessionObject)
        {
            sessionObject = null;
            this.Dispose();
        }

        #endregion IDisposable Members
    }

    #endregion
}
