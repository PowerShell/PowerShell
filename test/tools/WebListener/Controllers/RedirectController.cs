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
            string url = Regex.Replace(input: Request.GetDisplayUrl(), pattern: "\\/Redirect.*", replacement: "", options: RegexOptions.IgnoreCase);
            if (count <= 1)
            {
                url = $"{url}/Get/";
            }
            else
            {
                int nextHop = count - 1;
                url = $"{url}/Redirect/{nextHop}";
            }

            if (Request.Query.TryGetValue("type", out StringValues type) && Enum.TryParse(type.FirstOrDefault(), out HttpStatusCode status))
            {
                Response.StatusCode = (int)status;
                url = $"{url}?type={type.FirstOrDefault()}";
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
