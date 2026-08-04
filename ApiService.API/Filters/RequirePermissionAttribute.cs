using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ApiService.Application.Interfaces;

namespace ApiService.API.Filters
{
    /// <summary>
    /// Checks that the authenticated user has a specific permission claim in their JWT.
    /// Permissions are issued by AuthService and embedded in the JWT by JwtService.
    ///
    /// Usage: [RequirePermission("products.create")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _permission;

        public RequirePermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

            if (!currentUser.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!currentUser.HasPermission(_permission))
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    errorCode = "ERR-PERM-DENIED",
                    message = $"You don't have the required permission: '{_permission}'"
                })
                { StatusCode = 403 };
            }
        }
    }
}
