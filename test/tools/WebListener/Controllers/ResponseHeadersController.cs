// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using mvc.Models;

namespace mvc.Controllers
{
    public class ResponseHeadersController : Controller
    {
        public string Index()
        {
            Hashtable headers = new Hashtable();
            foreach (var key in Request.Query.Keys)
            {
                headers.Add(key, string.Join(Constants.HeaderSeparator, (string)Request.Query[key]));

                if (string.Equals("Content-Type", key, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Content-Type must be applied right before it is sent to the client or MVC will overwrite.
                    string contentType = Request.Query[key];
                    Response.OnStarting(state =>
                    {
                         var httpContext = (HttpContext)state;
                         httpContext.Response.ContentType = contentType;
                         return Task.FromResult(0);
                    }, HttpContext);
                }
                else
                {
                    Response.Headers.TryAdd(key, Request.Query[key]);
                }
            }

            return JsonConvert.SerializeObject(headers);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
