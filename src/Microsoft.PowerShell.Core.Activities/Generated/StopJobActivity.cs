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
    /// Activity to invoke the Microsoft.PowerShell.Core\Stop-Job command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class StopJob : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public StopJob()
        {
            this.DisplayName = "Stop-Job";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Stop-Job"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Job parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Job[]> Job { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Name { get; set; }

        /// <summary>
        /// Provides access to the InstanceId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid[]> InstanceId { get; set; }

        /// <summary>
        /// Provides access to the JobId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> JobId { get; set; }

        /// <summary>
        /// Provides access to the State parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.JobState> State { get; set; }

        /// <summary>
        /// Provides access to the Filter parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.Hashtable> Filter { get; set; }


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
            
            if(Job.Expression != null)
            {
                targetCommand.AddParameter("Job", Job.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(InstanceId.Expression != null)
            {
                targetCommand.AddParameter("InstanceId", InstanceId.Get(context));
            }

            if(JobId.Expression != null)
            {
                targetCommand.AddParameter("Id", JobId.Get(context));
            }

            if(State.Expression != null)
            {
                targetCommand.AddParameter("State", State.Get(context));
            }

            if(Filter.Expression != null)
            {
                targetCommand.AddParameter("Filter", Filter.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
