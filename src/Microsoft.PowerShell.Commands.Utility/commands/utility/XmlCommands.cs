/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security;
using System.Text;
using System.Xml;
using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// base impl for ConvertTo-Clixml and Export-Clixml
    /// </summary>
    public abstract class ToClixmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        /// <summary>
        /// Depth of serialization
        /// </summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get; set; } = 0;

        /// <summary>
        /// Input object to be exported
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        [AllowNull]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Encoding optional flag
        /// </summary>
        ///
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

        #endregion Command Line Parameters

        #region to-clixml 
        /// <summary>
        /// impl specific TextWriter for the xml serializer to use
        /// </summary>
        protected abstract TextWriter GetTextWriter();

        /// <summary>
        /// IDisposable style cleanup for any imple specific stuff, be sure to call base.CleanUp()
        /// </summary>
        protected virtual void CleanUp()
        {
            if (_xw != null)
            {
                _xw.Dispose();
                _xw = null;
            }
        }

        /// <summary>
        /// xml writer
        /// </summary>
        private XmlWriter _xw;

        /// <summary>
        /// xml serializer
        /// </summary>
        private Serializer _serializer;

        /// <summary>
        /// create the xml serializer using impl specific TextWriter
        /// </summary>
        private void CreateXmlSerializer()
        {
            TextWriter tw = GetTextWriter();
            if (tw == null)
            {
                return;
            }

            // create xml writer
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.CloseOutput = true;
            xmlSettings.Encoding = tw.Encoding;
            xmlSettings.Indent = true;
            xmlSettings.OmitXmlDeclaration = true;
            _xw = XmlWriter.Create(tw, xmlSettings);
            if (Depth == 0)
            {
                _serializer = new Serializer(_xw);
            }
            else
            {
                _serializer = new Serializer(_xw, Depth, true);
            }

        }
        #endregion to-clixml

        #region PSCmdlet Members

        /// <summary>
        /// BeginProcessing override, create the xml scaffolding stuff
        /// </summary>
        protected override void BeginProcessing()
        {
            CreateXmlSerializer();
        }

        /// <summary>
        /// ProcessRecord override, serialize the input object to the xml writer
        /// </summary>
        protected override void ProcessRecord()
        {
            if (null != _serializer)
            {
                _serializer.Serialize(InputObject);
                _xw.Flush();
            }
        }

        /// <summary>
        /// EndProcing override, dispose of the serializer
        /// </summary>
        protected override void EndProcessing()
        {
            if (_serializer != null)
            {
                _serializer.Done();
                _serializer = null;
            }
            CleanUp();
        }

        /// <summary>
        /// StopProcessing override
        /// </summary>
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _serializer.Stop();
        }

        #endregion PSCmdlet Members

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// public dispose method
        /// </summary>
        public void Dispose()
        {
            if (_disposed == false)
            {
                CleanUp();
            }
            _disposed = true;
        }

        #endregion IDisposable Members
    }

    /// <summary>
    /// implementation for the Export-Clixml command
    /// </summary>
    [Cmdlet(VerbsData.Export, "Clixml", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113297")]
    public sealed class ExportClixmlCommand : ToClixmlCommand
    {
        #region Command Line Parameters

        // If a Passthru parameter is added, the SupportsShouldProcess
        // implementation will need to be modified.

        /// <summary>
        /// mandatory file name to write to
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        public string Path { get; set; }

        /// <summary>
        /// mandatory file name to write to
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath")]
        public string LiteralPath
        {
            get
            {
                return Path;
            }
            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }
        private bool _isLiteralPath = false;

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter]
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
        [Parameter]
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

        #endregion Command Line Parameters

        #region FileStream impl for ToClixmlCommand

        /// <summary>
        /// handle to file stream
        /// </summary>
        private FileStream _fs;

        /// <summary>
        /// FileInfo of file to clear read-only flag when operation is complete
        /// </summary>
        private FileInfo _readOnlyFileInfo = null;

        /// <summary>
        /// Get a TextWriter (StreamWriter) for the file specified by the Path parameter
        /// </summary>
        protected override TextWriter GetTextWriter()
        {
            Dbg.Assert(Path != null, "FileName is mandatory parameter");

            if (!ShouldProcess(Path))
            {
                return null;
            }

            StreamWriter sw;
            PathUtils.MasterStreamOpen(
                this,
                this.Path,
                this.Encoding,
                false, // default encoding
                false, // append
                this.Force,
                this.NoClobber,
                out _fs,
                out sw,
                out _readOnlyFileInfo,
                _isLiteralPath
                );

            return sw;
        }

        /// <summary>
        /// disposable-style cleanup
        /// </summary>
        protected override void CleanUp()
        {
            if (_fs != null)
            {
                base.CleanUp();
                _fs.Dispose();
                _fs = null;
            }
        }

        #endregion FileStream impl for ToClixmlCommand

    }


     /// <summary>
    /// implementation for the ConvertTo-Clixml command
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Clixml")]
    public sealed class ConvertToClixmlCommand : ToClixmlCommand
    {
        #region Command Line Parameters

        // just use what ToClixmlCommand gives us

        #endregion Command Line Parameters

        #region PSCmdlet impls

        /// <summary>
        /// Extend ProcessRecord() to also write the converted string to the output stream
        /// </summary>
        protected override void ProcessRecord()
        {
            base.ProcessRecord(); // convert
            WriteObject(_sw.ToString());
        }

        #endregion PSCmdlet impls

        #region StringWriter impl for ToClixmlCommand

        /// <summary>
        /// handle to string stream
        /// </summary>
        private StringWriter _sw;

        /// <summary>
        /// Get a TextWriter (StringWriter) to output as string on pipeline
        /// </summary>
        protected override TextWriter GetTextWriter()
        {
            _sw = new StringWriter();

            return _sw;
        }

        /// <summary>
        /// disposable-style cleanup
        /// </summary>
        protected override void CleanUp()
        {
            if (_sw != null)
            {
                base.CleanUp();
                _sw.Dispose();
                _sw = null;
            }
        }

        #endregion StringWriter impl for ToClixmlCommand

    }

    /// <summary>
    /// Implements Import-Clixml command
    /// </summary>
    [Cmdlet(VerbsData.Import, "Clixml", SupportsPaging = true, DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113340")]
    public sealed class ImportClixmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        /// <summary>
        /// mandatory file name to read from
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByPath")]
        public String[] Path { get; set; }

        /// <summary>
        /// mandatory file name to read from
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public String[] LiteralPath
        {
            get
            {
                return Path;
            }
            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }
        private bool _isLiteralPath = false;


        #endregion Command Line Parameters

        #region IDisposable Members

        private bool _disposed = false;

        /// <summary>
        /// public dispose method
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
                if (_helper != null)
                {
                    _helper.Dispose();
                    _helper = null;
                }
                _disposed = true;
            }
        }

        #endregion

        private ImportXmlHelper _helper;

        /// <summary>
        /// ProcessRecord overload
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Path != null)
            {
                foreach (string path in Path)
                {
                    _helper = new ImportXmlHelper(this, ImportXmlHelper.CreateFileStream(path, this, _isLiteralPath));
                    _helper.Import();
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _helper.Stop();
        }
    }

    /// <summary>
    /// Implements ConvertFrom-Clixml command
    /// </summary>
    [Cmdlet(VerbsData.ConvertFrom, "Clixml", SupportsPaging = true)]
    public sealed class ConvertFromClixmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters

        /// <summary>
        /// gets or sets the InputString property
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowEmptyString]
        public string InputObject { get; set; }

        /// <summary>
        /// inputObjectBuffer buffers all InputObjet contents available in the pipeline.
        /// </summary>
        private List<string> _inputObjectBuffer = new List<string>();

        #endregion Command Line Parameters

        #region IDisposable Members

        private bool _disposed = false;

        /// <summary>
        /// public dispose method
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GC.SuppressFinalize(this);
                if (_helper != null)
                {
                    _helper.Dispose();
                    _helper = null;
                }
                _disposed = true;
            }
        }

        #endregion

        private ImportXmlHelper _helper;

        /// <summary>
        /// ProcessRecord overload
        /// </summary>
        protected override void ProcessRecord()
        {
            _inputObjectBuffer.Add(InputObject);
        }



        /// <summary>
        /// the main execution method for the convertfrom-clixml command
        /// </summary>
        protected override void EndProcessing()
        {
            // When Input is provided through pipeline, the input can be represented in the following two ways:
            // 1. Each input in the collection is a complete clixml content. There can be multiple inputs of this format.
            // 2. The complete input is a collection which represents a single clixml content. This is typically the majority of the case.
            if (_inputObjectBuffer.Count > 0)
            {
                if (_inputObjectBuffer.Count == 1)
                {
                    _helper = new ImportXmlHelper(this, ImportXmlHelper.CreateMemoryStream(_inputObjectBuffer[0]));
                    _helper.Import();
                }
                else
                {
                    bool successfullyConverted = false;
                    try
                    {
                        // Try to deserialize the first element.
                        _helper = new ImportXmlHelper(this, ImportXmlHelper.CreateMemoryStream(_inputObjectBuffer[0]));
                        _helper.Import();
                        successfullyConverted = true;
                    }
                    catch
                    {
                        successfullyConverted = false;
                        // The first input string does not represent a complete clixml Syntax.
                        // Hence consider the the entire input as a single clixml content.
                    }

                    if (successfullyConverted)
                    {
                        for (int index = 1; index < _inputObjectBuffer.Count; index++)
                        {
                            _helper = new ImportXmlHelper(this, ImportXmlHelper.CreateMemoryStream(_inputObjectBuffer[index]));
                            _helper.Import();
                        }
                    }
                    else
                    {
                        // Process the entire input as a single clixml content.
                        _helper = new ImportXmlHelper(this, ImportXmlHelper.CreateMemoryStream(string.Join(System.Environment.NewLine, _inputObjectBuffer.ToArray())));
                        _helper.Import();
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override void StopProcessing()
        {
            base.StopProcessing();
            _helper.Stop();
        }
    }


    /// <summary>
    /// implementation for the convertto-xml command
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "Xml", SupportsShouldProcess = false,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135204", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(XmlDocument), typeof(String))]
    public sealed class ConvertToXmlCommand : PSCmdlet, IDisposable
    {
        #region Command Line Parameters


        /// <summary>
        /// Depth of serialization
        /// </summary>
        [Parameter(HelpMessage = "Specifies how many levels of contained objects should be included in the XML representation")]
        [ValidateRange(1, int.MaxValue)]
        public int Depth { get; set; } = 0;


        /// <summary>
        /// Input Object which is written to XML format
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, Mandatory = true)]
        [AllowNull]
        public PSObject InputObject { get; set; }


        /// <summary>
        /// Property that sets NoTypeInformation parameter.
        /// </summary>
        [Parameter(HelpMessage = "Specifies not to include the Type information in the XML representation")]
        public SwitchParameter NoTypeInformation
        {
            get
            {
                return _notypeinformation;
            }
            set
            {
                _notypeinformation = value;
            }
        }
        private bool _notypeinformation;

        /// <summary>
        /// Property that sets As parameter.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [ValidateSet("Stream", "String", "Document")]
        public string As { get; set; } = "Document";

        #endregion Command Line Parameters


        #region Overrides

        /// <summary>
        /// BeginProcessing override
        /// </summary>
        protected override void BeginProcessing()
        {
            if (!As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                CreateMemoryStream();
            }
            else
            {
                WriteObject(string.Format(CultureInfo.InvariantCulture, "<?xml version=\"1.0\" encoding=\"{0}\"?>", Encoding.UTF8.WebName));
                WriteObject("<Objects>");
            }
        }


        /// <summary>
        /// override ProcessRecord
        /// </summary>
        protected override void ProcessRecord()
        {
            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                CreateMemoryStream();

                if (null != _serializer)
                    _serializer.SerializeAsStream(InputObject);


                if (null != _serializer)
                {
                    _serializer.DoneAsStream();
                    _serializer = null;
                }
                //Loading to the XML Document
                _ms.Position = 0;
                StreamReader read = new StreamReader(_ms);
                string data = read.ReadToEnd();
                WriteObject(data);

                //Cleanup
                CleanUp();
            }
            else
            {
                if (null != _serializer)
                    _serializer.Serialize(InputObject);
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected override void EndProcessing()
        {
            if (null != _serializer)
            {
                _serializer.Done();
                _serializer = null;
            }

            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                WriteObject("</Objects>");
            }
            else
            {
                //Loading to the XML Document
                _ms.Position = 0;
                if (As.Equals("Document", StringComparison.OrdinalIgnoreCase))
                {
                    // this is a trusted xml doc - the cmdlet generated the doc into a private memory stream
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(_ms);
                    WriteObject(xmldoc);
                }
                else if (As.Equals("String", StringComparison.OrdinalIgnoreCase))
                {
                    StreamReader read = new StreamReader(_ms);
                    string data = read.ReadToEnd();
                    WriteObject(data);
                }
            }

            //Cleaning up
            CleanUp();
        }

        /// <summary>
        ///
        /// </summary>
        protected override void StopProcessing()
        {
            _serializer.Stop();
        }

        #endregion Overrides

        #region memory

        /// <summary>
        /// XmlText writer
        /// </summary>
        private XmlWriter _xw;

        /// <summary>
        /// Serializer used for serialization
        /// </summary>
        private CustomSerialization _serializer;

        /// <summary>
        /// Memory Stream used for serialization
        /// </summary>
        private MemoryStream _ms;

        private void CreateMemoryStream()
        {
            // Memory Stream
            _ms = new MemoryStream();

            // We use XmlTextWriter originally:
            //     _xw = new XmlTextWriter(_ms, null);
            //     _xw.Formatting = Formatting.Indented;
            // This implies the following settings:
            //  - Encoding is null -> use the default encoding 'UTF-8' when creating the writer from the stream;
            //  - XmlTextWriter closes the underlying stream / writer when 'Close/Dispose' is called on it;
            //  - Use the default indentation setting -- two space characters.
            //
            // We configure the same settings in XmlWriterSettings when refactoring this code to use XmlWriter:
            //  - The default encoding used by XmlWriterSettings is 'UTF-8', but we call it out explicitly anyway;
            //  - Set CloseOutput to true;
            //  - Set Indent to true, and by default, IndentChars is two space characters.
            //
            // We use XmlWriterSettings.OmitXmlDeclaration instead of XmlWriter.WriteStartDocument because the
            // xml writer created by calling XmlWriter.Create(Stream, XmlWriterSettings) will write out the xml
            // declaration even without calling WriteStartDocument().

            var xmlSettings = new XmlWriterSettings();
            xmlSettings.Encoding = Encoding.UTF8;
            xmlSettings.CloseOutput = true;
            xmlSettings.Indent = true;

            if (As.Equals("Stream", StringComparison.OrdinalIgnoreCase))
            {
                // Omit xml declaration in this case because we will write out the declaration string in BeginProcess.
                xmlSettings.OmitXmlDeclaration = true;
            }

            _xw = XmlWriter.Create(_ms, xmlSettings);

            if (Depth == 0)
            {
                _serializer = new CustomSerialization(_xw, NoTypeInformation);
            }
            else
            {
                _serializer = new CustomSerialization(_xw, NoTypeInformation, Depth);
            }
        }

        /// <summary>
        ///Cleaning up the MemoryStream
        /// </summary>
        private void CleanUp()
        {
            if (_ms != null)
            {
                if (_xw != null)
                {
                    _xw.Dispose();
                    _xw = null;
                }

                _ms.Dispose();
                _ms = null;
            }
        }

        #endregion memory

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


    /// <summary>
    /// Base Helper class to import XML 
    /// </summary>
    internal class ImportXmlHelper : IDisposable
    {
        #region constructor

        /// <summary>
        /// Reference to cmdlet which is using this helper class
        /// </summary>
        private readonly PSCmdlet _cmdlet;
    
        /// <summary>
        /// handle to  stream
        /// </summary>
        internal Stream _stream;


        internal ImportXmlHelper(PSCmdlet cmdlet, Stream stream)
        {
            _cmdlet = cmdlet;
            _stream = stream;
        }

        #endregion constructor

        #region stream

        /// <summary>
        /// XmlReader used to read file
        /// </summary>
        internal XmlReader _xr;

        private static XmlReader CreateXmlReader(Stream stream)
        {
            TextReader textReader = new StreamReader(stream);

            // skip #< CLIXML directive
            const string cliXmlDirective = "#< CLIXML";
            if (textReader.Peek() == (int)cliXmlDirective[0])
            {
                string line = textReader.ReadLine();
                if (!line.Equals(cliXmlDirective, StringComparison.Ordinal))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            return XmlReader.Create(textReader, InternalDeserializer.XmlReaderSettingsForCliXml);
        }

        private void CleanUp()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        #endregion stream

        #region IDisposable Members

        /// <summary>
        /// Set to true when object is disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// public dispose method
        /// </summary>
        public void Dispose()
        {
            if (_disposed == false)
            {
                CleanUp();
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members

        private Deserializer _deserializer;

        internal void Import()
        {
            _xr = CreateXmlReader(_stream);
            _deserializer = new Deserializer(_xr);
            // If total count has been requested, return a dummy object with zero confidence
            if (_cmdlet.PagingParameters.IncludeTotalCount)
            {
                PSObject totalCount = _cmdlet.PagingParameters.NewTotalCount(0, 0);
                _cmdlet.WriteObject(totalCount);
            }


            ulong skip = _cmdlet.PagingParameters.Skip;
            ulong first = _cmdlet.PagingParameters.First;

            // if paging is not specified then keep the old V2 behavior
            if (skip == 0 && first == ulong.MaxValue)
            {
                while (!_deserializer.Done())
                {
                    object result = _deserializer.Deserialize();
                    _cmdlet.WriteObject(result);
                }
            }
            // else try to flatten the output if possible
            else
            {
                ulong skipped = 0;
                ulong count = 0;
                while (!_deserializer.Done() && count < first)
                {
                    object result = _deserializer.Deserialize();
                    PSObject psObject = result as PSObject;

                    if (psObject != null)
                    {
                        ICollection c = psObject.BaseObject as ICollection;
                        if (c != null)
                        {
                            foreach (object o in c)
                            {
                                if (count >= first)
                                    break;

                                if (skipped++ >= skip)
                                {
                                    count++;
                                    _cmdlet.WriteObject(o);
                                }
                            }
                        }
                        else
                        {
                            if (skipped++ >= skip)
                            {
                                count++;
                                _cmdlet.WriteObject(result);
                            }
                        }
                    }
                    else if (skipped++ >= skip)
                    {
                        count++;
                        _cmdlet.WriteObject(result);
                        continue;
                    }
                }
            }
        }

        internal void Stop()
        {
            if (_deserializer != null)
            {
                _deserializer.Stop();
            }
        }

        internal static Stream CreateFileStream(string path, PSCmdlet cmdlet, bool isLiteralPath)
        {
            return PathUtils.OpenFileStream(path, cmdlet, isLiteralPath);
            
        }

        internal static Stream CreateMemoryStream(string input)
        {
            Stream s = new MemoryStream();
            StreamWriter w = new StreamWriter(s);
            w.Write(input);
            w.Flush();
            s.Position = 0;
            return s;
        }


    } // ImportXmlMemoryHelper

    #region Select-Xml
    ///<summary>
    ///This cmdlet is used to search an xml document based on the XPath Query.
    ///</summary>
    [Cmdlet(VerbsCommon.Select, "Xml", DefaultParameterSetName = "Xml", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=135255")]
    [OutputType(typeof(SelectXmlInfo))]
    public class SelectXmlCommand : PSCmdlet
    {
        # region parameters
        /// <summary>
        /// Specifies the path which contains the xml files. The default is the current
        /// user directory
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Position = 1, Mandatory = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public String[] Path { get; set; }

        /// <summary>
        /// Specifies the literal path which contains the xml files. The default is the current
        /// user directory
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "LiteralPath")]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath")]
        public String[] LiteralPath
        {
            get { return Path; }
            set
            {
                Path = value;
                _isLiteralPath = true;
            }
        }
        private bool _isLiteralPath = false;

        /// <summary>
        /// The following is the definition of the input parameter "XML".
        /// Specifies the xml Node
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true, ValueFromPipeline = true,
                   ParameterSetName = "Xml")]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        [Alias("Node")]
        public System.Xml.XmlNode[] Xml { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter in string format.
        /// Specifies the string format of a fully qualified xml.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [Parameter(Mandatory = true, ValueFromPipeline = true,
                   ParameterSetName = "Content")]
        [ValidateNotNullOrEmpty]
        public string[] Content { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Xpath".
        /// Specifies the String in XPath language syntax. The xml documents will be
        /// searched for the nodes/values represented by this parameter.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string XPath { get; set; }

        /// <summary>
        /// The following definition used to specify the
        /// NameSpace of xml.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Hashtable Namespace { get; set; }

        # endregion parameters

        # region private

        private void WriteResults(XmlNodeList foundXmlNodes, string filePath)
        {
            Dbg.Assert(foundXmlNodes != null, "Caller should verify foundNodes != null");

            foreach (XmlNode foundXmlNode in foundXmlNodes)
            {
                SelectXmlInfo selectXmlInfo = new SelectXmlInfo();
                selectXmlInfo.Node = foundXmlNode;
                selectXmlInfo.Pattern = XPath;
                selectXmlInfo.Path = filePath;

                this.WriteObject(selectXmlInfo);
            }
        }

        private void ProcessXmlNode(XmlNode xmlNode, string filePath)
        {
            Dbg.Assert(xmlNode != null, "Caller should verify xmlNode != null");

            XmlNodeList xList;
            if (Namespace != null)
            {
                XmlNamespaceManager xmlns = AddNameSpaceTable(this.ParameterSetName, xmlNode as XmlDocument, Namespace);
                xList = xmlNode.SelectNodes(XPath, xmlns);
            }
            else
            {
                xList = xmlNode.SelectNodes(XPath);
            }
            this.WriteResults(xList, filePath);
        }

        private void ProcessXmlFile(string filePath)
        {
            //Cannot use ImportXMLHelper because it will throw terminating error which will
            //not be inline with Select-String
            //So doing self processing of the file.
            try
            {
                XmlDocument xmlDocument = InternalDeserializer.LoadUnsafeXmlDocument(
                    new FileInfo(filePath),
                    true, /* preserve whitespace, comments, etc. */
                    null); /* default maxCharactersInDocument */

                this.ProcessXmlNode(xmlDocument, filePath);
            }
            catch (NotSupportedException notSupportedException)
            {
                this.WriteFileReadError(filePath, notSupportedException);
            }
            catch (IOException ioException)
            {
                this.WriteFileReadError(filePath, ioException);
            }
            catch (SecurityException securityException)
            {
                this.WriteFileReadError(filePath, securityException);
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                this.WriteFileReadError(filePath, unauthorizedAccessException);
            }
            catch (XmlException xmlException)
            {
                this.WriteFileReadError(filePath, xmlException);
            }
            catch (InvalidOperationException invalidOperationException)
            {
                this.WriteFileReadError(filePath, invalidOperationException);
            }
        }

        private void WriteFileReadError(string filePath, Exception exception)
        {
            string errorMessage = string.Format(
                CultureInfo.InvariantCulture,
                // filePath is culture invariant, exception message is to be copied verbatim
                UtilityCommonStrings.FileReadError,
                filePath,
                exception.Message);

            ArgumentException argumentException = new ArgumentException(errorMessage, exception);
            ErrorRecord errorRecord = new ErrorRecord(argumentException, "ProcessingFile", ErrorCategory.InvalidArgument, filePath);

            this.WriteError(errorRecord);
        }

        private XmlNamespaceManager AddNameSpaceTable(string parametersetname, XmlDocument xDoc, Hashtable namespacetable)
        {
            XmlNamespaceManager xmlns;
            if (parametersetname.Equals("Xml"))
            {
                XmlNameTable xmlnt = new NameTable();
                xmlns = new XmlNamespaceManager(xmlnt);
            }
            else
            {
                xmlns = new XmlNamespaceManager(xDoc.NameTable);
            }

            foreach (DictionaryEntry row in namespacetable)
            {
                try
                {
                    xmlns.AddNamespace(row.Key.ToString(), row.Value.ToString());
                }
                catch (NullReferenceException)
                {
                    string message = StringUtil.Format(UtilityCommonStrings.SearchXMLPrefixNullError);
                    InvalidOperationException e = new InvalidOperationException(message);
                    ErrorRecord er = new ErrorRecord(e, "PrefixError", ErrorCategory.InvalidOperation, namespacetable);
                    WriteError(er);
                }
                catch (ArgumentNullException)
                {
                    string message = StringUtil.Format(UtilityCommonStrings.SearchXMLPrefixNullError);
                    InvalidOperationException e = new InvalidOperationException(message);
                    ErrorRecord er = new ErrorRecord(e, "PrefixError", ErrorCategory.InvalidOperation, namespacetable);
                    WriteError(er);
                }
            }

            return xmlns;
        }

        # endregion private

        #region override

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals("Xml", StringComparison.OrdinalIgnoreCase))
            {
                foreach (XmlNode xmlNode in this.Xml)
                {
                    ProcessXmlNode(xmlNode, string.Empty);
                }
            }
            else if (
                (ParameterSetName.Equals("Path", StringComparison.OrdinalIgnoreCase) ||
                (ParameterSetName.Equals("LiteralPath", StringComparison.OrdinalIgnoreCase))))
            {
                //If any file not resolved, execution stops. this is to make consistent with select-string.
                List<string> fullresolvedPaths = new List<string>();
                foreach (string fpath in Path)
                {
                    if (_isLiteralPath)
                    {
                        string resolvedPath = GetUnresolvedProviderPathFromPSPath(fpath);
                        fullresolvedPaths.Add(resolvedPath);
                    }
                    else
                    {
                        ProviderInfo provider;
                        Collection<string> resolvedPaths = GetResolvedProviderPathFromPSPath(fpath, out provider);
                        if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
                        {
                            //Cannot open File error
                            string message = StringUtil.Format(UtilityCommonStrings.FileOpenError, provider.FullName);
                            InvalidOperationException e = new InvalidOperationException(message);
                            ErrorRecord er = new ErrorRecord(e, "ProcessingFile", ErrorCategory.InvalidOperation, fpath);
                            WriteError(er);
                            continue;
                        }
                        fullresolvedPaths.AddRange(resolvedPaths);
                    }
                }
                foreach (string file in fullresolvedPaths)
                {
                    ProcessXmlFile(file);
                }
            }
            else if (ParameterSetName.Equals("Content", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string xmlstring in Content)
                {
                    XmlDocument xmlDocument;
                    try
                    {
                        xmlDocument = (XmlDocument)LanguagePrimitives.ConvertTo(xmlstring, typeof(XmlDocument), CultureInfo.InvariantCulture);
                    }
                    catch (PSInvalidCastException invalidCastException)
                    {
                        this.WriteError(invalidCastException.ErrorRecord);
                        continue;
                    }

                    this.ProcessXmlNode(xmlDocument, string.Empty);
                }
            }
            else
            {
                Dbg.Assert(false, "Unrecognized parameterset");
            }
        }//End ProcessRecord()

        #endregion overrides
    }//End Class

    /// <summary>
    /// The object returned by Select-Xml representing the result of a match.
    /// </summary>
    public sealed class SelectXmlInfo
    {
        /// <summary>
        /// If the object is InputObject, Input Stream is used.
        /// </summary>
        private const string inputStream = "InputStream";
        private const string MatchFormat = "{0}:{1}";
        private const string SimpleFormat = "{0}";

        /// <summary>
        /// The XmlNode that matches search
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public XmlNode Node { get; set; }

        /// <summary>
        /// The FileName from which the match is found.
        /// </summary>
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    _path = inputStream;
                }
                else
                {
                    _path = value;
                }
            }
        }
        private string _path;

        /// <summary>
        /// The pattern used to search
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Returns the string representation of this object. The format
        /// depends on whether a path has been set for this object or not.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Return String representation of the object
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private string ToString(string directory)
        {
            string displayPath = (directory != null) ? RelativePath(directory) : _path;
            return FormatLine(GetNodeText(), displayPath);
        }

        /// <summary>
        /// Returns the XmlNode Value or InnerXml.
        /// </summary>
        /// <returns></returns>
        internal string GetNodeText()
        {
            string nodeText = String.Empty;
            if (Node != null)
            {
                if (Node.Value != null)
                {
                    nodeText = Node.Value.Trim();
                }
                else
                {
                    nodeText = Node.InnerXml.Trim();
                }
            }
            return nodeText;
        }

        /// <summary>
        /// Returns the path of the matching file truncated relative to the <paramref name="directory"/> parameter.
        /// <remarks>
        /// For example, if the matching path was c:\foo\bar\baz.c and the directory argument was c:\foo
        /// the routine would return bar\baz.c
        /// </remarks>
        /// </summary>
        /// <param name="directory">The directory base the truncation on.</param>
        /// <returns>The relative path that was produced.</returns>
        private string RelativePath(string directory)
        {
            string relPath = _path;
            if (!relPath.Equals(inputStream))
            {
                if (relPath.StartsWith(directory, StringComparison.CurrentCultureIgnoreCase))
                {
                    int offset = directory.Length;
                    if (offset < relPath.Length)
                    {
                        if (directory[offset - 1] == '\\' || directory[offset - 1] == '/')
                            relPath = relPath.Substring(offset);
                        else if (relPath[offset] == '\\' || relPath[offset] == '/')
                            relPath = relPath.Substring(offset + 1);
                    }
                }
            }
            return relPath;
        }

        /// <summary>
        /// Formats a line for use in ToString.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="displaypath"></param>
        /// <returns></returns>
        private string FormatLine(string text, string displaypath)
        {
            if (_path.Equals(inputStream))
            {
                return StringUtil.Format(SimpleFormat, text);
            }
            else
            {
                return StringUtil.Format(MatchFormat, text, displaypath);
            }
        }
    }

    #endregion Select-Xml
}

