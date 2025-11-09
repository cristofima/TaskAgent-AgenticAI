IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.TaskAgent_WebApp>("taskagent-webapp");

await builder.Build().RunAsync();
