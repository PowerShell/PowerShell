// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using mvc.Models;

namespace mvc.Controllers
{
    public class CertController : Controller
    {
        public JsonResult Index()
        {
            // X509Certificate2 objects do not serialize as JSON. Create a HashTable instead
            Hashtable output = new Hashtable
            {
                {"Status", "FAILED"}
            };
            if (HttpContext.Connection.ClientCertificate != null)
            {
                output = new Hashtable
                {
                    {"Status"      , "OK"},
                    {"Thumbprint"  , HttpContext.Connection.ClientCertificate.Thumbprint},
                    {"Subject"     , HttpContext.Connection.ClientCertificate.Subject},
                    {"SubjectName" , HttpContext.Connection.ClientCertificate.SubjectName.Name},
                    {"Issuer"      , HttpContext.Connection.ClientCertificate.Issuer},
                    {"IssuerName"  , HttpContext.Connection.ClientCertificate.IssuerName.Name},
                    {"NotAfter"    , HttpContext.Connection.ClientCertificate.NotAfter},
                    {"NotBefore"   , HttpContext.Connection.ClientCertificate.NotBefore}
                };
            }

            return Json(output);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
