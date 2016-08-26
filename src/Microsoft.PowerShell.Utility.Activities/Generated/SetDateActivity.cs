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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Set-Date command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SetDate : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SetDate()
        {
            this.DisplayName = "Set-Date";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Set-Date"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Date parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> Date { get; set; }

        /// <summary>
        /// Provides access to the Adjust parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.TimeSpan> Adjust { get; set; }

        /// <summary>
        /// Provides access to the DisplayHint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.DisplayHintType> DisplayHint { get; set; }


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
            
            if(Date.Expression != null)
            {
                targetCommand.AddParameter("Date", Date.Get(context));
            }

            if(Adjust.Expression != null)
            {
                targetCommand.AddParameter("Adjust", Adjust.Get(context));
            }

            if(DisplayHint.Expression != null)
            {
                targetCommand.AddParameter("DisplayHint", DisplayHint.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
