using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SupportHub.Tickets.Api.Features.Tickets;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddAzureBlobServiceClient("blobs");
builder.AddAzureServiceBusClient("messaging");
builder.AddNpgsqlDbContext<TicketDbContext>("ticketsdb");

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHttpClient<CustomerLookupClient>(client =>
{
    client.BaseAddress = new("https+http://customersapi");
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("SUPPORTHUB_APPLY_MIGRATIONS"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/hello", () => "Hello World! from Tickets Api");

app.MapTicketEndpoints();
app.MapDefaultEndpoints();

app.Run();
