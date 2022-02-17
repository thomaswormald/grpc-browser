using System;
using System.Collections.Generic;
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
    public partial class ClientStreamingOperationView
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
            _requestId ??= new GrpcRequestId(Guid.NewGuid());

            if (ConnectionState?.Connected ?? false)
            {
                Dispatcher?.Dispatch(new SendMessageToConnectedClientStreamingOperation(_requestId, Service, Operation, _requestJson, DateTimeOffset.Now));
            }
            else
            {
                Dispatcher?.Dispatch(new CallClientStreamingOperation(Service, Operation, _requestJson, _requestId, new GrpcRequestHeaders(_headers.ToImmutableDictionary(h => h.Key, h => h.Value)), DateTimeOffset.Now));
            }
        }

        private GrpcResponse? Response => ConnectionState?.Response;

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
            Dispatcher!.Dispatch(new StopClientStreamingOperation(_requestId!, Service, Operation));
        }

        private ClientStreamingConnectionState? ConnectionState =>
            _requestId is not null &&
            RequestState!.Value.ClientStreamingRequests.TryGetValue(_requestId, out var streamingState)
                ? streamingState
                : null;

        record DownloadedClientStreamingOperationInformation(string Service, string Operation);
        record DownloadedClientStreamingConnectionInformation(ImmutableDictionary<string, string> Headers);
        record DownloadedClientStreamingRequest(DateTimeOffset Timestamp, object Body);
        record DownloadedClientStreamingResponse(DateTimeOffset TimeStamp, object Body);
        record DownloadedClientStreamingDocument(DownloadedClientStreamingOperationInformation Operation, DownloadedClientStreamingConnectionInformation Connection, ImmutableList<DownloadedClientStreamingRequest> Requests, DownloadedClientStreamingResponse Response);

        private async Task Download()
        {
            var operation = new DownloadedClientStreamingOperationInformation(
                Service.Name,
                Operation.Name);

            var connection = new DownloadedClientStreamingConnectionInformation(ConnectionState.Headers.Values);

            var requests =
                ConnectionState.Requests.Select(request => new DownloadedClientStreamingRequest(request.TimeStamp, request.RequestBody)).OrderBy(r => r.Timestamp).ToImmutableList();

            var response =
                ConnectionState.Response is not null
                    ? new DownloadedClientStreamingResponse(ConnectionState.Response.TimeStamp, ConnectionState.Response.ResponseBody)
                    : null;

            var document = new DownloadedClientStreamingDocument(operation, connection, requests, response);

            var documentJson = JsonConvert.SerializeObject(document, Formatting.Indented);

            await BlazorDownloadFileService.DownloadFileFromText($"{Operation.Name}-{DateTimeOffset.Now.Ticks}.json", documentJson, Encoding.UTF8, "text/plain", false);
        }
    }
}
