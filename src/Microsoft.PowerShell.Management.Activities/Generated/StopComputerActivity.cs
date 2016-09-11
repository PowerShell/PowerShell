//
//    Copyright (C) Microsoft.  All rights reserved.
//
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Management.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Management\Stop-Computer command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class StopComputer : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public StopComputer()
        {
            this.DisplayName = "Stop-Computer";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Stop-Computer"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the AsJob parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AsJob { get; set; }

        /// <summary>
        /// Provides access to the DcomAuthentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.AuthenticationLevel> DcomAuthentication { get; set; }

        /// <summary>
        /// Provides access to the WsmanAuthentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> WsmanAuthentication { get; set; }

        /// <summary>
        /// Provides access to the Protocol parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Protocol { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the Impersonation parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.ImpersonationLevel> Impersonation { get; set; }

        /// <summary>
        /// Provides access to the ThrottleLimit parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> ThrottleLimit { get; set; }

        /// <summary>
        /// Provides access to the Force parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Force { get; set; }

        /// <summary>
        /// Declares that this activity supports its own remoting.
        /// </summary>        
        protected override bool SupportsCustomRemoting { get { return true; } }


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
            
            if(AsJob.Expression != null)
            {
                targetCommand.AddParameter("AsJob", AsJob.Get(context));
            }

            if(DcomAuthentication.Expression != null)
            {
                targetCommand.AddParameter("DcomAuthentication", DcomAuthentication.Get(context));
            }

            if(WsmanAuthentication.Expression != null)
            {
                targetCommand.AddParameter("WsmanAuthentication", WsmanAuthentication.Get(context));
            }

            if(Protocol.Expression != null)
            {
                targetCommand.AddParameter("Protocol", Protocol.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(Impersonation.Expression != null)
            {
                targetCommand.AddParameter("Impersonation", Impersonation.Get(context));
            }

            if(ThrottleLimit.Expression != null)
            {
                targetCommand.AddParameter("ThrottleLimit", ThrottleLimit.Get(context));
            }

            if(Force.Expression != null)
            {
                targetCommand.AddParameter("Force", Force.Get(context));
            }

            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
