using SupportHub.Presentation;
using SupportHub.Sla.ConsoleApp;

namespace SupportHub.AppHost.Tests;

public sealed class SupportHubPresentationTests
{
    [Theory]
    [InlineData("InProgress", "In Progress")]
    [InlineData("AtRisk", "At Risk")]
    [InlineData("Enterprise", "Enterprise")]
    public void Humanize_UsesReadableLabels(string value, string expected)
    {
        Assert.Equal(expected, SupportHubVocabulary.Humanize(value));
    }

    [Fact]
    public void ConsoleTable_RendersBoundedRowsAndHeaders()
    {
        var output = ConsoleTable.Render(
            ["ID", "Status"],
            [
                (IReadOnlyList<string>)["12345678", "In Progress"],
                ["87654321", "Open"]
            ],
            maxRows: 1);

        Assert.Contains("| ID       | Status      |", output);
        Assert.Contains("| 12345678 | In Progress |", output);
        Assert.Contains("Showing 1 of 2 records.", output);
        Assert.DoesNotContain("87654321", output);
    }

    [Fact]
    public void ConsoleTable_ReportsEmptyResults()
    {
        Assert.Equal("No records found.", ConsoleTable.Render(["ID"], []));
    }

    [Fact]
    public void ConsoleTable_BoundsLongValuesToRequestedWidth()
    {
        var output = ConsoleTable.Render(
            ["ID", "Title", "Email"],
            [
                (IReadOnlyList<string>)
                [
                    "12345678",
                    "A ticket title that is much longer than a narrow terminal can display",
                    "operator-with-a-long-address@example.com"
                ]
            ],
            maxWidth: 40);

        Assert.All(output.Split(Environment.NewLine), line => Assert.True(line.Length <= 40));
        Assert.Contains("...", output);
    }
}
