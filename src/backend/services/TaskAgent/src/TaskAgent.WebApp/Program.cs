using TaskAgent.Application;
using TaskAgent.Infrastructure;
using TaskAgent.ServiceDefaults;
using TaskAgent.WebApp;
using TaskAgent.WebApp.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.ValidateConfiguration();

// Apply database migrations automatically on startup (all environments)
await app.ApplyDatabaseMigrationsAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseContentSafety();

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
