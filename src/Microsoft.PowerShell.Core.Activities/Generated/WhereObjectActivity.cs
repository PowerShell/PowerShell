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
    /// Activity to invoke the Microsoft.PowerShell.Core\Where-Object command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class WhereObject : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public WhereObject()
        {
            this.DisplayName = "Where-Object";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Core\\Where-Object"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the InputObject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSObject> InputObject { get; set; }

        /// <summary>
        /// Provides access to the FilterScript parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.ScriptBlock> FilterScript { get; set; }

        /// <summary>
        /// Provides access to the Property parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Property { get; set; }

        /// <summary>
        /// Provides access to the Value parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Object> Value { get; set; }

        /// <summary>
        /// Provides access to the EQ parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> EQ { get; set; }

        /// <summary>
        /// Provides access to the CEQ parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CEQ { get; set; }

        /// <summary>
        /// Provides access to the NE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NE { get; set; }

        /// <summary>
        /// Provides access to the CNE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CNE { get; set; }

        /// <summary>
        /// Provides access to the GT parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> GT { get; set; }

        /// <summary>
        /// Provides access to the CGT parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CGT { get; set; }

        /// <summary>
        /// Provides access to the LT parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> LT { get; set; }

        /// <summary>
        /// Provides access to the CLT parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CLT { get; set; }

        /// <summary>
        /// Provides access to the GE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> GE { get; set; }

        /// <summary>
        /// Provides access to the CGE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CGE { get; set; }

        /// <summary>
        /// Provides access to the LE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> LE { get; set; }

        /// <summary>
        /// Provides access to the CLE parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CLE { get; set; }

        /// <summary>
        /// Provides access to the Like parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Like { get; set; }

        /// <summary>
        /// Provides access to the CLike parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CLike { get; set; }

        /// <summary>
        /// Provides access to the NotLike parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NotLike { get; set; }

        /// <summary>
        /// Provides access to the CNotLike parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CNotLike { get; set; }

        /// <summary>
        /// Provides access to the Match parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Match { get; set; }

        /// <summary>
        /// Provides access to the CMatch parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CMatch { get; set; }

        /// <summary>
        /// Provides access to the NotMatch parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NotMatch { get; set; }

        /// <summary>
        /// Provides access to the CNotMatch parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CNotMatch { get; set; }

        /// <summary>
        /// Provides access to the Contains parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Contains { get; set; }

        /// <summary>
        /// Provides access to the CContains parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CContains { get; set; }

        /// <summary>
        /// Provides access to the NotContains parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NotContains { get; set; }

        /// <summary>
        /// Provides access to the CNotContains parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CNotContains { get; set; }

        /// <summary>
        /// Provides access to the In parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> In { get; set; }

        /// <summary>
        /// Provides access to the CIn parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CIn { get; set; }

        /// <summary>
        /// Provides access to the NotIn parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> NotIn { get; set; }

        /// <summary>
        /// Provides access to the CNotIn parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> CNotIn { get; set; }

        /// <summary>
        /// Provides access to the Is parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Is { get; set; }

        /// <summary>
        /// Provides access to the IsNot parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> IsNot { get; set; }


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
            
            if(InputObject.Expression != null)
            {
                targetCommand.AddParameter("InputObject", InputObject.Get(context));
            }

            if(FilterScript.Expression != null)
            {
                targetCommand.AddParameter("FilterScript", FilterScript.Get(context));
            }

            if(Property.Expression != null)
            {
                targetCommand.AddParameter("Property", Property.Get(context));
            }

            if(Value.Expression != null)
            {
                targetCommand.AddParameter("Value", Value.Get(context));
            }

            if(EQ.Expression != null)
            {
                targetCommand.AddParameter("EQ", EQ.Get(context));
            }

            if(CEQ.Expression != null)
            {
                targetCommand.AddParameter("CEQ", CEQ.Get(context));
            }

            if(NE.Expression != null)
            {
                targetCommand.AddParameter("NE", NE.Get(context));
            }

            if(CNE.Expression != null)
            {
                targetCommand.AddParameter("CNE", CNE.Get(context));
            }

            if(GT.Expression != null)
            {
                targetCommand.AddParameter("GT", GT.Get(context));
            }

            if(CGT.Expression != null)
            {
                targetCommand.AddParameter("CGT", CGT.Get(context));
            }

            if(LT.Expression != null)
            {
                targetCommand.AddParameter("LT", LT.Get(context));
            }

            if(CLT.Expression != null)
            {
                targetCommand.AddParameter("CLT", CLT.Get(context));
            }

            if(GE.Expression != null)
            {
                targetCommand.AddParameter("GE", GE.Get(context));
            }

            if(CGE.Expression != null)
            {
                targetCommand.AddParameter("CGE", CGE.Get(context));
            }

            if(LE.Expression != null)
            {
                targetCommand.AddParameter("LE", LE.Get(context));
            }

            if(CLE.Expression != null)
            {
                targetCommand.AddParameter("CLE", CLE.Get(context));
            }

            if(Like.Expression != null)
            {
                targetCommand.AddParameter("Like", Like.Get(context));
            }

            if(CLike.Expression != null)
            {
                targetCommand.AddParameter("CLike", CLike.Get(context));
            }

            if(NotLike.Expression != null)
            {
                targetCommand.AddParameter("NotLike", NotLike.Get(context));
            }

            if(CNotLike.Expression != null)
            {
                targetCommand.AddParameter("CNotLike", CNotLike.Get(context));
            }

            if(Match.Expression != null)
            {
                targetCommand.AddParameter("Match", Match.Get(context));
            }

            if(CMatch.Expression != null)
            {
                targetCommand.AddParameter("CMatch", CMatch.Get(context));
            }

            if(NotMatch.Expression != null)
            {
                targetCommand.AddParameter("NotMatch", NotMatch.Get(context));
            }

            if(CNotMatch.Expression != null)
            {
                targetCommand.AddParameter("CNotMatch", CNotMatch.Get(context));
            }

            if(Contains.Expression != null)
            {
                targetCommand.AddParameter("Contains", Contains.Get(context));
            }

            if(CContains.Expression != null)
            {
                targetCommand.AddParameter("CContains", CContains.Get(context));
            }

            if(NotContains.Expression != null)
            {
                targetCommand.AddParameter("NotContains", NotContains.Get(context));
            }

            if(CNotContains.Expression != null)
            {
                targetCommand.AddParameter("CNotContains", CNotContains.Get(context));
            }

            if(In.Expression != null)
            {
                targetCommand.AddParameter("In", In.Get(context));
            }

            if(CIn.Expression != null)
            {
                targetCommand.AddParameter("CIn", CIn.Get(context));
            }

            if(NotIn.Expression != null)
            {
                targetCommand.AddParameter("NotIn", NotIn.Get(context));
            }

            if(CNotIn.Expression != null)
            {
                targetCommand.AddParameter("CNotIn", CNotIn.Get(context));
            }

            if(Is.Expression != null)
            {
                targetCommand.AddParameter("Is", Is.Get(context));
            }

            if(IsNot.Expression != null)
            {
                targetCommand.AddParameter("IsNot", IsNot.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
