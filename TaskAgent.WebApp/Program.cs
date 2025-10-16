using TaskAgent.Infrastructure;
using TaskAgent.WebApp;
using TaskAgent.WebApp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration).AddPresentation(builder.Configuration);

var app = builder.Build();

app.ValidateConfiguration();

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
