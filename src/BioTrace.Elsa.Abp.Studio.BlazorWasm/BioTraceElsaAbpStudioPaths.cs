namespace BioTrace.Elsa.Abp.Studio;

public static class BioTraceElsaAbpStudioPaths
{
    private const string AuthenticationPrefix = "/authentication/";

    public static string NormalizePathBase(string pathBase)
    {
        if (string.IsNullOrWhiteSpace(pathBase))
        {
            return "/studio";
        }

        var normalized = pathBase.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.TrimEnd('/');
    }

    /// <summary>
    /// Elsa Studio WASM uses absolute <c>/authentication/*</c> routes in <c>NavigateToLogin</c>.
    /// When Studio is hosted under <see cref="BioTraceElsaAbpStudioOptions.PathBase"/> (e.g. <c>/studio</c>),
    /// rewrite those requests to the PathBase-scoped SPA routes.
    /// </summary>
    public static bool TryGetLegacyAuthenticationRedirect(
        string? requestPath,
        string pathBase,
        out string redirectPath)
    {
        redirectPath = string.Empty;

        var normalizedPathBase = NormalizePathBase(pathBase);
        if (normalizedPathBase is "/" or "")
        {
            return false;
        }

        if (string.IsNullOrEmpty(requestPath)
            || !requestPath.StartsWith(AuthenticationPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (requestPath.StartsWith(normalizedPathBase + "/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestPath, normalizedPathBase, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        redirectPath = normalizedPathBase + requestPath;
        return true;
    }

    public static string GetAuthenticationLoginPath(string pathBase)
    {
        return $"{NormalizePathBase(pathBase)}/authentication/login";
    }
}
