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
    public partial class DuplexOperationView
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

        private int DisplayedRequestNumber
        {
            get => _displayedRequestNumber;
            set
            {
                _displayedRequestNumber = value;
                _requestJson =
                    ConnectionState?.Requests[_displayedRequestNumber - 1] is null
                        ? _requestJson
                        : JsonConvert.SerializeObject(ConnectionState?.Requests[_displayedRequestNumber - 1].RequestBody, Formatting.Indented);
                StateHasChanged();
            }
        }
        private int _displayedRequestNumber = 1;

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

        private void SendMessage()
        {
            Dispatcher?.Dispatch(new SendMessageToConnectedDuplexOperation(_requestId, Service, Operation, _requestJson, DateTimeOffset.Now));
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
            Dispatcher!.Dispatch(new StopDuplexOperation(_requestId!, Service, Operation));
        }

        private void Connect()
        {
            _requestId ??= new GrpcRequestId(Guid.NewGuid());
            Dispatcher?.Dispatch(new OpenDuplexConnection(Service, Operation, _requestId, new GrpcRequestHeaders(_headers.ToImmutableDictionary(h => h.Key, h => h.Value))));
        }

        private DuplexConnectionState? ConnectionState =>
            _requestId is not null &&
            RequestState!.Value.DuplexRequests.TryGetValue(_requestId, out var streamingState)
                ? streamingState
                : null;

        record DownloadedDuplexOperationInformation(string Service, string Operation);
        record DownloadedDuplexConnectionInformation(ImmutableDictionary<string, string> Headers);
        record DownloadedDuplexMessage(DateTimeOffset Timestamp, string Direction, object Body);
        record DownloadedDuplexResponse(DateTimeOffset TimeStamp, object Body);
        record DownloadedDuplexDocument(DownloadedDuplexOperationInformation Operation, DownloadedDuplexConnectionInformation Connection, ImmutableList<DownloadedDuplexMessage> Messages);

        private async Task Download()
        {
            var operation = new DownloadedDuplexOperationInformation(
                Service.ServiceType.Name,
                Operation.Name);

            var connection = new DownloadedDuplexConnectionInformation(ConnectionState.Headers.Values);

            var requests =
                ConnectionState.Requests.Select(request => new DownloadedDuplexMessage(request.TimeStamp, "ClientToServer", request.RequestBody));

            var responses =
                ConnectionState.Responses.Select(response => new DownloadedDuplexMessage(response.TimeStamp, "ServerToClient", response.ResponseBody));

            var document = new DownloadedDuplexDocument(operation, connection, requests.Concat(responses).OrderBy(r => r.Timestamp).ToImmutableList());

            var documentJson = JsonConvert.SerializeObject(document, Formatting.Indented);

            await BlazorDownloadFileService.DownloadFileFromText($"{Operation.Name}-{DateTimeOffset.Now.Ticks}.json", documentJson, Encoding.UTF8, "text/plain", false);
        }
    }
}
