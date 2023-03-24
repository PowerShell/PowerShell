// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using mvc.Models;

namespace mvc.Controllers
{
    public class EncodingController : Controller
    {
        public ActionResult Index()
        {
            string url = "/Encoding/Utf8";
            ViewData["Url"] = url;
            Response.Redirect(url, false);
            return View("~/Views/Redirect/Index.cshtml");
        }

        public ActionResult Utf8()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            mediaType.Encoding = Encoding.UTF8;
            return View();
        }

        public async void Cp936()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(936);
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            mediaType.Encoding = encoding;
            Response.ContentType = mediaType.ToString();
            var body = new byte[]
            {
                178,
                226,
                202,
                212,
                49,
                50,
                51
            };
            await Response.Body.WriteAsync(body, 0, body.Length);
        }
        public async void Utf8BOM()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            Response.ContentType = mediaType.ToString();
            byte[] body = Encoding.UTF8.GetPreamble() + Encoding.UTF8.GetBytes("hello");

            await Response.Body.WriteAsync(body, 0, body.Length);
        }

        public async void Unicode()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            Response.ContentType = mediaType.ToString();
            byte[] body = Encoding.Unicode.GetPreamble() + Encoding.Unicode.GetBytes("hello");

            await Response.Body.WriteAsync(body, 0, body.Length);
        }

        public async void BigEndianUnicode()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            Response.ContentType = mediaType.ToString();
            byte[] body = Encoding.BigEndianUnicode.GetPreamble() + Encoding.BigEndianUnicode.GetBytes("hello");

            await Response.Body.WriteAsync(body, 0, body.Length);
        }

        public async void Utf32()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            Response.ContentType = mediaType.ToString();
            byte[] body = Encoding.UTF32.GetPreamble() + Encoding.UTF32.GetBytes("hello");

            await Response.Body.WriteAsync(body, 0, body.Length);
        }

        public async void Utf32BE()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/html");
            Response.ContentType = mediaType.ToString();

            UTF32Encoding Utf32BE = new(bigEndian: true, byteOrderMark: true);
            byte[] body = Utf32BE.GetPreamble() + Utf32BE.GetBytes("hello");

            await Response.Body.WriteAsync(body, 0, body.Length);
        }

        

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
