// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\Microsoft\Win32\RegistryKey.cs.
// Namespace: Microsoft.Win32
//
/*
  Note on transaction support:
  Eventually we will want to add support for NT's transactions to our
  TransactedRegistryKey API's (possibly Whidbey M3?).  When we do this, here's
  the list of API's we need to make transaction-aware:

  RegCreateKeyEx
  RegDeleteKey
  RegDeleteValue
  RegEnumKeyEx
  RegEnumValue
  RegOpenKeyEx
  RegQueryInfoKey
  RegQueryValueEx
  RegSetValueEx

  We can ignore RegConnectRegistry (remote registry access doesn't yet have
  transaction support) and RegFlushKey.  RegCloseKey doesn't require any
  additional work.  .
 */

/*
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to
  them.  So if you set an ACL to deny read access to yourself, you'll still be
  able to read with that handle, but not with new handles.

  Another peculiarity is a Terminal Server app compatibility hack.  The OS
  will second guess your attempt to open a handle sometimes.  If a certain
  combination of Terminal Server app compat registry keys are set, then the
  OS will try to reopen your handle with lesser permissions if you couldn't
  open it in the specified mode.  So on some machines, we will see handles that
  may not be able to read or write to a registry key.  It's very strange.  But
  the real test of these handles is attempting to read or set a value in an
  affected registry key.

  For reference, at least two registry keys must be set to particular values
  for this behavior:
  HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\RegistryExtensionFlags, the least significant bit must be 1.
  HKLM\SYSTEM\CurrentControlSet\Control\TerminalServer\TSAppCompat must be 1
  There might possibly be an interaction with yet a third registry key as well.

*/

