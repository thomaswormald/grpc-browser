# gRPC Browser

This project allows you to add a web-based gRPC Browser for debugging purposes to your .NET application.

![Test](docs/screenshots/duplex_streaming.png)

## Usage
1. Add the package `GrpcBrowser` from NuGet to your project. Your project SDK must be Microsoft.NET.Sdk.Web (see Troubleshooting for further details).
2. In the configure method of your Startup class, add `app.UseGrpcBrowser();`
3. In the configure method of your Startup class, where you call `endpoints.MapGrpcService()`, add the following:
   1. For Code-First GRPC Services: `.AddToGrpcBrowserWithService<ITheInterfaceOfMyCodeFirstService>()`
   2. For Proto-First (where you have defined a `.proto` file): `.AddToGrpcBrowserWithClient<GeneratedClientClassForMyProtoFirstGrpcService>()`
4. In the `UseEndpoints()` setup, add `endpoints.MapGrpcBrowser()`

For example, the `Configure` method of a service with one proto-first and one code-first GRPC service could look like this:
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseGrpcBrowser();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGrpcService<ProtoFirstSampleService>().AddToGrpcBrowserWithClient<ProtoFirstGreeter.ProtoFirstGreeterClient>();
        endpoints.MapGrpcService<CodeFirstGreeterService>().AddToGrpcBrowserWithService<ICodeFirstGreeterService>();

        endpoints.MapGrpcBrowser();
    });
}
```

5. In the `ConfigureServices` method of your Startup class, add `services.AddGrpcBrowser()`

6. Start your service, and navigate to `/grpc` in your browser.

## Troubleshooting

### Error when navigating to `/grpc`
Make sure that you service SDK type is Microsoft.NET.Sdk.Web. This is required because GrpcBrowser adds server-side Blazor to your project. In the `.csproj` file, set the Project Sdk like this: `<Project Sdk="Microsoft.NET.Sdk.Web">`