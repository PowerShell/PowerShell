// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.WSMan.Management
{
    #region Set-WsManQuickConfig

    //

    /// <summary>
    /// Performs configuration actions to enable the local machine for remote
    /// management. Steps include:
    /// 1. Check if WinRM service is running. If not start the WinRM service
    /// 2. Set the WinRM service type to auto start
    /// 3. Create a listener to accept request on any IP address. By default
    /// transport is http
    /// 4. Enable firewall exception for WS-Management traffic.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WSManQuickConfig", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=141463")]
    public class SetWSManQuickConfigCommand : PSCmdlet, IDisposable
    {
        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Indicates a https listener to be created. If this switch is not specified
        /// then by default a http listener will be created.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        // helper variable
        private WSManHelper helper;

        /// <summary>
        /// Property that sets force parameter. This will allow
        /// configuring WinRM without prompting the user.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get { return force; }

            set { force = value; }
        }

        private bool force = false;

        /// <summary>
        /// Property that will allow configuring WinRM with Public profile exception enabled.
        /// </summary>
        [Parameter()]
        public SwitchParameter SkipNetworkProfileCheck
        {
            get { return skipNetworkProfileCheck; }

            set { skipNetworkProfileCheck = value; }
        }

        private bool skipNetworkProfileCheck = false;

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            // If not running elevated, then throw an "elevation required" error message.
            WSManHelper.ThrowIfNotAdministrator();
            helper = new WSManHelper(this);
            string query = helper.GetResourceMsgFromResourcetext("QuickConfigContinueQuery");
            string caption = helper.GetResourceMsgFromResourcetext("QuickConfigContinueCaption");
            if (!force && !ShouldContinue(query, caption))
            {
                return;
            }

            QuickConfigRemoting(true);
            QuickConfigRemoting(false);
        }

        #region private

        private void QuickConfigRemoting(bool serviceonly)
        {
            IWSManSession m_SessionObj = null;
            try
            {
                string transport;
                IWSManEx wsmanObject = (IWSManEx)new WSManClass();
                m_SessionObj = (IWSManSession)wsmanObject.CreateSession(null, 0, null);
                string xpathEnabled = string.Empty;
                string xpathText = string.Empty;
                string xpathUpdate = string.Empty;
                string analysisInputXml = string.Empty;
                string action = string.Empty;
                string xpathStatus = string.Empty;
                string xpathResult = string.Empty;

                if (!usessl)
                {
                    transport = "http";
                }
                else
                {
                    transport = "https";
                }

                if (serviceonly)
                {
                    analysisInputXml = @"<AnalyzeService_INPUT xmlns=""http://schemas.microsoft.com/wbem/wsman/1/config/service""></AnalyzeService_INPUT>";
                    action = "AnalyzeService";
                }
                else
                {
                    string openAllProfiles = skipNetworkProfileCheck ? "<Force/>" : string.Empty;
                    analysisInputXml = @"<Analyze_INPUT xmlns=""http://schemas.microsoft.com/wbem/wsman/1/config/service""><Transport>" + transport + "</Transport>" + openAllProfiles + "</Analyze_INPUT>";
                    action = "Analyze";
                }

                string analysisOutputXml = m_SessionObj.Invoke(action, "winrm/config/service", analysisInputXml, 0);
                XmlDocument resultopxml = new XmlDocument();
                resultopxml.LoadXml(analysisOutputXml);

                if (serviceonly)
                {
                    xpathEnabled = "/cfg:AnalyzeService_OUTPUT/cfg:RemotingEnabled";
                    xpathText = "/cfg:AnalyzeService_OUTPUT/cfg:Results";
                    xpathUpdate = "/cfg:AnalyzeService_OUTPUT/cfg:EnableService_INPUT";
                }
                else
                {
                    xpathEnabled = "/cfg:Analyze_OUTPUT/cfg:RemotingEnabled";
                    xpathText = "/cfg:Analyze_OUTPUT/cfg:Results";
                    xpathUpdate = "/cfg:Analyze_OUTPUT/cfg:EnableRemoting_INPUT";
                }

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(resultopxml.NameTable);
                nsmgr.AddNamespace("cfg", "http://schemas.microsoft.com/wbem/wsman/1/config/service");
                string enabled = resultopxml.SelectSingleNode(xpathEnabled, nsmgr).InnerText;
                XmlNode sourceAttribute = resultopxml.SelectSingleNode(xpathEnabled, nsmgr).Attributes.GetNamedItem("Source");
                string source = null;
                if (sourceAttribute != null)
                {
                    source = sourceAttribute.Value;
                }

                string rxml = string.Empty;
                if (enabled.Equals("true"))
                {
                    string Err_Msg = string.Empty;
                    if (serviceonly)
                    {
                        Err_Msg = WSManResourceLoader.GetResourceString("L_QuickConfigNoServiceChangesNeeded_Message");
                    }
                    else
                    {
                        Err_Msg = WSManResourceLoader.GetResourceString("L_QuickConfigNoChangesNeeded_Message");
                    }
                    //  ArgumentException e = new ArgumentException(Err_Msg);
                    // ErrorRecord er = new ErrorRecord(e, "InvalidOperation", ErrorCategory.InvalidOperation, null);
                    //  WriteError(er);
                    WriteObject(Err_Msg);
                    return;
                }

                if (!enabled.Equals("false"))
                {
                    ArgumentException e = new ArgumentException(WSManResourceLoader.GetResourceString("L_QuickConfig_InvalidBool_0_ErrorMessage"));
                    ErrorRecord er = new ErrorRecord(e, "InvalidOperation", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                string resultAction = resultopxml.SelectSingleNode(xpathText, nsmgr).InnerText;
                if ( source != null && source.Equals("GPO"))
                {
                    string Info_Msg = WSManResourceLoader.GetResourceString("L_QuickConfig_RemotingDisabledbyGP_00_ErrorMessage");
                    Info_Msg += " " + resultAction;
                    ArgumentException e = new ArgumentException(Info_Msg);
                    WriteError(new ErrorRecord(e, "NotSpecified", ErrorCategory.NotSpecified, null));
                    return;
                }

                string inputXml = resultopxml.SelectSingleNode(xpathUpdate, nsmgr).OuterXml;
                if (resultAction.Equals(string.Empty) || inputXml.Equals(string.Empty))
                {
                    ArgumentException e = new ArgumentException(WSManResourceLoader.GetResourceString("L_ERR_Message") + WSManResourceLoader.GetResourceString("L_QuickConfig_MissingUpdateXml_0_ErrorMessage"));
                    ErrorRecord er = new ErrorRecord(e, "InvalidOperation", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                if (serviceonly)
                {
                    action = "EnableService";
                }
                else
                {
                    action = "EnableRemoting";
                }

                rxml = m_SessionObj.Invoke(action, "winrm/config/service", inputXml, 0);
                XmlDocument finalxml = new XmlDocument();
                finalxml.LoadXml(rxml);

                if (serviceonly)
                {
                    xpathStatus = "/cfg:EnableService_OUTPUT/cfg:Status";
                    xpathResult = "/cfg:EnableService_OUTPUT/cfg:Results";
                }
                else
                {
                    xpathStatus = "/cfg:EnableRemoting_OUTPUT/cfg:Status";
                    xpathResult = "/cfg:EnableRemoting_OUTPUT/cfg:Results";
                }

                if (finalxml.SelectSingleNode(xpathStatus, nsmgr).InnerText.ToString().Equals("succeeded"))
                {
                    if (serviceonly)
                    {
                        WriteObject(WSManResourceLoader.GetResourceString("L_QuickConfigUpdatedService_Message"));
                    }
                    else
                    {
                        WriteObject(WSManResourceLoader.GetResourceString("L_QuickConfigUpdated_Message"));
                    }

                    WriteObject(finalxml.SelectSingleNode(xpathResult, nsmgr).InnerText);
                }
                else
                {
                    helper.AssertError(WSManResourceLoader.GetResourceString("L_ERR_Message") + WSManResourceLoader.GetResourceString("L_QuickConfigUpdateFailed_ErrorMessage"), false, null);
                }
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
        #endregion private

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
    #endregion Set-WsManQuickConfig
}
