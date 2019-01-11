// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using Microsoft.Management.Infrastructure.Options;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// Define Protocol type.
    /// </summary>
    public enum ProtocolType
    {
        Default,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Dcom,
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Wsman
    };

    /// <summary>
    /// The Cmdlet allows the IT Pro to create a CimSessionOptions object that she/he
    /// can subsequently use to create one or more CimSession connections. The
    /// options object holds the CIM Session information that is less commonly set
    /// and used by the IT Pro, and most commonly defaulted.
    ///
    /// The Cmdlet has two parameter sets, one for WMMan options and one for DCOM
    /// options. Depending on the arguments the Cmdlet will return an instance of
    /// DComSessionOptions or WSManSessionOptions, which derive from
    /// CimSessionOptions.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "CimSessionOption", DefaultParameterSetName = ProtocolNameParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=227969")]
    [OutputType(typeof(CimSessionOptions))]
    public sealed class NewCimSessionOptionCommand : CimBaseCommand
    {
        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public NewCimSessionOptionCommand()
            : base(parameters, parameterSets)
        {
            DebugHelper.WriteLogEx();
        }

        #endregion

        #region cmdlet parameters

        /// <summary>
        /// The following is the definition of the input parameter "NoEncryption".
        /// Switch indicating if WSMan can use no encryption in the given CimSession (there are also global client and server WSMan settings - AllowUnencrypted).
        /// </summary>
        [Parameter(ParameterSetName = WSManParameterSet)]
        public SwitchParameter NoEncryption
        {
            get { return noEncryption; }

            set
            {
                noEncryption = value;
                noEncryptionSet = true;
                base.SetParameter(value, nameNoEncryption);
            }
        }

        private SwitchParameter noEncryption;
        private bool noEncryptionSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "CertificateCACheck".
        /// Switch indicating if Certificate Authority should be validated.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public SwitchParameter SkipCACheck
        {
            get { return skipCACheck; }

            set
            {
                skipCACheck = value;
                skipCACheckSet = true;
                base.SetParameter(value, nameSkipCACheck);
            }
        }

        private SwitchParameter skipCACheck;
        private bool skipCACheckSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "CertificateCNCheck".
        /// Switch indicating if Certificate Name should be validated.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public SwitchParameter SkipCNCheck
        {
            get { return skipCNCheck; }

            set
            {
                skipCNCheck = value;
                skipCNCheckSet = true;
                base.SetParameter(value, nameSkipCNCheck);
            }
        }

        private SwitchParameter skipCNCheck;
        private bool skipCNCheckSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "CertRevocationCheck".
        /// Switch indicating if certificate should be revoked.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public SwitchParameter SkipRevocationCheck
        {
            get { return skipRevocationCheck; }

            set
            {
                skipRevocationCheck = value;
                skipRevocationCheckSet = true;
                base.SetParameter(value, nameSkipRevocationCheck);
            }
        }

        private SwitchParameter skipRevocationCheck;
        private bool skipRevocationCheckSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "EncodePortInServicePrincipalName".
        /// Switch indicating if to encode Port In Service Principal Name.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public SwitchParameter EncodePortInServicePrincipalName
        {
            get { return encodeportinserviceprincipalname; }

            set
            {
                encodeportinserviceprincipalname = value;
                encodeportinserviceprincipalnameSet = true;
                base.SetParameter(value, nameEncodePortInServicePrincipalName);
            }
        }

        private SwitchParameter encodeportinserviceprincipalname;
        private bool encodeportinserviceprincipalnameSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "Encoding".
        /// Defined the message encoding.
        /// The allowed encodings are { Default | Utf8 | Utf16 }. The default value
        /// should be Utf8.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public PacketEncoding Encoding
        {
            get { return encoding; }

            set
            {
                encoding = value;
                encodingSet = true;
                base.SetParameter(value, nameEncoding);
            }
        }

        private PacketEncoding encoding;
        private bool encodingSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "HttpPrefix".
        /// This is the HTTP URL on the server on which the WSMan service is listening.
        /// In most cases it is /wsman, which is the default.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public Uri HttpPrefix
        {
            get { return httpprefix; }

            set
            {
                httpprefix = value;
                base.SetParameter(value, nameHttpPrefix);
            }
        }

        private Uri httpprefix;

        /// <summary>
        /// The following is the definition of the input parameter "MaxEnvelopeSizeKB".
        /// Sets the limit to the maximum size of the WSMan message envelope.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public UInt32 MaxEnvelopeSizeKB
        {
            get { return maxenvelopesizekb; }

            set
            {
                maxenvelopesizekb = value;
                maxenvelopesizekbSet = true;
                base.SetParameter(value, nameMaxEnvelopeSizeKB);
            }
        }

        private UInt32 maxenvelopesizekb;
        private bool maxenvelopesizekbSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyAuthentication".
        /// Which proxy authentication types to use: Allowed set is:
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public PasswordAuthenticationMechanism ProxyAuthentication
        {
            get { return proxyAuthentication; }

            set
            {
                proxyAuthentication = value;
                proxyauthenticationSet = true;
                base.SetParameter(value, nameProxyAuthentication);
            }
        }

        private PasswordAuthenticationMechanism proxyAuthentication;
        private bool proxyauthenticationSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyCertificateThumbprint".
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public string ProxyCertificateThumbprint
        {
            get { return proxycertificatethumbprint; }

            set
            {
                proxycertificatethumbprint = value;
                base.SetParameter(value, nameProxyCertificateThumbprint);
            }
        }

        private string proxycertificatethumbprint;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyCredential".
        /// Ps Credential used by the proxy server when required by the server.
        /// </summary>
        [Parameter(ParameterSetName = WSManParameterSet)]
        [Credential()]
        public PSCredential ProxyCredential
        {
            get { return proxycredential; }

            set
            {
                proxycredential = value;
                base.SetParameter(value, nameProxyCredential);
            }
        }

        private PSCredential proxycredential;

        /// <summary>
        /// The following is the definition of the input parameter "ProxyType".
        /// Which proxy type to use: Valid set is:
        ///  { InternetExplorer | WinHttp | Auto | None }
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public ProxyType ProxyType
        {
            get { return proxytype; }

            set
            {
                proxytype = value;
                proxytypeSet = true;
                base.SetParameter(value, nameProxyType);
            }
        }

        private ProxyType proxytype;
        private bool proxytypeSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "UseSSL".
        /// Switch indicating if Secure Sockets Layer connection should be used.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true,
                   ParameterSetName = WSManParameterSet)]
        public SwitchParameter UseSsl
        {
            get { return usessl; }

            set
            {
                usessl = value;
                usesslSet = true;
                base.SetParameter(value, nameUseSsl);
            }
        }

        private SwitchParameter usessl;
        private bool usesslSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "Impersonation".
        /// Used to select if, and if so what kind of, impersonation should be used.
        /// Applies only to the DCOM channel.
        /// </summary>
        [Parameter(ParameterSetName = DcomParameterSet)]
        public ImpersonationType Impersonation
        {
            get { return impersonation; }

            set
            {
                impersonation = value;
                impersonationSet = true;
                base.SetParameter(value, nameImpersonation);
            }
        }

        private ImpersonationType impersonation;
        private bool impersonationSet = false;

        /// <summary>
        /// The following is the definition of the input parameter "PacketIntegrity".
        /// Switch indicating if the package integrity in DCOM connections should be
        /// checked/enforced.
        /// </summary>
        [Parameter(ParameterSetName = DcomParameterSet)]
        public SwitchParameter PacketIntegrity
        {
            get { return packetintegrity; }

            set
            {
                packetintegrity = value;
                packetintegritySet = true;
                base.SetParameter(value, namePacketIntegrity);
            }
        }

        private SwitchParameter packetintegrity;
        private bool packetintegritySet = false;

        /// <summary>
        /// The following is the definition of the input parameter "PacketPrivacy".
        /// Switch indicating if packet privacy of the packets in DCOM communications
        /// should be checked/enforced.
        /// </summary>
        [Parameter(ParameterSetName = DcomParameterSet)]
        public SwitchParameter PacketPrivacy
        {
            get { return packetprivacy; }

            set
            {
                packetprivacy = value;
                packetprivacySet = true;
                base.SetParameter(value, namePacketPrivacy);
            }
        }

        private SwitchParameter packetprivacy;
        private bool packetprivacySet = false;

        /// <summary>
        /// The following is the definition of the input parameter "Protocol".
        /// Switch indicating if to encode Port In Service Principal Name.
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = ProtocolNameParameterSet)]
        public ProtocolType Protocol
        {
            get { return protocol; }

            set
            {
                protocol = value;
                base.SetParameter(value, nameProtocol);
            }
        }

        private ProtocolType protocol;

        /// <summary>
        /// The following is the definition of the input parameter "UICulture".
        /// Specifies the UI Culture to use. i.e. en-us, ar-sa.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public CultureInfo UICulture
        {
            get { return uiculture; }

            set { uiculture = value; }
        }

        private CultureInfo uiculture;

        /// <summary>
        /// The following is the definition of the input parameter "Culture".
        /// Specifies the culture to use. i.e. en-us, ar-sa.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public CultureInfo Culture
        {
            get { return culture; }

            set { culture = value; }
        }

        private CultureInfo culture;

        #endregion

        #region cmdlet processing methods

        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            this.CmdletOperation = new CmdletOperationBase(this);
            this.AtBeginProcess = false;
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            base.CheckParameterSet();
            CimSessionOptions options;
            switch (this.ParameterSetName)
            {
                case WSManParameterSet:
                    {
                        options = CreateWSMANSessionOptions();
                    }

                    break;
                case DcomParameterSet:
                    {
                        options = CreateDComSessionOptions();
                    }

                    break;
                case ProtocolNameParameterSet:
                    switch (Protocol)
                    {
                        case ProtocolType.Dcom:
                            options = CreateDComSessionOptions();
                            break;
                        case ProtocolType.Wsman:
                        default:
                            options = CreateWSMANSessionOptions();
                            break;
                    }

                    break;
                default:
                    return;
            }

            if (options != null)
            {
                if (this.Culture != null)
                {
                    options.Culture = this.Culture;
                }

                if (this.UICulture != null)
                {
                    options.UICulture = this.UICulture;
                }

                this.WriteObject(options);
            }
        }

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
        }

        #endregion

        #region helper functions
        /// <summary>
        /// Create DComSessionOptions.
        /// </summary>
        /// <returns></returns>
        internal DComSessionOptions CreateDComSessionOptions()
        {
            DComSessionOptions dcomoptions = new DComSessionOptions();
            if (this.impersonationSet)
            {
                dcomoptions.Impersonation = this.Impersonation;
                this.impersonationSet = false;
            }
            else
            {
                dcomoptions.Impersonation = ImpersonationType.Impersonate;
            }

            if (this.packetintegritySet)
            {
                dcomoptions.PacketIntegrity = this.packetintegrity;
                this.packetintegritySet = false;
            }
            else
            {
                dcomoptions.PacketIntegrity = true;
            }

            if (this.packetprivacySet)
            {
                dcomoptions.PacketPrivacy = this.PacketPrivacy;
                this.packetprivacySet = false;
            }
            else
            {
                dcomoptions.PacketPrivacy = true;
            }

            return dcomoptions;
        }

        /// <summary>
        /// Create WSMANSessionOptions.
        /// </summary>
        /// <returns></returns>
        internal WSManSessionOptions CreateWSMANSessionOptions()
        {
            WSManSessionOptions wsmanoptions = new WSManSessionOptions();
            if (this.noEncryptionSet)
            {
                wsmanoptions.NoEncryption = true;
                this.noEncryptionSet = false;
            }
            else
            {
                wsmanoptions.NoEncryption = false;
            }

            if (this.skipCACheckSet)
            {
                wsmanoptions.CertCACheck = false;
                this.skipCACheckSet = false;
            }
            else
            {
                wsmanoptions.CertCACheck = true;
            }

            if (this.skipCNCheckSet)
            {
                wsmanoptions.CertCNCheck = false;
                this.skipCNCheckSet = false;
            }
            else
            {
                wsmanoptions.CertCNCheck = true;
            }

            if (this.skipRevocationCheckSet)
            {
                wsmanoptions.CertRevocationCheck = false;
                this.skipRevocationCheckSet = false;
            }
            else
            {
                wsmanoptions.CertRevocationCheck = true;
            }

            if (this.encodeportinserviceprincipalnameSet)
            {
                wsmanoptions.EncodePortInServicePrincipalName = this.EncodePortInServicePrincipalName;
                this.encodeportinserviceprincipalnameSet = false;
            }
            else
            {
                wsmanoptions.EncodePortInServicePrincipalName = false;
            }

            if (this.encodingSet)
            {
                wsmanoptions.PacketEncoding = this.Encoding;
            }
            else
            {
                wsmanoptions.PacketEncoding = PacketEncoding.Utf8;
            }

            if (this.HttpPrefix != null)
            {
                wsmanoptions.HttpUrlPrefix = this.HttpPrefix;
            }

            if (this.maxenvelopesizekbSet)
            {
                wsmanoptions.MaxEnvelopeSize = this.MaxEnvelopeSizeKB;
            }
            else
            {
                wsmanoptions.MaxEnvelopeSize = 0;
            }

            if (!string.IsNullOrWhiteSpace(this.ProxyCertificateThumbprint))
            {
                CimCredential credentials = new CimCredential(CertificateAuthenticationMechanism.Default, this.ProxyCertificateThumbprint);
                wsmanoptions.AddProxyCredentials(credentials);
            }

            if (this.proxyauthenticationSet)
            {
                this.proxyauthenticationSet = false;
                DebugHelper.WriteLogEx("create credential", 1);
                CimCredential credentials = CreateCimCredentials(this.ProxyCredential, this.ProxyAuthentication, @"New-CimSessionOption", @"ProxyAuthentication");
                if (credentials != null)
                {
                    try
                    {
                        DebugHelper.WriteLogEx("Add proxy credential", 1);
                        wsmanoptions.AddProxyCredentials(credentials);
                    }
                    catch (Exception ex)
                    {
                        DebugHelper.WriteLogEx(ex.ToString(), 1);
                        throw ex;
                    }
                }
            }

            if (this.proxytypeSet)
            {
                wsmanoptions.ProxyType = this.ProxyType;
                this.proxytypeSet = false;
            }
            else
            {
                wsmanoptions.ProxyType = Options.ProxyType.WinHttp;
            }

            if (this.usesslSet)
            {
                wsmanoptions.UseSsl = this.UseSsl;
                this.usesslSet = false;
            }
            else
            {
                wsmanoptions.UseSsl = false;
            }

            wsmanoptions.DestinationPort = 0;
            return wsmanoptions;
        }
        #endregion

        #region private members

        #region const string of parameter names
        internal const string nameNoEncryption = "NoEncryption";
        internal const string nameSkipCACheck = "SkipCACheck";
        internal const string nameSkipCNCheck = "SkipCNCheck";
        internal const string nameSkipRevocationCheck = "SkipRevocationCheck";
        internal const string nameEncodePortInServicePrincipalName = "EncodePortInServicePrincipalName";
        internal const string nameEncoding = "Encoding";
        internal const string nameHttpPrefix = "HttpPrefix";
        internal const string nameMaxEnvelopeSizeKB = "MaxEnvelopeSizeKB";
        internal const string nameProxyAuthentication = "ProxyAuthentication";
        internal const string nameProxyCertificateThumbprint = "ProxyCertificateThumbprint";
        internal const string nameProxyCredential = "ProxyCredential";
        internal const string nameProxyType = "ProxyType";
        internal const string nameUseSsl = "UseSsl";
        internal const string nameImpersonation = "Impersonation";
        internal const string namePacketIntegrity = "PacketIntegrity";
        internal const string namePacketPrivacy = "PacketPrivacy";
        internal const string nameProtocol = "Protocol";
        #endregion

        /// <summary>
        /// Static parameter definition entries.
        /// </summary>
        static Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters = new Dictionary<string, HashSet<ParameterDefinitionEntry>>
        {
            {
                nameNoEncryption, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameSkipCACheck, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameSkipCNCheck, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameSkipRevocationCheck, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameEncodePortInServicePrincipalName, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameEncoding, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameHttpPrefix, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameMaxEnvelopeSizeKB, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameProxyAuthentication, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameProxyCertificateThumbprint, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameProxyCredential, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameProxyType, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },

            {
                nameUseSsl, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.WSManParameterSet, false),
                                 }
            },
            {
                nameImpersonation, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.DcomParameterSet, false),
                                 }
            },

            {
                namePacketIntegrity, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.DcomParameterSet, false),
                                 }
            },
            {
                namePacketPrivacy, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.DcomParameterSet, false),
                                 }
            },

            {
                nameProtocol, new HashSet<ParameterDefinitionEntry> {
                                    new ParameterDefinitionEntry(CimBaseCommand.ProtocolNameParameterSet, true),
                                 }
            },
        };

        /// <summary>
        /// Static parameter set entries.
        /// </summary>
        static Dictionary<string, ParameterSetEntry> parameterSets = new Dictionary<string, ParameterSetEntry>
        {
            {   CimBaseCommand.ProtocolNameParameterSet, new ParameterSetEntry(1, true)     },
            {   CimBaseCommand.DcomParameterSet, new ParameterSetEntry(0)     },
            {   CimBaseCommand.WSManParameterSet, new ParameterSetEntry(0)     },
        };
        #endregion
    }
}
