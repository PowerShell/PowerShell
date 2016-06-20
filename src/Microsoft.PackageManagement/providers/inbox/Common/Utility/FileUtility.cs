namespace Microsoft.PackageManagement.Provider.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;

    public static class FileUtility 
    {
        public static string GetTempFileFullPath(string fileExtension)
        {
           
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                return fileExtension;
            }

            string tempFolder = Path.GetTempPath();

            string randomFileName = Path.GetRandomFileName();

            //get rid of the file extension
            randomFileName = Path.GetFileNameWithoutExtension(randomFileName) + fileExtension;

            string file = Path.Combine(tempFolder, randomFileName);

            if (File.Exists(file))
            {
                //try it again if the generated file already exists
                file = GetTempFileFullPath(fileExtension);
            }

            return file;
        }

        public static string MakePackageFileName(bool excludeVersion, string packageName, string version, string fileExtension)
        {
            string fileName = (excludeVersion) ? packageName : (packageName + "." + version);
            return fileName + fileExtension;       
        }

        public static string MakePackageDirectoryName(bool excludeVersion, string destinationPath, string packageName, string version)
        {
            string baseDir = Path.Combine(destinationPath, packageName);

            return (excludeVersion) ? baseDir : (baseDir + "." + version);
        }

        public static void CopyDirectory(string source, string dest, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(source);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Source directory '{0}' does not exist or could not be found.", source));
            }
           
            // Some packages have directories with spaces. However it shows as $20, e.g. Install-Module -name xHyper-VBackup. 
            // It has something like Content\Deployment\Module%20References\..., with that, the PowerShellGet provider won't be able to handle it. 
            // Add the following code to unescape percent-encoding characters
            string newdest = Uri.UnescapeDataString(dest);

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(newdest))
            {
                Directory.CreateDirectory(newdest);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(newdest, file.Name);
                // unescape special characters like %20
                tempPath = Uri.UnescapeDataString(tempPath);

                file.CopyTo(tempPath, true /*overwrite*/);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                DirectoryInfo[] sourceDirs = dir.GetDirectories();

                foreach (DirectoryInfo subdir in sourceDirs)
                {
                    string temppath = Path.Combine(newdest, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath, true);
                }
            }
        }

        public static void DeleteDirectory(string fullPath, bool recursive, bool isThrow)
        {
            DoSafeAction(() => DeleteDirectory(fullPath, recursive), isThrow);
        }

        public static void DeleteFile(string fullPath, bool isThrow)
        {
            DoSafeAction(() => DeleteFile(fullPath), isThrow);
        }

        public static IEnumerable<string> GetFiles(string fullPath, string filter, bool recursive)
        {
            fullPath = PathUtility.EnsureTrailingSlash(fullPath);
            if (String.IsNullOrWhiteSpace(filter))
            {
                filter = "*.*";
            }
            try
            {
                if (!Directory.Exists(fullPath))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateFiles(fullPath, filter, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                // .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public static IEnumerable<string> GetDirectories(string fullPath)
        {
            try
            {
                fullPath = PathUtility.EnsureTrailingSlash(fullPath);
                if (!Directory.Exists(fullPath))
                {
                    return Enumerable.Empty<string>();
                }

                return Directory.EnumerateDirectories(fullPath);                                
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (DirectoryNotFoundException)
            {

            }

            return Enumerable.Empty<string>();
        }

        public static DateTimeOffset GetLastModified(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                return File.GetLastWriteTime(fullPath);             
            }
            return Directory.GetLastWriteTime(fullPath);
        }

        private static void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                //sometimes there are difficulties to delete readonly files
                MakeFileWritable(path);
                File.Delete(path);
            }
            catch (FileNotFoundException)
            {
            }
        }

        private static void MakeFileWritable(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if (attributes.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }

        private static void DeleteDirectory(string fullPath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !Directory.Exists(fullPath))
            {
                return;
            }

            try
            {
                //path = GetFullPath(path);
                Directory.Delete(fullPath, recursive);

                // The directory is not guaranteed to be gone since there could be
                // other open handles. Wait, up to half a second, until the directory is gone.
                for (int i = 0; Directory.Exists(fullPath) && i < 5; ++i)
                {
                    Thread.Sleep(100);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        // [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to log an exception as a warning and move on")]
        private static void DoSafeAction(Action action, bool isThrow)
        {
            try
            {
                Attempt(action);
            }
            catch (Exception e)
            {
                if (isThrow)
                {
                    throw new Exception(e.Message);
                }
            }
        }

        private static void Attempt(Action action, int retries = 5, int delayBeforeRetry = 100)
        {
            while (retries > 0)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }
                Thread.Sleep(delayBeforeRetry);
            }
        }
    }
}
