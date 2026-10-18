using SupportHub.Sla.Worker;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("tickets", client =>
{
    client.BaseAddress = new Uri("https+http://ticketsapi");
});
builder.Services.AddSingleton<SlaScanClient>();
builder.Services
    .AddOptions<SlaWorkerOptions>()
    .Bind(builder.Configuration.GetSection("Sla"))
    .Validate(options => options.IntervalSeconds > 0, "Sla:IntervalSeconds must be greater than zero.")
    .Validate(options => options.MaxItems is >= 1 and <= 50, "Sla:MaxItems must be between 1 and 50.")
    .ValidateOnStart();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.MapDefaultEndpoints();
await host.RunAsync();
