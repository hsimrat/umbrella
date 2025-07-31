using Microsoft.EntityFrameworkCore;
using TechScriptAid.API.DTOs;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using System.Linq;

// Add this alias to resolve ambiguity
using DocumentStatus = TechScriptAid.API.DTOs.DocumentStatus;

namespace TechScriptAid.API.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DocumentService(IDocumentRepository documentRepository, IUnitOfWork unitOfWork)
        {
            _documentRepository = documentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync(string? category = null, DocumentStatus? status = null)
        {
            // Get all documents first
            var allDocuments = await _documentRepository.GetAllAsync();

            var documents = allDocuments.ToList();

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(category))
            {
                documents = documents.Where(d => d.Tags != null && d.Tags.Contains(category)).ToList();
            }

            // Note: Status filtering would need to be implemented based on your business logic
            // since Document entity doesn't have a Status property

            // Map to DTOs
            return documents.Select(MapToDto).ToList();
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            return document != null ? MapToDto(document) : null;
        }

        public async Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto)
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Title = createDto.Title,
                Description = createDto.Description ?? string.Empty,
                Content = createDto.Content,
                DocumentType = createDto.DocumentType,
                FileName = createDto.FileName,
                FileSize = createDto.FileSize,
                Tags = createDto.Tags ?? new List<string>(),
                Metadata = createDto.Metadata ?? new Dictionary<string, string>(),
                ContentHash = GenerateHash(createDto.Content),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            var created = await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(created);
        }

        public async Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null)
                return false;

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateDto.Title))
                document.Title = updateDto.Title;

            if (!string.IsNullOrEmpty(updateDto.Description))
                document.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.Content))
            {
                document.Content = updateDto.Content;
                document.ContentHash = GenerateHash(updateDto.Content);
            }

            if (updateDto.DocumentType.HasValue)
                document.DocumentType = updateDto.DocumentType.Value;

            if (updateDto.Tags != null)
                document.Tags = updateDto.Tags;

            if (updateDto.Metadata != null)
                document.Metadata = updateDto.Metadata;

            document.UpdatedAt = DateTime.UtcNow;

            _documentRepository.Update(document);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            var document = await _documentRepository.GetByIdAsync(id);
            if (document == null)
                return false;

            _documentRepository.Delete(document);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(string searchTerm)
        {
            var documents = await _documentRepository.SearchDocumentsAsync(searchTerm);
            var documentList = documents.ToList();
            return documentList.Select(MapToDto).ToList();
        }

        public async Task<DocumentAnalysisDto?> AnalyzeDocumentAsync(Guid documentId)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
                return null;

            // This will be implemented in Episode 3 with AI integration
            await Task.Delay(100); // Simulate async operation

            return new DocumentAnalysisDto
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisType = AnalysisType.Summary,
                Status = AnalysisStatus.Pending,
                StartedAt = DateTime.UtcNow,
                ModelUsed = "gpt-4",
                Summary = "Analysis will be implemented in Episode 3",
                Keywords = new List<string> { "placeholder", "analysis" },
                Results = new Dictionary<string, object>()
            };
        }

        private static DocumentDto MapToDto(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                Description = document.Description ?? string.Empty,
                Content = document.Content,
                DocumentType = document.DocumentType,
                FileName = document.FileName,
                FileSize = document.FileSize,
                Tags = document.Tags ?? new List<string>(),
                Metadata = document.Metadata ?? new Dictionary<string, string>(),
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt,
                CreatedBy = document.CreatedBy ?? "System",
                AnalysisCount = document.Analyses?.Count ?? 0
            };
        }

        private static string GenerateHash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}