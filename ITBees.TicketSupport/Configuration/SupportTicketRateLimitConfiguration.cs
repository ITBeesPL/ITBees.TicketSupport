namespace ITBees.TicketSupport.Configuration;

/// <summary>
/// How much one requester may post. Every attempt counts, rejected ones included, so a flood is cut
/// off before the costly part: sanitizing up to 8 MB of HTML and raising an alert for each message.
/// Desk staff and trusted callers are not limited. A null limit switches that limit off; a null
/// <see cref="SupportTicketConfiguration.RateLimits"/> switches all of them off.
/// </summary>
public sealed class SupportTicketRateLimitConfiguration
{
    /// <summary>
    /// New tickets, the host's "report for analysis" actions included: 20 at once, then one every
    /// 3 minutes.
    /// </summary>
    public SupportTicketRateLimit NewTickets { get; init; } = new()
    {
        Burst = 20,
        Refill = 1,
        RefillPeriod = TimeSpan.FromMinutes(3)
    };

    /// <summary>Answers on existing tickets: 20 at once, then one a minute.</summary>
    public SupportTicketRateLimit Messages { get; init; } = new()
    {
        Burst = 20,
        Refill = 1,
        RefillPeriod = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Kilobytes of message content - the HTML with its embedded pictures - over new tickets and
    /// answers together: 32 MB at once, then 8 MB an hour. A single message is at most 8 MB, so keep
    /// the burst at 8192 or more, or the largest messages can never get through.
    /// </summary>
    public SupportTicketRateLimit ContentKilobytes { get; init; } = new()
    {
        Burst = 32 * 1024,
        Refill = 8 * 1024,
        RefillPeriod = TimeSpan.FromHours(1)
    };
}

/// <summary>
/// A token bucket: up to <see cref="Burst"/> at once, then <see cref="Refill"/> more every
/// <see cref="RefillPeriod"/>, never above <see cref="Burst"/>.
/// </summary>
public sealed class SupportTicketRateLimit
{
    public int Burst { get; init; }

    public int Refill { get; init; }

    public TimeSpan RefillPeriod { get; init; }
}
