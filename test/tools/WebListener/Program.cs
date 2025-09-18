// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;

namespace mvc
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 7)
            {
                Console.WriteLine("Required: <CertificatePath> <CertificatePassword> <HTTPPortNumber> <HTTPSPortNumberTls12> <HTTPSPortNumberTls11> <HTTPSPortNumberTls> <HTTPSPortNumberTls12>");
                Environment.Exit(1);
            }

            BuildHost(args).Run();
        }

        public static IHost BuildHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options =>
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
#pragma warning disable SYSLIB0057
                                var certificate = new X509Certificate2(args[0], args[1]);
#pragma warning restore SYSLIB0057

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
#pragma warning disable SYSLIB0057
                                var certificate = new X509Certificate2(args[0], args[1]);
#pragma warning restore SYSLIB0057

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
#pragma warning disable SYSLIB0057
                                var certificate = new X509Certificate2(args[0], args[1]);
#pragma warning restore SYSLIB0057

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
#pragma warning disable SYSLIB0057
                                var certificate = new X509Certificate2(args[0], args[1]);
#pragma warning restore SYSLIB0057

                                HttpsConnectionAdapterOptions httpsOption = new HttpsConnectionAdapterOptions();
                                httpsOption.SslProtocols = SslProtocols.Tls13;
                                httpsOption.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                                httpsOption.ClientCertificateValidation = (inCertificate, inChain, inPolicy) => { return true; };
                                httpsOption.CheckCertificateRevocation = false;
                                httpsOption.ServerCertificate = certificate;
                                listenOptions.UseHttps(httpsOption);
                                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                            });
                    });
                })
                .Build();
    }
}
