using System.Threading;
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace System.Management.Automation;

/// <summary>
/// Represents a lazily retrieved <see cref="Stream" /> for transfering bytes
/// to or from.
/// </summary>
internal abstract class BytePipe
{
    public abstract Task<Stream> GetStream(CancellationToken cancellationToken);

    internal AsyncByteStreamTransfer Bind(BytePipe bytePipe)
    {
        Debug.Assert(bytePipe is not null);
        return new AsyncByteStreamTransfer(
            bytePipe,
            (bytes, stream) => stream.Write(bytes),
            destinationPipe: this,
            stream => stream.Close());
    }
}

/// <summary>
/// Represents a <see cref="Stream" /> lazily retrieved from the underlying
/// <see cref="NativeCommandProcessor" />.
/// </summary>
internal sealed class NativeCommandProcessorBytePipe : BytePipe
{
    private readonly NativeCommandProcessor _nativeCommand;

    private readonly bool _stdout;

    internal NativeCommandProcessorBytePipe(
        NativeCommandProcessor nativeCommand,
        bool stdout)
    {
        Debug.Assert(nativeCommand is not null);
        _nativeCommand = nativeCommand;
        _stdout = stdout;
    }

    public override async Task<Stream> GetStream(CancellationToken cancellationToken)
    {
        // If the native command we're wrapping is the upstream command then
        // NativeCommandProcessor.Prepare will have already been called before
        // the creation of this BytePipe.
        if (_stdout)
        {
            return _nativeCommand.GetStream(stdout: true);
        }

        await _nativeCommand.WaitForProcessInitializationAsync(cancellationToken);
        return _nativeCommand.GetStream(stdout: false);
    }
}

/// <summary>
/// Provides an byte pipe implementation representing a <see cref="FileStream" />.
/// </summary>
internal sealed class FileBytePipe : BytePipe
{
    private readonly Stream _stream;

    private FileBytePipe(Stream stream)
    {
        Debug.Assert(stream is not null);
        _stream = stream;
    }

    internal static FileBytePipe Create(string fileName, bool append)
    {
        FileStream fileStream;
        try
        {
            PathUtils.MasterStreamOpen(
                fileName,
                resolvedEncoding: null,
                defaultEncoding: false,
                append,
                Force: true,
                NoClobber: false,
                out fileStream,
                streamWriter: out _,
                readOnlyFileInfo: out _,
                isLiteralPath: true);
        }
        catch (Exception e) when (e.Data.Contains(typeof(ErrorRecord)))
        {
            // The error record is attached to the exception when thrown to preserve
            // the call stack.
            ErrorRecord? errorRecord = e.Data[typeof(ErrorRecord)] as ErrorRecord;
            if (errorRecord is null)
            {
                throw;
            }

            e.Data.Remove(typeof(ErrorRecord));
            throw new RuntimeException(null, e, errorRecord);
        }

        return new FileBytePipe(fileStream);
    }

    public override Task<Stream> GetStream(CancellationToken cancellationToken) => Task.FromResult(_stream);
}
