using GrpcBrowser.Configuration;
using OAuthSecuredGrpcService;
using OAuthSecuredGrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddGrpcBrowser();
builder.Services.AddGrpcClient<SecureProtoFirstGreeter.SecureProtoFirstGreeterClient>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseGrpcBrowser();

// Configure the HTTP request pipeline.
app.MapGrpcService<SecureGreeterService>().AddToGrpcBrowserWithClient<SecureProtoFirstGreeter.SecureProtoFirstGreeterClient>();

app.MapGrpcBrowser();

app.MapGet("/", context =>
{
    context.Response.StatusCode = 302;
    context.Response.Headers.Add("Location", "https://localhost:7276/grpc");
    return Task.CompletedTask;
});

app.Run();
