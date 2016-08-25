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
    /// Activity to invoke the Microsoft.PowerShell.Utility\Send-MailMessage command in a Workflow.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("Microsoft.PowerShell.Activities.ActivityGenerator.GenerateFromName", "3.0")]
    public sealed class SendMailMessage : PSRemotingActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public SendMailMessage()
        {
            this.DisplayName = "Send-MailMessage";
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName { get { return "Microsoft.PowerShell.Utility\\Send-MailMessage"; } }
        
        // Arguments
        
        /// <summary>
        /// Provides access to the Attachments parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Attachments { get; set; }

        /// <summary>
        /// Provides access to the Bcc parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Bcc { get; set; }

        /// <summary>
        /// Provides access to the Body parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Body { get; set; }

        /// <summary>
        /// Provides access to the BodyAsHtml parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> BodyAsHtml { get; set; }

        /// <summary>
        /// Provides access to the Encoding parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Text.Encoding> Encoding { get; set; }

        /// <summary>
        /// Provides access to the Cc parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> Cc { get; set; }

        /// <summary>
        /// Provides access to the DeliveryNotificationOption parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Net.Mail.DeliveryNotificationOptions> DeliveryNotificationOption { get; set; }

        /// <summary>
        /// Provides access to the From parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> From { get; set; }

        /// <summary>
        /// Provides access to the SmtpServer parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> SmtpServer { get; set; }

        /// <summary>
        /// Provides access to the Priority parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Net.Mail.MailPriority> Priority { get; set; }

        /// <summary>
        /// Provides access to the Subject parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Subject { get; set; }

        /// <summary>
        /// Provides access to the To parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String[]> To { get; set; }

        /// <summary>
        /// Provides access to the Credential parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.PSCredential> Credential { get; set; }

        /// <summary>
        /// Provides access to the UseSsl parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> UseSsl { get; set; }

        /// <summary>
        /// Provides access to the Port parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Port { get; set; }


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
            
            if(Attachments.Expression != null)
            {
                targetCommand.AddParameter("Attachments", Attachments.Get(context));
            }

            if(Bcc.Expression != null)
            {
                targetCommand.AddParameter("Bcc", Bcc.Get(context));
            }

            if(Body.Expression != null)
            {
                targetCommand.AddParameter("Body", Body.Get(context));
            }

            if(BodyAsHtml.Expression != null)
            {
                targetCommand.AddParameter("BodyAsHtml", BodyAsHtml.Get(context));
            }

            if(Encoding.Expression != null)
            {
                targetCommand.AddParameter("Encoding", Encoding.Get(context));
            }

            if(Cc.Expression != null)
            {
                targetCommand.AddParameter("Cc", Cc.Get(context));
            }

            if(DeliveryNotificationOption.Expression != null)
            {
                targetCommand.AddParameter("DeliveryNotificationOption", DeliveryNotificationOption.Get(context));
            }

            if(From.Expression != null)
            {
                targetCommand.AddParameter("From", From.Get(context));
            }

            if(SmtpServer.Expression != null)
            {
                targetCommand.AddParameter("SmtpServer", SmtpServer.Get(context));
            }

            if(Priority.Expression != null)
            {
                targetCommand.AddParameter("Priority", Priority.Get(context));
            }

            if(Subject.Expression != null)
            {
                targetCommand.AddParameter("Subject", Subject.Get(context));
            }

            if(To.Expression != null)
            {
                targetCommand.AddParameter("To", To.Get(context));
            }

            if(Credential.Expression != null)
            {
                targetCommand.AddParameter("Credential", Credential.Get(context));
            }

            if(UseSsl.Expression != null)
            {
                targetCommand.AddParameter("UseSsl", UseSsl.Get(context));
            }

            if(Port.Expression != null)
            {
                targetCommand.AddParameter("Port", Port.Get(context));
            }


            return new ActivityImplementationContext() { PowerShellInstance = invoker };
        }
    }
}
