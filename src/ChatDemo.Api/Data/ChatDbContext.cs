using Microsoft.EntityFrameworkCore;
using ChatDemo.Api.Models;

namespace ChatDemo.Api.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).HasMaxLength(64).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
            e.Property(u => u.GrpcAddress).HasMaxLength(256);
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).HasMaxLength(4000).IsRequired();

            e.HasOne(m => m.Sender)
             .WithMany(u => u.SentMessages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(m => m.Receiver)
             .WithMany(u => u.ReceivedMessages)
             .HasForeignKey(m => m.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
