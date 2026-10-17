using SupportHub.Sla.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("tickets", client =>
{
    client.BaseAddress = new Uri("https+http://ticketsapi");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
