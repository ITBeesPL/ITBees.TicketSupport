using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using Microsoft.Extensions.Logging;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// The only place rows are written. Access checks happen in the services above it; this class
/// assumes the caller is allowed to do what it asks for.
/// </summary>
public class SupportTicketWriter
{
    private const int NumberRetries = 5;

    private readonly IWriteOnlyRepository<SupportTicket> _supportTicketWoRepo;
    private readonly IReadOnlyRepository<SupportTicket> _supportTicketRoRepo;
    private readonly IWriteOnlyRepository<SupportTicketMessage> _supportTicketMessageWoRepo;
    private readonly IReadOnlyRepository<SupportTicketMessage> _supportTicketMessageRoRepo;
    private readonly IWriteOnlyRepository<SupportTicketEvent> _supportTicketEventWoRepo;
    private readonly ISupportTicketNumberGenerator _supportTicketNumberGenerator;
    private readonly ILogger<SupportTicketWriter> _logger;

    public SupportTicketWriter(IWriteOnlyRepository<SupportTicket> supportTicketWoRepo,
        IReadOnlyRepository<SupportTicket> supportTicketRoRepo,
        IWriteOnlyRepository<SupportTicketMessage> supportTicketMessageWoRepo,
        IReadOnlyRepository<SupportTicketMessage> supportTicketMessageRoRepo,
        IWriteOnlyRepository<SupportTicketEvent> supportTicketEventWoRepo,
        ISupportTicketNumberGenerator supportTicketNumberGenerator, ILogger<SupportTicketWriter> logger)
    {
        _supportTicketWoRepo = supportTicketWoRepo;
        _supportTicketRoRepo = supportTicketRoRepo;
        _supportTicketMessageWoRepo = supportTicketMessageWoRepo;
        _supportTicketMessageRoRepo = supportTicketMessageRoRepo;
        _supportTicketEventWoRepo = supportTicketEventWoRepo;
        _supportTicketNumberGenerator = supportTicketNumberGenerator;
        _logger = logger;
    }

    public SupportTicket Create(SupportTicketCreateCommand command)
    {
        var now = DateTime.UtcNow;
        var supportTicket = new SupportTicket
        {
            Guid = Guid.NewGuid(),
            ContextType = command.ContextType,
            ContextGuid = command.ContextGuid,
            ContextName = SupportTicketInputValidation.Trim(command.ContextName, SupportTicketContentLimits.ContextName),
            ReferenceType = SupportTicketInputValidation.Trim(command.ReferenceType, SupportTicketContentLimits.ReferenceType),
            ReferenceId = SupportTicketInputValidation.Trim(command.ReferenceId, SupportTicketContentLimits.ReferenceId),
            Status = SupportTicketStatus.New,
            Priority = command.Priority ?? SupportTicketPriority.Normal,
            Channel = command.Channel,
            Subject = SupportTicketInputValidation.RequireText(command.Subject, SupportTicketContentLimits.Subject, "Subject"),
            RequesterGuid = command.RequesterGuid,
            RequesterEmail = SupportTicketInputValidation.Trim(command.RequesterEmail, SupportTicketContentLimits.Email),
            RequesterName = SupportTicketInputValidation.Trim(command.RequesterName, SupportTicketContentLimits.PersonName),
            CreatedUtc = now,
            UpdatedUtc = now
        };

        InsertWithNumber(supportTicket);
        LogEvent(supportTicket.Guid, SupportTicketEventTypes.Created, null, null, command.ActorGuid, command.ActorName);
        return supportTicket;
    }

    public SupportTicketMessage AppendMessage(SupportTicket supportTicket, SupportTicketMessageCommand command)
    {
        var (body, bodyHtml) = SupportTicketRichText.Normalize(command.Body, command.BodyHtml);
        var message = new SupportTicketMessage
        {
            Guid = Guid.NewGuid(),
            SupportTicketGuid = supportTicket.Guid,
            Direction = command.Direction,
            AuthorGuid = command.AuthorGuid,
            AuthorName = SupportTicketInputValidation.Trim(command.AuthorName, SupportTicketContentLimits.PersonName),
            AuthorEmail = SupportTicketInputValidation.Trim(command.AuthorEmail, SupportTicketContentLimits.Email),
            Body = body,
            BodyHtml = bodyHtml,
            // Only the two sides of the conversation are public; notes and drafts stay on the desk.
            IsPublic = command.Direction is SupportTicketMessageDirection.Inbound
                or SupportTicketMessageDirection.Outbound,
            CreatedUtc = DateTime.UtcNow
        };

        _supportTicketMessageWoRepo.InsertData(message);
        return message;
    }

