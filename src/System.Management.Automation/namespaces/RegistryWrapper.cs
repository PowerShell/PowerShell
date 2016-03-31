/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
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
    internal interface IRegistryWrapper
    {
        void SetValue(string name, object value);
        void SetValue(string name, object value, RegistryValueKind valueKind);
        string[] GetValueNames();
        void DeleteValue(string name);
        string[] GetSubKeyNames();
        IRegistryWrapper CreateSubKey(string subkey);
        IRegistryWrapper OpenSubKey(string name, bool writable);
        void DeleteSubKeyTree(string subkey);
        object GetValue(string name);
        object GetValue(string name, object defaultValue, RegistryValueOptions options);
        RegistryValueKind GetValueKind(string name);
        object RegistryKey { get; }
        void SetAccessControl(ObjectSecurity securityDescriptor);
        ObjectSecurity GetAccessControl(AccessControlSections includeSections);
        void Close();
        string Name { get;}
        int SubKeyCount { get;}
    }

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

    internal class RegistryWrapper : IRegistryWrapper
    {
        RegistryKey regKey;

        internal RegistryWrapper(RegistryKey regKey)
        {
            this.regKey = regKey;
        }

        #region IRegistryWrapper Members

        public void SetValue(string name, object value)
        {
            regKey.SetValue(name, value);
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            value = System.Management.Automation.PSObject.Base(value);
            value = RegistryWrapperUtils.ConvertUIntToValueForRegistryIfNeeded(value, valueKind);

            regKey.SetValue(name, value, valueKind);
        }

        public string[] GetValueNames()
        {
            return regKey.GetValueNames();
        }

        public void DeleteValue(string name)
        {
            regKey.DeleteValue(name);
        }

        public string[] GetSubKeyNames()
        {
            return regKey.GetSubKeyNames();
        }

        public IRegistryWrapper CreateSubKey(string subkey)
        {
            RegistryKey newKey = regKey.CreateSubKey(subkey);
            if (newKey == null)
                return null;
            else
                return new RegistryWrapper(newKey);
        }

        public IRegistryWrapper OpenSubKey(string name, bool writable)
        {
            RegistryKey newKey = regKey.OpenSubKey(name, writable);
            if (newKey == null)
                return null;
            else
                return new RegistryWrapper(newKey);
        }

        public void DeleteSubKeyTree(string subkey)
        {
            regKey.DeleteSubKeyTree(subkey);
        }

        public object GetValue(string name)
        {
            object value = regKey.GetValue(name);

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
            object value = regKey.GetValue(name, defaultValue, options);

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
            return regKey.GetValueKind(name);
        }

        public void Close()
        {
            regKey.Dispose();
        }

        public string Name
        {
            get { return regKey.Name; }
        }

        public int SubKeyCount
        {
            get { return regKey.SubKeyCount; }
        }

        public object RegistryKey
        {
            get { return regKey; }
        }

        public void SetAccessControl(ObjectSecurity securityDescriptor)
        {
            regKey.SetAccessControl((RegistrySecurity)securityDescriptor);
        }

        public ObjectSecurity GetAccessControl(AccessControlSections includeSections)
        {
            return regKey.GetAccessControl(includeSections);
        }

        #endregion
    }

    internal class TransactedRegistryWrapper : IRegistryWrapper
    {
        TransactedRegistryKey txRegKey;
        CmdletProvider provider;

        internal TransactedRegistryWrapper(TransactedRegistryKey txRegKey, CmdletProvider provider)
        {
            this.txRegKey = txRegKey;
            this.provider = provider;
        }

        #region IRegistryWrapper Members

        public void SetValue(string name, object value)
        {
            using (provider.CurrentPSTransaction)
            {
                txRegKey.SetValue(name, value);
            }
        }

        public void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            using (provider.CurrentPSTransaction)
            {
                value = System.Management.Automation.PSObject.Base(value);
                value = RegistryWrapperUtils.ConvertUIntToValueForRegistryIfNeeded(value, valueKind);

                txRegKey.SetValue(name, value, valueKind);
            }
        }

        public string[] GetValueNames()
        {
            using (provider.CurrentPSTransaction)
            {
                return txRegKey.GetValueNames();
            }
        }

        public void DeleteValue(string name)
        {
            using (provider.CurrentPSTransaction)
            {
                txRegKey.DeleteValue(name);
            }
        }

        public string[] GetSubKeyNames()
        {
            using (provider.CurrentPSTransaction)
            {
                return txRegKey.GetSubKeyNames();
            }
        }

        public IRegistryWrapper CreateSubKey(string subkey)
        {
            using (provider.CurrentPSTransaction)
            {
                TransactedRegistryKey newKey = txRegKey.CreateSubKey(subkey);
                if (newKey == null)
                    return null;
                else
                    return new TransactedRegistryWrapper(newKey, provider);
            }
        }

        public IRegistryWrapper OpenSubKey(string name, bool writable)
        {
            using (provider.CurrentPSTransaction)
            {
                TransactedRegistryKey newKey = txRegKey.OpenSubKey(name, writable);
                if (newKey == null)
                    return null;
                else
                    return new TransactedRegistryWrapper(newKey, provider);
            }
        }

        public void DeleteSubKeyTree(string subkey)
        {
            using (provider.CurrentPSTransaction)
            {
                txRegKey.DeleteSubKeyTree(subkey);
            }
        }

        public object GetValue(string name)
        {
            using (provider.CurrentPSTransaction)
            {
                object value = txRegKey.GetValue(name);

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
            using (provider.CurrentPSTransaction)
            {
                object value = txRegKey.GetValue(name, defaultValue, options);

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
            using (provider.CurrentPSTransaction)
            {
                return txRegKey.GetValueKind(name);
            }
        }

        public void Close()
        {
            using (provider.CurrentPSTransaction)
            {
                txRegKey.Close();
            }
        }

        public string Name
        {
            get 
            {
                using (provider.CurrentPSTransaction)
                {
                    return txRegKey.Name;
                }
            }
        }

        public int SubKeyCount
        {
            get 
            {
                using (provider.CurrentPSTransaction)
                {
                    return txRegKey.SubKeyCount;
                }
            }
        }

        public object RegistryKey
        {
            get { return txRegKey; }
        }

        public void SetAccessControl(ObjectSecurity securityDescriptor)
        {
            using (provider.CurrentPSTransaction)
            {
                txRegKey.SetAccessControl((TransactedRegistrySecurity)securityDescriptor);
            }
        }

        public ObjectSecurity GetAccessControl(AccessControlSections includeSections)
        {
            using (provider.CurrentPSTransaction)
            {
                return txRegKey.GetAccessControl(includeSections);
            }
        }

        #endregion
    }
}