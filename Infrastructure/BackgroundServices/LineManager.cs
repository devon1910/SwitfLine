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
using Domain.Models;

namespace Infrastructure.BackgroundServices
{
    public class LineManager(IServiceProvider serviceProvider) : BackgroundService
    {

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {          
            try
            {
                using var scope = serviceProvider.CreateScope();
                var linesRepo = scope.ServiceProvider.GetRequiredService<ILineRepo>();
                var eventsRepo = scope.ServiceProvider.GetRequiredService<IEventRepo>();
                var signalRNotifier = scope.ServiceProvider.GetRequiredService<ISignalRNotifierRepo>();

                while (!stoppingToken.IsCancellationRequested)
                {
                    var cachedEvents = await eventsRepo.GetActiveEvents();

                    foreach (var e in cachedEvents)
                    {
                        Line? line = await linesRepo.GetFirstLineMember(e.Id);

                        if (line is not null && await linesRepo.IsItUserTurnToBeServed(line,e.AverageTimeToServeSeconds))
                        {
                            await linesRepo.MarkUserAsServed(line,"served");
                            await signalRNotifier.BroadcastLineUpdate(line,-1);
                            await linesRepo.Notify2ndLineMember(line);

                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
