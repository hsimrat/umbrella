using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechScriptAid.Core.Entities;
using System.Text.Json;

namespace TechScriptAid.Infrastructure.Data.Configurations
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("Documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(d => d.Content)
                .IsRequired();

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.ContentHash)
                .IsRequired()
                .HasMaxLength(128);

            // Configure Tags with value comparer
            builder.Property(d => d.Tags)
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(500)
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ));

            // Configure Metadata with value comparer
            builder.Property(d => d.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), new JsonSerializerOptions()),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()) ?? new Dictionary<string, string>()
                )
                .HasColumnType("nvarchar(max)")
                .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToDictionary(entry => entry.Key, entry => entry.Value)
                ));

            // Relationships
            builder.HasMany(d => d.Analyses)
                .WithOne(a => a.Document)
                .HasForeignKey(a => a.DocumentId);

            // Query filter for soft delete
            builder.HasQueryFilter(d => !d.IsDeleted);
        }
    }
}