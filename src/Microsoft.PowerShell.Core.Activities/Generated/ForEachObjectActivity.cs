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
    /// Activity to invoke the Microsoft.PowerShell.Core\ForEach-Object command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class ForEachObject : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public ForEachObject()
        {
            this.DisplayName = "ForEach-Object";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\ForEach-Object"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the Begin parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock> Begin { get; set; }

        /// <summary>
        /// Provides access to the Process parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock[]> Process { get; set; }

        /// <summary>
        /// Provides access to the End parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock> End { get; set; }

        /// <summary>
        /// Provides access to the RemainingScripts parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock[]> RemainingScripts { get; set; }

        /// <summary>
        /// Provides access to the MemberName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> MemberName { get; set; }

        /// <summary>
        /// Provides access to the ArgumentList parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> ArgumentList { get; set; }


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

            if(Begin.Expression != null)
            {
                targetCommand.AddParameter("Begin", Begin.Get(context));
            }

            if(Process.Expression != null)
            {
                targetCommand.AddParameter("Process", Process.Get(context));
            }

            if(End.Expression != null)
            {
                targetCommand.AddParameter("End", End.Get(context));
            }

            if(RemainingScripts.Expression != null)
            {
                targetCommand.AddParameter("RemainingScripts", RemainingScripts.Get(context));
            }

            if(MemberName.Expression != null)
            {
                targetCommand.AddParameter("MemberName", MemberName.Get(context));
            }

            if(ArgumentList.Expression != null)
            {
                targetCommand.AddParameter("ArgumentList", ArgumentList.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
