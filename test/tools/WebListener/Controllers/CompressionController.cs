// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mvc.Models;

namespace mvc.Controllers
{
    public class CompressionController : Controller
    {
        public ActionResult Index()
        {
            string url = "/Compression/Gzip";
            ViewData["Url"] = url;
            Response.Redirect(url, false);
            return View("~/Views/Redirect/Index.cshtml");
        }

        [GzipFilter]
        public JsonResult Gzip()
        {
            var getController = new GetController();
            getController.ControllerContext = this.ControllerContext;            
            return getController.Index();
        }

        [DeflateFilter]
        public JsonResult Deflate()
        {
            var getController = new GetController();
            getController.ControllerContext = this.ControllerContext;            
            return getController.Index();
        }

        [BrotliFilter]
        public JsonResult Brotli()
        {
            var getController = new GetController();
            getController.ControllerContext = this.ControllerContext;            
            return getController.Index();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
