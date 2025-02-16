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
                    bool callGetLines= true;
                    List<Line> lines= [];
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        if (callGetLines)
                        {
                            lines = await queuesRepo.GetLines();
                            callGetLines = false;
                        }
                      

                        if (await queuesRepo.IsUserAttendedTo(lines[0]))
                        {
                            await queuesRepo.MarkUserAsAttendedTo(lines[0]);
                            callGetLines = true;
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
