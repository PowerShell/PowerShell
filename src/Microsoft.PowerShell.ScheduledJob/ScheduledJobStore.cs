// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace Microsoft.PowerShell.ScheduledJob
{
    /// <summary>
    /// This class encapsulates the work of determining the file location where
    /// a job definition will be stored and retrieved and where job runs will
    /// be stored and retrieved.  Scheduled job definitions are stored in a
    /// location based on the current user.  Job runs are stored in the
    /// corresponding scheduled job definition location under an "Output"
    /// directory, where each run will have a subdirectory with a name derived
    /// from the job run date/time.
    ///
    /// File Structure for "JobDefinitionFoo":
    /// $env:User\AppData\Local\Windows\PowerShell\ScheduledJobs\JobDefinitionFoo\
    ///     ScheduledJobDefinition.xml
    ///     Output\
    ///         110321-130942\
    ///             Status.xml
    ///             Results.xml
    ///         110319-173502\
    ///             Status.xml
    ///             Results.xml
    ///         ...
    /// </summary>
    internal class ScheduledJobStore
    {
        #region Public Enums

        public enum JobRunItem
        {
            None = 0,
            Status = 1,
            Results = 2
        }

        #endregion

        #region Public Strings

        public const string ScheduledJobsPath = @"Microsoft\Windows\PowerShell\ScheduledJobs";
        public const string DefinitionFileName = "ScheduledJobDefinition";
        public const string JobRunOutput = "Output";
        public const string ScheduledJobDefExistsFQEID = "ScheduledJobDefExists";

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns FileStream object for existing scheduled job definition.
        /// Definition file is looked for in the default user local appdata path.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <param name="fileMode">File mode.</param>
        /// <param name="fileAccess">File access.</param>
        /// <param name="fileShare">File share.</param>
        /// <returns>FileStream object.</returns>
        public static FileStream GetFileForJobDefinition(
            string definitionName,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            string filePathName = GetFilePathName(definitionName, DefinitionFileName);
            return File.Open(filePathName, fileMode, fileAccess, fileShare);
        }

        /// <summary>
        /// Returns FileStream object for existing scheduled job definition.
        /// Definition file is looked for in the path provided.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <param name="definitionPath">Scheduled job definition file path.</param>
        /// <param name="fileMode">File mode.</param>
        /// <param name="fileAccess">File share.</param>
        /// <param name="fileShare">File share.</param>
        /// <returns></returns>
        public static FileStream GetFileForJobDefinition(
            string definitionName,
            string definitionPath,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            if (string.IsNullOrEmpty(definitionPath))
            {
                throw new PSArgumentException("definitionPath");
            }

            string filePathName = string.Create(CultureInfo.InvariantCulture, $@"{definitionPath}\{definitionName}\{DefinitionFileName}.xml");
            return File.Open(filePathName, fileMode, fileAccess, fileShare);
        }

        /// <summary>
        /// Checks the provided path against the default path of scheduled jobs
        /// for the current user.
        /// </summary>
        /// <param name="definitionPath">Path for scheduled job definitions.</param>
        /// <returns>True if paths are equal.</returns>
        public static bool IsDefaultUserPath(string definitionPath)
        {
            return definitionPath.Equals(GetJobDefinitionLocation(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a FileStream object for a new scheduled job definition name.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <returns>FileStream object.</returns>
        public static FileStream CreateFileForJobDefinition(
            string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            string filePathName = CreateFilePathName(definitionName, DefinitionFileName);
            return File.Create(filePathName);
        }

        /// <summary>
        /// Returns an IEnumerable object of scheduled job definition names in
        /// the job store.
        /// </summary>
        /// <returns>IEnumerable of job definition names.</returns>
        public static IEnumerable<string> GetJobDefinitions()
        {
            // Directory names are identical to the corresponding scheduled job definition names.
            string directoryPath = GetDirectoryPath();
            IEnumerable<string> definitions = Directory.EnumerateDirectories(directoryPath);
            return (definitions != null) ? definitions : new Collection<string>() as IEnumerable<string>;
        }

        /// <summary>
        /// Returns a FileStream object for an existing scheduled job definition
        /// run.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <param name="runStart">DateTime of job run start time.</param>
        /// <param name="runItem">Job run item.</param>
        /// <param name="fileAccess">File access.</param>
        /// <param name="fileMode">File mode.</param>
        /// <param name="fileShare">File share.</param>
        /// <returns>FileStream object.</returns>
        public static FileStream GetFileForJobRunItem(
            string definitionName,
            DateTime runStart,
            JobRunItem runItem,
            FileMode fileMode,
            FileAccess fileAccess,
            FileShare fileShare)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            string filePathName = GetRunFilePathName(definitionName, runItem, runStart);
            return File.Open(filePathName, fileMode, fileAccess, fileShare);
        }

        /// <summary>
        /// Returns a FileStream object for a new scheduled job definition run.
        /// </summary>
        /// <param name="definitionOutputPath">Scheduled job definition path.</param>
        /// <param name="runStart">DateTime of job run start time.</param>
        /// <param name="runItem">Job run item.</param>
        /// <returns>FileStream object.</returns>
        public static FileStream CreateFileForJobRunItem(
            string definitionOutputPath,
            DateTime runStart,
            JobRunItem runItem)
        {
            if (string.IsNullOrEmpty(definitionOutputPath))
            {
                throw new PSArgumentException("definitionOutputPath");
            }

            string filePathName = GetRunFilePathNameFromPath(definitionOutputPath, runItem, runStart);

            // If the file already exists, we overwrite it because the job run
            // can be updated multiple times while the job is running.
            return File.Create(filePathName);
        }

        /// <summary>
        /// Returns a collection of DateTime objects which specify job run directories
        /// that are currently in the store.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <returns>Collection of DateTime objects.</returns>
        public static Collection<DateTime> GetJobRunsForDefinition(
            string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            string definitionOutputPath = GetJobRunOutputDirectory(definitionName);

            return GetJobRunsForDefinitionPath(definitionOutputPath);
        }

        /// <summary>
        /// Returns a collection of DateTime objects which specify job run directories
        /// that are currently in the store.
        /// </summary>
        /// <param name="definitionOutputPath">Scheduled job definition job run Output path.</param>
        /// <returns>Collection of DateTime objects.</returns>
        public static Collection<DateTime> GetJobRunsForDefinitionPath(
            string definitionOutputPath)
        {
            if (string.IsNullOrEmpty(definitionOutputPath))
            {
                throw new PSArgumentException("definitionOutputPath");
            }

            Collection<DateTime> jobRunInfos = new Collection<DateTime>();
            IEnumerable<string> jobRuns = Directory.EnumerateDirectories(definitionOutputPath);
            if (jobRuns != null)
            {
                // Job run directory names are the date/times that the job was started.
                foreach (string jobRun in jobRuns)
                {
                    DateTime jobRunDateTime;
                    int indx = jobRun.LastIndexOf('\\');
                    string jobRunName = (indx != -1) ? jobRun.Substring(indx + 1) : jobRun;
                    if (ConvertJobRunNameToDateTime(jobRunName, out jobRunDateTime))
                    {
                        jobRunInfos.Add(jobRunDateTime);
                    }
                }
            }

            return jobRunInfos;
        }

        /// <summary>
        /// Remove the job definition and all job runs from job store.
        /// </summary>
        /// <param name="definitionName">Scheduled Job Definition name.</param>
        public static void RemoveJobDefinition(
            string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            // Remove job runs, job definition file, and job definition directory.
            string jobDefDirectory = GetJobDefinitionPath(definitionName);
            Directory.Delete(jobDefDirectory, true);
        }

        /// <summary>
        /// Renames the directory containing the old job definition name
        /// to the new name provided.
        /// </summary>
        /// <param name="oldDefName">Existing job definition directory.</param>
        /// <param name="newDefName">Renamed job definition directory.</param>
        public static void RenameScheduledJobDefDir(
            string oldDefName,
            string newDefName)
        {
            if (string.IsNullOrEmpty(oldDefName))
            {
                throw new PSArgumentException("oldDefName");
            }

            if (string.IsNullOrEmpty(newDefName))
            {
                throw new PSArgumentException("newDefName");
            }

            string oldDirPath = GetJobDefinitionPath(oldDefName);
            string newDirPath = GetJobDefinitionPath(newDefName);
            Directory.Move(oldDirPath, newDirPath);
        }

        /// <summary>
        /// Remove a single job definition job run from the job store.
        /// </summary>
        /// <param name="definitionName">Scheduled Job Definition name.</param>
        /// <param name="runStart">DateTime of job run.</param>
        public static void RemoveJobRun(
            string definitionName,
            DateTime runStart)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            // Remove the job run files and directory.
            string runDirectory = GetRunDirectory(definitionName, runStart);
            Directory.Delete(runDirectory, true);
        }

        /// <summary>
        /// Remove a single job definition job run from the job store.
        /// </summary>
        /// <param name="definitionOutputPath">Scheduled Job Definition Output path.</param>
        /// <param name="runStart">DateTime of job run.</param>
        public static void RemoveJobRunFromOutputPath(
            string definitionOutputPath,
            DateTime runStart)
        {
            if (string.IsNullOrEmpty(definitionOutputPath))
            {
                throw new PSArgumentException("definitionOutputPath");
            }

            // Remove the job run files and directory.
            string runDirectory = GetRunDirectoryFromPath(definitionOutputPath, runStart);
            Directory.Delete(runDirectory, true);
        }

        /// <summary>
        /// Remove all job runs for this job definition.
        /// </summary>
        /// <param name="definitionName">Scheduled Job Definition name.</param>
        public static void RemoveAllJobRuns(
            string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            Collection<DateTime> jobRuns = GetJobRunsForDefinition(definitionName);
            foreach (DateTime jobRun in jobRuns)
            {
                string jobRunPath = GetRunDirectory(definitionName, jobRun);
                Directory.Delete(jobRunPath, true);
            }
        }

        /// <summary>
        /// Set read access on provided definition file for specified user.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="user">Account user name.</param>
        public static void SetReadAccessOnDefinitionFile(
            string definitionName,
            string user)
        {
            string filePath = GetFilePathName(definitionName, DefinitionFileName);

            // Get file security for existing file.
            FileSecurity fileSecurity = new FileSecurity(
                filePath,
                AccessControlSections.Access);

            // Create rule.
            FileSystemAccessRule fileAccessRule = new FileSystemAccessRule(
                user,
                FileSystemRights.Read,
                AccessControlType.Allow);
            fileSecurity.AddAccessRule(fileAccessRule);

            // Apply rule.
            File.SetAccessControl(filePath, fileSecurity);
        }

        /// <summary>
        /// Set write access on Output directory for provided definition for
        /// specified user.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="user">Account user name.</param>
        public static void SetWriteAccessOnJobRunOutput(
            string definitionName,
            string user)
        {
            string outputDirectoryPath = GetJobRunOutputDirectory(definitionName);
            AddFullAccessToDirectory(user, outputDirectoryPath);
        }

        /// <summary>
        /// Returns the directory path for job run output for the specified
        /// scheduled job definition.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <returns>Directory Path.</returns>
        public static string GetJobRunOutputDirectory(
            string definitionName)
        {
            if (string.IsNullOrEmpty(definitionName))
            {
                throw new PSArgumentException("definitionName");
            }

            return Path.Combine(GetJobDefinitionPath(definitionName), JobRunOutput);
        }

        /// <summary>
        /// Gets the directory path for a Scheduled Job Definition.
        /// </summary>
        /// <returns>Directory Path.</returns>
        public static string GetJobDefinitionLocation()
        {
#if UNIX
            return Path.Combine(Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE), "ScheduledJobs"));
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ScheduledJobsPath);
#endif
        }

        public static void CreateDirectoryIfNotExists()
        {
            GetDirectoryPath();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the directory path for Scheduled Jobs.  Will create the directory if
        /// it does not exist.
        /// </summary>
        /// <returns>Directory Path.</returns>
        private static string GetDirectoryPath()
        {
            string pathName;
#if UNIX
            pathName = Path.Combine(Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE), "ScheduledJobs"));
