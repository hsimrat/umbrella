using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechScriptAid.Core.Entities;
using System.Text.Json;

namespace TechScriptAid.Infrastructure.Data.Configurations
{
    public class DocumentAnalysisConfiguration : IEntityTypeConfiguration<DocumentAnalysis>
    {
        public void Configure(EntityTypeBuilder<DocumentAnalysis> builder)
        {
            builder.ToTable("DocumentAnalyses");

            builder.HasKey(da => da.Id);

            builder.Property(da => da.AnalysisType)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(da => da.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(AnalysisStatus.Pending);

            builder.Property(da => da.Cost)
                .HasPrecision(10, 4);

            builder.Property(da => da.SentimentScore)
                .HasPrecision(5, 4);

            // Configure Keywords as comma-separated string
            builder.Property(da => da.Keywords)
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                )
                .HasMaxLength(1000);

            // For Results, either change the entity to use ResultsJson string property
            // Or remove this configuration if you don't need to persist Results

            // Option: Ignore Results if it's computed/temporary
            builder.Ignore(da => da.Results);

            // Query filter for soft delete
            builder.HasQueryFilter(da => !da.IsDeleted);
        }
    }
}