using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.Property(t => t.TicketCode).HasMaxLength(50).IsRequired();
        builder.Property(t => t.QrCode).HasColumnType("text");
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => t.TicketCode).IsUnique(); // критично для валідації контролером

        builder.HasOne(t => t.Booking).WithMany(b => b.Tickets).HasForeignKey(t => t.BookingId);
        builder.HasOne(t => t.Session).WithMany(s => s.Tickets).HasForeignKey(t => t.SessionId);
        // Seat не має зворотної навігаційної колекції Tickets — достатньо однобічного зв'язку
        builder.HasOne(t => t.Seat).WithMany().HasForeignKey(t => t.SeatId);
    }
}
