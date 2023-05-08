// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class DosController : Controller
    {
        public string Index()
        {
            string output = string.Empty;
            string contentType = Constants.ApplicationJson;

            Response.StatusCode = 200;

            StringValues dosType;
            if (Request.Query.TryGetValue("dosType", out dosType))
            {
                output = dosType.FirstOrDefault();
            }

            StringValues dosLengths;
            Int32 dosLength = 1;
            if (Request.Query.TryGetValue("dosLength", out dosLengths))
            {
                Int32.TryParse(dosLengths.FirstOrDefault(), out dosLength);
            }

            string body = string.Empty;
            switch (dosType)
            {
                case "img":
                    contentType = "text/html; charset=utf8";
                    body = "<img" + (new string(' ', dosLength));
                    break;
                // This is not really a DOS test, but this is the best place for it at present.
                case "img-attribute":
                    contentType = "text/html; charset=utf8";
                    body = "<img src=\"https://fakesite.org/image.png\" id=\"mainImage\" class=\"lightbox\">";
                    break;
                case "charset":
                    contentType = "text/html; charset=melon";
                    body = "<meta " + (new string('.', dosLength));
                    break;
                default:
                    throw new InvalidOperationException("Invalid dosType: " + dosType);
            }

            // Content-Type must be applied right before it is sent to the client or MVC will overwrite.
            Response.OnStarting(state =>
                {
                    var httpContext = (HttpContext)state;
                    httpContext.Response.ContentType = contentType;
                    return Task.FromResult(0);
                }, HttpContext);

            Response.ContentLength = Encoding.UTF8.GetBytes(body).Length;

            return body;
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
