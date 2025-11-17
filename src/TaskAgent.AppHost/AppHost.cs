IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TaskAgent_WebApi>("task-agent-webapi");

await builder.Build().RunAsync().ConfigureAwait(false);
