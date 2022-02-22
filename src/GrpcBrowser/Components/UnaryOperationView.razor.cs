using System;
using System.Collections.Immutable;
using System.Text;
using AutoFixture;
using AutoFixture.Kernel;
using Fluxor;
using GrpcBrowser.Store.Requests;
using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Threading.Tasks;
using BlazorDownloadFile;

namespace GrpcBrowser.Components
{
    public partial class UnaryOperationView
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

        private void Execute()
        {
            _requestId ??= new GrpcRequestId(Guid.NewGuid());
            Dispatcher?.Dispatch(new CallUnaryOperation(Service, Operation, _requestJson, _requestId, new GrpcRequestHeaders(_headers.ToImmutableDictionary(h => h.Key, h => h.Value)), DateTimeOffset.Now));
        }

        private UnaryRequestState? UnaryRequestState => _requestId is not null && RequestState is not null && RequestState.Value.UnaryRequests.TryGetValue(_requestId, out var unaryRequest) ? unaryRequest : null;

        // This is a hack so that I can use the MudTextField to display the response
        private string? SerializedResponse
        {
            get => JsonConvert.SerializeObject(UnaryRequestState?.Response.ResponseBody, Formatting.Indented);
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

        record DownloadedUnaryOperationInformation(string Service, string Operation);
        record DownloadedUnaryRequest(DateTimeOffset Timestamp, ImmutableDictionary<string, string> Headers, object Body);
        record DownloadedUnaryResponse(DateTimeOffset Timestamp, object Response);
        record DownloadedUnaryDocument(DownloadedUnaryOperationInformation Operation, DownloadedUnaryRequest Request, DownloadedUnaryResponse Response);

        private async Task Download()
        {
            var operation = new DownloadedUnaryOperationInformation(
                UnaryRequestState.RequestAction.Service.ServiceType.Name,
                UnaryRequestState.RequestAction.Operation.Name);

            var request = new DownloadedUnaryRequest(
                UnaryRequestState.RequestAction.Timestamp,
                UnaryRequestState.RequestAction.Headers.Values,
                UnaryRequestState.Request);

            var response = new DownloadedUnaryResponse(
                UnaryRequestState.Response.TimeStamp,
                UnaryRequestState.Response.ResponseBody);

            var document = new DownloadedUnaryDocument(operation, request, response);

            var documentJson = JsonConvert.SerializeObject(document, Formatting.Indented);

            await BlazorDownloadFileService.DownloadFileFromText($"{UnaryRequestState.RequestAction.Operation.Name}-{UnaryRequestState.Response.TimeStamp.Ticks}.json", documentJson, Encoding.UTF8, "text/plain", false);
        }
    }
}
