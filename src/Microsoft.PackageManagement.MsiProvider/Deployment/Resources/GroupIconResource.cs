//---------------------------------------------------------------------
// <copyright file="GroupIconResource.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.Resources
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A subclass of Resource which provides specific methods for manipulating the resource data.
    /// </summary>
    /// <remarks>
    /// The resource is of type <see cref="ResourceType.GroupIcon"/> (RT_GROUPICON).
    /// </remarks>
    internal sealed class GroupIconResource : Resource
    {
        internal bool dirty;
        private GroupIconInfo rawGroupIconInfo;
        private List<Resource> icons;

        /// <summary>
        /// Creates a new GroupIconResource object without any data. The data can be later loaded from a file.
        /// </summary>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        public GroupIconResource(string name, int locale)
            : this(name, locale, null)
        {
        }

        /// <summary>
        /// Creates a new GroupIconResource object with data. The data can be later saved to a file.
        /// </summary>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        /// <param name="data">Raw resource data</param>
        public GroupIconResource(string name, int locale, byte[] data)
            : base(ResourceType.GroupIcon, name, locale, data)
        {
            this.RefreshIconGroupInfo(data);
        }

        /// <summary>
        /// Gets or sets the raw data of the resource.  The data is in the format of the RT_GROUPICON resource structure.
        /// </summary>
        public override byte[] Data
        {
            get
            {
                if (this.dirty)
                {
                    base.Data = this.rawGroupIconInfo.GetResourceData();
                    this.dirty = false;
                }

                return base.Data;
            }
            set
            {
                this.RefreshIconGroupInfo(value);

                base.Data = value;
                this.dirty = false;
            }
        }

        /// <summary>
        /// Enumerates the the icons in the icon group.
        /// </summary>
        public IEnumerable<Resource> Icons { get { return this.icons; } }

        /// <summary>
        /// Reads the icon group from a .ico file.
        /// </summary>
        /// <param name="path">Path to an icon file (.ico).</param>
        public void ReadFromFile(string path)
        {
            this.rawGroupIconInfo = new GroupIconInfo();
            this.icons = new List<Resource>();
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read))
            {
                this.rawGroupIconInfo.ReadFromFile(fs);

                // After reading the group icon info header from the file, read all the icons.
                for (int i = 0; i < this.rawGroupIconInfo.DirectoryInfo.Length; ++i)
                {
                    ushort index = this.rawGroupIconInfo.DirectoryInfo[i].imageIndex;
                    uint offset = this.rawGroupIconInfo.DirectoryInfo[i].imageOffset;
                    uint size = this.rawGroupIconInfo.DirectoryInfo[i].imageSize;
                    byte[] data = new byte[size];

                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Read(data, 0, data.Length);

                    Resource resource = new Resource(ResourceType.Icon, String.Concat("#", index), this.Locale, data);
                    this.icons.Add(resource);
                }
            }

            this.dirty = true;
        }

        private void RefreshIconGroupInfo(byte[] refreshData)
        {
            this.rawGroupIconInfo = new GroupIconInfo();
            this.icons = new List<Resource>();
            if (refreshData != null)
            {
                this.rawGroupIconInfo.ReadFromResource(refreshData);
            }

            this.dirty = true;
        }
    }
}
