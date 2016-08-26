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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Select-String command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SelectString : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SelectString()
        {
            this.DisplayName = "Select-String";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Select-String"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the Pattern parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Pattern { get; set; }

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
        /// Provides access to the SimpleMatch parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> SimpleMatch { get; set; }

        /// <summary>
        /// Provides access to the CaseSensitive parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CaseSensitive { get; set; }

        /// <summary>
        /// Provides access to the Quiet parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Quiet { get; set; }

        /// <summary>
        /// Provides access to the List parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> List { get; set; }

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
        /// Provides access to the NotMatch parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NotMatch { get; set; }

        /// <summary>
        /// Provides access to the AllMatches parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AllMatches { get; set; }

        /// <summary>
        /// Provides access to the Encoding parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Encoding { get; set; }

        /// <summary>
        /// Provides access to the Context parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> Context { get; set; }


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

            if(Pattern.Expression != null)
            {
                targetCommand.AddParameter("Pattern", Pattern.Get(context));
            }

            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if(LiteralPath.Expression != null)
            {
                targetCommand.AddParameter("LiteralPath", LiteralPath.Get(context));
            }

            if(SimpleMatch.Expression != null)
            {
                targetCommand.AddParameter("SimpleMatch", SimpleMatch.Get(context));
            }

            if(CaseSensitive.Expression != null)
            {
                targetCommand.AddParameter("CaseSensitive", CaseSensitive.Get(context));
            }

            if(Quiet.Expression != null)
            {
                targetCommand.AddParameter("Quiet", Quiet.Get(context));
            }

            if(List.Expression != null)
            {
                targetCommand.AddParameter("List", List.Get(context));
            }

            if(Include.Expression != null)
            {
                targetCommand.AddParameter("Include", Include.Get(context));
            }

            if(Exclude.Expression != null)
            {
                targetCommand.AddParameter("Exclude", Exclude.Get(context));
            }

            if(NotMatch.Expression != null)
            {
                targetCommand.AddParameter("NotMatch", NotMatch.Get(context));
            }

            if(AllMatches.Expression != null)
            {
                targetCommand.AddParameter("AllMatches", AllMatches.Get(context));
            }

            if(Encoding.Expression != null)
            {
                targetCommand.AddParameter("Encoding", Encoding.Get(context));
            }

            if(Context.Expression != null)
            {
                targetCommand.AddParameter("Context", Context.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
