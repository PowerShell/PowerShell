// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace mvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Count() != 6)
            {
                System.Console.WriteLine("Required: <CertificatePath> <CertificatePassword> <HTTPPortNumber> <HTTPSPortNumberTls2> <HTTPSPortNumberTls11> <HTTPSPortNumberTls>");
                Environment.Exit(1); 
            }

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>().UseKestrel(options =>
                {
                   options.Listen(IPAddress.Loopback, int.Parse(args[2]));
                   options.Listen(IPAddress.Loopback, int.Parse(args[3]), listenOptions =>
                   {
                       var certificate = new X509Certificate2(args[0], args[1]);
                       HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                       httpsOption.SslProtocols = SslProtocols.Tls12;
                       httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                       httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => {return true;};
                       httpsOption.CheckCertificateRevocation = false;
                       httpsOption.ServerCertificate = certificate;
                       listenOptions.UseHttps(httpsOption);
                   });
                   options.Listen(IPAddress.Loopback, int.Parse(args[4]), listenOptions =>
                   {
                       var certificate = new X509Certificate2(args[0], args[1]);
                       HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                       httpsOption.SslProtocols = SslProtocols.Tls11;
                       httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                       httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => {return true;};
                       httpsOption.CheckCertificateRevocation = false;
                       httpsOption.ServerCertificate = certificate;
                       listenOptions.UseHttps(httpsOption);
                   });
                   options.Listen(IPAddress.Loopback, int.Parse(args[5]), listenOptions =>
                   {
                       var certificate = new X509Certificate2(args[0], args[1]);
                       HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                       httpsOption.SslProtocols = SslProtocols.Tls;
                       httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                       httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => {return true;};
                       httpsOption.CheckCertificateRevocation = false;
                       httpsOption.ServerCertificate = certificate;
                       listenOptions.UseHttps(httpsOption);
                   });
                })
                .Build();
    }
}
