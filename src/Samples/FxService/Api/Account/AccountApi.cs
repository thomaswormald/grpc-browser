using FxService.Api.Account;
using Grpc.Core;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

public class AccountApi: AccountService.AccountServiceBase
{
    private readonly IAccountRepository _accountRepository;

    public AccountApi(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository; 
    }

    public override Task SubscribeAccountBalances(AccountBalanceRequest request, IServerStreamWriter<AccountBalanceUpdate> responseStream, ServerCallContext context)
    {
        return _accountRepository.BalanceChanges
            .Do(async balance =>
            {
                var update = new AccountBalanceUpdate();

                foreach (var kvp in balance)
                {
                    update.CurrencyBalances.Add(kvp.Key, (double)kvp.Value);
                }

                update.LastUpdate = DateTimeOffset.Now.ToString("o");

                await responseStream.WriteAsync(update);
            })
            .ToTask();
    }
}