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
    public class RedirectController : Controller
    {
        public IActionResult Index(int count)
        {
            string url;
            if (count <= 1)
            {
                url = "/Get/";
            }
            else
            {
                int nextHop = count - 1;
                url = String.Format("/Redirect/{0}", nextHop);
            }
            Response.Redirect(url, false);
            ViewData["Url"] = url;
            return View();
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
