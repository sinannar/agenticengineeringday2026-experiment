using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TicketsApiTests
{
    private const string InternalMaintenanceKey = "activity-test-key";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task TicketsApi_IsHealthy()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var client = app.CreateHttpClient("ticketsapi");

        using var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TicketsApi_EvaluatesSingleTicketSlaAndBoundedScan()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "SLA Customer", "sla@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "SLA ticket");

        using var statusResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/sla");
        statusResponse.EnsureSuccessStatusCode();
        var status = await statusResponse.Content.ReadFromJsonAsync<SlaStatusResponse>(JsonOptions);
        Assert.NotNull(status);
        Assert.Equal(ticket.Id, status!.TicketId);
        Assert.Equal(customer.Id, status.CustomerId);
        Assert.Equal("Standard", status.SupportTier);
        Assert.Equal("Medium", status.Priority);
        Assert.Equal("OnTrack", status.State);
        Assert.Equal(TimeSpan.FromHours(12), status.TargetDuration);
        Assert.Equal("v1", status.PolicyVersion);

        using var scanResponse = await ticketsClient.PostAsJsonAsync(
            "/tickets/sla/scan",
            new { priority = "Medium", maxItems = 50 },
            JsonOptions);
        scanResponse.EnsureSuccessStatusCode();
        var scan = await scanResponse.Content.ReadFromJsonAsync<SlaScanResponse>(JsonOptions);
        Assert.NotNull(scan);
        Assert.Equal(1, scan!.EvaluatedCount);
        Assert.Empty(scan.Failures);
        Assert.Contains(scan.Items, item => item.TicketId == ticket.Id && item.State == "OnTrack");
    }

    [Fact]
    public async Task TicketsApi_ManagesTicketLifecycleAndFilters()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Ada Lovelace", "ada@example.com");
        var secondCustomer = await CreateCustomerAsync(customersClient, "Grace Hopper", "grace@example.com");

        using var createResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Cannot sign in",
            description = "The customer cannot sign in.",
            status = "Open",
            priority = "High",
            assignee = "Ada",
            customerId = customer.Id
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(customer.Id, created.CustomerId);
        Assert.Equal(created.CreatedAt, created.UpdatedAt);

        using var secondCreateResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Password reset",
            description = "The customer needs a reset.",
            status = "New",
            priority = "Low",
            assignee = "Grace",
            customerId = secondCustomer.Id
        }, JsonOptions);
        secondCreateResponse.EnsureSuccessStatusCode();

        using var filteredResponse = await ticketsClient.GetAsync(
            $"/tickets?status=Open&priority=High&customerId={customer.Id}&assignee=Ada");
        filteredResponse.EnsureSuccessStatusCode();
        var filtered = await filteredResponse.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        var filteredTicket = Assert.Single(filtered!);
        Assert.Equal(created.Id, filteredTicket.Id);

        using var updateResponse = await ticketsClient.PutAsJsonAsync($"/tickets/{created.Id}", new
        {
            title = "Cannot sign in - updated",
            description = "The customer still cannot sign in.",
            status = "Resolved",
            priority = "Medium",
            assignee = (string?)null,
            customerId = secondCustomer.Id
        }, JsonOptions);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(secondCustomer.Id, updated.CustomerId);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);

        using var invalidCustomerResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Invalid customer",
            description = "This should fail.",
            status = "New",
            priority = "Low",
            assignee = (string?)null,
            customerId = Guid.NewGuid()
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalidCustomerResponse.StatusCode);

        using var deleteResponse = await ticketsClient.DeleteAsync($"/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var missingResponse = await ticketsClient.GetAsync($"/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task TicketsApi_ReturnsAuthoritativeDashboardSummary()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Dashboard Customer", "dashboard@example.com");
        await CreateTicketAsync(ticketsClient, customer.Id, "Open dashboard ticket");
        using var secondCreateResponse = await ticketsClient.PostAsJsonAsync("/tickets", new
        {
            title = "Resolved dashboard ticket",
            description = "A completed dashboard ticket.",
            status = "Resolved",
            priority = "High",
            assignee = (string?)null,
            customerId = customer.Id
        }, JsonOptions);
        secondCreateResponse.EnsureSuccessStatusCode();

        using var response = await ticketsClient.GetAsync("/tickets/dashboard-summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(2, summary!.TotalTickets);
        Assert.Equal(1, summary.OpenTickets);
        Assert.Equal(2, summary.StatusBreakdown.Sum(item => item.Count));
        Assert.Equal(2, summary.PriorityBreakdown.Sum(item => item.Count));
        Assert.Equal(1, summary.StatusBreakdown.Single(item => item.Status == TicketStatus.Open).Count);
        Assert.Equal(1, summary.StatusBreakdown.Single(item => item.Status == TicketStatus.Resolved).Count);
        Assert.Equal(1, summary.PriorityBreakdown.Single(item => item.Priority == TicketPriority.Medium).Count);
        Assert.Equal(1, summary.PriorityBreakdown.Single(item => item.Priority == TicketPriority.High).Count);
        Assert.Equal(0, summary.AtRiskSlaTickets);
        Assert.Equal(0, summary.BreachedSlaTickets);
        Assert.Equal("Complete", summary.DataQuality.ToString());
    }

    [Fact]
    public async Task SupportJourney_ReflectsAttachmentAuditSlaAndDashboardState()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("cache");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("blobs");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("activity-worker");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("sla-worker");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Journey Customer", "journey@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "Journey ticket");

        using var uploadResponse = await UploadAsync(
            ticketsClient,
            ticket.Id,
            "journey.txt",
            "text/plain",
            "journey attachment"u8.ToArray());
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        using var updateResponse = await ticketsClient.PutAsJsonAsync($"/tickets/{ticket.Id}", new
        {
            title = "Journey ticket resolved",
            description = "Completed journey.",
            status = "Resolved",
            priority = "High",
            assignee = "Support",
            customerId = customer.Id
        }, JsonOptions);
        updateResponse.EnsureSuccessStatusCode();

        using var slaResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/sla");
        slaResponse.EnsureSuccessStatusCode();
        var sla = await slaResponse.Content.ReadFromJsonAsync<SlaStatusResponse>(JsonOptions);
        Assert.NotNull(sla);
        Assert.Equal(ticket.Id, sla!.TicketId);
        Assert.Equal("Completed", sla.State);

        ActivityHistoryResponse? history = null;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var historyResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/activity");
            if (historyResponse.IsSuccessStatusCode)
            {
                history = await historyResponse.Content.ReadFromJsonAsync<ActivityHistoryResponse>(JsonOptions);
                if (history?.Items.Any(item => item.EventType == "ticket.updated") == true)
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        Assert.NotNull(history);
        Assert.Contains(history!.Items, item => item.EventType == "ticket.created");
        Assert.Contains(history.Items, item => item.EventType == "ticket.updated");

        using var dashboardResponse = await ticketsClient.GetAsync("/tickets/dashboard-summary");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardSummaryResponse>(JsonOptions);
        Assert.NotNull(dashboard);
        Assert.Equal("Complete", dashboard!.DataQuality.ToString());
        Assert.Contains(dashboard.StatusBreakdown, item => item.Status == TicketStatus.Resolved && item.Count > 0);
    }

    [Fact]
    public async Task DashboardAndCustomerTierContracts_CoverAllBreakdownsAndRejectInvalidBatch()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Breakdown Customer", "breakdown@example.com");
        foreach (var status in Enum.GetNames<TicketStatus>())
        {
            foreach (var priority in Enum.GetNames<TicketPriority>())
            {
                using var response = await ticketsClient.PostAsJsonAsync("/tickets", new
                {
                    title = $"{status}-{priority}",
                    description = "Dashboard breakdown coverage.",
                    status,
                    priority,
                    assignee = (string?)null,
                    customerId = customer.Id
                }, JsonOptions);
                response.EnsureSuccessStatusCode();
            }
        }

        using var invalidBatchResponse = await customersClient.PostAsJsonAsync(
            "/customers/support-tiers",
            new { customerIds = new[] { customer.Id, customer.Id } },
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalidBatchResponse.StatusCode);

        using var dashboardResponse = await ticketsClient.GetAsync("/tickets/dashboard-summary");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardSummaryResponse>(JsonOptions);

        Assert.NotNull(dashboard);
        Assert.All(Enum.GetValues<TicketStatus>(), status =>
            Assert.Contains(dashboard!.StatusBreakdown, item => item.Status == status && item.Count > 0));
        Assert.All(Enum.GetValues<TicketPriority>(), priority =>
            Assert.Contains(dashboard!.PriorityBreakdown, item => item.Priority == priority && item.Count > 0));
        Assert.Equal(dashboard.StatusBreakdown.Sum(item => item.Count), dashboard.TotalTickets);
        Assert.Equal(dashboard.PriorityBreakdown.Sum(item => item.Count), dashboard.TotalTickets);
    }

    [Fact]
    public async Task TicketChanges_AreAvailableThroughActivityHistory()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("activity-worker");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Activity Customer", "activity@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "Activity ticket");

        using var updateResponse = await ticketsClient.PutAsJsonAsync($"/tickets/{ticket.Id}", new
        {
            title = "Activity ticket updated",
            description = "Updated for activity testing.",
            status = "Resolved",
            priority = "High",
            assignee = "Ada",
            customerId = customer.Id
        }, JsonOptions);
        updateResponse.EnsureSuccessStatusCode();

        ActivityHistoryResponse? history = null;
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/activity");
            if (response.IsSuccessStatusCode)
            {
                history = await response.Content.ReadFromJsonAsync<ActivityHistoryResponse>(JsonOptions);
                if (history?.Items.Count >= 4)
                {
                    break;
                }
            }

            await Task.Delay(500);
        }

        Assert.NotNull(history);
        Assert.Contains(history!.Items, item => item.EventType == "ticket.created");
        Assert.Contains(history.Items, item => item.EventType == "ticket.updated");
        Assert.Contains(history.Items, item => item.EventType == "ticket.status-changed");
        Assert.Contains(history.Items, item => item.EventType == "ticket.assignment-changed");

        foreach (var item in history.Items)
        {
            using var unauthorizedReplayResponse = await ticketsClient.PostAsync(
                $"/internal/outbox/{item.Id}/replay",
                content: null);
            Assert.Equal(HttpStatusCode.NotFound, unauthorizedReplayResponse.StatusCode);

            using var replayRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/internal/outbox/{item.Id}/replay");
            replayRequest.Headers.Add("X-SupportHub-Internal-Key", InternalMaintenanceKey);
            using var replayResponse = await ticketsClient.SendAsync(replayRequest);
            Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        }

        await Task.Delay(3_000);
        using var duplicateHistoryResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/activity");
        duplicateHistoryResponse.EnsureSuccessStatusCode();
        var duplicateHistory =
            await duplicateHistoryResponse.Content.ReadFromJsonAsync<ActivityHistoryResponse>(JsonOptions);
        Assert.Equal(4, duplicateHistory!.Items.Count);
    }

    [Fact]
    public async Task TicketsApi_ManagesAttachmentLifecycleAndIsolation()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("blobs");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Attachment Customer", "attachment@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "Attachment ticket");
        var otherTicket = await CreateTicketAsync(ticketsClient, customer.Id, "Other ticket");

        using var emptyResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/attachments");
        emptyResponse.EnsureSuccessStatusCode();
        Assert.Empty(await emptyResponse.Content.ReadFromJsonAsync<List<AttachmentResponse>>(JsonOptions));

        using var uploadResponse = await UploadAsync(
            ticketsClient,
            ticket.Id,
            "evidence.txt",
            "text/plain",
            "attachment body"u8.ToArray());
        Assert.True(
            uploadResponse.StatusCode == HttpStatusCode.Created,
            await uploadResponse.Content.ReadAsStringAsync());
        var attachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentResponse>(JsonOptions);
        Assert.NotNull(attachment);
        Assert.Equal(ticket.Id, attachment.TicketId);
        Assert.Equal("evidence.txt", attachment.OriginalFileName);

        using var listResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/attachments");
        listResponse.EnsureSuccessStatusCode();
        var listed = Assert.Single(await listResponse.Content.ReadFromJsonAsync<List<AttachmentResponse>>(JsonOptions));
        Assert.Equal(attachment.Id, listed.Id);

        using var crossTicketResponse = await ticketsClient.GetAsync(
            $"/tickets/{otherTicket.Id}/attachments/{attachment.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, crossTicketResponse.StatusCode);

        using var downloadResponse = await ticketsClient.GetAsync(
            $"/tickets/{ticket.Id}/attachments/{attachment.Id}/content");
        downloadResponse.EnsureSuccessStatusCode();
        Assert.Equal("attachment body", await downloadResponse.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", downloadResponse.Content.Headers.ContentType?.MediaType);

        using var deleteResponse = await ticketsClient.DeleteAsync(
            $"/tickets/{ticket.Id}/attachments/{attachment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var deletedContentResponse = await ticketsClient.GetAsync(
            $"/tickets/{ticket.Id}/attachments/{attachment.Id}/content");
        Assert.Equal(HttpStatusCode.NotFound, deletedContentResponse.StatusCode);
    }

    [Fact]
    public async Task TicketsApi_RejectsInvalidAndOversizedAttachments()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Validation Customer", "validation@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "Validation ticket");

        using var invalidResponse = await UploadAsync(
            ticketsClient,
            ticket.Id,
            "malware.exe",
            "application/octet-stream",
            "invalid"u8.ToArray());
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

        using var oversizedResponse = await UploadAsync(
            ticketsClient,
            ticket.Id,
            "large.txt",
            "text/plain",
            new byte[10 * 1024 * 1024 + 1]);
        Assert.True(
            oversizedResponse.StatusCode == HttpStatusCode.RequestEntityTooLarge,
            await oversizedResponse.Content.ReadAsStringAsync());

        using var listResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/attachments");
        listResponse.EnsureSuccessStatusCode();
        Assert.Empty(await listResponse.Content.ReadFromJsonAsync<List<AttachmentResponse>>(JsonOptions));
    }

    [Fact]
    public async Task TicketsApi_CleansAttachmentsBeforeDeletingTicket()
    {
        ConfigureTestEnvironment();
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        await using var app = await builder.BuildAsync();
        await app.StartAsync();
        await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
        var customersClient = app.CreateHttpClient("customersapi");
        var ticketsClient = app.CreateHttpClient("ticketsapi");

        var customer = await CreateCustomerAsync(customersClient, "Delete Customer", "delete@example.com");
        var ticket = await CreateTicketAsync(ticketsClient, customer.Id, "Delete ticket");
        using var uploadResponse = await UploadAsync(
            ticketsClient,
            ticket.Id,
            "cleanup.txt",
            "text/plain",
            "cleanup"u8.ToArray());
        uploadResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await ticketsClient.DeleteAsync($"/tickets/{ticket.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using var attachmentsResponse = await ticketsClient.GetAsync($"/tickets/{ticket.Id}/attachments");
        Assert.Equal(HttpStatusCode.NotFound, attachmentsResponse.StatusCode);
    }

    private static void ConfigureTestEnvironment()
    {
        Environment.SetEnvironmentVariable("SUPPORTHUB_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_APPLY_MIGRATIONS", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_INTERNAL_MAINTENANCE_KEY", InternalMaintenanceKey);
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(
        HttpClient client,
        string name,
        string email)
    {
        using var response = await client.PostAsJsonAsync("/customers", new
        {
            name,
            email,
            phoneNumber = (string?)null,
            supportTier = "Standard"
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions))!;
    }

    private static async Task<TicketResponse> CreateTicketAsync(
        HttpClient client,
        Guid customerId,
        string title)
    {
        using var response = await client.PostAsJsonAsync("/tickets", new
        {
            title,
            description = "A ticket for attachment testing.",
            status = "Open",
            priority = "Medium",
            assignee = (string?)null,
            customerId
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions))!;
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid ticketId,
        string fileName,
        string contentType,
        byte[] content)
    {
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new(contentType);
        multipart.Add(file, "file", fileName);
        return await client.PostAsync($"/tickets/{ticketId}/attachments", multipart);
    }
}

public sealed record TicketResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    string? Assignee,
    Guid CustomerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AttachmentResponse(
    Guid Id,
    Guid TicketId,
    string OriginalFileName,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAt);

public sealed record SlaStatusResponse(
    Guid TicketId,
    Guid CustomerId,
    string Priority,
    string SupportTier,
    string TicketStatus,
    DateTimeOffset CreatedAtUtc,
    TimeSpan TargetDuration,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset EvaluatedAtUtc,
    string State,
    TimeSpan? Remaining,
    TimeSpan? Lateness,
    string PolicyVersion);

public sealed record SlaScanResponse(
    DateTimeOffset EvaluatedAtUtc,
    IReadOnlyList<SlaStatusResponse> Items,
    int EvaluatedCount,
    int FailedCount,
    IReadOnlyList<SlaScanFailure> Failures);

public sealed record SlaScanFailure(
    Guid TicketId,
    Guid CustomerId,
    string Code,
    string Message);

public sealed record ActivityHistoryResponse(
    Guid TicketId,
    IReadOnlyList<ActivityHistoryItem> Items);

public sealed record DashboardSummaryResponse(
    DateTimeOffset EvaluatedAtUtc,
    int TotalTickets,
    int OpenTickets,
    IReadOnlyList<DashboardStatusBreakdown> StatusBreakdown,
    IReadOnlyList<DashboardPriorityBreakdown> PriorityBreakdown,
    int AtRiskSlaTickets,
    int BreachedSlaTickets,
    DashboardDataQuality DataQuality);

public sealed record DashboardStatusBreakdown(TicketStatus Status, int Count);

public sealed record DashboardPriorityBreakdown(TicketPriority Priority, int Count);

public enum DashboardDataQuality
{
    Complete
}

public sealed record ActivityHistoryItem(
    Guid Id,
    string EventType,
    Guid TicketId,
    DateTimeOffset OccurredAtUtc,
    string? Actor,
    JsonElement Details);

public enum TicketStatus
{
    New,
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High
}
