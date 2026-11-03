using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);

        // В межах одного залу не може бути двох однакових місць (ряд+місце)
        builder.HasIndex(s => new { s.HallId, s.RowNumber, s.SeatNumber }).IsUnique();
    }
}
