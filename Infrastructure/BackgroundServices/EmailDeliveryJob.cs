using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundServices
{
    public class EmailDeliveryJob(IServiceProvider serviceProvider, ILogger<EmailDeliveryJob> logger) : BackgroundService
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
                        string message = "";
                        try
                        {
                            var result = await emailRepo.SendEmail(emailRecord);

                            if (result.Item1)
                            {
                                await emailRepo.MarkEmailAsSent(emailRecord.Id);
                            }
                            else
                            {
                                await emailRepo.UpdateRetryCount(emailRecord.Id, result.Item2);
                            }
                        }
                        catch (Exception ex)
                        {
                            message = ex.Message;                     
                        }
                        finally 
                        {
                            await emailRepo.UpdateRetryCount(emailRecord.Id, message);
                        }
                       
                    }
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while executing the EmailDeliveryJob background service.");
            }
        }
    }
}
