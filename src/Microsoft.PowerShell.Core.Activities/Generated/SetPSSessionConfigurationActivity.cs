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
    /// Activity to invoke the Microsoft.PowerShell.Core\Set-PSSessionConfiguration command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SetPSSessionConfiguration : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SetPSSessionConfiguration()
        {
            this.DisplayName = "Set-PSSessionConfiguration";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Set-PSSessionConfiguration"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the AssemblyName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> AssemblyName { get; set; }

        /// <summary>
        /// Provides access to the ApplicationBase parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ApplicationBase { get; set; }

        /// <summary>
        /// Provides access to the ConfigurationTypeName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ConfigurationTypeName { get; set; }

        /// <summary>
        /// Provides access to the RunAsCredential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> RunAsCredential { get; set; }

        /// <summary>
        /// Provides access to the ThreadApartmentState parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Threading.ApartmentState> ThreadApartmentState { get; set; }

        /// <summary>
        /// Provides access to the ThreadOptions parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Runspaces.PSThreadOptions> ThreadOptions { get; set; }

        /// <summary>
        /// Provides access to the AccessMode parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Runspaces.PSSessionConfigurationAccessMode> AccessMode { get; set; }

        /// <summary>
        /// Provides access to the UseSharedProcess parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseSharedProcess { get; set; }

        /// <summary>
        /// Provides access to the StartupScript parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> StartupScript { get; set; }

        /// <summary>
        /// Provides access to the MaximumReceivedDataSizePerCommandMB parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Double>> MaximumReceivedDataSizePerCommandMB { get; set; }

        /// <summary>
        /// Provides access to the MaximumReceivedObjectSizeMB parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Nullable<System.Double>> MaximumReceivedObjectSizeMB { get; set; }

        /// <summary>
        /// Provides access to the SecurityDescriptorSddl parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> SecurityDescriptorSddl { get; set; }

        /// <summary>
        /// Provides access to the ShowSecurityDescriptorUI parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ShowSecurityDescriptorUI { get; set; }

        /// <summary>
        /// Provides access to the Force parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Force { get; set; }

        /// <summary>
        /// Provides access to the NoServiceRestart parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NoServiceRestart { get; set; }

        /// <summary>
        /// Provides access to the PSVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> PSVersion { get; set; }

        /// <summary>
        /// Provides access to the SessionTypeOption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSSessionTypeOption> SessionTypeOption { get; set; }

        /// <summary>
        /// Provides access to the TransportOption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSTransportOption> TransportOption { get; set; }

        /// <summary>
        /// Provides access to the ModulesToImport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> ModulesToImport { get; set; }

        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Path { get; set; }


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

            if(AssemblyName.Expression != null)
            {
                targetCommand.AddParameter("AssemblyName", AssemblyName.Get(context));
            }

            if(ApplicationBase.Expression != null)
            {
                targetCommand.AddParameter("ApplicationBase", ApplicationBase.Get(context));
            }

            if(ConfigurationTypeName.Expression != null)
            {
                targetCommand.AddParameter("ConfigurationTypeName", ConfigurationTypeName.Get(context));
            }

            if(RunAsCredential.Expression != null)
            {
                targetCommand.AddParameter("RunAsCredential", RunAsCredential.Get(context));
            }

            if(ThreadApartmentState.Expression != null)
            {
                targetCommand.AddParameter("ThreadApartmentState", ThreadApartmentState.Get(context));
            }

            if(ThreadOptions.Expression != null)
            {
                targetCommand.AddParameter("ThreadOptions", ThreadOptions.Get(context));
            }

            if(AccessMode.Expression != null)
            {
                targetCommand.AddParameter("AccessMode", AccessMode.Get(context));
            }

            if(UseSharedProcess.Expression != null)
            {
                targetCommand.AddParameter("UseSharedProcess", UseSharedProcess.Get(context));
            }

            if(StartupScript.Expression != null)
            {
                targetCommand.AddParameter("StartupScript", StartupScript.Get(context));
            }

            if(MaximumReceivedDataSizePerCommandMB.Expression != null)
            {
                targetCommand.AddParameter("MaximumReceivedDataSizePerCommandMB", MaximumReceivedDataSizePerCommandMB.Get(context));
            }

            if(MaximumReceivedObjectSizeMB.Expression != null)
            {
                targetCommand.AddParameter("MaximumReceivedObjectSizeMB", MaximumReceivedObjectSizeMB.Get(context));
            }

            if(SecurityDescriptorSddl.Expression != null)
            {
                targetCommand.AddParameter("SecurityDescriptorSddl", SecurityDescriptorSddl.Get(context));
            }

            if(ShowSecurityDescriptorUI.Expression != null)
            {
                targetCommand.AddParameter("ShowSecurityDescriptorUI", ShowSecurityDescriptorUI.Get(context));
            }

            if(Force.Expression != null)
            {
                targetCommand.AddParameter("Force", Force.Get(context));
            }

            if(NoServiceRestart.Expression != null)
            {
                targetCommand.AddParameter("NoServiceRestart", NoServiceRestart.Get(context));
            }

            if(PSVersion.Expression != null)
            {
                targetCommand.AddParameter("PSVersion", PSVersion.Get(context));
            }

            if(SessionTypeOption.Expression != null)
            {
                targetCommand.AddParameter("SessionTypeOption", SessionTypeOption.Get(context));
            }

            if(TransportOption.Expression != null)
            {
                targetCommand.AddParameter("TransportOption", TransportOption.Get(context));
            }

            if(ModulesToImport.Expression != null)
            {
                targetCommand.AddParameter("ModulesToImport", ModulesToImport.Get(context));
            }

            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
