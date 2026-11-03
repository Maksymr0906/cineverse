using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(10,2)");
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(10,2)");

        builder.HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId);
        builder.HasOne(b => b.Session).WithMany(s => s.Bookings).HasForeignKey(b => b.SessionId);
        builder.HasOne(b => b.Promotion).WithMany().HasForeignKey(b => b.PromotionId).OnDelete(DeleteBehavior.SetNull);

        // Для фонового ExpiredBookingWorker: швидкий пошук PENDING-бронювань, що прострочились
        builder.HasIndex(b => new { b.Status, b.ExpiresAt });

        builder.HasOne(b => b.Payment)
            .WithOne(p => p.Booking)
            .HasForeignKey<Payment>(p => p.BookingId);
    }
}
