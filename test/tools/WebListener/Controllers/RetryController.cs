// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class RetryController : Controller
    {
        // Dictionary for sessionId as key and failureCode, failureCount and currentFailCount as the value.
        private static Dictionary<string, Tuple<int, int, int>> retryInfo;

        public JsonResult Retry(string sessionId, int failureCode, int failureCount)
        {
            int responseCode = 200;

            if(retryInfo == null)
            {
                retryInfo = new Dictionary<string, Tuple<int, int, int>>();
            }

            if(retryInfo.TryGetValue(sessionId, out Tuple<int, int, int> retry))
            {
                if(retry.Item3 > 0)
                {
                    responseCode = retry.Item1;
                    retryInfo[sessionId] = Tuple.Create(retry.Item1, retry.Item2, retry.Item3 - 1);
                }
                else
                {
                    retryInfo.Remove(sessionId);
                }
            }
            else
            {
                //initialize the currentFailCount as failureCount - 1 to account for the first fail response.
                var newRetryInfoItem = Tuple.Create(failureCode, failureCount, failureCount - 1);
                retryInfo.Add(sessionId, newRetryInfoItem);
                responseCode = failureCode;
            }

            Response.StatusCode = responseCode;

            if(Response.StatusCode != 200)
            {
                Hashtable error = new Hashtable {{"error", $"Error: HTTP - {failureCode} occurred."}};
                return Json(error);
            }
            else
            {
                if(retry != null)
                {
                    var resp = new Hashtable {{"totalRetries", retry.Item2}};
                    return Json(resp);
                }

                return Json("200: Status OK");
            }
        }

        public IActionResult Error()

        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
