// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using mvc.Models;

namespace mvc.Controllers
{
    public class MultipartController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public MultipartController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Index(IFormCollection collection)
        {
            if (!Request.HasFormContentType)
            {
                Response.StatusCode = 415;
                Hashtable error = new Hashtable { { "error", "Unsupported media type" } };
                return  Json(error);
            }

            List<Hashtable> fileList = new List<Hashtable>();
            foreach (var file in collection.Files)
            {
                string result = string.Empty;
                if (file.Length > 0)
                {
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        result = reader.ReadToEnd();
                    }
                }

                Hashtable fileHash = new Hashtable
                {
                    {"ContentDisposition", file.ContentDisposition},
                    {"ContentType", file.ContentType},
                    {"FileName", file.FileName},
                    {"Length", file.Length},
                    {"Name", file.Name},
                    {"Content", result},
                    {"Headers", file.Headers}
                };
                fileList.Add(fileHash);
            }

            Hashtable itemsHash = new Hashtable();
            foreach (var key in collection.Keys)
            {
                itemsHash.Add(key, collection[key]);
            }

            MediaTypeHeaderValue mediaContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            Hashtable headers = new Hashtable();
            foreach (var key in Request.Headers.Keys)
            {
                headers.Add(key, string.Join(Constants.HeaderSeparator, Request.Headers[key]));
            }

            Hashtable output = new Hashtable
            {
                {"Files", fileList},
                {"Items", itemsHash},
                {"Boundary", HeaderUtilities.RemoveQuotes(mediaContentType.Boundary).Value},
                {"Headers", headers}
            };
            return Json(output);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
