var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.QLN_AIPOV_Backend_API>("qln-aipov-backend-api");
builder.Build().Run();
