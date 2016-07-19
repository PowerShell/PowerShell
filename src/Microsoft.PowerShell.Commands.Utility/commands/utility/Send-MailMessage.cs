/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Net.Mail;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using System.Management.Automation;
using System.Management.Automation.Internal;
using Microsoft.PowerShell.Commands.Internal.Format;


namespace Microsoft.PowerShell.Commands
{
    #region SendMailMessage
    /// <summary>
    /// implementation for the Send-MailMessage command
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "MailMessage", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=135256")]
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
            get { return attachments; }
            set
            {
                attachments = value;
            }
        }
        private String[] attachments;

        /// <summary>
        /// Specifies the address collection that contains the 
        /// blind carbon copy (BCC) recipients for the e-mail message.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] Bcc
        {
            get { return bcc; }
            set
            {
                bcc = value;
            }
        }
        private String[] bcc;

        /// <summary>
        /// Specifies the body (content) of the message
        /// </summary>
        [Parameter(Position = 2)]
        [ValidateNotNullOrEmpty]
        public String Body
        {
            get { return body; }
            set
            {
                body = value;
            }
        }
        private String body;

        /// <summary>
        /// Specifies a value indicating whether the mail message body is in Html.
        /// </summary>
        [Parameter]
        [Alias("BAH")]
        public SwitchParameter BodyAsHtml
        {
            get { return bodyashtml; }
            set
            {
                bodyashtml = value;
            }
        }
        private SwitchParameter bodyashtml;

        /// <summary>
        /// Specifies the encoding used for the content of the body and also the subject. 
        /// </summary>
        [Parameter()]
        [Alias("BE")]
        [ValidateNotNullOrEmpty]
        [ArgumentToEncodingNameTransformationAttribute()]
        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                encoding = value;
            }
        }
        private Encoding encoding = new ASCIIEncoding();

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
            get { return cc; }
            set
            {
                cc = value;
            }
        }
        private String[] cc;

        /// <summary>
        /// Specifies the delivery notifications options for the e-mail message. The various 
        /// option available for this parameter are None, OnSuccess, OnFailure, Delay and  Never 
        /// </summary>
        [Parameter()]
        [Alias("DNO")]
        [ValidateNotNullOrEmpty]
        public DeliveryNotificationOptions DeliveryNotificationOption
        {
            get { return deliverynotification; }
            set
            {
                deliverynotification = value;
            }
        }
        private DeliveryNotificationOptions deliverynotification;

        /// <summary>
        /// Specifies the from address for this e-mail message. The default value for 
        /// this parameter is the email address of the currently logged on user 
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public String From
        {
            get { return from; }
            set
            {
                from = value;
            }
        }
        private String from;

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
            get { return smtpserver; }
            set
            {
                smtpserver = value;
            }
        }
        private String smtpserver;

        /// <summary>
        /// Specifies the priority of the email message. The valid values for this are Normal, High and Low
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public MailPriority Priority
        {
            get { return priority; }
            set
            {
                priority = value;
            }
        }
        private MailPriority priority;

        /// <summary>
        /// Specifies the  subject of the email message.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [Alias("sub")]
        [ValidateNotNullOrEmpty]
        public String Subject
        {
            get { return subject; }
            set
            {
                subject = value;
            }
        }
        private String subject;


        /// <summary>
        /// Specifies the To address for this e-mail message.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] To
        {
            get { return to; }
            set
            {
                to = value;
            }
        }
        private String[] to;

        /// <summary>
        /// Specifies the credential for this e-mail message.
        /// </summary>
        [Parameter()]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential
        {
            get { return credential; }
            set
            {
                credential = value;
            }
        }
        private PSCredential credential;

        /// <summary>
        /// Specifies if Secured layer is required or not
        /// </summary>
        [Parameter()]
        public SwitchParameter UseSsl
        {
            get { return usessl; }
            set
            {
                usessl = value;
            }
        }
        private SwitchParameter usessl;

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
            get { return port; }
            set { port = value; }
        }
        private int port = 0;

        #endregion


        #region private variables and methods


        // Instantiate a new instance of MailMessage
        private MailMessage mMailMessage = new MailMessage();

        private SmtpClient mSmtpClient = null;

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
                                mMailMessage.To.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "cc":
                            {
                                mMailMessage.CC.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "bcc":
                            {
                                mMailMessage.Bcc.Add(new MailAddress(strEmailAddress));
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
                mMailMessage.From = new MailAddress(from);
            }
            catch (FormatException e)
            {
                ErrorRecord er = new ErrorRecord(e, "FormatException", ErrorCategory.InvalidType, from);
                ThrowTerminatingError(er);
                // return;
            }

            // Set the recepient address of the mail message 
            AddAddressesToMailMessage(to, "to");

            // Set the BCC address of the mail message 
            if (bcc != null)
            {
                AddAddressesToMailMessage(bcc, "bcc");

            }

            // Set the CC address of the mail message
            if (cc != null)
            {
                AddAddressesToMailMessage(cc, "cc");

            }



            //set the delivery notification
            mMailMessage.DeliveryNotificationOptions = deliverynotification;

            // Set the subject of the mail message
            mMailMessage.Subject = subject;

            // Set the body of the mail message
            mMailMessage.Body = body;

            //set the subject and body encoding
            mMailMessage.SubjectEncoding = encoding;
            mMailMessage.BodyEncoding = encoding;

            // Set the format of the mail message body as HTML
            mMailMessage.IsBodyHtml = bodyashtml;

            // Set the priority of the mail message to normal
            mMailMessage.Priority = priority;


            //get the PowerShell environment variable
            //globalEmailServer might be null if it is deleted by: PS> del variable:PSEmailServer
            PSVariable globalEmailServer = SessionState.Internal.GetVariable(SpecialVariables.PSEmailServer);

            if (smtpserver == null && globalEmailServer != null)
            {
                smtpserver = Convert.ToString(globalEmailServer.Value, CultureInfo.InvariantCulture);
            }
            if (string.IsNullOrEmpty(smtpserver))
            {
                ErrorRecord er = new ErrorRecord(new InvalidOperationException(SendMailMessageStrings.HostNameValue), null, ErrorCategory.InvalidArgument, null);
                this.ThrowTerminatingError(er);
            }

            if (0 == port)
            {
                mSmtpClient = new SmtpClient(smtpserver);
            }
            else
            {
                mSmtpClient = new SmtpClient(smtpserver, port);
            }

            if (usessl)
            {
                mSmtpClient.EnableSsl = true;
            }

            if (credential != null)
            {

                mSmtpClient.UseDefaultCredentials = false;
                mSmtpClient.Credentials = credential.GetNetworkCredential();
            }
            else if (!usessl)
            {
                mSmtpClient.UseDefaultCredentials = true;
            }





        }

        /// <summary>
        /// ProcessRecord override
        /// </summary>
        protected override void ProcessRecord()
        {
            //add the attachments 
            if (attachments != null)
            {
                string filepath = string.Empty;
                foreach (string attachFile in attachments)
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
                    mMailMessage.Attachments.Add(mailAttachment);
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
                mSmtpClient.Send(mMailMessage);

            }
            catch (SmtpFailedRecipientsException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "SmtpFailedRecipientsException", ErrorCategory.InvalidOperation, mSmtpClient);
                WriteError(er);
            }
            catch (SmtpException ex)
            {

                if (ex.InnerException != null)
                {
                    ErrorRecord er = new ErrorRecord(new SmtpException(ex.InnerException.Message), "SmtpException", ErrorCategory.InvalidOperation, mSmtpClient);
                    WriteError(er);
                }
                else
                {
                    ErrorRecord er = new ErrorRecord(ex, "SmtpException", ErrorCategory.InvalidOperation, mSmtpClient);
                    WriteError(er);
                }

            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, mSmtpClient);
                WriteError(er);
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                ErrorRecord er = new ErrorRecord(ex, "AuthenticationException", ErrorCategory.InvalidOperation, mSmtpClient);
                WriteError(er);
            }

            //if we don't dispose the attachments, the sender can't modify or use the files sent.
            mMailMessage.Attachments.Dispose();

        }



        #endregion

    }

    /// <summary>
    /// To make it easier to specify -Encoding parameter, we add an ArgumentTransformationAttribute here.
    /// When the input data is of type string and is valid to be converted to System.Text.Encoding, we do 
    /// the conversion and return the converted value. Otherwise, we just return the input data.
    /// </summary>
    internal sealed class ArgumentToEncodingNameTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            string encodingName;
            if (LanguagePrimitives.TryConvertTo<string>(inputData, out encodingName))
            {
                if (string.Equals(encodingName, EncodingConversion.Unknown, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.String, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Unicode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.BigEndianUnicode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Utf8, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Utf7, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Utf32, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Ascii, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.Default, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(encodingName, EncodingConversion.OEM, StringComparison.OrdinalIgnoreCase))
                {
                    // the encodingName is guaranteed to be valid, so it is safe to pass null to method 
                    // Convert(Cmdlet cmdlet, string encoding) as the value of 'cmdlet'.
                    return EncodingConversion.Convert(null, encodingName);
                }
            }
            return inputData;
        }
    }

    #endregion
}