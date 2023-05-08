// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class AuthController : Controller
    {
        public JsonResult Basic()
        {
            StringValues authorization;
            if (Request.Headers.TryGetValue("Authorization", out authorization))
            {
                var getController = new GetController();
                getController.ControllerContext = this.ControllerContext;
                return getController.Index();
            }
            else
            {
                Response.Headers.Append("WWW-Authenticate", "Basic realm=\"WebListener\"");
                Response.StatusCode = 401;
                return Json("401 Unauthorized");
            }
        }

        public JsonResult Negotiate()
        {
            StringValues authorization;
            if (Request.Headers.TryGetValue("Authorization", out authorization))
            {
                var getController = new GetController();
                getController.ControllerContext = this.ControllerContext;
                return getController.Index();
            }
            else
            {
                Response.Headers.Append("WWW-Authenticate", "Negotiate");
                Response.StatusCode = 401;
                return Json("401 Unauthorized");
            }
        }

        public JsonResult Ntlm()
        {
            StringValues authorization;
            if (Request.Headers.TryGetValue("Authorization", out authorization))
            {
                var getController = new GetController();
                getController.ControllerContext = this.ControllerContext;
                return getController.Index();
            }
            else
            {
                Response.Headers.Append("WWW-Authenticate", "NTLM");
                Response.StatusCode = 401;
                return Json("401 Unauthorized");
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
