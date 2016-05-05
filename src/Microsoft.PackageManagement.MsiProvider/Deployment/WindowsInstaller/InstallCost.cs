//---------------------------------------------------------------------
// <copyright file="InstallCost.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Microsoft.Deployment.WindowsInstaller.InstallCost struct.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a per-drive disk space cost for an installation.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct InstallCost
    {
        private string driveName;
        private long cost;
        private long tempCost;

        /// <summary>
        /// Creates a new InstallCost object.
        /// </summary>
        /// <param name="driveName">name of the drive this cost data applies to</param>
        /// <param name="cost">installation cost on this drive, as a number of bytes</param>
        /// <param name="tempCost">temporary disk space required on this drive, as a number of bytes</param>
        internal InstallCost(string driveName, long cost, long tempCost)
        {
            this.driveName = driveName;
            this.cost = cost;
            this.tempCost = tempCost;
        }

        /// <summary>
        /// The name of the drive this cost data applies to.
        /// </summary>
        public string DriveName
        {
            get
            {
                return this.driveName;
            }
        }

        /// <summary>
        /// The installation cost on this drive, as a number of bytes.
        /// </summary>
        public long Cost
        {
            get
            {
                return this.cost;
            }
        }

        /// <summary>
        /// The temporary disk space required on this drive, as a number of bytes.
        /// </summary>
        /// <remarks><p>
        /// This temporary space requirement is space needed only for the duration
        /// of the installation, over the final footprint on disk.
        /// </p></remarks>
        public long TempCost
        {
            get
            {
                return this.tempCost;
            }
        }
    }
}
