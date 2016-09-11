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
    /// Activity to invoke the Microsoft.PowerShell.Core\Get-Help command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetHelp : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetHelp()
        {
            this.DisplayName = "Get-Help";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Get-Help"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Path { get; set; }

        /// <summary>
        /// Provides access to the Category parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Category { get; set; }

        /// <summary>
        /// Provides access to the Component parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Component { get; set; }

        /// <summary>
        /// Provides access to the Functionality parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Functionality { get; set; }

        /// <summary>
        /// Provides access to the Role parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Role { get; set; }

        /// <summary>
        /// Provides access to the Detailed parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Detailed { get; set; }

        /// <summary>
        /// Provides access to the Full parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Full { get; set; }

        /// <summary>
        /// Provides access to the Examples parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Examples { get; set; }

        /// <summary>
        /// Provides access to the Parameter parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Parameter { get; set; }

        /// <summary>
        /// Provides access to the Online parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Online { get; set; }

        /// <summary>
        /// Provides access to the ShowWindow parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ShowWindow { get; set; }


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
            
            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if(Category.Expression != null)
            {
                targetCommand.AddParameter("Category", Category.Get(context));
            }

            if(Component.Expression != null)
            {
                targetCommand.AddParameter("Component", Component.Get(context));
            }

            if(Functionality.Expression != null)
            {
                targetCommand.AddParameter("Functionality", Functionality.Get(context));
            }

            if(Role.Expression != null)
            {
                targetCommand.AddParameter("Role", Role.Get(context));
            }

            if(Detailed.Expression != null)
            {
                targetCommand.AddParameter("Detailed", Detailed.Get(context));
            }

            if(Full.Expression != null)
            {
                targetCommand.AddParameter("Full", Full.Get(context));
            }

            if(Examples.Expression != null)
            {
                targetCommand.AddParameter("Examples", Examples.Get(context));
            }

            if(Parameter.Expression != null)
            {
                targetCommand.AddParameter("Parameter", Parameter.Get(context));
            }

            if(Online.Expression != null)
            {
                targetCommand.AddParameter("Online", Online.Get(context));
            }

            if(ShowWindow.Expression != null)
            {
                targetCommand.AddParameter("ShowWindow", ShowWindow.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
