﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using Fluxor;
using GrpcBrowser.Store.Requests;
using GrpcBrowser.Store.Services;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace GrpcBrowser.Components
{
    public partial class UnaryOperationView
    {
        [Inject] public IDispatcher? Dispatcher { get; set; }
        [Parameter] public GrpcService? Service { get; set; }
        [Parameter] public GrpcOperation? Operation { get; set; }
        [Inject] public IState<RequestState>? RequestState { get; set; }

        private string _requestJson = "";
        private GrpcRequestId? _requestId = null;
        private ImmutableList<HeaderViewModel> _headers = ImmutableList<HeaderViewModel>.Empty;

        protected override void OnParametersSet()
        {
            var autoFixture = new Fixture();
            autoFixture.Register<string>(() => "string");
            var randomInstanceOfRequestObject =
                autoFixture.Create(Operation.RequestType, new SpecimenContext(autoFixture));


            _requestJson = JsonConvert.SerializeObject(randomInstanceOfRequestObject, Formatting.Indented);

            base.OnParametersSet();
        }

        private void Execute()
        {
            _requestId ??= new GrpcRequestId(Guid.NewGuid());
            Dispatcher?.Dispatch(new CallUnaryOperation(Service, Operation, _requestJson, _requestId, new GrpcRequestHeaders(_headers.ToImmutableDictionary(h => h.Key, h => h.Value))));
        }

        private GrpcResponse? Response => _requestId is not null && RequestState is not null && RequestState.Value.UnaryRequests.TryGetValue(_requestId, out var response) ? response : null;

        // This is a hack so that I can use the MudTextField to display the response
        private string? SerializedResponse
        {
            get => JsonConvert.SerializeObject(Response?.Response, Formatting.Indented);
            set { }
        }

        private void AddHeader()
        {
            _headers = _headers.Add(new HeaderViewModel());
        }

        private void RemoveHeader(HeaderViewModel header)
        {
            _headers = _headers.Remove(header);
        }
    }
}
