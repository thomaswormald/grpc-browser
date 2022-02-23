using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Kernel;
using BlazorDownloadFile;
using Fluxor;
using GrpcBrowser.Store.Requests;
using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace GrpcBrowser.Components
{
    public partial class ServerStreamingOperationView
    {
        [Inject] public IDispatcher? Dispatcher { get; set; }
        [Parameter] public GrpcService? Service { get; set; }
        [Parameter] public GrpcOperation? Operation { get; set; }
        [Inject] public IState<RequestState>? RequestState { get; set; }
        [Inject] IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

        private string _requestJson = "";
        private int _requestTextFieldLines = 5;
        private GrpcRequestId? _requestId = null;
        private ImmutableList<HeaderViewModel> _headers = ImmutableList<HeaderViewModel>.Empty;
        private int _displayedResponseNumber = 1;

        protected override void OnParametersSet()
        {
            var autoFixture = new Fixture();
            autoFixture.Register<string>(() => "string");
            var randomInstanceOfRequestObject =
                autoFixture.Create(Operation.RequestType, new SpecimenContext(autoFixture));

            _requestJson = JsonConvert.SerializeObject(randomInstanceOfRequestObject, Formatting.Indented);
            _requestTextFieldLines = Math.Min(_requestJson.Split('\n').Length, App.MaxTextFieldLines);

            base.OnParametersSet();
        }

        private void Connect()
        {
            _requestId ??= new GrpcRequestId(Guid.NewGuid());
            Dispatcher?.Dispatch(new CallServerStreamingOperation(Service, Operation, _requestJson, _requestId, new GrpcRequestHeaders(_headers.ToImmutableDictionary(h => h.Key, h => h.Value)), DateTimeOffset.Now));
        }

        private GrpcResponse? Response => ConnectionState?.Responses.Count >= _displayedResponseNumber ? ConnectionState?.Responses[_displayedResponseNumber - 1] : null;

        // This is a hack so that I can use the MudTextField to display the response
        private string? SerializedResponse
        {
            get => JsonConvert.SerializeObject(Response?.ResponseBody, Formatting.Indented);
            set { }
        }

        private int ResponseTextFieldLines
        {
            get => Math.Min(SerializedResponse?.Split('\n').Length ?? 5, App.MaxTextFieldLines);
        }

        private void AddHeader()
        {
            _headers = _headers.Add(new HeaderViewModel());
        }

        private void RemoveHeader(HeaderViewModel header)
        {
            _headers = _headers.Remove(header);
        }

        private void Disconnect()
        {
            Dispatcher!.Dispatch(new StopServerStreamingConnection(_requestId!));
        }

        private ServerStreamingConnectionState? ConnectionState =>
            _requestId is not null &&
            RequestState!.Value.ServerStreamingRequests.TryGetValue(_requestId, out var streamingState)
                ? streamingState
                : null;

        record DownloadedServerStreamingOperationInformation(string Service, string Operation);
        record DownloadedServerStreamingResponse(DateTimeOffset TimeStamp, object ResponseBody);
        record DownloadedServerStreamingRequest(DateTimeOffset Timestamp, ImmutableDictionary<string, string> Headers, object Body);
        record DownloadedServerStreamingDocument(DownloadedServerStreamingOperationInformation Operation, DownloadedServerStreamingRequest Request, ImmutableList<DownloadedServerStreamingResponse> Responses);

        private async Task Download()
        {
            var operation = new DownloadedServerStreamingOperationInformation(
                ConnectionState.RequestAction.Service.ServiceType.Name,
                ConnectionState.RequestAction.Operation.Name);

            var request = new DownloadedServerStreamingRequest(
                ConnectionState.RequestAction.Timestamp,
                ConnectionState.RequestAction.Headers.Values,
                ConnectionState.Request);

            var responses = ConnectionState!.Responses.Select(response => new DownloadedServerStreamingResponse(response.TimeStamp, response.ResponseBody)).OrderBy(r => r.TimeStamp).ToImmutableList();

            var document = new DownloadedServerStreamingDocument(operation, request, responses);


            var documentJson = JsonConvert.SerializeObject(document, Formatting.Indented);

            await BlazorDownloadFileService.DownloadFileFromText($"{ConnectionState.RequestAction.Operation.Name}-{ConnectionState.RequestAction.Timestamp.Ticks}.json", documentJson, Encoding.UTF8, "text/plain", false);
        }
    }
}
