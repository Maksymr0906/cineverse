using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.Property(bs => bs.Price).HasColumnType("decimal(10,2)");

        builder.HasOne(bs => bs.Booking).WithMany(b => b.BookingSeats).HasForeignKey(bs => bs.BookingId);
        builder.HasOne(bs => bs.Seat).WithMany(s => s.BookingSeats).HasForeignKey(bs => bs.SeatId);

        // Одне місце не може бути двічі у складі того самого бронювання
        builder.HasIndex(bs => new { bs.BookingId, bs.SeatId }).IsUnique();
    }
}
