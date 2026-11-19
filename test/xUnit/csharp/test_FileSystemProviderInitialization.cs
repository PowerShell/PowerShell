// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Sequential
{
    /// <summary>
    /// Tests default drive initialization when optional network mapping metadata is unavailable.
    /// </summary>
    public partial class FileSystemProviderInitializationTests
    {
        [SkippableFact]
        public void UnavailableNetworkMappingDoesNotInterruptDefaultDrives()
        {
            Skip.IfNot(Platform.IsWindows, "Network DOS-device mappings are Windows-only.");

            string device = FindUnusedDrive();
            string root = device + "\\";
            string target = @"\??\UNC\localhost\powershell-unavailable-mapping-" + Guid.NewGuid().ToString("N");

            // An unchecked user DOS-device link needs no share, credentials or server.
            // NO_BROADCAST_SYSTEM avoids notifying unrelated applications.
            const uint CreateFlags = 0x1 | 0x8;
            Assert.True(
                DefineDosDevice(CreateFlags, device, target),
                "Could not create the independent DOS-device fixture: " + Marshal.GetLastPInvokeError());

            try
            {
                Assert.Equal(target, ReadDosDevice(device));
                var drive = new DriveInfo(root);
                Assert.Equal(DriveType.Network, drive.DriveType);
                Assert.False(drive.IsReady);
                Assert.Contains(DriveInfo.GetDrives(), item => item.Name == root);
                Win32Exception lookupError = Assert.Throws<Win32Exception>(
                    () => FileSystemProvider.GetUNCForNetworkDrive(device.Substring(0, 1)));
                Assert.NotEqual(0, lookupError.NativeErrorCode);

                using Runspace runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
                runspace.Open();

                // Enumerate before any named drive lookup: lazy automount could hide
                // the failed startup registration, but cannot recover Temp creation.
                var drives = runspace.SessionStateProxy.Drive.GetAllForProvider("FileSystem");
                Assert.Contains(drives, item => item.Name == "Temp" && item.Root == Path.GetTempPath());
                Assert.Contains(drives, item => item.Root == Path.GetPathRoot(Environment.CurrentDirectory));
                PSDriveInfo networkDrive = Assert.Single(drives, item => item.Name == device.Substring(0, 1));
                Assert.Equal(root, networkDrive.Root);
                Assert.Null(networkDrive.DisplayRoot);
            }
            finally
            {
                // Only remove the exact definition created above, never another
                // mapping that might now own the same drive letter.
                Assert.Equal(target, ReadDosDevice(device));
                const uint RemoveFlags = 0x1 | 0x2 | 0x4 | 0x8;
                Assert.True(
                    DefineDosDevice(RemoveFlags, device, target),
                    "Could not remove the exact owned DOS-device fixture: " + Marshal.GetLastPInvokeError());
                char[] buffer = new char[32768];
                Assert.Equal(0u, QueryDosDevice(device, buffer, buffer.Length));
                Assert.Equal(2, Marshal.GetLastPInvokeError());
                Assert.DoesNotContain(DriveInfo.GetDrives(), item => item.Name == root);
            }
        }

        private static string FindUnusedDrive()
        {
            char[] buffer = new char[32768];
            for (char letter = 'Y'; letter >= 'F'; letter--)
            {
                string device = letter + ":";
                if (QueryDosDevice(device, buffer, buffer.Length) == 0 && Marshal.GetLastPInvokeError() == 2)
                {
                    return device;
                }
            }

            throw new InvalidOperationException("No unused drive letter is available for the independent DOS-device fixture.");
        }

        private static string ReadDosDevice(string device)
        {
            char[] buffer = new char[32768];
            uint count = QueryDosDevice(device, buffer, buffer.Length);
            if (count == 0)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }

            return new string(buffer, 0, Array.IndexOf(buffer, '\0'));
        }

        [LibraryImport("kernel32.dll", EntryPoint = "DefineDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DefineDosDevice(uint flags, string device, string target);

        [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial uint QueryDosDevice(string device, Span<char> target, int size);
    }
}
