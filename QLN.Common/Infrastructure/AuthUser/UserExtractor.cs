using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.AuthUser
{
    public static class UserExtractor
    {
        public static async Task<ApplicationUser?> GetUserFromToken(
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid userGuid))
            {
                return null;
            }

            return await userManager.FindByIdAsync(userId);
        }

        public static Guid? GetUserIdFromToken(HttpContext httpContext)
        {
            var userIdClaim = httpContext.User.Claims
                .FirstOrDefault(c => c.Type.ToLower().Contains("nameidentifier"));

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userGuid))
            {
                return null;
            }

            return userGuid;
        }


        public static string? GetUserEmailFromToken(HttpContext httpContext)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.Email);
        }

        public static string? GetUserPhoneFromToken(HttpContext httpContext)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.MobilePhone);
        }

        public static string? GetUserNameFromToken(HttpContext httpContext)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.Name);
        }

        public static List<string> GetUserRolesFromToken(HttpContext httpContext)
        {
            return httpContext.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        }
    }
}
