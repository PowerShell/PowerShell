// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/*
 * The registry wrapper provides a common interface to both the transacted
 * and non-transacted registry APIs.  It is used exclusively by the registry provider
 * to perform registry operations.  In most cases, the wrapper simply forwards the
 * call to the appropriate registry API.
 */

using System;
using System.Globalization;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{

#nullable enable
    internal interface IRegistryWrapper
    {
        void SetValue(string? name, object value);

        void SetValue(string? name, object value, RegistryValueKind valueKind);

        string[] GetValueNames();

        void DeleteValue(string name);

        string[] GetSubKeyNames();

        IRegistryWrapper? CreateSubKey(string subkey);

        IRegistryWrapper? OpenSubKey(string name, bool writable);

        void DeleteSubKeyTree(string subkey);

        object? GetValue(string? name);

        object? GetValue(string? name, object? defaultValue, RegistryValueOptions options);

        RegistryValueKind GetValueKind(string? name);

        object RegistryKey { get; }

        void SetAccessControl(ObjectSecurity securityDescriptor);

        ObjectSecurity GetAccessControl(AccessControlSections includeSections);

        void Close();

        string Name { get; }

        int SubKeyCount { get; }
    }
#nullable restore

    internal static class RegistryWrapperUtils
    {
        public static object ConvertValueToUIntFromRegistryIfNeeded(string name, object value, RegistryValueKind kind)
        {
            try
            {
                // Workaround for CLR bug that doesn't support full range of DWORD or QWORD
                if (kind == RegistryValueKind.DWord)
                {
                    value = (int)value;
                    if ((int)value < 0)
                    {
                        value = BitConverter.ToUInt32(BitConverter.GetBytes((int)value), 0);
                    }
                }
                else if (kind == RegistryValueKind.QWord)
                {
                    value = (long)value;
                    if ((long)value < 0)
                    {
                        value = BitConverter.ToUInt64(BitConverter.GetBytes((long)value), 0);
                    }
                }
            }
            catch (System.IO.IOException)
            {
                // This is expected if the value does not exist.
            }

            return value;
        }

        public static object ConvertUIntToValueForRegistryIfNeeded(object value, RegistryValueKind kind)
        {
            // Workaround for CLR bug that doesn't support full range of DWORD or QWORD
            if (kind == RegistryValueKind.DWord)
            {
                UInt32 intValue = 0;

                // See if it's already a positive number
                try
                {
                    intValue = Convert.ToUInt32(value, CultureInfo.InvariantCulture);
                    value = BitConverter.ToInt32(BitConverter.GetBytes(intValue), 0);
                }
                catch (OverflowException)
                {
                    // It must be a negative Int32, and therefore need no more conversion
                }
            }
            else if (kind == RegistryValueKind.QWord)
            {
                UInt64 intValue = 0;

                // See if it's already a positive number
                try
                {
                    intValue = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                    value = BitConverter.ToInt64(BitConverter.GetBytes(intValue), 0);
                }
                catch (OverflowException)
                {
                    // It must be a negative Int64, and therefore need no more conversion
                }
            }

            return value;
        }
    }

    internal sealed class RegistryWrapper : IRegistryWrapper
    {
        private readonly RegistryKey _regKey;

        internal RegistryWrapper(RegistryKey regKey)
        {
            _regKey = regKey;
        }

        #region IRegistryWrapper Members

        public void SetValue(string name, object value)
        {
            _regKey.SetValue(name, value);
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            value = System.Management.Automation.PSObject.Base(value);
            value = RegistryWrapperUtils.ConvertUIntToValueForRegistryIfNeeded(value, valueKind);

            _regKey.SetValue(name, value, valueKind);
        }

        public string[] GetValueNames()
        {
            return _regKey.GetValueNames();
        }

        public void DeleteValue(string name)
        {
            _regKey.DeleteValue(name);
        }

        public string[] GetSubKeyNames()
        {
            return _regKey.GetSubKeyNames();
        }

        public IRegistryWrapper CreateSubKey(string subkey)
        {
            RegistryKey newKey = _regKey.CreateSubKey(subkey);
            if (newKey == null)
                return null;
            else
                return new RegistryWrapper(newKey);
        }

        public IRegistryWrapper OpenSubKey(string name, bool writable)
        {
            RegistryKey newKey = _regKey.OpenSubKey(name, writable);
            if (newKey == null)
                return null;
            else
                return new RegistryWrapper(newKey);
        }

        public void DeleteSubKeyTree(string subkey)
        {
            _regKey.DeleteSubKeyTree(subkey);
        }

        public object GetValue(string name)
        {
            object value = _regKey.GetValue(name);

            try
            {
                value = RegistryWrapperUtils.ConvertValueToUIntFromRegistryIfNeeded(name, value, GetValueKind(name));
            }
            catch (System.IO.IOException)
            {
                // This is expected if the value does not exist.
            }

            return value;
        }

        public object GetValue(string name, object defaultValue, RegistryValueOptions options)
        {
            object value = _regKey.GetValue(name, defaultValue, options);

            try
            {
                value = RegistryWrapperUtils.ConvertValueToUIntFromRegistryIfNeeded(name, value, GetValueKind(name));
            }
            catch (System.IO.IOException)
            {
                // This is expected if the value does not exist.
            }

            return value;
        }

        public RegistryValueKind GetValueKind(string name)
        {
            return _regKey.GetValueKind(name);
        }

        public void Close()
        {
            _regKey.Dispose();
        }

        public string Name
        {
            get { return _regKey.Name; }
        }

        public int SubKeyCount
        {
            get { return _regKey.SubKeyCount; }
        }

        public object RegistryKey
        {
            get { return _regKey; }
        }

        public void SetAccessControl(ObjectSecurity securityDescriptor)
        {
            _regKey.SetAccessControl((RegistrySecurity)securityDescriptor);
        }

        public ObjectSecurity GetAccessControl(AccessControlSections includeSections)
        {
            return _regKey.GetAccessControl(includeSections);
        }

        #endregion
    }

    internal sealed class TransactedRegistryWrapper : IRegistryWrapper
    {
        private readonly TransactedRegistryKey _txRegKey;
        private readonly CmdletProvider _provider;

        internal TransactedRegistryWrapper(TransactedRegistryKey txRegKey, CmdletProvider provider)
        {
            _txRegKey = txRegKey;
            _provider = provider;
        }

        #region IRegistryWrapper Members

        public void SetValue(string name, object value)
        {
            using (_provider.CurrentPSTransaction)
            {
                _txRegKey.SetValue(name, value);
            }
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            using (_provider.CurrentPSTransaction)
            {
                value = System.Management.Automation.PSObject.Base(value);
                value = RegistryWrapperUtils.ConvertUIntToValueForRegistryIfNeeded(value, valueKind);

                _txRegKey.SetValue(name, value, valueKind);
            }
        }

        public string[] GetValueNames()
        {
            using (_provider.CurrentPSTransaction)
            {
                return _txRegKey.GetValueNames();
            }
        }

        public void DeleteValue(string name)
        {
            using (_provider.CurrentPSTransaction)
            {
                _txRegKey.DeleteValue(name);
            }
        }

        public string[] GetSubKeyNames()
        {
            using (_provider.CurrentPSTransaction)
            {
                return _txRegKey.GetSubKeyNames();
            }
        }

        public IRegistryWrapper CreateSubKey(string subkey)
        {
            using (_provider.CurrentPSTransaction)
            {
                TransactedRegistryKey newKey = _txRegKey.CreateSubKey(subkey);
                if (newKey == null)
                    return null;
                else
                    return new TransactedRegistryWrapper(newKey, _provider);
            }
        }

        public IRegistryWrapper OpenSubKey(string name, bool writable)
        {
            using (_provider.CurrentPSTransaction)
            {
                TransactedRegistryKey newKey = _txRegKey.OpenSubKey(name, writable);
                if (newKey == null)
                    return null;
                else
                    return new TransactedRegistryWrapper(newKey, _provider);
            }
        }

        public void DeleteSubKeyTree(string subkey)
        {
            using (_provider.CurrentPSTransaction)
            {
                _txRegKey.DeleteSubKeyTree(subkey);
            }
        }

        public object GetValue(string name)
        {
            using (_provider.CurrentPSTransaction)
            {
                object value = _txRegKey.GetValue(name);

                try
                {
                    value = RegistryWrapperUtils.ConvertValueToUIntFromRegistryIfNeeded(name, value, GetValueKind(name));
                }
                catch (System.IO.IOException)
                {
                    // This is expected if the value does not exist.
                }

                return value;
            }
        }

        public object GetValue(string name, object defaultValue, RegistryValueOptions options)
        {
            using (_provider.CurrentPSTransaction)
            {
                object value = _txRegKey.GetValue(name, defaultValue, options);

                try
                {
                    value = RegistryWrapperUtils.ConvertValueToUIntFromRegistryIfNeeded(name, value, GetValueKind(name));
                }
                catch (System.IO.IOException)
                {
                    // This is expected if the value does not exist.
                }

                return value;
            }
        }

        public RegistryValueKind GetValueKind(string name)
        {
            using (_provider.CurrentPSTransaction)
            {
                return _txRegKey.GetValueKind(name);
            }
        }

        public void Close()
        {
            using (_provider.CurrentPSTransaction)
            {
                _txRegKey.Close();
            }
        }

        public string Name
        {
            get
            {
                using (_provider.CurrentPSTransaction)
                {
                    return _txRegKey.Name;
                }
            }
        }

        public int SubKeyCount
        {
            get
            {
                using (_provider.CurrentPSTransaction)
                {
                    return _txRegKey.SubKeyCount;
                }
            }
        }

        public object RegistryKey
        {
            get { return _txRegKey; }
        }

        public void SetAccessControl(ObjectSecurity securityDescriptor)
        {
            using (_provider.CurrentPSTransaction)
            {
                _txRegKey.SetAccessControl((TransactedRegistrySecurity)securityDescriptor);
            }
        }

        public ObjectSecurity GetAccessControl(AccessControlSections includeSections)
        {
            using (_provider.CurrentPSTransaction)
            {
                return _txRegKey.GetAccessControl(includeSections);
            }
        }

        #endregion
    }
}