    public SupportTicketMessage GetDraftOrThrow(Guid supportTicketGuid, Guid supportTicketMessageGuid)
    {
        return _supportTicketMessageRoRepo.GetFirst(x => x.Guid == supportTicketMessageGuid &&
                                                         x.SupportTicketGuid == supportTicketGuid &&
                                                         x.Direction == SupportTicketMessageDirection.DraftReply)
               ?? throw new ResultNotFoundException("Draft was not found");
    }

    /// <summary>
    /// Turns the draft into the desk's answer in place, dated now: the requester sees it from this
    /// moment, and the draft is gone from the desk view. Null when somebody else got there first.
    /// </summary>
    public SupportTicketMessage PublishDraft(SupportTicketMessage draft)
    {
        var now = DateTime.UtcNow;
        return _supportTicketMessageWoRepo.UpdateData(
                x => x.Guid == draft.Guid && x.Direction == SupportTicketMessageDirection.DraftReply, x =>
                {
                    x.Direction = SupportTicketMessageDirection.Outbound;
                    x.IsPublic = true;
                    x.CreatedUtc = now;
                })
            .FirstOrDefault();
    }

    /// <summary>False when the draft was already published or discarded.</summary>
    public bool DeleteDraft(SupportTicketMessage draft) =>
        _supportTicketMessageWoRepo.DeleteData(x =>
            x.Guid == draft.Guid && x.Direction == SupportTicketMessageDirection.DraftReply) > 0;

    /// <summary>Moves the ticket through its life cycle after a public message was added.</summary>
    public void ApplyMessageToTicket(SupportTicket supportTicket, SupportTicketMessage message)
    {
        var now = DateTime.UtcNow;
        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.UpdatedUtc = now;
            switch (message.Direction)
            {
                case SupportTicketMessageDirection.Outbound:
                    x.FirstResponseUtc ??= now;
                    if (x.Status is SupportTicketStatus.New or SupportTicketStatus.Open or SupportTicketStatus.WaitingForAgent)
                        x.Status = SupportTicketStatus.WaitingForCustomer;
                    break;

                case SupportTicketMessageDirection.Inbound:
                    if (x.Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed)
                    {
                        // A requester answering a resolved thread reopens it rather than starting a new
                        // one. SupportTicketService refuses the requester's answer on a closed ticket
                        // before it gets here; the branch stays for callers that append on their own.
                        x.Status = SupportTicketStatus.WaitingForAgent;
                        x.ReopenCount++;
                        x.ClosedUtc = null;
                        x.ResolvedUtc = null;
                        x.CloseReason = null;
                        x.ClosedByGuid = null;
                        x.CloseNote = null;
                    }
                    else if (x.Status != SupportTicketStatus.New)
                    {
                        x.Status = SupportTicketStatus.WaitingForAgent;
                    }

                    break;
            }

            supportTicket.Status = x.Status;
            supportTicket.FirstResponseUtc = x.FirstResponseUtc;
        });
        supportTicket.UpdatedUtc = now;
    }

    public void LogEvent(Guid supportTicketGuid, string eventType, string fromValue, string toValue, Guid? actorGuid,
        string actorName)
    {
        _supportTicketEventWoRepo.InsertData(new SupportTicketEvent
        {
            Guid = Guid.NewGuid(),
            SupportTicketGuid = supportTicketGuid,
            EventType = eventType,
            FromValue = SupportTicketInputValidation.Trim(fromValue, SupportTicketContentLimits.Key),
            ToValue = SupportTicketInputValidation.Trim(toValue, SupportTicketContentLimits.Key),
            ActorGuid = actorGuid,
            ActorName = SupportTicketInputValidation.Trim(actorName, SupportTicketContentLimits.PersonName),
            CreatedUtc = DateTime.UtcNow
        });
    }

    public SupportTicket GetOrThrow(Guid supportTicketGuid)
    {
        return _supportTicketRoRepo.GetFirst(x => x.Guid == supportTicketGuid && !x.Deleted)
               ?? throw new ResultNotFoundException("Ticket was not found");
    }

    private void InsertWithNumber(SupportTicket supportTicket)
    {
        for (var attempt = 1; ; attempt++)
        {
            supportTicket.Number = _supportTicketNumberGenerator.Next();
            try
            {
                _supportTicketWoRepo.InsertData(supportTicket);
                return;
            }
            catch (Exception exception) when (attempt < NumberRetries)
            {
                // The unique index on Number is the arbiter between two instances; the loser retries.
                _logger.LogWarning(exception, "Ticket number {Number} was taken, retrying ({Attempt})",
                    supportTicket.Number, attempt);
            }
        }
    }
}
