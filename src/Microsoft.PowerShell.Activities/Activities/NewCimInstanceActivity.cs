
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to invoke the CimCmdlets\New-CimInstance command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class NewCimInstance : GenericCimCmdletActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public NewCimInstance()
        {
            this.DisplayName = "New-CimInstance";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "CimCmdlets\\New-CimInstance"; } }

        /// <summary>
        /// The .NET type implementing the cmdlet to invoke.
        /// </summary>
        public override System.Type TypeImplementingCmdlet { get { return typeof(Microsoft.Management.Infrastructure.CimCmdlets.NewCimInstanceCommand); } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the ClassName parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> ClassName { get; set; }

        /// <summary>
        /// Provides access to the Key parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Key { get; set; }

        /// <summary>
        /// Provides access to the CimClass parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.CimClass> CimClass { get; set; }

        /// <summary>
        /// Provides access to the Property parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.IDictionary> Property { get; set; }

        /// <summary>
        /// Provides access to the Namespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Namespace { get; set; }

        /// <summary>
        /// Provides access to the OperationTimeoutSec parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.UInt32> OperationTimeoutSec { get; set; }


        /// <summary>
        /// Provides access to the ClientOnly parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> ClientOnly { get; set; }

        /// <summary>
        /// Script module contents for this activity`n/// </summary>
        protected override string PSDefiningModule { get { return null; } }

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
            
            if(ClassName.Expression != null)
            {
                targetCommand.AddParameter("ClassName", ClassName.Get(context));
            }

            if(Key.Expression != null)
            {
                targetCommand.AddParameter("Key", Key.Get(context));
            }

            if(CimClass.Expression != null)
            {
                targetCommand.AddParameter("CimClass", CimClass.Get(context));
            }

            if(Property.Expression != null)
            {
                targetCommand.AddParameter("Property", Property.Get(context));
            }

            if(Namespace.Expression != null)
            {
                targetCommand.AddParameter("Namespace", Namespace.Get(context));
            }

            if(OperationTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("OperationTimeoutSec", OperationTimeoutSec.Get(context));
            }

            if (ResourceUri != null)
            {
                targetCommand.AddParameter("ResourceUri", ResourceUri.Get(context));
            }

            if (ClientOnly.Expression != null)
            {
                // Retrieve our host overrides
                var hostValues = context.GetExtension<HostParameterDefaults>();
                string[] computerName = null;

                if (hostValues != null)
                {
                    Dictionary<string, object> incomingArguments = hostValues.Parameters;
                    if (incomingArguments.ContainsKey("PSComputerName"))
                    {
                        computerName = incomingArguments["PSComputerName"] as string[];
                    }
                }

                if (computerName == null)
                {
                    targetCommand.AddParameter("ClientOnly", ClientOnly.Get(context));
                }
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
