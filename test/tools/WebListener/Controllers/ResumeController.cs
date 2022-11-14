// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using mvc.Models;

using RangeItemHeaderValue = System.Net.Http.Headers.RangeItemHeaderValue;
using RangeHeaderValue = System.Net.Http.Headers.RangeHeaderValue;

namespace mvc.Controllers
{
    public class ResumeController : Controller
    {
        private static readonly byte[] FileBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

        public async void Index()
        {
            SetResumeResponseHeaders();
            string rangeHeader;
            int from = 0;
            int to = FileBytes.Length - 1;
            if (TryGetRangeHeader(out rangeHeader))
            {
                var range = GetRange(rangeHeader);
                if (range.From != null)
                {
                    from = (int)range.From;
                }

                if (range.To != null)
                {
                    to = (int)range.To;
                }

            }
            else
            {
                Response.ContentType = MediaTypeNames.Application.Octet;
                Response.StatusCode = StatusCodes.Status200OK;
                await Response.Body.WriteAsync(FileBytes, 0, FileBytes.Length);
                return;
            }

            if (to >= FileBytes.Length || from >= FileBytes.Length)
            {
                Response.StatusCode = StatusCodes.Status416RequestedRangeNotSatisfiable;
                Response.Headers[HeaderNames.ContentRange] = $"bytes */{FileBytes.Length}";
                return;
            }
            else
            {
                Response.ContentType = MediaTypeNames.Application.Octet;
                Response.ContentLength = to - from + 1;
                Response.Headers[HeaderNames.ContentRange] = $"bytes {from}-{to}/{FileBytes.Length}";
                Response.StatusCode = StatusCodes.Status206PartialContent;
                await Response.Body.WriteAsync(FileBytes, from, (int)Response.ContentLength);
            }
        }

        public async void NoResume()
        {
            SetResumeResponseHeaders();
            Response.ContentType = MediaTypeNames.Application.Octet;
            Response.ContentLength = FileBytes.Length;
            Response.StatusCode = StatusCodes.Status200OK;
            await Response.Body.WriteAsync(FileBytes, 0, FileBytes.Length);
        }

        public async void Bytes(int NumberBytes)
        {
            if (NumberBytes > FileBytes.Length || NumberBytes < 0)
            {
                NumberBytes = FileBytes.Length;
            }

            Response.ContentType = MediaTypeNames.Application.Octet;
            Response.ContentLength = NumberBytes;
            await Response.Body.WriteAsync(FileBytes, 0, NumberBytes);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static RangeItemHeaderValue GetRange(string rangeHeader)
        {
            return RangeHeaderValue.Parse(rangeHeader).Ranges.FirstOrDefault();
        }

        private void SetResumeResponseHeaders()
        {
            string rangeHeader;
            if (TryGetRangeHeader(out rangeHeader))
            {
                Response.Headers["X-WebListener-Has-Range"] = "true";
                Response.Headers["X-WebListener-Request-Range"] = rangeHeader;
            }
            else
            {
                Response.Headers["X-WebListener-Has-Range"] = "false";
            }
        }

        private bool TryGetRangeHeader(out string rangeHeader)
        {
            var rangeHeaderSv = new StringValues();
            if (Request.Headers.TryGetValue("Range", out rangeHeaderSv))
            {
                rangeHeader = rangeHeaderSv.FirstOrDefault();
                return true;
            }
            else
            {
                rangeHeader = string.Empty;
                return false;
            }
        }
    }
}
