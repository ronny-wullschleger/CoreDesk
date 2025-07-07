// src/CoreDesk.App/Program.cs

using CoreDesk.App.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Unsere eigenen Services für das MVP ---
// Singleton, damit die Daten während der Laufzeit im Speicher bleiben
builder.Services.AddSingleton<TicketService>(); 
builder.Services.AddSingleton<MockErpService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<CoreDesk.App.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();