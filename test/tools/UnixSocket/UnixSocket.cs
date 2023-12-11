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
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenUnixSocket(args[0]);
            });

            var app = builder.Build();
            app.MapGet("/", () => "Hello World Unix Socket.");

            app.Run();
        }
    }
}
