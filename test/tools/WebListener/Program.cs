// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace mvc
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                System.Console.WriteLine("Required: <CertificatePath> <CertificatePassword> <HTTPPortNumber> <HTTPSPortNumberTls12> <HTTPSPortNumberTls11> <HTTPSPortNumberTls> <HTTPSPortNumberTls12>");
                Environment.Exit(1);
            }

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>().UseKestrel(options =>
                {
                    options.AllowSynchronousIO = true;

                    options.Listen(
                        IPAddress.Loopback,
                        int.Parse(args[2]),
                        listenOptions =>
                        {
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });

                    options.Listen(
                        IPAddress.Loopback,
                        int.Parse(args[3]),
                        listenOptions =>
                        {
                            var certificate = new X509Certificate2(args[0], args[1]);
                            HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                            httpsOption.SslProtocols = SslProtocols.Tls12;
                            httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => { return true; };
                            httpsOption.CheckCertificateRevocation = false;
                            httpsOption.ServerCertificate = certificate;
                            listenOptions.UseHttps(httpsOption);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });

                    options.Listen(
                        IPAddress.Loopback,
                        int.Parse(args[4]),
                        listenOptions =>
                        {
                            var certificate = new X509Certificate2(args[0], args[1]);
                            HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();

                            // TLS 1.1 is obsolete. Using this value now defaults to TLS 1.2.
                            httpsOption.SslProtocols = SslProtocols.Tls12;

                            httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => { return true; };
                            httpsOption.CheckCertificateRevocation = false;
                            httpsOption.ServerCertificate = certificate;
                            listenOptions.UseHttps(httpsOption);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });

                    options.Listen(
                        IPAddress.Loopback,
                        int.Parse(args[5]),
                        listenOptions =>
                        {
                            var certificate = new X509Certificate2(args[0], args[1]);
                            HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();

                            // TLS is obsolete. Using this value now defaults to TLS 1.2.
                            httpsOption.SslProtocols = SslProtocols.Tls12;

                            httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => { return true; };
                            httpsOption.CheckCertificateRevocation = false;
                            httpsOption.ServerCertificate = certificate;
                            listenOptions.UseHttps(httpsOption);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });

                    options.Listen(
                        IPAddress.Loopback,
                        int.Parse(args[6]),
                        listenOptions =>
                        {
                            var certificate = new X509Certificate2(args[0], args[1]);
                            HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                            httpsOption.SslProtocols = SslProtocols.Tls13;
                            httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                            httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => { return true; };
                            httpsOption.CheckCertificateRevocation = false;
                            httpsOption.ServerCertificate = certificate;
                            listenOptions.UseHttps(httpsOption);
                            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                        });
                })
                .Build();
    }
}
