//
//    Copyright (C) Microsoft.  All rights reserved.
//
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Diagnostics.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Diagnostics\Import-Counter command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class ImportCounter : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public ImportCounter()
        {
            this.DisplayName = "Import-Counter";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Diagnostics\\Import-Counter"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Path { get; set; }

        /// <summary>
        /// Provides access to the ListSet parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ListSet { get; set; }

        /// <summary>
        /// Provides access to the StartTime parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> StartTime { get; set; }

        /// <summary>
        /// Provides access to the EndTime parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> EndTime { get; set; }

        /// <summary>
        /// Provides access to the Counter parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Counter { get; set; }

        /// <summary>
        /// Provides access to the Summary parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Summary { get; set; }

        /// <summary>
        /// Provides access to the MaxSamples parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int64> MaxSamples { get; set; }


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
            
            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if(ListSet.Expression != null)
            {
                targetCommand.AddParameter("ListSet", ListSet.Get(context));
            }

            if(StartTime.Expression != null)
            {
                targetCommand.AddParameter("StartTime", StartTime.Get(context));
            }

            if(EndTime.Expression != null)
            {
                targetCommand.AddParameter("EndTime", EndTime.Get(context));
            }

            if(Counter.Expression != null)
            {
                targetCommand.AddParameter("Counter", Counter.Get(context));
            }

            if(Summary.Expression != null)
            {
                targetCommand.AddParameter("Summary", Summary.Get(context));
            }

            if(MaxSamples.Expression != null)
            {
                targetCommand.AddParameter("MaxSamples", MaxSamples.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
