// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

using Microsoft.Win32;

namespace Microsoft.WSMan.Management
{
    [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#")]
    internal sealed class WSManHelper
    {
        // regular expressions
        private const string PTRN_URI_LAST = @"([a-z_][-a-z0-9._]*)$";
        private const string PTRN_OPT = @"^-([a-z]+):(.*)";
        private const string PTRN_HASH_TOK = @"\s*([\w:]+)\s*=\s*(\$null|""([^""]*)"")\s*";

        // schemas
        private const string URI_IPMI = @"http://schemas.dmtf.org/wbem/wscim/1/cim-schema";
        private const string URI_WMI = @"http://schemas.microsoft.com/wbem/wsman/1/wmi";
        private const string NS_IPMI = @"http://schemas.dmtf.org/wbem/wscim/1/cim-schema";
        private const string NS_CIMBASE = @"http://schemas.dmtf.org/wbem/wsman/1/base";
        private const string NS_WSMANL = @"http://schemas.microsoft.com";
        private const string NS_XSI = @"xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""";
        private const string ATTR_NIL = @"xsi:nil=""true""";
        private const string ATTR_NIL_NAME = @"xsi:nil";
        private const string NS_XSI_URI = @"http://www.w3.org/2001/XMLSchema-instance";
        private const string ALIAS_XPATH = @"xpath";
        private const string URI_XPATH_DIALECT = @"http://www.w3.org/TR/1999/REC-xpath-19991116";

        // credSSP strings
        internal string CredSSP_RUri = "winrm/config/client/auth";
        internal string CredSSP_XMLNmsp = "http://schemas.microsoft.com/wbem/wsman/1/config/client/auth";
        internal string CredSSP_SNode = "/cfg:Auth/cfg:CredSSP";
        internal string Client_uri = "winrm/config/client";
        internal string urlprefix_node = "/cfg:Client/cfg:URLPrefix";
        internal string Client_XMLNmsp = "http://schemas.microsoft.com/wbem/wsman/1/config/client";

        internal string Service_Uri = "winrm/config/service";
        internal string Service_UrlPrefix_Node = "/cfg:Service/cfg:URLPrefix";
        internal string Service_XMLNmsp = "http://schemas.microsoft.com/wbem/wsman/1/config/service";
        internal string Service_CredSSP_Uri = "winrm/config/service/auth";
        internal string Service_CredSSP_XMLNmsp = "http://schemas.microsoft.com/wbem/wsman/1/config/service/auth";

        // gpo registry path and keys
        internal string Registry_Path_Credentials_Delegation = @"SOFTWARE\Policies\Microsoft\Windows";
        internal string Key_Allow_Fresh_Credentials = "AllowFreshCredentials";
        internal string Key_Concatenate_Defaults_AllowFresh = "ConcatenateDefaults_AllowFresh";
        internal string Delegate = "delegate";
        internal string keyAllowcredssp = "AllowCredSSP";

        // 'Constants for MS-XML
        private const string NODE_ATTRIBUTE = "2";
        private const int NODE_TEXT = 3;

        // strings for dialects
        internal string ALIAS_WQL = @"wql";
        internal string ALIAS_ASSOCIATION = @"association";
        internal string ALIAS_SELECTOR = @"selector";
        internal string URI_WQL_DIALECT = @"http://schemas.microsoft.com/wbem/wsman/1/WQL";
        internal string URI_SELECTOR_DIALECT = @"http://schemas.dmtf.org/wbem/wsman/1/wsman/SelectorFilter";
        internal string URI_ASSOCIATION_DIALECT = @" http://schemas.dmtf.org/wbem/wsman/1/cimbinding/associationFilter";

        // string for operation
        internal string WSManOp = null;

        private readonly PSCmdlet cmdletname;
        private readonly NavigationCmdletProvider _provider;

        private FileStream _fs;
        private StreamReader _sr;

        private static readonly ResourceManager _resourceMgr = new ResourceManager("Microsoft.WSMan.Management.resources.WsManResources", typeof(WSManHelper).GetTypeInfo().Assembly);

        //
        //
        // Below class is just a static container which would release sessions in case this DLL is unloaded.
        internal sealed class Sessions
        {
            /// <summary>
            /// Dictionary object to store the connection.
            /// </summary>
            internal static readonly Dictionary<string, object> SessionObjCache = new Dictionary<string, object>();

            ~Sessions()
            {
                ReleaseSessions();
            }
        }

        internal static readonly Sessions AutoSession = new Sessions();
        //
        //
        //

        internal static void ReleaseSessions()
        {
            lock (Sessions.SessionObjCache)
            {
                object sessionobj;
                foreach (string key in Sessions.SessionObjCache.Keys)
                {
                    Sessions.SessionObjCache.TryGetValue(key, out sessionobj);
                    try
                    {
                        Marshal.ReleaseComObject(sessionobj);
                    }
                    catch (ArgumentException)
                    {
                        // Somehow the object was a null reference. Ignore the error
                    }

                    sessionobj = null;
                }

                Sessions.SessionObjCache.Clear();
            }
        }

        internal WSManHelper()
        {
        }

        internal WSManHelper(PSCmdlet cmdlet)
        {
            cmdletname = cmdlet;
        }

        internal WSManHelper(NavigationCmdletProvider provider)
        {
            _provider = provider;
        }

        internal static void ThrowIfNotAdministrator()
        {
            System.Security.Principal.WindowsIdentity currentIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(currentIdentity);
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                string message = _resourceMgr.GetString("ErrorElevationNeeded");
                throw new InvalidOperationException(message);
            }
        }

        internal string GetResourceMsgFromResourcetext(string rscname)
        {
            return _resourceMgr.GetString(rscname);
        }

        internal static string FormatResourceMsgFromResourcetextS(string rscname,
            params object[] args)
        {
            return FormatResourceMsgFromResourcetextS(_resourceMgr, rscname, args);
        }

        internal string FormatResourceMsgFromResourcetext(string resourceName,
            params object[] args)
        {
            return FormatResourceMsgFromResourcetextS(_resourceMgr, resourceName, args);
        }

        private static string FormatResourceMsgFromResourcetextS(
            ResourceManager resourceManager,
            string resourceName,
            object[] args)
        {
            ArgumentNullException.ThrowIfNull(resourceManager);
            ArgumentException.ThrowIfNullOrEmpty(resourceName);

            string template = resourceManager.GetString(resourceName);

            string result = null;
            if (template != null)
            {
                result = string.Format(CultureInfo.CurrentCulture,
                    template, args);
            }

            return result;
        }

        /// <summary>
        /// Add a session to dictionary.
        /// </summary>
        /// <param name="key">Connection string.</param>
        /// <param name="value">Session object.</param>
        internal void AddtoDictionary(string key, object value)
        {
            key = key.ToLowerInvariant();
            lock (Sessions.SessionObjCache)
            {
                if (!Sessions.SessionObjCache.ContainsKey(key))
                {
                    Sessions.SessionObjCache.Add(key, value);
                }
                else
                {
                    object objsession = null;
                    Sessions.SessionObjCache.TryGetValue(key, out objsession);
                    try
                    {
                        Marshal.ReleaseComObject(objsession);
                    }
                    catch (ArgumentException)
                    {
                        // Somehow the object was a null reference. Ignore the error
                    }

                    Sessions.SessionObjCache.Remove(key);
                    Sessions.SessionObjCache.Add(key, value);
                }
            }
        }

        internal object RemoveFromDictionary(string computer)
        {
            object objsession = null;
            computer = computer.ToLowerInvariant();
            lock (Sessions.SessionObjCache)
            {
                if (Sessions.SessionObjCache.ContainsKey(computer))
                {
                    Sessions.SessionObjCache.TryGetValue(computer, out objsession);
                    try
                    {
                        Marshal.ReleaseComObject(objsession);
                    }
                    catch (ArgumentException)
                    {
                        // Somehow the object was a null reference. Ignore the error
                    }

                    Sessions.SessionObjCache.Remove(computer);
                }
            }

            return objsession;
        }

        internal static Dictionary<string, object> GetSessionObjCache()
        {
            try
            {
                lock (Sessions.SessionObjCache)
                {
                    if (!Sessions.SessionObjCache.ContainsKey("localhost"))
                    {
                        IWSManEx wsmanObject = (IWSManEx)new WSManClass();
                        IWSManSession SessionObj = (IWSManSession)wsmanObject.CreateSession(null, 0, null);
                        Sessions.SessionObjCache.Add("localhost", SessionObj);
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (System.UnauthorizedAccessException)
            {
            }
            catch (COMException)
            {
            }

            return Sessions.SessionObjCache;
        }

        internal string GetRootNodeName(string operation, string resourceUri, string actionStr)
        {
            string resultStr = null, sfx = null;
            if (resourceUri != null)
            {
                resultStr = resourceUri;
                resultStr = StripParams(resultStr);

                Regex regexpr = new Regex(PTRN_URI_LAST, RegexOptions.IgnoreCase);
                MatchCollection matches = regexpr.Matches(resultStr);
                if (matches.Count > 0)
                {
                    if (operation.Equals("invoke", StringComparison.OrdinalIgnoreCase))
                    {
                        sfx = "_INPUT";
                        resultStr = string.Concat(actionStr, sfx);
                    }
                    else
                    {
                        resultStr = matches[0].ToString();
                    }
                }
                else
                {
                    // error
                }
            }

            return resultStr;
        }

        internal string StripParams(string uri)
        {
            int pos = uri.IndexOf('?');
            if (pos > 0)
                return uri.Substring(pos, uri.Length - pos);
            else
                return uri;
        }

        internal string ReadFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException(GetResourceMsgFromResourcetext("InvalidFileName"));
            }

            string strOut = null;
            try
            {
                _fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                // create stream Reader
                _sr = new StreamReader(_fs);
                strOut = _sr.ReadToEnd();
            }
            catch (ArgumentNullException e)
            {
                ErrorRecord er = new ErrorRecord(e, "ArgumentNullException", ErrorCategory.InvalidArgument, null);
                cmdletname.ThrowTerminatingError(er);
            }
            catch (UnauthorizedAccessException e)
            {
                ErrorRecord er = new ErrorRecord(e, "UnauthorizedAccessException", ErrorCategory.PermissionDenied, null);
                cmdletname.ThrowTerminatingError(er);
            }
            catch (FileNotFoundException e)
            {
                ErrorRecord er = new ErrorRecord(e, "FileNotFoundException", ErrorCategory.ObjectNotFound, null);
                cmdletname.ThrowTerminatingError(er);
            }
            catch (DirectoryNotFoundException e)
            {
                ErrorRecord er = new ErrorRecord(e, "DirectoryNotFoundException", ErrorCategory.ObjectNotFound, null);
                cmdletname.ThrowTerminatingError(er);
            }
            catch (System.Security.SecurityException e)
            {
                ErrorRecord er = new ErrorRecord(e, "SecurityException", ErrorCategory.SecurityError, null);
                cmdletname.ThrowTerminatingError(er);
            }
            finally
            {
                _sr?.Dispose();
                _fs?.Dispose();
            }

            return strOut;
        }

        internal string ProcessInput(IWSManEx wsman, string filepath, string operation, string root, Hashtable valueset, IWSManResourceLocator resourceUri, IWSManSession sessionObj)
        {
            string resultString = null;

            // if file path is given
            if (!string.IsNullOrEmpty(filepath) && valueset == null)
            {
                if (!File.Exists(filepath))
                {
                    throw new FileNotFoundException(_resourceMgr.GetString("InvalidFileName"));
                }

                resultString = ReadFile(filepath);
                return resultString;
            }

            switch (operation)
            {
                case "new":
                case "invoke":

                    string parameters = null, nilns = null;
                    string xmlns = GetXmlNs(resourceUri.ResourceUri);

                    // if valueset is given, i.e hashtable
                    if (valueset != null)
                    {
                        foreach (DictionaryEntry entry in valueset)
                        {
                            parameters = parameters + "<p:" + entry.Key.ToString();
                            if (entry.Value.ToString() == null)
                            {
                                parameters = parameters + " " + ATTR_NIL;
                                nilns = " " + NS_XSI;
                            }

                            parameters = parameters + ">" + entry.Value.ToString() + "</p:" + entry.Key.ToString() + ">";
                        }
                    }

                    resultString = "<p:" + root + " " + xmlns + nilns + ">" + parameters + "</p:" + root + ">";

                    break;
                case "set":

                    string getResult = sessionObj.Get(resourceUri, 0);
                    XmlDocument xmlfile = new XmlDocument();
                    xmlfile.LoadXml(getResult);

                    string xpathString = null;
                    if (valueset != null)
                    {
                        foreach (DictionaryEntry entry in valueset)
                        {
                            xpathString = @"/*/*[local-name()=""" + entry.Key + @"""]";
                            if (entry.Key.ToString().Equals("location", StringComparison.OrdinalIgnoreCase))
                            {
                                // 'Ignore cim:Location
                                xpathString = @"/*/*[local-name()=""" + entry.Key + @""" and namespace-uri() != """ + NS_CIMBASE + @"""]";
                            }

                            XmlNodeList nodes = xmlfile.SelectNodes(xpathString);
                            if (nodes.Count == 0)
                            {
                                throw new ArgumentException(_resourceMgr.GetString("NoResourceMatch"));
                            }
                            else if (nodes.Count > 1)
                            {
                                throw new ArgumentException(_resourceMgr.GetString("MultipleResourceMatch"));
                            }
                            else
                            {
                                XmlNode node = nodes[0];
                                if (node.HasChildNodes)
                                {
                                    if (node.ChildNodes.Count > 1)
                                    {
                                        throw new ArgumentException(_resourceMgr.GetString("NOAttributeMatch"));
                                    }
                                    else
                                    {
                                        XmlNode tmpNode = node.ChildNodes[0]; //.Item[0];
                                        if (!tmpNode.NodeType.ToString().Equals("text", StringComparison.OrdinalIgnoreCase))
                                        {
                                            throw new ArgumentException(_resourceMgr.GetString("NOAttributeMatch"));
                                        }
                                    }
                                }

                                if (string.IsNullOrEmpty(entry.Key.ToString()))
                                {
                                    // XmlNode newnode = xmlfile.CreateNode(XmlNodeType.Attribute, ATTR_NIL_NAME, NS_XSI_URI);
                                    XmlAttribute newnode = xmlfile.CreateAttribute(nameof(XmlNodeType.Attribute), ATTR_NIL_NAME, NS_XSI_URI);
                                    newnode.Value = "true";
                                    node.Attributes.Append(newnode);
                                    // (newnode.Attributes.Item(0).FirstChild   );
                                    node.Value = string.Empty;
                                }
                                else
                                {
                                    node.Attributes.RemoveNamedItem(ATTR_NIL_NAME);
                                    node.InnerText = entry.Value.ToString();
                                }
                            }
                        }
                    }

                    resultString = xmlfile.OuterXml;
                    break;
            }

            return resultString;
        }

        internal string GetXmlNs(string resUri)
        {
            return (@"xmlns:p=""" + StripParams(resUri) + @"""");
        }

        internal XmlNode GetXmlNode(string xmlString, string xpathpattern, string xmlnamespace)
        {
            XmlNode node = null;
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(xmlString);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xDoc.NameTable);
            if (!string.IsNullOrEmpty(xmlnamespace))
            {
                nsmgr.AddNamespace("cfg", xmlnamespace);
            }

            node = xDoc.SelectSingleNode(xpathpattern, nsmgr);
            return node;
        }

        internal string CreateConnectionString(Uri ConnUri, int port, string computername, string applicationname)
        {
            string ConnectionString = null;
            if (ConnUri != null)
            {
                ConnectionString = ConnUri.OriginalString;
            }
            else
            {
                if (computername == null && (port != 0 || applicationname != null))
                {
                    // the user didn't give us a computer name but he gave a port and/or application name;
                    // in this case we need to have a computer name, to form the connection string;
                    // assume localhost
                    computername = "localhost";
                }

                ConnectionString = computername;
                if (port != 0)
                {
                    ConnectionString = ConnectionString + ":" + port;
                }

                if (applicationname != null)
                {
                    ConnectionString = ConnectionString + "/" + applicationname;
                }
            }

            return ConnectionString;
        }

        internal IWSManResourceLocator InitializeResourceLocator(Hashtable optionset, Hashtable selectorset, string fragment, Uri dialect, IWSManEx wsmanObj, Uri resourceuri)
        {
            string resource = null;
            if (resourceuri != null)
            {
                resource = resourceuri.ToString();
            }

            if (selectorset != null)
            {
                resource += "?";
                int i = 0;
                foreach (DictionaryEntry entry in selectorset)
                {
                    i++;
                    resource = resource + entry.Key.ToString() + "=" + entry.Value.ToString();
                    if (i < selectorset.Count)
                        resource += "+";
                }
            }

            IWSManResourceLocator m_resource = null;
            try
            {
                m_resource = (IWSManResourceLocator)wsmanObj.CreateResourceLocator(resource);

                if (optionset != null)
                {
                    foreach (DictionaryEntry entry in optionset)
                    {
                        if (entry.Value.ToString() == null)
                        {
                            m_resource.AddOption(entry.Key.ToString(), null, 1);
                        }
                        else
                        {
                            m_resource.AddOption(entry.Key.ToString(), entry.Value, 1);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(fragment))
                {
                    m_resource.FragmentPath = fragment;
                }

                if (dialect != null)
                {
                    m_resource.FragmentDialect = dialect.ToString();
                }
            }
            catch (COMException ex)
            {
                AssertError(ex.Message, false, null);
            }

            return m_resource;
        }

        /// <summary>
        /// Used to resolve authentication from the parameters chosen by the user.
        /// User has the following options:
        /// 1. AuthMechanism + Credential
        /// 2. CertificateThumbPrint
        ///
        /// All the above are mutually exclusive.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If there is ambiguity as specified above.
        /// </exception>
        internal static void ValidateSpecifiedAuthentication(AuthenticationMechanism authentication, PSCredential credential, string certificateThumbprint)
        {
            if ((credential != null) && (certificateThumbprint != null))
            {
                string message = FormatResourceMsgFromResourcetextS(
                    "AmbiguousAuthentication",
                        "CertificateThumbPrint", "credential");

                throw new InvalidOperationException(message);
            }

            if ((authentication != AuthenticationMechanism.Default) &&
                (authentication != AuthenticationMechanism.ClientCertificate) &&
                (certificateThumbprint != null))
            {
                string message = FormatResourceMsgFromResourcetextS(
                    "AmbiguousAuthentication",
                        "CertificateThumbPrint", authentication.ToString());

                throw new InvalidOperationException(message);
            }
        }

        internal IWSManSession CreateSessionObject(IWSManEx wsmanObject, AuthenticationMechanism authentication, SessionOption sessionoption, PSCredential credential, string connectionString, string certificateThumbprint, bool usessl)
        {
            ValidateSpecifiedAuthentication(authentication, credential, certificateThumbprint);

            ////if authentication is given
            int sessionFlags = 0;

            if (authentication.ToString() != null)
            {
                if (authentication.Equals(AuthenticationMechanism.None))
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUseNoAuthentication;
                }

                if (authentication.Equals(AuthenticationMechanism.Basic))
                {
                    sessionFlags = sessionFlags | (int)WSManSessionFlags.WSManFlagUseBasic | (int)WSManSessionFlags.WSManFlagCredUserNamePassword;
                }

                if (authentication.Equals(AuthenticationMechanism.Negotiate))
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUseNegotiate;
                }

                if (authentication.Equals(AuthenticationMechanism.Kerberos))
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUseKerberos;
                }

                if (authentication.Equals(AuthenticationMechanism.Digest))
                {
                    sessionFlags = sessionFlags | (int)WSManSessionFlags.WSManFlagUseDigest | (int)WSManSessionFlags.WSManFlagCredUserNamePassword;
                }

                if (authentication.Equals(AuthenticationMechanism.Credssp))
                {
                    sessionFlags = sessionFlags | (int)WSManSessionFlags.WSManFlagUseCredSsp | (int)WSManSessionFlags.WSManFlagCredUserNamePassword;
                }

                if (authentication.Equals(AuthenticationMechanism.ClientCertificate))
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUseClientCertificate;
                }
            }

            IWSManConnectionOptionsEx2 connObject = (IWSManConnectionOptionsEx2)wsmanObject.CreateConnectionOptions();
            if (credential != null)
            {
                // connObject = (IWSManConnectionOptionsEx2)wsmanObject.CreateConnectionOptions();
                System.Net.NetworkCredential nwCredential = new System.Net.NetworkCredential();
                if (credential.UserName != null)
                {
                    nwCredential = credential.GetNetworkCredential();
                    if (string.IsNullOrEmpty(nwCredential.Domain))
                    {
                        if (authentication.Equals(AuthenticationMechanism.Digest) || authentication.Equals(AuthenticationMechanism.Basic))
                        {
                            connObject.UserName = nwCredential.UserName;
                        }
                        else
                        {
                            // just wanted to not use null domain, empty is actually fine
                            connObject.UserName = "\\" + nwCredential.UserName;
                        }
                    }
                    else
                    {
                        connObject.UserName = nwCredential.Domain + "\\" + nwCredential.UserName;
                    }

                    connObject.Password = nwCredential.Password;
                    if (!authentication.Equals(AuthenticationMechanism.Credssp) || !authentication.Equals(AuthenticationMechanism.Digest) || authentication.Equals(AuthenticationMechanism.Basic))
                    {
                        sessionFlags |= (int)WSManSessionFlags.WSManFlagCredUserNamePassword;
                    }
                }
            }

            if (certificateThumbprint != null)
            {
                connObject.CertificateThumbprint = certificateThumbprint;
                sessionFlags |= (int)WSManSessionFlags.WSManFlagUseClientCertificate;
            }

            if (sessionoption != null)
            {
                if (sessionoption.ProxyAuthentication != 0)
                {
                    int ProxyAccessflags = 0;
                    int ProxyAuthenticationFlags = 0;
                    if (sessionoption.ProxyAccessType.Equals(ProxyAccessType.ProxyIEConfig))
                    {
                        ProxyAccessflags = connObject.ProxyIEConfig();
                    }
                    else if (sessionoption.ProxyAccessType.Equals(ProxyAccessType.ProxyAutoDetect))
                    {
                        ProxyAccessflags = connObject.ProxyAutoDetect();
                    }
                    else if (sessionoption.ProxyAccessType.Equals(ProxyAccessType.ProxyNoProxyServer))
                    {
                        ProxyAccessflags = connObject.ProxyNoProxyServer();
                    }
                    else if (sessionoption.ProxyAccessType.Equals(ProxyAccessType.ProxyWinHttpConfig))
                    {
                        ProxyAccessflags = connObject.ProxyWinHttpConfig();
                    }

                    if (sessionoption.ProxyAuthentication.Equals(ProxyAuthentication.Basic))
                    {
                        ProxyAuthenticationFlags = connObject.ProxyAuthenticationUseBasic();
                    }
                    else if (sessionoption.ProxyAuthentication.Equals(ProxyAuthentication.Negotiate))
                    {
                        ProxyAuthenticationFlags = connObject.ProxyAuthenticationUseNegotiate();
                    }
                    else if (sessionoption.ProxyAuthentication.Equals(ProxyAuthentication.Digest))
                    {
                        ProxyAuthenticationFlags = connObject.ProxyAuthenticationUseDigest();
                    }

                    if (sessionoption.ProxyCredential != null)
                    {
                        try
                        {
                            connObject.SetProxy(ProxyAccessflags, ProxyAuthenticationFlags, sessionoption.ProxyCredential.UserName, sessionoption.ProxyCredential.Password);
                        }
                        catch (Exception ex)
                        {
                            AssertError(ex.Message, false, null);
                        }
                    }
                    else
                    {
                        connObject.SetProxy((int)sessionoption.ProxyAccessType, (int)sessionoption.ProxyAuthentication, null, null);
                    }
                }

                if (sessionoption.SkipCACheck)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagSkipCACheck;
                }

                if (sessionoption.SkipCNCheck)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagSkipCNCheck;
                }

                if (sessionoption.SPNPort > 0)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagEnableSpnServerPort;
                }

                if (sessionoption.UseUtf16)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUtf16;
                }
                else
                {
                    // If UseUtf16 is false, then default Encoding is Utf8
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagUtf8;
                }

                if (!sessionoption.UseEncryption)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagNoEncryption;
                }

                if (sessionoption.SkipRevocationCheck)
                {
                    sessionFlags |= (int)WSManSessionFlags.WSManFlagSkipRevocationCheck;
                }
            }
            else
            {
                // If SessionOption is null then, default Encoding is Utf8
                sessionFlags |= (int)WSManSessionFlags.WSManFlagUtf8;
            }

            if (usessl)
            {
                sessionFlags |= (int)WSManSessionFlags.WSManFlagUseSsl;
            }

            IWSManSession m_SessionObj = null;
            try
            {
                m_SessionObj = (IWSManSession)wsmanObject.CreateSession(connectionString, sessionFlags, connObject);
                if (sessionoption != null)
                {
                    if (sessionoption.OperationTimeout > 0)
                    {
                        m_SessionObj.Timeout = sessionoption.OperationTimeout;
                    }
                }
            }
            catch (COMException ex)
            {
                AssertError(ex.Message, false, null);
            }

            return m_SessionObj;
        }

        internal void CleanUp()
        {
            if (_sr != null)
            {
                _sr.Dispose();
                _sr = null;
            }

            if (_fs != null)
            {
                _fs.Dispose();
                _fs = null;
            }
        }

        internal string GetFilterString(Hashtable seletorset)
        {
            StringBuilder filter = new StringBuilder();
            foreach (DictionaryEntry entry in seletorset)
            {
                if (entry.Key != null && entry.Value != null)
                {
                    filter.Append(entry.Key.ToString());
                    filter.Append('=');
                    filter.Append(entry.Value.ToString());
                    filter.Append('+');
                }
            }

            filter.Remove(filter.ToString().Length - 1, 1);
            return filter.ToString();
        }

        internal void AssertError(string ErrorMessage, bool IsWSManError, object targetobject)
        {
            if (IsWSManError)
            {
                XmlDocument ErrorDoc = new XmlDocument();
                ErrorDoc.LoadXml(ErrorMessage);
                InvalidOperationException ex = new InvalidOperationException(ErrorDoc.OuterXml);
                ErrorRecord er = new ErrorRecord(ex, "WsManError", ErrorCategory.InvalidOperation, targetobject);
                if (cmdletname != null)
                {
                    cmdletname.ThrowTerminatingError(er);
                }
                else
                {
                    _provider.ThrowTerminatingError(er);
                }
            }
            else
            {
                InvalidOperationException ex = new InvalidOperationException(ErrorMessage);
                ErrorRecord er = new ErrorRecord(ex, "WsManError", ErrorCategory.InvalidOperation, targetobject);
                if (cmdletname != null)
                {
                    cmdletname.ThrowTerminatingError(er);
                }
                else
                {
                    _provider.ThrowTerminatingError(er);
                }
            }
        }

        internal string GetURIWithFilter(string uri, string filter, Hashtable selectorset, string operation)
        {
            StringBuilder sburi = new StringBuilder();
            sburi.Append(uri);
            sburi.Append('?');

            if (operation.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                sburi.Append(GetFilterString(selectorset));
                if (sburi.ToString().EndsWith('?'))
                {
                    sburi.Remove(sburi.Length - 1, 1);
                }
            }

            return sburi.ToString();
        }

        /// <summary>
        /// This method is used by Connect-WsMan Cmdlet and New-Item of WsMan Provider to create connection to WsMan.
        /// </summary>
        /// <param name="ParameterSetName"></param>
        /// <param name="connectionuri"></param>
        /// <param name="port"></param>
        /// <param name="computername"></param>
        /// <param name="applicationname"></param>
        /// <param name="usessl"></param>
        /// <param name="authentication"></param>
        /// <param name="sessionoption"></param>
        /// <param name="credential"></param>
        /// <param name="certificateThumbprint"></param>
        internal void CreateWsManConnection(string ParameterSetName, Uri connectionuri, int port, string computername, string applicationname, bool usessl, AuthenticationMechanism authentication, SessionOption sessionoption, PSCredential credential, string certificateThumbprint)
        {
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            try
            {
                string connectionStr = CreateConnectionString(connectionuri, port, computername, applicationname);
                if (connectionuri != null)
                {
                    // in the format http(s)://server[:port/applicationname]
                    string[] constrsplit = connectionStr.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }

                IWSManSession m_session = CreateSessionObject(m_wsmanObject, authentication, sessionoption, credential, connectionStr, certificateThumbprint, usessl);
                m_session.Identify(0);
                string key = computername ?? "localhost";

                AddtoDictionary(key, m_session);
            }
            catch (IndexOutOfRangeException)
            {
                AssertError(_resourceMgr.GetString("NotProperURI"), false, connectionuri);
            }
            catch (Exception ex)
            {
                AssertError(ex.Message, false, computername);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    AssertError(m_wsmanObject.Error, true, computername);
                }
            }
        }

        /// <summary>
        /// Verifies all the registry keys are set as expected. In case of failure .. try ecery second for 60 seconds before returning false.
        /// </summary>
        /// <param name="AllowFreshCredentialsValueShouldBePresent">True if trying to Enable CredSSP.</param>
        /// <param name="DelegateComputer">Names of the delegate computer.</param>
        /// <param name="applicationname">Name of the application.</param>
        /// <returns>True if valid.</returns>
        internal bool ValidateCreadSSPRegistryRetry(bool AllowFreshCredentialsValueShouldBePresent, string[] DelegateComputer, string applicationname)
        {
            for (int i = 0; i < 60; i++)
            {
                if (!ValidateCredSSPRegistry(AllowFreshCredentialsValueShouldBePresent, DelegateComputer, applicationname))
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        internal bool ValidateCredSSPRegistry(bool AllowFreshCredentialsValueShouldBePresent, string[] DelegateComputer, string applicationname)
        {
            System.IntPtr NakedGPOCriticalSection = GpoNativeApi.EnterCriticalPolicySection(true);

            try
            {
                RegistryKey rGPOLocalMachineKey = Registry.LocalMachine.OpenSubKey(
                    Registry_Path_Credentials_Delegation + @"\CredentialsDelegation",
                    RegistryKeyPermissionCheck.ReadWriteSubTree,
                    System.Security.AccessControl.RegistryRights.FullControl);

                if (rGPOLocalMachineKey != null)
                {
                    rGPOLocalMachineKey = rGPOLocalMachineKey.OpenSubKey(
                        Key_Allow_Fresh_Credentials,
                        RegistryKeyPermissionCheck.ReadWriteSubTree,
                        System.Security.AccessControl.RegistryRights.FullControl);
                    if (rGPOLocalMachineKey == null)
                    {
                        return !AllowFreshCredentialsValueShouldBePresent;
                    }

                    string[] valuenames = rGPOLocalMachineKey.GetValueNames();
                    if (valuenames.Length == 0)
                    {
                        return !AllowFreshCredentialsValueShouldBePresent;
                    }

                    List<string> RegValues = new List<string>();
                    foreach (string value in valuenames)
                    {
                        object keyvalue = rGPOLocalMachineKey.GetValue(value);

                        if (keyvalue != null && keyvalue.ToString().StartsWith(applicationname, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!AllowFreshCredentialsValueShouldBePresent)
                            {
                                // If calling Disable-CredSSP .. no value should start with "applicationName" regardless of the computer.
                                return false;
                            }

                            RegValues.Add(keyvalue.ToString());
                        }
                    }

                    if (AllowFreshCredentialsValueShouldBePresent)
                    {
                        // For all the keys that starts with "applicationName" make sure the delegated computer is listed.
                        foreach (string comp in DelegateComputer)
                        {
                            if (!RegValues.Contains(applicationname + "/" + comp))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            finally
            {
                bool GPOReleaseCriticalSection = GpoNativeApi.LeaveCriticalPolicySection(NakedGPOCriticalSection);
            }

            return true;
        }
    }

    internal static class WSManResourceLoader
    {
        internal static void LoadResourceData()
        {
            try
            {
                string winDir = System.Environment.ExpandEnvironmentVariables("%Windir%");
                uint lcid = checked((uint)CultureInfo.CurrentUICulture.LCID);
                string filepath = string.Create(CultureInfo.CurrentCulture, $@"{winDir}\System32\Winrm\0{lcid:x2}\winrm.ini");

                if (File.Exists(filepath))
                {
                    FileStream _fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                    StreamReader _sr = new StreamReader(_fs);
                    while (!_sr.EndOfStream)
                    {
                        string Line = _sr.ReadLine();
                        if (Line.Contains('='))
                        {
                            string[] arr = Line.Split('=', count: 2);
                            if (!ResourceValueCache.ContainsKey(arr[0].Trim()))
                            {
                                string value = arr[1].Trim('"');
                                ResourceValueCache.Add(arr[0].Trim(), value.Trim());
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the resource value from WinRm.ini
        /// from %windir%\system32\winrm\[Hexadecimal Language Folder]\winrm.ini.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        internal static string GetResourceString(string Key)
        {
            // Checks whether resource values already loaded and loads.
            if (ResourceValueCache.Count == 0)
            {
                LoadResourceData();
            }

            string value = string.Empty;
            if (ResourceValueCache.ContainsKey(Key.Trim()))
            {
                ResourceValueCache.TryGetValue(Key.Trim(), out value);
            }

            return value.Trim();
        }

        /// <summary>
        /// </summary>
        private static readonly Dictionary<string, string> ResourceValueCache = new Dictionary<string, string>();
    }
}
