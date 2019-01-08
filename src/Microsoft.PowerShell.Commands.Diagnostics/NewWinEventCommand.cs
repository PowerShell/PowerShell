// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Globalization;
using System.Reflection;
using System.Diagnostics.Eventing;
using System.Diagnostics.Eventing.Reader;
using System.Resources;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace Microsoft.PowerShell.Commands
{
    ///
    /// Class that implements the New-WinEvent cmdlet.
    /// This cmdlet writes a new Etw event using the provider specified in parameter.
    ///
    [Cmdlet(VerbsCommon.New, "WinEvent", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=217469")]
    public sealed class NewWinEventCommand : PSCmdlet
    {
        private ProviderMetadata _providerMetadata;
        private EventDescriptor? _eventDescriptor;

        private const string TemplateTag = "template";
        private const string DataTag = "data";
        private ResourceManager _resourceMgr = Microsoft.PowerShell.Commands.Diagnostics.Common.CommonUtilities.GetResourceManager();

        /// <summary>
        /// ProviderName.
        /// </summary>
        [Parameter(
            Position = 0,
            Mandatory = true,
            ParameterSetName = ParameterAttribute.AllParameterSets)]
        public string ProviderName
        {
            get
            {
                return _providerName;
            }

            set
            {
                _providerName = value;
            }
        }

        private string _providerName;

        /// <summary>
        /// Id (EventId defined in manifest file)
        /// </summary>
        [Parameter(
            Position = 1,
            Mandatory = true,
            ParameterSetName = ParameterAttribute.AllParameterSets)]
        public int Id
        {
            get
            {
                return _id;
            }

            set
            {
                _id = value;
                _idSpecified = true;
            }
        }

        private int _id;
        private bool _idSpecified = false;

        /// <summary>
        /// Version (event version)
        /// </summary>
        [Parameter(
            Mandatory = false,
            ParameterSetName = ParameterAttribute.AllParameterSets)]
        public byte Version
        {
            get
            {
                return _version;
            }

            set
            {
                _version = value;
                _versionSpecified = true;
            }
        }

        private byte _version;
        private bool _versionSpecified = false;

        /// <summary>
        /// Event Payload.
        /// </summary>
        [Parameter(
            Position = 2,
            Mandatory = false,
            ParameterSetName = ParameterAttribute.AllParameterSets),
        AllowEmptyCollection,
        SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",
            Target = "Microsoft.PowerShell.Commands",
            Justification = "A string[] is required here because that is the type Powershell supports")]
        public object[] Payload
        {
            get
            {
                return _payload;
            }

            set
            {
                _payload = value;
            }
        }

        private object[] _payload;

        /// <summary>
        /// BeginProcessing.
        /// </summary>
        protected override void BeginProcessing()
        {
            LoadProvider();
            LoadEventDescriptor();

            base.BeginProcessing();
        }

        private void LoadProvider()
        {
            if (string.IsNullOrEmpty(_providerName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ProviderNotSpecified")), "ProviderName");
            }

            using (EventLogSession session = new EventLogSession())
            {
                foreach (string providerName in session.GetProviderNames())
                {
                    if (string.Equals(providerName, _providerName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            _providerMetadata = new ProviderMetadata(providerName);
                        }
                        catch (EventLogException exc)
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("ProviderMetadataUnavailable"), providerName, exc.Message);
                            throw new Exception(msg, exc);
                        }

                        break;
                    }
                }
            }

            if (_providerMetadata == null)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("NoProviderFound"), _providerName);
                throw new ArgumentException(msg);
            }
        }

        private void LoadEventDescriptor()
        {
            if (_idSpecified)
            {
                List<EventMetadata> matchedEvents = new List<EventMetadata>();
                foreach (EventMetadata emd in _providerMetadata.Events)
                {
                    if (emd.Id == _id)
                    {
                        matchedEvents.Add(emd);
                    }
                }

                if (matchedEvents.Count == 0)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        _resourceMgr.GetString("IncorrectEventId"),
                        _id,
                        _providerName);
                    throw new EventWriteException(msg);
                }

                EventMetadata matchedEvent = null;
                if (!_versionSpecified && matchedEvents.Count == 1)
                {
                    matchedEvent = matchedEvents[0];
                }
                else
                {
                    if (_versionSpecified)
                    {
                        foreach (EventMetadata emd in matchedEvents)
                        {
                            if (emd.Version == _version)
                            {
                                matchedEvent = emd;
                                break;
                            }
                        }

                        if (matchedEvent == null)
                        {
                            string msg = string.Format(CultureInfo.InvariantCulture,
                                _resourceMgr.GetString("IncorrectEventVersion"),
                                _version,
                                _id,
                                _providerName);

                            throw new EventWriteException(msg);
                        }
                    }
                    else
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture,
                            _resourceMgr.GetString("VersionNotSpecified"),
                            _id,
                            _providerName);

                        throw new EventWriteException(msg);
                    }
                }

                VerifyTemplate(matchedEvent);
                _eventDescriptor = CreateEventDescriptor(_providerMetadata, matchedEvent);
            }
            else
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("EventIdNotSpecified")), "Id");
            }
        }

        private bool VerifyTemplate(EventMetadata emd)
        {
            if (emd.Template != null)
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    MaxCharactersInDocument = 0, // no limit
                    ConformanceLevel = ConformanceLevel.Fragment,
                    XmlResolver = null
                };

                int definedParameterCount = 0;
                using (XmlReader reader = XmlReader.Create(new StringReader(emd.Template), readerSettings))
                {
                    if (reader.ReadToFollowing(TemplateTag))
                    {
                        bool found = reader.ReadToDescendant(DataTag);
                        while (found)
                        {
                            definedParameterCount++;
                            found = reader.ReadToFollowing(DataTag);
                        }
                    }
                }

                if ((_payload == null && definedParameterCount != 0)
                    || ((_payload != null) && _payload.Length != definedParameterCount))
                {
                    string warning = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("PayloadMismatch"), _id, emd.Template);
                    WriteWarning(warning);

                    return false;
                }
            }

            return true;
        }

        private static EventDescriptor CreateEventDescriptor(ProviderMetadata providerMetaData, EventMetadata emd)
        {
            long keywords = 0;
            foreach (EventKeyword keyword in emd.Keywords)
            {
                keywords |= keyword.Value;
            }

            byte channel = 0;
            foreach (EventLogLink logLink in providerMetaData.LogLinks)
            {
                if (string.Equals(logLink.LogName, emd.LogLink.LogName, StringComparison.OrdinalIgnoreCase))
                    break;
                channel++;
            }

            return new EventDescriptor(
                (int)emd.Id,
                emd.Version,
                channel,
                (byte)emd.Level.Value,
                (byte)emd.Opcode.Value,
                emd.Task.Value,
                keywords);
        }

        /// <summary>
        /// ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            using (EventProvider provider = new EventProvider(_providerMetadata.Id))
            {
                EventDescriptor ed = _eventDescriptor.Value;

                if (_payload != null && _payload.Length > 0)
                {
                    for (int i = 0; i < _payload.Length; i++)
                    {
                        if (_payload[i] == null)
                        {
                            _payload[i] = string.Empty;
                        }
                    }

                    provider.WriteEvent(ref ed, _payload);
                }
                else
                {
                    provider.WriteEvent(ref ed);
                }
            }

            base.ProcessRecord();
        }

        /// <summary>
        /// EndProcessing.
        /// </summary>
        protected override void EndProcessing()
        {
            if (_providerMetadata != null)
                _providerMetadata.Dispose();

            base.EndProcessing();
        }
    }

    internal class EventWriteException : Exception
    {
        internal EventWriteException(string msg, Exception innerException)
            : base(msg, innerException)
        { }

        internal EventWriteException(string msg)
            : base(msg)
        { }
    }
}
