using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>The rating a requester leaves in their panel after the desk closed the ticket.</summary>
public class SupportTicketRatingService : ISupportTicketRatingService
{
    private readonly IReadOnlyRepository<SupportTicketRating> _supportTicketRatingRoRepo;
    private readonly IWriteOnlyRepository<SupportTicketRating> _supportTicketRatingWoRepo;
    private readonly ISupportTicketNotifier _supportTicketNotifier;
    private readonly SupportTicketWriter _supportTicketWriter;
    private readonly SupportTicketConfiguration _supportTicketConfiguration;

    public SupportTicketRatingService(
        IReadOnlyRepository<SupportTicketRating> supportTicketRatingRoRepo,
        IWriteOnlyRepository<SupportTicketRating> supportTicketRatingWoRepo,
        ISupportTicketNotifier supportTicketNotifier,
        SupportTicketWriter supportTicketWriter,
        SupportTicketConfiguration supportTicketConfiguration)
    {
        _supportTicketRatingRoRepo = supportTicketRatingRoRepo;
        _supportTicketRatingWoRepo = supportTicketRatingWoRepo;
        _supportTicketNotifier = supportTicketNotifier;
        _supportTicketWriter = supportTicketWriter;
        _supportTicketConfiguration = supportTicketConfiguration;
    }

    public void EnsureRatingRequested(Guid supportTicketGuid, Guid? closingAgentGuid)
    {
        if (_supportTicketRatingRoRepo.GetFirst(x => x.SupportTicketGuid == supportTicketGuid) != null)
            return;

        _supportTicketRatingWoRepo.InsertData(new SupportTicketRating
        {
            Guid = Guid.NewGuid(),
            SupportTicketGuid = supportTicketGuid,
            RatedAgentGuid = closingAgentGuid,
            RequestedUtc = DateTime.UtcNow
        });
    }

    public void Submit(SupportTicket supportTicket, int score, string comment)
    {
        if (score is < 1 or > 5)
            throw new FasApiErrorException("Score has to be between 1 and 5", 400);

        if (supportTicket.Deleted)
            throw new ResultNotFoundException("Ticket was not found");

        if (supportTicket.Status is not (SupportTicketStatus.Closed or SupportTicketStatus.Resolved))
            throw new FasApiErrorException("Only closed tickets can be rated", 400);

        var rating = _supportTicketRatingRoRepo.GetFirst(x => x.SupportTicketGuid == supportTicket.Guid)
                     ?? throw new ResultNotFoundException("Rating has not been requested for this ticket");

        // One answer per ticket is what keeps the statistics honest.
        if (rating.Score != null)
            throw new FasApiErrorException("This ticket has already been rated", 400);

        var now = DateTime.UtcNow;
        var trimmedComment = SupportTicketInputValidation.Trim(comment, SupportTicketContentLimits.RatingComment);

        _supportTicketRatingWoRepo.UpdateData(x => x.Guid == rating.Guid, x =>
        {
            x.Score = score;
            x.Comment = trimmedComment;
            x.RatedUtc = now;
        });

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Rated, null, score.ToString(),
            supportTicket.RequesterGuid, supportTicket.RequesterName);

        if (score <= _supportTicketConfiguration.LowRatingThreshold)
            _supportTicketNotifier.LowRatingReceived(supportTicket, score, trimmedComment);
    }
}
