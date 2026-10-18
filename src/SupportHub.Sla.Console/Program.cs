
using System.Net;
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
builder.Services.AddSingleton<SlaScanClient>();

using var host = builder.Build();
await host.StartAsync();

var clients = host.Services.GetRequiredService<IHttpClientFactory>();
var slaScanClient = host.Services.GetRequiredService<SlaScanClient>();

while (true)
{
    Console.Clear();
    Console.WriteLine("SupportHub Operations");
    Console.WriteLine("-----------------------------");
    Console.WriteLine("1. Check service health");
    Console.WriteLine("2. List customers");
    Console.WriteLine("3. List tickets");
    Console.WriteLine("4. Watch service health");
    Console.WriteLine("5. Generate sample ticket data");
    Console.WriteLine("6. Run SLA scan");
    Console.WriteLine("0. Exit");
    Console.Write("\nSelect an option: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            await CheckHealthAsync();
            break;

        case "2":
            Console.WriteLine("Customer listing will be implemented in RPI Loop 1.");
            break;

        case "3":
            Console.WriteLine("Ticket listing will be implemented in RPI Loop 1.");
            break;

        case "4":
            await WatchHealthAsync();
            break;

        case "5":
            GenerateSampleTickets();
            break;

        case "6":
            await RunSlaScanAsync();
            break;

        case "0":
            await host.StopAsync();
            return;

        default:
            Console.WriteLine("Invalid option.");
            break;
    }

    async Task RunSlaScanAsync()
    {
        try
        {
            var result = await slaScanClient.ScanAsync(new(MaxItems: 50), CancellationToken.None);
            Console.WriteLine(
                $"SLA scan at {result.EvaluatedAtUtc:O}: evaluated {result.EvaluatedCount}, failed {result.FailedCount}.");
            Console.WriteLine(
                $"At risk: {result.Items.Count(item => item.State == SlaState.AtRisk)}, " +
                $"breached: {result.Items.Count(item => item.State == SlaState.Breached)}.");

            foreach (var failure in result.Failures)
            {
                Console.WriteLine($"Failure {failure.Code} for ticket {failure.TicketId}: {failure.Message}");
            }
        }
        catch (SlaScanHttpException exception)
        {
            Console.WriteLine($"SLA scan failed: HTTP {(int)exception.StatusCode} ({exception.StatusCode}).");
        }
        catch (SlaScanContractException exception)
        {
            Console.WriteLine($"SLA scan failed: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine($"SLA scan unavailable: {exception.Message}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("SLA scan cancelled.");
        }
    }

    Console.WriteLine("\nPress Enter to continue...");
    Console.ReadLine();
}

async Task CheckHealthAsync()
{
    await CheckApiAsync("customers");
    await CheckApiAsync("tickets");
}

async Task CheckApiAsync(string name)
{
    using var client = clients.CreateClient(name);

    try
    {
        using var response = await client.GetAsync("/health");

        Console.WriteLine(
            $"{name}: {(response.StatusCode == HttpStatusCode.OK ? "Healthy" : $"HTTP {(int)response.StatusCode}")}");
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
        Console.WriteLine($"{name}: Unavailable ({ex.Message})");
    }
}

async Task WatchHealthAsync()
{
    Console.WriteLine("Watching service health. Press Q to stop.\n");

    while (true)
    {
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}]");
        await CheckHealthAsync();
        Console.WriteLine();

        // Poll for up to 5 seconds while allowing Q to stop.
        for (var i = 0; i < 50; i++)
        {
            if (Console.KeyAvailable &&
                Console.ReadKey(intercept: true).Key == ConsoleKey.Q)
            {
                return;
            }

            await Task.Delay(100);
        }
    }
}

void GenerateSampleTickets()
{
    var tickets = new[]
    {
        new { Title = "Unable to sign in", Priority = "High" },
        new { Title = "Billing question", Priority = "Medium" },
        new { Title = "Feature request", Priority = "Low" }
    };

    foreach (var ticket in tickets)
    {
        Console.WriteLine($"{ticket.Priority,-8} {ticket.Title}");
    }
}
