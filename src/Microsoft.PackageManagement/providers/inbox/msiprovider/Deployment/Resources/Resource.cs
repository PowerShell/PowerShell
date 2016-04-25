//---------------------------------------------------------------------
// <copyright file="Resource.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.Resources
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    /// <summary>
    /// Represents a Win32 resource which can be loaded from and saved to a PE file.
    /// </summary>
    internal class Resource
    {
        private ResourceType type;
        private string name;
        private int locale;
        private byte[] data;

        /// <summary>
        /// Creates a new Resource object without any data. The data can be later loaded from a file.
        /// </summary>
        /// <param name="type">Type of the resource; may be one of the ResourceType constants or a user-defined type.</param>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        public Resource(ResourceType type, string name, int locale)
            : this(type, name, locale, null)
        {
        }

        /// <summary>
        /// Creates a new Resource object with data. The data can be later saved to a file.
        /// </summary>
        /// <param name="type">Type of the resource; may be one of the ResourceType constants or a user-defined type.</param>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        /// <param name="data">Raw resource data</param>
        public Resource(ResourceType type, string name, int locale, byte[] data)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            this.type = type;
            this.name = name;
            this.locale = locale;
            this.data = data;
        }

        /// <summary>
        /// Gets or sets the type of the resource.  This may be one of the ResourceType constants
        /// or a user-defined type name.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ResourceType ResourceType
        {
            get { return this.type; }
            set { this.type = value; }
        }

        /// <summary>
        /// Gets or sets the name of the resource.  For a numeric resource identifier, the decimal number is prefixed with a "#".
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the locale of the resource.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public int Locale
        {
            get { return this.locale; }
            set { this.locale = value; }
        }

        /// <summary>
        /// Gets or sets the raw data of the resource.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public virtual byte[] Data
        {
            get { return this.data; }
            set { this.data = value; }
        }

        /// <summary>
        /// Loads the resource data from a file.  The file is searched for a resource with matching type, name, and locale.
        /// </summary>
        /// <param name="file">Win32 PE file containing the resource</param>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Load(string file)
        {
            new FileIOPermission(FileIOPermissionAccess.Read, file).Demand();

            IntPtr module = NativeMethods.LoadLibraryEx(file, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                this.Load(module);
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal void Load(IntPtr module)
        {
            IntPtr resourceInfo = NativeMethods.FindResourceEx(module, (string) this.ResourceType, this.Name, (ushort) this.Locale);
            if (resourceInfo != IntPtr.Zero)
            {
                uint resourceLength = NativeMethods.SizeofResource(module, resourceInfo);
                IntPtr resourceData = NativeMethods.LoadResource(module, resourceInfo);
                IntPtr resourcePtr = NativeMethods.LockResource(resourceData);
                byte[] resourceBytes = new byte[resourceLength];
                Marshal.Copy(resourcePtr, resourceBytes, 0, resourceBytes.Length);
                this.Data = resourceBytes;
            }
            else
            {
                this.Data = null;
            }
        }

        /// <summary>
        /// Saves the resource to a file.  Any existing resource data with matching type, name, and locale is overwritten.
        /// </summary>
        /// <param name="file">Win32 PE file to contain the resource</param>
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Save(string file)
        {
            new FileIOPermission(FileIOPermissionAccess.AllAccess, file).Demand();

            IntPtr updateHandle = IntPtr.Zero;
            try
            {
                updateHandle = NativeMethods.BeginUpdateResource(file, false);
                this.Save(updateHandle);
                if (!NativeMethods.EndUpdateResource(updateHandle, false))
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new IOException(String.Format(CultureInfo.InvariantCulture, "Failed to save resource. Error code: {0}", err));
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

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal void Save(IntPtr updateHandle)
        {
            IntPtr dataPtr = IntPtr.Zero;
            try
            {
                int dataLength = 0;
                if (this.Data != null)
                {
                    dataLength = this.Data.Length;
                    dataPtr = Marshal.AllocHGlobal(dataLength);
                    Marshal.Copy(this.Data, 0, dataPtr, dataLength);
                }
                bool updateSuccess;
                if (this.Name.StartsWith("#", StringComparison.Ordinal))
                {
                    // A numeric-named resource must be saved via the integer version of UpdateResource.
                    IntPtr intName = new IntPtr(Int32.Parse(this.Name.Substring(1), CultureInfo.InvariantCulture));
                    updateSuccess = NativeMethods.UpdateResource(updateHandle, new IntPtr(this.ResourceType.IntegerValue), intName, (ushort) this.Locale, dataPtr, (uint) dataLength);
                }
                else
                {
                    updateSuccess = NativeMethods.UpdateResource(updateHandle, (string) this.ResourceType, this.Name, (ushort) this.Locale, dataPtr, (uint) dataLength);
                }
                if (!updateSuccess)
                {
                    throw new IOException("Failed to save resource. Error: " + Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
        }

        /// <summary>
        /// Tests if type, name, and locale of this Resource object match another Resource object.
        /// </summary>
        /// <param name="obj">Resource object to be compared</param>
        /// <returns>True if the objects represent the same resource; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            Resource res = obj as Resource;
            if (res == null) return false;
            return this.ResourceType == res.ResourceType && this.Name == res.Name && this.Locale == res.Locale;
        }

        /// <summary>
        /// Gets a hash code for this Resource object.
        /// </summary>
        /// <returns>Hash code generated from the resource type, name, and locale.</returns>
        public override int GetHashCode()
        {
            return this.ResourceType.GetHashCode() ^ this.Name.GetHashCode() ^ this.Locale.GetHashCode();
        }
    }
}
