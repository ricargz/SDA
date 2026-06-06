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
            const string seedSalt = "$2a$11$abcdefghijklmnopqrstuu";

            modelBuilder.Entity<User>()
                .Property(user => user.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin#2026!", seedSalt), Email = "admin@test.com", Balance = 1000m, CreatedAt = seedDate },
                new User { Id = 2, Username = "user1", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User1#2026!", seedSalt), Email = "user@test.com", Balance = 500m, CreatedAt = seedDate },
                new User { Id = 3, Username = "user2", PasswordHash = BCrypt.Net.BCrypt.HashPassword("User2#2026!", seedSalt), Email = "user2@test.com", Balance = 750m, CreatedAt = seedDate }
            );
        }
    }
}
