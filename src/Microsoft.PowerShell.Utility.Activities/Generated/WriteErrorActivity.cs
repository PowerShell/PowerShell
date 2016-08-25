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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Write-Error command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class WriteError : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public WriteError()
        {
            this.DisplayName = "Write-Error";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Write-Error"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Exception parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Exception> Exception { get; set; }

        /// <summary>
        /// Provides access to the Message parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Message { get; set; }

        /// <summary>
        /// Provides access to the ErrorRecord parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ErrorRecord> ErrorRecord { get; set; }

        /// <summary>
        /// Provides access to the Category parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ErrorCategory> Category { get; set; }

        /// <summary>
        /// Provides access to the ErrorId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ErrorId { get; set; }

        /// <summary>
        /// Provides access to the TargetObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object> TargetObject { get; set; }

        /// <summary>
        /// Provides access to the RecommendedAction parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> RecommendedAction { get; set; }

        /// <summary>
        /// Provides access to the CategoryActivity parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CategoryActivity { get; set; }

        /// <summary>
        /// Provides access to the CategoryReason parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CategoryReason { get; set; }

        /// <summary>
        /// Provides access to the CategoryTargetName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CategoryTargetName { get; set; }

        /// <summary>
        /// Provides access to the CategoryTargetType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CategoryTargetType { get; set; }


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
            
            if(Exception.Expression != null)
            {
                targetCommand.AddParameter("Exception", Exception.Get(context));
            }

            if(Message.Expression != null)
            {
                targetCommand.AddParameter("Message", Message.Get(context));
            }

            if(ErrorRecord.Expression != null)
            {
                targetCommand.AddParameter("ErrorRecord", ErrorRecord.Get(context));
            }

            if(Category.Expression != null)
            {
                targetCommand.AddParameter("Category", Category.Get(context));
            }

            if(ErrorId.Expression != null)
            {
                targetCommand.AddParameter("ErrorId", ErrorId.Get(context));
            }

            if(TargetObject.Expression != null)
            {
                targetCommand.AddParameter("TargetObject", TargetObject.Get(context));
            }

            if(RecommendedAction.Expression != null)
            {
                targetCommand.AddParameter("RecommendedAction", RecommendedAction.Get(context));
            }

            if(CategoryActivity.Expression != null)
            {
                targetCommand.AddParameter("CategoryActivity", CategoryActivity.Get(context));
            }

            if(CategoryReason.Expression != null)
            {
                targetCommand.AddParameter("CategoryReason", CategoryReason.Get(context));
            }

            if(CategoryTargetName.Expression != null)
            {
                targetCommand.AddParameter("CategoryTargetName", CategoryTargetName.Get(context));
            }

            if(CategoryTargetType.Expression != null)
            {
                targetCommand.AddParameter("CategoryTargetType", CategoryTargetType.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
