// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal struct ContentOutputMetadata
    {
        // Share the existing per-path cache across emitted lines. A FileInfo alone cannot
        // describe provider paths (including PSDrive/content-provider paths), so the cached
        // PathInfo context remains the source for the legacy path note values.
        internal ContentCommandBase.ContentPathsCache Paths;
        internal long ReadCount;

        [PSObjectMetadataAccessor("PSPath", 0)]
        private static string GetPath(object content, in ContentOutputMetadata metadata) => metadata.Paths.PSPath;

        [PSObjectMetadataAccessor("PSParentPath", 1, 1)]
        private static string GetParentPath(object content, in ContentOutputMetadata metadata) => metadata.Paths.ParentPath;

        [PSObjectMetadataAccessor("PSChildName", 2, 2)]
        private static string GetChildName(object content, in ContentOutputMetadata metadata) => metadata.Paths.ChildName;

        [PSObjectMetadataAccessor("PSDrive", 3, 4)]
        private static PSDriveInfo GetDrive(object content, in ContentOutputMetadata metadata) => metadata.Paths.Drive;

        [PSObjectMetadataAccessor("PSProvider", 4)]
        private static ProviderInfo GetProvider(object content, in ContentOutputMetadata metadata) => metadata.Paths.Provider;

        [PSObjectMetadataAccessor("ReadCount", 5)]
        private static long GetReadCount(object content, in ContentOutputMetadata metadata) => metadata.ReadCount;
    }
}
