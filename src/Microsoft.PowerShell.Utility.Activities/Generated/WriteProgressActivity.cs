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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Write-Progress command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class WriteProgress : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public WriteProgress()
        {
            this.DisplayName = "Write-Progress";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Write-Progress"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Activity parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Activity { get; set; }

        /// <summary>
        /// Provides access to the Status parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Status { get; set; }

        /// <summary>
        /// Provides access to the ProgressId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> ProgressId { get; set; }

        /// <summary>
        /// Provides access to the PercentComplete parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> PercentComplete { get; set; }

        /// <summary>
        /// Provides access to the SecondsRemaining parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> SecondsRemaining { get; set; }

        /// <summary>
        /// Provides access to the CurrentOperation parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CurrentOperation { get; set; }

        /// <summary>
        /// Provides access to the ParentId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> ParentId { get; set; }

        /// <summary>
        /// Provides access to the Completed parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Completed { get; set; }

        /// <summary>
        /// Provides access to the SourceId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> SourceId { get; set; }


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
            
            if(Activity.Expression != null)
            {
                targetCommand.AddParameter("Activity", Activity.Get(context));
            }

            if(Status.Expression != null)
            {
                targetCommand.AddParameter("Status", Status.Get(context));
            }

            if(ProgressId.Expression != null)
            {
                targetCommand.AddParameter("Id", ProgressId.Get(context));
            }

            if(PercentComplete.Expression != null)
            {
                targetCommand.AddParameter("PercentComplete", PercentComplete.Get(context));
            }

            if(SecondsRemaining.Expression != null)
            {
                targetCommand.AddParameter("SecondsRemaining", SecondsRemaining.Get(context));
            }

            if(CurrentOperation.Expression != null)
            {
                targetCommand.AddParameter("CurrentOperation", CurrentOperation.Get(context));
            }

            if(ParentId.Expression != null)
            {
                targetCommand.AddParameter("ParentId", ParentId.Get(context));
            }

            if(Completed.Expression != null)
            {
                targetCommand.AddParameter("Completed", Completed.Get(context));
            }

            if(SourceId.Expression != null)
            {
                targetCommand.AddParameter("SourceId", SourceId.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
