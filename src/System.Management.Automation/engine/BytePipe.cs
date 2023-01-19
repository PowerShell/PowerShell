// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics;
using System.IO;

namespace System.Management.Automation;

internal abstract class BytePipe
{
    public abstract Stream GetStream();

    internal AsyncByteStreamDrainer Bind(BytePipe bytePipe)
    {
        Debug.Assert(bytePipe is not null);
        return new AsyncByteStreamDrainer(
            bytePipe,
            (bytes, _) => GetStream().Write(bytes),
            callbackArg: null,
            () => GetStream().Close());
    }
}

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

    public override Stream GetStream() => _nativeCommand.GetStream(_stdout);
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

    public override Stream GetStream() => _stream;
}
