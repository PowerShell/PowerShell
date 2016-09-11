//
//    Copyright (C) Microsoft.  All rights reserved.
//
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Core.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Core\Remove-PSSession command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class RemovePSSession : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RemovePSSession()
        {
            this.DisplayName = "Remove-PSSession";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Remove-PSSession"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Session parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Runspaces.PSSession[]> Session { get; set; }

        /// <summary>
        /// Provides access to the ContainerId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ContainerId { get; set; }

        /// <summary>
        /// Provides access to the VMId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid[]> VMId { get; set; }

        /// <summary>
        /// Provides access to the VMName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> VMName { get; set; }

        /// <summary>
        /// Provides access to the InstanceId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid[]> InstanceId { get; set; }

        /// <summary>
        /// Provides access to the PSSessionId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> PSSessionId { get; set; }

        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Name { get; set; }

        /// <summary>
        /// Provides access to the ComputerName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ComputerName { get; set; }


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
            
            if(Session.Expression != null)
            {
                targetCommand.AddParameter("Session", Session.Get(context));
            }

            if(ContainerId.Expression != null)
            {
                targetCommand.AddParameter("ContainerId", ContainerId.Get(context));
            }

            if(VMId.Expression != null)
            {
                targetCommand.AddParameter("VMId", VMId.Get(context));
            }

            if(VMName.Expression != null)
            {
                targetCommand.AddParameter("VMName", VMName.Get(context));
            }

            if(InstanceId.Expression != null)
            {
                targetCommand.AddParameter("InstanceId", InstanceId.Get(context));
            }

            if(PSSessionId.Expression != null)
            {
                targetCommand.AddParameter("Id", PSSessionId.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(ComputerName.Expression != null)
            {
                targetCommand.AddParameter("ComputerName", ComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
