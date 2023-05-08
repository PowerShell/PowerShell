// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class RetryController : Controller
    {
        // Dictionary for sessionId as key and failureCode, failureCount and failureResponsesSent as the value.
        private static Dictionary<string, Tuple<int, int, int>> retryInfo;

        public JsonResult Retry(string sessionId, int failureCode, int failureCount, int retryAfter = 0)
        {
            retryInfo ??= new Dictionary<string, Tuple<int, int, int>>();

            if (failureCode == 409 && retryAfter > 0)
            {
                Response.Headers.Append("Retry-After", $"{retryAfter}");
            }

            if (retryInfo.TryGetValue(sessionId, out Tuple<int, int, int> retry))
            {
                // if failureResponsesSent is less than failureCount
                if (retry.Item3 < retry.Item2)
                {
                    Response.StatusCode = retry.Item1;
                    retryInfo[sessionId] = Tuple.Create(retry.Item1, retry.Item2, retry.Item3 + 1);
                    Hashtable error = new Hashtable { { "error", $"Error: HTTP - {retry.Item1} occurred." } };
                    return Json(error);
                }
                else
                {
                    retryInfo.Remove(sessionId);

                    // echo back sessionId for POST test.
                    var resp = new Hashtable { { "failureResponsesSent", retry.Item3 }, { "sessionId", sessionId } };
                    return Json(resp);
                }
            }
            else
            {
                // initialize the failureResponsesSent as 1.
                var newRetryInfoItem = Tuple.Create(failureCode, failureCount, 1);
                retryInfo.Add(sessionId, newRetryInfoItem);
                Response.StatusCode = failureCode;
                Hashtable error = new Hashtable { { "error", $"Error: HTTP - {failureCode} occurred." } };
                return Json(error);
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
