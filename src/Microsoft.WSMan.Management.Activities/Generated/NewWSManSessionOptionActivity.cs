//
//    Copyright (C) Microsoft.  All rights reserved.
//
using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.WSMan.Management.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.WSMan.Management\New-WSManSessionOption command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewWSManSessionOption : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewWSManSessionOption()
        {
            this.DisplayName = "New-WSManSessionOption";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.WSMan.Management\\New-WSManSessionOption"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the ProxyAccessType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.WSMan.Management.ProxyAccessType> ProxyAccessType { get; set; }

        /// <summary>
        /// Provides access to the ProxyAuthentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.WSMan.Management.ProxyAuthentication> ProxyAuthentication { get; set; }

        /// <summary>
        /// Provides access to the ProxyCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> ProxyCredential { get; set; }

        /// <summary>
        /// Provides access to the SkipCACheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> SkipCACheck { get; set; }

        /// <summary>
        /// Provides access to the SkipCNCheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> SkipCNCheck { get; set; }

        /// <summary>
        /// Provides access to the SkipRevocationCheck parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> SkipRevocationCheck { get; set; }

        /// <summary>
        /// Provides access to the SPNPort parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> SPNPort { get; set; }

        /// <summary>
        /// Provides access to the OperationTimeout parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> OperationTimeout { get; set; }

        /// <summary>
        /// Provides access to the NoEncryption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NoEncryption { get; set; }

        /// <summary>
        /// Provides access to the UseUTF16 parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseUTF16 { get; set; }


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
            
            if(ProxyAccessType.Expression != null)
            {
                targetCommand.AddParameter("ProxyAccessType", ProxyAccessType.Get(context));
            }

            if(ProxyAuthentication.Expression != null)
            {
                targetCommand.AddParameter("ProxyAuthentication", ProxyAuthentication.Get(context));
            }

            if(ProxyCredential.Expression != null)
            {
                targetCommand.AddParameter("ProxyCredential", ProxyCredential.Get(context));
            }

            if(SkipCACheck.Expression != null)
            {
                targetCommand.AddParameter("SkipCACheck", SkipCACheck.Get(context));
            }

            if(SkipCNCheck.Expression != null)
            {
                targetCommand.AddParameter("SkipCNCheck", SkipCNCheck.Get(context));
            }

            if(SkipRevocationCheck.Expression != null)
            {
                targetCommand.AddParameter("SkipRevocationCheck", SkipRevocationCheck.Get(context));
            }

            if(SPNPort.Expression != null)
            {
                targetCommand.AddParameter("SPNPort", SPNPort.Get(context));
            }

            if(OperationTimeout.Expression != null)
            {
                targetCommand.AddParameter("OperationTimeout", OperationTimeout.Get(context));
            }

            if(NoEncryption.Expression != null)
            {
                targetCommand.AddParameter("NoEncryption", NoEncryption.Get(context));
            }

            if(UseUTF16.Expression != null)
            {
                targetCommand.AddParameter("UseUTF16", UseUTF16.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
