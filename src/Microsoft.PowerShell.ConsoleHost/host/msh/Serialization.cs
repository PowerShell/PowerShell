/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Text;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using Microsoft.PowerShell;
using System.Xml;


using Dbg = System.Management.Automation.Diagnostics;



namespace Microsoft.PowerShell
{
    /// <summary>
    /// 
    /// Wraps Hitesh's xml serializer in such a way that it will select the proper serializer based on the data
    /// format.
    /// 
    /// </summary>

    internal class Serialization
    {
        /// <summary>
        /// 
        /// Describes the format of the data streamed between minishells, e.g. the allowed arguments to the minishell 
        /// -outputformat and -inputformat command line parameters.
        /// 
        /// </summary>

        internal enum DataFormat
        {


            /// <summary>
            /// 
            /// text format -- i.e. stream text just as out-default would display it.
            /// 
            /// </summary>

            Text = 0,

            /// <summary>
            /// 
            /// XML-serialized format
            /// 
            /// </summary>

            XML = 1,

            /// <summary>
            /// 
            /// Indicates that the data should be discarded instead of processed.
            /// 
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
                    xmlWriter = XmlWriter.Create(textWriter, settings);
                    xmlSerializer = new Serializer(xmlWriter);
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
                    if (firstCall)
                    {
                        firstCall = false;
                        textWriter.WriteLine(Serialization.XmlCliTag);
                    }
                    xmlSerializer.Serialize(o, streamName);
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
                    xmlSerializer.Done();
                    xmlSerializer = null;
                    break;

                case DataFormat.Text:
                default:
                    // do nothing

                    break;
            }
        }


        internal TextWriter textWriter;
        private XmlWriter xmlWriter;
        private Serializer xmlSerializer;
        bool firstCall = true;
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
            firstLine = textReader.ReadLine();
            if (String.Compare(firstLine, Serialization.XmlCliTag, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // format should be XML

                dataFormat = DataFormat.XML;
            }

            switch (format)
            {
                case DataFormat.XML:
                    xmlReader = XmlReader.Create(textReader);
                    xmlDeserializer = new Deserializer(xmlReader);
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
                    atEnd = true;
                    return null;

                case DataFormat.XML:
                    string unused;
                    o = xmlDeserializer.Deserialize(out unused);
                    break;

                case DataFormat.Text:
                default:
                    if (atEnd)
                    {
                        return null;
                    }
                    if (firstLine != null)
                    {
                        o = firstLine;
                        firstLine = null;
                    }
                    else
                    {
                        o = textReader.ReadLine();
                        if (o == null)
                        {
                            atEnd = true;
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
                        atEnd = true;
                        result = true;
                        break;

                    case DataFormat.XML:
                        result = xmlDeserializer.Done();
                        break;

                    case DataFormat.Text:
                    default:
                        result = atEnd;
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
        private XmlReader xmlReader;
        private Deserializer xmlDeserializer;
        private string firstLine;
        bool atEnd;
    }

}   // namespace 


