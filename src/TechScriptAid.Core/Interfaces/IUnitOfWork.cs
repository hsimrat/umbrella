using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechScriptAid.Core.Entities;

namespace TechScriptAid.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IDocumentRepository Documents { get; }
        IGenericRepository<DocumentAnalysis> DocumentAnalyses { get; }

        // Fix: Constrain T to BaseEntity to satisfy the generic constraint of IGenericRepository<T>
        IGenericRepository<T> Repository<T>() where T : BaseEntity;

        Task<int> SaveChangesAsync();

        // Add this method (it's just an alias for SaveChangesAsync)
        Task<int> CompleteAsync();

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
