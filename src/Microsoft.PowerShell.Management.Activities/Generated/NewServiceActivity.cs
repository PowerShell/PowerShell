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
    /// Activity to invoke the Microsoft.PowerShell.Management\New-Service command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewService : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewService()
        {
            this.DisplayName = "New-Service";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\New-Service"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the BinaryPathName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> BinaryPathName { get; set; }

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
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the DependsOn parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> DependsOn { get; set; }


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

            if(BinaryPathName.Expression != null)
            {
                targetCommand.AddParameter("BinaryPathName", BinaryPathName.Get(context));
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

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(DependsOn.Expression != null)
            {
                targetCommand.AddParameter("DependsOn", DependsOn.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
