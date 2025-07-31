using TechScriptAid.API.DTOs;
using TechScriptAid.Core.Interfaces;

namespace TechScriptAid.API.Services
{
    public interface IDocumentService
    {




        Task<IEnumerable<DocumentDto>> GetDocumentsAsync(string? category = null, DocumentStatus? status = null);
        Task<DocumentDto?> GetDocumentByIdAsync(Guid id);
        Task<DocumentDto> CreateDocumentAsync(CreateDocumentDto createDto);
        Task<bool> UpdateDocumentAsync(Guid id, UpdateDocumentDto updateDto);
        Task<bool> DeleteDocumentAsync(Guid id);
        Task<IEnumerable<DocumentDto>> SearchDocumentsAsync(string searchTerm);
        Task<DocumentAnalysisDto?> AnalyzeDocumentAsync(Guid documentId);
    }
}
