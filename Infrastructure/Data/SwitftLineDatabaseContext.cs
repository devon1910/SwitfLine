using Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class SwiftLineDatabaseContext : IdentityDbContext<SwiftLineUser>
    {
        public SwiftLineDatabaseContext(DbContextOptions<SwiftLineDatabaseContext> options)
           : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<SwiftLineUser> SwiftLineUsers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Line> Lines { get; set; }
        public DbSet<TokenInfo> TokenInfos { get; set; }
        public DbSet<PushNotification> PushNotifications { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
    }
}
