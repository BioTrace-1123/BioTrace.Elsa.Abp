using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BioTrace.Elsa.Abp.ElsaStudio.Extensions;

public static class AbpElsaStudioLocalizationExtensions
{
    public static async Task<WebAssemblyHost> UseAbpElsaStudioLocalizationAsync(this WebAssemblyHost host)
    {
        var js = host.Services.GetRequiredService<IJSRuntime>();
        var cultureName = await js.InvokeAsync<string?>("blazorCulture.get");

        if (string.IsNullOrWhiteSpace(cultureName))
        {
            cultureName = "en-US";
            await js.InvokeVoidAsync("blazorCulture.set", cultureName);
        }

        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        return host;
    }
}
