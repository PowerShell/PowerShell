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
    /// Activity to invoke the Microsoft.PowerShell.Management\Start-Process command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class StartProcess : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public StartProcess()
        {
            this.DisplayName = "Start-Process";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Management\\Start-Process"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the FilePath parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> FilePath { get; set; }

        /// <summary>
        /// Provides access to the ArgumentList parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ArgumentList { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the WorkingDirectory parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> WorkingDirectory { get; set; }

        /// <summary>
        /// Provides access to the LoadUserProfile parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> LoadUserProfile { get; set; }

        /// <summary>
        /// Provides access to the NoNewWindow parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NoNewWindow { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the RedirectStandardError parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> RedirectStandardError { get; set; }

        /// <summary>
        /// Provides access to the RedirectStandardInput parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> RedirectStandardInput { get; set; }

        /// <summary>
        /// Provides access to the RedirectStandardOutput parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> RedirectStandardOutput { get; set; }

        /// <summary>
        /// Provides access to the Verb parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Verb { get; set; }

        /// <summary>
        /// Provides access to the WindowStyle parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Diagnostics.ProcessWindowStyle> WindowStyle { get; set; }

        /// <summary>
        /// Provides access to the Wait parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Wait { get; set; }

        /// <summary>
        /// Provides access to the UseNewEnvironment parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseNewEnvironment { get; set; }


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
            
            if(FilePath.Expression != null)
            {
                targetCommand.AddParameter("FilePath", FilePath.Get(context));
            }

            if(ArgumentList.Expression != null)
            {
                targetCommand.AddParameter("ArgumentList", ArgumentList.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(WorkingDirectory.Expression != null)
            {
                targetCommand.AddParameter("WorkingDirectory", WorkingDirectory.Get(context));
            }

            if(LoadUserProfile.Expression != null)
            {
                targetCommand.AddParameter("LoadUserProfile", LoadUserProfile.Get(context));
            }

            if(NoNewWindow.Expression != null)
            {
                targetCommand.AddParameter("NoNewWindow", NoNewWindow.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(RedirectStandardError.Expression != null)
            {
                targetCommand.AddParameter("RedirectStandardError", RedirectStandardError.Get(context));
            }

            if(RedirectStandardInput.Expression != null)
            {
                targetCommand.AddParameter("RedirectStandardInput", RedirectStandardInput.Get(context));
            }

            if(RedirectStandardOutput.Expression != null)
            {
                targetCommand.AddParameter("RedirectStandardOutput", RedirectStandardOutput.Get(context));
            }

            if(Verb.Expression != null)
            {
                targetCommand.AddParameter("Verb", Verb.Get(context));
            }

            if(WindowStyle.Expression != null)
            {
                targetCommand.AddParameter("WindowStyle", WindowStyle.Get(context));
            }

            if(Wait.Expression != null)
            {
                targetCommand.AddParameter("Wait", Wait.Get(context));
            }

            if(UseNewEnvironment.Expression != null)
            {
                targetCommand.AddParameter("UseNewEnvironment", UseNewEnvironment.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
