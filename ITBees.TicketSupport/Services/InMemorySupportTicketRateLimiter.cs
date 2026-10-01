using System.Threading.RateLimiting;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// Token buckets per requester, kept in the memory of the API process. A bucket that has filled up
/// again is dropped by <see cref="PartitionedRateLimiter"/>, so memory follows the active requesters
/// only. A restart forgets the counters, which errs on the side of the requester.
/// </summary>
public sealed class InMemorySupportTicketRateLimiter : ISupportTicketRateLimiter, IDisposable
{
    /// <summary>Sent with the 429, so a panel can show its own text instead of the English message.</summary>
    public const string ErrorKey = "SupportTicketRateLimited";

    private readonly PartitionedRateLimiter<Guid> _newTickets;
    private readonly PartitionedRateLimiter<Guid> _messages;
    private readonly PartitionedRateLimiter<Guid> _contentKilobytes;
    private readonly int _contentBurst;

    public InMemorySupportTicketRateLimiter(SupportTicketConfiguration configuration)
    {
        var limits = configuration.RateLimits;
        _newTickets = Create(limits?.NewTickets, nameof(SupportTicketRateLimitConfiguration.NewTickets));
        _messages = Create(limits?.Messages, nameof(SupportTicketRateLimitConfiguration.Messages));
        _contentKilobytes = Create(limits?.ContentKilobytes,
            nameof(SupportTicketRateLimitConfiguration.ContentKilobytes));
        _contentBurst = limits?.ContentKilobytes?.Burst ?? 0;
    }

    public void CheckNewTicket(Guid requesterGuid, int contentLength)
    {
        Acquire(_newTickets, requesterGuid, 1, "Too many new tickets in a short time");
        AcquireContent(requesterGuid, contentLength);
    }

    public void CheckMessage(Guid requesterGuid, int contentLength)
    {
        Acquire(_messages, requesterGuid, 1, "Too many messages in a short time");
        AcquireContent(requesterGuid, contentLength);
    }

    public void Dispose()
    {
        _newTickets?.Dispose();
        _messages?.Dispose();
        _contentKilobytes?.Dispose();
    }

    private void AcquireContent(Guid requesterGuid, int contentLength)
    {
        if (_contentKilobytes == null)
            return;

        // One permit per started kilobyte, never more than a full bucket - asking a token bucket for
        // more than its limit throws instead of refusing.
        var kilobytes = Math.Clamp((int)Math.Ceiling(Math.Max(contentLength, 0) / 1024d), 1, _contentBurst);
        Acquire(_contentKilobytes, requesterGuid, kilobytes, "Too much content posted in a short time");
    }

    private static void Acquire(PartitionedRateLimiter<Guid> limiter, Guid requesterGuid, int permits,
        string message)
    {
        if (limiter == null)
            return;

        using var lease = limiter.AttemptAcquire(requesterGuid, permits);
        if (lease.IsAcquired)
            return;

        var retry = lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? $"Try again in {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} min."
            : "Try again later.";
        throw new FasApiErrorException($"{message}. {retry}", 429, ErrorKey);
    }

    private static PartitionedRateLimiter<Guid> Create(SupportTicketRateLimit limit, string name)
    {
        if (limit == null)
            return null;

        if (limit.Burst < 1 || limit.Refill < 1 || limit.RefillPeriod <= TimeSpan.Zero)
            throw new ArgumentException(
                $"Support ticket rate limit {name} needs a positive burst, refill and refill period");

        return PartitionedRateLimiter.Create<Guid, Guid>(requesterGuid =>
            RateLimitPartition.GetTokenBucketLimiter(requesterGuid, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = limit.Burst,
                TokensPerPeriod = limit.Refill,
                ReplenishmentPeriod = limit.RefillPeriod,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    }
}
