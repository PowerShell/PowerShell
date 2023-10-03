// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mvc.Models;
using System.IO.Compression;

namespace mvc.Controllers
{
    public class DelayController : Controller
    {
        private static readonly byte[] GenericBytes = "Hello worldHello world"u8.ToArray();

        private static readonly byte[] JsonBytes = """
            {"name1":"value1","name2":"value2","name3":"value3"}
            """u8.ToArray();

        private static readonly byte[] AtomFeed = """
            <?xml version="1.0" encoding="utf-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
            <title>Example Feed</title>
            <link href="http://example.org/"/>
            <updated>2003-12-13T18:30:02Z</updated>
            <author>
                <name>John Doe</name>
            </author>
            <id>urn:uuid:60a76c80-d399-11d9-b93C-0003939e0af6</id>
            <entry>
                <title>Atom-Powered Robots Run Amok</title>
                <link href="http://example.org/2003/12/13/atom03"/>
                <id>urn:uuid:1225c695-cfb8-4ebb-aaaa-80da344efa6a</id>
                <updated>2003-12-13T18:30:02Z</updated>
                <summary>Some text.</summary>
            </entry>
            </feed>
            """u8.ToArray();

        public JsonResult Index(int seconds)
        {
            if (seconds > 0)
            {
                int milliseconds = seconds * 1000;
                Thread.Sleep(milliseconds);
            }

            var getController = new GetController();
            getController.ControllerContext = this.ControllerContext;
            return getController.Index();
        }

        public async Task Stall(int seconds, string contentType, int chunks, bool contentLength, CancellationToken cancellationToken)
        {
            await WriteStallResponse(seconds, contentType, chunks, contentLength, null, null, cancellationToken);
        }

        public async Task StallBrotli(int seconds, string contentType, int chunks, bool contentLength, CancellationToken cancellationToken)
        {
            using var memStream = new MemoryStream();
            using var compressedStream = new BrotliStream(memStream, CompressionLevel.Fastest);
            Response.Headers.ContentEncoding = "br";
            await WriteStallResponse(seconds, contentType, chunks, contentLength, compressedStream, memStream, cancellationToken);
        }

        public async Task StallDeflate(int seconds, string contentType, int chunks, bool contentLength, CancellationToken cancellationToken)
        {
            using var memStream = new MemoryStream();
            using var compressedStream = new DeflateStream(memStream, CompressionLevel.Fastest);
            Response.Headers.ContentEncoding = "deflate";
            await WriteStallResponse(seconds, contentType, chunks, contentLength, compressedStream, memStream, cancellationToken);
        }

        public async Task StallGZip(int seconds, string contentType, int chunks, bool contentLength, CancellationToken cancellationToken)
        {
            using var memStream = new MemoryStream();
            using var compressedStream = new GZipStream(memStream, CompressionLevel.Fastest);
            Response.Headers.ContentEncoding = "gzip";
            await WriteStallResponse(seconds, contentType, chunks, contentLength, compressedStream, memStream, cancellationToken);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task WriteStallResponse(int seconds, string contentType, int chunks, bool contentLength, Stream stream, MemoryStream memStream, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "text/plain";
            }
            else
            {
                contentType = WebUtility.UrlDecode(contentType);
            }

            Response.ContentType = contentType;
            Response.StatusCode = StatusCodes.Status200OK;
            byte[] response;

            if (contentType.Contains("json"))
            {
                response = JsonBytes;
            }
            else if (contentType.Contains("xml"))
            {
                response = AtomFeed;
            }
            else
            {
                response = GenericBytes;
            }

            if (stream is not null && memStream is not null)
            {
                // Generate the compressed data for sending on the response stream
                stream.Write(response);
                stream.Flush();
                stream.Close();
                response = memStream.ToArray();
            }
            if (chunks < 2)
            {
                chunks = 2;
            }
            if (chunks > response.Length)
            {
                throw new InvalidDataException($"Response message is not big enough to break into {chunks} chunks. (Size {response.Length} bytes).");
            }

            if (contentLength)
            {
                Response.ContentLength = response.Length;
            }
            int chunkSize = response.Length / chunks;
            int currentPos = 0;

            // Write each of the content chunks followed by a delay
            // The last segment makes up the remainder of the content if
            // it doesn't divide neatly into the required chunks
            for (int i = 0; i < chunks; i++)
            {
                if (i == chunks - 1)
                {
                    chunkSize = response.Length - currentPos;
                    seconds = 0;
                }
                await Response.Body.WriteAsync(response, currentPos, chunkSize, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                currentPos += chunkSize;
                if (seconds > 0)
                {
                    int milliseconds = seconds * 1000;
                    await Task.Delay(milliseconds);
                }
            }
        }
    }
}
