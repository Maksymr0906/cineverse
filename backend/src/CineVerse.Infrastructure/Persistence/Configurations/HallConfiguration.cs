using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class HallConfiguration : IEntityTypeConfiguration<Hall>
{
    public void Configure(EntityTypeBuilder<Hall> builder)
    {
        builder.Property(h => h.Name).HasMaxLength(50).IsRequired();
        builder.Property(h => h.Type).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(h => h.Seats)
            .WithOne(s => s.Hall)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(h => h.Sessions)
            .WithOne(s => s.Hall)
            .HasForeignKey(s => s.HallId)
            .OnDelete(DeleteBehavior.Restrict); // не видаляти зал, якщо є сеанси
    }
}
