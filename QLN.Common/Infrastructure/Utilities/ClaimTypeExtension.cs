using System.Security.Claims;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class ClaimTypesExtension
    {
        public static Guid GetId(this ClaimsPrincipal user)
        {
            try
            {
                var id = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

                if (Guid.TryParse(id, out Guid result))
                {
                    return result;
                }

                return Guid.Empty;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public static string GetName(this ClaimsPrincipal user)
        {
            try
            {
                return user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetGivenName(this ClaimsPrincipal user)
        {
            try
            {
                return user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetEmail(this ClaimsPrincipal user)
        {
            try
            {
                return user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
