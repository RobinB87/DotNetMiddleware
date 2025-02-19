﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DotNetMiddleware
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true);

            Configuration = builder.Build();
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.AddCors();

            services.AddRouting();

            services.AddDistributedMemoryCache();
            services.AddSession();

            var myOptions = Configuration.GetSection("MyMiddlewareOptionsSection");
            services.Configure<MyCustomMiddlewareOptions>(o => o.OptionOne = myOptions["OptionOne"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseSession();

            var routeBuilder = new RouteBuilder(app);
            routeBuilder.MapMiddlewareGet("greeting/{name}", appBuilder =>
            {
                appBuilder.Run(async (context) =>
                {
                    var name = context.GetRouteValue("name");
                    await context.Response.WriteAsync($"Hey there, {name}");
                });
            });

            app.UseRouter(routeBuilder.Build());

            app.UseCors((builder) =>
            {
                builder.WithOrigins("http://localhost:5000");
            });

            var newline = Environment.NewLine;
            app.Use(async (context, next) =>
            {
                context.Session.SetString("my message", "you have completed setting a string in session");

                await context.Response.WriteAsync($"Hello from component one!{newline}");
                await next.Invoke();
                await context.Response.WriteAsync($"Hello from component one again!{newline}");

                context.Session.GetString("my message");
            });

            app.UseMyCustomMiddleware();

            app.Map("/mymapbranch", (appBuilder) =>
            {
                appBuilder.Use(async (context, next) =>
                {
                    await next.Invoke();
                });

                appBuilder.Run(async (context) =>
                {
                    await context.Response.WriteAsync($"Greetings from my Map branche!{newline}");
                });
            });

            app.MapWhen(context => context.Request.Query.ContainsKey("querybranch"), (appBuilder) =>
            {
                appBuilder.Run(async (context) =>
                {
                    await context.Response.WriteAsync($"You have arrived at your MapWhen branch!{newline}");
                });
            });

            // Eg: http://localhost:5000/test.timetable
            app.MapWhen(context => context.Request.Path.ToString().EndsWith(".timetable"),
                appBuilder =>
                {
                    appBuilder.UseMyHandlerMiddleware();
                });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync($"Hello World! {context.Items["message"]}{newline}");
            });
        }
    }
}
