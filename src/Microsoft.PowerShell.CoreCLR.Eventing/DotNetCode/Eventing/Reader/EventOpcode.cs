// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public class describes the metadata for a specific Opcode
** defined by a Provider. An instance of this class is obtained from
** a ProviderMetadata object.
**
============================================================*/

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
    public sealed class EventOpcode
    {
        private int _value;
        private string _name;
        private string _displayName;
        private bool _dataReady;
        private ProviderMetadata _pmReference;
        private object _syncObject;

        // call from EventMetadata
        internal EventOpcode(int value, ProviderMetadata pmReference)
        {
            _value = value;
            _pmReference = pmReference;
            _syncObject = new object();
        }

        // call from ProviderMetadata
        internal EventOpcode(string name, int value, string displayName)
        {
            _value = value;
            _name = name;
            _displayName = displayName;
            _dataReady = true;
            _syncObject = new object();
        }

        internal void PrepareData()
        {
            lock (_syncObject)
            {
                if (_dataReady == true) return;

                // get the data
                IEnumerable<EventOpcode> result = _pmReference.Opcodes;
                // set the names and display names to null
                _name = null;
                _displayName = null;
                _dataReady = true;
                foreach (EventOpcode op in result)
                {
                    if (op.Value == _value)
                    {
                        _name = op.Name;
                        _displayName = op.DisplayName;
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
    }
}
