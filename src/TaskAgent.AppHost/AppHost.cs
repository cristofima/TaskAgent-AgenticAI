IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TaskAgent_WebApp>("task-agent-webapi");

await builder.Build().RunAsync().ConfigureAwait(false);
