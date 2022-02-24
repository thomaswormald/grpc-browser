using Fluxor;
using GrpcBrowser.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.AspNetCore.Routing;
using BlazorDownloadFile;
using System;
using System.Collections.Generic;

namespace GrpcBrowser.Configuration
{
    public static class StartupExtensions
    {
        public class GrpcBrowserOptions
        {
            public List<IRequestInterceptor> GlobalInterceptors { get; set; } = new List<IRequestInterceptor>();
        }

        public static void AddGrpcBrowser(this IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMudServices();
            services.AddScoped<GrpcChannelUrlProvider>();
            services.AddFluxor(o => o
                .ScanAssemblies(typeof(StartupExtensions).Assembly)
                .UseReduxDevTools());
            services.AddBlazorDownloadFile(ServiceLifetime.Scoped);
        }

        public static void UseGrpcBrowser(this IApplicationBuilder app, Action<GrpcBrowserOptions> optionsAction = null)
        {
            var options = new GrpcBrowserOptions();
            optionsAction?.Invoke(options);
            ConfiguredGrpcServices.GlobalRequestInterceptors = ConfiguredGrpcServices.GlobalRequestInterceptors.AddRange(options.GlobalInterceptors);

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


        public static GrpcServiceEndpointConventionBuilder AddToGrpcBrowserWithClient<TClient>(this GrpcServiceEndpointConventionBuilder builder, Action<GrpcServiceOptions> optionsAction = null) where TClient : Grpc.Core.ClientBase<TClient>
        {
            var options = new GrpcServiceOptions();

            optionsAction?.Invoke(options);

            GrpcBrowser.AddProtoFirstService<TClient>(options);

            return builder;
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcBrowserWithService<TServiceInterface>(this GrpcServiceEndpointConventionBuilder builder, Action<GrpcServiceOptions> optionsAction = null)
        {
            var options = new GrpcServiceOptions();

            optionsAction?.Invoke(options);

            GrpcBrowser.AddCodeFirstService<TServiceInterface>(options);

            return builder;
        }
    }
}
