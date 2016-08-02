//
//    Copyright (C) Microsoft.  All rights reserved.
//

namespace Microsoft.PowerShell.Commands
{
    using System.Management.Automation;
    using System;

    internal class ScalarTypeColumnInfo : ColumnInfo
    {
        private Type _type;

        internal ScalarTypeColumnInfo(Type type)
            : base(type.Name, type.Name)
        {
            _type = type;
        }

        internal override Object GetValue(PSObject liveObject)
        {
            // Strip a wrapping PSObject.
            object baseObject = ((PSObject)liveObject).BaseObject;
            if (baseObject.GetType().Equals(_type))
            {
                return ColumnInfo.LimitString(baseObject);
            }
            return null;
        }
    }

    internal class TypeNameColumnInfo : ColumnInfo
    {
        internal TypeNameColumnInfo(string staleObjectPropertyName, string displayName)
            : base(staleObjectPropertyName, displayName)
        { }

        internal override Object GetValue(PSObject liveObject)
        {
            // Strip a wrapping PSObject.
            object baseObject = ((PSObject)liveObject).BaseObject;
            return baseObject.GetType().FullName;
        }
    }

    internal class ToStringColumnInfo : ColumnInfo
    {
        private OutGridViewCommand _parentCmdlet;

        internal ToStringColumnInfo(string staleObjectPropertyName, string displayName, OutGridViewCommand parentCmdlet)
            : base(staleObjectPropertyName, displayName)
        {
            _parentCmdlet = parentCmdlet;
        }

        internal override Object GetValue(PSObject liveObject)
        {
            // Convert to a string preserving PowerShell formatting.
            return ColumnInfo.LimitString(_parentCmdlet.ConvertToString(liveObject));
        }
    }

    internal class IndexColumnInfo : ColumnInfo
    {
        private int _index = 0;

        internal IndexColumnInfo(string staleObjectPropertyName, string displayName, int index)
            : base(staleObjectPropertyName, displayName)
        {
            _index = index;
        }

        internal override Object GetValue(PSObject liveObject)
        {
            // Every time this method is called, another raw is added to ML.
            return _index++;
        }
    }
}