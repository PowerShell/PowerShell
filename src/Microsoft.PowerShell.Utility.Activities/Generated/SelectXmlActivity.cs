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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Select-Xml command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SelectXml : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SelectXml()
        {
            this.DisplayName = "Select-Xml";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Select-Xml"; } }
        
        // Arguments
        
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
        /// Provides access to the Xml parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Xml.XmlNode[]> Xml { get; set; }

        /// <summary>
        /// Provides access to the Content parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Content { get; set; }

        /// <summary>
        /// Provides access to the XPath parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> XPath { get; set; }

        /// <summary>
        /// Provides access to the Namespace parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Collections.Hashtable> Namespace { get; set; }


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

            if(LiteralPath.Expression != null)
            {
                targetCommand.AddParameter("LiteralPath", LiteralPath.Get(context));
            }

            if(Xml.Expression != null)
            {
                targetCommand.AddParameter("Xml", Xml.Get(context));
            }

            if(Content.Expression != null)
            {
                targetCommand.AddParameter("Content", Content.Get(context));
            }

            if(XPath.Expression != null)
            {
                targetCommand.AddParameter("XPath", XPath.Get(context));
            }

            if(Namespace.Expression != null)
            {
                targetCommand.AddParameter("Namespace", Namespace.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
