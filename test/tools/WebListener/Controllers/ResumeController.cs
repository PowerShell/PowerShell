// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mvc.Models;


namespace mvc.Controllers
{
    public class ResumeController : Controller
    {
        private static Byte[] FileBytes = new Byte[]{1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20};

        public async void Index()
        {
            SetResumeResponseHeaders();
            string rangeHeader;
            int from = 0;
            int to = FileBytes.Length - 1;
            if (TryGetRangeHeader(out rangeHeader))
            {
                var range = GetRange(rangeHeader);
                if(range.From != null)
                {
                    from = (int)range.From;
                }
                if(range.To != null)
                {
                    to = (int)range.To;
                }

            }
            else
            {
                Response.ContentType = "application/octet-stream";
                Response.StatusCode = 200;
                await Response.Body.WriteAsync(FileBytes, 0, FileBytes.Length);
                return;
            }

            if(to >= FileBytes.Length || from >= FileBytes.Length)
            {
                Response.StatusCode = 416;
                Response.Headers["Content-Range"] = $"bytes */{FileBytes.Length}";
                return;
            }
            else
            {
                Response.ContentType = "application/octet-stream";
                Response.ContentLength = to - from + 1;
                Response.Headers["Content-Range"] = $"bytes {from}-{to}/{FileBytes.Length}";
                Response.StatusCode = 206;
                await Response.Body.WriteAsync(FileBytes, from, (int)Response.ContentLength);
            }
        }

        public async void NoResume() 
        {
            SetResumeResponseHeaders();
            Response.ContentType = "application/octet-stream";
            Response.ContentLength = FileBytes.Length;
            Response.StatusCode = 200;
            await Response.Body.WriteAsync(FileBytes, 0, FileBytes.Length);
        }

        public async void Bytes(int NumberBytes)
        {
            if (NumberBytes > FileBytes.Length || NumberBytes < 0)
            {
                NumberBytes = FileBytes.Length;
            }
            Response.ContentType = "application/octet-stream";
            Response.ContentLength = NumberBytes;
            await Response.Body.WriteAsync(FileBytes, 0, NumberBytes);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private RangeItemHeaderValue GetRange(string rangeHeader)
        {
            return RangeHeaderValue.Parse(rangeHeader).Ranges.FirstOrDefault();
        }

        private void SetResumeResponseHeaders()
        {
            string rangeHeader;
            if (TryGetRangeHeader(out rangeHeader))
            {
                Response.Headers["X-Has-Range"] = "true";
                Response.Headers["X-Request-Range"] = rangeHeader;
            }
            else
            {
                Response.Headers["X-Has-Range"] = "false";
            }
        }

        private bool TryGetRangeHeader(out string rangeHeader)
        {
            var rangeHeaderSv = new StringValues();
            if(Request.Headers.TryGetValue("Range", out rangeHeaderSv))
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
