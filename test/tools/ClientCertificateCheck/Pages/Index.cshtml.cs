using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace ClientCertificateCheck.Pages
{
    public class IndexModel : PageModel
    {
        public string CertSubject;
        public string CertNotAfter;
        public string CertNotBefore;
        public string CertIssuer;
        public string CertIssuerName;
        public string CertSubjectName;
        public string CertThumbprint;
        public string Status = "FAILED";
        
        public void OnGet()
        {
            if(null == HttpContext.Connection.ClientCertificate){
                return;
            }
            CertSubject = HttpContext.Connection.ClientCertificate.Subject;
            CertNotAfter = HttpContext.Connection.ClientCertificate.NotAfter.ToString();
            CertNotBefore = HttpContext.Connection.ClientCertificate.NotBefore.ToString();
            CertIssuer = HttpContext.Connection.ClientCertificate.Issuer;
            CertIssuerName = HttpContext.Connection.ClientCertificate.IssuerName.Name;
            CertSubjectName = HttpContext.Connection.ClientCertificate.SubjectName.Name;
            CertThumbprint = HttpContext.Connection.ClientCertificate.Thumbprint;
            if (!string.IsNullOrEmpty(CertThumbprint))
            {
                Status = "OK";
            }
        }
    }
}
