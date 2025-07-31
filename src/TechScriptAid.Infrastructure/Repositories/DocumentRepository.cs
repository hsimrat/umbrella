using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;
using TechScriptAid.Core.Interfaces;
using TechScriptAid.Infrastructure.Data;

namespace TechScriptAid.Infrastructure.Repositories
{
    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Document> UpdateAsync(Document document)
        {
            // Update the document
            Update(document);
            // Note: SaveChanges should be called by Unit of Work pattern
            // For now, we'll just return the document
            return document;
        }

        public async Task DeleteAsync(Document document)
        {
            // Soft delete is handled by the base Delete method
            Delete(document);
            // SaveChanges should be called by Unit of Work
            await Task.CompletedTask; // Make it async
        }

        public async Task<Document> AiSummary(Document document)
        {
            // This is a placeholder for AI summary generation
            // In Episode 3, this will integrate with Azure OpenAI
            // For now, just return the document

            // Simulate async operation
            await Task.Delay(100);

            // In real implementation, this would:
            // 1. Send document content to AI service
            // 2. Get summary back
            // 3. Update document or create analysis record

            return document;
        }

        public async Task<decimal> AiConfidenceScore(Document document)
        {
            // Placeholder for AI confidence scoring
            // Will be implemented with AI integration in Episode 3

            await Task.Delay(50);

            // Return a mock confidence score for now
            return 0.85m; // 85% confidence
        }

        public async Task<Document> GetByHashAsync(string contentHash)
        {
            return await _dbSet
                .FirstOrDefaultAsync(d => d.ContentHash == contentHash);
        }

        public async Task<IReadOnlyList<Document>> GetByTypeAsync(DocumentType documentType)
        {
            return await _dbSet
                .Where(d => d.DocumentType == documentType)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Document>> GetRecentDocumentsAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(d => d.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetDocumentsByCategoryAsync(string category)
        {
            return await _dbSet
                .Where(d => d.Tags.Contains(category)) // Changed from d.Category since Document doesn't have Category property
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetDocumentsByStatusAsync(DocumentStatus status)
        {
            // Since Document doesn't have a Status property, we'll need to handle this differently
            // For now, returning all documents - this should be implemented based on your business logic
            return await _dbSet
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<Document?> GetDocumentWithAnalysesAsync(Guid id)
        {
            return await _dbSet
                .Include(d => d.Analyses)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Document>> SearchDocumentsAsync(string searchTerm)
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _dbSet
                .Where(d => d.Title.ToLower().Contains(lowerSearchTerm) ||
                           d.Content.ToLower().Contains(lowerSearchTerm) ||
                           d.Tags.Any(t => t.ToLower().Contains(lowerSearchTerm)))
                .OrderByDescending(d => d.CreatedAt)
                .Take(50) // Limit results
                .ToListAsync();
        }
    } // This closing brace was in the wrong place
}