using Fluxor;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace GrpcBrowser
{
    public static class StartupHelpers
    {
        public static void AddGrpcBrowser(this IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddMudServices();
            services.AddFluxor(o => o
                .ScanAssemblies(typeof(StartupHelpers).Assembly)
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
    }
}
