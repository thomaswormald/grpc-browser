using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;

public interface IAccountRepository
{
    Task<bool> Withdraw(string currency, decimal amount);
    Task Deposit(string currency, decimal amount);
    IObservable<ImmutableDictionary<string, decimal>> BalanceChanges { get; }
}

public class AccountRepository : IAccountRepository
{
    private readonly ConcurrentDictionary<string, decimal> _currencyBalances;
    private readonly Subject<ImmutableDictionary<string, decimal>> _balanceStream;
    
    public AccountRepository()
    {
        _currencyBalances = new ConcurrentDictionary<string, decimal>();
        _balanceStream = new Subject<ImmutableDictionary<string, decimal>>();

        _currencyBalances["USD"] = 10_000;
        _currencyBalances["GBP"] = 20_000;
        _currencyBalances["EUR"] = 30_000;
    }

    private void UpdateBalanceStream()
    {
        _balanceStream.OnNext(_currencyBalances.ToImmutableDictionary());
    }

    public Task<bool> Withdraw(string currency, decimal amount)
    {
        bool success = false;
        currency = currency.ToUpper();

        _currencyBalances.AddOrUpdate(currency, _ => 0, (_, balance) =>
        {
            if (balance >= amount)
            {
                success = true;
                return balance - amount;
            }

            return balance;
        });

        UpdateBalanceStream();

        return Task.FromResult(success);
    }

    public Task Deposit(string currency, decimal amount)
    {
        currency = currency.ToUpper();

        _currencyBalances.AddOrUpdate(currency, amount, (_, balance) => balance + amount);

        UpdateBalanceStream();

        return Task.CompletedTask;
    }

    public IObservable<ImmutableDictionary<string, decimal>> BalanceChanges => _balanceStream;
}
