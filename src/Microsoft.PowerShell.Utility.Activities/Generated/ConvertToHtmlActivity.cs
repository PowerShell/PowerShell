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
    /// Activity to invoke the Microsoft.PowerShell.Utility\ConvertTo-Html command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class ConvertToHtml : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public ConvertToHtml()
        {
            this.DisplayName = "ConvertTo-Html";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\ConvertTo-Html"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the Property parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> Property { get; set; }

        /// <summary>
        /// Provides access to the Body parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Body { get; set; }

        /// <summary>
        /// Provides access to the Head parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Head { get; set; }

        /// <summary>
        /// Provides access to the Title parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Title { get; set; }

        /// <summary>
        /// Provides access to the As parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> As { get; set; }

        /// <summary>
        /// Provides access to the CssUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> CssUri { get; set; }

        /// <summary>
        /// Provides access to the Fragment parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Fragment { get; set; }

        /// <summary>
        /// Provides access to the PostContent parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> PostContent { get; set; }

        /// <summary>
        /// Provides access to the PreContent parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> PreContent { get; set; }


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

            if(Property.Expression != null)
            {
                targetCommand.AddParameter("Property", Property.Get(context));
            }

            if(Body.Expression != null)
            {
                targetCommand.AddParameter("Body", Body.Get(context));
            }

            if(Head.Expression != null)
            {
                targetCommand.AddParameter("Head", Head.Get(context));
            }

            if(Title.Expression != null)
            {
                targetCommand.AddParameter("Title", Title.Get(context));
            }

            if(As.Expression != null)
            {
                targetCommand.AddParameter("As", As.Get(context));
            }

            if(CssUri.Expression != null)
            {
                targetCommand.AddParameter("CssUri", CssUri.Get(context));
            }

            if(Fragment.Expression != null)
            {
                targetCommand.AddParameter("Fragment", Fragment.Get(context));
            }

            if(PostContent.Expression != null)
            {
                targetCommand.AddParameter("PostContent", PostContent.Get(context));
            }

            if(PreContent.Expression != null)
            {
                targetCommand.AddParameter("PreContent", PreContent.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