using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.Commands.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Security.AccessControl;
    using System.Security.Permissions;
    using System.Text;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Transactions;
    using System.Diagnostics.CodeAnalysis;

    // Putting this in a separate internal class to avoid OACR warning DoNotDeclareReadOnlyMutableReferenceTypes.
    internal sealed class BaseRegistryKeys
    {
        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));
    }

    /// <summary>
    /// Registry encapsulation. To get an instance of a TransactedRegistryKey use the
    /// Registry class's static members then call OpenSubKey.
    ///
    /// @see Registry
    /// @security(checkDllCalls=off)
    /// @security(checkClassLinking=on)
    /// </summary>
    [ComVisible(true)]
    // Suppressed because these objects are written to the pipeline so need to be accessible.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public sealed class TransactedRegistryKey : MarshalByRefObject, IDisposable
    {
        private const string resBaseName = "RegistryProviderStrings";

        // Dirty indicates that we have munged data that should be potentially
        // written to disk.
        //
        private const int STATE_DIRTY = 0x0001;

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened"
        // or "closed".
        //
        private const int STATE_SYSTEMKEY = 0x0002;

        // Access
        //
        private const int STATE_WRITEACCESS = 0x0004;

        // Names of keys.  This array must be in the same order as the HKEY values listed above.
        //
        private static readonly string[] s_hkeyNames = new string[] {
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS",
                "HKEY_PERFORMANCE_DATA",
                "HKEY_CURRENT_CONFIG",
                "HKEY_DYN_DATA"
                };

        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  Win9x: 255 NT: 16,383 Unicode characters, or 260 ANSI chars
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueNameLength = 16383;
        private const int MaxValueDataLength = 1024 * 1024;

        private SafeRegistryHandle _hkey = null;
        private int _state = 0;
        private string _keyName;
        private RegistryKeyPermissionCheck _checkMode;
        private System.Transactions.Transaction _myTransaction;
        private SafeTransactionHandle _myTransactionHandle;

        // This is a wrapper around RegOpenKeyTransacted that implements a workaround
        // to TxF bug number 181242 After calling RegOpenKeyTransacted, it calls RegQueryInfoKey.
        // If that call fails with ERROR_INVALID_TRANSACTION, we have possibly run into bug 181242. To workaround
        // this, we open the key without a transaction and then open it again with
        // a transaction and return THAT hkey.

        // Suppressed because there is no way for arbitrary data to be passed.
        [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage")]
        private int RegOpenKeyTransactedWrapper(SafeRegistryHandle hKey, string lpSubKey,
                    int ulOptions, int samDesired, out SafeRegistryHandle hkResult,
                    SafeTransactionHandle hTransaction, IntPtr pExtendedParameter)
        {
            int error = Win32Native.ERROR_SUCCESS;
            SafeRegistryHandle hKeyToReturn = null;

            error = Win32Native.RegOpenKeyTransacted(_hkey, lpSubKey, ulOptions, samDesired, out hKeyToReturn, hTransaction, pExtendedParameter);

            if (Win32Native.ERROR_SUCCESS == error && !hKeyToReturn.IsInvalid)
            {
                // This is a check and workaround for TxR bug 181242. If we try to use the transacted hKey we just opened
                // for a call to RegQueryInfoKey and get back a ERROR_INVALID_TRANSACTION error, then the key might be a symbolic link and TxR didn't
                // do the open correctly. The workaround is to open it non-transacted, then open it again transacted without
                // a subkey string. If we get some error other than ERROR_INVALID_TRANSACTION from RegQueryInfoKey, just ignore it for now.
                int subkeyCount = 0;
                int valueCount = 0;
                error = Win32Native.RegQueryInfoKey(hKeyToReturn,
                                          null,
                                          null,
                                          Win32Native.NULL,
                                          ref subkeyCount,  // subkeys
                                          null,
                                          null,
                                          ref valueCount,     // values
                                          null,
                                          null,
                                          null,
                                          null);
                if (Win32Native.ERROR_INVALID_TRANSACTION == error)
                {
                    SafeRegistryHandle nonTxKey = null;
                    SafeRegistryHandle txKey = null;
                    error = Win32Native.RegOpenKeyEx(_hkey, lpSubKey, ulOptions, samDesired, out nonTxKey);
                    // If we got some error on this open, just ignore it and continue on with the handle
                    // we got on the original RegOpenKeyTransacted.
                    if (Win32Native.ERROR_SUCCESS == error)
                    {
                        // Now do an RegOpenKeyTransacted with the non-transacted key and no "subKey" parameter.
                        error = Win32Native.RegOpenKeyTransacted(nonTxKey, null, ulOptions, samDesired, out txKey, hTransaction, pExtendedParameter);
                        if (Win32Native.ERROR_SUCCESS == error)
                        {
                            // Let's use this hkey instead.
                            hKeyToReturn.Dispose();
                            hKeyToReturn = txKey;
                        }

                        nonTxKey.Dispose();
                        nonTxKey = null;
                    }
                }
            }

            hkResult = hKeyToReturn;
            return error;
        }

        /**
         * Creates a TransactedRegistryKey.
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations
         * will be allowed. If systemkey is set then the hkey won't be released
         * when the object is GC'ed.
         */
        private TransactedRegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey,
                                      System.Transactions.Transaction transaction, SafeTransactionHandle txHandle)
        {
            _hkey = hkey;
            _keyName = string.Empty;
            if (systemkey)
            {
                _state |= STATE_SYSTEMKEY;
            }

            if (writable)
            {
                _state |= STATE_WRITEACCESS;
            }
            // We want to take our own clone so we can dispose it when we want and
            // aren't susceptible to the caller disposing it.
            if (transaction != null)
            {
                _myTransaction = transaction.Clone();
                _myTransactionHandle = txHandle;
            }
            else
            {
                _myTransaction = null;
                _myTransactionHandle = null;
            }
        }

        private SafeTransactionHandle GetTransactionHandle()
        {
            SafeTransactionHandle safeTransactionHandle = null;

            // If myTransaction is not null and is not the same as Transaction.Current
            // this is an invalid operation. The transaction within which the RegistryKey object was created
            // needs to be the same as the transaction being used now.
            if (_myTransaction != null)
            {
                if (!_myTransaction.Equals(Transaction.Current))
                {
                    throw new InvalidOperationException(RegistryProviderStrings.InvalidOperation_MustUseSameTransaction);
                }
                else
                {
                    safeTransactionHandle = _myTransactionHandle;
                }
            }
            else  // we want to use Transaction.Current for the transaction.
            {
                safeTransactionHandle = SafeTransactionHandle.Create();
            }

            return safeTransactionHandle;
        }

        /// <summary>TransactedRegistryKey.Close
        /// <para>Closes this key, flushes it to disk if the contents have been modified.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        public void Close()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        _hkey.Dispose();
                    }
                    catch (IOException)
                    {
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        _hkey = null;
                    }
                }
            }

            if (_myTransaction != null)
            {
                // Dispose the transaction because we cloned it.
                try
                {
                    _myTransaction.Dispose();
                }
                catch (TransactionException)
                {
                    // ignore.
                }
                finally
                {
                    _myTransaction = null;
                }
            }
        }

        /// <summary>TransactedRegistryKey.Flush
        /// <para>Flushes this key. Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        public void Flush()
        {
            // Require a transaction. This will throw for "Base" keys because they aren't associated with a transaction.
            VerifyTransaction();
            if (_hkey != null)
            {
                if (IsDirty())
                {
                    int ret = Win32Native.RegFlushKey(_hkey);
                    if (Win32Native.ERROR_SUCCESS != ret)
                    {
                        throw new IOException(Win32Native.GetMessage(ret), ret);
                    }
                }
            }
        }

        /// <summary>TransactedRegistryKey.Dispose
        /// <para>Disposes this key. Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// <para>Creates a new subkey, or opens an existing one.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name='subkey'>Name or path to subkey to create or open. Cannot be null or an empty string,
        /// otherwise an ArgumentException is thrown.</param>
        /// <returns>A TransactedRegistryKey object for the subkey, which is associated with Transaction.Current.
        /// returns null if the operation failed.</returns>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey CreateSubKey(string subkey)
        {
            return CreateSubKey(subkey, _checkMode);
        }

        /// <summary>
        /// <para>Creates a new subkey, or opens an existing one.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A TransactedRegistryKey object for the subkey, which is associated with Transaction.Current.
        /// returns null if the operation failed.</returns>
        /// <param name='subkey'>Name or path to subkey to create or open. Cannot be null or an empty string,
        /// otherwise an ArgumentException is thrown.</param>
        /// <param name='permissionCheck'>One of the Microsoft.Win32.RegistryKeyPermissionCheck values that
        /// specifies whether the key is opened for read or read/write access.</param>
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck)
        {
            return CreateSubKeyInternal(subkey, permissionCheck, (TransactedRegistrySecurity)null);
        }

        /// <summary>
        /// <para>Creates a new subkey, or opens an existing one.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A TransactedRegistryKey object for the subkey, which is associated with Transaction.Current.
        /// returns null if the operation failed.</returns>
        /// <param name='subkey'>Name or path to subkey to create or open. Cannot be null or an empty string,
        /// otherwise an ArgumentException is thrown.</param>
        /// <param name='permissionCheck'>One of the Microsoft.Win32.RegistryKeyPermissionCheck values that
        /// specifies whether the key is opened for read or read/write access.</param>
        /// <param name='registrySecurity'>A TransactedRegistrySecurity object that specifies the access control security for the new key.</param>
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public unsafe TransactedRegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, TransactedRegistrySecurity registrySecurity)
        {
            return CreateSubKeyInternal(subkey, permissionCheck, registrySecurity);
        }

        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private unsafe TransactedRegistryKey CreateSubKeyInternal(string subkey, RegistryKeyPermissionCheck permissionCheck, object registrySecurityObj)
        {
            ValidateKeyName(subkey);
            // RegCreateKeyTransacted requires a non-empty key name, so let's deal with that here.
            if (string.Empty == subkey)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegKeyStrEmpty);
            }

            ValidateKeyMode(permissionCheck);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // only keys opened under read mode is not writable
            TransactedRegistryKey existingKey = InternalOpenSubKey(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree));
            if (existingKey != null)
            { // Key already exits
                CheckSubKeyWritePermission(subkey);
                CheckSubTreePermission(subkey, permissionCheck);
                existingKey._checkMode = permissionCheck;
                return existingKey;
            }

            CheckSubKeyCreatePermission(subkey);

            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            TransactedRegistrySecurity registrySecurity = registrySecurityObj as TransactedRegistrySecurity;
            // For ACL's, get the security descriptor from the RegistrySecurity.
            if (registrySecurity != null)
            {
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                byte[] sd = registrySecurity.GetSecurityDescriptorBinaryForm();
                // We allocate memory on the stack to improve the speed.
                // So this part of code can't be refactored into a method.
                byte* pSecDescriptor = stackalloc byte[sd.Length];
                Microsoft.PowerShell.Commands.Internal.Buffer.memcpy(sd, 0, pSecDescriptor, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor;
            }

            int disposition = 0;

            // By default, the new key will be writable.
            SafeRegistryHandle result = null;
            int ret = 0;
            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();

            ret = Win32Native.RegCreateKeyTransacted(_hkey,
                subkey,
                0,
                null,
                0,
                GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree),
                secAttrs,
                out result,
                out disposition,
                safeTransactionHandle,
                IntPtr.Zero
                );

            if (ret == 0 && !result.IsInvalid)
            {
                TransactedRegistryKey key = new TransactedRegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false,
                                                                      Transaction.Current, safeTransactionHandle);
                CheckSubTreePermission(subkey, permissionCheck);
                key._checkMode = permissionCheck;

                if (subkey.Length == 0)
                    key._keyName = _keyName;
                else
                    key._keyName = _keyName + "\\" + subkey;
                return key;
            }
            else if (ret != 0) // syscall failed, ret is an error code.
                Win32Error(ret, _keyName + "\\" + subkey);  // Access denied?

            BCLDebug.Assert(false, "Unexpected code path in RegistryKey::CreateSubKey");
            return null;
        }

        /// <summary>
        /// <para>Deletes the specified subkey. Will throw an exception if the subkey has
        /// subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// <exception cref="InvalidOperationException">Thrown if the subkey as child subkeys.</exception>
        /// </summary>
        /// <param name='subkey'>The subkey to delete.</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public void DeleteSubKey(string subkey)
        {
            DeleteSubKey(subkey, true);
        }

        /// <summary>
        /// <para>Deletes the specified subkey. Will throw an exception if the subkey has
        /// subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// <exception cref="InvalidOperationException">Thrown if the subkey as child subkeys.</exception>
        /// <exception cref="ArgumentException">Thrown if true is specified for throwOnMissingSubKey and the
        /// specified subkey does not exist.</exception>
        /// </summary>
        /// <param name='subkey'>The subkey to delete.</param>
        /// <param name='throwOnMissingSubKey'>Specify true if an ArgumentException should be thrown if
        /// the specified subkey does not exist. If false is specified, a missing subkey does not throw
        /// an exception.</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public void DeleteSubKey(string subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash
            CheckSubKeyWritePermission(subkey);

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open.
            //
            TransactedRegistryKey key = InternalOpenSubKey(subkey, false);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        throw new InvalidOperationException(RegistryProviderStrings.InvalidOperation_RegRemoveSubKey);
                    }
                }
                finally
                {
                    key.Close();
                }

                int ret = 0;

                SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();
                ret = Win32Native.RegDeleteKeyTransacted(_hkey, subkey, 0, 0, safeTransactionHandle, IntPtr.Zero);

                if (ret != 0)
                {
                    if (ret == Win32Native.ERROR_FILE_NOT_FOUND)
                    {
                        if (throwOnMissingSubKey)
                        {
                            throw new ArgumentException(RegistryProviderStrings.ArgumentException_RegSubKeyAbsent);
                        }
                    }
                    else
                        Win32Error(ret, null);
                }
            }
            else
            { // there is no key which also means there is no subkey
                if (throwOnMissingSubKey)
                    throw new ArgumentException(RegistryProviderStrings.ArgumentException_RegSubKeyAbsent);
            }
        }

        /// <summary>
        /// <para>Recursively deletes a subkey and any child subkeys.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="subkey">The subkey to delete.</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public void DeleteSubKeyTree(string subkey)
        {
            ValidateKeyName(subkey);

            // Security concern: Deleting a hive's "" subkey would delete all
            // of that hive's contents.  Don't allow "".
            if ((string.IsNullOrEmpty(subkey) || subkey.Length == 0) && IsSystemKey())
            {
                throw new ArgumentException(RegistryProviderStrings.ArgRegKeyDelHive);
            }

            EnsureWriteable();

            int ret = 0;

            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash
            CheckSubTreeWritePermission(subkey);

            TransactedRegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        string[] keys = key.InternalGetSubKeyNames();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally
                {
                    key.Close();
                }

                ret = Win32Native.RegDeleteKeyTransacted(_hkey, subkey, 0, 0, safeTransactionHandle, IntPtr.Zero);
                if (ret != 0) Win32Error(ret, null);
            }
            else
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSubKeyAbsent);
            }
        }

        // An internal version which does no security checks or argument checking.  Skipping the
        // security checks should give us a slight perf gain on large trees.
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private void DeleteSubKeyTreeInternal(string subkey)
        {
            int ret = 0;

            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();
            TransactedRegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        string[] keys = key.InternalGetSubKeyNames();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally
                {
                    key.Close();
                }

                ret = Win32Native.RegDeleteKeyTransacted(_hkey, subkey, 0, 0, safeTransactionHandle, IntPtr.Zero);
                if (ret != 0) Win32Error(ret, null);
            }
            else
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSubKeyAbsent);
            }
        }

        /// <summary>
        /// <para>Deletes the specified value from this key.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="name">Name of the value to delete.</param>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void DeleteValue(string name)
        {
            DeleteValue(name, true);
        }

        /// <summary>
        /// <para>Deletes the specified value from this key.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="name">Name of the value to delete.</param>
        /// <param name="throwOnMissingValue">Specify true if an ArgumentException should be thrown if
        /// the specified value does not exist. If false is specified, a missing value does not throw
        /// an exception.</param>
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            EnsureWriteable();
            CheckValueWritePermission(name);
            // Require a transaction. This will throw for "Base" keys because they aren't associated with a transaction.
            VerifyTransaction();
            int errorCode = Win32Native.RegDeleteValue(_hkey, name);

            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
            {
                if (throwOnMissingValue)
                {
                    throw new ArgumentException(RegistryProviderStrings.Arg_RegSubKeyValueAbsent);
                }
                else
                {
                    errorCode = Win32Native.ERROR_SUCCESS;
                }
            }

            if (Win32Native.ERROR_SUCCESS != errorCode)
            {
                Win32Error(errorCode, null);
            }
        }

        /**
         * Retrieves a new TransactedRegistryKey that represents the requested key. Valid
         * values are:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER,
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA.
         *
         * @param hKey HKEY_* to open.
         *
         * @return the TransactedRegistryKey requested.
         */
        internal static TransactedRegistryKey GetBaseKey(IntPtr hKey)
        {
            int index = ((int)hKey) & 0x0FFFFFFF;
            BCLDebug.Assert(index >= 0 && index < s_hkeyNames.Length, "index is out of range!");
            BCLDebug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, false);

            // For Base keys, there is no transaction associated with the HKEY.
            TransactedRegistryKey key = new TransactedRegistryKey(srh, true, true, null, null);
            key._checkMode = RegistryKeyPermissionCheck.Default;
            key._keyName = s_hkeyNames[index];
            return key;
        }

        /// <summary>
        /// <para>Retrieves a subkey. If readonly is true, then the subkey is opened with
        /// read-only access.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>The subkey requested or null if the operation failed.</returns>
        /// <param name="name">Name or path of the subkey to open.</param>
        /// <param name="writable">Set to true of you only need readonly access.</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            CheckOpenSubKeyPermission(name, writable);
            SafeRegistryHandle result = null;
            int ret = 0;
            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();

            ret = RegOpenKeyTransactedWrapper(_hkey, name, 0, GetRegistryKeyAccess(writable), out result, safeTransactionHandle, IntPtr.Zero);

            if (ret == 0 && !result.IsInvalid)
            {
                TransactedRegistryKey key = new TransactedRegistryKey(result, writable, false, Transaction.Current, safeTransactionHandle);
                key._checkMode = GetSubKeyPermissionCheck(writable);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(RegistryProviderStrings.Security_RegistryPermission);
            }

            return null;
        }

        /// <summary>
        /// <para>Retrieves a subkey.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>The subkey requested or null if the operation failed.</returns>
        /// <param name="name">Name or path of the subkey to open.</param>
        /// <param name="permissionCheck">One of the Microsoft.Win32.RegistryKeyPermissionCheck values that specifies
        /// whether the key is opened for read or read/write access.</param>
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            ValidateKeyMode(permissionCheck);
            return InternalOpenSubKey(name, permissionCheck, GetRegistryKeyAccess(permissionCheck));
        }

        /// <summary>
        /// <para>Retrieves a subkey.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>The subkey requested or null if the operation failed.</returns>
        /// <param name="name">Name or path of the subkey to open.</param>
        /// <param name="permissionCheck">One of the Microsoft.Win32.RegistryKeyPermissionCheck values that specifies
        /// whether the key is opened for read or read/write access.</param>
        /// <param name="rights">A bitwise combination of Microsoft.Win32.RegistryRights values that specifies the desired security access.</param>
        [ComVisible(false)]
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights)
        {
            return InternalOpenSubKey(name, permissionCheck, (int)rights);
        }

        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private TransactedRegistryKey InternalOpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, int rights)
        {
            ValidateKeyName(name);
            ValidateKeyMode(permissionCheck);
            ValidateKeyRights(rights);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            CheckOpenSubKeyPermission(name, permissionCheck);
            SafeRegistryHandle result = null;
            int ret = 0;

            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();

            ret = RegOpenKeyTransactedWrapper(_hkey, name, 0, rights, out result, safeTransactionHandle, IntPtr.Zero);

            if (ret == 0 && !result.IsInvalid)
            {
                TransactedRegistryKey key = new TransactedRegistryKey(result, (permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree), false,
                                                                      Transaction.Current, safeTransactionHandle);
                key._keyName = _keyName + "\\" + name;
                key._checkMode = permissionCheck;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reason,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(RegistryProviderStrings.Security_RegistryPermission);
            }

            return null;
        }

        // This required no security checks. This is to get around the Deleting SubKeys which only require
        // write permission. They call OpenSubKey which required read. Now instead call this function w/o security checks
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        internal TransactedRegistryKey InternalOpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();

            int winAccess = GetRegistryKeyAccess(writable);
            SafeRegistryHandle result = null;
            int ret = 0;
            SafeTransactionHandle safeTransactionHandle = GetTransactionHandle();

            ret = RegOpenKeyTransactedWrapper(_hkey, name, 0, winAccess, out result, safeTransactionHandle, IntPtr.Zero);

            if (ret == 0 && !result.IsInvalid)
            {
                TransactedRegistryKey key = new TransactedRegistryKey(result, writable, false, Transaction.Current, safeTransactionHandle);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            return null;
        }

        /// <summary>
        /// <para>Retrieves a subkey for readonly access.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>The subkey requested or null if the operation failed.</returns>
        /// <param name="name">Name or path of the subkey to open.</param>
        [ResourceExposure(ResourceScope.Machine)]
        [ResourceConsumption(ResourceScope.Machine)]
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public TransactedRegistryKey OpenSubKey(string name)
        {
            return OpenSubKey(name, false);
        }

        /// <summary>
        /// <para>Retrieves the count of subkeys.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>The count of subkeys.</returns>
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public int SubKeyCount
        {
            get
            {
                CheckKeyReadPermission();
                return InternalSubKeyCount();
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        internal int InternalSubKeyCount()
        {
            EnsureNotDisposed();
            // Don't require a transaction. We don't want to throw for "Base" keys.

            int subkeys = 0;
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(_hkey,
                                      null,
                                      null,
                                      Win32Native.NULL,
                                      ref subkeys,  // subkeys
                                      null,
                                      null,
                                      ref junk,     // values
                                      null,
                                      null,
                                      null,
                                      null);

            if (ret != 0)
                Win32Error(ret, null);
            return subkeys;
        }

        /// <summary>
        /// <para>Retrieves an array of strings containing all the subkey names.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A string array containing all the subkey names.</returns>
        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        public string[] GetSubKeyNames()
        {
            CheckKeyReadPermission();
            return InternalGetSubKeyNames();
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        internal string[] InternalGetSubKeyNames()
        {
            EnsureNotDisposed();
            // Don't require a transaction. We don't want to throw for "Base" keys.
            int subkeys = InternalSubKeyCount();
            string[] names = new string[subkeys];  // Returns 0-length array if empty.

            if (subkeys > 0)
            {
                StringBuilder name = new StringBuilder(256);
                int namelen;

                for (int i = 0; i < subkeys; i++)
                {
                    namelen = name.Capacity; // Don't remove this. The API's doesn't work if this is not properly initialised.
                    int ret = Win32Native.RegEnumKeyEx(_hkey,
                        i,
                        name,
                        out namelen,
                        null,
                        null,
                        null,
                        null);
                    if (ret != 0)
                        Win32Error(ret, null);
                    names[i] = name.ToString();
                }
            }

            return names;
        }

        /// <summary>
        /// <para>Retrieves the count of values.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A count of values.</returns>
        public int ValueCount
        {
            get
            {
                CheckKeyReadPermission();
                return InternalValueCount();
            }
        }

        internal int InternalValueCount()
        {
            EnsureNotDisposed();
            // Don't require a transaction. We don't want to throw for "Base" keys.
            int values = 0;
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(_hkey,
                                      null,
                                      null,
                                      Win32Native.NULL,
                                      ref junk,     // subkeys
                                      null,
                                      null,
                                      ref values,   // values
                                      null,
                                      null,
                                      null,
                                      null);
            if (ret != 0)
                Win32Error(ret, null);
            return values;
        }

        /// <summary>
        /// <para>Retrieves an array of strings containing all the value names.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>All the value names.</returns>
        public string[] GetValueNames()
        {
            CheckKeyReadPermission();
            EnsureNotDisposed();
            // Don't require a transaction. We don't want to throw for "Base" keys.

            int values = InternalValueCount();
            string[] names = new string[values];

            if (values > 0)
            {
                StringBuilder name = new StringBuilder(256);
                int namelen;
                int currentlen;
                int ret;

                for (int i = 0; i < values; i++)
                {
                    currentlen = name.Capacity;
                    ret = Win32Native.ERROR_MORE_DATA;

                    // loop while we get error_more_data or until we have exceeded
                    // the max name length.
                    while (Win32Native.ERROR_MORE_DATA == ret)
                    {
                        namelen = currentlen;
                        ret = Win32Native.RegEnumValue(_hkey,
                            i,
                            name,
                            ref namelen,
                            Win32Native.NULL,
                            null,
                            null,
                            null);

                        if (ret != 0)
                        {
                            if (ret != Win32Native.ERROR_MORE_DATA)
                                Win32Error(ret, null);

                            // We got ERROR_MORE_DATA. Let's see if we can make the buffer
                            // bigger.
                            if (MaxValueNameLength == currentlen)
                                Win32Error(ret, null);

                            currentlen = currentlen * 2;
                            if (MaxValueNameLength < currentlen)
                                currentlen = MaxValueNameLength;

                            // Allocate a new buffer.
                            name = new StringBuilder(currentlen);
                        }
                    }

                    names[i] = name.ToString();
                }
            }

            return names;
        }

        /// <summary>
        /// <para>Retrieves the specified value. null is returned if the value
        /// doesn't exist. Utilizes Transaction.Current for its transaction.</para>
        /// <para>Note that name can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.</para>
        /// </summary>
        /// <returns>The data associated with the value.</returns>
        /// <param name="name">Name of value to retrieve.</param>
        public object GetValue(string name)
        {
            CheckValueReadPermission(name);
            return InternalGetValue(name, null, false, true);
        }

        /// <summary>
        /// <para>Retrieves the specified value. null is returned if the value
        /// doesn't exist. Utilizes Transaction.Current for its transaction.</para>
        /// <para>Note that name can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.</para>
        /// </summary>
        /// <returns>The data associated with the value.</returns>
        /// <param name="name">Name of value to retrieve.</param>
        /// <param name="defaultValue">Value to return if name doesn't exist.</param>
        public object GetValue(string name, object defaultValue)
        {
            CheckValueReadPermission(name);
            return InternalGetValue(name, defaultValue, false, true);
        }

        /// <summary>
        /// <para>Retrieves the specified value. null is returned if the value
        /// doesn't exist. Utilizes Transaction.Current for its transaction.</para>
        /// <para>Note that name can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.</para>
        /// </summary>
        /// <returns>The data associated with the value.</returns>
        /// <param name="name">Name of value to retrieve.</param>
        /// <param name="defaultValue">Value to return if name doesn't exist.</param>
        /// <param name="options">One of the Microsoft.Win32.RegistryValueOptions values that specifies
        /// optional processing of the retrieved value.</param>
        [ComVisible(false)]
        public object GetValue(string name, object defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                string resourceTemplate = RegistryProviderStrings.Arg_EnumIllegalVal;
                string resource = string.Format(CultureInfo.CurrentCulture, resourceTemplate, options.ToString());
                throw new ArgumentException(resource);
            }

            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            CheckValueReadPermission(name);
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        internal object InternalGetValue(string name, object defaultValue, bool doNotExpand, bool checkSecurity)
        {
            if (checkSecurity)
            {
                // Name can be null!  It's the most common use of RegQueryValueEx
                EnsureNotDisposed();
            }

            // Don't require a transaction. We don't want to throw for "Base" keys.

            object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0)
            {
                // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                // Some OS's returned ERROR_MORE_DATA even in success cases, so we
                // want to continue on through the function.
                if (ret != Win32Native.ERROR_MORE_DATA)
                    return data;
            }

            switch (type)
            {
                case Win32Native.REG_DWORD_BIG_ENDIAN:
                case Win32Native.REG_BINARY:
                    {
                        byte[] blob = new byte[datasize];
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        data = blob;
                    }

                    break;
                case Win32Native.REG_QWORD:
                    {    // also REG_QWORD_LITTLE_ENDIAN
                        if (datasize > 8)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(long)
                            goto case Win32Native.REG_BINARY;
                        }

                        long blob = 0;
                        BCLDebug.Assert(datasize == 8, "datasize==8");
                        // Here, datasize must be 8 when calling this
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }

                    break;
                case Win32Native.REG_DWORD:
                    {    // also REG_DWORD_LITTLE_ENDIAN
                        if (datasize > 4)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(int)
                            goto case Win32Native.REG_QWORD;
                        }

                        int blob = 0;
                        BCLDebug.Assert(datasize == 4, "datasize==4");
                        // Here, datasize must be four when calling this
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }

                    break;

                case Win32Native.REG_SZ:
                    {
                        StringBuilder blob = new StringBuilder(datasize / 2);
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        data = blob.ToString();
                    }

                    break;

                case Win32Native.REG_EXPAND_SZ:
                    {
                        StringBuilder blob = new StringBuilder(datasize / 2);
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        if (doNotExpand)
                            data = blob.ToString();
                        else
                            data = Environment.ExpandEnvironmentVariables(blob.ToString());
                    }

                    break;
                case Win32Native.REG_MULTI_SZ:
                    {
                        IList<string> strings = new List<string>();

                        char[] blob = new char[datasize / 2];
                        ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);

                        int cur = 0;
                        int len = blob.Length;

                        while (ret == 0 && cur < len)
                        {
                            int nextNull = cur;
                            while (nextNull < len && blob[nextNull] != (char)0)
                            {
                                nextNull++;
                            }

                            if (nextNull < len)
                            {
                                BCLDebug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                if (nextNull - cur > 0)
                                {
                                    strings.Add(new string(blob, cur, nextNull - cur));
                                }
                                else
                                {
                                    // we found an empty string.  But if we're at the end of the data,
                                    // it's just the extra null terminator.
                                    if (nextNull != len - 1)
                                        strings.Add(string.Empty);
                                }
                            }
                            else
                            {
                                strings.Add(new string(blob, cur, len - cur));
                            }

                            cur = nextNull + 1;
                        }

                        data = new string[strings.Count];
                        strings.CopyTo((string[])data, 0);
                        // data = strings.GetAllItems(String.class);
                    }

                    break;
                case Win32Native.REG_NONE:
                case Win32Native.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        /// <summary>
        /// <para>Retrieves the registry data type of the value associated with the specified name.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A RegistryValueKind value representing the registry data type of the value associated with name.</returns>
        /// <param name="name">The value name whose data type is to be retrieved.</param>
        [ComVisible(false)]
        public RegistryValueKind GetValueKind(string name)
        {
            CheckValueReadPermission(name);
            EnsureNotDisposed();

            int type = 0;
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null);

            if (!Enum.IsDefined(typeof(RegistryValueKind), type))
                return RegistryValueKind.Unknown;
            else
                return (RegistryValueKind)type;
        }

        /**
         * Retrieves the current state of the dirty property.
         *
         * A key is marked as dirty if any operation has occurred that modifies the
         * contents of the key.
         *
         * @return <b>true</b> if the key has been modified.
         */
        private bool IsDirty()
        {
            return (_state & STATE_DIRTY) != 0;
        }

        private bool IsSystemKey()
        {
            return (_state & STATE_SYSTEMKEY) != 0;
        }

        private bool IsWritable()
        {
            return (_state & STATE_WRITEACCESS) != 0;
        }

        /// <summary>
        /// <para>Retrieves the name of the key.</para>
        /// </summary>
        /// <returns>The name of the key.</returns>
        public string Name
        {
            get
            {
                EnsureNotDisposed();
                return _keyName;
            }
        }

        private void SetDirty()
        {
            _state |= STATE_DIRTY;
        }

        /// <summary>
        /// <para>Sets the specified value. Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="name">Name of value to store data in.</param>
        /// <param name="value">Data to store.</param>
        public void SetValue(string name, object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        /// <summary>
        /// <para>Sets the specified value. Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="name">Name of value to store data in.</param>
        /// <param name="value">Data to store.</param>
        /// <param name="valueKind">The registry data type to use when storing the data.</param>
        [ComVisible(false)]
        public unsafe void SetValue(string name, object value, RegistryValueKind valueKind)
        {
            if (value == null)
                throw new ArgumentNullException(RegistryProviderStrings.Arg_Value);

            if (name != null && name.Length > MaxValueNameLength)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegValueNameStrLenBug);
            }

            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
                throw new ArgumentException(RegistryProviderStrings.Arg_RegBadKeyKind);

            EnsureWriteable();

            // Require a transaction. This will throw for "Base" keys because they aren't associated with a transaction.
            VerifyTransaction();

            if (ContainsRegistryValue(name))
            { // Existing key
                CheckValueWritePermission(name);
            }
            else
            { // Creating a new value
                CheckValueCreatePermission(name);
            }

            if (valueKind == RegistryValueKind.Unknown)
            {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value);
            }

            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            string data = value.ToString();
                            // divide by 2 to account for unicode.
                            if (MaxValueDataLength / 2 < data.Length)
                            {
                                throw new ArgumentException(RegistryProviderStrings.Arg_ValueDataLenBug);
                            }

                            ret = Win32Native.RegSetValueEx(_hkey,
                                name,
                                0,
                                valueKind,
                                data,
                                data.Length * 2 + 2);
                            break;
                        }

                    case RegistryValueKind.MultiString:
                        {
                            // Other thread might modify the input array after we calculate the buffer length.
                            // Make a copy of the input array to be safe.
                            string[] dataStrings = (string[])(((string[])value).Clone());

                            int sizeInBytes = 0;

                            // First determine the size of the array
                            //
                            for (int i = 0; i < dataStrings.Length; i++)
                            {
                                if (dataStrings[i] == null)
                                {
                                    throw new ArgumentException(RegistryProviderStrings.Arg_RegSetStrArrNull);
                                }

                                sizeInBytes += (dataStrings[i].Length + 1) * 2;
                            }

                            sizeInBytes += 2;

                            if (MaxValueDataLength < sizeInBytes)
                            {
                                throw new ArgumentException(RegistryProviderStrings.Arg_ValueDataLenBug);
                            }

                            byte[] basePtr = new byte[sizeInBytes];
                            fixed (byte* b = basePtr)
                            {
                                int totalBytesMoved = 0;
                                int currentBytesMoved = 0;

                                // Write out the strings...
                                //
                                for (int i = 0; i < dataStrings.Length; i++)
                                {
                                    currentBytesMoved = System.Text.Encoding.Unicode.GetBytes(dataStrings[i], 0, dataStrings[i].Length, basePtr, totalBytesMoved);
                                    totalBytesMoved += currentBytesMoved;
                                    basePtr[totalBytesMoved] = 0;
                                    basePtr[totalBytesMoved + 1] = 0;
                                    totalBytesMoved += 2;
                                }

                                ret = Win32Native.RegSetValueEx(_hkey,
                                    name,
                                    0,
                                    RegistryValueKind.MultiString,
                                    basePtr,
                                    sizeInBytes);
                            }

                            break;
                        }

                    case RegistryValueKind.Binary:
                        byte[] dataBytes = (byte[])value;
                        if (MaxValueDataLength < dataBytes.Length)
                        {
                            throw new ArgumentException(RegistryProviderStrings.Arg_ValueDataLenBug);
                        }

                        ret = Win32Native.RegSetValueEx(_hkey,
                            name,
                            0,
                            RegistryValueKind.Binary,
                            dataBytes,
                            dataBytes.Length);
                        break;

                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail.
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Win32Native.RegSetValueEx(_hkey,
                                name,
                                0,
                                RegistryValueKind.DWord,
                                ref data,
                                4);
                            break;
                        }

                    case RegistryValueKind.QWord:
                        {
                            long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Win32Native.RegSetValueEx(_hkey,
                                name,
                                0,
                                RegistryValueKind.QWord,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSetMismatchedKind);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSetMismatchedKind);
            }
            catch (FormatException)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(RegistryProviderStrings.Arg_RegSetMismatchedKind);
            }

            if (ret == 0)
            {
                SetDirty();
            }
            else
                Win32Error(ret, null);
        }

        private RegistryValueKind CalculateValueKind(object value)
        {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array)
            {
                if (value is byte[])
                    return RegistryValueKind.Binary;
                else if (value is string[])
                    return RegistryValueKind.MultiString;
                else
                {
                    string resourceTemplate = RegistryProviderStrings.Arg_RegSetBadArrType;
                    string resource = string.Format(CultureInfo.CurrentCulture, resourceTemplate, value.GetType().Name);
                    throw new ArgumentException(resource);
                }
            }
            else
                return RegistryValueKind.String;
        }

        /**
         * Retrieves a string representation of this key.
         *
         * @return a string representing the key.
         */
        /// <summary>
        /// <para>Retrieves a string representation of this key.</para>
        /// </summary>
        /// <returns>A string representing the key.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return _keyName;
        }

        /// <summary>
        /// <para>Returns the access control security for the current registry key.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A TransactedRegistrySecurity object that describes the access control
        /// permissions on the registry key represented by the current TransactedRegistryKey.</returns>
        public TransactedRegistrySecurity GetAccessControl()
        {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        /// <summary>
        /// <para>Returns the access control security for the current registry key.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <returns>A TransactedRegistrySecurity object that describes the access control
        /// permissions on the registry key represented by the current TransactedRegistryKey.</returns>
        /// <param name="includeSections">A bitwise combination of AccessControlSections values that specifies the type of security information to get.</param>
        public TransactedRegistrySecurity GetAccessControl(AccessControlSections includeSections)
        {
            EnsureNotDisposed();
            // Don't require a transaction. We don't want to throw for "Base" keys.
            return new TransactedRegistrySecurity(_hkey, _keyName, includeSections);
        }

        /// <summary>
        /// <para>Applies Windows access control security to an existing registry key.
        /// Utilizes Transaction.Current for its transaction.</para>
        /// </summary>
        /// <param name="registrySecurity">A TransactedRegistrySecurity object that specifies the access control security to apply to the current subkey.</param>
        public void SetAccessControl(TransactedRegistrySecurity registrySecurity)
        {
            EnsureWriteable();
            ArgumentNullException.ThrowIfNull(registrySecurity);
            // Require a transaction. This will throw for "Base" keys because they aren't associated with a transaction.
            VerifyTransaction();

            registrySecurity.Persist(_hkey, _keyName);
        }

        /**
         * After calling GetLastWin32Error(), it clears the last error field,
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager.
         */
        internal void Win32Error(int errorCode, string str)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                    {
                        string resourceTemplate = RegistryProviderStrings.UnauthorizedAccess_RegistryKeyGeneric_Key;
                        string resource = string.Format(CultureInfo.CurrentCulture, resourceTemplate, str);
                        throw new UnauthorizedAccessException(resource);
                    }
                    else
                        throw new UnauthorizedAccessException();

                case Win32Native.ERROR_INVALID_HANDLE:
                    // **
                    // * For normal RegistryKey instances we dispose the SafeRegHandle and throw IOException.
                    // * However, for HKEY_PERFORMANCE_DATA (on a local or remote machine) we avoid disposing the
                    // * SafeRegHandle and only throw the IOException.  This is to workaround reentrancy issues
                    // * in PerformanceCounter.NextValue() where the API could throw {NullReference, ObjectDisposed, ArgumentNull}Exception
                    // * on reentrant calls because of this error code path in RegistryKey
                    // *
                    // * Normally we'd make our caller synchronize access to a shared RegistryKey instead of doing something like this,
                    // * however we shipped PerformanceCounter.NextValue() un-synchronized in v2.0RTM and customers have taken a dependency on
                    // * this behavior (being able to simultaneously query multiple remote-machine counters on multiple threads, instead of
                    // * having serialized access).
                    // *
                    // * FUTURE: Consider changing PerformanceCounterLib to handle its own Win32 RegistryKey API calls instead of depending
                    // * on Microsoft.Win32.RegistryKey, so that RegistryKey can be clean of special-cases for HKEY_PERFORMANCE_DATA.
                    //
                    _hkey.SetHandleAsInvalid();
                    _hkey = null;
                    goto default;

                case Win32Native.ERROR_FILE_NOT_FOUND:
                    {
                        string resourceTemplate = RegistryProviderStrings.Arg_RegKeyNotFound;
                        string resource = string.Format(CultureInfo.CurrentCulture, resourceTemplate, errorCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        throw new IOException(resource);
                    }

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        internal static void Win32ErrorStatic(int errorCode, string str)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                    {
                        string resourceTemplate = RegistryProviderStrings.UnauthorizedAccess_RegistryKeyGeneric_Key;
                        string resource = string.Format(CultureInfo.CurrentCulture, resourceTemplate, str);
                        throw new UnauthorizedAccessException(resource);
                    }
                    else
                        throw new UnauthorizedAccessException();

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        internal static string FixupName(string name)
        {
            BCLDebug.Assert(name != null, "[FixupName]name!=null");
            if (name.Contains('\\'))
                return name;

            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (sb[temp] == '\\') // Remove trailing slash
                sb.Length = temp;
            return sb.ToString();
        }

        private static void FixupPath(StringBuilder path)
        {
            int length = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;

            int i = 1;
            while (i < length - 1)
            {
                if (path[i] == '\\')
                {
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\')
                        {
                            path[i] = markerChar;
                            i++;
                            fixup = true;
                        }
                        else
                            break;
                    }
                }

                i++;
            }

            if (fixup)
            {
                i = 0;
                int j = 0;
                while (i < length)
                {
                    if (path[i] == markerChar)
                    {
                        i++;
                        continue;
                    }

                    path[j] = path[i];
                    i++;
                    j++;
                }

                path.Length += j - i;
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private void CheckOpenSubKeyPermission(string subkeyName, bool subKeyWritable)
        {
            // If the parent key is not opened under default mode, we have access already.
            // If the parent key is opened under default mode, we need to check for permission.
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                CheckSubKeyReadPermission(subkeyName);
            }

            if (subKeyWritable && (_checkMode == RegistryKeyPermissionCheck.ReadSubTree))
            {
                CheckSubTreeReadWritePermission(subkeyName);
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private void CheckOpenSubKeyPermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck)
        {
            if (subKeyCheck == RegistryKeyPermissionCheck.Default)
            {
                if (_checkMode == RegistryKeyPermissionCheck.Default)
                {
                    CheckSubKeyReadPermission(subkeyName);
                }
            }

            CheckSubTreePermission(subkeyName, subKeyCheck);
        }

        private void CheckSubTreePermission(string subkeyName, RegistryKeyPermissionCheck subKeyCheck)
        {
            if (subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree)
            {
                if (_checkMode == RegistryKeyPermissionCheck.Default)
                {
                    CheckSubTreeReadPermission(subkeyName);
                }
            }
            else if (subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree)
            {
                if (_checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree)
                {
                    CheckSubTreeReadWritePermission(subkeyName);
                }
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        // Suppressed because keyName and subkeyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubKeyWritePermission(string subkeyName)
        {
            BCLDebug.Assert(_checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                // If we want to open a subkey of a read-only key as writeable, we need to do the check.
                new RegistryPermission(RegistryPermissionAccess.Write, _keyName + "\\" + subkeyName + "\\.").Demand();
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        // Suppressed because keyName and subkeyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubKeyReadPermission(string subkeyName)
        {
            BCLDebug.Assert(_checkMode == RegistryKeyPermissionCheck.Default, "Should be called from a key opened under default mode only!");
            new RegistryPermission(RegistryPermissionAccess.Read, _keyName + "\\" + subkeyName + "\\.").Demand();
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        // Suppressed because keyName and subkeyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubKeyCreatePermission(string subkeyName)
        {
            BCLDebug.Assert(_checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                new RegistryPermission(RegistryPermissionAccess.Create, _keyName + "\\" + subkeyName + "\\.").Demand();
            }
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        // Suppressed because keyName and subkeyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubTreeReadPermission(string subkeyName)
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                new RegistryPermission(RegistryPermissionAccess.Read, _keyName + "\\" + subkeyName + "\\").Demand();
            }
        }

        // Suppressed because keyName and subkeyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubTreeWritePermission(string subkeyName)
        {
            BCLDebug.Assert(_checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                new RegistryPermission(RegistryPermissionAccess.Write, _keyName + "\\" + subkeyName + "\\").Demand();
            }
        }

        // Suppressed because keyName and valueName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckSubTreeReadWritePermission(string subkeyName)
        {
            // If we want to open a subkey of a read-only key as writeable, we need to do the check.
            new RegistryPermission(RegistryPermissionAccess.Write | RegistryPermissionAccess.Read,
                    _keyName + "\\" + subkeyName).Demand();
        }

        // Suppressed because keyName and valueName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckValueWritePermission(string valueName)
        {
            BCLDebug.Assert(_checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");
            // skip the security check if the key is opened under write mode
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                new RegistryPermission(RegistryPermissionAccess.Write, _keyName + "\\" + valueName).Demand();
            }
        }

        // Suppressed because keyName and valueName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckValueCreatePermission(string valueName)
        {
            BCLDebug.Assert(_checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating value under read-only key!");
            // skip the security check if the key is opened under write mode
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                new RegistryPermission(RegistryPermissionAccess.Create, _keyName + "\\" + valueName).Demand();
            }
        }

        // Suppressed because keyName and valueName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckValueReadPermission(string valueName)
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                // only need to check for default mode (dynamic check)
                new RegistryPermission(RegistryPermissionAccess.Read, _keyName + "\\" + valueName).Demand();
            }
        }

        // Suppressed because keyName won't change.
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        private void CheckKeyReadPermission()
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                // only need to check for default mode (dynamic check)
                new RegistryPermission(RegistryPermissionAccess.Read, _keyName + "\\.").Demand();
            }
        }

        private bool ContainsRegistryValue(string name)
        {
            int type = 0;
            int datasize = 0;
            int retval = Win32Native.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);
            return retval == 0;
        }

        private void EnsureNotDisposed()
        {
            if (_hkey == null)
            {
                throw new ObjectDisposedException(_keyName,
                                  RegistryProviderStrings.ObjectDisposed_RegKeyClosed);
            }
        }

        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                throw new UnauthorizedAccessException(RegistryProviderStrings.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        private static int GetRegistryKeyAccess(bool isWritable)
        {
            int winAccess;
            if (!isWritable)
            {
                winAccess = Win32Native.KEY_READ;
            }
            else
            {
                winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE;
            }

            return winAccess;
        }

        private static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode)
        {
            int winAccess = 0;
            switch (mode)
            {
                case RegistryKeyPermissionCheck.ReadSubTree:
                case RegistryKeyPermissionCheck.Default:
                    winAccess = Win32Native.KEY_READ;
                    break;

                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE;
                    break;

                default:
                    BCLDebug.Assert(false, "unexpected code path");
                    break;
            }

            return winAccess;
        }

        // Suppressed to be consistent with naming in Microsoft.Win32.RegistryKey
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly")]
        private RegistryKeyPermissionCheck GetSubKeyPermissionCheck(bool subkeyWritable)
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                return _checkMode;
            }

            if (subkeyWritable)
            {
                return RegistryKeyPermissionCheck.ReadWriteSubTree;
            }
            else
            {
                return RegistryKeyPermissionCheck.ReadSubTree;
            }
        }

        private static void ValidateKeyName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(RegistryProviderStrings.Arg_Name);
            }

            int nextSlash = name.IndexOf('\\');
            int current = 0;
            while (nextSlash != -1)
            {
                if ((nextSlash - current) > MaxKeyLength)
                    throw new ArgumentException(RegistryProviderStrings.Arg_RegKeyStrLenBug);

                current = nextSlash + 1;
                nextSlash = name.IndexOf('\\', current);
            }

            if ((name.Length - current) > MaxKeyLength)
                throw new ArgumentException(RegistryProviderStrings.Arg_RegKeyStrLenBug);
        }

        private static void ValidateKeyMode(RegistryKeyPermissionCheck mode)
        {
            if (mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree)
            {
                throw new ArgumentException(RegistryProviderStrings.Argument_InvalidRegistryKeyPermissionCheck);
            }
        }

        private static void ValidateKeyRights(int rights)
        {
            if (0 != (rights & ~((int)RegistryRights.FullControl)))
            {
                // We need to throw SecurityException here for compatibility reason,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(RegistryProviderStrings.Security_RegistryPermission);
            }
        }

        private void VerifyTransaction()
        {
            // Require a transaction. This will throw for "Base" keys because they aren't associated with a transaction.
            if (_myTransaction == null)
            {
                throw new InvalidOperationException(RegistryProviderStrings.InvalidOperation_NotAssociatedWithTransaction);
            }

            if (!_myTransaction.Equals(Transaction.Current))
            {
                throw new InvalidOperationException(RegistryProviderStrings.InvalidOperation_MustUseSameTransaction);
            }
        }
        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }
}
