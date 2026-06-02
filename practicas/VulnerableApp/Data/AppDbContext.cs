using Microsoft.EntityFrameworkCore;
using VulnerableApp.Models;

namespace VulnerableApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Local);

            modelBuilder.Entity<User>()
                .Property(user => user.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin", Email = "admin@test.com", Balance = 1000m, CreatedAt = seedDate },
                new User { Id = 2, Username = "user1", Password = "123456", Email = "user@test.com", Balance = 500m, CreatedAt = seedDate },
                new User { Id = 3, Username = "user2", Password = "password", Email = "user2@test.com", Balance = 750m, CreatedAt = seedDate }
            );
        }
    }
}
