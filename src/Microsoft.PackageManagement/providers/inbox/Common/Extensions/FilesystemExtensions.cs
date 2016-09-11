// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace Microsoft.PackageManagement.Provider.Utility {
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    internal static class FilesystemExtensions {
        private static readonly char[] _pathCharacters = "/\\".ToCharArray();
        private static int _counter = Process.GetCurrentProcess().Id << 16;
        public static string OriginalTempFolder;

        static FilesystemExtensions() {
            OriginalTempFolder = OriginalTempFolder ?? Path.GetTempPath();
           // ResetTempFolder();
        }

        public static string TempPath {get; private set;}

        public static int Counter {
            get {
                return ++_counter;
            }
        }

        public static string CounterHex {
            get {
                return Counter.ToString("x8", CultureInfo.CurrentCulture);
            }
        }

        public static bool LooksLikeAFilename(this string text) {
            return text.IndexOfAny(_pathCharacters) > -1;
        }

        public static void TryHardToDelete(this string location)
        {
            if (Directory.Exists(location))
            {
                try
                {
                    Directory.Delete(location, true);
                }
                catch
                {
                    // didn't take, eh?
                }
            }

            if (File.Exists(location))
            {
                try
                {
                    File.Delete(location);
                }
                catch
                {
                    // didn't take, eh?
                }
            }

            // if it is still there, move and mark it.
            if (File.Exists(location) || Directory.Exists(location))
            {
                try
                {
                    // move the file to the tmp file
                    // and tell the OS to remove it next reboot.
                    var tmpFilename = location.GenerateTemporaryFilename() + ".delete_me"; // generates a unique filename but not a file!
                    //MoveFileOverwrite(location, tmpFilename);

                    if (File.Exists(location) || Directory.Exists(location))
                    {
                        // of course, if the tmpFile isn't on the same volume as the location, this doesn't work.
                        // then, last ditch effort, let's rename it in the current directory
                        // and then we can hide it and mark it for cleanup .
                        tmpFilename = Path.Combine(Path.GetDirectoryName(location), "tmp." + CounterHex + "." + Path.GetFileName(location) + ".delete_me");
                        //MoveFileOverwrite(location, tmpFilename);
                        if (File.Exists(tmpFilename) || Directory.Exists(location))
                        {
                            // hide the file for convenience.
                            File.SetAttributes(tmpFilename, File.GetAttributes(tmpFilename) | FileAttributes.Hidden);
                        }
                    }

                    // Now we mark the locked file to be deleted upon next reboot (or until another coapp app gets there)
                  //  MoveFileOverwrite(File.Exists(tmpFilename) ? tmpFilename : location, null);
                }
                catch
                {
                    // really. Hmmm.
                }

                if (File.Exists(location))
                {
                    // err("Unable to forcibly remove file '{0}'. This can't be good.", location);
                }
            }
            return;
        }

        ///// <summary>
        /////     File move abstraction that can be implemented to handle non-windows platforms
        ///// </summary>
        ///// <param name="sourceFile"></param>
        ///// <param name="destinationFile"></param>
        //public static void MoveFileOverwrite(string sourceFile, string destinationFile) {
        //    NativeMethods.MoveFileEx(sourceFile, destinationFile, MoveFileFlags.ReplaceExisting);
        //}

        //public static void MoveFileAtNextBoot(string sourceFile, string destinationFile) {
        //    NativeMethods.MoveFileEx(sourceFile, destinationFile, MoveFileFlags.DelayUntilReboot);
        //}

        public static string GenerateTemporaryFilename(this string filename) {
            string ext = null;
            string name = null;
            string folder = null;

            if (!string.IsNullOrWhiteSpace(filename)) {
                ext = Path.GetExtension(filename);
                name = Path.GetFileNameWithoutExtension(filename);
                folder = Path.GetDirectoryName(filename);
            }

            if (string.IsNullOrWhiteSpace(ext)) {
                ext = ".tmp";
            }
            if (string.IsNullOrWhiteSpace(folder)) {
                folder = TempPath;
            }

            do
            {
                name = Path.Combine(folder, "tmpFile." + Path.GetRandomFileName() + (string.IsNullOrWhiteSpace(name) ? ext : "." + name + ext));
            }
            while (File.Exists(name));

            // return MarkFileTemporary(name);
            return name;
        }

        //public static void ResetTempFolder() {
        //    // set the temporary folder to be a child of the User temporary folder
        //    // based on the application name
        //    var appName = typeof(FilesystemExtensions).GetTypeInfo().Assembly.GetName().Name;
        //    if (OriginalTempFolder.IndexOf(appName, StringComparison.CurrentCultureIgnoreCase) == -1) {
        //        var appTempPath = Path.Combine(OriginalTempFolder, appName);
        //        if (!Directory.Exists(appTempPath)) {
        //            Directory.CreateDirectory(appTempPath);
        //        }

        //        TempPath = Path.Combine(appTempPath, (Directory.EnumerateDirectories(appTempPath).Count() + 1).ToString(CultureInfo.CurrentCulture));
        //        if (!Directory.Exists(TempPath)) {
        //            Directory.CreateDirectory(TempPath);
        //        }

        //        // make sure this temp directory gets marked for eventual cleanup.
        //        MoveFileOverwrite(appTempPath, null);
        //        MoveFileAtNextBoot(TempPath, null);
        //    }

        //    TempPath = TempPath ?? OriginalTempFolder;
        //}

        /// <summary>
        ///     This takes a string that is representative of a filename and tries to create a path that can be considered the
        ///     'canonical' path. path on drives that are mapped as remote shares are rewritten as their \\server\share\path
        /// </summary>
        /// <returns> </returns>
        public static string CanonicalizePath(this string path, bool isPotentiallyRelativePath) {
            Uri pathUri = null;
            try {
                pathUri = new Uri(path);
                if (!pathUri.IsFile) {
                    // perhaps try getting the fullpath
                    try {
                        pathUri = new Uri(Path.GetFullPath(path));
                    } catch {
                        return null;
                    }
                }

                // is this a unc path?
                if (string.IsNullOrWhiteSpace(pathUri.Host)) {
                    // no, this is a drive:\path path
                    // use API to resolve out the drive letter to see if it is a remote
                    var drive = pathUri.Segments[1].Replace('/', '\\'); // the zero segment is always just '/'

                    var sb = new StringBuilder(512);
                    var size = sb.Capacity;

                    //TODO
                   // var error = NativeMethods.WNetGetConnection(drive, sb, ref size);
                    //if (error == 0) {
                    //    if (pathUri.Segments.Length > 2) {
                    //        return pathUri.Segments.Skip(2).Aggregate(sb.ToString().Trim(), (current, item) => current + item);
                    //    }
                   // }
                }
                // not a remote (or resolvable-remote) path or
                // it is already a path that is in it's correct form (via localpath)
                return pathUri.LocalPath;
            } catch (UriFormatException) {
                // we could try to see if it is a relative path...
                if (isPotentiallyRelativePath) {
                    return CanonicalizePath(Path.GetFullPath(path), false);
                }
                return null;
            }
        }

        public static byte[] ReadBytes(this string path, int maxLength) {
            if (path.FileExists()) {
                try {
                    var buffer = new byte[Math.Min(new FileInfo(path).Length, maxLength)];
                    using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        file.Read(buffer, 0, buffer.Length);
                    }

                    return buffer;
                } catch {
                    // not openable. whatever.
                }
            }
            return new byte[0];
        }

        public static bool FileExists(this string path) {
            if (!string.IsNullOrWhiteSpace(path)) {
                try {
                    return File.Exists(CanonicalizePath(path, true));
                } catch {
                }
            }
            return false;
        }

        public static bool DirectoryExists(this string path) {
            if (!string.IsNullOrWhiteSpace(path)) {
                try {
                    return Directory.Exists(CanonicalizePath(path, true));
                } catch {
                }
            }
            return false;
        }

        public static string MakeSafeFileName(this string input) {
            return new Regex(@"-+").Replace(new Regex(@"[^\d\w\[\]_\-\.\ ]").Replace(input, "-"), "-").Replace(" ", "");
        }
    }
}