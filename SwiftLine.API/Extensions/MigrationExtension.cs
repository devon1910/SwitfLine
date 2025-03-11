using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SwiftLine.API.Extensions
{
    public static class MigrationExtension
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            SwiftLineDatabaseContext ApplicationDBContext = scope.ServiceProvider.GetRequiredService<SwiftLineDatabaseContext>();
            ApplicationDBContext.Database.Migrate();
        }
    }
}
