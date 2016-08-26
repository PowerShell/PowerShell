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
    /// Activity to invoke the Microsoft.PowerShell.Diagnostics\New-WinEvent command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewWinEvent : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewWinEvent()
        {
            this.DisplayName = "New-WinEvent";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Diagnostics\\New-WinEvent"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the ProviderName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ProviderName { get; set; }

        /// <summary>
        /// Provides access to the WinEventId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> WinEventId { get; set; }

        /// <summary>
        /// Provides access to the Version parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Byte> Version { get; set; }

        /// <summary>
        /// Provides access to the Payload parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> Payload { get; set; }


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
            
            if(ProviderName.Expression != null)
            {
                targetCommand.AddParameter("ProviderName", ProviderName.Get(context));
            }

            if(WinEventId.Expression != null)
            {
                targetCommand.AddParameter("Id", WinEventId.Get(context));
            }

            if(Version.Expression != null)
            {
                targetCommand.AddParameter("Version", Version.Get(context));
            }

            if(Payload.Expression != null)
            {
                targetCommand.AddParameter("Payload", Payload.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
