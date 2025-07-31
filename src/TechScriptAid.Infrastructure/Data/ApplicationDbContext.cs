using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;
using System.Text.Json;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _currentUser;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // In a real application, this would come from IHttpContextAccessor or similar
            _currentUser = "System";
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, string currentUser)
            : base(options)
        {
            _currentUser = currentUser ?? "System";
        }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }

        public DbSet<AIOperation> AIOperations { get; set; }
        public DbSet<AIOperationDocument> AIOperationDocuments { get; set; }
        public DbSet<DocumentEmbedding> DocumentEmbeddings { get; set; }
        public DbSet<AIPromptTemplate> AIPromptTemplates { get; set; }
        public DbSet<AIPromptTemplateVersion> AIPromptTemplateVersions { get; set; }
        public DbSet<AICache> AICaches { get; set; }
        public DbSet<AIRateLimitTracker> AIRateLimitTrackers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());


            // AI Operation configurations
            modelBuilder.Entity<AIOperation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OperationType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.RequestId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
                entity.Property(e => e.RequestContent).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ResponseContent).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.OperationType);
            });

            // AI Operation Document junction table
            modelBuilder.Entity<AIOperationDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RelationType).HasMaxLength(50).IsRequired();
                entity.HasOne(e => e.AIOperation)
                    .WithMany(o => o.Documents)
                    .HasForeignKey(e => e.AIOperationId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Document)
                    .WithMany()
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.AIOperationId, e.DocumentId });
            });

            // Document Embedding configurations
            modelBuilder.Entity<DocumentEmbedding>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ChunkId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ChunkText).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Embedding)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions)null));
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                entity.HasOne(e => e.Document)
                    .WithMany()
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.ChunkIndex);
            });

            // AI Prompt Template configurations
            modelBuilder.Entity<AIPromptTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Template).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.SystemPrompt).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Version).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Parameters)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsActive);
            });

            // AI Prompt Template Version configurations
            modelBuilder.Entity<AIPromptTemplateVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Template).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(e => e.SystemPrompt).HasColumnType("nvarchar(max)");
                entity.Property(e => e.ChangedBy).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ChangeDescription).HasMaxLength(500);
                entity.HasOne(e => e.PromptTemplate)
                    .WithMany(p => p.Versions)
                    .HasForeignKey(e => e.PromptTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.PromptTemplateId, e.Version }).IsUnique();
            });

            // AI Cache configurations
            modelBuilder.Entity<AICache>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CacheKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OperationType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.RequestHash).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Response).HasColumnType("nvarchar(max)");
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));
                entity.HasIndex(e => e.CacheKey).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.LastAccessedAt);
            });

            // AI Rate Limit Tracker configurations
            modelBuilder.Entity<AIRateLimitTracker>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Resource).HasMaxLength(200).IsRequired();
                entity.HasIndex(e => new { e.UserId, e.Resource, e.WindowStart }).IsUnique();
                entity.HasIndex(e => e.WindowStart);
                entity.HasIndex(e => e.ThrottledUntil);
            });

            // Global configurations
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Configure decimal properties
                var decimalProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));

                foreach (var property in decimalProperties)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasPrecision(18, 4);
                }
            }
        }

        public override int SaveChanges()
        {
            OnBeforeSaving();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            OnBeforeSaving();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

            var currentTime = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedAt = currentTime;
                        entity.CreatedBy = _currentUser;
                        entity.UpdatedAt = currentTime;
                        entity.UpdatedBy = _currentUser;
                        entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entity.UpdatedAt = currentTime;
                        entity.UpdatedBy = _currentUser;
                        // Ensure CreatedAt/CreatedBy are not modified
                        entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                        break;

                    case EntityState.Deleted:
                        // Implement soft delete
                        entry.State = EntityState.Modified;
                        entity.IsDeleted = true;
                        entity.DeletedAt = currentTime;
                        entity.DeletedBy = _currentUser;
                        entity.UpdatedAt = currentTime;
                        entity.UpdatedBy = _currentUser;
                        break;
                }
            }
        }

        // Method to permanently delete soft-deleted records (use with caution)
        public void PermanentlyDelete<T>(T entity) where T : BaseEntity
        {
            base.Remove(entity);
        }

        // Method to restore soft-deleted records
        public void Restore<T>(T entity) where T : BaseEntity
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedBy = null;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = _currentUser;
        }
    }
}