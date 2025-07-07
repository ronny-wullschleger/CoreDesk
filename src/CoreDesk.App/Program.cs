// src/CoreDesk.App/Program.cs

using CoreDesk.App.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- CoreDesk Services für das erweiterte MVP ---
// Order matters for dependency injection
builder.Services.AddSingleton<MockErpService>();
builder.Services.AddSingleton<TeamService>();
builder.Services.AddSingleton<AuthenticationService>();
builder.Services.AddSingleton<AutomationService>();
builder.Services.AddSingleton<TicketService>();
builder.Services.AddSingleton<TicketFilterService>();

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