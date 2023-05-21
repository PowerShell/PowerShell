// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using static Microsoft.AspNetCore.Hosting.WebHostBuilderKestrelExtensions;

namespace UnixSocket
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            const string UnixSocketPath = "/tmp/UnixSocket.sock";

            if (!Directory.Exists("/tmp"))
            {
                Directory.CreateDirectory("/tmp");
            }

            if (File.Exists(UnixSocketPath))
            {
                File.Delete(UnixSocketPath);
            }

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenUnixSocket(UnixSocketPath);
            });

            var app = builder.Build();
            app.MapGet("/", () => "Hello World Unix Socket.");

            app.Run();
        }
    }
}
