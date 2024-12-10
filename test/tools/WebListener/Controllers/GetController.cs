// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using mvc.Models;

namespace mvc.Controllers
{
    public class GetController : Controller
    {
        public JsonResult Index()
        {
            Hashtable args = new Hashtable();
            foreach (var key in Request.Query.Keys)
            {
                args.Add(key, string.Join(Constants.HeaderSeparator, (string)Request.Query[key]));
            }

            Hashtable headers = new Hashtable();
            foreach (var key in Request.Headers.Keys)
            {
                headers.Add(key, string.Join(Constants.HeaderSeparator, (string)Request.Headers[key]));
            }

            Hashtable output = new Hashtable
            {
                { "args", args },
                { "headers", headers },
                { "origin", Request.HttpContext.Connection.RemoteIpAddress.ToString() },
                { "url", UriHelper.GetDisplayUrl(Request) },
                { "query", Request.QueryString.ToUriComponent() },
                { "method", Request.Method },
                { "protocol", Request.Protocol }
            };

            if (Request.HasFormContentType)
            {
                Hashtable form = new Hashtable();
                foreach (var key in Request.Form.Keys)
                {
                    form.Add(key, Request.Form[key]);
                }

                output["form"] = form;
            }

            string data = new StreamReader(Request.Body).ReadToEnd();
            if (!string.IsNullOrEmpty(data))
            {
                output["data"] = data;
            }

            return Json(output);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
