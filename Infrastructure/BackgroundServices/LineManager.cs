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
                
                using (var scope = serviceProvider.CreateScope()) 
                {
                    var queuesRepo = scope.ServiceProvider.GetRequiredService<ILineRepo>();
                    var eventsRepo = scope.ServiceProvider.GetRequiredService<IEventRepo>();

                    // Initialize cache and refresh tracking
                    IEnumerable<Event> cachedEvents = await eventsRepo.GetActiveEvents(); ;
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
                            Line? line = await queuesRepo.GetFirstLineMember(e.Id);

                            if (line is not null && await queuesRepo.IsUserAttendedTo(line))
                            {
                                await queuesRepo.MarkUserAsAttendedTo(line);
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
