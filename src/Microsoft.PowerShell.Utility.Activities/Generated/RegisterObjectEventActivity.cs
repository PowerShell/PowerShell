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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Register-ObjectEvent command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class RegisterObjectEvent : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RegisterObjectEvent()
        {
            this.DisplayName = "Register-ObjectEvent";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Register-ObjectEvent"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the EventName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> EventName { get; set; }

        /// <summary>
        /// Provides access to the SourceIdentifier parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> SourceIdentifier { get; set; }

        /// <summary>
        /// Provides access to the Action parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock> Action { get; set; }

        /// <summary>
        /// Provides access to the MessageData parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> MessageData { get; set; }

        /// <summary>
        /// Provides access to the SupportEvent parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> SupportEvent { get; set; }

        /// <summary>
        /// Provides access to the Forward parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Forward { get; set; }

        /// <summary>
        /// Provides access to the MaxTriggerCount parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> MaxTriggerCount { get; set; }


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
            
            if(InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if(EventName.Expression != null)
            {
                targetCommand.AddParameter("EventName", EventName.Get(context));
            }

            if(SourceIdentifier.Expression != null)
            {
                targetCommand.AddParameter("SourceIdentifier", SourceIdentifier.Get(context));
            }

            if(Action.Expression != null)
            {
                targetCommand.AddParameter("Action", Action.Get(context));
            }

            if(MessageData.Expression != null)
            {
                targetCommand.AddParameter("MessageData", MessageData.Get(context));
            }

            if(SupportEvent.Expression != null)
            {
                targetCommand.AddParameter("SupportEvent", SupportEvent.Get(context));
            }

            if(Forward.Expression != null)
            {
                targetCommand.AddParameter("Forward", Forward.Get(context));
            }

            if(MaxTriggerCount.Expression != null)
            {
                targetCommand.AddParameter("MaxTriggerCount", MaxTriggerCount.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
