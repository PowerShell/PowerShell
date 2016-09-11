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
    /// Activity to invoke the Microsoft.PowerShell.Core\New-ModuleManifest command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewModuleManifest : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewModuleManifest()
        {
            this.DisplayName = "New-ModuleManifest";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\New-ModuleManifest"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Path parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Path { get; set; }

        /// <summary>
        /// Provides access to the NestedModules parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> NestedModules { get; set; }

        /// <summary>
        /// Provides access to the Guid parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Guid> Guid { get; set; }

        /// <summary>
        /// Provides access to the Author parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Author { get; set; }

        /// <summary>
        /// Provides access to the CompanyName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> CompanyName { get; set; }

        /// <summary>
        /// Provides access to the Copyright parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Copyright { get; set; }

        /// <summary>
        /// Provides access to the RootModule parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> RootModule { get; set; }

        /// <summary>
        /// Provides access to the ModuleVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> ModuleVersion { get; set; }

        /// <summary>
        /// Provides access to the Description parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Description { get; set; }

        /// <summary>
        /// Provides access to the ProcessorArchitecture parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Reflection.ProcessorArchitecture> ProcessorArchitecture { get; set; }

        /// <summary>
        /// Provides access to the PowerShellVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> PowerShellVersion { get; set; }

        /// <summary>
        /// Provides access to the ClrVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> ClrVersion { get; set; }

        /// <summary>
        /// Provides access to the DotNetFrameworkVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> DotNetFrameworkVersion { get; set; }

        /// <summary>
        /// Provides access to the PowerShellHostName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> PowerShellHostName { get; set; }

        /// <summary>
        /// Provides access to the PowerShellHostVersion parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Version> PowerShellHostVersion { get; set; }

        /// <summary>
        /// Provides access to the RequiredModules parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> RequiredModules { get; set; }

        /// <summary>
        /// Provides access to the TypesToProcess parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> TypesToProcess { get; set; }

        /// <summary>
        /// Provides access to the FormatsToProcess parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> FormatsToProcess { get; set; }

        /// <summary>
        /// Provides access to the ScriptsToProcess parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> ScriptsToProcess { get; set; }

        /// <summary>
        /// Provides access to the RequiredAssemblies parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> RequiredAssemblies { get; set; }

        /// <summary>
        /// Provides access to the FileList parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> FileList { get; set; }

        /// <summary>
        /// Provides access to the ModuleList parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object[]> ModuleList { get; set; }

        /// <summary>
        /// Provides access to the FunctionsToExport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> FunctionsToExport { get; set; }

        /// <summary>
        /// Provides access to the AliasesToExport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> AliasesToExport { get; set; }

        /// <summary>
        /// Provides access to the VariablesToExport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> VariablesToExport { get; set; }

        /// <summary>
        /// Provides access to the CmdletsToExport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> CmdletsToExport { get; set; }

        /// <summary>
        /// Provides access to the DscResourcesToExport parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> DscResourcesToExport { get; set; }

        /// <summary>
        /// Provides access to the PrivateData parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object> PrivateData { get; set; }

        /// <summary>
        /// Provides access to the Tags parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Tags { get; set; }

        /// <summary>
        /// Provides access to the ProjectUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> ProjectUri { get; set; }

        /// <summary>
        /// Provides access to the LicenseUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> LicenseUri { get; set; }

        /// <summary>
        /// Provides access to the IconUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Uri> IconUri { get; set; }

        /// <summary>
        /// Provides access to the ReleaseNotes parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ReleaseNotes { get; set; }

        /// <summary>
        /// Provides access to the HelpInfoUri parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> HelpInfoUri { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Provides access to the DefaultCommandPrefix parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> DefaultCommandPrefix { get; set; }


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

            if(NestedModules.Expression != null)
            {
                targetCommand.AddParameter("NestedModules", NestedModules.Get(context));
            }

            if(Guid.Expression != null)
            {
                targetCommand.AddParameter("Guid", Guid.Get(context));
            }

            if(Author.Expression != null)
            {
                targetCommand.AddParameter("Author", Author.Get(context));
            }

            if(CompanyName.Expression != null)
            {
                targetCommand.AddParameter("CompanyName", CompanyName.Get(context));
            }

            if(Copyright.Expression != null)
            {
                targetCommand.AddParameter("Copyright", Copyright.Get(context));
            }

            if(RootModule.Expression != null)
            {
                targetCommand.AddParameter("RootModule", RootModule.Get(context));
            }

            if(ModuleVersion.Expression != null)
            {
                targetCommand.AddParameter("ModuleVersion", ModuleVersion.Get(context));
            }

            if(Description.Expression != null)
            {
                targetCommand.AddParameter("Description", Description.Get(context));
            }

            if(ProcessorArchitecture.Expression != null)
            {
                targetCommand.AddParameter("ProcessorArchitecture", ProcessorArchitecture.Get(context));
            }

            if(PowerShellVersion.Expression != null)
            {
                targetCommand.AddParameter("PowerShellVersion", PowerShellVersion.Get(context));
            }

            if(ClrVersion.Expression != null)
            {
                targetCommand.AddParameter("ClrVersion", ClrVersion.Get(context));
            }

            if(DotNetFrameworkVersion.Expression != null)
            {
                targetCommand.AddParameter("DotNetFrameworkVersion", DotNetFrameworkVersion.Get(context));
            }

            if(PowerShellHostName.Expression != null)
            {
                targetCommand.AddParameter("PowerShellHostName", PowerShellHostName.Get(context));
            }

            if(PowerShellHostVersion.Expression != null)
            {
                targetCommand.AddParameter("PowerShellHostVersion", PowerShellHostVersion.Get(context));
            }

            if(RequiredModules.Expression != null)
            {
                targetCommand.AddParameter("RequiredModules", RequiredModules.Get(context));
            }

            if(TypesToProcess.Expression != null)
            {
                targetCommand.AddParameter("TypesToProcess", TypesToProcess.Get(context));
            }

            if(FormatsToProcess.Expression != null)
            {
                targetCommand.AddParameter("FormatsToProcess", FormatsToProcess.Get(context));
            }

            if(ScriptsToProcess.Expression != null)
            {
                targetCommand.AddParameter("ScriptsToProcess", ScriptsToProcess.Get(context));
            }

            if(RequiredAssemblies.Expression != null)
            {
                targetCommand.AddParameter("RequiredAssemblies", RequiredAssemblies.Get(context));
            }

            if(FileList.Expression != null)
            {
                targetCommand.AddParameter("FileList", FileList.Get(context));
            }

            if(ModuleList.Expression != null)
            {
                targetCommand.AddParameter("ModuleList", ModuleList.Get(context));
            }

            if(FunctionsToExport.Expression != null)
            {
                targetCommand.AddParameter("FunctionsToExport", FunctionsToExport.Get(context));
            }

            if(AliasesToExport.Expression != null)
            {
                targetCommand.AddParameter("AliasesToExport", AliasesToExport.Get(context));
            }

            if(VariablesToExport.Expression != null)
            {
                targetCommand.AddParameter("VariablesToExport", VariablesToExport.Get(context));
            }

            if(CmdletsToExport.Expression != null)
            {
                targetCommand.AddParameter("CmdletsToExport", CmdletsToExport.Get(context));
            }

            if(DscResourcesToExport.Expression != null)
            {
                targetCommand.AddParameter("DscResourcesToExport", DscResourcesToExport.Get(context));
            }

            if(PrivateData.Expression != null)
            {
                targetCommand.AddParameter("PrivateData", PrivateData.Get(context));
            }

            if(Tags.Expression != null)
            {
                targetCommand.AddParameter("Tags", Tags.Get(context));
            }

            if(ProjectUri.Expression != null)
            {
                targetCommand.AddParameter("ProjectUri", ProjectUri.Get(context));
            }

            if(LicenseUri.Expression != null)
            {
                targetCommand.AddParameter("LicenseUri", LicenseUri.Get(context));
            }

            if(IconUri.Expression != null)
            {
                targetCommand.AddParameter("IconUri", IconUri.Get(context));
            }

            if(ReleaseNotes.Expression != null)
            {
                targetCommand.AddParameter("ReleaseNotes", ReleaseNotes.Get(context));
            }

            if(HelpInfoUri.Expression != null)
            {
                targetCommand.AddParameter("HelpInfoUri", HelpInfoUri.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if(DefaultCommandPrefix.Expression != null)
            {
                targetCommand.AddParameter("DefaultCommandPrefix", DefaultCommandPrefix.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
