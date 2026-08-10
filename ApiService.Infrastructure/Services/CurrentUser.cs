using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ApiService.Application.Interfaces;

namespace ApiService.Infrastructure.Services
{
    /// <summary>
    /// Reads the current user's identity from JWT claims injected by AuthService.
    /// Registered as Scoped - one instance per HTTP request.
    /// </summary>
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

        public string[] Roles => User?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray() ?? Array.Empty<string>();

        public string[] Permissions => User?.FindAll("permission")
            .Select(c => c.Value)
            .ToArray() ?? Array.Empty<string>();

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public bool HasPermission(string permission)
            => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

        public bool HasRole(string role)
            => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
