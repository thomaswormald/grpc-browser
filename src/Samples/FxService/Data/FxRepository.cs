using System.Collections.Immutable;
using System.Reactive.Subjects;

public interface IFxRepository
{
    IObservable<FxRate> FxRateObservable { get; }
    Task SetFxRate(FxRate rate);
    Task<FxRate?> GetFxRate(string FromCurrency, string ToCurrency);
    Task<ImmutableDictionary<(string, string), decimal>> GetAllFxRates();
}

public record FxRate(string FromCurrency, string ToCurrency, decimal Rate);

public class FxRepository : IFxRepository
{
    private readonly Subject<FxRate> _fxRateSubject;
    private ImmutableDictionary<(string, string), decimal> _rates;

    public FxRepository()
    {
        _rates = ImmutableDictionary<(string, string), decimal>.Empty;
        _fxRateSubject = new Subject<FxRate>();

        _fxRateSubject.Subscribe(update =>
        {
            _rates = _rates.SetItem((update.FromCurrency.ToUpper(), update.ToCurrency.ToUpper()), update.Rate);
        });
    }

    public IObservable<FxRate> FxRateObservable => _fxRateSubject;

    public async Task SetFxRate(FxRate rate)
    {
        _fxRateSubject.OnNext(rate);
    }

    public Task<FxRate?> GetFxRate(string FromCurrency, string ToCurrency)
    {
        return Task.FromResult(
            _rates.TryGetValue((FromCurrency.ToUpper(), ToCurrency.ToUpper()), out var result)
            ? new FxRate(FromCurrency, ToCurrency, result)
            : null);
    }

    public Task<ImmutableDictionary<(string, string), decimal>> GetAllFxRates()
    {
        return Task.FromResult(_rates);
    }
}
