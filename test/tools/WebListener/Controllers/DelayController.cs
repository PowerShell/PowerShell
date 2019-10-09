// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using mvc.Models;

namespace mvc.Controllers
{
    public class DelayController : Controller
    {
        public JsonResult Index(int seconds)
        {
            if (seconds > 0){
                int milliseconds = seconds * 1000;
                Thread.Sleep(milliseconds);
            }

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
