//
//    Copyright (C) Microsoft.  All rights reserved.
//
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Utility.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Utility\Invoke-WebRequest command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class InvokeWebRequest : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public InvokeWebRequest()
        {
            this.DisplayName = "Invoke-WebRequest";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Invoke-WebRequest"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the UseBasicParsing parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseBasicParsing { get; set; }

        /// <summary>
        /// Provides access to the Uri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> Uri { get; set; }

        /// <summary>
        /// Provides access to the WebSession parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.WebRequestSession> WebSession { get; set; }

        /// <summary>
        /// Provides access to the SessionVariable parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> SessionVariable { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the UseDefaultCredentials parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseDefaultCredentials { get; set; }

        /// <summary>
        /// Provides access to the CertificateThumbprint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CertificateThumbprint { get; set; }

        /// <summary>
        /// Provides access to the Certificate parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Security.Cryptography.X509Certificates.X509Certificate> Certificate { get; set; }

        /// <summary>
        /// Provides access to the UserAgent parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> UserAgent { get; set; }

        /// <summary>
        /// Provides access to the DisableKeepAlive parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> DisableKeepAlive { get; set; }

        /// <summary>
        /// Provides access to the TimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> TimeoutSec { get; set; }

        /// <summary>
        /// Provides access to the Headers parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.IDictionary> Headers { get; set; }

        /// <summary>
        /// Provides access to the MaximumRedirection parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> MaximumRedirection { get; set; }

        /// <summary>
        /// Provides access to the Method parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.WebRequestMethod> Method { get; set; }

        /// <summary>
        /// Provides access to the Proxy parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> Proxy { get; set; }

        /// <summary>
        /// Provides access to the ProxyCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> ProxyCredential { get; set; }

        /// <summary>
        /// Provides access to the ProxyUseDefaultCredentials parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ProxyUseDefaultCredentials { get; set; }

        /// <summary>
        /// Provides access to the Body parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object> Body { get; set; }

        /// <summary>
        /// Provides access to the ContentType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ContentType { get; set; }

        /// <summary>
        /// Provides access to the TransferEncoding parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> TransferEncoding { get; set; }

        /// <summary>
        /// Provides access to the InFile parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> InFile { get; set; }

        /// <summary>
        /// Provides access to the OutFile parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> OutFile { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }


        // Module defining this command
        

        // Optional custom code for this activity
        

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
            
            if(UseBasicParsing.Expression != null)
            {
                targetCommand.AddParameter("UseBasicParsing", UseBasicParsing.Get(context));
            }

            if(Uri.Expression != null)
            {
                targetCommand.AddParameter("Uri", Uri.Get(context));
            }

            if(WebSession.Expression != null)
            {
                targetCommand.AddParameter("WebSession", WebSession.Get(context));
            }

            if(SessionVariable.Expression != null)
            {
                targetCommand.AddParameter("SessionVariable", SessionVariable.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(UseDefaultCredentials.Expression != null)
            {
                targetCommand.AddParameter("UseDefaultCredentials", UseDefaultCredentials.Get(context));
            }

            if(CertificateThumbprint.Expression != null)
            {
                targetCommand.AddParameter("CertificateThumbprint", CertificateThumbprint.Get(context));
            }

            if(Certificate.Expression != null)
            {
                targetCommand.AddParameter("Certificate", Certificate.Get(context));
            }

            if(UserAgent.Expression != null)
            {
                targetCommand.AddParameter("UserAgent", UserAgent.Get(context));
            }

            if(DisableKeepAlive.Expression != null)
            {
                targetCommand.AddParameter("DisableKeepAlive", DisableKeepAlive.Get(context));
            }

            if(TimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("TimeoutSec", TimeoutSec.Get(context));
            }

            if(Headers.Expression != null)
            {
                targetCommand.AddParameter("Headers", Headers.Get(context));
            }

            if(MaximumRedirection.Expression != null)
            {
                targetCommand.AddParameter("MaximumRedirection", MaximumRedirection.Get(context));
            }

            if(Method.Expression != null)
            {
                targetCommand.AddParameter("Method", Method.Get(context));
            }

            if(Proxy.Expression != null)
            {
                targetCommand.AddParameter("Proxy", Proxy.Get(context));
            }

            if(ProxyCredential.Expression != null)
            {
                targetCommand.AddParameter("ProxyCredential", ProxyCredential.Get(context));
            }

            if(ProxyUseDefaultCredentials.Expression != null)
            {
                targetCommand.AddParameter("ProxyUseDefaultCredentials", ProxyUseDefaultCredentials.Get(context));
            }

            if(Body.Expression != null)
            {
                targetCommand.AddParameter("Body", Body.Get(context));
            }

            if(ContentType.Expression != null)
            {
                targetCommand.AddParameter("ContentType", ContentType.Get(context));
            }

            if(TransferEncoding.Expression != null)
            {
                targetCommand.AddParameter("TransferEncoding", TransferEncoding.Get(context));
            }

            if(InFile.Expression != null)
            {
                targetCommand.AddParameter("InFile", InFile.Get(context));
            }

            if(OutFile.Expression != null)
            {
                targetCommand.AddParameter("OutFile", OutFile.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
