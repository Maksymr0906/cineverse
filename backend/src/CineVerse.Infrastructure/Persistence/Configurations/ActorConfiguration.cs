using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class ActorConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> builder)
    {
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.PhotoUrl).HasColumnType("text");
    }
}
