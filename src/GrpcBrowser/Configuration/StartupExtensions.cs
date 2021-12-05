using Fluxor;
using GrpcBrowser.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using ProtoBuf.Grpc.Configuration;
using System;
using System.Collections.Generic;
using System.ServiceModel;

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
        public static void MapGrpcBrowser(this IApplicationBuilder app)
        {
            app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/grpc"), tools =>
            {
                app.UseStaticFiles("/grpc");
            });

            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub("/grpc/_blazor");
                endpoints.MapFallbackToPage("/grpc/{*path:nonfile}", "/_Host");
                endpoints.MapFallbackToPage("/grpc", "/_Host");
            });
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcUiWithClient<TClient>(this GrpcServiceEndpointConventionBuilder builder) where TClient : Grpc.Core.ClientBase<TClient>
        {
            ConfiguredGrpcServices.ProtoGrpcClients.Add(typeof(TClient));

            return builder;
        }

        public static GrpcServiceEndpointConventionBuilder AddToGrpcUiWithService<TServiceInterface>(this GrpcServiceEndpointConventionBuilder builder)
        {
            if (!typeof(TServiceInterface).IsInterface)
            {
                throw new ArgumentException(
                    $"The generic parameter to {nameof(AddToGrpcUiWithService)} must be your GRPC service's interface. '{typeof(TServiceInterface).Name}' is not an interface");
            }

            if (!typeof(TServiceInterface).IsDefined(typeof(ServiceAttribute), false) &&
                !typeof(TServiceInterface).IsDefined(typeof(ServiceContractAttribute), false))
            {
                throw new ArgumentException(
                    $"The generic parameter to {nameof(AddToGrpcUiWithService)} must be your GRPC service's interface. '{typeof(TServiceInterface).Name}' does not have the attribute '{nameof(ServiceAttribute)}' or '{nameof(ServiceContractAttribute)}', which is required for code-first GRPC services");
            }

            ConfiguredGrpcServices.CodeFirstGrpcServiceInterfaces.Add(typeof(TServiceInterface));

            return builder;
        }
    }
}
