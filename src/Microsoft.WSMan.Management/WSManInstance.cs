// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;

using Dbg = System.Management.Automation;

namespace Microsoft.WSMan.Management
{
    #region Get-WSManInstance
    /// <summary>
    /// Executes action on a target object specified by RESOURCE_URI, where
    /// parameters are specified by key value pairs.
    /// eg., Call StartService method on the spooler service
    /// Invoke-WSManAction -Action StartService -ResourceURI wmicimv2/Win32_Service
    /// -SelectorSet {Name=Spooler}
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WSManInstance", DefaultParameterSetName = "GetInstance", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096627")]
    [OutputType(typeof(XmlElement))]
    public class GetWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        #region parameter
        /// <summary>
        /// The following is the definition of the input parameter "ApplicationName".
        /// ApplicationName identifies the remote endpoint.
        /// </summary>
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]
        public string ApplicationName
        {
            get
            {
                return applicationname;
            }

            set
            {
                { applicationname = value; }
            }
        }

        private string applicationname = null;

        /// <summary>
        /// The following is the definition of the input parameter "BasePropertiesOnly".
        /// Enumerate only those properties that are part of the base class
        /// specification in the Resource URI. When
        /// Shallow is specified then this flag has no effect.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]
        [Alias("UBPO", "Base")]
        public SwitchParameter BasePropertiesOnly
        {
            get
            {
                return basepropertiesonly;
            }

            set
            {
                { basepropertiesonly = value; }
            }
        }

        private SwitchParameter basepropertiesonly;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]
        [Alias("CN")]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        /// <summary>
        /// The following is the definition of the input parameter "ConnectionURI".
        /// Specifies the transport, server, port, and Prefix, needed to connect to the
        /// remote machine. The format of this string is:
        /// transport://server:port/Prefix.
        /// </summary>
        [Parameter(
                  ParameterSetName = "GetInstance")]
        [Parameter(
                  ParameterSetName = "Enumerate")]
        [Alias("CURI", "CU")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get
            {
                return connectionuri;
            }

            set
            {
                { connectionuri = value; }
            }
        }

        private Uri connectionuri;

        /// <summary>
        /// The following is the definition of the input parameter "Dialect".
        /// Defines the dialect for the filter predicate.
        /// </summary>
        [Parameter]
        public Uri Dialect
        {
            get
            {
                return dialect;
            }

            set
            {
                { dialect = value; }
            }
        }

        private Uri dialect;

        /// <summary>
        /// The following is the definition of the input parameter "Enumerate".
        /// Switch indicates list all instances of a management resource. Equivalent to
        /// WSManagement Enumerate.
        /// </summary>
        [Parameter(Mandatory = true,
                  ParameterSetName = "Enumerate")]
        public SwitchParameter Enumerate
        {
            get
            {
                return enumerate;
            }

            set
            {
                { enumerate = value; }
            }
        }

        private SwitchParameter enumerate;

        /// <summary>
        /// The following is the definition of the input parameter "Filter".
        /// Indicates the filter expression for the enumeration.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]
        [ValidateNotNullOrEmpty]
        public string Filter
        {
            get
            {
                return filter;
            }

            set
            {
                { filter = value; }
            }
        }

        private string filter;

        /// <summary>
        /// The following is the definition of the input parameter "Fragment".
        /// Specifies a section inside the instance that is to be updated or retrieved
        /// for the given operation.
        /// </summary>
        [Parameter(ParameterSetName = "GetInstance")]
        [ValidateNotNullOrEmpty]
        public string Fragment
        {
            get
            {
                return fragment;
            }

            set
            {
                { fragment = value; }
            }
        }

        private string fragment;

        /// <summary>
        /// The following is the definition of the input parameter "OptionSet".
        /// OptionSet is a hashtable and is used to pass a set of switches to the
        /// service to modify or refine the nature of the request.
        /// </summary>
        [Parameter(ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("OS")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable OptionSet
        {
            get
            {
                return optionset;
            }

            set
            {
                { optionset = value; }
            }
        }

        private Hashtable optionset;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]
        [Parameter(ParameterSetName = "GetInstance")]
        public int Port
        {
            get
            {
                return port;
            }

            set
            {
                { port = value; }
            }
        }

        private int port = 0;

        /// <summary>
        /// The following is the definition of the input parameter "Associations".
        /// Associations indicates retrieval of association instances as opposed to
        /// associated instances. This can only be used when specifying the Dialect as
        /// Association.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]
        public SwitchParameter Associations
        {
            get
            {
                return associations;
            }

            set
            {
                { associations = value; }
            }
        }

        private SwitchParameter associations;

        /// <summary>
        /// The following is the definition of the input parameter "ResourceURI".
        /// URI of the resource class/instance representation.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Alias("RURI")]
        public Uri ResourceURI
        {
            get
            {
                return resourceuri;
            }

            set
            {
                { resourceuri = value; }
            }
        }

        private Uri resourceuri;

        /// <summary>
        /// The following is the definition of the input parameter "ReturnType".
        /// Indicates the type of data returned. Possible options are 'Object', 'EPR',
        /// and 'ObjectAndEPR'. Default is Object.
        /// If Object is specified or if this parameter is absent then only the objects
        /// are returned
        /// If EPR is specified then only the EPRs of the objects
        /// are returned. EPRs contain information about the Resource URI and selectors
        /// for the instance
        /// If ObjectAndEPR is specified, then both the object and the associated EPRs
        /// are returned.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]

        [ValidateNotNullOrEmpty]
        [ValidateSet(new string[] { "object", "epr", "objectandepr" })]
        [Alias("RT")]
        public string ReturnType
        {
            get
            {
                return returntype;
            }

            set
            {
                { returntype = value; }
            }
        }

        private string returntype = "object";

        /// <summary>
        /// The following is the definition of the input parameter "SelectorSet".
        /// SelectorSet is a hash table which helps in identify an instance of the
        /// management resource if there are more than 1 instance of the resource
        /// class.
        /// </summary>
        [Parameter(
                   ParameterSetName = "GetInstance")]

        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable SelectorSet
        {
            get
            {
                return selectorset;
            }

            set
            {
                { selectorset = value; }
            }
        }

        private Hashtable selectorset;

        /// <summary>
        /// The following is the definition of the input parameter "SessionOption".
        /// Defines a set of extended options for the WSMan session.  This can be
        /// created by using the cmdlet New-WSManSessionOption.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("SO")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public SessionOption SessionOption
        {
            get
            {
                return sessionoption;
            }

            set
            {
                { sessionoption = value; }
            }
        }

        private SessionOption sessionoption;

        /// <summary>
        /// The following is the definition of the input parameter "Shallow".
        /// Enumerate only instances of the base class specified in the resource URI. If
        /// this flag is not specified, instances of the base class specified in the URI
        /// and all its derived classes are returned.
        /// </summary>
        [Parameter(ParameterSetName = "Enumerate")]

        public SwitchParameter Shallow
        {
            get
            {
                return shallow;
            }

            set
            {
                { shallow = value; }
            }
        }

        private SwitchParameter shallow;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Uses the Secure Sockets Layer (SSL) protocol to establish a connection to
        /// the remote computer. If SSL is not available on the port specified by the
        /// Port parameter, the command fails.
        /// </summary>
        [Parameter(ParameterSetName = "GetInstance")]
        [Parameter(ParameterSetName = "Enumerate")]

        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("SSL")]
        public SwitchParameter UseSSL
        {
            get
            {
                return usessl;
            }

            set
            {
                { usessl = value; }
            }
        }

        private SwitchParameter usessl;

        #endregion parameter

        #region private
        private WSManHelper helper;

        private string GetFilter()
        {
            string name;
            string value;
            string[] Split = filter.Trim().Split(new char[] { '=', ';' });
            if ((Split.Length) % 2 != 0)
            {
                // mismatched property name/value pair
                return null;
            }

            filter = "<wsman:SelectorSet xmlns:wsman='http://schemas.dmtf.org/wbem/wsman/1/wsman.xsd'>";
            for (int i = 0; i < Split.Length; i += 2)
            {
                value = Split[i + 1].Substring(1, Split[i + 1].Length - 2);
                name = Split[i];
                filter = filter + "<wsman:Selector Name='" + name + "'>" + value + "</wsman:Selector>";
            }

            filter += "</wsman:SelectorSet>";
            return (filter);
        }

        private void ReturnEnumeration(IWSManEx wsmanObject, IWSManResourceLocator wsmanResourceLocator, IWSManSession wsmanSession)
        {
            string fragment;
            try
            {
                int flags = 0;
                IWSManEnumerator obj;
                if (returntype != null)
                {
                    if (returntype.Equals("object", StringComparison.OrdinalIgnoreCase))
                    {
                        flags = wsmanObject.EnumerationFlagReturnObject();
                    }
                    else if (returntype.Equals("epr", StringComparison.OrdinalIgnoreCase))
                    {
                        flags = wsmanObject.EnumerationFlagReturnEPR();
                    }
                    else
                    {
                        flags = wsmanObject.EnumerationFlagReturnObjectAndEPR();
                    }
                }

                if (shallow)
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyShallow();
                }
                else if (basepropertiesonly)
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyDeepBasePropsOnly();
                }
                else
                {
                    flags |= wsmanObject.EnumerationFlagHierarchyDeep();
                }

                if (dialect != null && filter != null)
                {
                    if (dialect.ToString().Equals(helper.ALIAS_WQL, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_WQL_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        fragment = helper.URI_WQL_DIALECT;
                        dialect = new Uri(fragment);
                    }
                    else if (dialect.ToString().Equals(helper.ALIAS_ASSOCIATION, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_ASSOCIATION_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        if (associations)
                        {
                            flags |= wsmanObject.EnumerationFlagAssociationInstance();
                        }
                        else
                        {
                            flags |= wsmanObject.EnumerationFlagAssociatedInstance();
                        }

                        fragment = helper.URI_ASSOCIATION_DIALECT;
                        dialect = new Uri(fragment);
                    }
                    else if (dialect.ToString().Equals(helper.ALIAS_SELECTOR, StringComparison.OrdinalIgnoreCase) || dialect.ToString().Equals(helper.URI_SELECTOR_DIALECT, StringComparison.OrdinalIgnoreCase))
                    {
                        filter = GetFilter();
                        fragment = helper.URI_SELECTOR_DIALECT;
                        dialect = new Uri(fragment);
                    }

                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, dialect.ToString(), flags);
                }
                else if (filter != null)
                {
                    fragment = helper.URI_WQL_DIALECT;
                    dialect = new Uri(fragment);
                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, dialect.ToString(), flags);
                }
                else
                {
                    obj = (IWSManEnumerator)wsmanSession.Enumerate(wsmanResourceLocator, filter, null, flags);
                }
                while (!obj.AtEndOfStream)
                {
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(obj.ReadItem());
                    WriteObject(xmldoc.FirstChild);
                }
            }
            catch (Exception ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "Exception", ErrorCategory.InvalidOperation, null);
                WriteError(er);
            }
        }
        #endregion private
        #region override
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            IWSManSession m_session = null;
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper = new WSManHelper(this);
            helper.WSManOp = "Get";
            string connectionStr = null;
            connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
            if (connectionuri != null)
            {
                try
                {
                    // in the format http(s)://server[:port/applicationname]
                    string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }

            try
            {
                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, fragment, dialect, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsSpecified);

                if (!enumerate)
                {
                    XmlDocument xmldoc = new XmlDocument();
                    try
                    {
                        xmldoc.LoadXml(m_session.Get(m_resource, 0));
                    }
                    catch (XmlException ex)
                    {
                        helper.AssertError(ex.Message, false, computername);
                    }

                    if (!string.IsNullOrEmpty(fragment))
                    {
                        WriteObject(xmldoc.FirstChild.LocalName + "=" + xmldoc.FirstChild.InnerText);
                    }
                    else
                    {
                        WriteObject(xmldoc.FirstChild);
                    }
                }
                else
                {
                    try
                    {
                        ReturnEnumeration(m_wsmanObject, m_resource, m_session);
                    }
                    catch (Exception ex)
                    {
                        helper.AssertError(ex.Message, false, computername);
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (m_session != null)
                    Dispose(m_session);
            }
        }
        #endregion override
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

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            helper.CleanUp();
        }
    }
    #endregion

    #region Set-WsManInstance

    /// <summary>
    /// Executes action on a target object specified by RESOURCE_URI, where
    /// parameters are specified by key value pairs.
    /// eg., Call StartService method on the spooler service
    /// Set-WSManInstance -Action StartService -ResourceURI wmicimv2/Win32_Service
    /// -SelectorSet {Name=Spooler}
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096937")]
    [OutputType(typeof(XmlElement), typeof(string))]
    public class SetWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        #region Parameters
        /// <summary>
        /// The following is the definition of the input parameter "ApplicationName".
        /// ApplicationName identifies the remote endpoint.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [Alias("cn")]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        /// <summary>
        /// The following is the definition of the input parameter "ConnectionURI".
        /// Specifies the transport, server, port, and ApplicationName of the new
        /// runspace. The format of this string is:
        /// transport://server:port/ApplicationName.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        /// <summary>
        /// The following is the definition of the input parameter "Dialect".
        /// Defines the dialect for the filter predicate.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public Uri Dialect
        {
            get { return dialect; }

            set { dialect = value; }
        }

        private Uri dialect;

        /// <summary>
        /// The following is the definition of the input parameter "FilePath".
        /// Updates the management resource specified by the ResourceURI and SelectorSet
        /// via this input file.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Path")]
        [ValidateNotNullOrEmpty]
        public string FilePath
        {
            get { return filepath; }

            set { filepath = value; }
        }

        private string filepath;

        /// <summary>
        /// The following is the definition of the input parameter "Fragment".
        /// Specifies a section inside the instance that is to be updated or retrieved
        /// for the given operation.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public string Fragment
        {
            get { return fragment; }

            set { fragment = value; }
        }

        private string fragment;

        /// <summary>
        /// The following is the definition of the input parameter "OptionSet".
        /// OptionSet is a hahs table which help modify or refine the nature of the
        /// request. These are similar to switches used in command line shells in that
        /// they are service-specific.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        [ValidateNotNullOrEmpty]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        /// <summary>
        /// The following is the definition of the input parameter "ResourceURI".
        /// URI of the resource class/instance representation.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Resourceuri")]

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("ruri")]
        [ValidateNotNullOrEmpty]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        /// <summary>
        /// The following is the definition of the input parameter "SelectorSet".
        /// SelectorSet is a hash table which helps in identify an instance of the
        /// management resource if there are more than 1 instance of the resource
        /// class.
        /// </summary>
        [Parameter(Position = 1,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        /// <summary>
        /// The following is the definition of the input parameter "SessionOption".
        /// Defines a set of extended options for the WSMan session. This can be created
        /// by using the cmdlet New-WSManSessionOption.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
        [ValidateNotNullOrEmpty]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Uses the Secure Sockets Layer (SSL) protocol to establish a connection to
        /// the remote computer. If SSL is not available on the port specified by the
        /// Port parameter, the command fails.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("ssl")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        /// <summary>
        /// The following is the definition of the input parameter "ValueSet".
        /// ValueSet is a hash table which helps to modify resource represented by the
        /// ResourceURI and SelectorSet.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable ValueSet
        {
            get { return valueset; }

            set { valueset = value; }
        }

        private Hashtable valueset;

        #endregion

        private WSManHelper helper;
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper = new WSManHelper(this);
            helper.WSManOp = "set";
            IWSManSession m_session = null;

            if (dialect != null)
            {
                if (dialect.ToString().Equals(helper.ALIAS_WQL, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_WQL_DIALECT);
                if (dialect.ToString().Equals(helper.ALIAS_SELECTOR, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_SELECTOR_DIALECT);
                if (dialect.ToString().Equals(helper.ALIAS_ASSOCIATION, StringComparison.OrdinalIgnoreCase))
                    dialect = new Uri(helper.URI_ASSOCIATION_DIALECT);
            }

            try
            {
                string connectionStr = string.Empty;
                connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
                if (connectionuri != null)
                {
                    try
                    {
                        // in the format http(s)://server[:port/applicationname]
                        string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                        string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                        computername = constrsplit1[1].Trim();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                    }
                }

                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, fragment, dialect, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsSpecified);
                string rootNode = helper.GetRootNodeName(helper.WSManOp, m_resource.ResourceUri, null);
                string input = helper.ProcessInput(m_wsmanObject, filepath, helper.WSManOp, rootNode, valueset, m_resource, m_session);

                XmlDocument xmldoc = new XmlDocument();
                try
                {
                    xmldoc.LoadXml(m_session.Put(m_resource, input, 0));
                }
                catch (XmlException ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }

                if (!string.IsNullOrEmpty(fragment))
                {
                    if (xmldoc.DocumentElement.ChildNodes.Count > 0)
                    {
                        foreach (XmlNode node in xmldoc.DocumentElement.ChildNodes)
                        {
                            if (node.Name.Equals(fragment, StringComparison.OrdinalIgnoreCase))
                                WriteObject(node.Name + " = " + node.InnerText);
                        }
                    }
                }
                else
                    WriteObject(xmldoc.DocumentElement);
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (m_session != null)
                    Dispose(m_session);
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

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            helper.CleanUp();
        }
    }

    #endregion

    #region Remove-WsManInstance

    /// <summary>
    /// Executes action on a target object specified by RESOURCE_URI, where
    /// parameters are specified by key value pairs.
    /// eg., Call StartService method on the spooler service
    /// Set-WSManInstance -Action StartService -ResourceURI wmicimv2/Win32_Service
    /// -SelectorSet {Name=Spooler}
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096721")]
    public class RemoveWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        #region Parameters
        /// <summary>
        /// The following is the definition of the input parameter "ApplicationName".
        /// ApplicationName identifies the remote endpoint.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [Alias("cn")]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        /// <summary>
        /// The following is the definition of the input parameter "ConnectionURI".
        /// Specifies the transport, server, port, and ApplicationName of the new
        /// runspace. The format of this string is:
        /// transport://server:port/ApplicationName.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        /// <summary>
        /// The following is the definition of the input parameter "OptionSet".
        /// OptionSet is a hahs table which help modify or refine the nature of the
        /// request. These are similar to switches used in command line shells in that
        /// they are service-specific.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        [ValidateNotNullOrEmpty]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        /// <summary>
        /// The following is the definition of the input parameter "ResourceURI".
        /// URI of the resource class/instance representation.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Resourceuri")]

        [Parameter(Mandatory = true, Position = 0)]
        [Alias("ruri")]
        [ValidateNotNullOrEmpty]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        /// <summary>
        /// The following is the definition of the input parameter "SelectorSet".
        /// SelectorSet is a hash table which helps in identify an instance of the
        /// management resource if there are more than 1 instance of the resource
        /// class.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [ValidateNotNullOrEmpty]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        /// <summary>
        /// The following is the definition of the input parameter "SessionOption".
        /// Defines a set of extended options for the WSMan session. This can be created
        /// by using the cmdlet New-WSManSessionOption.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
        [ValidateNotNullOrEmpty]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Uses the Secure Sockets Layer (SSL) protocol to establish a connection to
        /// the remote computer. If SSL is not available on the port specified by the
        /// Port parameter, the command fails.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        [Alias("ssl")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        #endregion

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            WSManHelper helper = new WSManHelper(this);
            IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
            helper.WSManOp = "remove";
            IWSManSession m_session = null;
            try
            {
                string connectionStr = string.Empty;
                connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
                if (connectionuri != null)
                {
                    try
                    {
                        // in the format http(s)://server[:port/applicationname]
                        string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                        string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                        computername = constrsplit1[1].Trim();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                    }
                }

                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, null, null, m_wsmanObject, resourceuri);
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsSpecified);
                string ResourceURI = helper.GetURIWithFilter(resourceuri.ToString(), null, selectorset, helper.WSManOp);
                try
                {
                    ((IWSManSession)m_session).Delete(ResourceURI, 0);
                }
                catch (Exception ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
                }

                if (m_session != null)
                    Dispose(m_session);
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

    #endregion

    #region New-WsManInstance
    /// <summary>
    /// Creates an instance of a management resource identified by the resource URI
    /// using specified ValueSet or input File.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WSManInstance", DefaultParameterSetName = "ComputerName", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096933")]
    [OutputType(typeof(XmlElement))]
    public class NewWSManInstanceCommand : AuthenticatingWSManCommand, IDisposable
    {
        /// <summary>
        /// The following is the definition of the input parameter "ApplicationName".
        /// ApplicationName identifies the remote endpoint.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        public string ApplicationName
        {
            get { return applicationname; }

            set { applicationname = value; }
        }

        private string applicationname = null;

        /// <summary>
        /// The following is the definition of the input parameter "ComputerName".
        /// Executes the management operation on the specified computer(s). The default
        /// is the local computer. Type the fully qualified domain name, NETBIOS name or
        /// IP address to indicate the remote host(s)
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [Alias("cn")]
        public string ComputerName
        {
            get
            {
                return computername;
            }

            set
            {
                computername = value;
                if ((string.IsNullOrEmpty(computername)) || (computername.Equals(".", StringComparison.OrdinalIgnoreCase)))
                {
                    computername = "localhost";
                }
            }
        }

        private string computername = null;

        /// <summary>
        /// The following is the definition of the input parameter "ConnectionURI".
        /// Specifies the transport, server, port, and ApplicationName of the new
        /// runspace. The format of this string is:
        /// transport://server:port/ApplicationName.
        /// </summary>
        [Parameter(ParameterSetName = "URI")]
        [ValidateNotNullOrEmpty]
        [Alias("CURI", "CU")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ConnectionURI
        {
            get { return connectionuri; }

            set { connectionuri = value; }
        }

        private Uri connectionuri;

        /// <summary>
        /// The following is the definition of the input parameter "FilePath".
        /// Updates the management resource specified by the ResourceURI and SelectorSet
        /// via this input file.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("Path")]
        public string FilePath
        {
            get { return filepath; }

            set { filepath = value; }
        }

        private string filepath;

        /// <summary>
        /// The following is the definition of the input parameter "OptionSet".
        /// OptionSet is a hash table and is used to pass a set of switches to the
        /// service to modify or refine the nature of the request.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("os")]
        public Hashtable OptionSet
        {
            get { return optionset; }

            set { optionset = value; }
        }

        private Hashtable optionset;

        /// <summary>
        /// The following is the definition of the input parameter "Port".
        /// Specifies the port to be used when connecting to the ws management service.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [ValidateNotNullOrEmpty]
        [ValidateRange(1, int.MaxValue)]
        public int Port
        {
            get { return port; }

            set { port = value; }
        }

        private int port = 0;

        /// <summary>
        /// The following is the definition of the input parameter "ResourceURI".
        /// URI of the resource class/instance representation.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [Alias("ruri")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI")]
        public Uri ResourceURI
        {
            get { return resourceuri; }

            set { resourceuri = value; }
        }

        private Uri resourceuri;

        /// <summary>
        /// The following is the definition of the input parameter "SelectorSet".
        /// SelectorSet is a hash table which helps in identify an instance of the
        /// management resource if there are more than 1 instance of the resource
        /// class.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1,
                   ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable SelectorSet
        {
            get { return selectorset; }

            set { selectorset = value; }
        }

        private Hashtable selectorset;

        /// <summary>
        /// The following is the definition of the input parameter "SessionOption".
        /// Defines a set of extended options for the WSMan session.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [Alias("so")]
        public SessionOption SessionOption
        {
            get { return sessionoption; }

            set { sessionoption = value; }
        }

        private SessionOption sessionoption;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Uses the Secure Sockets Layer (SSL) protocol to establish a connection to
        /// the remote computer. If SSL is not available on the port specified by the
        /// Port parameter, the command fails.
        /// </summary>
        [Parameter(ParameterSetName = "ComputerName")]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SSL")]
        public SwitchParameter UseSSL
        {
            get { return usessl; }

            set { usessl = value; }
        }

        private SwitchParameter usessl;

        /// <summary>
        /// The following is the definition of the input parameter "ValueSet".
        /// ValueSet is a hash table which helps to modify resource represented by the
        /// ResourceURI and SelectorSet.
        /// </summary>
        [Parameter]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable ValueSet
        {
            get { return valueset; }

            set { valueset = value; }
        }

        private Hashtable valueset;

        private WSManHelper helper;
        private readonly IWSManEx m_wsmanObject = (IWSManEx)new WSManClass();
        private IWSManSession m_session = null;
        private string connectionStr = string.Empty;

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            helper = new WSManHelper(this);
            helper.WSManOp = "new";
            connectionStr = helper.CreateConnectionString(connectionuri, port, computername, applicationname);
            if (connectionuri != null)
            {
                try
                {
                    // in the format http(s)://server[:port/applicationname]
                    string[] constrsplit = connectionuri.OriginalString.Split(":" + port + "/" + applicationname, StringSplitOptions.None);
                    string[] constrsplit1 = constrsplit[0].Split("//", StringSplitOptions.None);
                    computername = constrsplit1[1].Trim();
                }
                catch (IndexOutOfRangeException)
                {
                    helper.AssertError(helper.GetResourceMsgFromResourcetext("NotProperURI"), false, connectionuri);
                }
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                IWSManResourceLocator m_resource = helper.InitializeResourceLocator(optionset, selectorset, null, null, m_wsmanObject, resourceuri);
                // create the session object
                m_session = helper.CreateSessionObject(m_wsmanObject, Authentication, sessionoption, Credential, connectionStr, CertificateThumbprint, usessl.IsSpecified);
                string rootNode = helper.GetRootNodeName(helper.WSManOp, m_resource.ResourceUri, null);
                string input = helper.ProcessInput(m_wsmanObject, filepath, helper.WSManOp, rootNode, valueset, m_resource, m_session);

                try
                {
                    string resultXml = m_session.Create(m_resource, input, 0);
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.LoadXml(resultXml);
                    WriteObject(xmldoc.DocumentElement);
                }
                catch (Exception ex)
                {
                    helper.AssertError(ex.Message, false, computername);
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(m_wsmanObject.Error))
                {
                    helper.AssertError(m_wsmanObject.Error, true, resourceuri);
                }

                if (!string.IsNullOrEmpty(m_session.Error))
                {
                    helper.AssertError(m_session.Error, true, resourceuri);
                }

                if (m_session != null)
                {
                    Dispose(m_session);
                }
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

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            helper.CleanUp();
        }
    }

    #endregion
}
