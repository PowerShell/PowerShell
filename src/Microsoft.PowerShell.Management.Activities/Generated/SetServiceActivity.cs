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
    /// Activity to invoke the Microsoft.PowerShell.Management\Set-Service command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SetService : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SetService()
        {
            this.DisplayName = "Set-Service";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Set-Service"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the ServiceDisplayName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ServiceDisplayName { get; set; }

        /// <summary>
        /// Provides access to the Description parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Description { get; set; }

        /// <summary>
        /// Provides access to the StartupType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.ServiceProcess.ServiceStartMode> StartupType { get; set; }

        /// <summary>
        /// Provides access to the Status parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Status { get; set; }

        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.ServiceProcess.ServiceController> InputObject { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

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
            
            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(ServiceDisplayName.Expression != null)
            {
                targetCommand.AddParameter("DisplayName", ServiceDisplayName.Get(context));
            }

            if(Description.Expression != null)
            {
                targetCommand.AddParameter("Description", Description.Get(context));
            }

            if(StartupType.Expression != null)
            {
                targetCommand.AddParameter("StartupType", StartupType.Get(context));
            }

            if(Status.Expression != null)
            {
                targetCommand.AddParameter("Status", Status.Get(context));
            }

            if(InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
