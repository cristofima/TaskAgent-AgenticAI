using TaskAgent.Application;
using TaskAgent.Infrastructure;
using TaskAgent.ServiceDefaults;
using TaskAgent.WebApp;
using TaskAgent.WebApp.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .Services.AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.ValidateConfiguration();

// Apply database migrations automatically on startup (all environments)
await app.ApplyDatabaseMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskAgent API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable CORS before authentication
app.UseCors();

// Content Safety middleware before authorization
app.UseContentSafety();

app.UseAuthorization();

// Map controllers for REST API
app.MapControllers();

await app.RunAsync();
