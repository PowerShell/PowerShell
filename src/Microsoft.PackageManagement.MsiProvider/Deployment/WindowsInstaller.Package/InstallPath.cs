//---------------------------------------------------------------------
// <copyright file="InstallPath.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Package
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Represents the installation path of a file or directory from an installer product database.
    /// </summary>
    internal class InstallPath
    {
        /// <summary>
        /// Creates a new InstallPath, specifying a filename.
        /// </summary>
        /// <param name="name">The name of the file or directory.  Not a full path.</param>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallPath(string name) : this(name, false) { }

        /// <summary>
        /// Creates a new InstallPath, parsing out either the short or long filename.
        /// </summary>
        /// <param name="name">The name of the file or directory, in short|long syntax for a filename
        /// or targetshort|targetlong:sourceshort|sourcelong syntax for a directory.</param>
        /// <param name="useShortNames">true to parse the short part of the combined filename; false to parse the long part</param>
        public InstallPath(string name, bool useShortNames)
        {
            if(name == null)
            {
                throw new ArgumentNullException("name");
            }
            this.parentPath = null;
            ParseName(name, useShortNames);
        }

        private void ParseName(string name, bool useShortNames)
        {
            string[] parse = name.Split(new char[] { ':' }, 3);
            if(parse.Length == 3)
            {
                // Syntax was targetshort:sourceshort|targetlong:sourcelong.
                // Change it to targetshort|targetlong:sourceshort|sourcelong.
                parse = name.Split(new char[] { ':', '|' }, 4);
                if(parse.Length == 4)
                    parse = new string[] { parse[0] + '|' + parse[2], parse[1] + '|' + parse[3] };
                else
                    parse = new string[] { parse[0] + '|' + parse[1], parse[1] + '|' + parse[2] };
            }
            string targetName = parse[0];
            string sourceName = (parse.Length == 2 ? parse[1] : parse[0]);
            parse = targetName.Split(new char[] { '|' }, 2);
            if(parse.Length == 2) targetName = (useShortNames ? parse[0] : parse[1]);
            parse = sourceName.Split(new char[] { '|' }, 2);
            if(parse.Length == 2) sourceName = (useShortNames ? parse[0] : parse[1]);

            this.SourceName = sourceName;
            this.TargetName = targetName;
        }

        /// <summary>
        /// Gets the path of the parent directory.
        /// </summary>
        public InstallPath ParentPath
        {
            get
            {
                return parentPath;
            }
        }
        internal void SetParentPath(InstallPath value)
        {
            parentPath = value;
            ResetSourcePath();
            ResetTargetPath();
        }
        private InstallPath parentPath;

        /// <summary>
        /// Gets the set of child paths if this InstallPath object represents a a directory.
        /// </summary>
        public InstallPathCollection ChildPaths
        {
            get
            {
                if(childPaths == null)
                {
                    childPaths = new InstallPathCollection(this);
                }
                return childPaths;
            }
        }
        private InstallPathCollection childPaths;

        /// <summary>
        /// Gets or sets the source name of the InstallPath.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string SourceName
        {
            get
            {
                return sourceName;
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("value");
                }
                sourceName = value;
                ResetSourcePath();
            }
        }
        private string sourceName;

        /// <summary>
        /// Gets or sets the target name of the install path.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string TargetName
        {
            get
            {
                return targetName;
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("value");
                }
                targetName = value;
                ResetTargetPath();
            }
        }
        private string targetName;

        /// <summary>
        /// Gets the full source path.
        /// </summary>
        public string SourcePath
        {
            get
            {
                if(sourcePath == null)
                {
                    if(parentPath != null)
                    {
                        sourcePath = (sourceName.Equals(".") ? parentPath.SourcePath
                                      : Path.Combine(parentPath.SourcePath, sourceName));
                    }
                    else
                    {
                        sourcePath = sourceName;
                    }
                }
                return sourcePath;
            }
            set
            {
                ResetSourcePath();
                sourcePath = value;
            }
        }
        private string sourcePath;

        /// <summary>
        /// Gets the full target path.
        /// </summary>
        public string TargetPath
        {
            get
            {
                if(targetPath == null)
                {
                    if(parentPath != null)
                    {
                        targetPath = (targetName.Equals(".") ? parentPath.TargetPath
                                      : Path.Combine(parentPath.TargetPath, targetName));
                    }
                    else
                    {
                        targetPath = targetName;
                    }
                }
                return targetPath;
            }
            set
            {
                ResetTargetPath();
                targetPath = value;
            }
        }
        private string targetPath;

        private void ResetSourcePath()
        {
            if(sourcePath != null)
            {
                sourcePath = null;
                if(childPaths != null)
                {
                    foreach(InstallPath ip in childPaths)
                    {
                        ip.ResetSourcePath();
                    }
                }
            }
        }

        private void ResetTargetPath()
        {
            if(targetPath != null)
            {
                targetPath = null;
                if(childPaths != null)
                {
                    foreach(InstallPath ip in childPaths)
                    {
                        ip.ResetTargetPath();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the full source path.
        /// </summary>
        /// <returns><see cref="SourcePath"/></returns>
        public override String ToString()
        {
            return SourcePath;
        }
    }

    /// <summary>
    /// Represents a collection of InstallPaths that are the child paths of the same parent directory.
    /// </summary>
    internal class InstallPathCollection : IList<InstallPath>
    {
        private InstallPath parentPath;
        private List<InstallPath> items;

        internal InstallPathCollection(InstallPath parentPath)
        {
            this.parentPath = parentPath;
            this.items = new List<InstallPath>();
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public InstallPath this[int index]
        {
            get
            {
                return this.items[index];
            }
            set
            {
                this.OnSet(this.items[index], value);
                this.items[index] = value;
            }
        }

        /// <summary>
        /// Adds a new child path to the collection.
        /// </summary>
        /// <param name="item">The InstallPath to add.</param>
        public void Add(InstallPath item)
        {
            this.OnInsert(item);
            this.items.Add(item);
        }

        /// <summary>
        /// Removes a child path to the collection.
        /// </summary>
        /// <param name="item">The InstallPath to remove.</param>
        public bool Remove(InstallPath item)
        {
            int index = this.items.IndexOf(item);
            if (index >= 0)
            {
                this.OnRemove(item);
                this.items.RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the index of a child path in the collection.
        /// </summary>
        /// <param name="item">The InstallPath to search for.</param>
        /// <returns>The index of the item, or -1 if not found.</returns>
        public int IndexOf(InstallPath item)
        {
            return this.items.IndexOf(item);
        }

        /// <summary>
        /// Inserts a child path into the collection.
        /// </summary>
        /// <param name="index">The insertion index.</param>
        /// <param name="item">The InstallPath to insert.</param>
        public void Insert(int index, InstallPath item)
        {
            this.OnInsert(item);
            this.items.Insert(index, item);
        }

        /// <summary>
        /// Tests if the collection contains a child path.
        /// </summary>
        /// <param name="item">The InstallPath to search for.</param>
        /// <returns>true if the item is found; false otherwise</returns>
        public bool Contains(InstallPath item)
        {
            return this.items.Contains(item);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">The array to copy into.</param>
        /// <param name="index">The starting index in the destination array.</param>
        public void CopyTo(InstallPath[] array, int index)
        {
            this.items.CopyTo(array, index);
        }

        private void OnInsert(InstallPath item)
        {
            if (item.ParentPath != null)
            {
                item.ParentPath.ChildPaths.Remove(item);
            }

            item.SetParentPath(this.parentPath);
        }

        private void OnRemove(InstallPath item)
        {
            item.SetParentPath(null);
        }

        private void OnSet(InstallPath oldItem, InstallPath newItem)
        {
            this.OnRemove(oldItem);
            this.OnInsert(newItem);
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            this.OnRemove(this[index]);
            this.items.RemoveAt(index);
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            foreach (InstallPath item in this)
            {
                this.OnRemove(item);
            }

            this.items.Clear();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        bool ICollection<InstallPath>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an enumerator over all items in the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<InstallPath> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<InstallPath>) this).GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a mapping of install paths for all directories, components, or files in
    /// an installation database.
    /// </summary>
    internal class InstallPathMap : IDictionary<string, InstallPath>
    {
        /// <summary>
        /// Builds a mapping from File keys to installation paths.
        /// </summary>
        /// <param name="db">Installation database.</param>
        /// <param name="componentPathMap">Component mapping returned by <see cref="BuildComponentPathMap"/>.</param>
        /// <param name="useShortNames">true to use short file names; false to use long names</param>
        /// <returns>An InstallPathMap with the described mapping.</returns>
        public static InstallPathMap BuildFilePathMap(Database db, InstallPathMap componentPathMap,
            bool useShortNames)
        {
            if(db == null)
            {
                throw new ArgumentNullException("db");
            }

            if(componentPathMap == null)
            {
                componentPathMap = BuildComponentPathMap(db, BuildDirectoryPathMap(db, useShortNames));
            }

            InstallPathMap filePathMap = new InstallPathMap();

            using (View fileView = db.OpenView("SELECT `File`, `Component_`, `FileName` FROM `File`"))
            {
                fileView.Execute();

                foreach (Record fileRec in fileView)
                {
                    using (fileRec)
                    {
                        string file = (string) fileRec[1];
                        string comp = (string) fileRec[2];
                        string fileName = (string) fileRec[3];

                        InstallPath compPath = (InstallPath) componentPathMap[comp];
                        if(compPath != null)
                        {
                            InstallPath filePath = new InstallPath(fileName, useShortNames);
                            compPath.ChildPaths.Add(filePath);
                            filePathMap[file] = filePath;
                        }
                    }
                }
            }

            return filePathMap;
        }

        /// <summary>
        /// Builds a mapping from Component keys to installation paths.
        /// </summary>
        /// <param name="db">Installation database.</param>
        /// <param name="directoryPathMap">Directory mapping returned by
        /// <see cref="BuildDirectoryPathMap(Database,bool)"/>.</param>
        /// <returns>An InstallPathMap with the described mapping.</returns>
        public static InstallPathMap BuildComponentPathMap(Database db, InstallPathMap directoryPathMap)
        {
            if(db == null)
            {
                throw new ArgumentNullException("db");
            }

            InstallPathMap compPathMap = new InstallPathMap();

            using (View compView = db.OpenView("SELECT `Component`, `Directory_` FROM `Component`"))
            {
                compView.Execute();

                foreach (Record compRec in compView)
                {
                    using (compRec)
                    {
                        string comp = (string) compRec[1];
                        InstallPath dirPath = (InstallPath) directoryPathMap[(string) compRec[2]];

                        if (dirPath != null)
                        {
                            compPathMap[comp] = dirPath;
                        }
                    }
                }
            }

            return compPathMap;
        }

        /// <summary>
        /// Builds a mapping from Directory keys to installation paths.
        /// </summary>
        /// <param name="db">Installation database.</param>
        /// <param name="useShortNames">true to use short directory names; false to use long names</param>
        /// <returns>An InstallPathMap with the described mapping.</returns>
        public static InstallPathMap BuildDirectoryPathMap(Database db, bool useShortNames)
        {
            return BuildDirectoryPathMap(db, useShortNames, null, null);
        }

        /// <summary>
        /// Builds a mapping of Directory keys to directory paths, specifying root directories
        /// for the source and target paths.
        /// </summary>
        /// <param name="db">Database containing the Directory table.</param>
        /// <param name="useShortNames">true to use short directory names; false to use long names</param>
        /// <param name="sourceRootDir">The root directory path of all source paths, or null to leave them relative.</param>
        /// <param name="targetRootDir">The root directory path of all source paths, or null to leave them relative.</param>
        /// <returns>An InstallPathMap with the described mapping.</returns>
        public static InstallPathMap BuildDirectoryPathMap(Database db, bool useShortNames,
            string sourceRootDir, string targetRootDir)
        {
            if(db == null)
            {
                throw new ArgumentNullException("db");
            }

            if(sourceRootDir == null) sourceRootDir = "";
            if(targetRootDir == null) targetRootDir = "";

            InstallPathMap dirMap = new InstallPathMap();
            IDictionary dirTreeMap = new Hashtable();

            using (View dirView = db.OpenView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
            {
                dirView.Execute();

                foreach (Record dirRec in dirView) using (dirRec)
                {
                    string key = (string) dirRec[1];
                    string parentKey = (string) dirRec[2];
                    InstallPath dir = new InstallPath((string) dirRec[3], useShortNames);

                    dirMap[key] = dir;

                    InstallPathMap siblingDirs = (InstallPathMap) dirTreeMap[parentKey];
                    if (siblingDirs == null)
                    {
                        siblingDirs = new InstallPathMap();
                        dirTreeMap[parentKey] = siblingDirs;
                    }
                    siblingDirs.Add(key, dir);
                }
            }

            foreach (KeyValuePair<string, InstallPath> entry in (InstallPathMap) dirTreeMap[""])
            {
                string key = (string) entry.Key;
                InstallPath dir = (InstallPath) entry.Value;
                LinkSubdirectories(key, dir, dirTreeMap);
            }

            InstallPath targetDirPath = (InstallPath) dirMap["TARGETDIR"];
            if(targetDirPath != null)
            {
                targetDirPath.SourcePath = sourceRootDir;
                targetDirPath.TargetPath = targetRootDir;
            }

            return dirMap;
        }

        private static void LinkSubdirectories(string key, InstallPath dir, IDictionary dirTreeMap)
        {
            InstallPathMap subDirs = (InstallPathMap) dirTreeMap[key];
            if(subDirs != null)
            {
                foreach (KeyValuePair<string, InstallPath> entry in subDirs)
                {
                    string subKey = (string) entry.Key;
                    InstallPath subDir = (InstallPath) entry.Value;
                    dir.ChildPaths.Add(subDir);
                    LinkSubdirectories(subKey, subDir, dirTreeMap);
                }
            }
        }

        private Dictionary<string, InstallPath> items;

        /// <summary>
        /// Creates a new empty InstallPathMap.
        /// </summary>
        public InstallPathMap()
        {
            this.items = new Dictionary<string,InstallPath>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a mapping from keys to source paths.
        /// </summary>
        public IDictionary<string, string> SourcePaths
        {
            get
            {
                return new SourcePathMap(this);
            }
        }

        /// <summary>
        /// Gets a mapping from keys to target paths.
        /// </summary>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IDictionary<string, string> TargetPaths
        {
            get
            {
                return new TargetPathMap(this);
            }
        }

        /// <summary>
        /// Gets or sets an install path for a directory, component, or file key.
        /// </summary>
        /// <param name="key">Depending on the type of InstallPathMap, this is the primary key from the
        /// either the Directory, Component, or File table.</param>
        /// <remarks>
        /// Changing an install path does not modify the Database used to generate this InstallPathMap.
        /// </remarks>
        public InstallPath this[string key]
        {
            get
            {
                InstallPath value = null;
                this.items.TryGetValue(key, out value);
                return value;
            }
            set
            {
                this.items[key] = value;
            }
        }

        /// <summary>
        /// Gets the collection of keys in the InstallPathMap. Depending on the type of InstallPathMap,
        /// they are all directory, component, or file key strings.
        /// </summary>
        public ICollection<string> Keys
        {
            get
            {
                return this.items.Keys;
            }
        }

        /// <summary>
        /// Gets the collection of InstallPath values in the InstallPathMap.
        /// </summary>
        public ICollection<InstallPath> Values
        {
            get
            {
                return this.items.Values;
            }
        }

        /// <summary>
        /// Sets an install path for a directory, component, or file key.
        /// </summary>
        /// <param name="key">Depending on the type of InstallPathMap, this is the primary key from the
        /// either the Directory, Component, or File table.</param>
        /// <param name="installPath">The install path of the key item.</param>
        /// <remarks>
        /// Changing an install path does not modify the Database used to generate this InstallPathMap.
        /// </remarks>
        public void Add(string key, InstallPath installPath)
        {
            this.items.Add(key, installPath);
        }

        /// <summary>
        /// Removes an install path from the map.
        /// </summary>
        /// <param name="key">Depending on the type of InstallPathMap, this is the primary key from the
        /// either the Directory, Component, or File table.</param>
        /// <returns>true if the item was removed, false if it did not exist</returns>
        /// <remarks>
        /// Changing an install path does not modify the Database used to generate this InstallPathMap.
        /// </remarks>
        public bool Remove(string key)
        {
            return this.items.Remove(key);
        }

        /// <summary>
        /// Tests whether a directory, component, or file key exists in the map.
        /// </summary>
        /// <param name="key">Depending on the type of InstallPathMap, this is the primary key from the
        /// either the Directory, Component, or File table.</param>
        /// <returns>true if the key is found; false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return this.items.ContainsKey(key);
        }

        /*
        public override string ToString()
        {
            System.Text.StringBuilder buf = new System.Text.StringBuilder();
            foreach(KeyValuePair<string, InstallPath> entry in this)
            {
                buf.AppendFormat("{0}={1}", entry.Key, entry.Value);
                buf.Append("\n");
            }
            return buf.ToString();
        }
        */

        /// <summary>
        /// Attempts to get a value from the dictionary.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">Receives the value, or null if they key was not found.</param>
        /// <returns>True if the value was found, else false.</returns>
        public bool TryGetValue(string key, out InstallPath value)
        {
            return this.items.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<string, InstallPath>>.Add(KeyValuePair<string, InstallPath> item)
        {
            ((ICollection<KeyValuePair<string, InstallPath>>) this.items).Add(item);
        }

        /// <summary>
        /// Removes all entries from the dictionary.
        /// </summary>
        public void Clear()
        {
            this.items.Clear();
        }

        bool ICollection<KeyValuePair<string, InstallPath>>.Contains(KeyValuePair<string, InstallPath> item)
        {
            return ((ICollection<KeyValuePair<string, InstallPath>>) this.items).Contains(item);
        }

        void ICollection<KeyValuePair<string, InstallPath>>.CopyTo(KeyValuePair<string, InstallPath>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, InstallPath>>) this.items).CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of entries in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }

        bool ICollection<KeyValuePair<string, InstallPath>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<string, InstallPath>>.Remove(KeyValuePair<string, InstallPath> item)
        {
            return ((ICollection<KeyValuePair<string, InstallPath>>) this.items).Remove(item);
        }

        /// <summary>
        /// Gets an enumerator over all entries in the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        public IEnumerator<KeyValuePair<string, InstallPath>> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }

    internal class SourcePathMap : IDictionary<string, string>
    {
        private const string RO_MSG =
            "The SourcePathMap collection is read-only. " +
            "Modify the InstallPathMap instead.";

        private InstallPathMap map;

        internal SourcePathMap(InstallPathMap map)
        {
            this.map = map;
        }

        public void Add(string key, string value)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool ContainsKey(string key)
        {
            return this.map.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get
            {
                return this.map.Keys;
            }
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool TryGetValue(string key, out string value)
        {
            InstallPath installPath;
            if (this.map.TryGetValue(key, out installPath))
            {
                value = installPath.SourcePath;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                List<string> values = new List<string>(this.Count);
                foreach (KeyValuePair<string, InstallPath> entry in this.map)
                {
                    values.Add(entry.Value.SourcePath);
                }
                return values;
            }
        }

        public string this[string key]
        {
            get
            {
                string value = null;
                this.TryGetValue(key, out value);
                return value;
            }
            set
            {
                throw new InvalidOperationException(RO_MSG);
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public void Clear()
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            string value = this[item.Key];
            return value == item.Value;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<string, string> entry in this)
            {
                array[arrayIndex] = entry;
                arrayIndex++;
            }
        }

        public int Count
        {
            get
            {
                return this.map.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string, InstallPath> entry in this.map)
            {
                yield return new KeyValuePair<string, string>(
                    entry.Key, entry.Value.SourcePath);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class TargetPathMap : IDictionary<string, string>
    {
        private const string RO_MSG =
            "The TargetPathMap collection is read-only. " +
            "Modify the InstallPathMap instead.";

        private InstallPathMap map;

        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal TargetPathMap(InstallPathMap map)
        {
            this.map = map;
        }

        public void Add(string key, string value)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool ContainsKey(string key)
        {
            return this.map.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get
            {
                return this.map.Keys;
            }
        }

        public bool Remove(string key)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool TryGetValue(string key, out string value)
        {
            InstallPath installPath;
            if (this.map.TryGetValue(key, out installPath))
            {
                value = installPath.TargetPath;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                List<string> values = new List<string>(this.Count);
                foreach (KeyValuePair<string, InstallPath> entry in this.map)
                {
                    values.Add(entry.Value.TargetPath);
                }
                return values;
            }
        }

        public string this[string key]
        {
            get
            {
                string value = null;
                this.TryGetValue(key, out value);
                return value;
            }
            set
            {
                throw new InvalidOperationException(RO_MSG);
            }
        }

        public void Add(KeyValuePair<string, string> item)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public void Clear()
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            string value = this[item.Key];
            return value == item.Value;
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<string, string> entry in this)
            {
                array[arrayIndex] = entry;
                arrayIndex++;
            }
        }

        public int Count
        {
            get
            {
                return this.map.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            throw new InvalidOperationException(RO_MSG);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string, InstallPath> entry in this.map)
            {
                yield return new KeyValuePair<string, string>(
                    entry.Key, entry.Value.TargetPath);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
