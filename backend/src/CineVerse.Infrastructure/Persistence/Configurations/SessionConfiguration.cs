using CineVerse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineVerse.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.Property(s => s.BasePrice).HasColumnType("decimal(10,2)");
        builder.Property(s => s.Format).HasMaxLength(20);

        builder.HasOne(s => s.Movie).WithMany(m => m.Sessions).HasForeignKey(s => s.MovieId);
        builder.HasOne(s => s.Hall).WithMany(h => h.Sessions).HasForeignKey(s => s.HallId);

        // Індекс для швидкого пошуку конфліктів розкладу в межах залу
        builder.HasIndex(s => new { s.HallId, s.StartTime, s.EndTime });
    }
}
