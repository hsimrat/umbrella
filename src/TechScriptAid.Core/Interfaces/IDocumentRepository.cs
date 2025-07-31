using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Interfaces
{
    public interface IDocumentRepository : IGenericRepository<Document>
    {

        Task<Document> UpdateAsync(Document document);
        Task DeleteAsync(Document document);
        Task<Document> AiSummary(Document document);
        Task<decimal> AiConfidenceScore(Document document);
                // You might also want these common document-specific methods
        Task<Document> GetByHashAsync(string contentHash);
        Task<IReadOnlyList<Document>> GetByTypeAsync(DocumentType documentType);
        Task<IReadOnlyList<Document>> GetRecentDocumentsAsync(int count);

        Task<IEnumerable<Document>> GetDocumentsByCategoryAsync(string category);
        //Task<IEnumerable<Document>> GetDocumentsByStatusAsync(DocumentStatus status);
        Task<Document?> GetDocumentWithAnalysesAsync(Guid id);
        Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm);
    }
}
