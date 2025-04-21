using Microsoft.Extensions.Diagnostics.HealthChecks;
using SwiftLine.API.Extensions.Health.FeedbackService.Api.HealthCheck;
using SwiftLine.API.Extensions.Health.FeedbackService.Api;

namespace SwiftLine.API.Extensions.Health
{
    public static class HealthCheck
    {

        public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            string apiUrl = "http://localhost:5267/api/v1/heartbeats/ping";
            services.AddHealthChecks()
                .AddNpgSql(configuration.GetConnectionString("Database"), healthQuery: "select 1", name: "PostGre Server", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Feedback", "Database" })
                .AddCheck<RemoteHealthCheck>("Remote endpoints Health Check", failureStatus: HealthStatus.Unhealthy)
                .AddCheck<MemoryHealthCheck>($"Feedback Service Memory Check", failureStatus: HealthStatus.Unhealthy, tags: new[] { "Feedback Service" })
                .AddUrlGroup(new Uri(apiUrl), name: "base URL", failureStatus: HealthStatus.Unhealthy);

            services.AddHealthChecksUI(opt =>
            {
                opt.SetEvaluationTimeInSeconds(10); //time in seconds between check    
                opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
                opt.SetApiMaxActiveRequests(1); //api requests concurrency    
                opt.AddHealthCheckEndpoint("feedback api", "/api/health"); //map health check api    

            }).AddInMemoryStorage();


        }
    }
}
