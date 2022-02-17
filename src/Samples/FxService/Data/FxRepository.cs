using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

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

public class FxRateRandomiser : BackgroundService
{
    private readonly IFxRepository _fxRepository;
    private readonly IDisposable _updater;

    public FxRateRandomiser(IFxRepository fxRepository)
    {
        _fxRepository = fxRepository;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var random = new Random();

        var fxRates = new Dictionary<(string, string), decimal>();

        fxRates[("USD", "EUR")] = 0.88m;
        fxRates[("EUR", "USD")] = 1.14m;
        fxRates[("USD", "GBP")] = 0.74m;
        fxRates[("GBP", "USD")] = 1.36m;
        fxRates[("GBP", "EUR")] = 1.19m;
        fxRates[("EUR", "GBP")] = 0.84m;

        return Observable.Interval(TimeSpan.FromSeconds(1))
            .Do(_ =>
            {
                var adjustment = (random.NextDouble() - 0.5) * 0.1;
                var rateToAdjust = fxRates.Keys.ToList()[random.Next(0, fxRates.Count - 1)];
                var currentValue = fxRates[rateToAdjust];
                var newValue = Math.Round(currentValue + (decimal)adjustment, 2);

                fxRates[rateToAdjust] = newValue;
                var (fromCurrency, toCurrency) = rateToAdjust;

                _fxRepository.SetFxRate(new FxRate(fromCurrency, toCurrency, newValue));
            })
            .ToTask();
    }
}
