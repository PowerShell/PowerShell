//
//    Copyright (C) Microsoft.  All rights reserved.
//

using Microsoft.PowerShell.Activities;
using System.Management.Automation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Security.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Security\Set-Acl command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SetAcl : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SetAcl()
        {
            this.DisplayName = "Set-Acl";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Security\\Set-Acl"; } }

        // Arguments

        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Path { get; set; }

        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the LiteralPath parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> LiteralPath { get; set; }

        /// <summary>
        /// Provides access to the AclObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object> AclObject { get; set; }

        /// <summary>
        /// Provides access to the SecurityDescriptor parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Security.AccessControl.CommonSecurityDescriptor> SecurityDescriptor { get; set; }

        /// <summary>
        /// Provides access to the CentralAccessPolicy parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CentralAccessPolicy { get; set; }

        /// <summary>
        /// Provides access to the ClearCentralAccessPolicy parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ClearCentralAccessPolicy { get; set; }

        /// <summary>
        /// Provides access to the Passthru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Passthru { get; set; }

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

            if (Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if (InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if (LiteralPath.Expression != null)
            {
                targetCommand.AddParameter("LiteralPath", LiteralPath.Get(context));
            }

            if (AclObject.Expression != null)
            {
                targetCommand.AddParameter("AclObject", AclObject.Get(context));
            }

            if (SecurityDescriptor.Expression != null)
            {
                targetCommand.AddParameter("SecurityDescriptor", SecurityDescriptor.Get(context));
            }

            if (CentralAccessPolicy.Expression != null)
            {
                targetCommand.AddParameter("CentralAccessPolicy", CentralAccessPolicy.Get(context));
            }

            if (ClearCentralAccessPolicy.Expression != null)
            {
                targetCommand.AddParameter("ClearCentralAccessPolicy", ClearCentralAccessPolicy.Get(context));
            }

            if (Passthru.Expression != null)
            {
                targetCommand.AddParameter("Passthru", Passthru.Get(context));
            }

            if (Filter.Expression != null)
            {
                targetCommand.AddParameter("Filter", Filter.Get(context));
            }

            if (Include.Expression != null)
            {
                targetCommand.AddParameter("Include", Include.Get(context));
            }

            if (Exclude.Expression != null)
            {
                targetCommand.AddParameter("Exclude", Exclude.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
