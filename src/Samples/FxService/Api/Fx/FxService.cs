using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Reactive;
using System.Reactive.Linq;

[Service]
public interface IFxApi
{
    [Operation]
    Task<FxOrderResult> PlaceFxOrder(FxOrder request, CallContext context = default);

    [Operation]
    IAsyncEnumerable<FxRateUpdate> StreamFxRates(IAsyncEnumerable<FxRateRequest> request, CallContext context = default);

    [Operation]
    Task<SetFxRateResult> SetFxRates(IAsyncEnumerable<SetFxRateRequest> requestStream, CallContext context = default);
}

public class FxApi : IFxApi
{
    private readonly IFxRepository _fxRepository;
    private readonly IAccountRepository _accountRepository;

    public FxApi(IFxRepository fxRepository, IAccountRepository accountRepository)
    {
        _fxRepository = fxRepository;
        _accountRepository = accountRepository;
    }

    public async Task<FxOrderResult> PlaceFxOrder(FxOrder request, CallContext context = default)
    {
        // Good job I don't work for a bank, this is not safe at all. Bank repository needs to withdraw and deposit in one transaction

        var fxRate = await _fxRepository.GetFxRate(request.FromCurrency, request.ToCurrency);
        
        if (fxRate is null)
        {
            return new FxOrderResult
            {
                Success = false,
                ErrorMessage = $"Unable to find FX Rate from {request.FromCurrency} to {request.ToCurrency}"
            };
        }

        var sufficientFunds = await _accountRepository.Withdraw(request.FromCurrency, request.AmountInFromCurrency);

        if (!sufficientFunds)
        {
            return new FxOrderResult
            {
                Success = false,
                ErrorMessage = $"Insufficient funds to withdraw {request.FromCurrency} {request.AmountInFromCurrency}"
            };
        }

        var convertedAmount = fxRate.Rate * request.AmountInFromCurrency;

        await _accountRepository.Deposit(request.ToCurrency, convertedAmount);

        return new FxOrderResult
        {
            Success = true,
            OrderId = Guid.NewGuid()
        };
    }

    public async Task<SetFxRateResult> SetFxRates(IAsyncEnumerable<SetFxRateRequest> requestStream, CallContext context = default)
    {
        await foreach (var fxRate in requestStream)
        {
            await _fxRepository.SetFxRate(new FxRate(fxRate.FromCurrency, fxRate.ToCurrency, fxRate.ConversionRate));
        }

        return new SetFxRateResult
        {
            FxRates = await _fxRepository.GetAllFxRates()
        };
    }

    public IAsyncEnumerable<FxRateUpdate> StreamFxRates(IAsyncEnumerable<FxRateRequest> request, CallContext context = default)
    {
        Observable.
    }
}
