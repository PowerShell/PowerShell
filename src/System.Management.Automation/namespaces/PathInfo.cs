// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation;

/// <summary>
/// An object that represents a path.
/// </summary>
public sealed class PathInfo
{
    private string _providerPath = null;
    private readonly SessionState _sessionState;
    private readonly PSDriveInfo _drive;
    private readonly ProviderInfo _provider;
    private readonly string _path;

    /// <summary>
    /// Gets the drive that contains the path.
    /// </summary>
    public PSDriveInfo Drive => _drive == null || _drive.Hidden ? null : _drive;

    /// <summary>
    /// Gets the provider that contains the path.
    /// </summary>
    public ProviderInfo Provider => _provider;

    /// <summary>
    /// This is the internal mechanism to get the hidden drive.
    /// </summary>
    /// <returns>
    /// The drive associated with this PathInfo.
    /// </returns>
    internal PSDriveInfo GetDrive() => _drive;

    /// <summary>
    /// Gets the provider internal path for the PSPath that this PathInfo represents.
    /// </summary>
    /// <exception cref="ProviderInvocationException">
    /// The provider encountered an error when resolving the path.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The path was a home relative path but the home path was not
    /// set for the provider.
    /// </exception>
    public string ProviderPath
    {
        // Construct the providerPath
        get => _providerPath ??= _sessionState.Internal.ExecutionContext.LocationGlobber.GetProviderPath(Path);
    }

    /// <summary>
    /// Gets the PowerShell path that this object represents.
    /// </summary>
    public string Path => Drive == null
            ? LocationGlobber.GetProviderQualifiedPath(_path, Provider)
            : LocationGlobber.GetDriveQualifiedPath(_path, Drive);

    /// <summary>
    /// Gets the PowerShell path that this object represents, in a format suitable for display to the user.
    /// For scripting, prefer to use the other properties of PathInfo, do not rely on the exact format of this property.
    /// </summary>
    public string DisplayPath
    {
        // for real filesystem paths, always return the provider path, since the provider-qualified paths can be a bit
        // unwieldy (e.g., after `Get-Item D:\dir | cd`, `Path` will be `Microsoft.PowerShell.Core\FileSystem::D:\dir`)
        //
        // for other providers (such as Registry), the provider-qualified path has the same issue, but the solution is
        // less clear-cut, since the provider path may not have a drive (Registry), or it may be empty (Env), or it may
        // not match the semantic location (virtual drive from New-PSDrive); return the fully-qualified path for now
        get => Drive == null && Provider.ImplementingType == typeof(FileSystemProvider)
                ? ProviderPath
                : Path;
    }

    /// <summary>
    /// Gets a string representing the PowerShell path.
    /// </summary>
    /// <returns>
    /// A string representing the PowerShell path.
    /// </returns>
    public override string ToString() => Path;

    /// <summary>
    /// The constructor of the PathInfo object.
    /// </summary>
    /// <param name="drive">
    /// The drive that contains the path
    /// </param>
    /// <param name="provider">
    /// The provider that contains the path.
    /// </param>
    /// <param name="path">
    /// The path this object represents.
    /// </param>
    /// <param name="sessionState">
    /// The session state associated with the drive, provider, and path information.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="drive"/>, <paramref name="provider"/>,
    /// <paramref name="path"/>, or <paramref name="sessionState"/> is null.
    /// </exception>
    internal PathInfo(PSDriveInfo drive, ProviderInfo provider, string path, SessionState sessionState)
    {
        _drive = drive;
        _provider = provider ?? throw PSTraceSource.NewArgumentNullException(nameof(provider));
        _path = path ?? throw PSTraceSource.NewArgumentNullException(nameof(path));
        _sessionState = sessionState ?? throw PSTraceSource.NewArgumentNullException(nameof(sessionState));
    }
}
