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
    /// Activity to invoke the Microsoft.PowerShell.Core\Get-Module command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetModule : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetModule()
        {
            this.DisplayName = "Get-Module";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Get-Module"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Name { get; set; }

        /// <summary>
        /// Provides access to the FullyQualifiedName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.ModuleSpecification[]> FullyQualifiedName { get; set; }

        /// <summary>
        /// Provides access to the All parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> All { get; set; }

        /// <summary>
        /// Provides access to the ListAvailable parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ListAvailable { get; set; }

        /// <summary>
        /// Provides access to the Refresh parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Refresh { get; set; }

        /// <summary>
        /// Provides access to the PSSession parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Runspaces.PSSession> PSSession { get; set; }

        /// <summary>
        /// Provides access to the CimSession parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.CimSession> CimSession { get; set; }

        /// <summary>
        /// Provides access to the CimResourceUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> CimResourceUri { get; set; }

        /// <summary>
        /// Provides access to the CimNamespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CimNamespace { get; set; }


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

            if(FullyQualifiedName.Expression != null)
            {
                targetCommand.AddParameter("FullyQualifiedName", FullyQualifiedName.Get(context));
            }

            if(All.Expression != null)
            {
                targetCommand.AddParameter("All", All.Get(context));
            }

            if(ListAvailable.Expression != null)
            {
                targetCommand.AddParameter("ListAvailable", ListAvailable.Get(context));
            }

            if(Refresh.Expression != null)
            {
                targetCommand.AddParameter("Refresh", Refresh.Get(context));
            }

            if(PSSession.Expression != null)
            {
                targetCommand.AddParameter("PSSession", PSSession.Get(context));
            }

            if(CimSession.Expression != null)
            {
                targetCommand.AddParameter("CimSession", CimSession.Get(context));
            }

            if(CimResourceUri.Expression != null)
            {
                targetCommand.AddParameter("CimResourceUri", CimResourceUri.Get(context));
            }

            if(CimNamespace.Expression != null)
            {
                targetCommand.AddParameter("CimNamespace", CimNamespace.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
