// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'get-credential' cmdlet.
    /// The get-credential Cmdlet establishes a credential object called a
    /// PSCredential, by pairing a given username with
    /// a prompted password. That credential object can then be used for other
    /// operations involving security.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Credential", DefaultParameterSetName = GetCredentialCommand.credentialSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096824")]
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
        [Parameter(Position = 0, ParameterSetName = credentialSet)]
        [ValidateNotNull]
        [Credential]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// Gets and sets the user supplied message providing description about which script/function is
        /// requesting the PSCredential from the user.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = messageSet)]
        [ValidateNotNullOrEmpty]
        public string Message
        {
            get { return _message; }

            set { _message = value; }
        }

        private string _message = UtilsStrings.PromptForCredential_DefaultMessage;

        /// <summary>
        /// Gets and sets the user supplied username to be used while creating the PSCredential.
        /// </summary>
        [Parameter(Position = 0, Mandatory = false, ParameterSetName = messageSet)]
        [ValidateNotNullOrEmpty]
        public string UserName
        {
            get { return _userName; }

            set { _userName = value; }
        }

        private string _userName = null;

        /// <summary>
        /// Gets and sets the title on the window prompt.
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = messageSet)]
        [ValidateNotNullOrEmpty]
        public string Title
        {
            get { return _title; }

            set { _title = value; }
        }

        private string _title = UtilsStrings.PromptForCredential_DefaultCaption;

        /// <summary>
        /// Initializes a new instance of the GetCredentialCommand
        /// class.
        /// </summary>
        public GetCredentialCommand() : base()
        {
        }

        /// <summary>
        /// The command outputs the stored PSCredential.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Credential != null)
            {
                WriteObject(Credential);
                return;
            }

            try
            {
                Credential = this.Host.UI.PromptForCredential(_title, _message, _userName, string.Empty);
            }
            catch (ArgumentException exception)
            {
                ErrorRecord errorRecord = new(
                    exception,
                    "CouldNotPromptForCredential",
                    ErrorCategory.InvalidOperation,
                    targetObject: null);
                WriteError(errorRecord);
            }

            if (Credential != null)
            {
                WriteObject(Credential);
            }
        }
    }
}