#else
            pathName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ScheduledJobsPath);
#endif
            if (!Directory.Exists(pathName))
            {
                Directory.CreateDirectory(pathName);
            }

            return pathName;
        }

        /// <summary>
        /// Creates a ScheduledJob definition directory with provided definition name
        /// along with a job run Output directory, and returns a file path/name.
        ///  ...\ScheduledJobs\definitionName\fileName.xml
        ///  ...\ScheduledJobs\definitionName\Output\
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>File path/name.</returns>
        private static string CreateFilePathName(string definitionName, string fileName)
        {
            string filePath = GetJobDefinitionPath(definitionName);
            string outputPath = GetJobRunOutputDirectory(definitionName);
            if (Directory.Exists(filePath))
            {
                ScheduledJobException ex = new ScheduledJobException(StringUtil.Format(ScheduledJobErrorStrings.JobDefFileAlreadyExists, definitionName));
                ex.FQEID = ScheduledJobDefExistsFQEID;
                throw ex;
            }

            Directory.CreateDirectory(filePath);
            Directory.CreateDirectory(outputPath);
            return string.Create(CultureInfo.InstalledUICulture, $@"{filePath}\{fileName}.xml");
        }

        /// <summary>
        /// Returns a file path/name for an existing Scheduled job definition directory.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>File path/name.</returns>
        private static string GetFilePathName(string definitionName, string fileName)
        {
            string filePath = GetJobDefinitionPath(definitionName);
            return string.Create(CultureInfo.InvariantCulture, $@"{filePath}\{fileName}.xml");
        }

        /// <summary>
        /// Gets the directory path for a Scheduled Job Definition.
        /// </summary>
        /// <param name="definitionName">Scheduled job definition name.</param>
        /// <returns>Directory Path.</returns>
        private static string GetJobDefinitionPath(string definitionName)
        {
#if UNIX
            return Path.Combine(Platform.SelectProductNameForDirectory(Platform.XDG_Type.CACHE), "ScheduledJobs", definitionName);
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                ScheduledJobsPath,
                                definitionName);
