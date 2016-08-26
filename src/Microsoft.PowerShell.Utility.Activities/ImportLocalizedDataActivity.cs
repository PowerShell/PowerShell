using System;
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Utility.Activities
{
    /// <summary>
    /// Activity to invoke the Microsoft.PowerShell.Utility\Import-LocalizedData command in a Workflow.
    /// </summary>
    public sealed class ImportLocalizedData : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public ImportLocalizedData()
        {
            this.DisplayName = "Import-LocalizedData";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Import-LocalizedData"; } }
        
        // Arguments

        /// <summary>
        /// Provides access to the UICulture parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> UICulture { get; set; }

        /// <summary>
        /// Provides access to the BaseDirectory parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> BaseDirectory { get; set; }

        /// <summary>
        /// Provides access to the FileName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> FileName { get; set; }

        /// <summary>
        /// Provides access to the SupportedCommand parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> SupportedCommand { get; set; }

        // Module defining this command
        

        // Additional custom code for this activity
        

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

            if(UICulture.Expression != null)
            {
                targetCommand.AddParameter("UICulture", UICulture.Get(context));
            }

            if(BaseDirectory.Expression != null)
            {
                targetCommand.AddParameter("BaseDirectory", BaseDirectory.Get(context));
            }
            //If BaseDirectory is not specified, try to use the workflow base directory.
            else
            {
                throw new ArgumentException(GeneratedActivitiesResources.ImportLocalizedDataWithEmptyEmptyorNullBaseDirectory);                  
            }

            if(FileName.Expression != null)
            {
                targetCommand.AddParameter("FileName", FileName.Get(context));
            }

            if(SupportedCommand.Expression != null)
            {
                targetCommand.AddParameter("SupportedCommand", SupportedCommand.Get(context));
            }

            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
