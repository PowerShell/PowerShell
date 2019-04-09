// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing
{
    public class EventProviderTraceListener : TraceListener
    {
        //
        // The listener uses the EtwProvider base class.
        // Because Listener data is not schematized at the moment the listener will
        // log events using WriteMessageEvent method.
        //
        // Because WriteMessageEvent takes a string as the event payload
        // all the overridden logging methods convert the arguments into strings.
        // Event payload is "delimiter" separated, which can be configured
        //
        //
        private EventProvider _provider;
        private const string s_nullStringValue = "null";
        private const string s_nullStringComaValue = "null,";
        private const string s_nullCStringValue = ": null";
        private string _delimiter = ";";
        private const uint s_keyWordMask = 0xFFFFFF00;
        private const int s_defaultPayloadSize = 512;

        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public string Delimiter
        {
            get
            {
                return _delimiter;
            }

            [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Delimiter");

                if (value.Length == 0)
                    throw new ArgumentException(DotNetEventingStrings.Argument_NeedNonemptyDelimiter);

                _delimiter = value;
            }
        }

        /// <summary>
        /// This method creates an instance of the ETW provider.
        /// The guid argument must be a valid GUID or a format exception will be
        /// thrown when creating an instance of the ControlGuid.
        /// We need to be running on Vista or above. If not an
        /// PlatformNotSupported exception will be thrown by the EventProvider.
        /// </summary>
        public EventProviderTraceListener(string providerId)
        {
            InitProvider(providerId);
        }

        public EventProviderTraceListener(string providerId, string name)
            : base(name)
        {
            InitProvider(providerId);
        }

        public EventProviderTraceListener(string providerId, string name, string delimiter)
            : base(name)
        {
            if (delimiter == null)
                throw new ArgumentNullException("delimiter");

            if (delimiter.Length == 0)
                throw new ArgumentException(DotNetEventingStrings.Argument_NeedNonemptyDelimiter);

            _delimiter = delimiter;
            InitProvider(providerId);
        }

        private void InitProvider(string providerId)
        {
            Guid controlGuid = new Guid(providerId);
            //
            // Create The ETW TraceProvider
            //

            _provider = new EventProvider(controlGuid);
        }

        //
        // override Listener methods
        //
        public sealed override void Flush()
        {
        }

        public sealed override bool IsThreadSafe
        {
            get
            {
                return true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _provider.Close();
            }
        }

        public sealed override void Write(string message)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            _provider.WriteMessageEvent(message, (byte)TraceEventType.Information, 0);
        }

        public sealed override void WriteLine(string message)
        {
            Write(message);
        }

        //
        // For all the methods below the string to be logged contains:
        // m_delimiter separated data converted to string
        //
        // The source parameter is ignored.
        //
        public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, object data)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
            {
                return;
            }

            StringBuilder dataString = new StringBuilder(s_defaultPayloadSize);

            if (data != null)
            {
                dataString.Append(data.ToString());
            }
            else
            {
                dataString.Append(s_nullCStringValue);
            }

            _provider.WriteMessageEvent(dataString.ToString(),
                            (byte)eventType,
                            (long)eventType & s_keyWordMask);
        }

        public sealed override void TraceData(TraceEventCache eventCache, string source, TraceEventType eventType, int id, params object[] data)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
            {
                return;
            }

            int index;
            StringBuilder dataString = new StringBuilder(s_defaultPayloadSize);

            if ((data != null) && (data.Length > 0))
            {
                for (index = 0; index < (data.Length - 1); index++)
                {
                    if (data[index] != null)
                    {
                        dataString.Append(data[index].ToString());
                        dataString.Append(Delimiter);
                    }
                    else
                    {
                        dataString.Append(s_nullStringComaValue);
                    }
                }

                if (data[index] != null)
                {
                    dataString.Append(data[index].ToString());
                }
                else
                {
                    dataString.Append(s_nullStringValue);
                }
            }
            else
            {
                dataString.Append(s_nullStringValue);
            }

            _provider.WriteMessageEvent(dataString.ToString(),
                            (byte)eventType,
                            (long)eventType & s_keyWordMask);
        }

        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
            {
                return;
            }

            _provider.WriteMessageEvent(string.Empty,
                            (byte)eventType,
                            (long)eventType & s_keyWordMask);
        }

        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
            {
                return;
            }

            StringBuilder dataString = new StringBuilder(s_defaultPayloadSize);
            dataString.Append(message);

            _provider.WriteMessageEvent(dataString.ToString(),
                            (byte)eventType,
                            (long)eventType & s_keyWordMask);
        }

        public sealed override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (!_provider.IsEnabled())
            {
                return;
            }

            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, null, null, null, null))
            {
                return;
            }

            if (args == null)
            {
                _provider.WriteMessageEvent(format,
                                (byte)eventType,
                                (long)eventType & s_keyWordMask);
            }
            else
            {
                _provider.WriteMessageEvent(string.Format(CultureInfo.InvariantCulture, format, args),
                                (byte)eventType,
                                (long)eventType & s_keyWordMask);
            }
        }

        public override void Fail(string message, string detailMessage)
        {
            StringBuilder failMessage = new StringBuilder(message);
            if (detailMessage != null)
            {
                failMessage.Append(" ");
                failMessage.Append(detailMessage);
            }

            this.TraceEvent(null, null, TraceEventType.Error, 0, failMessage.ToString());
        }
    }
}
