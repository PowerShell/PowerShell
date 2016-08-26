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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Get-Unique command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetUnique : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetUnique()
        {
            this.DisplayName = "Get-Unique";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Get-Unique"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the AsString parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AsString { get; set; }

        /// <summary>
        /// Provides access to the OnType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> OnType { get; set; }


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
            
            if(InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if(AsString.Expression != null)
            {
                targetCommand.AddParameter("AsString", AsString.Get(context));
            }

            if(OnType.Expression != null)
            {
                targetCommand.AddParameter("OnType", OnType.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
