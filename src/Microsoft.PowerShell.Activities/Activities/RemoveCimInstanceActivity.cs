
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to invoke the CimCmdlets\Remove-CimInstance command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class RemoveCimInstance : GenericCimCmdletActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RemoveCimInstance()
        {
            this.DisplayName = "Remove-CimInstance";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "CimCmdlets\\Remove-CimInstance"; } }

        /// <summary>
        /// The .NET type implementing the cmdlet to invoke.
        /// </summary>
        public override System.Type TypeImplementingCmdlet { get { return typeof(Microsoft.Management.Infrastructure.CimCmdlets.RemoveCimInstanceCommand); } }

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
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.Management.Infrastructure.CimInstance> InputObject { get; set; }

        /// <summary>
        /// Provides access to the Query parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Query { get; set; }

        /// <summary>
        /// Provides access to the QueryDialect parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> QueryDialect { get; set; }


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
            
            if(Namespace.Expression != null)
            {
                targetCommand.AddParameter("Namespace", Namespace.Get(context));
            }

            if(OperationTimeoutSec.Expression != null)
            {
                targetCommand.AddParameter("OperationTimeoutSec", OperationTimeoutSec.Get(context));
            }

            if (InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if(Query.Expression != null)
            {
                targetCommand.AddParameter("Query", Query.Get(context));
            }

            if(QueryDialect.Expression != null)
            {
                targetCommand.AddParameter("QueryDialect", QueryDialect.Get(context));
            }

            if (ResourceUri != null)
            {
                targetCommand.AddParameter("ResourceUri", ResourceUri.Get(context));
            }

            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
