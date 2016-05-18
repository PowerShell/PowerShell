//---------------------------------------------------------------------
// <copyright file="SourceMediaList.cs" company="Microsoft Corporation">
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
    /// A list of source media for an installed product or patch.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    internal class SourceMediaList : ICollection<MediaDisk>
    {
        private Installation installation;

        internal SourceMediaList(Installation installation)
        {
            this.installation = installation;
        }

        /// <summary>
        /// Gets the number of source media in the list.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                IEnumerator<MediaDisk> e = this.GetEnumerator();
                while (e.MoveNext())
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the list is read-only.
        /// A SourceMediaList is never read-only.
        /// </summary>
        /// <value>read-only status of the list</value>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Adds or updates a disk of the media source for the product or patch.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistaddmediadisk.asp">MsiSourceListAddMediaDisk</a>
        /// </p></remarks>
        public void Add(MediaDisk item)
        {
            uint ret = NativeMethods.MsiSourceListAddMediaDisk(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) this.installation.InstallationType,
                (uint) item.DiskId,
                item.VolumeLabel,
                item.DiskPrompt);

            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Removes all source media from the list.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearallex.asp">MsiSourceListClearAllEx</a>
        /// </p></remarks>
        public void Clear()
        {
            uint ret = NativeMethods.MsiSourceListClearAllEx(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) NativeMethods.SourceType.Media | (uint) this.installation.InstallationType);

            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Checks if the specified media disk id exists in the list.
        /// </summary>
        /// <param name="diskId">disk id of the media to look for</param>
        /// <returns>true if the media exists in the list, false otherwise</returns>
        public bool Contains(int diskId)
        {
            foreach (MediaDisk mediaDisk in this)
            {
                if (mediaDisk.DiskId == diskId)
                {
                    return true;
                }
            }
            return false;
        }

        bool ICollection<MediaDisk>.Contains(MediaDisk mediaDisk)
        {
            return this.Contains(mediaDisk.DiskId);
        }

        /// <summary>
        /// Copies the source media info from this list into an array.
        /// </summary>
        /// <param name="array">destination array to be filed</param>
        /// <param name="arrayIndex">offset into the destination array where copying begins</param>
        public void CopyTo(MediaDisk[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            foreach (MediaDisk mediaDisk in this)
            {
                array[arrayIndex++] = mediaDisk;
            }
        }

        /// <summary>
        /// Removes a specified disk from the set of registered disks.
        /// </summary>
        /// <param name="diskId">ID of the disk to remove</param>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistclearmediadisk.asp">MsiSourceListClearMediaDisk</a>
        /// </p></remarks>
        public bool Remove(int diskId)
        {
            uint ret = NativeMethods.MsiSourceListClearMediaDisk(
                this.installation.InstallationCode,
                this.installation.UserSid,
                this.installation.Context,
                (uint) this.installation.InstallationType,
                (uint) diskId);

            if (ret != 0)
            {
                // TODO: Figure out when to return false.
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return true;
        }

        bool ICollection<MediaDisk>.Remove(MediaDisk mediaDisk)
        {
            return this.Remove(mediaDisk.DiskId);
        }

        /// <summary>
        /// Enumerates the source media in the source list of the patch or product installation.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisourcelistenummediadisks.asp">MsiSourceListEnumMediaDisks</a>
        /// </p></remarks>
        public IEnumerator<MediaDisk> GetEnumerator()
        {
            uint diskId;
            StringBuilder volumeBuf = new StringBuilder(40);
            uint volumeBufSize = (uint) volumeBuf.Capacity;
            StringBuilder promptBuf = new StringBuilder(80);
            uint promptBufSize = (uint) promptBuf.Capacity;
            for (uint i = 0; true; i++)
            {
                uint ret = NativeMethods.MsiSourceListEnumMediaDisks(
                    this.installation.InstallationCode,
                    this.installation.UserSid,
                    this.installation.Context,
                    (uint) this.installation.InstallationType,
                    i,
                    out diskId,
                    volumeBuf,
                    ref volumeBufSize,
                    promptBuf,
                    ref promptBufSize);

                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    volumeBuf.Capacity = (int) ++volumeBufSize;
                    promptBuf.Capacity = (int) ++promptBufSize;

                    ret = NativeMethods.MsiSourceListEnumMediaDisks(
                        this.installation.InstallationCode,
                        this.installation.UserSid,
                        this.installation.Context,
                        (uint) this.installation.InstallationType,
                        i,
                        out diskId,
                        volumeBuf,
                        ref volumeBufSize,
                        promptBuf,
                        ref promptBufSize);
                }

                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                {
                    break;
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                yield return new MediaDisk((int) diskId, volumeBuf.ToString(), promptBuf.ToString());
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
