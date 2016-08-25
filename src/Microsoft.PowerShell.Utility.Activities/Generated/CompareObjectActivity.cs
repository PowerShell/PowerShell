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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Compare-Object command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class CompareObject : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public CompareObject()
        {
            this.DisplayName = "Compare-Object";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Compare-Object"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the ReferenceObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject[]> ReferenceObject { get; set; }

        /// <summary>
        /// Provides access to the DifferenceObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject[]> DifferenceObject { get; set; }

        /// <summary>
        /// Provides access to the SyncWindow parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> SyncWindow { get; set; }

        /// <summary>
        /// Provides access to the Property parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> Property { get; set; }

        /// <summary>
        /// Provides access to the ExcludeDifferent parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ExcludeDifferent { get; set; }

        /// <summary>
        /// Provides access to the IncludeEqual parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IncludeEqual { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the Culture parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Culture { get; set; }

        /// <summary>
        /// Provides access to the CaseSensitive parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CaseSensitive { get; set; }


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
            
            if(ReferenceObject.Expression != null)
            {
                targetCommand.AddParameter("ReferenceObject", ReferenceObject.Get(context));
            }

            if(DifferenceObject.Expression != null)
            {
                targetCommand.AddParameter("DifferenceObject", DifferenceObject.Get(context));
            }

            if(SyncWindow.Expression != null)
            {
                targetCommand.AddParameter("SyncWindow", SyncWindow.Get(context));
            }

            if(Property.Expression != null)
            {
                targetCommand.AddParameter("Property", Property.Get(context));
            }

            if(ExcludeDifferent.Expression != null)
            {
                targetCommand.AddParameter("ExcludeDifferent", ExcludeDifferent.Get(context));
            }

            if(IncludeEqual.Expression != null)
            {
                targetCommand.AddParameter("IncludeEqual", IncludeEqual.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(Culture.Expression != null)
            {
                targetCommand.AddParameter("Culture", Culture.Get(context));
            }

            if(CaseSensitive.Expression != null)
            {
                targetCommand.AddParameter("CaseSensitive", CaseSensitive.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
