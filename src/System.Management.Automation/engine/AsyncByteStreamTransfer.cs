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

    private readonly SpanAction<byte, object?> _callback;

    private readonly Action _callbackCompleted;

    private readonly object? _callbackArg;

    private readonly BytePipe _bytePipe;

    private readonly Memory<byte> _buffer;

    private readonly CancellationTokenSource _cts = new();

    private Task? _readToBufferTask;

    public AsyncByteStreamTransfer(BytePipe bytePipe, SpanAction<byte, object?> callback, object? callbackArg, Action completedCallback)
    {
        _bytePipe = bytePipe;
        _callback = callback;
        _callbackArg = callbackArg;
        _callbackCompleted = completedCallback;
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
        while (true)
        {
            int bytesRead;
            try
            {
                bytesRead = await _bytePipe.GetStream().ReadAsync(_buffer, _cts.Token);
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

            _callback(_buffer.Span.Slice(0, bytesRead), _callbackArg);
        }

        _callbackCompleted();
    }
}
