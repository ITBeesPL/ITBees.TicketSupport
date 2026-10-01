using ITBees.Interfaces.Repository;
using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// Hands out the next number by reading the highest one used. Two instances creating a ticket in
/// the same millisecond would pick the same number; the unique index on Number rejects the loser,
/// and <see cref="SupportTicketWriter"/> retries. That is cheaper than a sequence table for a
/// volume measured in tickets per hour, and it stays correct because the database, not this
/// method, is the arbiter.
/// </summary>
public class SupportTicketNumberGenerator : ISupportTicketNumberGenerator
{
    private readonly IReadOnlyRepository<SupportTicket> _supportTicketRoRepo;
    private readonly SupportTicketConfiguration _configuration;

    public SupportTicketNumberGenerator(IReadOnlyRepository<SupportTicket> supportTicketRoRepo,
        SupportTicketConfiguration configuration)
    {
        _supportTicketRoRepo = supportTicketRoRepo;
        _configuration = configuration;
    }

    public long Next()
    {
        var highest = _supportTicketRoRepo
            .GetDataQueryable(x => true)
            .Select(x => (long?)x.Number)
            .Max();

        return highest.HasValue ? highest.Value + 1 : _configuration.FirstTicketNumber;
    }
}
