// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace mvc.Controllers
{
    internal sealed class DeflateFilter : ResultFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            using (var memoryStream = new MemoryStream())
            {
                var responseStream = httpContext.Response.Body;
                httpContext.Response.Body = memoryStream;

                await next();

                using (var compressedStream = new DeflateStream(responseStream, CompressionLevel.Fastest))
                {
                    httpContext.Response.Headers.Append("Content-Encoding", new[] { "deflate" });
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await memoryStream.CopyToAsync(compressedStream);
                }
            }
        }
    }
}
