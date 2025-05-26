using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundServices
{
    public class EmailDeliveryJob(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var emailRepo = scope.ServiceProvider.GetRequiredService<IEmailsDeliveryRepo>();

                while (!stoppingToken.IsCancellationRequested)
                {
                    List<EmailsDelivery>? dueEmails = await emailRepo.GetAllUnsentEmails();

                    foreach (var emailRecord in dueEmails)
                    {
                        var result= await emailRepo.SendEmail (emailRecord);

                        if (result.Item1)
                        {
                            await emailRepo.MarkEmailAsSent(emailRecord.Id);
                        }
                        else {
                            await emailRepo.UpdateRetryCount(emailRecord.Id, result.Item2);
                        }

                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
