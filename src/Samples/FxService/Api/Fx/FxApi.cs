using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using System.Reactive.Linq;

/// <summary>
/// Place FX trade orders, stream and update FX rates.
/// </summary>
[Service]
public interface IFxApi
{
    /// <summary>
    /// Make an FX trade. Supported currencies are GBP, USD and EUR
    /// </summary>
    [Operation]
    Task<FxOrderResult> PlaceFxOrder(FxOrder request, CallContext context = default);

    /// <summary>
    /// Stream out individual FX rate changes 
    /// </summary>
    [Operation]
    IAsyncEnumerable<FxRateUpdate> StreamFxRates(IAsyncEnumerable<FxRateRequest> request, CallContext context = default);

    /// <summary>
    /// Set the FX rate between two currencies
    /// </summary>
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
        var currencies = new HashSet<(string, string)>(new[] {("USD", "EUR"), ("USD", "GBP"), ("GBP", "EUR"), ("GBP", "USD"), ("EUR", "GBP"), ("EUR", "USD")});

        request.ToObservable().Subscribe(currencyUpdate =>
        {
            if (currencyUpdate.Show)
            {
                currencies.Add((currencyUpdate.FromCurrency, currencyUpdate.ToCurrency));
            }
            else
            {
                currencies.Remove((currencyUpdate.FromCurrency, currencyUpdate.ToCurrency));
            }
        });

        return
            _fxRepository.FxRateObservable
            .Where(update => currencies.Contains((update.FromCurrency, update.ToCurrency)))
            .Select(update => new FxRateUpdate
            {
                FromCurrency = update.FromCurrency,
                ToCurrency = update.ToCurrency,
                ConversionRate = update.Rate,
                Timestamp = DateTimeOffset.Now.ToString("o")
            })
            .ToAsyncEnumerable();
    }
}
