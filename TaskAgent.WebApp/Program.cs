using TaskAgent.Infrastructure;
using TaskAgent.WebApp;

var builder = WebApplication.CreateBuilder(args);

// Register layers in dependency order: Infrastructure -> Application -> Presentation
builder.Services.AddInfrastructure(builder.Configuration).AddPresentation(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
