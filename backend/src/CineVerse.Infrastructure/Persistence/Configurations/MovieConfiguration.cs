using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(m => m.Title).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Description).HasColumnType("text");
        builder.Property(m => m.AgeRating).HasMaxLength(10);
        builder.Property(m => m.PosterUrl).HasColumnType("text");
        builder.Property(m => m.TrailerUrl).HasColumnType("text");
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        // Швидка вибірка афіші за статусом (NOW_SHOWING / COMING_SOON)
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.Title);
    }
}
