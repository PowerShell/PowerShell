/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.Globalization;
using System.Net.Mail;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;


namespace Microsoft.PowerShell.Commands
{
    #region SendMailMessage
    /// <summary>
    /// implementation for the Send-MailMessage command
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "MailMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135256")]
    public sealed class SendMailMessage : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Specifies the files names to be attached to the email.
        /// If the filename specified can not be found, then the relevant error
        /// message should be thrown.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PsPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] Attachments
        {
            get { return _attachments; }
            set
            {
                _attachments = value;
            }
        }
        private String[] _attachments;

        /// <summary>
        /// Specifies the address collection that contains the
        /// blind carbon copy (BCC) recipients for the e-mail message.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] Bcc
        {
            get { return _bcc; }
            set
            {
                _bcc = value;
            }
        }
        private String[] _bcc;

        /// <summary>
        /// Specifies the body (content) of the message
        /// </summary>
        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public String Body
        {
            get { return _body; }
            set
            {
                _body = value;
            }
        }
        private String _body;

        /// <summary>
        /// Specifies a value indicating whether the mail message body is in Html.
        /// </summary>
        [Parameter]
        [Alias("BAH")]
        public SwitchParameter BodyAsHtml
        {
            get { return _bodyashtml; }
            set
            {
                _bodyashtml = value;
            }
        }
        private SwitchParameter _bodyashtml;

        /// <summary>
        /// Specifies the encoding used for the content of the body and also the subject.
        /// This is set to ASCII to ensure there are no problems with any email server
        /// </summary>
        [Parameter()]
        [Alias("BE")]
        [ValidateNotNullOrEmpty]
        [ArgumentCompletions(
            EncodingConversion.Ascii,
            EncodingConversion.BigEndianUnicode,
            EncodingConversion.OEM,
            EncodingConversion.Unicode,
            EncodingConversion.Utf7,
            EncodingConversion.Utf8,
            EncodingConversion.Utf8Bom,
            EncodingConversion.Utf8NoBom,
            EncodingConversion.Utf32
            )]
        [ArgumentToEncodingTransformationAttribute()]
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        /// <summary>
        /// Specifies the address collection that contains the
        /// carbon copy (CC) recipients for the e-mail message.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Cc")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] Cc
        {
            get { return _cc; }
            set
            {
                _cc = value;
            }
        }
        private String[] _cc;

        /// <summary>
        /// Specifies the delivery notifications options for the e-mail message. The various
        /// option available for this parameter are None, OnSuccess, OnFailure, Delay and Never
        /// </summary>
        [Parameter()]
        [Alias("DNO")]
        [ValidateNotNullOrEmpty]
        public DeliveryNotificationOptions DeliveryNotificationOption
        {
            get { return _deliverynotification; }
            set
            {
                _deliverynotification = value;
            }
        }
        private DeliveryNotificationOptions _deliverynotification;

        /// <summary>
        /// Specifies the from address for this e-mail message. The default value for
        /// this parameter is the email address of the currently logged on user
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public String From
        {
            get { return _from; }
            set
            {
                _from = value;
            }
        }
        private String _from;

        /// <summary>
        /// Specifies the name of the Host used to send the email. This host name will be assigned
        /// to the Powershell variable PSEmailServer,if this host can not reached an appropriate error
        /// message will be displayed.
        /// </summary>
        [Parameter(Position = 3)]
        [Alias("ComputerName")]
        [ValidateNotNullOrEmpty]
        public String SmtpServer
        {
            get { return _smtpserver; }
            set
            {
                _smtpserver = value;
            }
        }
        private String _smtpserver;

        /// <summary>
        /// Specifies the priority of the email message. The valid values for this are Normal, High and Low
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public MailPriority Priority
        {
            get { return _priority; }
            set
            {
                _priority = value;
            }
        }
        private MailPriority _priority;

        /// <summary>
        /// Specifies the subject of the email message.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [Alias("sub")]
        [ValidateNotNullOrEmpty]
        public String Subject
        {
            get { return _subject; }
            set
            {
                _subject = value;
            }
        }
        private String _subject;


        /// <summary>
        /// Specifies the To address for this e-mail message.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] To
        {
            get { return _to; }
            set
            {
                _to = value;
            }
        }
        private String[] _to;

        /// <summary>
        /// Specifies the credential for this e-mail message.
        /// </summary>
        [Parameter()]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential
        {
            get { return _credential; }
            set
            {
                _credential = value;
            }
        }
        private PSCredential _credential;

        /// <summary>
        /// Specifies if Secured layer is required or not
        /// </summary>
        [Parameter()]
        public SwitchParameter UseSsl
        {
            get { return _usessl; }
            set
            {
                _usessl = value;
            }
        }
        private SwitchParameter _usessl;

        /// <summary>
        /// Specifies the Port to be used on the server. <see cref="SmtpServer"/>
        /// </summary>
        /// <remarks>
        /// Value must be greater than zero.
        /// </remarks>
        [Parameter()]
        [ValidateRange(0, Int32.MaxValue)]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
        private int _port = 0;

        #endregion


        #region private variables and methods


        // Instantiate a new instance of MailMessage
        private MailMessage _mMailMessage = new MailMessage();

        private SmtpClient _mSmtpClient = null;

        /// <summary>
        /// Add the input addresses which are either string or hashtable to the MailMessage
        /// It returns true if the from parameter has more than one value
        /// </summary>
        /// <param name="address"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private void AddAddressesToMailMessage(object address, string param)
        {
            string[] objEmailAddresses = address as string[];
            foreach (string strEmailAddress in objEmailAddresses)
            {
                try
                {
                    switch (param)
                    {
                        case "to":
                            {
                                _mMailMessage.To.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "cc":
                            {
                                _mMailMessage.CC.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "bcc":
                            {
                                _mMailMessage.Bcc.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                    }
                }
                catch (FormatException e)
                {
                    ErrorRecord er = new ErrorRecord(e, "FormatException", ErrorCategory.InvalidType, null);
                    WriteError(er);
                    continue;
                }
            }
        }

        #endregion

        #region Overrides

        /// <summary>
        /// ProcessRecord override
        /// </summary>
        protected override
        void
         BeginProcessing()
        {
            try
            {
                // Set the sender address of the mail message
                _mMailMessage.From = new MailAddress(_from);
            }
            catch (FormatException e)
            {
                ErrorRecord er = new ErrorRecord(e, "FormatException", ErrorCategory.InvalidType, _from);
                ThrowTerminatingError(er);
                // return;
            }

            // Set the recipient address of the mail message
            AddAddressesToMailMessage(_to, "to");

            // Set the BCC address of the mail message
            if (_bcc != null)
            {
                AddAddressesToMailMessage(_bcc, "bcc");
            }

            // Set the CC address of the mail message
            if (_cc != null)
            {
                AddAddressesToMailMessage(_cc, "cc");
            }



            //set the delivery notification
            _mMailMessage.DeliveryNotificationOptions = _deliverynotification;

            // Set the subject of the mail message
            _mMailMessage.Subject = _subject;

            // Set the body of the mail message
            _mMailMessage.Body = _body;

            //set the subject and body encoding
            _mMailMessage.SubjectEncoding = Encoding;
            _mMailMessage.BodyEncoding = Encoding;

            // Set the format of the mail message body as HTML
            _mMailMessage.IsBodyHtml = _bodyashtml;

            // Set the priority of the mail message to normal
            _mMailMessage.Priority = _priority;


            //get the PowerShell environment variable
            //globalEmailServer might be null if it is deleted by: PS> del variable:PSEmailServer
            PSVariable globalEmailServer = SessionState.Internal.GetVariable(SpecialVariables.PSEmailServer);

            if (_smtpserver == null && globalEmailServer != null)
            {
                _smtpserver = Convert.ToString(globalEmailServer.Value, CultureInfo.InvariantCulture);
            }
            if (string.IsNullOrEmpty(_smtpserver))
            {
                ErrorRecord er = new ErrorRecord(new InvalidOperationException(SendMailMessageStrings.HostNameValue), null, ErrorCategory.InvalidArgument, null);
                this.ThrowTerminatingError(er);
            }

            if (0 == _port)
            {
                _mSmtpClient = new SmtpClient(_smtpserver);
            }
            else
            {
                _mSmtpClient = new SmtpClient(_smtpserver, _port);
            }

            if (_usessl)
            {
                _mSmtpClient.EnableSsl = true;
            }

            if (_credential != null)
            {
                _mSmtpClient.UseDefaultCredentials = false;
                _mSmtpClient.Credentials = _credential.GetNetworkCredential();
            }
            else if (!_usessl)
            {
                _mSmtpClient.UseDefaultCredentials = true;
            }
        }

        /// <summary>
        /// ProcessRecord override
        /// </summary>
        protected override void ProcessRecord()
        {
            //add the attachments
            if (_attachments != null)
            {
                string filepath = string.Empty;
                foreach (string attachFile in _attachments)
                {
                    try
                    {
                        filepath = PathUtils.ResolveFilePath(attachFile, this);
                    }
                    catch (ItemNotFoundException e)
                    {
                        //NOTE: This will throw
                        PathUtils.ReportFileOpenFailure(this, filepath, e);
                    }
                    Attachment mailAttachment = new Attachment(filepath);
                    _mMailMessage.Attachments.Add(mailAttachment);
                }
            }
        }

        /// <summary>
        /// EndProcessing
        /// </summary>
        protected override void EndProcessing()
        {
            try
            {
                // Send the mail message
                _mSmtpClient.Send(_mMailMessage);
            }
            catch (SmtpFailedRecipientsException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SmtpFailedRecipientsException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            catch (SmtpException ex)
            {
                if (ex.InnerException != null)
                {
                    ErrorRecord er = new ErrorRecord(new SmtpException(ex.InnerException.Message), "SmtpException", ErrorCategory.InvalidOperation, _mSmtpClient);
                    WriteError(er);
                }
                else
                {
                    ErrorRecord er = new ErrorRecord(ex, "SmtpException", ErrorCategory.InvalidOperation, _mSmtpClient);
                    WriteError(er);
                }
            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "AuthenticationException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }

            //if we don't dispose the attachments, the sender can't modify or use the files sent.
            _mMailMessage.Attachments.Dispose();
        }

        #endregion
    }

    #endregion
}
