using RpgBattleWeb.Components;
using RpgBattleWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor Components + interactive server render mode (SignalR-based, like the
// original console app this keeps all game state live on the server).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One GameEngine per user session (per "circuit"), so def/atk/wins persist
// while the browser tab stays open - same feel as the console app persisting
// state across battles within a single run.
builder.Services.AddScoped<GameEngine>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
