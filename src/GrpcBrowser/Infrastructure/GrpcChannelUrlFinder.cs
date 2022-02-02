using System;
using Microsoft.AspNetCore.Components;

namespace GrpcBrowser.Infrastructure
{
    public class GrpcChannelUrlProvider
    {
        public string BaseUrl { get; private set; }

        public GrpcChannelUrlProvider(NavigationManager navManager)
        {
            BaseUrl = navManager.BaseUri;

            if (BaseUrl.EndsWith("/grpc/"))
            {
                BaseUrl = BaseUrl.Substring(0, BaseUrl.Length - "/grpc/".Length);
            }

            Console.WriteLine("Base URL: " + BaseUrl);

        }
    }
}
