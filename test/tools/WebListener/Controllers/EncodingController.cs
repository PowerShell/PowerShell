// Copyright (c) Microsoft Corporation. All rights reserved.
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

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
