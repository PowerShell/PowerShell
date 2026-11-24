// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Provider;

#if !UNIX
/// <summary>
/// Carries the registry metadata captured when a key is emitted, independently of its lifetime.
/// </summary>
internal struct RegistryProviderMetadata
{
    internal PSDriveInfo Drive;
    internal string KeyName;
    internal string[] ValueNames;

    [PSObjectMetadataAccessor("Property", 0)]
    private static string[] GetProperty(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata)
        => metadata.ValueNames;

    [PSObjectMetadataAccessor("PSPath", 1)]
    private static string GetPath(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata)
        => LocationGlobber.GetProviderQualifiedPath(metadata.KeyName, metadata.Drive.Provider);

    [PSObjectMetadataAccessor("PSParentPath", 2)]
    private static string GetParentPath(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata)
    {
        string keyName = metadata.KeyName;
        if (string.Equals(keyName, metadata.Drive.Root, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        int separatorIndex = keyName.LastIndexOf('\\');
        return separatorIndex < 0
            ? string.Empty
            : LocationGlobber.GetProviderQualifiedPath(keyName.Substring(0, separatorIndex), metadata.Drive.Provider);
    }

    [PSObjectMetadataAccessor("PSChildName", 3)]
    private static string GetChildName(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata)
    {
        string keyName = metadata.KeyName;
        int separatorIndex = keyName.LastIndexOf('\\');
        return separatorIndex < 0 ? keyName : keyName.Substring(separatorIndex + 1);
    }

    [PSObjectMetadataAccessor("PSDrive", 4)]
    private static PSDriveInfo GetDrive(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata) => metadata.Drive;

    [PSObjectMetadataAccessor("PSProvider", 5)]
    private static ProviderInfo GetProvider(Microsoft.Win32.RegistryKey key, in RegistryProviderMetadata metadata) => metadata.Drive.Provider;

    [PSObjectMetadataAccessor("PSIsContainer", 6)]
    private static object GetIsContainer(Microsoft.Win32.RegistryKey key) => Boxed.True;
}
#endif
