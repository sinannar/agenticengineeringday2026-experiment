using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;

[Collection(PersistentResourceCollection.Name)]
public sealed class TicketPersistenceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Ticket_SurvivesSeparateAppHostLifecycles()
    {
        var volumeName = $"supporthub-ticket-persistence-{Guid.NewGuid():N}";
        var password = Guid.NewGuid().ToString("N");
        ConfigurePersistenceEnvironment(volumeName, password);

        try
        {
            TicketResponse created;
            await using (var firstApp = await StartAppAsync())
            {
                var customersClient = firstApp.CreateHttpClient("customersapi");
                var ticketsClient = firstApp.CreateHttpClient("ticketsapi");
                var customer = await CreateCustomerAsync(customersClient);

                using var response = await ticketsClient.PostAsJsonAsync("/tickets", new
                {
                    title = "Persisted ticket",
                    description = "This ticket must survive an AppHost restart.",
                    status = "Open",
                    priority = "High",
                    assignee = "Persistence Test",
                    customerId = customer.Id
                }, JsonOptions);

                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                created = (await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions))!;
                Assert.NotNull(created);
                Assert.Equal(customer.Id, created.CustomerId);
            }

            await using (var secondApp = await StartAppAsync())
            {
                var ticketsClient = secondApp.CreateHttpClient("ticketsapi");
                using var response = await ticketsClient.GetAsync($"/tickets/{created.Id}");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var persisted = (await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions))!;
                Assert.Equal(created.Id, persisted.Id);
                Assert.Equal(created.Title, persisted.Title);
                Assert.Equal(created.Description, persisted.Description);
                Assert.Equal(created.Status, persisted.Status);
                Assert.Equal(created.Priority, persisted.Priority);
                Assert.Equal(created.Assignee, persisted.Assignee);
                Assert.Equal(created.CustomerId, persisted.CustomerId);
                Assert.Equal(created.CreatedAt, persisted.CreatedAt);
                Assert.Equal(created.UpdatedAt, persisted.UpdatedAt);
            }
        }
        finally
        {
            ClearPersistenceEnvironment();
            RemoveVolume(volumeName);
        }
    }

    private static async Task<DistributedApplication> StartAppAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.SupportHub_AppHost>();
        var app = await builder.BuildAsync();
        try
        {
            await app.StartAsync();
            await app.ResourceNotifications.WaitForResourceHealthyAsync("customersapi");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("ticketsapi");
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static void ConfigurePersistenceEnvironment(string volumeName, string password)
    {
        Environment.SetEnvironmentVariable("SUPPORTHUB_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_APPLY_MIGRATIONS", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_PERSISTENCE_TEST_MODE", "true");
        Environment.SetEnvironmentVariable("SUPPORTHUB_PERSISTENCE_TEST_VOLUME_NAME", volumeName);
        Environment.SetEnvironmentVariable(
            "Parameters__supporthub-persistence-test-postgres-password",
            password);
    }

    private static void ClearPersistenceEnvironment()
    {
        Environment.SetEnvironmentVariable("SUPPORTHUB_PERSISTENCE_TEST_MODE", null);
        Environment.SetEnvironmentVariable("SUPPORTHUB_PERSISTENCE_TEST_VOLUME_NAME", null);
        Environment.SetEnvironmentVariable(
            "Parameters__supporthub-persistence-test-postgres-password",
            null);
    }

    private static void RemoveVolume(string volumeName)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };
        process.StartInfo.ArgumentList.Add("volume");
        process.StartInfo.ArgumentList.Add("rm");
        process.StartInfo.ArgumentList.Add("--force");
        process.StartInfo.ArgumentList.Add(volumeName);

        try
        {
            process.Start();
            process.WaitForExit();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Unable to remove the dedicated persistence-test volume '{volumeName}'.",
                exception);
        }

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"Docker cleanup failed for the dedicated persistence-test volume '{volumeName}': {error}");
        }
    }

    private static async Task<CustomerResponse> CreateCustomerAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/customers", new
        {
            name = "Persistence Test Customer",
            email = $"persistence-{Guid.NewGuid():N}@example.com",
            phoneNumber = (string?)null,
            supportTier = "Standard"
        }, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerResponse>(JsonOptions))!;
    }
}
