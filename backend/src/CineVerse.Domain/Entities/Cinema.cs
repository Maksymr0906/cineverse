using CineVerse.Domain.Common;

namespace CineVerse.Domain.Entities;

/// <summary>Кінотеатр мережі (одна фізична локація).</summary>
public class Cinema : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public ICollection<Hall> Halls { get; set; } = new List<Hall>();
}
