// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Globalization;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the base class from which all SecureString commands
    /// are derived.
    /// </summary>
    public abstract class SecureStringCommandBase : PSCmdlet
    {
        private SecureString _ss;

        /// <summary>
        /// Gets or sets the secure string to be used by the get- and set-
        /// commands.
        /// </summary>
        protected SecureString SecureStringData
        {
            get { return _ss; }

            set { _ss = value; }
        }

        //
        // name of this command
        //
        private string _commandName;

        /// <summary>
        /// Initializes a new instance of the SecureStringCommandBase
        /// class.
        /// </summary>
        /// <param name="name">
        /// The command name deriving from this class
        /// </param>
        protected SecureStringCommandBase(string name) : base()
        {
            _commandName = name;
        }

        private SecureStringCommandBase() : base() { }
    }

    /// <summary>
    /// Defines the base class from which all SecureString import and
    /// export commands are derived.
    /// </summary>
    public abstract class ConvertFromToSecureStringCommandBase : SecureStringCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the ConvertFromToSecureStringCommandBase
        /// class.
        /// </summary>
        protected ConvertFromToSecureStringCommandBase(string name) : base(name) { }

        private SecureString _secureKey = null;
        private byte[] _key;

        /// <summary>
        /// Gets or sets the SecureString version of the encryption
        /// key used by the SecureString cmdlets.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Secure")]
        public SecureString SecureKey
        {
            get
            {
                return _secureKey;
            }

            set
            {
                _secureKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the byte version of the encryption
        /// key used by the SecureString cmdlets.
        /// </summary>
        [Parameter(ParameterSetName = "Open")]
        public byte[] Key
        {
            get
            {
                return _key;
            }

            set
            {
                _key = value;
            }
        }
    }

    /// <summary>
    /// Defines the implementation of the 'ConvertFrom-SecureString' cmdlet.
    /// This cmdlet exports a new SecureString -- one that represents
    /// text that should be kept confidential. The text is encrypted
    /// for privacy when being used, and deleted from computer memory
    /// when no longer needed.  When no key is specified, the command
    /// uses the DPAPI to encrypt the string. When a key is specified, the
    /// command uses the AES algorithm to encrypt the string.
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "SecureString", DefaultParameterSetName = "Secure", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113287")]
    [OutputType(typeof(string))]
    public sealed class ConvertFromSecureStringCommand : ConvertFromToSecureStringCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the ExportSecureStringCommand class.
        /// </summary>
        public ConvertFromSecureStringCommand() : base("ConvertFrom-SecureString") { }

        /// <summary>
        /// Gets or sets the secure string to be exported.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        public SecureString SecureString
        {
            get
            {
                return SecureStringData;
            }

            set
            {
                SecureStringData = value;
            }
        }

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command encrypts
        /// and exports the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            string exportedString = null;
            EncryptionResult encryptionResult = null;

            const string argumentName = "SecureString";
            Utils.CheckSecureStringArg(SecureStringData, argumentName);
            if (SecureStringData.Length == 0)
            {
                throw PSTraceSource.NewArgumentException(argumentName);
            }

            if (SecureKey != null)
            {
                Dbg.Diagnostics.Assert(Key == null, "Only one encryption key should be specified");
                encryptionResult = SecureStringHelper.Encrypt(SecureString, SecureKey);
            }
            else if (Key != null)
            {
                encryptionResult = SecureStringHelper.Encrypt(SecureString, Key);
            }
            else
            {
                exportedString = SecureStringHelper.Protect(SecureString);
            }

            if (encryptionResult != null)
            {
                // The formatted string is Algorithm Version,
                // Initialization Vector, Encrypted Data
                string dataPackage = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0}|{1}|{2}",
                    2,
                    encryptionResult.IV,
                    encryptionResult.EncryptedData);

                // encode the package, and output it.
                // We also include a recognizable prefix so that
                // we can use the old decryption mechanism if we
                // don't see it. While the old decryption
                // generated invalid data for the first bit of the
                // SecureString, it at least didn't generate an
                // exception.
                byte[] outputBytes = System.Text.Encoding.Unicode.GetBytes(dataPackage);
                string encodedString = Convert.ToBase64String(outputBytes);
                WriteObject(SecureStringHelper.SecureStringExportHeader + encodedString);
            }
            else if (exportedString != null)
            {
                WriteObject(exportedString);
            }
        }
    }

    /// <summary>
    /// Defines the implementation of the 'ConvertTo-SecureString' cmdlet.
    /// This cmdlet imports a new SecureString from encrypted data --
    /// one that represents text that should be kept confidential.
    /// The text is encrypted for privacy when being used, and deleted
    /// from computer memory when no longer needed.  When no key is
    /// specified, the command uses the DPAPI to decrypt the data.
    /// When a key is specified, the command uses the AES algorithm
    /// to decrypt the data.
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "SecureString", DefaultParameterSetName = "Secure", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113291")]
    [OutputType(typeof(SecureString))]
    public sealed class ConvertToSecureStringCommand : ConvertFromToSecureStringCommandBase
    {
        /// <summary>
        /// Initializes a new instance of the ImportSecureStringCommand class.
        /// </summary>
        public ConvertToSecureStringCommand() : base("ConvertTo-SecureString") { }

        /// <summary>
        /// Gets or sets the unsecured string to be imported.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        public String String
        {
            get
            {
                return _s;
            }

            set
            {
                _s = value;
            }
        }

        private string _s;

        /// <summary>
        /// Gets or sets the flag that marks the unsecured string as a plain
        /// text string.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "PlainText")]
        public SwitchParameter AsPlainText
        {
            get
            {
                return _asPlainText;
            }

            set
            {
                _asPlainText = value;
            }
        }

        private bool _asPlainText;

        /// <summary>
        /// Gets or sets the flag that will force the import of a plaintext
        /// unsecured string.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "PlainText")]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        private bool _force;

        /// <summary>
        /// Processes records from the input pipeline.
        /// For each input object, the command decrypts the data,
        /// then exports a new SecureString created from the object.
        /// </summary>
        protected override void ProcessRecord()
        {
            SecureString importedString = null;

            Utils.CheckArgForNullOrEmpty(_s, "String");

            try
            {
                string encryptedContent = String;
                byte[] iv = null;

                // If this is a V2 package
                if (String.IndexOf(SecureStringHelper.SecureStringExportHeader,
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    try
                    {
                        // Trim out the header, and retrieve the
                        // rest of the string
                        string remainingData = this.String.Substring(
                            SecureStringHelper.SecureStringExportHeader.Length,
                            String.Length - SecureStringHelper.SecureStringExportHeader.Length);

                        // Unpack it from Base64, get the string
                        // representation, then parse it into its components.
                        byte[] inputBytes = Convert.FromBase64String(remainingData);
                        string dataPackage = System.Text.Encoding.Unicode.GetString(inputBytes);
                        string[] dataElements = dataPackage.Split(Utils.Separators.Pipe);

                        if (dataElements.Length == 3)
                        {
                            encryptedContent = dataElements[2];
                            iv = Convert.FromBase64String(dataElements[1]);
                        }
                    }
                    catch (FormatException)
                    {
                        // Will be raised if we can't convert the
                        // input from a Base64 string. This means
                        // it's not really a V2 package.
                        encryptedContent = String;
                        iv = null;
                    }
                }

                if (SecureKey != null)
                {
                    Dbg.Diagnostics.Assert(Key == null, "Only one encryption key should be specified");
                    importedString = SecureStringHelper.Decrypt(encryptedContent, SecureKey, iv);
                }
                else if (Key != null)
                {
                    importedString = SecureStringHelper.Decrypt(encryptedContent, Key, iv);
                }
                else if (!AsPlainText)
                {
                    importedString = SecureStringHelper.Unprotect(String);
                }
                else
                {
                    if (!Force)
                    {
                        String error =
                            SecureStringCommands.ForceRequired;
                        Exception e = new ArgumentException(error);
                        WriteError(new ErrorRecord(e, "ImportSecureString_ForceRequired", ErrorCategory.InvalidArgument, null));
                    }
                    else
                    {
                        // The entire purpose of the SecureString is to prevent a secret from being
                        // permanently stored in memory as a .Net string.  If they use the
                        // -AsPlainText and -Force flags, they consciously have made the decision to be OK
                        // with that.
                        importedString = new SecureString();
                        foreach (char currentChar in String) { importedString.AppendChar(currentChar); }
                    }
                }
            }
            catch (ArgumentException e)
            {
                ErrorRecord er =
                    SecurityUtils.CreateInvalidArgumentErrorRecord(
                        e,
                        "ImportSecureString_InvalidArgument"
                    );
                WriteError(er);
            }
            catch (CryptographicException e)
            {
                ErrorRecord er =
                    SecurityUtils.CreateInvalidArgumentErrorRecord(
                        e,
                        "ImportSecureString_InvalidArgument_CryptographicError"
                    );
                WriteError(er);
            }

            if (importedString != null)
            {
                WriteObject(importedString);
            }
        }
    }
}

