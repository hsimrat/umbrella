using System.ComponentModel.DataAnnotations;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.API.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public int AnalysisCount { get; set; }
    }

    public class CreateDocumentDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long FileSize { get; set; }

        public List<string> Tags { get; set; } = new List<string>();

        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class UpdateDocumentDto
    {
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public string Content { get; set; }

        public DocumentType? DocumentType { get; set; }

        public List<string> Tags { get; set; }

        public Dictionary<string, string> Metadata { get; set; }
    }

    public class DocumentAnalysisDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public AnalysisType AnalysisType { get; set; }
        public AnalysisStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? DurationInSeconds { get; set; }
        public string ModelUsed { get; set; }
        public decimal? Cost { get; set; }
        public string Summary { get; set; }
        public List<string> Keywords { get; set; }
        public string Sentiment { get; set; }
        public decimal? SentimentScore { get; set; }
        public Dictionary<string, object> Results { get; set; }
    }

    public enum DocumentStatus
    {
        Active,
        Archived,
        Processing,
        Failed
    }
    public class DocumentSearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; }
        public DocumentType DocumentType { get; set; }
        public int AnalysisCount { get; set; }
    }


}// This code defines the Data Transfer Objects (DTOs) for the Document entity in the TechScriptAid API.