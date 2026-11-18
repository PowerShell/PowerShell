// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider;

/// <summary>
/// Carries the drive context and Unix stat captured when a FileSystem item is emitted.
/// </summary>
/// <remarks>Path getters use the current FileSystemInfo path; they do not refresh the captured drive or stat.</remarks>
internal struct FileSystemProviderMetadata
{
    /// <summary>
    /// The drive through which the item was emitted.
    /// </summary>
    internal PSDriveInfo Drive;
#if UNIX
    /// <summary>
    /// The Unix stat snapshot captured at emission.
    /// </summary>
    internal Platform.Unix.CommonStat UnixStat;
#endif

    /// <summary>
    /// Gets the current provider-qualified path.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <param name="metadata">The captured provider context.</param>
    /// <returns>The current full path qualified with the captured provider.</returns>
    [PSObjectMetadataAccessor("PSPath", 0)]
    private static string GetPath(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata)
        => System.Management.Automation.LocationGlobber.GetProviderQualifiedPath(file.FullName, metadata.Drive.Provider);

    /// <summary>
    /// Gets the current parent path relative to the captured drive root.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <param name="metadata">The captured provider context.</param>
    /// <returns>The qualified parent path, or an empty string when there is no parent.</returns>
    [PSObjectMetadataAccessor("PSParentPath", 1, 1)]
    private static string GetParentPath(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata)
    {
        string parentPath = Microsoft.PowerShell.Commands.FileSystemProvider.GetParentPathForMetadata(file.FullName, metadata.Drive.Root);
        return string.IsNullOrEmpty(parentPath)
            ? string.Empty
            : System.Management.Automation.LocationGlobber.GetProviderQualifiedPath(parentPath, metadata.Drive.Provider);
    }

    /// <summary>
    /// Gets the child name from the current filesystem path.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <returns>The lexical child name, including root-path handling.</returns>
    [PSObjectMetadataAccessor("PSChildName", 2, 2)]
    private static string GetChildName(System.IO.FileSystemInfo file)
        => Microsoft.PowerShell.Commands.FileSystemProvider.GetChildNameForMetadata(file.FullName);

#if UNIX
    /// <summary>
    /// Gets the captured Unix stat without querying the filesystem again.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <param name="metadata">The captured provider context.</param>
    /// <returns>The stat snapshot stored in the metadata.</returns>
    [PSObjectMetadataAccessor("UnixStat", 3, 4)]
    private static Platform.Unix.CommonStat GetUnixStat(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.UnixStat;
#endif

    /// <summary>
    /// Gets the drive captured when the item was emitted.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <param name="metadata">The captured provider context.</param>
    /// <returns>The original drive reference.</returns>
    [PSObjectMetadataAccessor("PSDrive", 4, 8)]
    private static PSDriveInfo GetDrive(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.Drive;

    /// <summary>
    /// Gets the provider associated with the captured drive.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <param name="metadata">The captured provider context.</param>
    /// <returns>The provider reference from the captured drive.</returns>
    [PSObjectMetadataAccessor("PSProvider", 5)]
    private static ProviderInfo GetProvider(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.Drive.Provider;

    /// <summary>
    /// Determines container status from the base object type.
    /// </summary>
    /// <param name="file">The underlying filesystem item.</param>
    /// <returns>The shared boxed Boolean indicating whether the object is a directory.</returns>
    [PSObjectMetadataAccessor("PSIsContainer", 6, 16)]
    private static object GetIsContainer(System.IO.FileSystemInfo file)
        => file is System.IO.DirectoryInfo ? Boxed.True : Boxed.False;
}
