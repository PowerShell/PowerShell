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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Get-Date command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetDate : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetDate()
        {
            this.DisplayName = "Get-Date";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Get-Date"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Date parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> Date { get; set; }

        /// <summary>
        /// Provides access to the Year parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Year { get; set; }

        /// <summary>
        /// Provides access to the Month parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Month { get; set; }

        /// <summary>
        /// Provides access to the Day parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Day { get; set; }

        /// <summary>
        /// Provides access to the Hour parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Hour { get; set; }

        /// <summary>
        /// Provides access to the Minute parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Minute { get; set; }

        /// <summary>
        /// Provides access to the Second parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Second { get; set; }

        /// <summary>
        /// Provides access to the Millisecond parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Millisecond { get; set; }

        /// <summary>
        /// Provides access to the DisplayHint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.DisplayHintType> DisplayHint { get; set; }

        /// <summary>
        /// Provides access to the UFormat parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> UFormat { get; set; }

        /// <summary>
        /// Provides access to the Format parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Format { get; set; }


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

            if(Year.Expression != null)
            {
                targetCommand.AddParameter("Year", Year.Get(context));
            }

            if(Month.Expression != null)
            {
                targetCommand.AddParameter("Month", Month.Get(context));
            }

            if(Day.Expression != null)
            {
                targetCommand.AddParameter("Day", Day.Get(context));
            }

            if(Hour.Expression != null)
            {
                targetCommand.AddParameter("Hour", Hour.Get(context));
            }

            if(Minute.Expression != null)
            {
                targetCommand.AddParameter("Minute", Minute.Get(context));
            }

            if(Second.Expression != null)
            {
                targetCommand.AddParameter("Second", Second.Get(context));
            }

            if(Millisecond.Expression != null)
            {
                targetCommand.AddParameter("Millisecond", Millisecond.Get(context));
            }

            if(DisplayHint.Expression != null)
            {
                targetCommand.AddParameter("DisplayHint", DisplayHint.Get(context));
            }

            if(UFormat.Expression != null)
            {
                targetCommand.AddParameter("UFormat", UFormat.Get(context));
            }

            if(Format.Expression != null)
            {
                targetCommand.AddParameter("Format", Format.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
