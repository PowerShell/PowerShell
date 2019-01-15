// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// Wraps Hitesh's xml serializer in such a way that it will select the proper serializer based on the data
    /// format.
    /// </summary>

    internal class Serialization
    {
        /// <summary>
        /// Describes the format of the data streamed between minishells, e.g. the allowed arguments to the minishell
        /// -outputformat and -inputformat command line parameters.
        /// </summary>

        internal enum DataFormat
        {
            /// <summary>
            /// Text format -- i.e. stream text just as out-default would display it.
            /// </summary>

            Text = 0,

            /// <summary>
            /// XML-serialized format.
            /// </summary>

            XML = 1,

            /// <summary>
            /// Indicates that the data should be discarded instead of processed.
            /// </summary>
            None = 2
        }

        protected
        Serialization(DataFormat dataFormat, string streamName)
        {
            Dbg.Assert(!string.IsNullOrEmpty(streamName), "stream needs a name");

            format = dataFormat;
            this.streamName = streamName;
        }

        protected static string XmlCliTag = "#< CLIXML";

        protected string streamName;
        protected DataFormat format;
    }

    internal
    class WrappedSerializer : Serialization
    {
        internal
        WrappedSerializer(DataFormat dataFormat, string streamName, TextWriter output)
            :
            base(dataFormat, streamName)
        {
            Dbg.Assert(output != null, "output should have a value");

            textWriter = output;
            switch (format)
            {
                case DataFormat.XML:
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.CheckCharacters = false;
                    settings.OmitXmlDeclaration = true;
                    _xmlWriter = XmlWriter.Create(textWriter, settings);
                    _xmlSerializer = new Serializer(_xmlWriter);
                    break;
                case DataFormat.Text:
                default:
                    // do nothing; we'll just write to the TextWriter
                    // or discard it.

                    break;
            }
        }

        internal
        void
        Serialize(object o)
        {
            Serialize(o, this.streamName);
        }

        internal
        void
        Serialize(object o, string streamName)
        {
            switch (format)
            {
                case DataFormat.None:
                    break;
                case DataFormat.XML:
                    if (_firstCall)
                    {
                        _firstCall = false;
                        textWriter.WriteLine(Serialization.XmlCliTag);
                    }

                    _xmlSerializer.Serialize(o, streamName);
                    break;
                case DataFormat.Text:
                default:
                    textWriter.Write(o.ToString());
                    break;
            }
        }

        internal
        void
        End()
        {
            switch (format)
            {
                case DataFormat.None:
                    // do nothing
                    break;

                case DataFormat.XML:
                    _xmlSerializer.Done();
                    _xmlSerializer = null;
                    break;

                case DataFormat.Text:
                default:
                    // do nothing

                    break;
            }
        }

        internal TextWriter textWriter;
        private XmlWriter _xmlWriter;
        private Serializer _xmlSerializer;
        private bool _firstCall = true;
    }

    internal
    class WrappedDeserializer : Serialization
    {
        internal
        WrappedDeserializer(DataFormat dataFormat, string streamName, TextReader input)
            :
            base(dataFormat, streamName)
        {
            Dbg.Assert(input != null, "input should have a value");

            // If the data format is none - do nothing...
            if (dataFormat == DataFormat.None)
                return;

            textReader = input;
            _firstLine = textReader.ReadLine();
            if (string.Compare(_firstLine, Serialization.XmlCliTag, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // format should be XML

                dataFormat = DataFormat.XML;
            }

            switch (format)
            {
                case DataFormat.XML:
                    _xmlReader = XmlReader.Create(textReader,  new XmlReaderSettings { XmlResolver = null });
                    _xmlDeserializer = new Deserializer(_xmlReader);
                    break;
                case DataFormat.Text:
                default:
                    // do nothing; we'll just read from the TextReader

                    break;
            }
        }

        internal
        object
        Deserialize()
        {
            object o;
            switch (format)
            {
                case DataFormat.None:
                    _atEnd = true;
                    return null;

                case DataFormat.XML:
                    string unused;
                    o = _xmlDeserializer.Deserialize(out unused);
                    break;

                case DataFormat.Text:
                default:
                    if (_atEnd)
                    {
                        return null;
                    }

                    if (_firstLine != null)
                    {
                        o = _firstLine;
                        _firstLine = null;
                    }
                    else
                    {
                        o = textReader.ReadLine();
                        if (o == null)
                        {
                            _atEnd = true;
                        }
                    }

                    break;
            }

            return o;
        }

        internal
        bool
        AtEnd
        {
            get
            {
                bool result = false;
                switch (format)
                {
                    case DataFormat.None:
                        _atEnd = true;
                        result = true;
                        break;

                    case DataFormat.XML:
                        result = _xmlDeserializer.Done();
                        break;

                    case DataFormat.Text:
                    default:
                        result = _atEnd;
                        break;
                }

                return result;
            }
        }

        internal
        void
        End()
        {
            switch (format)
            {
                case DataFormat.None:
                case DataFormat.XML:
                case DataFormat.Text:
                default:
                    // do nothing

                    break;
            }
        }

        internal TextReader textReader;
        private XmlReader _xmlReader;
        private Deserializer _xmlDeserializer;
        private string _firstLine;
        private bool _atEnd;
    }
}   // namespace

