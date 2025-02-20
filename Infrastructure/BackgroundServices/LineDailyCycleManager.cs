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
    public class LineDailyCycleManager(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var eventsRepo = scope.ServiceProvider.GetRequiredService<IEventRepo>();

                    // Initialize cache and refresh tracking
                    IEnumerable<Event> cachedEvents = [];
                    DateTime lastCacheRefresh = DateTime.MinValue;
                    TimeSpan cacheRefreshInterval = TimeSpan.FromSeconds(60); // Adjust interval as needed

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
                            TimeOnly currentTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));

                            if (currentTime >= e.EventStartTime && currentTime <= e.EventEndTime && !e.IsOngoing) 
                            {
                                await eventsRepo.UpdateEventVisibility(e.Id, true);
                            }

                            if (!(currentTime >= e.EventStartTime && currentTime <= e.EventEndTime) && e.IsOngoing)
                            {
                                await eventsRepo.UpdateEventVisibility(e.Id, false);
                            }
                          
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
