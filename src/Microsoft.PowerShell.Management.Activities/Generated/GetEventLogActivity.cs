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
    /// Activity to invoke the Microsoft.PowerShell.Management\Get-EventLog command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetEventLog : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetEventLog()
        {
            this.DisplayName = "Get-EventLog";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Get-EventLog"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the LogName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> LogName { get; set; }

        /// <summary>
        /// Provides access to the Newest parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Newest { get; set; }

        /// <summary>
        /// Provides access to the After parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> After { get; set; }

        /// <summary>
        /// Provides access to the Before parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.DateTime> Before { get; set; }

        /// <summary>
        /// Provides access to the UserName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> UserName { get; set; }

        /// <summary>
        /// Provides access to the InstanceId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int64[]> InstanceId { get; set; }

        /// <summary>
        /// Provides access to the Index parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> Index { get; set; }

        /// <summary>
        /// Provides access to the EntryType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> EntryType { get; set; }

        /// <summary>
        /// Provides access to the Source parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Source { get; set; }

        /// <summary>
        /// Provides access to the Message parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Message { get; set; }

        /// <summary>
        /// Provides access to the AsBaseObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AsBaseObject { get; set; }

        /// <summary>
        /// Provides access to the List parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> List { get; set; }

        /// <summary>
        /// Provides access to the AsString parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AsString { get; set; }

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

            if(Newest.Expression != null)
            {
                targetCommand.AddParameter("Newest", Newest.Get(context));
            }

            if(After.Expression != null)
            {
                targetCommand.AddParameter("After", After.Get(context));
            }

            if(Before.Expression != null)
            {
                targetCommand.AddParameter("Before", Before.Get(context));
            }

            if(UserName.Expression != null)
            {
                targetCommand.AddParameter("UserName", UserName.Get(context));
            }

            if(InstanceId.Expression != null)
            {
                targetCommand.AddParameter("InstanceId", InstanceId.Get(context));
            }

            if(Index.Expression != null)
            {
                targetCommand.AddParameter("Index", Index.Get(context));
            }

            if(EntryType.Expression != null)
            {
                targetCommand.AddParameter("EntryType", EntryType.Get(context));
            }

            if(Source.Expression != null)
            {
                targetCommand.AddParameter("Source", Source.Get(context));
            }

            if(Message.Expression != null)
            {
                targetCommand.AddParameter("Message", Message.Get(context));
            }

            if(AsBaseObject.Expression != null)
            {
                targetCommand.AddParameter("AsBaseObject", AsBaseObject.Get(context));
            }

            if(List.Expression != null)
            {
                targetCommand.AddParameter("List", List.Get(context));
            }

            if(AsString.Expression != null)
            {
                targetCommand.AddParameter("AsString", AsString.Get(context));
            }

            if(GetIsComputerNameSpecified(context) && (PSRemotingBehavior.Get(context) == RemotingBehavior.Custom))
            {
                targetCommand.AddParameter("ComputerName", PSComputerName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
