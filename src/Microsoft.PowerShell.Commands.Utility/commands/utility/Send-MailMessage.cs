// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MailKit;
using MimeKit;
using System;
using System.Globalization;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    #region SendMailMessage
    /// <summary>
    /// Implementation for the Send-MailMessage command.
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "MailMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135256")]
    public sealed class SendMailMessage : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Gets or sets the files names to be attached to the email.
        /// If the filename specified can not be found, then the relevant error
        /// message should be thrown.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PsPath")]
        public string[] Attachments { get; set; }

        /// <summary>
        /// Gets or sets the address collection that contains the
        /// blind carbon copy (BCC) recipients for the e-mail message.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Bcc { get; set; }

        /// <summary>
        /// Gets or sets the body (content) of the message.
        /// </summary>
        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the mail message body is in Html.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("BAH")]
        public SwitchParameter BodyAsHtml { get; set; }

        /// <summary>
        /// Gets or sets the encoding used for the content of the body and also the subject.
        /// This is set to ASCII to ensure there are no problems with any email server.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("BE")]
        [ValidateNotNullOrEmpty]
        [ArgumentEncodingCompletionsAttribute]
        [ArgumentToEncodingTransformationAttribute]
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Gets or sets the address collection that contains the
        /// carbon copy (CC) recipients for the e-mail message.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Cc { get; set; }

        /// <summary>
        /// Gets or sets the delivery notifications options for the e-mail message. The various
        /// options available for this parameter are None, OnSuccess, OnFailure, Delay and Never.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("DNO")]
        [ValidateNotNullOrEmpty]
        public DeliveryStatusNotification DeliveryNotificationOption { get; set; }

        /// <summary>
        /// Gets or sets the from address for this e-mail message. The default value for
        /// this parameter is the email address of the currently logged on user.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string From { get; set; }

        /// <summary>
        /// Gets or sets the name of the Host used to send the email. This host name will be assigned
        /// to the Powershell variable PSEmailServer, if this host can not reached an appropriate error.
        /// message will be displayed.
        /// </summary>
        [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
        [Alias("ComputerName")]
        [ValidateNotNullOrEmpty]
        public string SmtpServer { get; set; }

        /// <summary>
        /// Gets or sets the priority of the email message. The valid values for this are Normal, High and Low.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;

        /// <summary>
        /// Gets or sets the Reply-To field for this e-mail message.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[] ReplyTo { get; set; }

        /// <summary>
        /// Gets or sets the subject of the email message.
        /// </summary>
        [Parameter(Mandatory = false, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("sub")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the To address for this e-mail message.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] To { get; set; }

        /// <summary>
        /// Gets or sets the credential for this e-mail message.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Gets or sets if Secured layer is required or not.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UseSsl { get; set; }

        /// <summary>
        /// Gets or sets the Port to be used on the server. <see cref="SmtpServer"/>
        /// </summary>
        /// <remarks>
        /// Value must be greater than zero.
        /// </remarks>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, Int32.MaxValue)]
        public int Port { get; set; }

        /// <summary>
        /// Specifies the priority of the mail message.
        /// </summary>
        /// <remarks>
        /// Backward compability matching with System.Net.Mail.MailPriority Enum.
        /// </remarks>
        public enum MessagePriority
        {
            /// <summary>
            /// The email has low priority.
            /// </summary>
            Low,
            
            /// <summary>
            /// The email has normal priority.
            /// </summary>
            Normal,
            
            /// <summary>
            /// The email has high priority.
            /// </summary>
            High
        }

        #endregion

        #region Private variables and methods

        private class DsnSmtpClient : MailKit.Net.Smtp.SmtpClient
        {
            public DeliveryStatusNotification DeliveryStatusNotification { get; set; }
            
            protected override DeliveryStatusNotification? GetDeliveryStatusNotifications (MimeMessage message, MailboxAddress mailbox)
            {
                return DeliveryStatusNotification;
            }
        }

        private DsnSmtpClient _mSmtpClient = null;

        private PSVariable _mGlobalEmailServer = null;

        /// <summary>
        /// Add the input address to the MimeMessage list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="address"></param>
        private void AddMailAddress(InternetAddressList list, string address)
        {
            try
            {
                list.Add(MailboxAddress.Parse(new MimeKit.ParserOptions(){ AddressParserComplianceMode = RfcComplianceMode.Strict, AllowAddressesWithoutDomain = false }, address));
            }
            catch (MimeKit.ParseException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "FormatException", ErrorCategory.InvalidArgument, null); // Keep FormatException for error record for backward compability
                WriteError(er);
            }
        }

        /// <summary>
        /// Add the input addresses to the MimeMessage list.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="addresses"></param>
        private void AddMailAddresses(InternetAddressList list, string[] addresses)
        {
            foreach(var strEmailAddress in addresses)
            {
                AddMailAddress(list, strEmailAddress);
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            _mSmtpClient = new DsnSmtpClient();

            // Get the PowerShell environment variable
            // PSEmailServer might be null if it is deleted by: PS> del variable:PSEmailServer
            _mGlobalEmailServer = SessionState.Internal.GetVariable(SpecialVariables.PSEmailServer);
        }

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Fallback to global email server if SmtpServer parameter is not set
            if (SmtpServer == null && _mGlobalEmailServer != null)
            {
                SmtpServer = Convert.ToString(_mGlobalEmailServer.Value, CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(SmtpServer))
            {
                ErrorRecord er = new ErrorRecord(new InvalidOperationException(SendMailMessageStrings.HostNameValue), null, ErrorCategory.InvalidArgument, null);
                return;
            }

            // Set default port for protocol
            if (Port == 0)
            {
                if(UseSsl)
                {
                    Port = 465; // Standard SMTPS port
                }
                else
                {
                    Port = 25; // Standard SMTP port
                }
            }

            // Create mail message
            var msg = new MimeMessage();

            // Set the sender address of the mail message
            AddMailAddress(msg.From, From);

            // Set the recipient addresses of the mail message
            AddMailAddresses(msg.To, To);

            // Set the CC addresses of the mail message
            if (Cc != null)
            {
                AddMailAddresses(msg.Cc, Cc);
            }

            // Set the BCC addresses of the mail message
            if (Bcc != null)
            {
                AddMailAddresses(msg.Bcc, Bcc);
            }

            // Set the Reply-To addresses of the mail message
            if (ReplyTo != null)
            {
                AddMailAddresses(msg.ReplyTo, ReplyTo);
            }

            // Set the subject of the mail message
            if (Subject != null)
            {
                msg.Subject = Subject;
            }

            // Set the priority of the mail message
            msg.Priority = (MimeKit.MessagePriority)Priority;

            // Create body
            var builder = new BodyBuilder();

            if (BodyAsHtml)
            {
                builder.HtmlBody = Body;
            }
            else
            {
                builder.TextBody = Body;
            }

            // Add the attachments
            if (Attachments != null)
            {
                foreach (string attachFile in Attachments)
                {
                    try
                    {
                        builder.Attachments.Add(attachFile);
                    }
                    catch (ArgumentException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidArgument, builder);
                        ThrowTerminatingError(er);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "UnauthorizedAccessException", ErrorCategory.PermissionDenied, builder);
                        ThrowTerminatingError(er);
                    }
                    catch (System.IO.DirectoryNotFoundException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "DirectoryNotFoundException", ErrorCategory.InvalidArgument, builder);
                        ThrowTerminatingError(er);
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "FileNotFoundException", ErrorCategory.ObjectNotFound, builder);
                        ThrowTerminatingError(er);
                    }
                    catch (System.IO.IOException ex)
                    {
                        ErrorRecord er = new ErrorRecord(ex, "IOException", ErrorCategory.ReadError, builder);
                        ThrowTerminatingError(er);
                    }
                }
            }

            // Set the body of the mail message
            msg.Body = builder.ToMessageBody();

            try
            {
                // Connect to SMTP server
                _mSmtpClient.Connect (SmtpServer, Port, UseSsl);

                // Authenticate if credentials are provided
                if (Credential != null)
                {
                    _mSmtpClient.Authenticate(Credential.GetNetworkCredential());
                }

                // Set the delivery notification
                _mSmtpClient.DeliveryStatusNotification = DeliveryNotificationOption;

                // Send the mail message
                _mSmtpClient.Send(msg);
            }
            catch (MailKit.ProtocolException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "ProtocolException", ErrorCategory.ProtocolError, _mSmtpClient);
                WriteError(er);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "AuthenticationException", ErrorCategory.AuthenticationError, _mSmtpClient);
                WriteError(er);
            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            catch (ArgumentNullException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "ArgumentNullException", ErrorCategory.InvalidArgument, _mSmtpClient);
                WriteError(er);
            }
            finally
            {
                _mSmtpClient.Disconnect(true);
            }
        }

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void EndProcessing()
        {
            _mSmtpClient?.Dispose();
        }

        #endregion
    }
    #endregion
}
