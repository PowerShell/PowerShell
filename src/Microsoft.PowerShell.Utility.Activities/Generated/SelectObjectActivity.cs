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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Select-Object command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SelectObject : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SelectObject()
        {
            this.DisplayName = "Select-Object";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Select-Object"; } }
        
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
        /// Provides access to the ExcludeProperty parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ExcludeProperty { get; set; }

        /// <summary>
        /// Provides access to the ExpandProperty parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ExpandProperty { get; set; }

        /// <summary>
        /// Provides access to the Unique parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Unique { get; set; }

        /// <summary>
        /// Provides access to the Last parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Last { get; set; }

        /// <summary>
        /// Provides access to the First parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> First { get; set; }

        /// <summary>
        /// Provides access to the Skip parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Skip { get; set; }

        /// <summary>
        /// Provides access to the SkipLast parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> SkipLast { get; set; }

        /// <summary>
        /// Provides access to the Wait parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Wait { get; set; }

        /// <summary>
        /// Provides access to the Index parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> Index { get; set; }


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

            if(ExcludeProperty.Expression != null)
            {
                targetCommand.AddParameter("ExcludeProperty", ExcludeProperty.Get(context));
            }

            if(ExpandProperty.Expression != null)
            {
                targetCommand.AddParameter("ExpandProperty", ExpandProperty.Get(context));
            }

            if(Unique.Expression != null)
            {
                targetCommand.AddParameter("Unique", Unique.Get(context));
            }

            if(Last.Expression != null)
            {
                targetCommand.AddParameter("Last", Last.Get(context));
            }

            if(First.Expression != null)
            {
                targetCommand.AddParameter("First", First.Get(context));
            }

            if(Skip.Expression != null)
            {
                targetCommand.AddParameter("Skip", Skip.Get(context));
            }

            if(SkipLast.Expression != null)
            {
                targetCommand.AddParameter("SkipLast", SkipLast.Get(context));
            }

            if(Wait.Expression != null)
            {
                targetCommand.AddParameter("Wait", Wait.Get(context));
            }

            if(Index.Expression != null)
            {
                targetCommand.AddParameter("Index", Index.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
