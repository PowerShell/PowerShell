/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Dbg = System.Management.Automation.Diagnostics;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace Microsoft.PowerShell.Commands
{
    #region BaseCsvWritingCommand

    /// <summary>
    /// This class implements the base for exportcsv and converttocsv commands
    /// </summary>
    public abstract class BaseCsvWritingCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Property that sets delimiter
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Delimiter")]
        [ValidateNotNull]
        public char Delimiter
        {
            get
            {
                return _delimiter;
            }
            set
            {
                _delimiter = value;
            }
        }

        /// <summary>
        /// Delimiter to be used.
        /// </summary>
        private char _delimiter;

        ///<summary>
        ///Culture switch for csv conversion
        ///</summary>
        [Parameter(ParameterSetName = "UseCulture")]
        public SwitchParameter UseCulture { get; set; }


        /// <summary>
        /// Abstract Property - Input Object which is written in Csv format
        /// Derived as Different Attributes.In ConvertTo-CSV, This is a positional parameter. Export-CSV not a Positional behaviour.
        /// </summary>

        public abstract PSObject InputObject
        {
            get;
            set;
        }

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

        #endregion Command Line Parameters



        /// <summary>
        /// Write the string to a file or pipeline
        /// </summary>
        public virtual void WriteCsvLine(string line)
        {
        }

        /// <summary>
        /// BeginProcessing override
        /// </summary>
        protected override void BeginProcessing()
        {
            if (this.MyInvocation.BoundParameters.ContainsKey(nameof(IncludeTypeInformation)) && this.MyInvocation.BoundParameters.ContainsKey(nameof(NoTypeInformation)))
            {
                InvalidOperationException exception = new InvalidOperationException(CsvCommandStrings.CannotSpecifyIncludeTypeInformationAndNoTypeInformation);
                ErrorRecord errorRecord = new ErrorRecord(exception, "CannotSpecifyIncludeTypeInformationAndNoTypeInformation", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }
            if (this.MyInvocation.BoundParameters.ContainsKey("IncludeTypeInformation"))
            {
                NoTypeInformation = !IncludeTypeInformation;
            }
            _delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, _delimiter, UseCulture);
        }
    }
    #endregion

    #region Export-CSV Command

    /// <summary>
    /// implementation for the export-csv command
    /// </summary>
    [Cmdlet(VerbsData.Export, "Csv", SupportsShouldProcess = true, DefaultParameterSetName = "Delimiter", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113299")]
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
        /// mandatory file name to write to
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
        /// The literal path of the mandatory file name to write to
        /// </summary>
        [Parameter()]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath")]
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
        /// Property that sets force parameter.
        /// </summary>
        [Parameter()]
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
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }
            set
            {
                _noclobber = value;
            }
        }
        private bool _noclobber;

        /// <summary>
        /// Encoding optional flag
        /// </summary>
        [Parameter()]
        [ArgumentToEncodingTransformationAttribute()]
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
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// Property that sets append parameter.
        /// </summary>
        [Parameter]
        public SwitchParameter Append { get; set; }
        private bool _isActuallyAppending; // true if Append=true AND the file written was not empty (or nonexistent) when the cmdlet was invoked

        #endregion

        #region Overrides

        private bool _shouldProcess;
        private IList<string> _propertyNames;
        private IList<string> _preexistingPropertyNames;
        private ExportCsvHelper _helper;

        /// <summary>
        /// BeginProcessing override
        /// </summary>
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            // Validate that they don't provide both Path and LiteralPath, but have provided at least one.
            if (!(_specifiedPath ^ _isLiteralPath))
            {
                InvalidOperationException exception = new InvalidOperationException(CsvCommandStrings.CannotSpecifyPathAndLiteralPath);
                ErrorRecord errorRecord = new ErrorRecord(exception, "CannotSpecifyPathAndLiteralPath", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            _shouldProcess = ShouldProcess(Path);
            if (!_shouldProcess) return;

            CreateFileStream();

            _helper = new ExportCsvHelper(this, base.Delimiter);
        }


        /// <summary>
        /// Convert the current input object to Csv and write to file/WriteObject
        /// </summary>
        protected override
        void
        ProcessRecord()
        {
            if (InputObject == null || _sw == null)
            {
                return;
            }

            if (!_shouldProcess) return;
            //Process first object
            if (_propertyNames == null)
            {
                // figure out the column names (and lock-in their order)
                _propertyNames = _helper.BuildPropertyNames(InputObject, _propertyNames);
                if (_isActuallyAppending && _preexistingPropertyNames != null)
                {
                    this.ReconcilePreexistingPropertyNames();
                }

                // write headers (row1: typename + row2: column names)
                if (!_isActuallyAppending)
                {
                    if (NoTypeInformation == false)
                    {
                        WriteCsvLine(_helper.GetTypeString(InputObject));
                    }

                    WriteCsvLine(_helper.ConvertPropertyNamesCSV(_propertyNames));
                }
            }

            string csv = _helper.ConvertPSObjectToCSV(InputObject, _propertyNames);
            WriteCsvLine(csv);
            _sw.Flush();
        }

        /// <summary>
        /// EndProcessing
        /// </summary>
        protected override void EndProcessing()
        {
            CleanUp();
        }

        #endregion Overrides

        #region file

        /// <summary>
        /// handle to file stream
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// stream writer used to write to file
        /// </summary>
        private StreamWriter _sw = null;

        /// <summary>
        /// handle to file whose read-only attribute should be reset when we are done
        /// </summary>
        private FileInfo _readOnlyFileInfo = null;

        private void CreateFileStream()
        {
            Dbg.Assert(_path != null, "FileName is mandatory parameter");

            string resolvedFilePath = PathUtils.ResolveFilePath(this.Path, this, _isLiteralPath);

            bool isCsvFileEmpty = true;

            if (this.Append && File.Exists(resolvedFilePath))
            {
                using (StreamReader streamReader = PathUtils.OpenStreamReader(this, this.Path, Encoding, _isLiteralPath))
                {
                    isCsvFileEmpty = streamReader.Peek() == -1 ? true : false;
                }
            }

            // If the csv file is empty then even append is treated as regular export (i.e., both header & values are added to the CSV file).
            _isActuallyAppending = this.Append && File.Exists(resolvedFilePath) && !isCsvFileEmpty;

            if (_isActuallyAppending)
            {
                Encoding encodingObject;

                using (StreamReader streamReader = PathUtils.OpenStreamReader(this, this.Path, Encoding, _isLiteralPath))
                {
                    ImportCsvHelper readingHelper = new ImportCsvHelper(
                        this, this.Delimiter, null /* header */, null /* typeName */, streamReader);
                    readingHelper.ReadHeader();
                    _preexistingPropertyNames = readingHelper.Header;

                    encodingObject = streamReader.CurrentEncoding;
                }

                PathUtils.MasterStreamOpen(
                    this,
                    this.Path,
                    encodingObject,
                    false, // defaultEncoding
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
                    false, // defaultEncoding
                    Append,
                    Force,
                    NoClobber,
                    out _fs,
                    out _sw,
                    out _readOnlyFileInfo,
                    _isLiteralPath);
            }
        }

        private
        void
        CleanUp()
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
                if (null != _readOnlyFileInfo)
                    _readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
            }
            if (_helper != null)
            {
                _helper.Dispose();
            }
        }

        private void ReconcilePreexistingPropertyNames()
        {
            Dbg.Assert(_isActuallyAppending, "This method should only get called when appending");
            Dbg.Assert(_preexistingPropertyNames != null, "This method should only get called when we have successfully read preexisting property names");

            HashSet<string> appendedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        InvalidOperationException exception = new InvalidOperationException(errorMessage);
                        ErrorRecord errorRecord = new ErrorRecord(exception, "CannotAppendCsvWithMismatchedPropertyNames", ErrorCategory.InvalidData, preexistingPropertyName);
                        this.ThrowTerminatingError(errorRecord);
                    }
                }
            }

            _propertyNames = _preexistingPropertyNames;
            _preexistingPropertyNames = null;
        }

        /// <summary>
        /// Write the csv line to file
        /// </summary>
        /// <param name="line"></param>
        public override void
        WriteCsvLine(string line)
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
        /// Set to true when object is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// public dispose method
        /// </summary>
        public
        void
        Dispose()
        {
            if (_disposed == false)
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
    /// Implements Import-Csv command
    /// </summary>
    [Cmdlet(VerbsData.Import, "Csv", DefaultParameterSetName = "Delimiter", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113341")]
    public sealed
    class
    ImportCsvCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Property that sets delimiter
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Delimiter")]
        [ValidateNotNull]
        public char Delimiter { get; set; }


        /// <summary>
        /// mandatory file name to read from
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        public String[] Path
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
        /// The literal path of the mandatory file name to read from
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath")]
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
        /// Property that sets UseCulture parameter
        /// </summary>
        [Parameter(ParameterSetName = "UseCulture", Mandatory = true)]
        [ValidateNotNull]
        public SwitchParameter UseCulture
        {
            get
            {
                return _useculture;
            }
            set
            {
                _useculture = value;
            }
        }
        private bool _useculture;


        ///<summary>
        /// Header property to customize the names
        ///</summary>
        [Parameter(Mandatory = false)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Header { get; set; }

        /// <summary>
        /// Encoding optional flag
        /// </summary>
        [Parameter()]
        [ArgumentToEncodingTransformationAttribute()]
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
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are
        /// one or more unspecified names
        /// </summary>
        private bool _alreadyWarnedUnspecifiedNames = false;

        #endregion Command Line Parameters

        #region Override Methods

        /// <summary>
        ///
        /// </summary>
        protected override void BeginProcessing()
        {
            Delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, Delimiter, _useculture);
        }
        /// <summary>
        /// ProcessRecord overload
        /// </summary>
        protected override void ProcessRecord()
        {
            // Validate that they don't provide both Path and LiteralPath, but have provided at least one.
            if (!(_specifiedPath ^ _isLiteralPath))
            {
                InvalidOperationException exception = new InvalidOperationException(CsvCommandStrings.CannotSpecifyPathAndLiteralPath);
                ErrorRecord errorRecord = new ErrorRecord(exception, "CannotSpecifyPathAndLiteralPath", ErrorCategory.InvalidData, null);
                this.ThrowTerminatingError(errorRecord);
            }

            if (_paths != null)
            {
                foreach (string path in _paths)
                {
                    using (StreamReader streamReader = PathUtils.OpenStreamReader(this, path, this.Encoding, _isLiteralPath))
                    {
                        ImportCsvHelper helper = new ImportCsvHelper(this, Delimiter, Header, null /* typeName */, streamReader);

                        try
                        {
                            helper.Import(ref _alreadyWarnedUnspecifiedNames);
                        }
                        catch (ExtendedTypeSystemException exception)
                        {
                            ErrorRecord errorRecord = new ErrorRecord(exception, "AlreadyPresentPSMemberInfoInternalCollectionAdd", ErrorCategory.NotSpecified, null);
                            this.ThrowTerminatingError(errorRecord);
                        }
                    }
                }
            }//if
        }////ProcessRecord
    }
    #endregion Override Methods

    #endregion Import-CSV Command

    #region ConvertTo-CSV Command

    /// <summary>
    /// Implements ConvertTo-Csv command
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Csv", DefaultParameterSetName = "Delimiter",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135203", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(String))]
    public sealed class ConvertToCsvCommand : BaseCsvWritingCommand
    {
        #region Parameter

        /// <summary>
        /// Overrides Base InputObject
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public override PSObject InputObject { get; set; }

        #endregion Parameter

        #region Overrides

        /// <summary>
        /// Stores Property Names
        /// </summary>
        private IList<string> _propertyNames;

        /// <summary>
        ///
        /// </summary>
        private ExportCsvHelper _helper;

        /// <summary>
        /// BeginProcessing override
        /// </summary>
        protected override
        void
        BeginProcessing()
        {
            base.BeginProcessing();
            _helper = new ExportCsvHelper(this, base.Delimiter);
        }



        /// <summary>
        /// Convert the current input object to Csv and write to stream/WriteObject
        /// </summary>
        protected override
        void
        ProcessRecord()
        {
            if (InputObject == null)
            {
                return;
            }
            //Process first object
            if (_propertyNames == null)
            {
                _propertyNames = _helper.BuildPropertyNames(InputObject, _propertyNames);
                if (NoTypeInformation == false)
                {
                    WriteCsvLine(_helper.GetTypeString(InputObject));
                }
                //Write property information
                string properties = _helper.ConvertPropertyNamesCSV(_propertyNames);
                if (!properties.Equals(""))
                    WriteCsvLine(properties);
            }

            string csv = _helper.ConvertPSObjectToCSV(InputObject, _propertyNames);
            //write to the console
            if (csv != "")
                WriteCsvLine(csv);
        }

        #endregion Overrides

        #region CSV conversion
        /// <summary>
        ///
        /// </summary>
        /// <param name="line"></param>
        public override void
        WriteCsvLine(string line)
        {
            WriteObject(line);
        }

        #endregion CSV conversion
    }

    #endregion ConvertTo-CSV Command

    #region ConvertFrom-CSV Command

    /// <summary>
    /// Implements ConvertFrom-Csv command
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Csv", DefaultParameterSetName = "Delimiter",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135201", RemotingCapability = RemotingCapability.None)]
    public sealed
    class
    ConvertFromCsvCommand : PSCmdlet
    {
        #region Command Line Parameters

        /// <summary>
        /// Property that sets delimiter
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "Delimiter")]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        public char Delimiter { get; set; }

        ///<summary>
        ///Culture switch for csv conversion
        ///</summary>
        [Parameter(ParameterSetName = "UseCulture", Mandatory = true)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        public SwitchParameter UseCulture { get; set; }

        /// <summary>
        /// Input Object which is written in Csv format
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PSObject[] InputObject { get; set; }

        ///<summary>
        /// Header property to customize the names
        ///</summary>
        [Parameter(Mandatory = false)]
        [ValidateNotNull]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Header { get; set; }

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are
        /// one or more unspecified names
        /// </summary>
        private bool _alreadyWarnedUnspecifiedNames = false;

        #endregion Command Line Parameters

        #region Overrides

        /// <summary>
        /// BeginProcessing override
        /// </summary>
        protected override
        void
        BeginProcessing()
        {
            Delimiter = ImportExportCSVHelper.SetDelimiter(this, ParameterSetName, Delimiter, UseCulture);
        }

        /// <summary>
        /// Convert the current input object to Csv and write to stream/WriteObject
        /// </summary>
        protected override
        void
        ProcessRecord()
        {
            foreach (PSObject pObject in InputObject)
            {
                using (MemoryStream memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(pObject.ToString())))
                using (StreamReader streamReader = new StreamReader(memoryStream, System.Text.Encoding.Unicode))
                {
                    ImportCsvHelper helper = new ImportCsvHelper(this, Delimiter, Header, _typeName, streamReader);

                    try
                    {
                        helper.Import(ref _alreadyWarnedUnspecifiedNames);
                    }
                    catch (ExtendedTypeSystemException exception)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(exception, "AlreadyPresentPSMemberInfoInternalCollectionAdd", ErrorCategory.NotSpecified, null);
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
    ///
    /// </summary>
    internal class ExportCsvHelper : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        private PSCmdlet _cmdlet;

        private char _delimiter;

        /// <summary>
        ///
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="delimiter"></param>
        internal
        ExportCsvHelper(PSCmdlet cmdlet, char delimiter)
        {
            if (cmdlet == null)
            {
            }
            _cmdlet = cmdlet;
            _delimiter = delimiter;
        }

        //Name of properties to be written in CSV format


        /// <summary>
        /// Get the name of properties from source PSObject and
        /// add them to _propertyNames.
        /// </summary>
        internal
        IList<string>
        BuildPropertyNames(PSObject source, IList<string> propertyNames)
        {
            Dbg.Assert(propertyNames == null, "This method should be called only once per cmdlet instance");

            // serialize only Extended and Adapted properties..
            PSMemberInfoCollection<PSPropertyInfo> srcPropertiesToSearch =
                new PSMemberInfoIntegratingCollection<PSPropertyInfo>(source,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted));

            propertyNames = new Collection<string>();
            foreach (PSPropertyInfo prop in srcPropertiesToSearch)
            {
                propertyNames.Add(prop.Name);
            }
            return propertyNames;
        }

        /// <summary>
        /// Converts PropertyNames in to a CSV string
        /// </summary>
        /// <returns></returns>
        internal
        string
        ConvertPropertyNamesCSV(IList<string> propertyNames)
        {
            Dbg.Assert(propertyNames != null, "BuildPropertyNames should be called before this method");

            StringBuilder dest = new StringBuilder();
            bool first = true;
            foreach (string propertyName in propertyNames)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    //changed to delimiter
                    dest.Append(_delimiter);
                }
                EscapeAndAppendString(dest, propertyName);
            }
            return dest.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="mshObject"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        internal
        string
        ConvertPSObjectToCSV(PSObject mshObject, IList<string> propertyNames)
        {
            Dbg.Assert(propertyNames != null, "PropertyName collection can be empty here, but it should not be null");

            StringBuilder dest = new StringBuilder();
            bool first = true;

            foreach (string propertyName in propertyNames)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    dest.Append(_delimiter);
                }
                PSPropertyInfo property = mshObject.Properties[propertyName] as PSPropertyInfo;
                string value = null;
                //If property is not present, assume value is null
                if (property != null)
                {
                    value = GetToStringValueForProperty(property);
                }
                EscapeAndAppendString(dest, value);
            }
            return dest.ToString();
        }

        /// <summary>
        /// Get value from property object
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        internal
        string
        GetToStringValueForProperty(PSPropertyInfo property)
        {
            Dbg.Assert(property != null, "Caller should validate the parameter");
            string value = null;
            try
            {
                object temp = property.Value;
                if (temp != null)
                {
                    value = temp.ToString();
                }
            }
            //If we cannot read some value, treat it as null.
            catch (Exception)
            {
            }
            return value;
        }

        /// <summary>
        /// Prepares string for writing type information
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal
        string
        GetTypeString(PSObject source)
        {
            string type = null;

            //get type of source
            Collection<string> tnh = source.TypeNames;
            if (tnh == null || tnh.Count == 0)
            {
                type = "#TYPE";
            }
            else
            {
                Dbg.Assert(tnh[0] != null, "type hierarchy should not have null values");
                string temp = tnh[0];
                //If type starts with CSV: remove it. This would happen when you export
                //an imported object. import-csv adds CSV. prefix to the type.
                if (temp.StartsWith(ImportExportCSVHelper.CSVTypePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    temp = temp.Substring(4);
                }
                type = string.Format(System.Globalization.CultureInfo.InvariantCulture, "#TYPE {0}", temp);
            }

            return type;
        }

        /// <summary>
        /// Escapes the " in string if necessary.
        /// Encloses the string in double quotes if necessary.
        /// </summary>
        /// <returns></returns>
        internal static
        void
        EscapeAndAppendString(StringBuilder dest, string source)
        {
            if (source == null)
            {
                return;
            }
            //Adding Double quote to all strings
            dest.Append('"');
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                //Double quote in the string is escaped with double quote
                if ((c == '"'))
                {
                    dest.Append('"');
                }
                dest.Append(c);
            }
            dest.Append('"');
        }
        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// public dispose method
        /// </summary>
        public
        void
        Dispose()
        {
            if (_disposed == false)
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
    /// Helper class to import single CSV file
    /// </summary>
    internal class ImportCsvHelper
    {
        #region constructor

        /// <summary>
        /// Reference to cmdlet which is using this helper class
        /// </summary>
        private readonly PSCmdlet _cmdlet;

        /// <summary>
        /// CSV delimiter (default is the "comma" / "," character)
        /// </summary>
        private readonly char _delimiter;

        /// <summary>
        /// Use "UnspecifiedName" when the name is null or empty
        /// </summary>
        private const string UnspecifiedName = "H";

        /// <summary>
        /// Avoid writing out duplicate warning messages when there are
        /// one or more unspecified names
        /// </summary>
        private bool _alreadyWarnedUnspecifiedName = false;

        /// <summary>
        /// Reference to header values
        /// </summary>
        internal IList<string> Header { get; private set; }

        /// <summary>
        /// ETS type name from the first line / comment in the CSV
        /// </summary>
        internal string TypeName { get; private set; }

        /// <summary>
        /// Reader of the csv content
        /// </summary>
        private readonly StreamReader _sr;

        internal ImportCsvHelper(PSCmdlet cmdlet, char delimiter, IList<string> header, string typeName, StreamReader streamReader)
        {
            Dbg.Assert(cmdlet != null, "Caller should verify cmdlet != null");
            Dbg.Assert(streamReader != null, "Caller should verify textReader != null");

            _cmdlet = cmdlet;
            _delimiter = delimiter;
            Header = header;
            TypeName = typeName;
            _sr = streamReader;
        }

        #endregion constructor

        #region reading helpers

        /// <summary>
        /// This is set to true when end of file is reached
        /// </summary>
        private
        bool EOF
        {
            get
            {
                return _sr.EndOfStream;
            }
        }

        private
        char
        ReadChar()
        {
            Dbg.Assert(!EOF, "This should not be called if EOF is reached");
            int i = _sr.Read();
            return (char)i;
        }

        /// <summary>
        /// Peeks the next character in the stream and returns true if it is
        /// same as passed in character.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private
        bool
        PeekNextChar(char c)
        {
            int i = _sr.Peek();
            if (i == -1)
            {
                return false;
            }
            return (c == (char)i);
        }

        /// <summary>
        /// Reads a line from file. This consumes the end of line.
        /// Only use it when end of line chars are not important.
        /// </summary>
        /// <returns></returns>
        private string
        ReadLine()
        {
            return _sr.ReadLine();
        }

        #endregion reading helpers

        internal void ReadHeader()
        {
            //Read #Type record if available
            if ((TypeName == null) && (!this.EOF))
            {
                TypeName = ReadTypeInformation();
            }

            while ((Header == null) && (!this.EOF))
            {
                Collection<string> values = ParseNextRecord();

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
                } else if (values.Count != 0 && values[0].StartsWith("#"))
                {
                    // Skip all lines starting with '#'
                } else
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

        internal
        void
        Import(ref bool alreadyWriteOutWarning)
        {
            _alreadyWarnedUnspecifiedName = alreadyWriteOutWarning;
            ReadHeader();
            while (true)
            {
                Collection<string> values = ParseNextRecord();
                if (values.Count == 0)
                    break;

                if (values.Count == 1 && String.IsNullOrEmpty(values[0]))
                {
                    // skip the blank lines
                    continue;
                }

                PSObject result = BuildMshobject(TypeName, Header, values, _delimiter);
                _cmdlet.WriteObject(result);
            }
            alreadyWriteOutWarning = _alreadyWarnedUnspecifiedName;
        }

        /// <summary>
        /// Validate the names of properties
        /// </summary>
        /// <param name="names"></param>
        private static void ValidatePropertyNames(IList<string> names)
        {
            if (names != null)
            {
                if (names.Count == 0)
                {
                    //If there are no names, it is an error
                }
                else
                {
                    HashSet<string> headers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string currentHeader in names)
                    {
                        if (!String.IsNullOrEmpty(currentHeader))
                        {
                            if (!headers.Contains(currentHeader))
                            {
                                headers.Add(currentHeader);
                            }
                            else
                            {
                                // throw a terminating error as there are duplicate headers in the input.
                                string memberAlreadyPresentMsg =
                                    String.Format(CultureInfo.InvariantCulture,
                                    ExtendedTypeSystem.MemberAlreadyPresent,
                                    currentHeader);

                                ExtendedTypeSystemException exception = new ExtendedTypeSystemException(memberAlreadyPresentMsg);
                                throw exception;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Read the type information, if present
        /// </summary>
        /// <returns>Type string if present else null</returns>
        private string
        ReadTypeInformation()
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
                    else
                    {
                        type = ImportExportCSVHelper.CSVTypePrefix + type;
                    }
                }
            }
            return type;
        }

        /// <summary>
        /// Reads the next record from the file and returns parsed collection
        /// of string.
        /// </summary>
        /// <returns>
        /// Parsed collection of strings.
        /// </returns>
        private Collection<string>
        ParseNextRecord()
        {
            //Collection of strings to return
            Collection<string> result = new Collection<string>();
            //current string
            StringBuilder current = new StringBuilder();

            bool seenBeginQuote = false;
            // int i = 0;
            while (!EOF)
            {
                //Read the next character
                char ch = ReadChar();


                if ((ch == _delimiter))
                {
                    if (seenBeginQuote)
                    {
                        //Delimiter inside double quotes is part of string.
                        //Ex:
                        //"foo, bar"
                        //is parsed as
                        //->foo, bar<-
                        current.Append(ch);
                    }
                    else
                    {
                        //Delimiter outside quotes is end of current word.
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
                            //"" inside double quote are single quote
                            //ex: "foo""bar"
                            //is read as
                            //->foo"bar<-

                            //PeekNextChar only peeks. Read the next char.

                            ReadChar();
                            current.Append('"');
                        }
                        else
                        {
                            //We have seen a matching end quote.
                            seenBeginQuote = false;

                            //Read
                            //everything till we hit next delimiter.
                            //In correct CSV,1) end quote is followed by delimiter
                            //2)end quote is followed some whitespaces and
                            //then delimiter.
                            //We eat the whitespaces seen after the ending quote.
                            //However if there are other characters, we add all of them
                            //to string.
                            //Ex: ->"foo bar"<- is read as ->foo bar<-
                            //->"foo bar"  <- is read as ->foo bar<-
                            //->"foo bar" ab <- is read as ->"foo bar" ab <-
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
                        //We are at the beginning of a new word.
                        //This quote is the first quote.
                        seenBeginQuote = true;
                    }
                    else
                    {
                        //We are seeing a quote after the start of
                        //the word. This is error, however we will be
                        //lenient here and do what excel does:
                        //Ex: foo "ba,r"
                        //In above example word read is ->foo "ba<-
                        //Basically we read till next delimiter
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
                        //Spaces in side quote are valid
                        current.Append(ch);
                    }
                    else if (current.Length == 0)
                    {
                        //ignore leading spaces
                        continue;
                    }
                    else
                    {
                        //We are not in quote and we are not at the
                        //beginning of a word. We should not be seeing
                        //spaces here. This is an error condition, however
                        //we will be lenient here and do what excel does,
                        //that is read till next delimiter.
                        //Ex: ->foo <- is read as ->foo<-
                        //Ex: ->foo bar<- is read as ->foo bar<-
                        //Ex: ->foo bar <- is read as ->foo bar <-
                        //Ex: ->foo bar "er,ror"<- is read as ->foo bar "er<-
                        bool endOfRecord = false;
                        current.Append(ch);
                        ReadTillNextDelimiter(current, ref endOfRecord, true);
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);

                        if (endOfRecord)
                            break;
                    }
                }
                else if (IsNewLine(ch, out string newLine))
                {
                    if (seenBeginQuote)
                    {
                        //newline inside quote are valid
                        current.Append(newLine);
                    }
                    else
                    {
                        result.Add(current.ToString());
                        current.Remove(0, current.Length);
                        //New line outside quote is end of word and end of record
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

            return result;
        }

        // If we detect a newline we return it as a string "\r", "\n" or "\r\n"
        private
        bool
        IsNewLine(char ch, out string newLine)
        {
            newLine = "";
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

            return newLine != "";
        }

        /// <summary>
        /// This function reads the characters till next delimiter and adds them
        /// to current
        /// </summary>
        /// <param name="current"></param>
        /// <param name="endOfRecord">
        /// this is true if end of record is reached
        /// when delimiter is hit. This would be true if delimiter is NewLine
        /// </param>
        /// <param name="eatTrailingBlanks">
        /// If this is true, eat the trailing blanks. Note:if there are non
        /// whitespace characters present, then trailing blanks are not consumed
        /// </param>
        private
        void
        ReadTillNextDelimiter(StringBuilder current, ref bool endOfRecord, bool eatTrailingBlanks)
        {
            StringBuilder temp = new StringBuilder();
            //Did we see any non-whitespace character
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

        private
        PSObject
        BuildMshobject(string type, IList<string> names, Collection<string> values, char delimiter)
        {
            //string[] namesarray = null;
            PSObject result = new PSObject();
            char delimiterlocal = delimiter;
            int unspecifiedNameIndex = 1;
            if (type != null && type.Length > 0)
            {
                result.TypeNames.Clear();
                result.TypeNames.Add(type);
            }
            for (int i = 0; i <= names.Count - 1; i++)
            {
                string name = names[i];
                string value = null;
                ////if name is null and delimiter is '"', continue
                if (name.Length == 0 && delimiterlocal == '"')
                    continue;
                ////if name is null and delimiter is not '"', use a default property name 'UnspecifiedName'
                if (string.IsNullOrEmpty(name))
                {
                    name = UnspecifiedName + unspecifiedNameIndex;
                    unspecifiedNameIndex++;
                }
                //If no value was present in CSV file, we write null.
                if (i < values.Count)
                {
                    value = values[i];
                }
                result.Properties.Add(new PSNoteProperty(name, value));
            }

            if (!_alreadyWarnedUnspecifiedName && unspecifiedNameIndex != 1)
            {
                _cmdlet.WriteWarning(CsvCommandStrings.UseDefaultNameForUnspecifiedHeader);
                _alreadyWarnedUnspecifiedName = true;
            }

            return result;
        }
    }

    #endregion ImportHelperConversion

    #region ExportImport Helper

    /// <summary>
    /// Helper class for CSV conversion
    /// </summary>
    internal static class ImportExportCSVHelper
    {
        internal const char CSVDelimiter = ',';
        internal const string CSVTypePrefix = "CSV:";

        internal static char SetDelimiter(PSCmdlet Cmdlet, string ParameterSetName, char Delimiter, bool UseCulture)
        {
            switch (ParameterSetName)
            {
                case "Delimiter":
                    //if delimiter is not given, it should take , as value
                    if (Delimiter == '\0')
                    {
                        Delimiter = ImportExportCSVHelper.CSVDelimiter;
                    }

                    break;
                case "UseCulture":
                    if (UseCulture == true)
                    {
                        // ListSeparator is apparently always a character even though the property returns a string, checked via:
                        // [CultureInfo]::GetCultures("AllCultures") | % { ([CultureInfo]($_.Name)).TextInfo.ListSeparator } | ? Length -ne 1
                        Delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                    }
                    break;
                default:
                    {
                        Delimiter = ImportExportCSVHelper.CSVDelimiter;
                    }
                    break;
            }
            return Delimiter;
        }
    }

    #endregion ExportImport Helper

    #endregion CSV conversion
}
