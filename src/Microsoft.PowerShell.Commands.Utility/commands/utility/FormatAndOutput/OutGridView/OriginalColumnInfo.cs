// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    internal class OriginalColumnInfo : ColumnInfo
    {
        private readonly string _liveObjectPropertyName;
        private readonly OutGridViewCommand _parentCmdlet;

        internal OriginalColumnInfo(string staleObjectPropertyName, string displayName, string liveObjectPropertyName, OutGridViewCommand parentCmdlet)
            : base(staleObjectPropertyName, displayName)
        {
            _liveObjectPropertyName = liveObjectPropertyName;
            _parentCmdlet = parentCmdlet;
        }

        internal override object GetValue(PSObject liveObject)
        {
            try
            {
                PSPropertyInfo propertyInfo = liveObject.Properties[_liveObjectPropertyName];
                if (propertyInfo == null)
                {
                    return null;
                }

                // The live object has the liveObjectPropertyName property.
                object liveObjectValue = propertyInfo.Value;
                if (liveObjectValue is ICollection collectionValue)
                {
                    liveObjectValue = _parentCmdlet.ConvertToString(PSObjectHelper.AsPSObject(propertyInfo.Value));
                }
                else
                {
                    if (liveObjectValue is PSObject psObjectValue)
                    {
                        // Since PSObject implements IComparable there is a need to verify if its BaseObject actually implements IComparable.
                        if (psObjectValue.BaseObject is IComparable)
                        {
                            liveObjectValue = psObjectValue;
                        }
                        else
                        {
                            // Use the String type as default.
                            liveObjectValue = _parentCmdlet.ConvertToString(psObjectValue);
                        }
                    }
                }

                return ColumnInfo.LimitString(liveObjectValue);
            }
            catch (GetValueException)
            {
                // ignore
            }
            catch (ExtendedTypeSystemException)
            {
                // ignore
            }

            return null;
        }
    }
}
