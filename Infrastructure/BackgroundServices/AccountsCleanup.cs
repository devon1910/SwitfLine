using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.BackgroundServices
{
    public class AccountsCleanup(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepo>();

                while (!stoppingToken.IsCancellationRequested)
                {
                    List<SwiftLineUser>? expiredAccounts = await authRepo.GetExpiredAccounts();

                    foreach (var account in expiredAccounts)
                    {
                       await authRepo.DeleteExpiredAccount(account);
                    }
                    await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
