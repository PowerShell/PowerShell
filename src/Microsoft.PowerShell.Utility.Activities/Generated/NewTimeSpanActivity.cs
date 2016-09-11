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
    /// Activity to invoke the Microsoft.PowerShell.Utility\New-TimeSpan command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewTimeSpan : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewTimeSpan()
        {
            this.DisplayName = "New-TimeSpan";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\New-TimeSpan"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Start parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> Start { get; set; }

        /// <summary>
        /// Provides access to the End parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> End { get; set; }

        /// <summary>
        /// Provides access to the Days parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Days { get; set; }

        /// <summary>
        /// Provides access to the Hours parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Hours { get; set; }

        /// <summary>
        /// Provides access to the Minutes parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Minutes { get; set; }

        /// <summary>
        /// Provides access to the Seconds parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Seconds { get; set; }


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
            
            if(Start.Expression != null)
            {
                targetCommand.AddParameter("Start", Start.Get(context));
            }

            if(End.Expression != null)
            {
                targetCommand.AddParameter("End", End.Get(context));
            }

            if(Days.Expression != null)
            {
                targetCommand.AddParameter("Days", Days.Get(context));
            }

            if(Hours.Expression != null)
            {
                targetCommand.AddParameter("Hours", Hours.Get(context));
            }

            if(Minutes.Expression != null)
            {
                targetCommand.AddParameter("Minutes", Minutes.Get(context));
            }

            if(Seconds.Expression != null)
            {
                targetCommand.AddParameter("Seconds", Seconds.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
