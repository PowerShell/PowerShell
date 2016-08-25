
using Microsoft.PowerShell.Activities;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;


namespace cimcmdlets.Activities
{
    /// <summary>
    /// Activity to invoke the CimCmdlets\Set-CimInstance command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SetCimInstance : GenericCimCmdletActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SetCimInstance()
        {
            this.DisplayName = "Set-CimInstance";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "CimCmdlets\\Set-CimInstance"; } }

        /// <summary>
        /// The .NET type implementing the cmdlet to invoke.
        /// </summary>
        public override System.Type TypeImplementingCmdlet { get { return typeof(Microsoft.Management.Infrastructure.CimCmdlets.SetCimInstanceCommand); } }
       
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
        /// Provides access to the Property parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.IDictionary> Property { get; set; }

        /// <summary>
        /// Provides access to the PassThru parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> PassThru { get; set; }

        /// <summary>
        /// Module defining this command
        /// Script module contents for this activity
        /// </summary>
        protected override string PSDefiningModule { get { return "CimCmdlets"; } }

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

            if(Property.Expression != null)
            {
                targetCommand.AddParameter("Property", Property.Get(context));
            }

            if(PassThru.Expression != null)
            {
                targetCommand.AddParameter("PassThru", PassThru.Get(context));
            }

            if (ResourceUri != null)
            {
                targetCommand.AddParameter("ResourceUri", ResourceUri.Get(context));
            }

            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
