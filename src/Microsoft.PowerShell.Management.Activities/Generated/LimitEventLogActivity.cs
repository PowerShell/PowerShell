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
    /// Activity to invoke the Microsoft.PowerShell.Management\Limit-EventLog command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class LimitEventLog : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public LimitEventLog()
        {
            this.DisplayName = "Limit-EventLog";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Limit-EventLog"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the LogName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> LogName { get; set; }

        /// <summary>
        /// Provides access to the RetentionDays parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> RetentionDays { get; set; }

        /// <summary>
        /// Provides access to the OverflowAction parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Diagnostics.OverflowAction> OverflowAction { get; set; }

        /// <summary>
        /// Provides access to the MaximumSize parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int64> MaximumSize { get; set; }

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
            
            if(LogName.Expression != null)
            {
                targetCommand.AddParameter("LogName", LogName.Get(context));
            }

            if(RetentionDays.Expression != null)
            {
                targetCommand.AddParameter("RetentionDays", RetentionDays.Get(context));
            }

            if(OverflowAction.Expression != null)
            {
                targetCommand.AddParameter("OverflowAction", OverflowAction.Get(context));
            }

            if(MaximumSize.Expression != null)
            {
                targetCommand.AddParameter("MaximumSize", MaximumSize.Get(context));
            }

            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
