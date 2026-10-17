
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

using var host = builder.Build();
await host.StartAsync();

var clients = host.Services.GetRequiredService<IHttpClientFactory>();

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

        case "0":
            await host.StopAsync();
            return;

        default:
            Console.WriteLine("Invalid option.");
            break;
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
