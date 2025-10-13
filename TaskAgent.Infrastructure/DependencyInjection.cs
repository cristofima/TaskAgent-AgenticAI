using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAgent.Application.Interfaces;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.Repositories;

namespace TaskAgent.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Configure Database (SQL Server)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TaskDbContext>(options => options.UseSqlServer(connectionString));

        // Register Repository (Repository Pattern)
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }
}
