//---------------------------------------------------------------------
// <copyright file="MediaDisk.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Microsoft.Deployment.WindowsInstaller.MediaDisk struct.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a media disk source of a product or a patch.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct MediaDisk
    {
        private int diskId;
        private string volumeLabel;
        private string diskPrompt;

        /// <summary>
        /// Creates a new media disk.
        /// </summary>
        /// <param name="diskId"></param>
        /// <param name="volumeLabel"></param>
        /// <param name="diskPrompt"></param>
        public MediaDisk(int diskId, string volumeLabel, string diskPrompt)
        {
            this.diskId = diskId;
            this.volumeLabel = volumeLabel;
            this.diskPrompt = diskPrompt;
        }

        /// <summary>
        /// Gets or sets the disk id of the media disk.
        /// </summary>
        public int DiskId
        {
            get { return this.diskId; }
            set { this.diskId = value; }
        }

        /// <summary>
        /// Gets or sets the volume label of the media disk.
        /// </summary>
        public string VolumeLabel
        {
            get { return this.volumeLabel; }
            set { this.volumeLabel = value; }
        }

        /// <summary>
        /// Gets or sets the disk prompt of the media disk.
        /// </summary>
        public string DiskPrompt
        {
            get { return this.diskPrompt; }
            set { this.diskPrompt = value; }
        }
    }
}
