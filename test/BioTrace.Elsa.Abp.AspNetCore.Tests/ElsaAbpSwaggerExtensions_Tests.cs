using Shouldly;
using Xunit;

namespace BioTrace.Elsa.Abp;

public class ElsaAbpSwaggerExtensions_Tests
{
    [Fact]
    public void OpenApi_paths_should_use_elsa_document_name()
    {
        ElsaAbpSwaggerExtensions.DocumentName.ShouldBe("elsa");
        ElsaAbpSwaggerExtensions.OpenApiJsonPath.ShouldBe("/swagger/elsa/openapi.json");
        ElsaAbpSwaggerExtensions.UiPath.ShouldBe("/swagger/elsa");
    }
}
