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
    /// Activity to invoke the Microsoft.PowerShell.Management\Rename-Computer command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class RenameComputer : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RenameComputer()
        {
            this.DisplayName = "Rename-Computer";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Rename-Computer"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the DomainCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> DomainCredential { get; set; }

        /// <summary>
        /// Provides access to the LocalCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> LocalCredential { get; set; }

        /// <summary>
        /// Provides access to the NewName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> NewName { get; set; }

        /// <summary>
        /// Provides access to the Force parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Force { get; set; }

        /// <summary>
        /// Provides access to the Restart parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Restart { get; set; }

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
            
            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(DomainCredential.Expression != null)
            {
                targetCommand.AddParameter("DomainCredential", DomainCredential.Get(context));
            }

            if(LocalCredential.Expression != null)
            {
                targetCommand.AddParameter("LocalCredential", LocalCredential.Get(context));
            }

            if(NewName.Expression != null)
            {
                targetCommand.AddParameter("NewName", NewName.Get(context));
            }

            if(Force.Expression != null)
            {
                targetCommand.AddParameter("Force", Force.Get(context));
            }

            if(Restart.Expression != null)
            {
                targetCommand.AddParameter("Restart", Restart.Get(context));
            }

            if(WsmanAuthentication.Expression != null)
            {
                targetCommand.AddParameter("WsmanAuthentication", WsmanAuthentication.Get(context));
            }

            if(Protocol.Expression != null)
            {
                targetCommand.AddParameter("Protocol", Protocol.Get(context));
            }

            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
