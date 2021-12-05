using GrpcBrowser;
using ProtoBuf.Grpc.Server;
using SampleGrpcService.net60.Services.CodeFirst;
using SampleGrpcService.net60.Services.ProtoFirst;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddCodeFirstGrpc();
builder.Services.AddGrpcBrowser();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ProtoFirstGreeterService>();
app.MapGrpcService<CodeFirstGreeterService>();
app.UseRouting();
app.MapGrpcBrowser();
app.MapGet("/", context =>
{
    context.Response.StatusCode = 302;
    context.Response.Headers.Add("Location", "https://localhost:7262/grpc");
    return Task.CompletedTask;
});

app.Run();
