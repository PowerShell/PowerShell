// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Management.Automation;

/// <summary>
/// Represents the transfer of bytes from one <see cref="Stream" /> to another
/// asynchronously.
/// </summary>
internal sealed class AsyncByteStreamTransfer : IDisposable
{
    private const int DefaultBufferSize = 64 * 1024;

    private readonly BytePipe _bytePipe;

    private readonly BytePipe _destinationPipe;

    private readonly CancellationTokenSource _cts = new();

    private Task? _readToBufferTask;

    public AsyncByteStreamTransfer(
        BytePipe bytePipe,
        BytePipe destinationPipe)
    {
        _bytePipe = bytePipe;
        _destinationPipe = destinationPipe;
    }

    public Task EOF => _readToBufferTask ?? Task.CompletedTask;

    public void BeginReadChunks()
    {
        _readToBufferTask = Task.Run(ReadBufferAsync);
    }

    public void Dispose() => _cts.Cancel();

    private async Task ReadBufferAsync()
    {
        Stream stream;
        Stream? destinationStream = null;
        try
        {
            stream = await _bytePipe.GetStream(_cts.Token);
            destinationStream = await _destinationPipe.GetStream(_cts.Token);

            await stream.CopyToAsync(destinationStream, DefaultBufferSize, _cts.Token);
        }
        catch (IOException)
        {
            return;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            destinationStream?.Close();
        }
    }
}
