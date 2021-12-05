using GrpcBrowser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;
using SampleGrpcService.net50.Services.CodeFirst;
using SampleGrpcService.net50.Services.ProtoFirst;
using System.Threading.Tasks;

namespace SampleGrpcService.net50
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddCodeFirstGrpc();
            services.AddGrpcBrowser();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.MapGrpcBrowser();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ProtoFirstGreeterService>();
                endpoints.MapGrpcService<CodeFirstGreeterService>();

                endpoints.MapGet("/", context =>
                {
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", "https://localhost:5001/grpc");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
