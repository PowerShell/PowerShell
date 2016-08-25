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
    /// Activity to invoke the Microsoft.PowerShell.Core\New-PSTransportOption command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewPSTransportOption : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewPSTransportOption()
        {
            this.DisplayName = "New-PSTransportOption";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\New-PSTransportOption"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the MaxIdleTimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxIdleTimeoutSec { get; set; }

        /// <summary>
        /// Provides access to the ProcessIdleTimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> ProcessIdleTimeoutSec { get; set; }

        /// <summary>
        /// Provides access to the MaxSessions parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxSessions { get; set; }

        /// <summary>
        /// Provides access to the MaxConcurrentCommandsPerSession parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxConcurrentCommandsPerSession { get; set; }

        /// <summary>
        /// Provides access to the MaxSessionsPerUser parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxSessionsPerUser { get; set; }

        /// <summary>
        /// Provides access to the MaxMemoryPerSessionMB parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxMemoryPerSessionMB { get; set; }

        /// <summary>
        /// Provides access to the MaxProcessesPerSession parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxProcessesPerSession { get; set; }

        /// <summary>
        /// Provides access to the MaxConcurrentUsers parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> MaxConcurrentUsers { get; set; }

        /// <summary>
        /// Provides access to the IdleTimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Int32>> IdleTimeoutSec { get; set; }

        /// <summary>
        /// Provides access to the OutputBufferingMode parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Management.Automation.Runspaces.OutputBufferingMode>> OutputBufferingMode { get; set; }


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
            
            if(MaxIdleTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("MaxIdleTimeoutSec", MaxIdleTimeoutSec.Get(context));
            }

            if(ProcessIdleTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("ProcessIdleTimeoutSec", ProcessIdleTimeoutSec.Get(context));
            }

            if(MaxSessions.Expression != null)
            {
                targetCommand.AddParameter("MaxSessions", MaxSessions.Get(context));
            }

            if(MaxConcurrentCommandsPerSession.Expression != null)
            {
                targetCommand.AddParameter("MaxConcurrentCommandsPerSession", MaxConcurrentCommandsPerSession.Get(context));
            }

            if(MaxSessionsPerUser.Expression != null)
            {
                targetCommand.AddParameter("MaxSessionsPerUser", MaxSessionsPerUser.Get(context));
            }

            if(MaxMemoryPerSessionMB.Expression != null)
            {
                targetCommand.AddParameter("MaxMemoryPerSessionMB", MaxMemoryPerSessionMB.Get(context));
            }

            if(MaxProcessesPerSession.Expression != null)
            {
                targetCommand.AddParameter("MaxProcessesPerSession", MaxProcessesPerSession.Get(context));
            }

            if(MaxConcurrentUsers.Expression != null)
            {
                targetCommand.AddParameter("MaxConcurrentUsers", MaxConcurrentUsers.Get(context));
            }

            if(IdleTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("IdleTimeoutSec", IdleTimeoutSec.Get(context));
            }

            if(OutputBufferingMode.Expression != null)
            {
                targetCommand.AddParameter("OutputBufferingMode", OutputBufferingMode.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
