<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>
    <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>
    <RootNamespace>{{RootNamespace}}</RootNamespace>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Align static web assets with ElsaStudio:PathBase (default /studio). -->
    <StaticWebAssetBasePath>studio</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="BioTrace.Elsa.Abp.Studio.BlazorWasm" Version="{{PackageVersion}}" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.8" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.8" PrivateAssets="all" />
  </ItemGroup>

</Project>
