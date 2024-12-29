// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace mvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "resume_bytes",
                    template: "Resume/Bytes/{NumberBytes?}",
                    defaults: new { controller = "Resume", action = "Bytes" });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "redirect",
                    template: "Redirect/{count?}",
                    defaults: new { controller = "Redirect", action = "Index" });
                routes.MapRoute(
                    name: "delay",
                    template: "Delay/{seconds?}",
                    defaults: new { controller = "Delay", action = "Index" });
                routes.MapRoute(
                    name: "stall",
                    template: "Stall/{seconds?}/{contentType?}",
                    defaults: new { controller = "Delay", action = "Stall" });
                routes.MapRoute(
                    name: $"stallbrotli",
                    template: "StallBrotli/{seconds?}/{contentType?}",
                    defaults: new { controller = "Delay", action = $"StallBrotli" });
                routes.MapRoute(
                    name: $"stalldeflate",
                    template: "StallDeflate/{seconds?}/{contentType?}",
                    defaults: new { controller = "Delay", action = $"StallDeflate" });
                routes.MapRoute(
                    name: $"stallgzip",
                    template: "StallGZip/{seconds?}/{contentType?}",
                    defaults: new { controller = "Delay", action = $"StallGZip" });
                routes.MapRoute(
                    name: "post",
                    template: "Post",
                    defaults: new { controller = "Get", action = "Index" },
                    constraints: new RouteValueDictionary(new { httpMethod = new HttpMethodRouteConstraint("POST") }));
                routes.MapRoute(
                    name: "put",
                    template: "Put",
                    defaults: new { controller = "Get", action = "Index" },
                    constraints: new RouteValueDictionary(new { httpMethod = new HttpMethodRouteConstraint("PUT") }));
                routes.MapRoute(
                    name: "patch",
                    template: "Patch",
                    defaults: new { controller = "Get", action = "Index" },
                    constraints: new RouteValueDictionary(new { httpMethod = new HttpMethodRouteConstraint("PATCH") }));
                routes.MapRoute(
                    name: "delete",
                    template: "Delete",
                    defaults: new { controller = "Get", action = "Index" },
                    constraints: new RouteValueDictionary(new { httpMethod = new HttpMethodRouteConstraint("DELETE") }));
                routes.MapRoute(
                    name: "retry",
                    template: "Retry/{sessionId?}/{failureCode?}/{failureCount?}",
                    defaults: new { controller = "Retry", action = "Retry" });
            });
        }
    }
}
