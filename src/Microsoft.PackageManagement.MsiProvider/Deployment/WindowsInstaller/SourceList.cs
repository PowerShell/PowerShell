//---------------------------------------------------------------------
// <copyright file="SourceList.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// A list of sources for an installed product or patch.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal class SourceList : ICollection<string>
    {
        private Installation installation;
        private SourceMediaList mediaList;

        internal SourceList(Installation installation)
        {
            this.installation = installation;
        }

        /// <summary>
        /// Gets the list of disks registered for the media source of
        /// the patch or product installation.
        /// </summary>
        public SourceMediaList MediaList
        {
            get
            {
                if (this.mediaList == null)
                {
                    this.mediaList = new SourceMediaList(this.installation);
                }
                return this.mediaList;
            }
        }

        /// <summary>
        /// Gets the number of network and URL sources in the list.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                IEnumerator<string> e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the list is read-only.
        /// A SourceList is never read-only.
        /// </summary>
        /// <value>read-only status of the list</value>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Adds a network or URL source to the source list of the installed product.
        /// </summary>
        /// <param name="item">Path to the source to be added. This parameter is
        /// expected to contain only the path without the filename.</param>
        /// <remarks><p>
        /// If this method is called with a new source, the installer adds the source
        /// to the end of the source list.
        /// </p><p>
        /// If this method is called with a source already existing in the source
        /// list, it has no effect.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistaddsource.asp">MsiSourceListAddSource</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistaddsourceex.asp">MsiSourceListAddSourceEx</a>
        /// </p></remarks>
        /// <seealso cref="Insert"/>
        public void Add(string item)
        {
            if (!this.Contains(item))
            {
                this.Insert(item, 0);
            }
        }

        /// <summary>
        /// Adds or reorders a network or URL source for the product or patch.
        /// </summary>
        /// <param name="item">Path to the source to be added. This parameter is
        /// expected to contain only the path without the filename.</param>
        /// <param name="index">Specifies the priority order in which the source
        /// will be inserted</param>
        /// <remarks><p>
        /// If this method is called with a new source and <paramref name="index"/>
        /// is set to 0, the installer adds the source to the end of the source list.
        /// </p><p>
        /// If this method is called with a source already existing in the source
        /// list and <paramref name="index"/> is set to 0, the installer retains the
        /// source's existing index.
        /// </p><p>
        /// If the method is called with an existing source in the source list
        /// and <paramref name="index"/> is set to a non-zero value, the source is
        /// removed from its current location in the list and inserted at the position
        /// specified by Index, before any source that already exists at that position.
        /// </p><p>
        /// If the method is called with a new source and Index is set to a
        /// non-zero value, the source is inserted at the position specified by
        /// <paramref name="index"/>, before any source that already exists at
        /// that position. The index value for all sources in the list after the
        /// index specified by Index are updated to ensure unique index values and
        /// the pre-existing order is guaranteed to remain unchanged.
        /// </p><p>
        /// If <paramref name="index"/> is greater than the number of sources
        /// in the list, the source is placed at the end of the list with an index
        /// value one larger than any existing source.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistaddsourceex.asp">MsiSourceListAddSourceEx</a>
        /// </p></remarks>
        public void Insert(string item, int index)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            NativeMethods.SourceType type = item.Contains("://") ? NativeMethods.SourceType.Url : NativeMethods.SourceType.Network;

            uint ret = NativeMethods.MsiSourceListAddSourceEx(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) type | (uint) this.installation.InstallationType,
                item,
                (uint) index);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Clears sources of all types: network, url, and media.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearall.asp">MsiSourceListClearAll</a>
        /// </p></remarks>
        public void Clear()
        {
            this.ClearSourceType(NativeMethods.SourceType.Url);
            this.ClearSourceType(NativeMethods.SourceType.Network);
            this.MediaList.Clear();
        }

        /// <summary>
        /// Removes all network sources from the list. URL sources are not affected.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearallex.asp">MsiSourceListClearAllEx</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ClearNetworkSources()
        {
            this.ClearSourceType(NativeMethods.SourceType.Network);
        }

        /// <summary>
        /// Removes all URL sources from the list. Network sources are not affected.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearallex.asp">MsiSourceListClearAllEx</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ClearUrlSources()
        {
            this.ClearSourceType(NativeMethods.SourceType.Url);
        }

        /// <summary>
        /// Checks if the specified source exists in the list.
        /// </summary>
        /// <param name="item">case-insensitive source to look for</param>
        /// <returns>true if the source exists in the list, false otherwise</returns>
        public bool Contains(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                throw new ArgumentNullException("item");
            }

            foreach (string s in this)
            {
                if (s.Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies the network and URL sources from this list into an array.
        /// </summary>
        /// <param name="array">destination array to be filed</param>
        /// <param name="arrayIndex">offset into the destination array where copying begins</param>
        public void CopyTo(string[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            foreach (string source in this)
            {
                array[arrayIndex++] = source;
            }
        }

        /// <summary>
        /// Removes a network or URL source.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearsource.asp">MsiSourceListClearSource</a>
        /// </p></remarks>
        public bool Remove(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                throw new ArgumentNullException("item");
            }

            NativeMethods.SourceType type = item.Contains("://") ? NativeMethods.SourceType.Url : NativeMethods.SourceType.Network;

            uint ret = NativeMethods.MsiSourceListClearSource(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) type | (uint) this.installation.InstallationType,
                item);
            if (ret != 0)
            {
                // TODO: Figure out when to return false.
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return true;
        }

        /// <summary>
        /// Enumerates the network and URL sources in the source list of the patch or product installation.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistenumsources.asp">MsiSourceListEnumSources</a>
        /// </p></remarks>
        public IEnumerator<string> GetEnumerator()
        {
            StringBuilder sourceBuf = new StringBuilder(256);
            uint sourceBufSize = (uint) sourceBuf.Capacity;
            for (uint i = 0; true; i++)
            {
                uint ret = this.EnumSources(sourceBuf, i, NativeMethods.SourceType.Network);
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                {
                    break;
                }
                else if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                else
                {
                    yield return sourceBuf.ToString();
                }
            }

            for (uint i = 0; true; i++)
            {
                uint ret = this.EnumSources(sourceBuf, i, NativeMethods.SourceType.Url);
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                {
                    break;
                }
                else if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                else
                {
                    yield return sourceBuf.ToString();
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Forces the installer to search the source list for a valid
        /// source the next time a source is required. For example, when the
        /// installer performs an installation or reinstallation, or when it
        /// requires the path for a component that is set to run from source.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistforceresolution.asp">MsiSourceListForceResolution</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistforceresolutionex.asp">MsiSourceListForceResolutionEx</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ForceResolution()
        {
            uint ret = NativeMethods.MsiSourceListForceResolutionEx(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) this.installation.InstallationType);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Gets or sets the path relative to the root of the installation media.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string MediaPackagePath
        {
            get
            {
                return this["MediaPackagePath"];
            }
            set
            {
                this["MediaPackagePath"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the prompt template that is used when prompting the user
        /// for installation media.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string DiskPrompt
        {
            get
            {
                return this["DiskPrompt"];
            }
            set
            {
                this["DiskPrompt"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the most recently used source location for the product.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string LastUsedSource
        {
            get
            {
                return this["LastUsedSource"];
            }
            set
            {
                this["LastUsedSource"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the Windows Installer package or patch package
        /// on the source.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string PackageName
        {
            get
            {
                return this["PackageName"];
            }
            set
            {
                this["PackageName"] = value;
            }
        }

        /// <summary>
        /// Gets the type of the last-used source.
        /// </summary>
        /// <remarks><p>
        /// <ul>
        /// <li>&quot;n&quot; = network location</li>
        /// <li>&quot;u&quot; = URL location</li>
        /// <li>&quot;m&quot; = media location</li>
        /// <li>(empty string) = no last used source</li>
        /// </ul>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string LastUsedType
        {
            get
            {
                return this["LastUsedType"];
            }
        }

        /// <summary>
        /// Gets or sets source list information properties of a product or patch installation.
        /// </summary>
        /// <param name="property">The source list information property name.</param>
        /// <exception cref="ArgumentOutOfRangeException">An unknown product, patch, or property was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistgetinfo.asp">MsiSourceListGetInfo</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string this[string property]
        {
            get
            {
                StringBuilder buf = new StringBuilder("");
                uint bufSize = 0;
                uint ret = NativeMethods.MsiSourceListGetInfo(
                    this.installation.InstallationCode,
                    this.installation.UserSid,
                    this.installation.Context,
                    (uint) this.installation.InstallationType,
                    property,
                    buf,
                    ref bufSize);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.MORE_DATA)
                    {
                        buf.Capacity = (int) ++bufSize;
                        ret = NativeMethods.MsiSourceListGetInfo(
                            this.installation.InstallationCode,
                            this.installation.UserSid,
                            this.installation.Context,
                            (uint) this.installation.InstallationType,
                            property,
                            buf,
                            ref bufSize);
                    }

                    if (ret != 0)
                    {
                        if (ret == (uint) NativeMethods.Error.UNKNOWN_PRODUCT ||
                            ret == (uint) NativeMethods.Error.UNKNOWN_PROPERTY)
                        {
                            throw new ArgumentOutOfRangeException("property");
                        }
                        else
                        {
                            throw InstallerException.ExceptionFromReturnCode(ret);
                        }
                    }
                }
                return buf.ToString();
            }
            set
            {
                uint ret = NativeMethods.MsiSourceListSetInfo(
                    this.installation.InstallationCode,
                    this.installation.UserSid,
                    this.installation.Context,
                    (uint) this.installation.InstallationType,
                    property,
                    value);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_PRODUCT ||
                        ret == (uint) NativeMethods.Error.UNKNOWN_PROPERTY)
                    {
                        throw new ArgumentOutOfRangeException("property");
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }
            }
        }

        private void ClearSourceType(NativeMethods.SourceType type)
        {
            uint ret = NativeMethods.MsiSourceListClearAllEx(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) type | (uint) this.installation.InstallationType);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        private uint EnumSources(StringBuilder sourceBuf, uint i, NativeMethods.SourceType sourceType)
        {
            int enumType = (this.installation.InstallationType | (int) sourceType);
            uint sourceBufSize = (uint) sourceBuf.Capacity;
            uint ret = NativeMethods.MsiSourceListEnumSources(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) enumType,
                i,
                sourceBuf,
                ref sourceBufSize);
            if (ret == (uint) NativeMethods.Error.MORE_DATA)
            {
                sourceBuf.Capacity = (int) ++sourceBufSize;
                ret = NativeMethods.MsiSourceListEnumSources(
                    this.installation.InstallationCode,
                    this.installation.UserSid,
                    this.installation.Context,
                    (uint) enumType,
                    i,
                    sourceBuf,
                    ref sourceBufSize);
            }
            return ret;
        }
    }
}
