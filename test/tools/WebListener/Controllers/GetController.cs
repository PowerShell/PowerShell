using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
                args.Add(key, String.Join(Constants.HeaderSeparator, Request.Query[key]));
            }
            Hashtable headers = new Hashtable();
            foreach (var key in Request.Headers.Keys)
            {
                headers.Add(key, String.Join(Constants.HeaderSeparator, Request.Headers[key]));
            }
            Hashtable output = new Hashtable
            {
                {"args"   , args},
                {"headers", headers},
                {"origin" , Request.HttpContext.Connection.RemoteIpAddress.ToString()},
                {"url"    , UriHelper.GetDisplayUrl(Request)}
            };
            return Json(output);
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
