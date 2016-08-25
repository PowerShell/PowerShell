
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to invoke the CimCmdlets\New-CimSessionOption command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewCimSessionOption : GenericCimCmdletActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewCimSessionOption()
        {
            this.DisplayName = "New-CimSessionOption";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "CimCmdlets\\New-CimSessionOption"; } }

        /// <summary>
        /// The .NET type implementing the cmdlet to invoke.
        /// </summary>
        public override System.Type TypeImplementingCmdlet { get { return typeof(Microsoft.Management.Infrastructure.CimCmdlets.NewCimSessionOptionCommand); } }

        // Arguments
        
        /// <summary>
        /// Provides access to the NoEncryption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NoEncryption { get; set; }

        /// <summary>
        /// Provides access to the CertificateCACheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CertificateCACheck { get; set; }

        /// <summary>
        /// Provides access to the CertificateCNCheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CertificateCNCheck { get; set; }

        /// <summary>
        /// Provides access to the CertRevocationCheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CertRevocationCheck { get; set; }

        /// <summary>
        /// Provides access to the EncodePortInServicePrincipalName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> EncodePortInServicePrincipalName { get; set; }

        /// <summary>
        /// Provides access to the Encoding parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.PacketEncoding> Encoding { get; set; }

        /// <summary>
        /// Provides access to the HttpPrefix parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> HttpPrefix { get; set; }

        /// <summary>
        /// Provides access to the MaxEnvelopeSizeKB parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.UInt32> MaxEnvelopeSizeKB { get; set; }

        /// <summary>
        /// Provides access to the ProxyAuthentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.PasswordAuthenticationMechanism> ProxyAuthentication { get; set; }

        /// <summary>
        /// Provides access to the ProxyCertificateThumbprint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ProxyCertificateThumbprint { get; set; }

        /// <summary>
        /// Provides access to the ProxyCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> ProxyCredential { get; set; }

        /// <summary>
        /// Provides access to the ProxyType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.ProxyType> ProxyType { get; set; }

        /// <summary>
        /// Provides access to the UseSsl parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseSsl { get; set; }

        /// <summary>
        /// Provides access to the Impersonation parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.ImpersonationType> Impersonation { get; set; }

        /// <summary>
        /// Provides access to the PacketIntegrity parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PacketIntegrity { get; set; }

        /// <summary>
        /// Provides access to the PacketPrivacy parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PacketPrivacy { get; set; }

        /// <summary>
        /// Provides access to the Protocol parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.CimCmdlets.ProtocolType> Protocol { get; set; }

        /// <summary>
        /// Provides access to the UICulture parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Globalization.CultureInfo> UICulture { get; set; }

        /// <summary>
        /// Provides access to the Culture parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Globalization.CultureInfo> Culture { get; set; }


        /// <summary>
        /// Script module contents for this activity`n/// </summary>
        protected override string PSDefiningModule { get { return null; } }

        /// <summary>
        /// Returns a configured instance of System.Management.Automation.PowerShell, pre-populated with the command to run.
        /// </summary>
        /// <param name="context">The NativeActivityContext for the currently running activity.</param>
        /// <returns>A populated instance of System.Management.Automation.PowerShell</returns>
        /// <remarks>The infrastructure takes responsibility for closing and disposing the PowerShell instance returned.</remarks>
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            System.Management.Automation.PowerShell invoker = global::System.Management.Automation.PowerShell.Create();
            System.Management.Automation.PowerShell targetCommand = invoker.AddCommand(PSCommandName);

            // Initialize the arguments
            
            if(NoEncryption.Expression != null)
            {
                targetCommand.AddParameter("NoEncryption", NoEncryption.Get(context));
            }

            if(CertificateCACheck.Expression != null)
            {
                targetCommand.AddParameter("CertificateCACheck", CertificateCACheck.Get(context));
            }

            if(CertificateCNCheck.Expression != null)
            {
                targetCommand.AddParameter("CertificateCNCheck", CertificateCNCheck.Get(context));
            }

            if(CertRevocationCheck.Expression != null)
            {
                targetCommand.AddParameter("CertRevocationCheck", CertRevocationCheck.Get(context));
            }

            if(EncodePortInServicePrincipalName.Expression != null)
            {
                targetCommand.AddParameter("EncodePortInServicePrincipalName", EncodePortInServicePrincipalName.Get(context));
            }

            if(Encoding.Expression != null)
            {
                targetCommand.AddParameter("Encoding", Encoding.Get(context));
            }

            if(HttpPrefix.Expression != null)
            {
                targetCommand.AddParameter("HttpPrefix", HttpPrefix.Get(context));
            }

            if(MaxEnvelopeSizeKB.Expression != null)
            {
                targetCommand.AddParameter("MaxEnvelopeSizeKB", MaxEnvelopeSizeKB.Get(context));
            }

            if(ProxyAuthentication.Expression != null)
            {
                targetCommand.AddParameter("ProxyAuthentication", ProxyAuthentication.Get(context));
            }

            if(ProxyCertificateThumbprint.Expression != null)
            {
                targetCommand.AddParameter("ProxyCertificateThumbprint", ProxyCertificateThumbprint.Get(context));
            }

            if(ProxyCredential.Expression != null)
            {
                targetCommand.AddParameter("ProxyCredential", ProxyCredential.Get(context));
            }

            if(ProxyType.Expression != null)
            {
                targetCommand.AddParameter("ProxyType", ProxyType.Get(context));
            }

            if(UseSsl.Expression != null)
            {
                targetCommand.AddParameter("UseSsl", UseSsl.Get(context));
            }

            if(Impersonation.Expression != null)
            {
                targetCommand.AddParameter("Impersonation", Impersonation.Get(context));
            }

            if(PacketIntegrity.Expression != null)
            {
                targetCommand.AddParameter("PacketIntegrity", PacketIntegrity.Get(context));
            }

            if(PacketPrivacy.Expression != null)
            {
                targetCommand.AddParameter("PacketPrivacy", PacketPrivacy.Get(context));
            }

            if(Protocol.Expression != null)
            {
                targetCommand.AddParameter("Protocol", Protocol.Get(context));
            }

            if(UICulture.Expression != null)
            {
                targetCommand.AddParameter("UICulture", UICulture.Get(context));
            }

            if(Culture.Expression != null)
            {
                targetCommand.AddParameter("Culture", Culture.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