#endif
        }

        /// <summary>
        /// Returns a directory path for an existing ScheduledJob run result directory.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="runStart">File name.</param>
        /// <returns>Directory Path.</returns>
        private static string GetRunDirectory(
            string definitionName,
            DateTime runStart)
        {
            string directoryPath = GetJobRunOutputDirectory(definitionName);
            return string.reate(CultureInfo.InvariantCulture, $@"{directoryPath}\{ConvertDateTimeToJobRunName(runStart)}");
        }

        /// <summary>
        /// Returns a directory path for an existing ScheduledJob run based on
        /// provided definition Output directory path.
        /// </summary>
        /// <param name="definitionOutputPath">Output directory path.</param>
        /// <param name="runStart">File name.</param>
        /// <returns>Directory Path.</returns>
        private static string GetRunDirectoryFromPath(
            string definitionOutputPath,
            DateTime runStart)
        {
            return string.Create(CultureInfo.InvariantCulture, $@"{definitionOutputPath}\{ConvertDateTimeToJobRunName(runStart)}");
        }

        /// <summary>
        /// Returns a file path/name for a run result file.  Will create the
        /// job run directory if it does not exist.
        /// </summary>
        /// <param name="definitionName">Definition name.</param>
        /// <param name="runItem">Result type.</param>
        /// <param name="runStart">Run date.</param>
        /// <returns>File path/name.</returns>
        private static string GetRunFilePathName(
            string definitionName,
            JobRunItem runItem,
            DateTime runStart)
        {
            string directoryPath = GetJobRunOutputDirectory(definitionName);
            string jobRunPath = string.Create(CultureInfo.InvariantCulture, $@"{directoryPath}\{ConvertDateTimeToJobRunName(runStart)}");

            return string.Create(CultureInfo.InvariantCulture, $@"{jobRunPath}\{runItem.ToString()}.xml");
        }

        /// <summary>
        /// Returns a file path/name for a job run result, based on the passed in
        /// job run output path.  Will create the job run directory if it does not
        /// exist.
        /// </summary>
        /// <param name="outputPath">Definition job run output path.</param>
        /// <param name="runItem">Result type.</param>
        /// <param name="runStart">Run date.</param>
        /// <returns></returns>
        private static string GetRunFilePathNameFromPath(
            string outputPath,
            JobRunItem runItem,
            DateTime runStart)
        {
            string jobRunPath = string.Create(CultureInfo.InvariantCulture, $@"{outputPath}\{ConvertDateTimeToJobRunName(runStart)}");

            if (!Directory.Exists(jobRunPath))
            {
                // Create directory for this job run date.
                Directory.CreateDirectory(jobRunPath);
            }

            return string.Create(CultureInfo.InvariantCulture, $@"{jobRunPath}\{runItem.ToString()}.xml");
        }

        private static void AddFullAccessToDirectory(
            string user,
            string directoryPath)
        {
            // Create rule.
            DirectoryInfo info = new DirectoryInfo(directoryPath);
            DirectorySecurity dSecurity = info.GetAccessControl();
            FileSystemAccessRule fileAccessRule = new FileSystemAccessRule(
                user,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            // Apply rule.
            dSecurity.AddAccessRule(fileAccessRule);
            info.SetAccessControl(dSecurity);
        }

        //
        // String format: 'YYYYMMDD-HHMMSS-SSS'
        // ,where SSS is milliseconds.
        //

        private static string ConvertDateTimeToJobRunName(DateTime dt)
        {
            return string.Format(CultureInfo.InvariantCulture,
                @"{0:d4}{1:d2}{2:d2}-{3:d2}{4:d2}{5:d2}-{6:d3}",
                dt.Year, dt.Month, dt.Day,
                dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }

        /// <summary>
        /// Converts a jobRun name string to an equivalent DateTime.
        /// </summary>
        /// <param name="jobRunName"></param>
        /// <param name="jobRun"></param>
        /// <returns></returns>
        internal static bool ConvertJobRunNameToDateTime(string jobRunName, out DateTime jobRun)
        {
            if (jobRunName == null || jobRunName.Length != 19)
            {
                jobRun = new DateTime();
                return false;
            }

            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;
            int second = 0;
            int msecs = 0;
            bool success = true;

            try
            {
                year = Convert.ToInt32(jobRunName.Substring(0, 4));
                month = Convert.ToInt32(jobRunName.Substring(4, 2));
                day = Convert.ToInt32(jobRunName.Substring(6, 2));
                hour = Convert.ToInt32(jobRunName.Substring(9, 2));
                minute = Convert.ToInt32(jobRunName.Substring(11, 2));
                second = Convert.ToInt32(jobRunName.Substring(13, 2));
                msecs = Convert.ToInt32(jobRunName.Substring(16, 3));
            }
            catch (FormatException)
            {
                success = false;
            }
            catch (OverflowException)
            {
                success = false;
            }

            if (success)
            {
                jobRun = new DateTime(year, month, day, hour, minute, second, msecs);
            }
            else
            {
                jobRun = new DateTime();
            }

            return success;
        }

        #endregion
    }
}
