//---------------------------------------------------------------------
// <copyright file="FixedFileVersionInfo.cs" company="Microsoft Corporation">
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
    using System.IO;

    internal class FixedFileVersionInfo
    {
        public FixedFileVersionInfo()
        {
            // Set reasonable defaults
            this.signature = 0xFEEF04BD;
            this.structVersion = 0x00010000; // v1.0
            this.FileVersion = new Version(0, 0, 0, 0);
            this.ProductVersion = new Version(0, 0, 0, 0);
            this.FileFlagsMask = VersionBuildTypes.Debug | VersionBuildTypes.Prerelease;
            this.FileFlags = VersionBuildTypes.None;
            this.FileOS = VersionFileOS.NT_WINDOWS32;
            this.FileType = VersionFileType.Application;
            this.FileSubtype = VersionFileSubtype.Unknown;
            this.Timestamp = DateTime.MinValue;
        }

        private uint signature;
        private uint structVersion;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Version FileVersion
        {
            get
            {
                return this.fileVersion;
            }

            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException();
                }

                this.fileVersion = value;
            }
        }
        private Version fileVersion;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public Version ProductVersion
        {
            get
            {
                return this.productVersion;
            }

            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException();
                }

                this.productVersion = value;
            }
        }
        private Version productVersion;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionBuildTypes FileFlagsMask
        {
            get { return this.fileFlagsMask; }
            set { this.fileFlagsMask = value; }
        }
        private VersionBuildTypes fileFlagsMask;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionBuildTypes FileFlags
        {
            get { return this.fileFlags; }
            set { this.fileFlags = value; }
        }
        private VersionBuildTypes fileFlags;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionFileOS FileOS
        {
            get { return this.fileOS; }
            set { this.fileOS = value; }
        }
        private VersionFileOS fileOS;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionFileType FileType
        {
            get { return this.fileType; }
            set { this.fileType = value; }
        }
        private VersionFileType fileType;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public VersionFileSubtype FileSubtype
        {
            get { return this.fileSubtype; }
            set { this.fileSubtype = value; }
        }
        private VersionFileSubtype fileSubtype;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public DateTime Timestamp
        {
            get { return this.timestamp; }
            set { this.timestamp = value; }
        }
        private DateTime timestamp;

        public void Read(BinaryReader reader)
        {
            this.signature = reader.ReadUInt32();
            this.structVersion = reader.ReadUInt32();
            this.fileVersion = UInt64ToVersion(reader.ReadUInt64());
            this.productVersion = UInt64ToVersion(reader.ReadUInt64());
            this.fileFlagsMask = (VersionBuildTypes) reader.ReadInt32();
            this.fileFlags = (VersionBuildTypes) reader.ReadInt32();
            this.fileOS = (VersionFileOS) reader.ReadInt32();
            this.fileType = (VersionFileType) reader.ReadInt32();
            this.fileSubtype = (VersionFileSubtype) reader.ReadInt32();
            this.timestamp = UInt64ToDateTime(reader.ReadUInt64());
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(this.signature);
            writer.Write(this.structVersion);
            writer.Write(VersionToUInt64(this.fileVersion));
            writer.Write(VersionToUInt64(this.productVersion));
            writer.Write((int) this.fileFlagsMask);
            writer.Write((int) this.fileFlags);
            writer.Write((int) this.fileOS);
            writer.Write((int) this.fileType);
            writer.Write((int) this.fileSubtype);
            writer.Write(DateTimeToUInt64(this.timestamp));
        }

        public static explicit operator FixedFileVersionInfo(byte[] bytesValue)
        {
            FixedFileVersionInfo ffviValue = new FixedFileVersionInfo();
            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytesValue, false)))
            {
                ffviValue.Read(reader);
            }
            return ffviValue;
        }

        public static explicit operator byte[](FixedFileVersionInfo ffviValue)
        {
            const int FFVI_LENGTH = 52;

            byte[] bytesValue = new byte[FFVI_LENGTH];
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(bytesValue, true)))
            {
                ffviValue.Write(writer);
            }
            return bytesValue;
        }

        private static Version UInt64ToVersion(ulong version)
        {
            return new Version((int) ((version >> 16) & 0xFFFF), (int) (version & 0xFFFF), (int) (version >> 48), (int) ((version >> 32) & 0xFFFF));
        }
        private static ulong VersionToUInt64(Version version)
        {
            return (((ulong) (ushort) version.Major) << 16) | ((ulong) (ushort) version.Minor)
                | (((ulong) (ushort) version.Build) << 48) | (((ulong) (ushort) version.Revision) << 32);
        }

        private static DateTime UInt64ToDateTime(ulong dateTime)
        {
            return (dateTime == 0 ? DateTime.MinValue : DateTime.FromFileTime((long) dateTime));
        }
        private static ulong DateTimeToUInt64(DateTime dateTime)
        {
            return (dateTime == DateTime.MinValue ? 0 : (ulong) dateTime.ToFileTime());
        }
    }
}
