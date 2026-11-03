using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
{
    public void Configure(EntityTypeBuilder<Cinema> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Address).HasColumnType("text");
        builder.Property(c => c.City).HasMaxLength(50);
        builder.Property(c => c.Phone).HasMaxLength(20);

        builder.HasIndex(c => c.City);

        builder.HasMany(c => c.Halls)
            .WithOne(h => h.Cinema)
            .HasForeignKey(h => h.CinemaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
