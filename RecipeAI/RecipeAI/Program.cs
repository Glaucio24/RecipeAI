using Microsoft.AspNetCore.Components;
using RecipeAI.Client.Pages;
using RecipeAI.Components;
using RecipeAI.Components.Layout;
using RecipeAI.Models;
using System.Diagnostics;
using System.Net.Http;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton<RecipeState>();
builder.Services.AddHttpClient();


builder.Services.AddHttpClient("RecipeAPI", client =>
{
    client.BaseAddress = new Uri("GetRecipeIdeas"); // Use your server’s URL
});





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RecipeAI.Client._Imports).Assembly);

app.Run();
