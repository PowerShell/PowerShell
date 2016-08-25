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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Measure-Object command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class MeasureObject : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public MeasureObject()
        {
            this.DisplayName = "Measure-Object";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Measure-Object"; } }
        
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
        public InArgument<System.String[]> Property { get; set; }

        /// <summary>
        /// Provides access to the Sum parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Sum { get; set; }

        /// <summary>
        /// Provides access to the Average parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Average { get; set; }

        /// <summary>
        /// Provides access to the Maximum parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Maximum { get; set; }

        /// <summary>
        /// Provides access to the Minimum parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Minimum { get; set; }

        /// <summary>
        /// Provides access to the Line parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Line { get; set; }

        /// <summary>
        /// Provides access to the Word parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Word { get; set; }

        /// <summary>
        /// Provides access to the Character parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Character { get; set; }

        /// <summary>
        /// Provides access to the IgnoreWhiteSpace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IgnoreWhiteSpace { get; set; }


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

            if(Sum.Expression != null)
            {
                targetCommand.AddParameter("Sum", Sum.Get(context));
            }

            if(Average.Expression != null)
            {
                targetCommand.AddParameter("Average", Average.Get(context));
            }

            if(Maximum.Expression != null)
            {
                targetCommand.AddParameter("Maximum", Maximum.Get(context));
            }

            if(Minimum.Expression != null)
            {
                targetCommand.AddParameter("Minimum", Minimum.Get(context));
            }

            if(Line.Expression != null)
            {
                targetCommand.AddParameter("Line", Line.Get(context));
            }

            if(Word.Expression != null)
            {
                targetCommand.AddParameter("Word", Word.Get(context));
            }

            if(Character.Expression != null)
            {
                targetCommand.AddParameter("Character", Character.Get(context));
            }

            if(IgnoreWhiteSpace.Expression != null)
            {
                targetCommand.AddParameter("IgnoreWhiteSpace", IgnoreWhiteSpace.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
