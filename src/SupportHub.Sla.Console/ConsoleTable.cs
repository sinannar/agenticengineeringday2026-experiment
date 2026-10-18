namespace SupportHub.Sla.ConsoleApp;

public static class ConsoleTable
{
    public static string Render(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        int maxRows = 25)
    {
        if (rows.Count == 0)
        {
            return "No records found.";
        }

        var selectedRows = rows.Take(maxRows).ToArray();
        var widths = headers
            .Select((header, index) => Math.Max(
                header.Length,
                selectedRows.Max(row => index < row.Count ? row[index].Length : 0)))
            .ToArray();

        var separator = $"+-{string.Join("-+-", widths.Select(width => new string('-', width)))}-+";
        var lines = new List<string> { separator, FormatRow(headers), separator };
        lines.AddRange(selectedRows.Select(FormatRow));
        lines.Add(separator);

        if (rows.Count > maxRows)
        {
            lines.Add($"Showing {maxRows} of {rows.Count} records.");
        }

        return string.Join(Environment.NewLine, lines);

        string FormatRow(IReadOnlyList<string> row) =>
            $"| {string.Join(" | ", widths.Select((width, index) =>
                (index < row.Count ? row[index] : string.Empty).PadRight(width)))} |";
    }
}
