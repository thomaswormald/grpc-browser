using FxService.Api.Account;
using GrpcBrowser.Configuration;
using GrpcBrowser.Configuration.RequestInterceptors.Implementations;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddCodeFirstGrpc();
builder.Services.AddGrpcBrowser();

builder.Services.AddSingleton<IFxRepository, FxRepository>();
builder.Services.AddSingleton<IAccountRepository, AccountRepository>();
builder.Services.AddHostedService<FxRateRandomiser>();

var app = builder.Build();

app.UseGrpcBrowser(options =>
{
    options.GlobalRequestInterceptors.Add(new SetHeaderInterceptor("Header-Key", "Test Header Value"));
});

// Configure the HTTP request pipeline.
app.MapGrpcService<AccountApi>().AddToGrpcBrowserWithClient<AccountService.AccountServiceClient>(options =>
{
    options.Interceptors.Add(new SetHeaderInterceptor("Operation-Type", "ProtoFirst"));
});

app.MapGrpcService<FxApi>().AddToGrpcBrowserWithService<IFxApi>(options =>
{
    options.Interceptors.Add(new SetHeaderInterceptor("Operation-Type", "CodeFirst"));
});

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapGrpcBrowser();

app.Run();
