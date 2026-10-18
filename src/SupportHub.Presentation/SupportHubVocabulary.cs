namespace SupportHub.Presentation;

public static class SupportHubVocabulary
{
    public static string Humanize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "Not provided"
            : string.Concat(
                value.Select((character, index) =>
                    index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));

    public static string TicketStatus(string value) => Humanize(value);
    public static string TicketPriority(string value) => Humanize(value);
    public static string SlaState(string value) => Humanize(value);
    public static string SupportTier(string value) => Humanize(value);

    public static string Empty(string subject) => $"No {subject} found.";
    public static string Unavailable(string subject) => $"{subject} is temporarily unavailable.";
    public static string RequestFailed(string subject) => $"We could not load {subject}. Please try again.";
}
