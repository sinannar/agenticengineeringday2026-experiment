using SupportHub.Activity.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureCosmosClient("activitydb");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
