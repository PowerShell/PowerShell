// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using mvc.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http.Features;

namespace mvc.Controllers
{
    public class ResponseController : Controller
    {
        public string Index()
        {
            string output = string.Empty;
            string contentType = Constants.ApplicationJson;

            StringValues contentTypes;
            if (Request.Query.TryGetValue("contenttype", out contentTypes))
            {
                contentType = contentTypes.FirstOrDefault();
            }

            StringValues statusCodes;
            Int32 statusCode;
            if (Request.Query.TryGetValue("statuscode", out statusCodes) &&
                Int32.TryParse(statusCodes.FirstOrDefault(), out statusCode))
            {
                Response.StatusCode = statusCode;
            }

            StringValues responsePhrase;
            if (Request.Query.TryGetValue("responsephrase", out responsePhrase))
            {
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = responsePhrase.FirstOrDefault();
            }

            StringValues body;
            if (Request.Query.TryGetValue("body", out body))
            {
                output = body.FirstOrDefault();
            }

            StringValues headers;
            if (Request.Query.TryGetValue("headers", out headers))
            {
                try
                {
                    Response.Headers.Clear();
                    JObject jobject = JObject.Parse(headers.FirstOrDefault());
                    foreach (JProperty property in (JToken)jobject)
                    {
                        // Only set Content-Type through contenttype field.
                        if (string.Equals(property.Name, "Content-Type", StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }

                        foreach (string entry in GetSingleOrArray<string>(property.Value))
                        {
                            Response.Headers.Append(property.Name, entry);
                        }
                    }
                }
                catch (Exception ex)
                {
                    output = JsonConvert.SerializeObject(ex);
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    contentType = Constants.ApplicationJson;
                }
            }

            // Content-Type must be applied right before it is sent to the client or MVC will overwrite.
            Response.OnStarting(state =>
                {
                     var httpContext = (HttpContext)state;
                     httpContext.Response.ContentType = contentType;
                     return Task.FromResult(0);
                }, HttpContext);

            Response.ContentLength = Encoding.UTF8.GetBytes(output).Length;

            return output;
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static List<T> GetSingleOrArray<T>(JToken token)
        {
            if (token.HasValues)
            {
                return token.ToObject<List<T>>();
            }
            else
            {
                return new List<T> { token.ToObject<T>() };
            }
        }
    }
}
