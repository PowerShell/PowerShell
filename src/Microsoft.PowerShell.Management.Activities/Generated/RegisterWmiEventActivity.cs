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
    /// Activity to invoke the Microsoft.PowerShell.Management\Register-WmiEvent command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class RegisterWmiEvent : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RegisterWmiEvent()
        {
            this.DisplayName = "Register-WmiEvent";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Register-WmiEvent"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Namespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Namespace { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the ComputerName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ComputerName { get; set; }

        /// <summary>
        /// Provides access to the Class parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Class { get; set; }

        /// <summary>
        /// Provides access to the Query parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Query { get; set; }

        /// <summary>
        /// Provides access to the Timeout parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int64> Timeout { get; set; }

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
            
            if(Namespace.Expression != null)
            {
                targetCommand.AddParameter("Namespace", Namespace.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(ComputerName.Expression != null)
            {
                targetCommand.AddParameter("ComputerName", ComputerName.Get(context));
            }

            if(Class.Expression != null)
            {
                targetCommand.AddParameter("Class", Class.Get(context));
            }

            if(Query.Expression != null)
            {
                targetCommand.AddParameter("Query", Query.Get(context));
            }

            if(Timeout.Expression != null)
            {
                targetCommand.AddParameter("Timeout", Timeout.Get(context));
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
