using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Requests.Domain.Entities;

namespace Requests.Infrastructure.Persistence;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    private const int RequestNumberMaxLength = 20;

    // SQLite has no date type and reads dates back as DateTimeKind.Unspecified.
    // All dates are stored as UTC, so mark them as UTC again when reading.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.Property(x => x.RequestNumber)
            .IsRequired()
            .HasMaxLength(RequestNumberMaxLength);

        builder.Property(x => x.CreatedAt).HasConversion(UtcConverter);
        builder.Property(x => x.UpdatedAt).HasConversion(UtcConverter);

        builder.HasIndex(x => x.RequestNumber).IsUnique();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequestType);
        builder.HasIndex(x => new { x.OwnerId, x.CreatedAt });
        builder.HasIndex(x => new { x.AssignedToUserId, x.CreatedAt });
    }
}
