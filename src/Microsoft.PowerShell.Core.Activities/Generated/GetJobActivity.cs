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
    /// Activity to invoke the Microsoft.PowerShell.Core\Get-Job command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetJob : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetJob()
        {
            this.DisplayName = "Get-Job";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Get-Job"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the IncludeChildJob parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IncludeChildJob { get; set; }

        /// <summary>
        /// Provides access to the ChildJobState parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.JobState> ChildJobState { get; set; }

        /// <summary>
        /// Provides access to the HasMoreData parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Boolean> HasMoreData { get; set; }

        /// <summary>
        /// Provides access to the Before parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> Before { get; set; }

        /// <summary>
        /// Provides access to the After parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> After { get; set; }

        /// <summary>
        /// Provides access to the Newest parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Newest { get; set; }

        /// <summary>
        /// Provides access to the JobId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> JobId { get; set; }

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
        /// Provides access to the State parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.JobState> State { get; set; }

        /// <summary>
        /// Provides access to the Command parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Command { get; set; }

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
            
            if(IncludeChildJob.Expression != null)
            {
                targetCommand.AddParameter("IncludeChildJob", IncludeChildJob.Get(context));
            }

            if(ChildJobState.Expression != null)
            {
                targetCommand.AddParameter("ChildJobState", ChildJobState.Get(context));
            }

            if(HasMoreData.Expression != null)
            {
                targetCommand.AddParameter("HasMoreData", HasMoreData.Get(context));
            }

            if(Before.Expression != null)
            {
                targetCommand.AddParameter("Before", Before.Get(context));
            }

            if(After.Expression != null)
            {
                targetCommand.AddParameter("After", After.Get(context));
            }

            if(Newest.Expression != null)
            {
                targetCommand.AddParameter("Newest", Newest.Get(context));
            }

            if(JobId.Expression != null)
            {
                targetCommand.AddParameter("Id", JobId.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(InstanceId.Expression != null)
            {
                targetCommand.AddParameter("InstanceId", InstanceId.Get(context));
            }

            if(State.Expression != null)
            {
                targetCommand.AddParameter("State", State.Get(context));
            }

            if(Command.Expression != null)
            {
                targetCommand.AddParameter("Command", Command.Get(context));
            }

            if(Filter.Expression != null)
            {
                targetCommand.AddParameter("Filter", Filter.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
