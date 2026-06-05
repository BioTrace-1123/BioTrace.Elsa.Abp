using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace BioTrace.Elsa.Abp.Helpers;

public static class JwtPayloadReader
{
    public static IReadOnlyList<(string Type, string Value)> ReadClaims(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        return token.Claims.Select(c => (c.Type, c.Value)).ToList();
    }

    public static bool ContainsClaim(string accessToken, string claimType)
    {
        return ReadClaims(accessToken).Any(c =>
            string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));
    }

    public static JsonElement ReadPayload(string accessToken)
    {
        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Invalid JWT access token.");
        }

        var payloadBytes = Base64UrlDecode(parts[1]);
        using var document = JsonDocument.Parse(payloadBytes);
        return document.RootElement.Clone();
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }
}
