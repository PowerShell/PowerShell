// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Windows
    {
        internal const int CONNECT_NOPERSIST = 0x00000000;
        internal const int CONNECT_UPDATE_PROFILE = 0x00000001;
        internal const int RESOURCE_GLOBALNET = 0x00000002;
        internal const int RESOURCETYPE_ANY = 0x00000000;
        internal const int RESOURCEDISPLAYTYPE_GENERIC = 0x00000000;
        internal const int RESOURCEUSAGE_CONNECTABLE = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct NETRESOURCEW
        {
            public int Scope;
            public int Type;
            public int DisplayType;
            public int Usage;
            public char* LocalName;
            public char* RemoteName;
            public char* Comment;
            public char* Provider;
        }

        [LibraryImport("mpr.dll", EntryPoint = "WNetAddConnection2W", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int WNetAddConnection2(ref NETRESOURCEW netResource, byte[] password, string userName, int flags);

        internal static unsafe int WNetAddConnection2(string localName, string remoteName, byte[] password, string userName, int connectType)
        {
            if (s_WNetApiNotAvailable)
            {
                return ERROR_NOT_SUPPORTED;
            }

            int errorCode = ERROR_NO_NETWORK;

            fixed (char* pinnedLocalName = localName)
            fixed (char* pinnedRemoteName = remoteName)
            {
                NETRESOURCEW resource = new NETRESOURCEW()
                {
                    Comment = null,
                    DisplayType = RESOURCEDISPLAYTYPE_GENERIC,
                    LocalName = pinnedLocalName,
                    Provider = null,
                    RemoteName = pinnedRemoteName,
                    Scope = RESOURCE_GLOBALNET,
                    Type = RESOURCETYPE_ANY,
                    Usage = RESOURCEUSAGE_CONNECTABLE
                };

                try
                {
                    errorCode = WNetAddConnection2(ref resource, password, userName, connectType);
                }
                catch (System.DllNotFoundException)
                {
                    s_WNetApiNotAvailable = true;
                    return ERROR_NOT_SUPPORTED;
                }
            }

            return errorCode;
        }
    }
}
