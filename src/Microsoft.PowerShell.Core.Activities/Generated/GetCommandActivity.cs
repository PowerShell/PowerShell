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
    /// Activity to invoke the Microsoft.PowerShell.Core\Get-Command command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetCommand : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetCommand()
        {
            this.DisplayName = "Get-Command";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Get-Command"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Name { get; set; }

        /// <summary>
        /// Provides access to the Verb parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Verb { get; set; }

        /// <summary>
        /// Provides access to the Noun parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Noun { get; set; }

        /// <summary>
        /// Provides access to the Module parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Module { get; set; }

        /// <summary>
        /// Provides access to the FullyQualifiedModule parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.ModuleSpecification[]> FullyQualifiedModule { get; set; }

        /// <summary>
        /// Provides access to the CommandType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.CommandTypes> CommandType { get; set; }

        /// <summary>
        /// Provides access to the TotalCount parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> TotalCount { get; set; }

        /// <summary>
        /// Provides access to the Syntax parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Syntax { get; set; }

        /// <summary>
        /// Provides access to the ShowCommandInfo parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ShowCommandInfo { get; set; }

        /// <summary>
        /// Provides access to the ArgumentList parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> ArgumentList { get; set; }

        /// <summary>
        /// Provides access to the All parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> All { get; set; }

        /// <summary>
        /// Provides access to the ListImported parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ListImported { get; set; }

        /// <summary>
        /// Provides access to the ParameterName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ParameterName { get; set; }

        /// <summary>
        /// Provides access to the ParameterType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSTypeName[]> ParameterType { get; set; }


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

            if(Verb.Expression != null)
            {
                targetCommand.AddParameter("Verb", Verb.Get(context));
            }

            if(Noun.Expression != null)
            {
                targetCommand.AddParameter("Noun", Noun.Get(context));
            }

            if(Module.Expression != null)
            {
                targetCommand.AddParameter("Module", Module.Get(context));
            }

            if(FullyQualifiedModule.Expression != null)
            {
                targetCommand.AddParameter("FullyQualifiedModule", FullyQualifiedModule.Get(context));
            }

            if(CommandType.Expression != null)
            {
                targetCommand.AddParameter("CommandType", CommandType.Get(context));
            }

            if(TotalCount.Expression != null)
            {
                targetCommand.AddParameter("TotalCount", TotalCount.Get(context));
            }

            if(Syntax.Expression != null)
            {
                targetCommand.AddParameter("Syntax", Syntax.Get(context));
            }

            if(ShowCommandInfo.Expression != null)
            {
                targetCommand.AddParameter("ShowCommandInfo", ShowCommandInfo.Get(context));
            }

            if(ArgumentList.Expression != null)
            {
                targetCommand.AddParameter("ArgumentList", ArgumentList.Get(context));
            }

            if(All.Expression != null)
            {
                targetCommand.AddParameter("All", All.Get(context));
            }

            if(ListImported.Expression != null)
            {
                targetCommand.AddParameter("ListImported", ListImported.Get(context));
            }

            if(ParameterName.Expression != null)
            {
                targetCommand.AddParameter("ParameterName", ParameterName.Get(context));
            }

            if(ParameterType.Expression != null)
            {
                targetCommand.AddParameter("ParameterType", ParameterType.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
