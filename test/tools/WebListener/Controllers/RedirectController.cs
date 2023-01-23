// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class RedirectController : Controller
    {
        public IActionResult Index(int count)
        {
            string url = Regex.Replace(input: Request.GetDisplayUrl(), pattern: "\\/Redirect.*", replacement: string.Empty, options: RegexOptions.IgnoreCase);
            var destinationIsPresent = Request.Query.TryGetValue("destination", out StringValues destination);
            if (count <= 1)
            {
                url = destinationIsPresent ? destination.FirstOrDefault() : $"{url}/Get/";
            }
            else
            {
                int nextHop = count - 1;
                url = $"{url}/Redirect/{nextHop}";
            }

            var typeIsPresent = Request.Query.TryGetValue("type", out StringValues type);

            if (typeIsPresent && Enum.TryParse(type.FirstOrDefault(), out HttpStatusCode status))
            {
                Response.StatusCode = (int)status;
                url = $"{url}?type={type.FirstOrDefault()}";
                Response.Headers.Add("Location", url);
            }
            else if (typeIsPresent && string.Equals(type.FirstOrDefault(), "relative", StringComparison.InvariantCultureIgnoreCase))
            {
                url = new Uri($"{url}?type={type.FirstOrDefault()}").PathAndQuery;
                Response.Redirect(url, false);
            }
            else if (destinationIsPresent)
            {
                Response.StatusCode = 302;
                Response.Headers.Add("Location", url);
            }
            else
            {
                Response.Redirect(url, false);
            }

            ViewData["Url"] = url;

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
