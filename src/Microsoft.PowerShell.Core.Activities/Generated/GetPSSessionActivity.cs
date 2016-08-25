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
    /// Activity to invoke the Microsoft.PowerShell.Core\Get-PSSession command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class GetPSSession : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public GetPSSession()
        {
            this.DisplayName = "Get-PSSession";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Get-PSSession"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the ComputerName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ComputerName { get; set; }

        /// <summary>
        /// Provides access to the ApplicationName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ApplicationName { get; set; }

        /// <summary>
        /// Provides access to the ConnectionUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri[]> ConnectionUri { get; set; }

        /// <summary>
        /// Provides access to the ConfigurationName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ConfigurationName { get; set; }

        /// <summary>
        /// Provides access to the AllowRedirection parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> AllowRedirection { get; set; }

        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Name { get; set; }

        /// <summary>
        /// Provides access to the InstanceId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid[]> InstanceId { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the Authentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Runspaces.AuthenticationMechanism> Authentication { get; set; }

        /// <summary>
        /// Provides access to the CertificateThumbprint parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CertificateThumbprint { get; set; }

        /// <summary>
        /// Provides access to the Port parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Port { get; set; }

        /// <summary>
        /// Provides access to the UseSSL parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseSSL { get; set; }

        /// <summary>
        /// Provides access to the ThrottleLimit parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> ThrottleLimit { get; set; }

        /// <summary>
        /// Provides access to the State parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.SessionFilterState> State { get; set; }

        /// <summary>
        /// Provides access to the SessionOption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.Remoting.PSSessionOption> SessionOption { get; set; }

        /// <summary>
        /// Provides access to the PSSessionId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32[]> PSSessionId { get; set; }

        /// <summary>
        /// Provides access to the ContainerId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ContainerId { get; set; }

        /// <summary>
        /// Provides access to the VMId parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid[]> VMId { get; set; }

        /// <summary>
        /// Provides access to the VMName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> VMName { get; set; }


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
            
            if(ComputerName.Expression != null)
            {
                targetCommand.AddParameter("ComputerName", ComputerName.Get(context));
            }

            if(ApplicationName.Expression != null)
            {
                targetCommand.AddParameter("ApplicationName", ApplicationName.Get(context));
            }

            if(ConnectionUri.Expression != null)
            {
                targetCommand.AddParameter("ConnectionUri", ConnectionUri.Get(context));
            }

            if(ConfigurationName.Expression != null)
            {
                targetCommand.AddParameter("ConfigurationName", ConfigurationName.Get(context));
            }

            if(AllowRedirection.Expression != null)
            {
                targetCommand.AddParameter("AllowRedirection", AllowRedirection.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(InstanceId.Expression != null)
            {
                targetCommand.AddParameter("InstanceId", InstanceId.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(Authentication.Expression != null)
            {
                targetCommand.AddParameter("Authentication", Authentication.Get(context));
            }

            if(CertificateThumbprint.Expression != null)
            {
                targetCommand.AddParameter("CertificateThumbprint", CertificateThumbprint.Get(context));
            }

            if(Port.Expression != null)
            {
                targetCommand.AddParameter("Port", Port.Get(context));
            }

            if(UseSSL.Expression != null)
            {
                targetCommand.AddParameter("UseSSL", UseSSL.Get(context));
            }

            if(ThrottleLimit.Expression != null)
            {
                targetCommand.AddParameter("ThrottleLimit", ThrottleLimit.Get(context));
            }

            if(State.Expression != null)
            {
                targetCommand.AddParameter("State", State.Get(context));
            }

            if(SessionOption.Expression != null)
            {
                targetCommand.AddParameter("SessionOption", SessionOption.Get(context));
            }

            if(PSSessionId.Expression != null)
            {
                targetCommand.AddParameter("Id", PSSessionId.Get(context));
            }

            if(ContainerId.Expression != null)
            {
                targetCommand.AddParameter("ContainerId", ContainerId.Get(context));
            }

            if(VMId.Expression != null)
            {
                targetCommand.AddParameter("VMId", VMId.Get(context));
            }

            if(VMName.Expression != null)
            {
                targetCommand.AddParameter("VMName", VMName.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
