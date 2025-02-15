using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Interfaces;

namespace Infrastructure.BackgroundServices
{
    public class LineManager(IServiceProvider serviceProvider) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queuesRepo = serviceProvider.GetRequiredService<ILineRepo>();
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var lines = await queuesRepo.GetLines();

                    foreach (var line in lines)
                    {
                        if (await queuesRepo.IsUserAttendedTo(line)) 
                        {
                            await queuesRepo.MarkUserAsAttendedTo(line);
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
