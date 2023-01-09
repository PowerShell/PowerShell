// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

#if !UNIX

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Defines the implementation of the 'Clear-RecycleBin' cmdlet.
    /// This cmdlet clear all files in the RecycleBin for the given DriveLetter.
    /// If not DriveLetter is specified, then the RecycleBin for all drives are cleared.
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "RecycleBin", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2109377", ConfirmImpact = ConfirmImpact.High)]
    public class ClearRecycleBinCommand : PSCmdlet
    {
        private string[] _drivesList;
        private DriveInfo[] _availableDrives;
        private bool _force;

        /// <summary>
        /// Property that sets DriveLetter parameter.
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] DriveLetter
        {
            get { return _drivesList; }

            set { _drivesList = value; }
        }

        /// <summary>
        /// Property that sets force parameter. This will allow to clear the recyclebin.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        /// <summary>
        /// This method implements the BeginProcessing method for Clear-RecycleBin command.
        /// </summary>
        protected override void BeginProcessing()
        {
            _availableDrives = DriveInfo.GetDrives();
        }

        /// <summary>
        /// This method implements the ProcessRecord method for Clear-RecycleBin command.
        /// </summary>
        protected override void ProcessRecord()
        {
            // There are two scenarios:
            // 1) The user provides a list of drives.
            if (_drivesList != null)
            {
                foreach (var drive in _drivesList)
                {
                    if (!IsValidPattern(drive))
                    {
                        WriteError(new ErrorRecord(
                            new ArgumentException(
                                string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.InvalidDriveNameFormat, "C", "C:", "C:\\")),
                                "InvalidDriveNameFormat",
                                 ErrorCategory.InvalidArgument,
                                 drive));
                        continue;
                    }

                    // Get the full path for the drive.
                    string drivePath = GetDrivePath(drive);
                    if (ValidDrivePath(drivePath))
                    {
                        EmptyRecycleBin(drivePath);
                    }
                }
            }
            else
            {
                // 2) No drivesList is provided by the user.
                EmptyRecycleBin(null);
            }
        }

        /// <summary>
        /// Returns true if the given drive is 'fixed' and its path exist; otherwise, return false.
        /// </summary>
        /// <param name="drivePath"></param>
        /// <returns></returns>
        private bool ValidDrivePath(string drivePath)
        {
            DriveInfo actualDrive = null;
            if (_availableDrives != null)
            {
                foreach (DriveInfo drive in _availableDrives)
                {
                    if (string.Equals(drive.Name, drivePath, StringComparison.OrdinalIgnoreCase))
                    {
                        actualDrive = drive;
                        break;
                    }
                }
            }

            // The drive was not found.
            if (actualDrive == null)
            {
                WriteError(new ErrorRecord(
                            new System.IO.DriveNotFoundException(
                                string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.DriveNotFound, drivePath, "Get-Volume")),
                                "DriveNotFound",
                                ErrorCategory.InvalidArgument,
                                drivePath));
            }
            else
            {
                if (actualDrive.DriveType == DriveType.Fixed)
                {
                    // The drive path exists, and the drive is 'fixed'.
                    return true;
                }

                WriteError(new ErrorRecord(
                            new ArgumentException(
                                string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.InvalidDriveType, drivePath, "Get-Volume")),
                                "InvalidDriveType",
                                ErrorCategory.InvalidArgument,
                                drivePath));
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given input is of the form c, c:, c:\, C, C: or C:\
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static bool IsValidPattern(string input)
        {
            return Regex.IsMatch(input, @"^[a-z]{1}$|^[a-z]{1}:$|^[a-z]{1}:\\$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns a drive path of the form C:\ for the given drive driveName.
        /// Supports the following inputs: C, C:, C:\
        /// </summary>
        /// <param name="driveName"></param>
        /// <returns></returns>
        private static string GetDrivePath(string driveName)
        {
            string drivePath;
            if (driveName.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                drivePath = driveName;
            }
            else if (driveName.EndsWith(':'))
            {
                drivePath = driveName + "\\";
            }
            else
            {
                drivePath = driveName + ":\\";
            }

            return drivePath;
        }

        /// <summary>
        /// Clear the recyclebin for the given drive name.
        /// If no driveName is provided, it clears the recyclebin for all drives.
        /// </summary>
        /// <param name="drivePath"></param>
        private void EmptyRecycleBin(string drivePath)
        {
            string clearRecycleBinShouldProcessTarget;
            if (drivePath == null)
            {
                clearRecycleBinShouldProcessTarget = string.Format(CultureInfo.InvariantCulture,
                                                                   ClearRecycleBinResources.ClearRecycleBinContent);
            }
            else
            {
                clearRecycleBinShouldProcessTarget = string.Format(CultureInfo.InvariantCulture,
                                                                   ClearRecycleBinResources.ClearRecycleBinContentForDrive,
                                                                   drivePath);
            }

            if (_force || (ShouldProcess(clearRecycleBinShouldProcessTarget, "Clear-RecycleBin")))
            {
                // If driveName is null, then clear the recyclebin for all drives; otherwise, just for the specified driveName.

                string activity = string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.ClearRecycleBinProgressActivity);
                string statusDescription;

                if (drivePath == null)
                {
                    statusDescription = string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.ClearRecycleBinStatusDescriptionForAllDrives);
                }
                else
                {
                    statusDescription = string.Format(CultureInfo.InvariantCulture, ClearRecycleBinResources.ClearRecycleBinStatusDescriptionByDrive, drivePath);
                }

                ProgressRecord progress = new(0, activity, statusDescription);
                progress.PercentComplete = 30;
                progress.RecordType = ProgressRecordType.Processing;
                WriteProgress(progress);

                // no need to check result as a failure is returned only if recycle bin is already empty
                uint result = NativeMethod.SHEmptyRecycleBin(IntPtr.Zero, drivePath,
                                                            NativeMethod.RecycleFlags.SHERB_NOCONFIRMATION |
                                                            NativeMethod.RecycleFlags.SHERB_NOPROGRESSUI |
                                                            NativeMethod.RecycleFlags.SHERB_NOSOUND);
                progress.PercentComplete = 100;
                progress.RecordType = ProgressRecordType.Completed;
                WriteProgress(progress);
            }
        }
    }

    internal static partial class NativeMethod
    {
        // Internal code to SHEmptyRecycleBin
        internal enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        [LibraryImport("Shell32.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "SHEmptyRecycleBinW")]
        internal static partial uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);
    }
}
#endif
