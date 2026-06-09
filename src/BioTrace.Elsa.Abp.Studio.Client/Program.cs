using BioTrace.Elsa.Abp.Studio;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.AddBioTraceElsaAbpStudio();

var app = builder.Build();
await app.RunBioTraceElsaAbpStudioAsync();
