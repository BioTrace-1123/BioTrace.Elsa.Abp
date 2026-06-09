using System.Globalization;
using Elsa.Studio.Localization.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BioTrace.Elsa.Abp.ElsaStudio.Services;

/// <summary>
/// Uses the globally loaded <c>blazorCulture</c> script (see index.html) instead of dynamic ES module import.
/// </summary>
public class AbpElsaStudioCultureService : ICultureService
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public AbpElsaStudioCultureService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public virtual async Task ChangeCultureAsync(CultureInfo culture)
    {
        if (CultureInfo.CurrentUICulture.Name == culture.Name)
        {
            return;
        }

        await _jsRuntime.InvokeVoidAsync("blazorCulture.set", culture.Name);
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
    }
}
