// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class describes the metadata for a specific Task
** defined by a Provider. An instance of this class is obtained
** from a ProviderMetadata object.
**
============================================================*/

using System.Collections.Generic;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the metadata for a specific Task defined by a Provider.
    /// An instance of this class is obtained from a ProviderMetadata object.
    /// </summary>
    public sealed class EventTask
    {
        private int _value;
        private string _name;
        private string _displayName;
        private Guid _guid;
        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        // called from EventMetadata
        internal EventTask(int value, ProviderMetadata pmReference)
        {
            _value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        // called from ProviderMetadata
        internal EventTask(string name, int value, string displayName, Guid guid)
        {
            _value = value;
            _name = name;
            _displayName = displayName;
            _guid = guid;
            _dataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            lock (_syncObject)
            {
                if (_dataReady == true) return;

                IEnumerable<EventTask> result = _pmReference.Tasks;

                _name = null;
                _displayName = null;
                _guid = Guid.Empty;
                _dataReady = true;

                foreach (EventTask task in result)
                {
                    if (task.Value == _value)
                    {
                        _name = task.Name;
                        _displayName = task.DisplayName;
                        _guid = task.EventGuid;
                        _dataReady = true;
                        break;
                    }
                }
            }
        }

        public string Name
        {
            get
            {
                PrepareData();
                return _name;
            }
        }

        public int Value
        {
            get
            {
                return _value;
            }
        }

        public string DisplayName
        {
            get
            {
                PrepareData();
                return _displayName;
            }
        }

        public Guid EventGuid
        {
            get
            {
                PrepareData();
                return _guid;
            }
        }
    }
}
