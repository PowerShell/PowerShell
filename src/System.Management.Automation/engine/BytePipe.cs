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

internal sealed class NativeCommandProcessorBytePipe : BytePipe
{
    private readonly NativeCommandProcessor _nativeCommand;

    internal NativeCommandProcessorBytePipe(NativeCommandProcessor nativeCommand)
    {
        Debug.Assert(nativeCommand is not null);
        _nativeCommand = nativeCommand;
    }

    public override Stream GetStream() => _nativeCommand.GetInputStream();
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
        FileStream fileStream;
        try
        {
            PathUtils.MasterStreamOpen(
                fileName,
                null,
                false,
                append,
                true,
                false,
                out fileStream,
                out _,
                out _,
                true);
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

    public override Stream GetStream() => _stream;
}
