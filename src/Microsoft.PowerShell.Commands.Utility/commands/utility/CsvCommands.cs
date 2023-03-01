// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace Microsoft.PowerShell.Commands
{
    #region BaseCsvWritingCommand

    /// <summary>
    /// This class implements the base for exportcsv and converttocsv commands.
    /// </summary>
    public abstract class BaseCsvWritingCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Property that sets delimiter.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Delimiter")]
        [ValidateNotNull]
        public char Delimiter { get; set; }

        /// <summary>
        /// Culture switch for csv conversion
        /// </summary>
        [Parameter(ParameterSetName = "UseCulture")]
        public SwitchParameter UseCulture { get; set; }

        /// <summary>
        /// Abstract Property - Input Object which is written in Csv format.
        /// Derived as Different Attributes.In ConvertTo-CSV, This is a positional parameter. Export-CSV not a Positional behaviour.
        /// </summary>
        public abstract PSObject InputObject { get; set; }

        /// <summary>
        /// IncludeTypeInformation : The #TYPE line should be generated. Default is false. Cannot specify with NoTypeInformation.
        /// </summary>
        [Parameter]
        [Alias("ITI")]
        public SwitchParameter IncludeTypeInformation { get; set; }

        /// <summary>
        /// NoTypeInformation : The #TYPE line should not be generated. Default is true. Cannot specify with IncludeTypeInformation.
        /// </summary>
        [Parameter(DontShow = true)]
        [Alias("NTI")]
        public SwitchParameter NoTypeInformation { get; set; } = true;

        /// <summary>
        /// Gets or sets list of fields to quote in output.
        /// </summary>
        [Parameter]
        [Alias("QF")]
        public string[] QuoteFields { get; set; }

        /// <summary>
        /// Gets or sets option to use or suppress quotes in output.
        /// </summary>
        [Parameter]
        [Alias("UQ")]
        public QuoteKind UseQuotes { get; set; } = QuoteKind.Always;

        /// <summary>
        /// Gets or sets property that writes csv file with no headers.
        /// </summary>
        [Parameter]
        public SwitchParameter NoHeader { get; set; }

        #endregion Command Line Parameters

        /// <summary>
        /// Kind of output quoting.
        /// </summary>
        public enum QuoteKind
        {
            /// <summary>
            /// Never quote output.
            /// </summary>
            Never,

            /// <summary>
            /// Always quote output.
            /// </summary>
            Always,

            /// <summary>
            /// Quote output as needed (a field contains used delimiter).
            /// </summary>
            AsNeeded
        }

        /// <summary>
        /// Write the string to a file or pipeline.
        /// </summary>
        public virtual void WriteCsvLine(string line)
        {
        }

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey(nameof(QuoteFields)) && this.MyInvocation.BoundParameters.ContainsKey(nameof(UseQuotes)))
            {
                InvalidOperationException exception = new(CsvCommandStrings.CannotSpecifyQuoteFieldsAndUseQuotes);
                ErrorRecord errorRecord = new(exception, "CannotSpecifyQuoteFieldsAndUseQuotes", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            if (this.MyInvocation.BoundParameters.ContainsKey(nameof(IncludeTypeInformation)) && this.MyInvocation.BoundParameters.ContainsKey(nameof(NoTypeInformation)))
            {
                InvalidOperationException exception = new(CsvCommandStrings.CannotSpecifyIncludeTypeInformationAndNoTypeInformation);
                ErrorRecord errorRecord = new(exception, "CannotSpecifyIncludeTypeInformationAndNoTypeInformation", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            if (this.MyInvocation.BoundParameters.ContainsKey(nameof(IncludeTypeInformation)))
            {
                NoTypeInformation = !IncludeTypeInformation;
            }

            Delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, Delimiter, UseCulture);
        }
    }
    #endregion

    #region Export-CSV Command

    /// <summary>
    /// Implementation for the Export-Csv command.
    /// </summary>
    [Cmdlet(VerbsData.Export, "Csv", SupportsShouldProcess = true, DefaultParameterSetName = "Delimiter", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096608")]
    public sealed class ExportCsvCommand : BaseCsvWritingCommand, IDisposable
    {
        #region Command Line Parameters

        // If a Passthru parameter is added, the ShouldProcess
        // implementation will need to be changed.

        /// <summary>
        /// Input Object for CSV Writing.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public override PSObject InputObject { get; set; }

        /// <summary>
        /// Mandatory file name to write to.
        /// </summary>
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Path
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
                _specifiedPath = true;
            }
        }

        private string _path;
        private bool _specifiedPath = false;

        /// <summary>
        /// The literal path of the mandatory file name to write to.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string LiteralPath
        {
            get
            {
                return _path;
            }

            set
            {
                _path = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// Gets or sets property that sets force parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets or sets property that prevents file overwrite.
        /// </summary>
        [Parameter]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber { get; set; }

        /// <summary>
        /// Gets or sets encoding optional flag.
        /// </summary>
        [Parameter]
        [ArgumentToEncodingTransformationAttribute]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                EncodingConversion.WarnIfObsolete(this, value);
                _encoding = value;
            }
        }

        private Encoding _encoding = Encoding.Default;

        /// <summary>
        /// Gets or sets property that sets append parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter Append { get; set; }

        // true if Append=true AND the file written was not empty (or nonexistent) when the cmdlet was invoked
        private bool _isActuallyAppending;

        #endregion

        #region Overrides

        private bool _shouldProcess;
        private IList<string> _propertyNames;
        private IList<string> _preexistingPropertyNames;
        private ExportCsvHelper _helper;

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Validate that they don't provide both Path and LiteralPath, but have provided at least one.
            if (!(_specifiedPath ^ _isLiteralPath))
            {
                InvalidOperationException exception = new(CsvCommandStrings.CannotSpecifyPathAndLiteralPath);
                ErrorRecord errorRecord = new(exception, "CannotSpecifyPathAndLiteralPath", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            _shouldProcess = ShouldProcess(Path);
            if (!_shouldProcess)
            {
                return;
            }

            CreateFileStream();

            _helper = new ExportCsvHelper(base.Delimiter, base.UseQuotes, base.QuoteFields);
        }

        /// <summary>
        /// Convert the current input object to Csv and write to file/WriteObject.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject == null || _sw == null)
            {
                return;
            }

            if (!_shouldProcess)
            {
                return;
            }

            // Process first object
            if (_propertyNames == null)
            {
                // figure out the column names (and lock-in their order)
                _propertyNames = ExportCsvHelper.BuildPropertyNames(InputObject, _propertyNames);
                if (_isActuallyAppending && _preexistingPropertyNames != null)
                {
                    this.ReconcilePreexistingPropertyNames();
                }

                // write headers (row1: typename + row2: column names)
                if (!_isActuallyAppending && !NoHeader.IsPresent)
                {
                    if (NoTypeInformation == false)
                    {
                        WriteCsvLine(ExportCsvHelper.GetTypeString(InputObject));
                    }

                    WriteCsvLine(_helper.ConvertPropertyNamesCSV(_propertyNames));
                }
            }

            string csv = _helper.ConvertPSObjectToCSV(InputObject, _propertyNames);
            WriteCsvLine(csv);
            _sw.Flush();
        }

        /// <summary>
        /// EndProcessing.
        /// </summary>
        protected override void EndProcessing()
        {
            CleanUp();
        }

        #endregion Overrides

        #region file

        /// <summary>
        /// Handle to file stream.
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// Stream writer used to write to file.
        /// </summary>
        private StreamWriter _sw = null;

        /// <summary>
        /// Handle to file whose read-only attribute should be reset when we are done.
        /// </summary>
        private FileInfo _readOnlyFileInfo = null;

        private void CreateFileStream()
        {
            if (_path == null)
            {
                throw new InvalidOperationException(CsvCommandStrings.FileNameIsAMandatoryParameter);
            }

            string resolvedFilePath = PathUtils.ResolveFilePath(this.Path, this, _isLiteralPath);

            bool isCsvFileEmpty = true;

            if (this.Append && File.Exists(resolvedFilePath))
            {
                using (StreamReader streamReader = PathUtils.OpenStreamReader(this, this.Path, Encoding, _isLiteralPath))
                {
                    isCsvFileEmpty = streamReader.Peek() == -1;
                }
            }

            // If the csv file is empty then even append is treated as regular export (i.e., both header & values are added to the CSV file).
            _isActuallyAppending = this.Append && File.Exists(resolvedFilePath) && !isCsvFileEmpty;

            if (_isActuallyAppending)
            {
                Encoding encodingObject;

                using (StreamReader streamReader = PathUtils.OpenStreamReader(this, this.Path, Encoding, _isLiteralPath))
                {
                    ImportCsvHelper readingHelper = new(
                        this, this.Delimiter, null /* header */, null /* typeName */, streamReader);
                    readingHelper.ReadHeader();
                    _preexistingPropertyNames = readingHelper.Header;

                    encodingObject = streamReader.CurrentEncoding;
                }

                PathUtils.MasterStreamOpen(
                    this,
                    this.Path,
                    encodingObject,
                    defaultEncoding: false,
                    Append,
                    Force,
                    NoClobber,
                    out _fs,
                    out _sw,
                    out _readOnlyFileInfo,
                    _isLiteralPath);
            }
            else
            {
                PathUtils.MasterStreamOpen(
                    this,
                    this.Path,
                    Encoding,
                    defaultEncoding: false,
                    Append,
                    Force,
                    NoClobber,
                    out _fs,
                    out _sw,
                    out _readOnlyFileInfo,
                    _isLiteralPath);
            }
        }

        private void CleanUp()
        {
            if (_fs != null)
            {
                if (_sw != null)
                {
                    _sw.Flush();
                    _sw.Dispose();
                    _sw = null;
                }

                _fs.Dispose();
                _fs = null;

                // reset the read-only attribute
                if (_readOnlyFileInfo != null)
                    _readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
            }

            _helper?.Dispose();
        }

        private void ReconcilePreexistingPropertyNames()
        {
            if (!_isActuallyAppending)
            {
                throw new InvalidOperationException(CsvCommandStrings.ReconcilePreexistingPropertyNamesMethodShouldOnlyGetCalledWhenAppending);
            }

            if (_preexistingPropertyNames == null)
            {
                throw new InvalidOperationException(CsvCommandStrings.ReconcilePreexistingPropertyNamesMethodShouldOnlyGetCalledWhenPreexistingPropertyNamesHaveBeenReadSuccessfully);
            }

            HashSet<string> appendedPropertyNames = new(StringComparer.OrdinalIgnoreCase);
            foreach (string appendedPropertyName in _propertyNames)
            {
                appendedPropertyNames.Add(appendedPropertyName);
            }

            foreach (string preexistingPropertyName in _preexistingPropertyNames)
            {
                if (!appendedPropertyNames.Contains(preexistingPropertyName))
                {
                    if (!Force)
                    {
                        string errorMessage = string.Format(
                            CultureInfo.InvariantCulture, // property names and file names are culture invariant
                            CsvCommandStrings.CannotAppendCsvWithMismatchedPropertyNames,
                            preexistingPropertyName,
                            this.Path);
                        InvalidOperationException exception = new(errorMessage);
                        ErrorRecord errorRecord = new(exception, "CannotAppendCsvWithMismatchedPropertyNames", ErrorCategory.InvalidData, preexistingPropertyName);
                        this.ThrowTerminatingError(errorRecord);
                    }
                }
            }

            _propertyNames = _preexistingPropertyNames;
            _preexistingPropertyNames = null;
        }

        /// <summary>
        /// Write the csv line to file.
        /// </summary>
        /// <param name="line">Line to write.</param>
        public override void WriteCsvLine(string line)
        {
            // NTRAID#Windows Out Of Band Releases-915851-2005/09/13
            if (_disposed)
            {
                throw PSTraceSource.NewObjectDisposedException("ExportCsvCommand");
            }

            _sw.WriteLine(line);
        }
        #endregion file

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                CleanUp();
            }

            _disposed = true;
        }

        #endregion IDisposable Members
    }

    #endregion Export-CSV Command

    #region Import-CSV Command

    /// <summary>
    /// Implements Import-Csv command.
    /// </summary>
    [Cmdlet(VerbsData.Import, "Csv", DefaultParameterSetName = "DelimiterPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097020")]
    public sealed class ImportCsvCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Gets or sets property that sets delimiter.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "DelimiterPath")]
        [Parameter(Position = 1, ParameterSetName = "DelimiterLiteralPath")]
        [ValidateNotNull]
        public char Delimiter { get; set; }

        /// <summary>
        /// Gets or sets mandatory file name to read from.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "DelimiterPath", Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(Position = 0, ParameterSetName = "CulturePath", Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string[] Path
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
                _specifiedPath = true;
            }
        }

        private string[] _paths;
        private bool _specifiedPath = false;

        /// <summary>
        /// Gets or sets the literal path of the mandatory file name to read from.
        /// </summary>
        [Parameter(ParameterSetName = "DelimiterLiteralPath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = "CultureLiteralPath", Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] LiteralPath
        {
            get
            {
                return _paths;
            }

            set
            {
                _paths = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        /// <summary>
        /// Gets or sets property that sets UseCulture parameter.
        /// </summary>
        [Parameter(ParameterSetName = "CulturePath", Mandatory = true)]
        [Parameter(ParameterSetName = "CultureLiteralPath", Mandatory = true)]
        [ValidateNotNull]
        public SwitchParameter UseCulture { get; set; }

        /// <summary>
        /// Gets or sets header property to customize the names.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Header { get; set; }

        /// <summary>
        /// Gets or sets encoding optional flag.
        /// </summary>
        [Parameter]
        [ArgumentToEncodingTransformationAttribute]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                EncodingConversion.WarnIfObsolete(this, value);
                _encoding = value;
            }
        }

        private Encoding _encoding = Encoding.Default;

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are one or more unspecified names.
        /// </summary>
        private bool _alreadyWarnedUnspecifiedNames = false;

        #endregion Command Line Parameters

        #region Override Methods

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            Delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, Delimiter, UseCulture);
        }

        /// <summary>
        /// ProcessRecord overload.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Validate that they don't provide both Path and LiteralPath, but have provided at least one.
            if (!(_specifiedPath ^ _isLiteralPath))
            {
                InvalidOperationException exception = new(CsvCommandStrings.CannotSpecifyPathAndLiteralPath);
                ErrorRecord errorRecord = new(exception, "CannotSpecifyPathAndLiteralPath", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            if (_paths != null)
            {
                foreach (string path in _paths)
                {
                    using (StreamReader streamReader = PathUtils.OpenStreamReader(this, path, this.Encoding, _isLiteralPath))
                    {
                        ImportCsvHelper helper = new(this, Delimiter, Header, null /* typeName */, streamReader);

                        try
                        {
                            helper.Import(ref _alreadyWarnedUnspecifiedNames);
                        }
                        catch (ExtendedTypeSystemException exception)
                        {
                            ErrorRecord errorRecord = new(exception, "AlreadyPresentPSMemberInfoInternalCollectionAdd", ErrorCategory.NotSpecified, null);
                            this.ThrowTerminatingError(errorRecord);
                        }
                    }
                }
            }
        }
    }
    #endregion Override Methods

    #endregion Import-CSV Command

    #region ConvertTo-CSV Command

    /// <summary>
    /// Implements ConvertTo-Csv command.
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Csv", DefaultParameterSetName = "Delimiter",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096832", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(string))]
    public sealed class ConvertToCsvCommand : BaseCsvWritingCommand
    {
        #region Parameter

        /// <summary>
        /// Overrides Base InputObject.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public override PSObject InputObject { get; set; }

        #endregion Parameter

        #region Overrides

        /// <summary>
        /// Stores Property Names.
        /// </summary>
        private IList<string> _propertyNames;

        /// <summary>
        /// </summary>
        private ExportCsvHelper _helper;

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _helper = new ExportCsvHelper(base.Delimiter, base.UseQuotes, base.QuoteFields);
        }

        /// <summary>
        /// Convert the current input object to Csv and write to stream/WriteObject.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject == null)
            {
                return;
            }

            // Process first object
            if (_propertyNames == null)
            {
                _propertyNames = ExportCsvHelper.BuildPropertyNames(InputObject, _propertyNames);

                if (!NoHeader.IsPresent)
                {
                    if (NoTypeInformation == false)
                    {
                        WriteCsvLine(ExportCsvHelper.GetTypeString(InputObject));
                    }

                    // Write property information
                    string properties = _helper.ConvertPropertyNamesCSV(_propertyNames);
                    if (!properties.Equals(string.Empty))
                    {
                        WriteCsvLine(properties);
                    }
                }
            }

            string csv = _helper.ConvertPSObjectToCSV(InputObject, _propertyNames);

            // Write to the output stream
            if (csv != string.Empty)
            {
                WriteCsvLine(csv);
            }
        }

        #endregion Overrides

        #region CSV conversion
        /// <summary>
        /// Write the line to output.
        /// </summary>
        /// <param name="line">Line to write.</param>
        public override void WriteCsvLine(string line)
        {
            WriteObject(line);
        }

        #endregion CSV conversion
    }

    #endregion ConvertTo-CSV Command

    #region ConvertFrom-CSV Command

    /// <summary>
    /// Implements ConvertFrom-Csv command.
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Csv", DefaultParameterSetName = "Delimiter",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096830", RemotingCapability = RemotingCapability.None)]
    public sealed class ConvertFromCsvCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Property that sets delimiter.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Delimiter")]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        public char Delimiter { get; set; }

        /// <summary>
        /// Culture switch for csv conversion
        /// </summary>
        [Parameter(ParameterSetName = "UseCulture", Mandatory = true)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        public SwitchParameter UseCulture { get; set; }

        /// <summary>
        /// Gets or sets input object which is written in Csv format.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject[] InputObject { get; set; }

        /// <summary>
        /// Gets or sets header property to customize the names.
        /// </summary>
        [Parameter(Mandatory = false)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Header { get; set; }

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are one or more unspecified names.
        /// </summary>
        private bool _alreadyWarnedUnspecifiedNames = false;

        #endregion Command Line Parameters

        #region Overrides

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            Delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, Delimiter, UseCulture);
        }

        /// <summary>
        /// Convert the current input object to Csv and write to stream/WriteObject.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (PSObject inputObject in InputObject)
            {
                using (MemoryStream memoryStream = new(Encoding.Unicode.GetBytes(inputObject.ToString())))
                using (StreamReader streamReader = new(memoryStream, System.Text.Encoding.Unicode))
                {
                    ImportCsvHelper helper = new(this, Delimiter, Header, _typeName, streamReader);

                    try
                    {
                        helper.Import(ref _alreadyWarnedUnspecifiedNames);
                    }
                    catch (ExtendedTypeSystemException exception)
                    {
                        ErrorRecord errorRecord = new(exception, "AlreadyPresentPSMemberInfoInternalCollectionAdd", ErrorCategory.NotSpecified, null);
                        this.ThrowTerminatingError(errorRecord);
                    }

                    if ((Header == null) && (helper.Header != null))
                    {
                        Header = helper.Header.ToArray();
                    }

                    if ((_typeName == null) && (helper.TypeName != null))
                    {
                        _typeName = helper.TypeName;
                    }
                }
            }
        }

        #endregion Overrides

        private string _typeName;
    }

    #endregion ConvertFrom-CSV Command

    #region CSV conversion

    #region ExportHelperConversion

    /// <summary>
    /// Helper class for Export-Csv and ConvertTo-Csv.
    /// </summary>
    internal class ExportCsvHelper : IDisposable
    {
        private readonly char _delimiter;
        private readonly BaseCsvWritingCommand.QuoteKind _quoteKind;
        private readonly HashSet<string> _quoteFields;
        private readonly StringBuilder _outputString;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportCsvHelper"/> class.
        /// </summary>
        /// <param name="delimiter">Delimiter char.</param>
        /// <param name="quoteKind">Kind of quoting.</param>
        /// <param name="quoteFields">List of fields to quote.</param>
        internal ExportCsvHelper(char delimiter, BaseCsvWritingCommand.QuoteKind quoteKind, string[] quoteFields)
        {
            _delimiter = delimiter;
            _quoteKind = quoteKind;
            _quoteFields = quoteFields == null ? null : new HashSet<string>(quoteFields, StringComparer.OrdinalIgnoreCase);
            _outputString = new StringBuilder(128);
        }

        // Name of properties to be written in CSV format

        /// <summary>
        /// Get the name of properties from source PSObject and add them to _propertyNames.
        /// </summary>
        internal static IList<string> BuildPropertyNames(PSObject source, IList<string> propertyNames)
        {
            if (propertyNames != null)
            {
                throw new InvalidOperationException(CsvCommandStrings.BuildPropertyNamesMethodShouldBeCalledOnlyOncePerCmdletInstance);
            }

            propertyNames = new Collection<string>();
            if (source.BaseObject is IDictionary dictionary)
            {
                foreach (var key in dictionary.Keys)
                {
                    propertyNames.Add(LanguagePrimitives.ConvertTo<string>(key));
                }

                // Add additional extended members added to the dictionary object, if any
                var propertiesToSearch = new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                    source,
                    PSObject.GetPropertyCollection(PSMemberViewTypes.Extended));

                foreach (var prop in propertiesToSearch)
                {
                    propertyNames.Add(prop.Name);
                }
            }
            else
            {
                // serialize only Extended and Adapted properties.
                PSMemberInfoCollection<PSPropertyInfo> srcPropertiesToSearch =
                    new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                        source,
                        PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted));

                foreach (PSPropertyInfo prop in srcPropertiesToSearch)
                {
                    propertyNames.Add(prop.Name);
                }
            }

            return propertyNames;
        }

        /// <summary>
        /// Converts PropertyNames in to a CSV string.
        /// </summary>
        /// <returns>Converted string.</returns>
        internal string ConvertPropertyNamesCSV(IList<string> propertyNames)
        {
            ArgumentNullException.ThrowIfNull(propertyNames); 

            _outputString.Clear();
            bool first = true;

            foreach (string propertyName in propertyNames)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    _outputString.Append(_delimiter);
                }

                if (_quoteFields != null)
                {
                    if (_quoteFields.TryGetValue(propertyName, out _))
                    {
                        AppendStringWithEscapeAlways(_outputString, propertyName);
                    }
                    else
                    {
                        _outputString.Append(propertyName);
                    }
                }
                else
                {
                    switch (_quoteKind)
                    {
                        case BaseCsvWritingCommand.QuoteKind.Always:
                            AppendStringWithEscapeAlways(_outputString, propertyName);
                            break;
                        case BaseCsvWritingCommand.QuoteKind.AsNeeded:
                            
                            if (propertyName.AsSpan().IndexOfAny(_delimiter, '\n', '"') != -1)
                            {
                                AppendStringWithEscapeAlways(_outputString, propertyName);
                            }
                            else
                            {
                                _outputString.Append(propertyName);
                            }

                            break;
                        case BaseCsvWritingCommand.QuoteKind.Never:
                            _outputString.Append(propertyName);
                            break;
                    }
                }
            }

            return _outputString.ToString();
        }

        /// <summary>
        /// Convert PSObject to CSV string.
        /// </summary>
        /// <param name="mshObject">PSObject to convert.</param>
        /// <param name="propertyNames">Property names.</param>
        /// <returns></returns>
        internal string ConvertPSObjectToCSV(PSObject mshObject, IList<string> propertyNames)
        {
            ArgumentNullException.ThrowIfNull(propertyNames); 

            _outputString.Clear();
            bool first = true;

            foreach (string propertyName in propertyNames)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    _outputString.Append(_delimiter);
                }

                string value = null;
                if (mshObject.BaseObject is IDictionary dictionary)
                {
                    if (dictionary.Contains(propertyName))
                    {
                        value = dictionary[propertyName].ToString();
                    }
                    else if (mshObject.Properties[propertyName] is PSPropertyInfo property)
                    {
                        value = GetToStringValueForProperty(property);
                    }
                }
                else if (mshObject.Properties[propertyName] is PSPropertyInfo property)
                {
                    value = GetToStringValueForProperty(property);
                }

                // If value is null, assume property is not present and skip it.
                if (value != null)
                {
                    if (_quoteFields != null)
                    {
                        if (_quoteFields.TryGetValue(propertyName, out _))
                        {
                            AppendStringWithEscapeAlways(_outputString, value);
                        }
                        else
                        {
                            _outputString.Append(value);
                        }
                    }
                    else
                    {
                        switch (_quoteKind)
                        {
                            case BaseCsvWritingCommand.QuoteKind.Always:
                                AppendStringWithEscapeAlways(_outputString, value);
                                break;
                            case BaseCsvWritingCommand.QuoteKind.AsNeeded:
                                if (value != null && value.AsSpan().IndexOfAny(_delimiter, '\n', '"') != -1)
                                {
                                    AppendStringWithEscapeAlways(_outputString, value);
                                }
                                else
                                {
                                    _outputString.Append(value);
                                }

                                break;
                            case BaseCsvWritingCommand.QuoteKind.Never:
                                _outputString.Append(value);
                                break;
                            default:
                                Diagnostics.Assert(false, "BaseCsvWritingCommand.QuoteKind has new item.");
                                break;
                        }
                    }
                }
            }

            return _outputString.ToString();
        }

        /// <summary>
        /// Get value from property object.
        /// </summary>
        /// <param name="property"> Property to convert.</param>
        /// <returns>ToString() value.</returns>
        internal static string GetToStringValueForProperty(PSPropertyInfo property)
        {
            ArgumentNullException.ThrowIfNull(property); 

            string value = null;
            try
            {
                object temp = property.Value;
                if (temp != null)
                {
                    value = temp.ToString();
                }
            }
            catch (Exception)
            {
                // If we cannot read some value, treat it as null.
            }

            return value;
        }

        /// <summary>
        /// Prepares string for writing type information.
        /// </summary>
        /// <param name="source">PSObject whose type to determine.</param>
        /// <returns>String with type information.</returns>
        internal static string GetTypeString(PSObject source)
        {
            string type = null;

            // get type of source
            Collection<string> tnh = source.TypeNames;
            if (tnh == null || tnh.Count == 0)
            {
                type = "#TYPE";
            }
            else
            {
                if (tnh[0] == null)
                {
                    throw new InvalidOperationException(CsvCommandStrings.TypeHierarchyShouldNotHaveNullValues);
                }

                string temp = tnh[0];

                // If type starts with CSV: remove it. This would happen when you export
                // an imported object. import-csv adds CSV. prefix to the type.
                if (temp.StartsWith(ImportExportCSVHelper.CSVTypePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    temp = temp.Substring(4);
                }

                type = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"#TYPE {temp}");
            }

            return type;
        }

        /// <summary>
        /// Escapes the " in string if necessary.
        /// Encloses the string in double quotes if necessary.
        /// </summary>
        internal static void AppendStringWithEscapeAlways(StringBuilder dest, string source)
        {
            if (source == null)
            {
                return;
            }

            // Adding Double quote to all strings
            dest.Append('"');
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];

                // Double quote in the string is escaped with double quote
                if (c == '"')
                {
                    dest.Append('"');
                }

                dest.Append(c);
            }

            dest.Append('"');
        }

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Public dispose method.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
            }

            _disposed = true;
        }

        #endregion IDisposable Members
    }

    #endregion ExportHelperConversion

    #region ImportHelperConversion

    /// <summary>
    /// Helper class to import single CSV file.
    /// </summary>
    internal class ImportCsvHelper
    {
        #region constructor

        /// <summary>
        /// Reference to cmdlet which is using this helper class.
        /// </summary>
        private readonly PSCmdlet _cmdlet;

        /// <summary>
        /// CSV delimiter (default is the "comma" / "," character).
        /// </summary>
        private readonly char _delimiter;

        /// <summary>
        /// Use "UnspecifiedName" when the name is null or empty.
        /// </summary>
        private const string UnspecifiedName = "H";

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are one or more unspecified names.
        /// </summary>
        private bool _alreadyWarnedUnspecifiedName = false;

        /// <summary>
        /// Gets reference to header values.
        /// </summary>
        internal IList<string> Header { get; private set; }

        /// <summary>
        /// Gets ETS type name from the first line / comment in the CSV.
        /// </summary>
        internal string TypeName { get; private set; }

        /// <summary>
        /// Reader of the csv content.
        /// </summary>
        private readonly StreamReader _sr;

        // Initial sizes of the value list and the line stringbuilder.
        // Set to reasonable initial sizes. They may grow beyond these,
        // but this will prevent a few reallocations.
        private const int ValueCountGuestimate = 16;
        private const int LineLengthGuestimate = 256;

        internal ImportCsvHelper(PSCmdlet cmdlet, char delimiter, IList<string> header, string typeName, StreamReader streamReader)
        {
            ArgumentNullException.ThrowIfNull(cmdlet); 
            ArgumentNullException.ThrowIfNull(streamReader);

            _cmdlet = cmdlet;
            _delimiter = delimiter;
            Header = header;
            TypeName = typeName;
            _sr = streamReader;
        }

        #endregion constructor

        #region reading helpers

        /// <summary>
        /// This is set to true when end of file is reached.
        /// </summary>
        private bool EOF => _sr.EndOfStream;

        private char ReadChar()
        {
            if (EOF)
            {
                throw new InvalidOperationException(CsvCommandStrings.EOFIsReached);
            }

            int i = _sr.Read();
            return (char)i;
        }

        /// <summary>
        /// Peeks the next character in the stream and returns true if it is same as passed in character.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool PeekNextChar(char c)
        {
            int i = _sr.Peek();
            if (i == -1)
            {
                return false;
            }

            return c == (char)i;
        }

        /// <summary>
        /// Reads a line from file. This consumes the end of line.
        /// Only use it when end of line chars are not important.
        /// </summary>
        /// <returns>Line from file.</returns>
        private string ReadLine() => _sr.ReadLine();

        #endregion reading helpers

        internal void ReadHeader()
        {
            // Read #Type record if available
            if ((TypeName == null) && (!this.EOF))
            {
                TypeName = ReadTypeInformation();
            }

            var values = new List<string>(ValueCountGuestimate);
            var builder = new StringBuilder(LineLengthGuestimate);
            while ((Header == null) && (!this.EOF))
            {
                ParseNextRecord(values, builder);

                // Trim all trailing blankspaces and delimiters ( single/multiple ).
                // If there is only one element in the row and if its a blankspace we dont trim it.
                // A trailing delimiter is represented as a blankspace while being added to result collection
                // which is getting trimmed along with blankspaces supplied through the CSV in the below loop.
                while (values.Count > 1 && values[values.Count - 1].Equals(string.Empty))
                {
                    values.RemoveAt(values.Count - 1);
                }

                // File starts with '#' and contains '#Fields:' is W3C Extended Log File Format
                if (values.Count != 0 && values[0].StartsWith("#Fields: "))
                {
                    values[0] = values[0].Substring(9);
                    Header = values;
                }
                else if (values.Count != 0 && values[0].StartsWith('#'))
                {
                    // Skip all lines starting with '#'
                }
                else
                {
                    // This is not W3C Extended Log File Format
                    // By default first line is Header
                    Header = values;
                }
            }

            if (Header != null && Header.Count > 0)
            {
                ValidatePropertyNames(Header);
            }
        }

        internal void Import(ref bool alreadyWriteOutWarning)
        {
            _alreadyWarnedUnspecifiedName = alreadyWriteOutWarning;
            ReadHeader();
            var prevalidated = false;
            var values = new List<string>(ValueCountGuestimate);
            var builder = new StringBuilder(LineLengthGuestimate);
            while (true)
            {
                ParseNextRecord(values, builder);
                if (values.Count == 0)
                    break;

                if (values.Count == 1 && string.IsNullOrEmpty(values[0]))
                {
                    // skip the blank lines
                    continue;
                }

                PSObject result = BuildMshobject(TypeName, Header, values, _delimiter, prevalidated);
                prevalidated = true;
                _cmdlet.WriteObject(result);
            }

            alreadyWriteOutWarning = _alreadyWarnedUnspecifiedName;
        }

        /// <summary>
        /// Validate the names of properties.
        /// </summary>
        /// <param name="names"></param>
        private static void ValidatePropertyNames(IList<string> names)
        {
            if (names != null)
            {
                if (names.Count == 0)
                {
                    // If there are no names, it is an error
                }
                else
                {
                    HashSet<string> headers = new(StringComparer.OrdinalIgnoreCase);
                    foreach (string currentHeader in names)
                    {
                        if (!string.IsNullOrEmpty(currentHeader))
                        {
                            if (!headers.Contains(currentHeader))
                            {
                                headers.Add(currentHeader);
                            }
                            else
                            {
                                // throw a terminating error as there are duplicate headers in the input.
                                string memberAlreadyPresentMsg =
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        ExtendedTypeSystem.MemberAlreadyPresent,
                                        currentHeader);

                                ExtendedTypeSystemException exception = new(memberAlreadyPresentMsg);
                                throw exception;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read the type information, if present.
        /// </summary>
        /// <returns>Type string if present else null.</returns>
        private string ReadTypeInformation()
        {
            string type = null;
            if (PeekNextChar('#'))
            {
                string temp = ReadLine();
                if (temp.StartsWith("#Type", StringComparison.OrdinalIgnoreCase))
                {
                    type = temp.Substring(5);
                    type = type.Trim();
                    if (type.Length == 0)
                    {
                        type = null;
                    }
                }
            }

            return type;
        }

        /// <summary>
        /// Reads the next record from the file and returns parsed collection of string.
        /// </summary>
        /// <returns>
        /// Parsed collection of strings.
        /// </returns>
        private void ParseNextRecord(List<string> result, StringBuilder current)
        {
            result.Clear();

            // current string
            current.Clear();

            bool seenBeginQuote = false;

            while (!EOF)
            {
                // Read the next character
                char ch = ReadChar();

                if (ch == _delimiter)
                {
                    if (seenBeginQuote)
                    {
                        // Delimiter inside double quotes is part of string.
                        // Ex:
                        // "foo, bar"
                        // is parsed as
                        // ->foo, bar<-
                        current.Append(ch);
                    }
                    else
                    {
                        // Delimiter outside quotes is end of current word.
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);
                    }
                }
                else if (ch == '"')
                {
                    if (seenBeginQuote)
                    {
                        if (PeekNextChar('"'))
                        {
                            // "" inside double quote are single quote
                            // ex: "foo""bar"
                            // is read as
                            // ->foo"bar<-

                            // PeekNextChar only peeks. Read the next char.
                            ReadChar();
                            current.Append('"');
                        }
                        else
                        {
                            // We have seen a matching end quote.
                            seenBeginQuote = false;

                            // Read
                            // everything till we hit next delimiter.
                            // In correct CSV,1) end quote is followed by delimiter
                            // 2)end quote is followed some whitespaces and
                            // then delimiter.
                            // We eat the whitespaces seen after the ending quote.
                            // However if there are other characters, we add all of them
                            // to string.
                            // Ex: ->"foo bar"<- is read as ->foo bar<-
                            // ->"foo bar"  <- is read as ->foo bar<-
                            // ->"foo bar" ab <- is read as ->"foo bar" ab <-
                            bool endofRecord = false;
                            ReadTillNextDelimiter(current, ref endofRecord, true);
                            result.Add(current.ToString());
                            current.Remove(0, current.Length);
                            if (endofRecord)
                                break;
                        }
                    }
                    else if (current.Length == 0)
                    {
                        // We are at the beginning of a new word.
                        // This quote is the first quote.
                        seenBeginQuote = true;
                    }
                    else
                    {
                        // We are seeing a quote after the start of
                        // the word. This is error, however we will be
                        // lenient here and do what excel does:
                        // Ex: foo "ba,r"
                        // In above example word read is ->foo "ba<-
                        // Basically we read till next delimiter
                        bool endOfRecord = false;
                        current.Append(ch);
                        ReadTillNextDelimiter(current, ref endOfRecord, false);
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);
                        if (endOfRecord)
                            break;
                    }
                }
                else if (ch == ' ' || ch == '\t')
                {
                    if (seenBeginQuote)
                    {
                        // Spaces in side quote are valid
                        current.Append(ch);
                    }
                    else if (current.Length == 0)
                    {
                        // ignore leading spaces
                        continue;
                    }
                    else
                    {
                        // We are not in quote and we are not at the
                        // beginning of a word. We should not be seeing
                        // spaces here. This is an error condition, however
                        // we will be lenient here and do what excel does,
                        // that is read till next delimiter.
                        // Ex: ->foo <- is read as ->foo<-
                        // Ex: ->foo bar<- is read as ->foo bar<-
                        // Ex: ->foo bar <- is read as ->foo bar <-
                        // Ex: ->foo bar "er,ror"<- is read as ->foo bar "er<-
                        bool endOfRecord = false;
                        current.Append(ch);
                        ReadTillNextDelimiter(current, ref endOfRecord, true);
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);

                        if (endOfRecord)
                        {
                            break;
                        }
                    }
                }
                else if (IsNewLine(ch, out string newLine))
                {
                    if (seenBeginQuote)
                    {
                        // newline inside quote are valid
                        current.Append(newLine);
                    }
                    else
                    {
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);

                        // New line outside quote is end of word and end of record
                        break;
                    }
                }
                else
                {
                    current.Append(ch);
                }
            }

            if (current.Length != 0)
            {
                result.Add(current.ToString());
            }
        }

        // If we detect a newline we return it as a string "\r", "\n" or "\r\n"
        private bool IsNewLine(char ch, out string newLine)
        {
            newLine = string.Empty;
            if (ch == '\r')
            {
                if (PeekNextChar('\n'))
                {
                    ReadChar();
                    newLine = "\r\n";
                }
                else
                {
                    newLine = "\r";
                }
            }
            else if (ch == '\n')
            {
                newLine = "\n";
            }

            return newLine != string.Empty;
        }

        /// <summary>
        /// This function reads the characters till next delimiter and adds them to current.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="endOfRecord">
        /// This is true if end of record is reached
        /// when delimiter is hit. This would be true if delimiter is NewLine.
        /// </param>
        /// <param name="eatTrailingBlanks">
        /// If this is true, eat the trailing blanks. Note:if there are non
        /// whitespace characters present, then trailing blanks are not consumed.
        /// </param>
        private void ReadTillNextDelimiter(StringBuilder current, ref bool endOfRecord, bool eatTrailingBlanks)
        {
            StringBuilder temp = new();

            // Did we see any non-whitespace character
            bool nonWhiteSpace = false;

            while (true)
            {
                if (EOF)
                {
                    endOfRecord = true;
                    break;
                }

                char ch = ReadChar();

                if (ch == _delimiter)
                {
                    break;
                }
                else if (IsNewLine(ch, out string newLine))
                {
                    endOfRecord = true;
                    break;
                }
                else
                {
                    temp.Append(ch);
                    if (ch != ' ' && ch != '\t')
                    {
                        nonWhiteSpace = true;
                    }
                }
            }

            if (eatTrailingBlanks && !nonWhiteSpace)
            {
                string s = temp.ToString();
                s = s.Trim();
                current.Append(s);
            }
            else
            {
                current.Append(temp);
            }
        }

        private PSObject BuildMshobject(string type, IList<string> names, List<string> values, char delimiter, bool preValidated = false)
        {
            PSObject result = new(names.Count);
            char delimiterlocal = delimiter;
            int unspecifiedNameIndex = 1;
            for (int i = 0; i <= names.Count - 1; i++)
            {
                string name = names[i];
                string value = null;

                // if name is null and delimiter is '"', use a default property name 'UnspecifiedName'
                if (name.Length == 0 && delimiterlocal == '"')
                {
                    name = UnspecifiedName + unspecifiedNameIndex;
                    unspecifiedNameIndex++;
                }

                // if name is null and delimiter is not '"', use a default property name 'UnspecifiedName'
                if (string.IsNullOrEmpty(name))
                {
                    name = UnspecifiedName + unspecifiedNameIndex;
                    unspecifiedNameIndex++;
                }

                // If no value was present in CSV file, we write null.
                if (i < values.Count)
                {
                    value = values[i];
                }

                result.Properties.Add(new PSNoteProperty(name, value), preValidated);
            }

            if (!_alreadyWarnedUnspecifiedName && unspecifiedNameIndex != 1)
            {
                _cmdlet.WriteWarning(CsvCommandStrings.UseDefaultNameForUnspecifiedHeader);
                _alreadyWarnedUnspecifiedName = true;
            }

            if (!string.IsNullOrEmpty(type))
            {
                result.TypeNames.Clear();
                result.TypeNames.Add(type);
                result.TypeNames.Add(ImportExportCSVHelper.CSVTypePrefix + type);
            }

            return result;
        }
    }

    #endregion ImportHelperConversion

    #region ExportImport Helper

    /// <summary>
    /// Helper class for CSV conversion.
    /// </summary>
    internal static class ImportExportCSVHelper
    {
        internal const char CSVDelimiter = ',';
        internal const string CSVTypePrefix = "CSV:";

        internal static char SetDelimiter(PSCmdlet cmdlet, string parameterSetName, char explicitDelimiter, bool useCulture)
        {
            char delimiter = explicitDelimiter;
            switch (parameterSetName)
            {
                case "Delimiter":
                case "DelimiterPath":
                case "DelimiterLiteralPath":

                    // if delimiter is not given, it should take , as value
                    if (explicitDelimiter == '\0')
                    {
                        delimiter = ImportExportCSVHelper.CSVDelimiter;
                    }

                    break;
                case "UseCulture":
                case "CulturePath":
                case "CultureLiteralPath":
                    if (useCulture)
                    {
                        // ListSeparator is apparently always a character even though the property returns a string, checked via:
                        // [CultureInfo]::GetCultures("AllCultures") | % { ([CultureInfo]($_.Name)).TextInfo.ListSeparator } | ? Length -ne 1
                        delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                    }

                    break;
                default:
                    {
                        delimiter = ImportExportCSVHelper.CSVDelimiter;
                    }

                    break;
            }

            return delimiter;
        }
    }

    #endregion ExportImport Helper

    #endregion CSV conversion
}
