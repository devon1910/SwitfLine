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
                var notifier = scope.ServiceProvider.GetRequiredService<INotifierRepo>();
                // Initialize cache and refresh tracking
                IEnumerable<Event> cachedEvents = [];
                DateTime lastCacheRefresh = DateTime.MinValue;
                TimeSpan cacheRefreshInterval = TimeSpan.FromMinutes(1); // Adjust interval as needed

                while (!stoppingToken.IsCancellationRequested)
                {

                    // Refresh cache only if the interval has elapsed
                    if (DateTime.UtcNow.AddHours(1) - lastCacheRefresh > cacheRefreshInterval)
                    {
                        cachedEvents = await eventsRepo.GetActiveEvents();
                        lastCacheRefresh = DateTime.UtcNow;
                    }

                    foreach (var e in cachedEvents)
                    {
                        Line? line = await linesRepo.GetFirstLineMember(e.Id);

                        if (line is not null  && await linesRepo.IsUserAttendedTo(line))
                        {
                            await linesRepo.MarkUserAsAttendedTo(line);
                            await notifier.BroadcastLineUpdate(line);
                            await linesRepo.NotifyFifthMember(e.Id);

                        }

                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
