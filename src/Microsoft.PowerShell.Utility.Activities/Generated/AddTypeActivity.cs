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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Add-Type command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class AddType : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public AddType()
        {
            this.DisplayName = "Add-Type";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Add-Type"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the CodeDomProvider parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.CodeDom.Compiler.CodeDomProvider> CodeDomProvider { get; set; }

        /// <summary>
        /// Provides access to the CompilerParameters parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.CodeDom.Compiler.CompilerParameters> CompilerParameters { get; set; }

        /// <summary>
        /// Provides access to the TypeDefinition parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> TypeDefinition { get; set; }

        /// <summary>
        /// Provides access to the Name parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Name { get; set; }

        /// <summary>
        /// Provides access to the MemberDefinition parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> MemberDefinition { get; set; }

        /// <summary>
        /// Provides access to the Namespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Namespace { get; set; }

        /// <summary>
        /// Provides access to the UsingNamespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> UsingNamespace { get; set; }

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
        /// Provides access to the AssemblyName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> AssemblyName { get; set; }

        /// <summary>
        /// Provides access to the Language parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.Language> Language { get; set; }

        /// <summary>
        /// Provides access to the ReferencedAssemblies parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ReferencedAssemblies { get; set; }

        /// <summary>
        /// Provides access to the OutputAssembly parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> OutputAssembly { get; set; }

        /// <summary>
        /// Provides access to the OutputType parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.OutputAssemblyType> OutputType { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the IgnoreWarnings parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IgnoreWarnings { get; set; }


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
            
            if(CodeDomProvider.Expression != null)
            {
                targetCommand.AddParameter("CodeDomProvider", CodeDomProvider.Get(context));
            }

            if(CompilerParameters.Expression != null)
            {
                targetCommand.AddParameter("CompilerParameters", CompilerParameters.Get(context));
            }

            if(TypeDefinition.Expression != null)
            {
                targetCommand.AddParameter("TypeDefinition", TypeDefinition.Get(context));
            }

            if(Name.Expression != null)
            {
                targetCommand.AddParameter("Name", Name.Get(context));
            }

            if(MemberDefinition.Expression != null)
            {
                targetCommand.AddParameter("MemberDefinition", MemberDefinition.Get(context));
            }

            if(Namespace.Expression != null)
            {
                targetCommand.AddParameter("Namespace", Namespace.Get(context));
            }

            if(UsingNamespace.Expression != null)
            {
                targetCommand.AddParameter("UsingNamespace", UsingNamespace.Get(context));
            }

            if(Path.Expression != null)
            {
                targetCommand.AddParameter("Path", Path.Get(context));
            }

            if(LiteralPath.Expression != null)
            {
                targetCommand.AddParameter("LiteralPath", LiteralPath.Get(context));
            }

            if(AssemblyName.Expression != null)
            {
                targetCommand.AddParameter("AssemblyName", AssemblyName.Get(context));
            }

            if(Language.Expression != null)
            {
                targetCommand.AddParameter("Language", Language.Get(context));
            }

            if(ReferencedAssemblies.Expression != null)
            {
                targetCommand.AddParameter("ReferencedAssemblies", ReferencedAssemblies.Get(context));
            }

            if(OutputAssembly.Expression != null)
            {
                targetCommand.AddParameter("OutputAssembly", OutputAssembly.Get(context));
            }

            if(OutputType.Expression != null)
            {
                targetCommand.AddParameter("OutputType", OutputType.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(IgnoreWarnings.Expression != null)
            {
                targetCommand.AddParameter("IgnoreWarnings", IgnoreWarnings.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
