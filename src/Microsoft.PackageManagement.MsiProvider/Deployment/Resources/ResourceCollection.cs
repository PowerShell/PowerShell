//---------------------------------------------------------------------
// <copyright file="ResourceCollection.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.Resources
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    /// <summary>
    /// Allows reading and editing of resource data in a Win32 PE file.
    /// </summary>
    /// <remarks>
    /// To use this class:<list type="number">
    /// <item>Create a new ResourceCollection</item>
    /// <item>Locate resources for the collection by calling one of the <see cref="ResourceCollection.Find(string)"/> methods</item>
    /// <item>Load data of one or more <see cref="Resource"/>s from a file by calling the <see cref="Load"/> method of the
    /// Resource class, or load them all at once (more efficient) with the <see cref="Load"/> method of the ResourceCollection.</item>
    /// <item>Read and/or edit data of the individual Resource objects using the methods on that class.</item>
    /// <item>Save data of one or more <see cref="Resource"/>s to a file by calling the <see cref="Save"/> method of the
    /// Resource class, or save them all at once (more efficient) with the <see cref="Save"/> method of the ResourceCollection.</item>
    /// </list>
    /// </remarks>
    internal class ResourceCollection : ICollection<Resource>
    {
        private List<Resource> resources;

        /// <summary>
        /// Creates a new, empty ResourceCollection.
        /// </summary>
        public ResourceCollection()
        {
            this.resources = new List<Resource>();
        }

        /// <summary>
        /// Locates all resources in a file, including all resource types and languages.  For each located resource,
        /// a <see cref="Resource"/> instance (or subclass) is added to the collection.
        /// </summary>
        /// <param name="resFile">The file to be searched for resources.</param>
        /// <exception cref="IOException">resources could not be read from the file</exception>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Find(string resFile)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, resFile).Demand();

            this.Clear();

            IntPtr module = NativeMethods.LoadLibraryEx(resFile, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            if (module == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException(String.Format(CultureInfo.InvariantCulture, "Failed to load resource file. Error code: {0}", err));
            }
            try
            {
                if (!NativeMethods.EnumResourceTypes(module, new NativeMethods.EnumResTypesProc(this.EnumResTypes), IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException(String.Format(CultureInfo.InvariantCulture, "Failed to enumerate resources. Error code: {0}", err));
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        /// <summary>
        /// Locates all resources in a file of a given type, including all languages.  For each located resource,
        /// a <see cref="Resource"/> instance (or subclass) is added to the collection.
        /// </summary>
        /// <param name="resFile">The file to be searched for resources.</param>
        /// <param name="type">The type of resource to search for; may be one of the ResourceType constants or a user-defined type.</param>
        /// <exception cref="IOException">resources could not be read from the file</exception>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Find(string resFile, ResourceType type)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, resFile).Demand();

            this.Clear();

            IntPtr module = NativeMethods.LoadLibraryEx(resFile, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                if (!NativeMethods.EnumResourceNames(module, (string) type, new NativeMethods.EnumResNamesProc(this.EnumResNames), IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException(String.Format(CultureInfo.InvariantCulture, "EnumResourceNames error. Error code: {0}", err));
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        /// <summary>
        /// Locates all resources in a file of a given type and language.  For each located resource,
        /// a <see cref="Resource"/> instance (or subclass) is added to the collection.
        /// </summary>
        /// <param name="resFile">The file to be searched for resources.</param>
        /// <param name="type">The type of resource to search for; may be one of the ResourceType constants or a user-defined type.</param>
        /// <param name="name">The name of the resource to search for.</param>
        /// <exception cref="IOException">resources could not be read from the file</exception>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Find(string resFile, ResourceType type, string name)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, resFile).Demand();

            this.Clear();

            IntPtr module = NativeMethods.LoadLibraryEx(resFile, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                if (!NativeMethods.EnumResourceLanguages(module, (string) type, name, new NativeMethods.EnumResLangsProc(this.EnumResLangs), IntPtr.Zero))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException(String.Format(CultureInfo.InvariantCulture, "EnumResourceLanguages error. Error code: {0}", err));
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        private bool EnumResTypes(IntPtr module, IntPtr type, IntPtr param)
        {
            if (!NativeMethods.EnumResourceNames(module, type, new NativeMethods.EnumResNamesProc(EnumResNames), IntPtr.Zero))
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException(String.Format(CultureInfo.InvariantCulture, "EnumResourceNames error! Error code: {0}", err));
            }
            return true;
        }

        private bool EnumResNames(IntPtr module, IntPtr type, IntPtr name, IntPtr param)
        {
            if (!NativeMethods.EnumResourceLanguages(module, type, name, new NativeMethods.EnumResLangsProc(EnumResLangs), IntPtr.Zero))
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException(String.Format(CultureInfo.InvariantCulture, "EnumResourceLanguages error. Error code: {0}", err));
            }
            return true;
        }

        private bool EnumResLangs(IntPtr module, IntPtr type, IntPtr name, ushort langId, IntPtr param)
        {
            Resource res;
            if (((int) type) == ResourceType.Version.IntegerValue)
            {
                res = new VersionResource(ResourceNameToString(name), langId);
            }
            else
            {
                res = new Resource(ResourceNameToString(type), ResourceNameToString(name), langId);
            }

            if (!this.Contains(res))
            {
                this.Add(res);
            }

            return true;
        }

        private static string ResourceNameToString(IntPtr resName)
        {
            if ((resName.ToInt64() >> 16) == 0)
            {
                return "#" + resName.ToString();
            }
            else
            {
                return Marshal.PtrToStringAuto(resName);
            }
        }

        /// <summary>
        /// For all resources in the collection, loads their data from a resource file.
        /// </summary>
        /// <param name="file">The file from which resources are loaded.</param>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Load(string file)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, file).Demand();

            IntPtr module = NativeMethods.LoadLibraryEx(file, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                foreach (Resource res in this)
                {
                    res.Load(module);
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        /// <summary>
        /// For all resources in the collection, saves their data to a resource file.
        /// </summary>
        /// <param name="file">The file to which resources are saved.</param>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Save(string file)
        {
            new FileIOPermission(FileIOPermissionAccess.AllAccess, file).Demand();

            IntPtr updateHandle = IntPtr.Zero;
            try
            {
                updateHandle = NativeMethods.BeginUpdateResource(file, false);
                foreach (Resource res in this)
                {
                    res.Save(updateHandle);
                }
                if (!NativeMethods.EndUpdateResource(updateHandle, false))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException(String.Format(CultureInfo.InvariantCulture, "Failed to save resource. Error {0}", err));
                }
                updateHandle = IntPtr.Zero;
            }
            finally
            {
                if (updateHandle != IntPtr.Zero)
                {
                    NativeMethods.EndUpdateResource(updateHandle, true);
                }
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public Resource this[int index]
        {
            get
            {
                return (Resource) this.resources[index];
            }
            set
            {
                this.resources[index] = value;
            }
        }

        /// <summary>
        /// Adds a new item to the collection.
        /// </summary>
        /// <param name="item">The Resource to add.</param>
        public void Add(Resource item)
        {
            this.resources.Add(item);
        }

        /// <summary>
        /// Removes an item to the collection.
        /// </summary>
        /// <param name="item">The Resource to remove.</param>
        public bool Remove(Resource item)
        {
            return this.resources.Remove(item);
        }

        /// <summary>
        /// Gets the index of an item in the collection.
        /// </summary>
        /// <param name="item">The Resource to search for.</param>
        /// <returns>The index of the item, or -1 if not found.</returns>
        public int IndexOf(Resource item)
        {
            return this.resources.IndexOf(item);
        }

        /// <summary>
        /// Inserts a item into the collection.
        /// </summary>
        /// <param name="index">The insertion index.</param>
        /// <param name="item">The Resource to insert.</param>
        public void Insert(int index, Resource item)
        {
            this.resources.Insert(index, item);
        }

        /// <summary>
        /// Tests if the collection contains an item.
        /// </summary>
        /// <param name="item">The Resource to search for.</param>
        /// <returns>true if the item is found; false otherwise</returns>
        public bool Contains(Resource item)
        {
            return this.resources.Contains(item);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">The array to copy into.</param>
        /// <param name="arrayIndex">The starting index in the destination array.</param>
        public void CopyTo(Resource[] array, int arrayIndex)
        {
            this.resources.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes all resources from the collection.
        /// </summary>
        public void Clear()
        {
            this.resources.Clear();
        }

        /// <summary>
        /// Gets the number of resources in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                return this.resources.Count;
            }
        }

        /// <summary>
        /// Gets an enumerator over all resources in the collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Resource> GetEnumerator()
        {
            return this.resources.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) this.resources).GetEnumerator();
        }

        bool ICollection<Resource>.IsReadOnly
        {
            get
            {
                return false;
            }
        }
    }
}
