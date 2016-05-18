//---------------------------------------------------------------------
// <copyright file="VersionResource.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// A subclass of Resource which provides specific methods for manipulating the resource data.
    /// </summary>
    /// <remarks>
    /// The resource is of type <see cref="ResourceType.Version"/> (RT_VERSION).
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal sealed class VersionResource : Resource, ICollection<VersionStringTable>
    {
        internal bool dirty;
        private VersionInfo rawVersionInfo;
        private FixedFileVersionInfo rawFileVersionInfo;

        /// <summary>
        /// Creates a new VersionResource object without any data. The data can be later loaded from a file.
        /// </summary>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        public VersionResource(string name, int locale)
            : this(name, locale, null)
        {
        }

        /// <summary>
        /// Creates a new VersionResource object with data. The data can be later saved to a file.
        /// </summary>
        /// <param name="name">Name of the resource. For a numeric resource identifier, prefix the decimal number with a "#".</param>
        /// <param name="locale">Locale of the resource</param>
        /// <param name="data">Raw resource data</param>
        public VersionResource(string name, int locale, byte[] data)
            : base(ResourceType.Version, name, locale, data)
        {
            this.RefreshVersionInfo(data);
        }

        /// <summary>
        /// Gets or sets the raw data of the resource.  The data is in the format of the VS_VERSIONINFO structure.
        /// </summary>
        public override byte[] Data
        {
            get
            {
                if (this.dirty)
                {
                    this.rawVersionInfo.Data = (byte[]) this.rawFileVersionInfo;
                    base.Data = (byte[]) this.rawVersionInfo;
                    this.dirty = false;
                }

                return base.Data;
            }
            set
            {
                base.Data = value;
                this.RefreshVersionInfo(value);
                this.dirty = false;
            }
        }

        private void RefreshVersionInfo(byte[] refreshData)
        {
            if (refreshData == null)
            {
                this.rawVersionInfo = new VersionInfo("VS_VERSION_INFO");
                this.rawFileVersionInfo = new FixedFileVersionInfo();
            }
            else
            {
                this.rawVersionInfo = (VersionInfo) refreshData;
                this.rawFileVersionInfo = (FixedFileVersionInfo) this.rawVersionInfo.Data;
            }
        }

        /// <summary>
        /// Gets or sets the binary locale-independent file version of the version resource.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Version FileVersion
        {
            get
            {
                return this.rawFileVersionInfo.FileVersion;
            }
            set
            {
                this.rawFileVersionInfo.FileVersion = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the binary locale-independent product version of the version resource.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Version ProductVersion
        {
            get
            {
                return this.rawFileVersionInfo.ProductVersion;
            }
            set
            {
                this.rawFileVersionInfo.ProductVersion = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets a bitmask that specifies the build types of the file.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionBuildTypes BuildTypes
        {
            get
            {
                return this.rawFileVersionInfo.FileFlags &
                    this.rawFileVersionInfo.FileFlagsMask;
            }
            set
            {
                this.rawFileVersionInfo.FileFlags = value;
                this.rawFileVersionInfo.FileFlagsMask = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the general type of the file.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionFileType FileType
        {
            get
            {
                return this.rawFileVersionInfo.FileType;
            }
            set
            {
                this.rawFileVersionInfo.FileType = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the specific type of the file.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionFileSubtype FileSubtype
        {
            get
            {
                return this.rawFileVersionInfo.FileSubtype;
            }
            set
            {
                this.rawFileVersionInfo.FileSubtype = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets the binary creation date and time.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime Timestamp
        {
            get
            {
                return this.rawFileVersionInfo.Timestamp;
            }
            set
            {
                this.rawFileVersionInfo.Timestamp = value;
                this.dirty = true;
            }
        }

        /// <summary>
        /// Gets the string table for a specific locale, or null if there is no table for that locale.
        /// </summary>
        /// <seealso cref="Add(int)"/>
        /// <seealso cref="Remove(int)"/>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionStringTable this[int locale]
        {
            get
            {
                VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
                if (svi != null)
                {
                    foreach (VersionInfo strings in svi)
                    {
                        int stringsLocale = UInt16.Parse(strings.Key.Substring(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        if (stringsLocale == locale)
                        {
                            return new VersionStringTable(this, strings);
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Adds a new version string table for a locale.
        /// </summary>
        /// <param name="locale">Locale of the table</param>
        /// <returns>The new string table, or the existing table if the locale already existed.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionStringTable Add(int locale)
        {
            VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
            if (svi == null)
            {
                svi = new VersionInfo("StringFileInfo");
                this.rawVersionInfo.Add(svi);
            }
            foreach (VersionInfo strings in svi)
            {
                int stringsLocale = UInt16.Parse(strings.Key.Substring(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (stringsLocale == locale)
                {
                    return new VersionStringTable(this, strings);
                }
            }

            VersionInfo newStrings = new VersionInfo(
                ((ushort) locale).ToString("x4", CultureInfo.InvariantCulture) + ((ushort) 1200).ToString("x4", CultureInfo.InvariantCulture));
            svi.Add(newStrings);
            this.dirty = true;

            VersionInfo vvi = this.rawVersionInfo["VarFileInfo"];
            if (vvi == null)
            {
                vvi = new VersionInfo("VarFileInfo");
                vvi.Add(new VersionInfo("Translation"));
                this.rawVersionInfo.Add(vvi);
            }
            VersionInfo tVerInfo = vvi["Translation"];
            if (tVerInfo != null)
            {
                byte[] oldValue = tVerInfo.Data;
                if (oldValue == null) oldValue = new byte[0];
                tVerInfo.Data = new byte[oldValue.Length + 4];
                Array.Copy(oldValue, tVerInfo.Data, oldValue.Length);
                using (BinaryWriter bw = new BinaryWriter(new MemoryStream(tVerInfo.Data, oldValue.Length, 4, true)))
                {
                    bw.Write((ushort) locale);
                    bw.Write((ushort) 1200);
                }
            }

            return new VersionStringTable(this, newStrings);
        }

        /// <summary>
        /// Removes a version string table for a locale.
        /// </summary>
        /// <param name="locale">Locale of the table</param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void Remove(int locale)
        {
            VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
            if (svi != null)
            {
                foreach (VersionInfo strings in svi)
                {
                    int stringsLocale = UInt16.Parse(strings.Key.Substring(0, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (stringsLocale == locale)
                    {
                        svi.Remove(strings);
                        this.dirty = true;
                        break;
                    }
                }

            }

            VersionInfo vvi = this.rawVersionInfo["VarFileInfo"];
            if (vvi != null)
            {
                VersionInfo tVerInfo = vvi["Translation"];
                if (tVerInfo != null)
                {
                    byte[] newValue = new byte[tVerInfo.Data.Length];
                    int j = 0;
                    using (BinaryWriter bw = new BinaryWriter(new MemoryStream(newValue, 0, newValue.Length, true)))
                    {
                        using (BinaryReader br = new BinaryReader(new MemoryStream(tVerInfo.Data)))
                        {
                            for (int i = tVerInfo.Data.Length / 4; i > 0; i--)
                            {
                                ushort tLocale = br.ReadUInt16();
                                ushort cp = br.ReadUInt16();
                                if (tLocale != locale)
                                {
                                    bw.Write((ushort) tLocale);
                                    bw.Write((ushort) cp);
                                    j++;
                                }
                            }
                        }
                    }
                    tVerInfo.Data = new byte[j * 4];
                    Array.Copy(newValue, tVerInfo.Data, tVerInfo.Data.Length);
                }
            }
        }

        /// <summary>
        /// Checks if a version string table exists for a given locale.
        /// </summary>
        /// <param name="locale">Locale to search for</param>
        /// <returns>True if a string table was found for the locale; false otherwise.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool Contains(int locale)
        {
            return this[locale] != null;
        }

        /// <summary>
        /// Gets the number string tables in the version resource.
        /// </summary>
        public int Count
        {
            get
            {
                VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
                return svi != null ? svi.Count : 0;
            }
        }

        /// <summary>
        /// Removes all string tables from the version resource.
        /// </summary>
        public void Clear()
        {
            VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
            if (svi != null)
            {
                svi.Clear();
                this.dirty = true;
            }
        }

        bool ICollection<VersionStringTable>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        void ICollection<VersionStringTable>.Add(VersionStringTable item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<VersionStringTable>.Remove(VersionStringTable item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<VersionStringTable>.Contains(VersionStringTable item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Copies the version string tables to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied
        /// from the collection. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(VersionStringTable[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
            if (svi != null)
            {
                foreach (VersionInfo strings in svi)
                {
                    array[arrayIndex++] = new VersionStringTable(this, strings);
                }
            }
        }

        /// <summary>
        /// Gets an enumerator that can iterate over the version string tables in the collection.
        /// </summary>
        /// <returns>An enumerator that returns <see cref="VersionStringTable"/> objects.</returns>
        public IEnumerator<VersionStringTable> GetEnumerator()
        {
            VersionInfo svi = this.rawVersionInfo["StringFileInfo"];
            if (svi != null)
            {
                foreach (VersionInfo strings in svi)
                {
                    yield return new VersionStringTable(this, strings);
                }
            }
        }

        /// <summary>
        /// Gets an enumerator that can iterate over the version string tables in the collection.
        /// </summary>
        /// <returns>An enumerator that returns <see cref="VersionStringTable"/> objects.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
