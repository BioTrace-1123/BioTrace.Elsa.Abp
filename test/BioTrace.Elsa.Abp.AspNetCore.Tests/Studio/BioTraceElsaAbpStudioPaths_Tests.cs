using BioTrace.Elsa.Abp.Studio;
using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp.Studio;

public class BioTraceElsaAbpStudioPaths_Tests
{
    [Theory]
    [InlineData("/authentication/login", "/studio", "/studio/authentication/login")]
    [InlineData("/authentication/login-callback", "/studio", "/studio/authentication/login-callback")]
    [InlineData("/authentication/logout", "/studio", "/studio/authentication/logout")]
    public void TryGetLegacyAuthenticationRedirect_Should_Rewrite_Root_Authentication_Routes(
        string requestPath,
        string pathBase,
        string expectedRedirect)
    {
        var redirected = BioTraceElsaAbpStudioPaths.TryGetLegacyAuthenticationRedirect(
            requestPath,
            pathBase,
            out var redirectPath);

        redirected.ShouldBeTrue();
        redirectPath.ShouldBe(expectedRedirect);
    }

    [Theory]
    [InlineData("/studio/authentication/login", "/studio")]
    [InlineData("/studio/workflows", "/studio")]
    [InlineData("/Account/Login", "/studio")]
    [InlineData("/authentication/login", "/")]
    public void TryGetLegacyAuthenticationRedirect_Should_Not_Rewrite_Already_Scoped_Or_Unrelated_Paths(
        string requestPath,
        string pathBase)
    {
        BioTraceElsaAbpStudioPaths.TryGetLegacyAuthenticationRedirect(
                requestPath,
                pathBase,
                out _)
            .ShouldBeFalse();
    }

    [Fact]
    public void GetAuthenticationLoginPath_Should_Include_PathBase()
    {
        BioTraceElsaAbpStudioPaths.GetAuthenticationLoginPath("/studio")
            .ShouldBe("/studio/authentication/login");
    }
}
