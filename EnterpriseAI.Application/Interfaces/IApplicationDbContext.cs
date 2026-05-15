using EnterpriseAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAI.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

