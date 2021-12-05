using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;

namespace GrpcBrowser.Components
{
    public partial class ServiceView
    {
        [Parameter] public GrpcService? Service { get; set; }

        // TODO For some reason when applied as a css class this is all ignored, but as a style it works fine
        public const string jsonTextBoxStyle = "font-family: monospace; font-size: 12px; color: white; background-color: rgba(51, 51, 51, 1)";
    }
}
