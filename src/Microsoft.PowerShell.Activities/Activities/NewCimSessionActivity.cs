
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to invoke the CimCmdlets\New-CimSession command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewCimSession : GenericCimCmdletActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewCimSession()
        {
            this.DisplayName = "New-CimSession";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "CimCmdlets\\New-CimSession"; } }

        /// <summary>
        /// The .NET type implementing the cmdlet to invoke.
        /// </summary>
        public override System.Type TypeImplementingCmdlet { get { return typeof(Microsoft.Management.Infrastructure.CimCmdlets.NewCimSessionCommand); } }

        // Arguments
        
        /// <summary>
        /// Provides access to the Authentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.PasswordAuthenticationMechanism> Authentication { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the CertificateThumbprint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CertificateThumbprint { get; set; }

        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the OperationTimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.UInt32> OperationTimeoutSec { get; set; }

        /// <summary>
        /// Provides access to the Port parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.UInt32> Port { get; set; }

        /// <summary>
        /// Provides access to the SessionOption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.Options.CimSessionOptions> SessionOption { get; set; }



        /// <summary>
        /// Script module contents for this activity`n/// </summary>
        protected override string PSDefiningModule { get { return "CimCmdlets"; } }

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

            if (GetIsComputerNameSpecified(context))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }

            if(Authentication.Expression != null)
            {
                targetCommand.AddParameter("Authentication", Authentication.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(CertificateThumbprint.Expression != null)
            {
                targetCommand.AddParameter("CertificateThumbprint", CertificateThumbprint.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(OperationTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("OperationTimeoutSec", OperationTimeoutSec.Get(context));
            }

            if(Port.Expression != null)
            {
                targetCommand.AddParameter("Port", Port.Get(context));
            }

            if(SessionOption.Expression != null)
            {
                targetCommand.AddParameter("SessionOption", SessionOption.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
