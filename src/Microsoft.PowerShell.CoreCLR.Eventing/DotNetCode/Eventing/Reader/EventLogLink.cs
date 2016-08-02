
/*============================================================
**
**
** Purpose: 
** This public class describes the metadata for a specific Log 
** Reference defined by a Provider. An instance of this class is obtained from 
** a ProviderMetadata object.
**
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the metadata for a specific Log Reference defined
    /// by a Provider. An instance of this class is obtained from 
    /// a ProviderMetadata object.
    /// </summary>
    public sealed class EventLogLink
    {
        private string _channelName;
        private bool _isImported;
        private string _displayName;
        private uint _channelId;

        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        internal EventLogLink(uint channelId, ProviderMetadata pmReference)
        {
            _channelId = channelId;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        internal EventLogLink(string channelName, bool isImported, string displayName, uint channelId)
        {
            _channelName = channelName;
            _isImported = isImported;
            _displayName = displayName;
            _channelId = channelId;

            _dataReady = true;
            _syncObject = new object();
        }

        private void PrepareData()
        {
            if (_dataReady == true) return;

            lock (_syncObject)
            {
                if (_dataReady == true) return;

                IEnumerable<EventLogLink> result = _pmReference.LogLinks;

                _channelName = null;
                _isImported = false;
                _displayName = null;
                _dataReady = true;

                foreach (EventLogLink ch in result)
                {
                    if (ch.ChannelId == _channelId)
                    {
                        _channelName = ch.LogName;
                        _isImported = ch.IsImported;
                        _displayName = ch.DisplayName;

                        _dataReady = true;

                        break;
                    }
                }
            }
        }


        public string LogName
        {
            get
            {
                this.PrepareData();
                return _channelName;
            }
        }

        public bool IsImported
        {
            get
            {
                this.PrepareData();
                return _isImported;
            }
        }

        public string DisplayName
        {
            get
            {
                this.PrepareData();
                return _displayName;
            }
        }

        internal uint ChannelId
        {
            get
            {
                return _channelId;
            }
        }
    }
}
