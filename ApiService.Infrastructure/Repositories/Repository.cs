using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Domain.Entities;
using ApiService.Infrastructure.Persistence;

namespace ApiService.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository — works for any entity extending BaseEntity.
    /// Usage: inject IRepository<Product> in your service.
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(string id);
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, int skip = 0, int take = 50);
        Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task SoftDeleteAsync(string id, string? deletedBy = null);
    }

    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly ServiceDbContext _context;
        private readonly DbSet<T> _set;

        public Repository(ServiceDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public Task<T?> GetByIdAsync(string id)
            => _set.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        public Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, int skip = 0, int take = 50)
        {
            var query = _set.Where(e => !e.IsDeleted);
            if (filter != null) query = query.Where(filter);
            return query.OrderByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToListAsync();
        }

        public Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        {
            var query = _set.Where(e => !e.IsDeleted);
            if (filter != null) query = query.Where(filter);
            return query.CountAsync();
        }

        public async Task<T> AddAsync(T entity)
        {
            _set.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _set.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(string id, string? deletedBy = null)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = deletedBy;
                await _context.SaveChangesAsync();
            }
        }
    }
}
