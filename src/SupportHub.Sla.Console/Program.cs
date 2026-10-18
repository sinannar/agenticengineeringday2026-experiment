using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SupportHub.Sla.ConsoleApp;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("customers", client =>
{
    client.BaseAddress = new Uri("https+http://customersapi");
});

builder.Services.AddHttpClient("tickets", client =>
{
    client.BaseAddress = new Uri("https+http://ticketsapi");
});

builder.Services.AddSingleton<CustomerConsoleClient>();
builder.Services.AddSingleton<TicketConsoleClient>();
builder.Services.AddSingleton<SlaScanClient>();

using var host = builder.Build();
await host.StartAsync();

try
{
    var application = new ConsoleApplication(
        new SystemConsoleTerminal(),
        host.Services.GetRequiredService<CustomerConsoleClient>(),
        host.Services.GetRequiredService<TicketConsoleClient>(),
        host.Services.GetRequiredService<SlaScanClient>());
    await application.RunAsync();
}
finally
{
    await host.StopAsync();
}
