using System.Text.Json;
using System.Text;

public static class JwtHelper
{
    public static string? GetEmailFromJwt(string jwtToken)
    {
        var parts = jwtToken.Split('.');
        if (parts.Length != 3)
            return null;

        var payload = parts[1];
        var jsonBytes = Convert.FromBase64String(PadBase64(payload));
        var json = Encoding.UTF8.GetString(jsonBytes);

        using var doc = JsonDocument.Parse(json);
        var emailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";

        if (doc.RootElement.TryGetProperty(emailClaim, out var email))
        {
            return email.GetString();
        }

        return null;
    }

    private static string PadBase64(string base64)
    {
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }
}
