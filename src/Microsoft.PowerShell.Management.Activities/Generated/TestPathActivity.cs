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
    /// Activity to invoke the Microsoft.PowerShell.Management\Test-Path command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class TestPath : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public TestPath()
        {
            this.DisplayName = "Test-Path";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Test-Path"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Path { get; set; }

        /// <summary>
        /// Provides access to the LiteralPath parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> LiteralPath { get; set; }

        /// <summary>
        /// Provides access to the Filter parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Filter { get; set; }

        /// <summary>
        /// Provides access to the Include parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Include { get; set; }

        /// <summary>
        /// Provides access to the Exclude parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Exclude { get; set; }

        /// <summary>
        /// Provides access to the PathType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.TestPathType> PathType { get; set; }

        /// <summary>
        /// Provides access to the IsValid parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IsValid { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the OlderThan parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.DateTime>> OlderThan { get; set; }

        /// <summary>
        /// Provides access to the NewerThan parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.DateTime>> NewerThan { get; set; }


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
            
            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if(LiteralPath.Expression != null)
            {
                targetCommand.AddParameter("LiteralPath", LiteralPath.Get(context));
            }

            if(Filter.Expression != null)
            {
                targetCommand.AddParameter("Filter", Filter.Get(context));
            }

            if(Include.Expression != null)
            {
                targetCommand.AddParameter("Include", Include.Get(context));
            }

            if(Exclude.Expression != null)
            {
                targetCommand.AddParameter("Exclude", Exclude.Get(context));
            }

            if(PathType.Expression != null)
            {
                targetCommand.AddParameter("PathType", PathType.Get(context));
            }

            if(IsValid.Expression != null)
            {
                targetCommand.AddParameter("IsValid", IsValid.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(OlderThan.Expression != null)
            {
                targetCommand.AddParameter("OlderThan", OlderThan.Get(context));
            }

            if(NewerThan.Expression != null)
            {
                targetCommand.AddParameter("NewerThan", NewerThan.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
