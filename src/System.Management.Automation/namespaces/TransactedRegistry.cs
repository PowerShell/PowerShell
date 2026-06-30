// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

//
// NOTE: A vast majority of this code was copied from BCL in
// ndp\clr\src\BCL\Microsoft\Win32\Registry.cs.
// Namespace: Microsoft.Win32
//

using BCLDebug = System.Diagnostics.Debug;

namespace Microsoft.PowerShell.Commands.Internal
{
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.CodeAnalysis;

    /**
     * Registry encapsulation. Contains members representing all top level system
     * keys.
     *
     * @security(checkClassLinking=on)
     */
    // This class contains only static members and does not need to be serializable.
    [ComVisible(true)]
    // Suppressed because these objects need to be accessed from CmdLets.
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal static class TransactedRegistry
    {
        private const string resBaseName = "RegistryProviderStrings";
        /**
         * Current User Key.
         *
         * This key should be used as the root for all user specific settings.
         */
        /// <summary>TransactedRegistry.CurrentUser
        /// <para>This static method returns a TransactedRegistryKey object that represents the base
        /// key HKEY_CURRENT_USER. Because it is a base key, there is no transaction associated with
        /// the returned TransactedRegistryKey. This means that values modified using the returned
        /// TransactedRegistryKey are NOT modified within a transaction.</para>
        /// <para>However, if the returned TransactedRegistryKey is used to create, open, or delete
        /// subkeys, there must be a Transaction.Current and the resulting TransactedRegistryKey from those operations ARE associated with
        /// the transaction.</para>
        /// </summary>
        internal static readonly TransactedRegistryKey CurrentUser = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CURRENT_USER);

        /**
         * Local Machine Key.
         *
         * This key should be used as the root for all machine specific settings.
         */
        /// <summary>TransactedRegistry.LocalMachine
        /// <para>This static method returns a TransactedRegistryKey object that represents the base
        /// key HKEY_LOCAL_MACHINE. Because it is a base key, there is no transaction associated with
        /// the returned TransactedRegistryKey. This means that values modified using the returned
        /// TransactedRegistryKey are NOT modified within a transaction.</para>
        /// <para>However, if the returned TransactedRegistryKey is used to create, open, or delete
        /// subkeys, there must be a Transaction.Current and the resulting TransactedRegistryKey from those operations ARE associated with
        /// the transaction.</para>
        /// </summary>
        internal static readonly TransactedRegistryKey LocalMachine = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_LOCAL_MACHINE);

        /**
         * Classes Root Key.
         *
         * This is the root key of class information.
         */
        /// <summary>TransactedRegistry.ClassesRoot
        /// <para>This static method returns a TransactedRegistryKey object that represents the base
        /// key HKEY_CLASSES_ROOT. Because it is a base key, there is no transaction associated with
        /// the returned TransactedRegistryKey. This means that values modified using the returned
        /// TransactedRegistryKey are NOT modified within a transaction.</para>
        /// <para>However, if the returned TransactedRegistryKey is used to create, open, or delete
        /// subkeys, there must be a Transaction.Current and the resulting TransactedRegistryKey from those operations ARE associated with
        /// the transaction.</para>
        /// </summary>
        internal static readonly TransactedRegistryKey ClassesRoot = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CLASSES_ROOT);

        /**
         * Users Root Key.
         *
         * This is the root of users.
         */
        /// <summary>TransactedRegistry.Users
        /// <para>This static method returns a TransactedRegistryKey object that represents the base
        /// key HKEY_USERS. Because it is a base key, there is no transaction associated with
        /// the returned TransactedRegistryKey. This means that values modified using the returned
        /// TransactedRegistryKey are NOT modified within a transaction.</para>
        /// <para>However, if the returned TransactedRegistryKey is used to create, open, or delete
        /// subkeys, there must be a Transaction.Current and the resulting TransactedRegistryKey from those operations ARE associated with
        /// the transaction.</para>
        /// </summary>
        internal static readonly TransactedRegistryKey Users = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_USERS);

        /**
         * Current Config Root Key.
         *
         * This is where current configuration information is stored.
         */
        /// <summary>TransactedRegistry.CurrentConfig
        /// <para>This static method returns a TransactedRegistryKey object that represents the base
        /// key HKEY_CURRENT_CONFIG. Because it is a base key, there is no transaction associated with
        /// the returned TransactedRegistryKey. This means that values modified using the returned
        /// TransactedRegistryKey are NOT modified within a transaction.</para>
        /// <para>However, if the returned TransactedRegistryKey is used to create, open, or delete
        /// subkeys, there must be a Transaction.Current and the resulting TransactedRegistryKey from those operations ARE associated with
        /// the transaction.</para>
        /// </summary>
        internal static readonly TransactedRegistryKey CurrentConfig = TransactedRegistryKey.GetBaseKey(BaseRegistryKeys.HKEY_CURRENT_CONFIG);
    }
}
