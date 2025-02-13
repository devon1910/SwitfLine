using Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class SwiftLineDatabaseContext  : IdentityDbContext<SwiftLineUser>
    {
        public SwiftLineDatabaseContext(DbContextOptions<SwiftLineDatabaseContext> options)
           : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<QueueMember>()
                .Property(e => e.BasePriority)
                .HasConversion<string>();
           

            //modelBuilder.HasDefaultSchema("SmartDelivery");
        }
        public DbSet<SwiftLineUser> SwiftLineUsers { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<QueueMember> QueueItems { get; set; }
    }
}
