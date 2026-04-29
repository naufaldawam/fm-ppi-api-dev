using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyService.Domain.Entities;

namespace MyService.Application.Interfaces
{
    /// <summary>
    /// Database context interface for Clean Architecture.
    /// Add new DbSets here when you add new entities.
    /// </summary>
    public interface IServiceDbContext
    {
        // ===================================
        // ADD YOUR DBSETS HERE
        // Example: DbSet<Order> Orders { get; set; }
        // ===================================
        DbSet<Product> Products { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Current user context - populated from JWT claims by middleware
    /// </summary>
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? Email { get; }
        string[] Roles { get; }
        string[] Permissions { get; }
        bool IsAuthenticated { get; }
        bool HasPermission(string permission);
        bool HasRole(string role);
    }
}
