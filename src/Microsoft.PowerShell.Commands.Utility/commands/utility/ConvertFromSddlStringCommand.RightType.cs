using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    public sealed partial class ConvertFromSddlStringCommand
    {
        /// <summary>
        /// Types defining access control right flags.
        /// </summary>
        public enum RightType
        {
            /// <summary>
            /// <see cref="System.Security.AccessControl.FileSystemRights"/> type rights.
            /// </summary>
            FileSystemRights,

            /// <summary>
            /// <see cref="System.Security.AccessControl.RegistryRights"/> type rights.
            /// </summary>
            RegistryRights,

            /// <summary>
            /// <see cref="System.DirectoryServices.ActiveDirectoryRights"/> type rights.
            /// </summary>
            ActiveDirectoryRights,

            /// <summary>
            /// <see cref="System.Security.AccessControl.MutexRights"/> type rights.
            /// </summary>
            MutexRights,

            /// <summary>
            /// <see cref="System.Security.AccessControl.SemaphoreRights"/> type rights.
            /// </summary>
            SemaphoreRights,

#if !CORECLR
            /// <summary>
            /// <see cref="System.Security.AccessControl.CryptoKeyRights"/> type rights.
            /// </summary>
            CryptoKeyRights,
#endif

            /// <summary>
            /// <see cref="System.Security.AccessControl.EventWaitHandleRights"/> type rights.
            /// </summary>
            EventWaitHandleRights,

        }

        private static readonly Dictionary<RightType, Dictionary<int, string>> RightTypeFlags =
            new Dictionary<RightType, Dictionary<int, string>>
            {
                {
                    RightType.FileSystemRights,
                    new Dictionary<int, string>
                    {
                        { 1, "ReadData" },
                        { 2, "CreateFiles" },
                        { 4, "AppendData" },
                        { 8, "ReadExtendedAttributes" },
                        { 16, "WriteExtendedAttributes" },
                        { 32, "ExecuteFile" },
                        { 64, "DeleteSubdirectoriesAndFiles" },
                        { 128, "ReadAttributes" },
                        { 256, "WriteAttributes" },
                        { 278, "Write" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 131209, "Read" },
                        { 131241, "ReadAndExecute" },
                        { 197055, "Modify" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 1048576, "Synchronize" },
                        { 2032127, "FullControl" },
                    }
                },
                {
                    RightType.RegistryRights,
                    new Dictionary<int, string>
                    {
                        { 1, "QueryValues" },
                        { 2, "SetValue" },
                        { 4, "CreateSubKey" },
                        { 8, "EnumerateSubKeys" },
                        { 16, "Notify" },
                        { 32, "CreateLink" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 131078, "WriteKey" },
                        { 131097, "ReadKey" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 983103, "FullControl" },
                    }
                },
                {
                    RightType.ActiveDirectoryRights,
                    new Dictionary<int, string>
                    {
                        { 1, "CreateChild" },
                        { 2, "DeleteChild" },
                        { 4, "ListChildren" },
                        { 8, "Self" },
                        { 16, "ReadProperty" },
                        { 32, "WriteProperty" },
                        { 64, "DeleteTree" },
                        { 128, "ListObject" },
                        { 256, "ExtendedRight" },
                        { 65536, "Delete" },
                        { 131072, "ReadControl" },
                        { 131076, "GenericExecute" },
                        { 131112, "GenericWrite" },
                        { 131220, "GenericRead" },
                        { 262144, "WriteDacl" },
                        { 524288, "WriteOwner" },
                        { 983551, "GenericAll" },
                        { 1048576, "Synchronize" },
                        { 16777216, "AccessSystemSecurity" },
                    }
                },
                {
                    RightType.MutexRights,
                    new Dictionary<int, string>
                    {
                        { 1, "Modify" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 1048576, "Synchronize" },
                        { 2031617, "FullControl" },
                    }
                },
                {
                    RightType.SemaphoreRights,
                    new Dictionary<int, string>
                    {
                        { 2, "Modify" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 1048576, "Synchronize" },
                        { 2031619, "FullControl" },
                    }
                },
#if !CORECLR
                {
                    RightType.CryptoKeyRights,
                    new Dictionary<int, string>
                    {
                        { 1, "ReadData" },
                        { 2, "WriteData" },
                        { 8, "ReadExtendedAttributes" },
                        { 16, "WriteExtendedAttributes" },
                        { 128, "ReadAttributes" },
                        { 256, "WriteAttributes" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 1048576, "Synchronize" },
                        { 2032027, "FullControl" },
                        { 268435456, "GenericAll" },
                        { 536870912, "GenericExecute" },
                        { 1073741824, "GenericWrite" },
                        { -2147483648, "GenericRead" },
                    }
                },
#endif
                {
                    RightType.EventWaitHandleRights,
                    new Dictionary<int, string>
                    {
                        { 2, "Modify" },
                        { 65536, "Delete" },
                        { 131072, "ReadPermissions" },
                        { 262144, "ChangePermissions" },
                        { 524288, "TakeOwnership" },
                        { 1048576, "Synchronize" },
                        { 2031619, "FullControl" },
                    }
                },
            };
    }
}

