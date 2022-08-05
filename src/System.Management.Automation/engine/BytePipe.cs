// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Management.Automation;

internal interface IStreamSource
{
    Stream GetStream();
}

internal sealed class StdOutStreamSource : IStreamSource
{
    private readonly Process _process;

    public StdOutStreamSource(Process process) => _process = process;

    public Stream GetStream() => _process.StandardOutput.BaseStream;
}

internal abstract class BytePipe : IStreamSource
{
    private readonly object _syncObject = new();

    private int _refCount;

    public abstract Stream GetStream();

    internal AsyncByteStreamDrainer Bind(IStreamSource sourceStream)
    {
        Debug.Assert(sourceStream is not null);
        Interlocked.Increment(ref _refCount);
        try
        {
            return new AsyncByteStreamDrainer(
                sourceStream,
                (bytes, _) =>
                {
                    lock (_syncObject)
                    {
                        GetStream().Write(bytes);
                    }
                },
                callbackArg: null,
                () =>
                {
                    if (Interlocked.Decrement(ref _refCount) <= 0)
                    {
                        GetStream().Close();
                    }
                });
        }
        catch
        {
            Interlocked.Decrement(ref _refCount);
            throw;
        }
    }
}

internal sealed class ProcessBytePipe : BytePipe
{
    private readonly Process _process;

    internal ProcessBytePipe(Process process)
    {
        Debug.Assert(process is not null);
        _process = process;
    }

    public override Stream GetStream() => _process.StandardInput.BaseStream;
}

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
        return new FileBytePipe(
            new FileStream(
                fileName,
                append ? FileMode.Append : FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete));
    }

    public override Stream GetStream() => _stream;
}
