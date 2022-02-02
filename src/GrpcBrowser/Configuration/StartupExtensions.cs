using Fluxor;
using GrpcBrowser.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.AspNetCore.Routing;

namespace GrpcBrowser.Configuration
{
    public static class StartupExtensions
    {
        public static void AddGrpcBrowser(this IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMudServices();
            services.AddScoped<GrpcChannelUrlProvider>();
            services.AddFluxor(o => o
                .ScanAssemblies(typeof(StartupExtensions).Assembly)
                .UseReduxDevTools());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void UseGrpcBrowser(this IApplicationBuilder app)
        {
            app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/grpc"), tools =>
            {
                app.UseStaticFiles("/grpc");
            });

            app.UseStaticFiles();
        }

        public static void MapGrpcBrowser(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapBlazorHub("/grpc/_blazor");
            endpoints.MapFallbackToPage("/grpc/{*path:nonfile}", "/_Host");
            endpoints.MapFallbackToPage("/grpc", "/_Host");
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcBrowserWithClient<TClient>(this GrpcServiceEndpointConventionBuilder builder) where TClient : Grpc.Core.ClientBase<TClient>
        {
            GrpcBrowser.AddProtoFirstService<TClient>();

            return builder;
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcBrowserWithService<TServiceInterface>(this GrpcServiceEndpointConventionBuilder builder)
        {
            GrpcBrowser.AddCodeFirstService<TServiceInterface>();

            return builder;
        }
    }
}
