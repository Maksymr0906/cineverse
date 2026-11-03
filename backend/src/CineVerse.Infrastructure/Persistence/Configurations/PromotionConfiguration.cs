using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.DiscountType).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.DiscountValue).HasColumnType("decimal(10,2)");

        builder.HasIndex(p => p.Code).IsUnique();
    }
}
