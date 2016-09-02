/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;


namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'get-credential' cmdlet.
    /// The get-credential Cmdlet establishes a credential object called a 
    /// Msh credential, by pairing a given username with
    /// a prompted password. That credential object can then be used for other 
    /// operations involving security.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Credential", DefaultParameterSetName = GetCredentialCommand.credentialSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113311")]
    [OutputType(typeof(PSCredential), ParameterSetName = new string[] { GetCredentialCommand.credentialSet, GetCredentialCommand.messageSet })]
    public sealed class GetCredentialCommand : PSCmdlet
    {
        /// <summary>
        /// The Credential parameter set name.
        /// </summary>
        private const string credentialSet = "CredentialSet";

        /// <summary>
        /// The Message parameter set name.
        /// </summary>
        private const string messageSet = "MessageSet";

        /// <summary>
        /// Gets or sets the underlying PSCredential of
        /// the instance.
        /// </summary>
        ///
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = credentialSet)]
        [Credential()]
        public PSCredential Credential
        {
            get
            {
                return _cred;
            }

            set
            {
                _cred = value;
            }
        }
        private PSCredential _cred;

        /// <summary>
        /// Gets and sets the user supplied message providing description about which script/function is 
        /// requesting the PSCredential from the user.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = messageSet)]
        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }
        private string _message = null;

        /// <summary>
        /// Gets and sets the user supplied username to be used while creating the PSCredential.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ParameterSetName = messageSet)]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }
        private string _userName = null;

        /// <summary>
        /// Initializes a new instance of the GetCredentialCommand
        /// class
        /// </summary>
        public GetCredentialCommand() : base()
        {
        }

        /// <summary>
        /// The command outputs the stored PSCredential.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (!string.IsNullOrEmpty(this.Message))
            {
                string caption = UtilsStrings.PromptForCredential_DefaultCaption;
                string message = this.Message;

                try
                {
                    Credential = this.Host.UI.PromptForCredential(caption, message, _userName, string.Empty);
                }
                catch (ArgumentException exception)
                {
                    ErrorRecord errorRecord = new ErrorRecord(exception, "CouldNotPromptForCredential", ErrorCategory.InvalidOperation, null);
                    WriteError(errorRecord);
                }
            }

            if (Credential != null)
            {
                WriteObject(Credential);
            }
        }
    }
}
