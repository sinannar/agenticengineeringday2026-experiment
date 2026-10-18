using System.Net;
using SupportHub.Presentation;

namespace SupportHub.Sla.ConsoleApp;

public interface IConsoleTerminal
{
    bool IsInteractive { get; }
    void Clear();
    string? ReadLine();
    bool TryReadKey(out ConsoleKey key);
    void Write(string value);
    void WriteLine(string value = "");
}

public sealed class SystemConsoleTerminal : IConsoleTerminal
{
    public bool IsInteractive => !Console.IsInputRedirected;
    public void Clear()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
    }

    public string? ReadLine() => Console.ReadLine();
    public void Write(string value) => Console.Write(value);
    public bool TryReadKey(out ConsoleKey key)
    {
        if (Console.IsInputRedirected || !Console.KeyAvailable)
        {
            key = default;
            return false;
        }

        key = Console.ReadKey(intercept: true).Key;
        return true;
    }

    public void WriteLine(string value = "") => Console.WriteLine(value);
}

public sealed class ConsoleApplication(
    IConsoleTerminal terminal,
    CustomerConsoleClient customerClient,
    TicketConsoleClient ticketClient,
    SlaScanClient slaScanClient)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            terminal.Clear();
            WriteMenu();
            var choice = terminal.ReadLine();
            if (choice is null or "0")
            {
                return;
            }

            if (string.Equals(choice, "h", StringComparison.OrdinalIgnoreCase))
            {
                WriteHelp();
            }
            else
            {
                await ExecuteAsync(choice, cancellationToken);
            }

            if (terminal.IsInteractive)
            {
                terminal.WriteLine();
                terminal.WriteLine("Press Enter to return to the menu.");
                terminal.ReadLine();
            }
        }
    }

    private void WriteMenu()
    {
        terminal.WriteLine("SupportHub Operations");
        terminal.WriteLine("=====================");
        terminal.WriteLine("1  Check service health       Verify Customers and Tickets APIs.");
        terminal.WriteLine("2  List customers             Read customer records.");
        terminal.WriteLine("3  List tickets               Read the ticket queue.");
        terminal.WriteLine("4  Watch service health       Poll until Q or cancellation.");
        terminal.WriteLine("5  Sample ticket data         Unavailable; no supported mutation exists.");
        terminal.WriteLine("6  Run SLA scan               Evaluate current SLA state.");
        terminal.WriteLine("0  Exit");
        terminal.WriteLine("H  Help");
        terminal.Write("Select an option: ");
    }

    private void WriteHelp()
    {
        terminal.WriteLine("Commands are read-only except for the existing SLA scan endpoint.");
        terminal.WriteLine("Use 1-6 to run an operation, H for this help, or 0 to exit.");
        terminal.WriteLine("Option 5 is retained for compatibility but is intentionally unavailable.");
    }

    private async Task ExecuteAsync(string choice, CancellationToken cancellationToken)
    {
        switch (choice)
        {
            case "1":
                await CheckHealthAsync(cancellationToken);
                break;
            case "2":
                await ListCustomersAsync(cancellationToken);
                break;
            case "3":
                await ListTicketsAsync(cancellationToken);
                break;
            case "4":
                await WatchHealthAsync(cancellationToken);
                break;
            case "5":
                terminal.WriteLine("Sample ticket data is unavailable. Use the web application to create real tickets.");
                break;
            case "6":
                await RunSlaScanAsync(cancellationToken);
                break;
            default:
                terminal.WriteLine($"Unknown command '{choice}'. Enter H for help.");
                break;
        }
    }

    private async Task ListCustomersAsync(CancellationToken cancellationToken)
    {
        terminal.WriteLine("Loading customers...");
        try
        {
            var customers = await customerClient.GetCustomersAsync(cancellationToken);
            terminal.WriteLine(ConsoleTable.Render(
                ["ID", "Name", "Support tier", "Email"],
                customers.Select(customer => (IReadOnlyList<string>)
                [
                    customer.Id.ToString()[..8],
                    customer.Name,
                    SupportHubVocabulary.SupportTier(customer.SupportTier.ToString()),
                    customer.Email
                ]).ToArray()));
        }
        catch (Exception exception) when (exception is ConsoleApiException or ConsoleApiContractException or HttpRequestException)
        {
            terminal.WriteLine($"Customer list unavailable: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            terminal.WriteLine("Customer list cancelled.");
        }
    }

    private async Task ListTicketsAsync(CancellationToken cancellationToken)
    {
        terminal.WriteLine("Loading tickets...");
        try
        {
            var tickets = await ticketClient.GetTicketsAsync(cancellationToken);
            terminal.WriteLine(ConsoleTable.Render(
                ["ID", "Title", "Status", "Priority", "Customer", "Updated"],
                tickets.Select(ticket => (IReadOnlyList<string>)
                [
                    ticket.Id.ToString()[..8],
                    ticket.Title,
                    SupportHubVocabulary.TicketStatus(ticket.Status.ToString()),
                    SupportHubVocabulary.TicketPriority(ticket.Priority.ToString()),
                    ticket.CustomerId.ToString()[..8],
                    ticket.UpdatedAt.ToLocalTime().ToString("g")
                ]).ToArray()));
        }
        catch (Exception exception) when (exception is ConsoleApiException or ConsoleApiContractException or HttpRequestException)
        {
            terminal.WriteLine($"Ticket list unavailable: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            terminal.WriteLine("Ticket list cancelled.");
        }
    }

    private async Task RunSlaScanAsync(CancellationToken cancellationToken)
    {
        terminal.WriteLine("Running SLA scan...");
        try
        {
            var result = await slaScanClient.ScanAsync(new(MaxItems: 50), cancellationToken);
            terminal.WriteLine(
                $"Evaluated {result.EvaluatedCount} tickets at {result.EvaluatedAtUtc.ToLocalTime():g}; " +
                $"{result.FailedCount} failed.");
            terminal.WriteLine(
                $"At risk: {result.Items.Count(item => item.State == SlaState.AtRisk)}, " +
                $"breached: {result.Items.Count(item => item.State == SlaState.Breached)}.");

            foreach (var failure in result.Failures)
            {
                terminal.WriteLine($"Failure {failure.Code} for ticket {failure.TicketId}: {failure.Message}");
            }
        }
        catch (SlaScanHttpException exception)
        {
            terminal.WriteLine($"SLA scan failed: HTTP {(int)exception.StatusCode} ({exception.StatusCode}).");
        }
        catch (SlaScanContractException exception)
        {
            terminal.WriteLine($"SLA scan failed: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            terminal.WriteLine($"SLA scan unavailable: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            terminal.WriteLine("SLA scan cancelled.");
        }
    }

    private async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        await CheckApiAsync("customers", cancellationToken);
        await CheckApiAsync("tickets", cancellationToken);
    }

    private async Task CheckApiAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var statusCode = await (name == "customers"
                ? customerClient.GetHealthAsync(cancellationToken)
                : ticketClient.GetHealthAsync(cancellationToken));
            terminal.WriteLine($"{name}: {(statusCode == HttpStatusCode.OK ? "Healthy" : $"HTTP {(int)statusCode}")}");
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            terminal.WriteLine($"{name}: Unavailable ({exception.Message})");
        }
    }

    private async Task WatchHealthAsync(CancellationToken cancellationToken)
    {
        terminal.WriteLine("Watching service health. Press Q to stop.");
        if (!terminal.IsInteractive)
        {
            terminal.WriteLine("Health watch requires an interactive terminal; returning to the menu.");
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            terminal.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}]");
            await CheckHealthAsync(cancellationToken);
            for (var i = 0; i < 50 && !cancellationToken.IsCancellationRequested; i++)
            {
                if (terminal.TryReadKey(out var key) && key == ConsoleKey.Q)
                {
                    terminal.WriteLine("Health watch stopped.");
                    return;
                }

                await Task.Delay(100, cancellationToken);
            }
        }
    }
}
