// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Buffers;
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
    private const int DefaultBufferSize = 1024;

    private readonly BytePipe _bytePipe;

    private readonly BytePipe _destinationPipe;

    private readonly Memory<byte> _buffer;

    private readonly CancellationTokenSource _cts = new();

    private Task? _readToBufferTask;

    public AsyncByteStreamTransfer(
        BytePipe bytePipe,
        BytePipe destinationPipe)
    {
        _bytePipe = bytePipe;
        _destinationPipe = destinationPipe;
        _buffer = new byte[DefaultBufferSize];
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
            try
            {
                stream = await _bytePipe.GetStream(_cts.Token);
                destinationStream = await _destinationPipe.GetStream(_cts.Token);
            }
            catch (IOException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(_buffer, _cts.Token);
                    if (bytesRead is 0)
                    {
                        break;
                    }
                }
                catch (IOException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                destinationStream.Write(_buffer.Span.Slice(0, bytesRead));
            }
        }
        finally
        {
            destinationStream?.Close();
        }
    }
}
