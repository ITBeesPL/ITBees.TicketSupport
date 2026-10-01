namespace ITBees.TicketSupport.Configuration;

public sealed class SupportTicketConfiguration
{
    public long FirstTicketNumber { get; init; } = 100000L;

    /// <summary>A score at or below this raises the low-rating alert.</summary>
    public int LowRatingThreshold { get; init; } = 2;

    public SupportTicketLinkConfiguration Links { get; init; }
}
