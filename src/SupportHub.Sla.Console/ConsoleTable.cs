namespace SupportHub.Sla.ConsoleApp;

public static class ConsoleTable
{
    public static string Render(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        int maxRows = 25,
        int maxWidth = 100)
    {
        if (rows.Count == 0)
        {
            return "No records found.";
        }

        var selectedRows = rows.Take(maxRows).ToArray();
        var naturalWidths = headers
            .Select((header, index) => Math.Max(
                CellLength(header),
                selectedRows.Max(row => index < row.Count ? CellLength(row[index]) : 0)))
            .ToArray();
        var availableWidth = Math.Max(4 * headers.Count, maxWidth - 2 - (3 * (headers.Count - 1)));
        var columnWidth = Math.Max(4, availableWidth / headers.Count);
        var widths = naturalWidths.Select(width => Math.Min(width, columnWidth)).ToArray();

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
                Fit(index < row.Count ? row[index] : string.Empty, width).PadRight(width)))} |";

        static int CellLength(string value) =>
            value.Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Length;

        static string Fit(string value, int width)
        {
            var normalized = value.Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
            return normalized.Length <= width
                ? normalized
                : width <= 3
                    ? normalized[..width]
                    : normalized[..(width - 3)] + "...";
        }
    }
}
