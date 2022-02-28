using Fluxor;
using GrpcBrowser.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Microsoft.AspNetCore.Routing;
using BlazorDownloadFile;
using System;
using ProtoBuf.Grpc.Configuration;
using System.ServiceModel;
using System.Collections.Immutable;

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
            services.AddBlazorDownloadFile(ServiceLifetime.Scoped);
        }

        public static void UseGrpcBrowser(this IApplicationBuilder app, Action<GrpcBrowserOptions> optionsAction = null)
        {
            var options = new GrpcBrowserOptions();
            optionsAction?.Invoke(options);

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

            if (GrpcBrowserConfiguration.ProtoGrpcClients.ContainsKey(typeof(TClient).FullName))
            {
                throw new ArgumentException($"A proto-first service with the type {typeof(TClient).Name} has already been configured");
            }

            GrpcBrowserConfiguration.ProtoGrpcClients = GrpcBrowserConfiguration.ProtoGrpcClients.Add(typeof(TClient).FullName, new ConfiguredGrpcClient(typeof(TClient)));

            return builder;
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcBrowserWithService<TServiceInterface>(this GrpcServiceEndpointConventionBuilder builder, Action<GrpcServiceOptions> optionsAction = null)
        {
            var options = new GrpcServiceOptions();

            optionsAction?.Invoke(options);

            if (GrpcBrowserConfiguration.CodeFirstGrpcServiceInterfaces.ContainsKey(typeof(TServiceInterface).FullName))
            {
                throw new ArgumentException($"A code-first service with interface {typeof(TServiceInterface).Name} has already been configured");
            }

            if (!typeof(TServiceInterface).IsInterface)
            {
                throw new ArgumentException(
                    $"To add your code-first service to GrpcBrowser, you must provide your GRPC service's interface. '{typeof(TServiceInterface).Name}' is not an interface");
            }

            if (!typeof(TServiceInterface).IsDefined(typeof(ServiceAttribute), false) &&
                !typeof(TServiceInterface).IsDefined(typeof(ServiceContractAttribute), false))
            {
                throw new ArgumentException(
                    $"To add your code-first service to GrpcBrowser, you must provide your GRPC service's interface. '{typeof(TServiceInterface).Name}' does not have the attribute '{nameof(ServiceAttribute)}' or '{nameof(ServiceContractAttribute)}', which is required for code-first GRPC services");
            }

            GrpcBrowserConfiguration.CodeFirstGrpcServiceInterfaces = GrpcBrowserConfiguration.CodeFirstGrpcServiceInterfaces.Add(typeof(TServiceInterface).FullName, new ConfiguredGrpcClient(typeof(TServiceInterface)));

            return builder;
        }
    }
}
