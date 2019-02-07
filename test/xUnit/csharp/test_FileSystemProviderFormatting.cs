using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Parallel
{
    public class FileSystemProviderFormatting : IDisposable
    {
        private readonly string modeTestDir;

        public FileSystemProviderFormatting()
        {
            modeTestDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(modeTestDir);

        }

        void IDisposable.Dispose()
        {
            var junctionPath = $"{modeTestDir}\\junctionDir";
            if (Directory.Exists(junctionPath))
            {
                Directory.Delete(junctionPath);
            }

            Directory.Delete(modeTestDir, recursive: true);
        }

        [Theory]
        [InlineData("d----", "d----", "Directory", "directory", FileAttributes.Directory)]
        [InlineData("l----", "l----", "SymbolicLink", "symDir", FileAttributes.Directory | FileAttributes.ReparsePoint, "targetDir2")]
#if !UNIX
        [InlineData("l----", "l----", "Junction", "junctionDir", FileAttributes.Directory | FileAttributes.ReparsePoint, "targetDir1")]
        [InlineData("-a---", "-a---", "File", "archiveFile", FileAttributes.Archive)]
        [InlineData("la---", "la---", "SymbolicLink", "symFile", FileAttributes.Archive | FileAttributes.ReparsePoint, "targetFile1")]
        [InlineData("la---", "-a---", "HardLink", "hardlink", FileAttributes.Archive, "targetFile2")]
#endif
        public void TestFileSystemInfoModeString(
            string expectedMode,
            string expectedModeWithoutHardLink,
            string itemType,
            string itemName,
            FileAttributes fileAttributes,
            string target = null)
        {
            var targetFullName = target != null ? Path.Combine(modeTestDir, target) : null;
            if (target != null)
            {
                if (target.IndexOf("File", StringComparison.Ordinal) != -1)
                {
                    if (!File.Exists(targetFullName))
                    {
                        using (File.Create(targetFullName))
                        {
                        }
                    }
                }
                else if (target.IndexOf("Dir", StringComparison.Ordinal) != -1)
                {
                    if (!Directory.Exists(targetFullName))
                    {
                        Directory.CreateDirectory(targetFullName);
                    }
                }
                else
                {
                    throw new ArgumentException($"Unknown target {target}", nameof(target));
                }
            }

            var iss = InitialSessionState.CreateDefault2();
            using (var powerShell = PowerShell.Create(iss))
            {
                PSObject item = powerShell.Runspace.SessionStateProxy.InvokeProvider.Item.New(modeTestDir, itemName, itemType, targetFullName).First();

                Assert.True(item.BaseObject is FileSystemInfo);

                var actualMode = FileSystemProvider.Mode(item);
                Assert.Equal(expectedMode, actualMode);

                var actualModeWithOutHardLink = FileSystemProvider.ModeWithoutHardLink(item);
                Assert.Equal(expectedModeWithoutHardLink, actualModeWithOutHardLink);
                var fsi = (FileSystemInfo)item.BaseObject;
                Assert.Equal(fileAttributes, fsi.Attributes);
            }
        }
    }
}
