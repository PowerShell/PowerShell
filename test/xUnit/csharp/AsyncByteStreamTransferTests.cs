// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PSTests.Parallel
{
    public static class AsyncByteStreamTransferTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(65539)]
        public static async Task TransfersCompletePayload(int length)
        {
            byte[] expected = new byte[length];
            new Random(42).NextBytes(expected);
            using var source = new MemoryStream(expected);
            using var destination = new MemoryStream();
            using var transfer = new AsyncByteStreamTransfer(new TestBytePipe(source), new TestBytePipe(destination));

            transfer.BeginReadChunks();
            await transfer.EOF.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(expected, destination.ToArray());
            Assert.False(destination.CanWrite);
        }

        [Fact]
        public static async Task TransfersShortReadsWithoutDroppingBytes()
        {
            byte[] expected = new byte[65539];
            new Random(42).NextBytes(expected);
            using var source = new ShortReadStream(expected);
            using var destination = new MemoryStream();
            using var transfer = new AsyncByteStreamTransfer(new TestBytePipe(source), new TestBytePipe(destination));

            transfer.BeginReadChunks();
            await transfer.EOF.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(expected, destination.ToArray());
        }

        [Fact]
        public static async Task CancelsPendingReadAndClosesDestination()
        {
            using var source = new PendingReadStream();
            using var destination = new MemoryStream();
            using var transfer = new AsyncByteStreamTransfer(new TestBytePipe(source), new TestBytePipe(destination));

            transfer.BeginReadChunks();
            await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            transfer.Dispose();
            await transfer.EOF.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.False(destination.CanWrite);
            Assert.Empty(destination.ToArray());
        }

        private sealed class TestBytePipe : BytePipe
        {
            private readonly Stream _stream;

            internal TestBytePipe(Stream stream) => _stream = stream;

            public override Task<Stream> GetStream(CancellationToken cancellationToken) => Task.FromResult(_stream);
        }

        private sealed class ShortReadStream : MemoryStream
        {
            internal ShortReadStream(byte[] bytes)
                : base(bytes)
            {
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                => base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, 7)), cancellationToken);
        }

        private sealed class PendingReadStream : MemoryStream
        {
            internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                Started.SetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
            }
        }
    }
}
