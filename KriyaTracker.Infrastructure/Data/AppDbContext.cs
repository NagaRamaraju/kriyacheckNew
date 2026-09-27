using Microsoft.EntityFrameworkCore;
using KriyaTracker.Domain.Entities;

namespace KriyaTracker.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<MeditationSession> Sessions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            // Ensures the database is created
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<MeditationSession>()
                .HasKey(s => s.Id);

            // Mock Data Seeding
            modelBuilder.Entity<MeditationSession>().HasData(
                new MeditationSession
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    StartTime = DateTime.UtcNow.AddDays(-2),
                    EndTime = DateTime.UtcNow.AddDays(-2).AddMinutes(20),
                    TotalKriyas = 12,
                    TotalDurationMinutes = 20,
                    Notes = "Morning practice"
                },
                new MeditationSession
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(35),
                    TotalKriyas = 24,
                    TotalDurationMinutes = 35,
                    Notes = "Deep meditation"
                },
                new MeditationSession
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    StartTime = DateTime.UtcNow.AddHours(-2),
                    EndTime = DateTime.UtcNow.AddHours(-2).AddMinutes(15),
                    TotalKriyas = 12,
                    TotalDurationMinutes = 15,
                    Notes = "Quick session"
                }
            );
        }
    }
}
