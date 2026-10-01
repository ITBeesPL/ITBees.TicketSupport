using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly IWriteOnlyRepository<SupportTicket> _supportTicketWoRepo;
    private readonly SupportTicketWriter _supportTicketWriter;
    private readonly SupportTicketViewMapper _supportTicketViewMapper;
    private readonly ISupportTicketDeskAccess _supportTicketDeskAccess;
    private readonly ISupportTicketRequesterResolver _supportTicketRequesterResolver;
    private readonly ISupportTicketRatingService _supportTicketRatingService;
    private readonly ISupportTicketNotifier _supportTicketNotifier;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly SupportTicketRequesterAccess _supportTicketRequesterAccess;

    public SupportTicketService(IWriteOnlyRepository<SupportTicket> supportTicketWoRepo,
        SupportTicketWriter supportTicketWriter, SupportTicketViewMapper supportTicketViewMapper,
        ISupportTicketDeskAccess supportTicketDeskAccess, ISupportTicketRequesterResolver supportTicketRequesterResolver,
        ISupportTicketRatingService supportTicketRatingService, ISupportTicketNotifier supportTicketNotifier,
        IAspCurrentUserService aspCurrentUserService, SupportTicketRequesterAccess supportTicketRequesterAccess)
    {
        _supportTicketWoRepo = supportTicketWoRepo;
        _supportTicketWriter = supportTicketWriter;
        _supportTicketViewMapper = supportTicketViewMapper;
        _supportTicketDeskAccess = supportTicketDeskAccess;
        _supportTicketRequesterResolver = supportTicketRequesterResolver;
        _supportTicketRatingService = supportTicketRatingService;
        _supportTicketNotifier = supportTicketNotifier;
        _aspCurrentUserService = aspCurrentUserService;
        _supportTicketRequesterAccess = supportTicketRequesterAccess;
    }

    public SupportTicketVm Create(SupportTicketIm supportTicketIm, SupportTicketReference reference = null)
    {
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        var isDeskUser = _supportTicketDeskAccess.IsDeskUser();
        var requester = ResolveRequester(supportTicketIm, currentUser, isDeskUser);
        var context = _supportTicketRequesterAccess.CheckContext(supportTicketIm.ContextType,
            supportTicketIm.ContextGuid, true);

        // Validate the message before anything is written, so a rejected body leaves no orphan ticket.
        var (body, bodyHtml) = SupportTicketRichText.Normalize(supportTicketIm.Message, supportTicketIm.MessageHtml);

        if (supportTicketIm.Priority.HasValue && !Enum.IsDefined(supportTicketIm.Priority.Value))
            throw new FasApiErrorException("Invalid priority", 400);

        if (reference != null && (string.IsNullOrWhiteSpace(reference.Type) || string.IsNullOrWhiteSpace(reference.Id)))
            throw new FasApiErrorException("Reference type and identifier must be provided together", 400);

        // Support entering a ticket on somebody else's behalf means it came in by phone.
        var channel = isDeskUser && requester.Guid != currentUser?.Guid
            ? SupportTicketChannel.Phone
            : SupportTicketChannel.Panel;

        var supportTicket = _supportTicketWriter.Create(new SupportTicketCreateCommand
        {
            Subject = supportTicketIm.Subject,
            ContextType = context?.Type,
            ContextGuid = context?.Guid,
            ContextName = context?.Name,
            ReferenceType = reference?.Type,
            ReferenceId = reference?.Id,
            Priority = supportTicketIm.Priority,
            Channel = channel,
            RequesterGuid = requester.Guid == Guid.Empty ? null : requester.Guid,
            RequesterEmail = requester.Email,
            RequesterName = requester.DisplayName,
            ActorGuid = currentUser?.Guid,
            ActorName = currentUser?.DisplayName
        });

        _supportTicketWriter.AppendMessage(supportTicket, new SupportTicketMessageCommand
        {
            Direction = SupportTicketMessageDirection.Inbound,
            AuthorGuid = currentUser?.Guid,
            AuthorName = currentUser?.DisplayName ?? requester.DisplayName,
            AuthorEmail = requester.Email,
            Body = body,
            BodyHtml = bodyHtml
        });

        _supportTicketNotifier.TicketCreated(supportTicket);
        return _supportTicketViewMapper.ToDetails(supportTicket, false);
    }

    public SupportTicketVm ReplyAsRequester(SupportTicketReplyIm supportTicketReplyIm)
    {
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketReplyIm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        _supportTicketRequesterAccess.Check(supportTicket, true);

        var wasFinished = IsFinished(supportTicket);
        var message = _supportTicketWriter.AppendMessage(supportTicket, new SupportTicketMessageCommand
        {
            Direction = SupportTicketMessageDirection.Inbound,
            AuthorGuid = currentUser?.Guid,
            AuthorName = currentUser?.DisplayName,
            AuthorEmail = currentUser?.Email ?? supportTicket.RequesterEmail,
            Body = supportTicketReplyIm.Message,
            BodyHtml = supportTicketReplyIm.MessageHtml
        });

        _supportTicketWriter.ApplyMessageToTicket(supportTicket, message);
        _supportTicketWriter.LogEvent(supportTicket.Guid,
            wasFinished ? SupportTicketEventTypes.Reopened : SupportTicketEventTypes.MessageReceived,
            null, null, currentUser?.Guid, currentUser?.DisplayName);
        _supportTicketNotifier.RequesterReplied(supportTicket, message);
        return _supportTicketViewMapper.ToDetails(_supportTicketWriter.GetOrThrow(supportTicket.Guid), false);
    }

    public SupportTicketVm ReplyAsAgent(SupportTicketReplyIm supportTicketReplyIm)
    {
        _supportTicketDeskAccess.CheckDeskAccess();
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketReplyIm.SupportTicketGuid);
        if (IsFinished(supportTicket))
            throw new FasApiErrorException("Reopen the ticket before replying", 409);

        var currentUser = _aspCurrentUserService.GetCurrentUser();
        var message = _supportTicketWriter.AppendMessage(supportTicket, new SupportTicketMessageCommand
        {
            Direction = SupportTicketMessageDirection.Outbound,
            AuthorGuid = currentUser?.Guid,
            AuthorName = currentUser?.DisplayName,
            AuthorEmail = currentUser?.Email,
            Body = supportTicketReplyIm.Message,
            BodyHtml = supportTicketReplyIm.MessageHtml
        });

        _supportTicketWriter.ApplyMessageToTicket(supportTicket, message);
        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Replied, null, null,
            currentUser?.Guid, currentUser?.DisplayName);
        return _supportTicketViewMapper.ToDetails(supportTicket, true);
    }

    public SupportTicketVm AddInternalNote(SupportTicketNoteIm supportTicketNoteIm)
    {
        _supportTicketDeskAccess.CheckDeskAccess();
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketNoteIm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();

        _supportTicketWriter.AppendMessage(supportTicket, new SupportTicketMessageCommand
        {
            Direction = SupportTicketMessageDirection.InternalNote,
            AuthorGuid = currentUser?.Guid,
            AuthorName = currentUser?.DisplayName,
            AuthorEmail = currentUser?.Email,
            Body = supportTicketNoteIm.Message,
            BodyHtml = supportTicketNoteIm.MessageHtml
        });

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.NoteAdded, null, null,
            currentUser?.Guid, currentUser?.DisplayName);
        return _supportTicketViewMapper.ToDetails(supportTicket, true);
    }

    public SupportTicketVm ReplyAsTrustedCaller(SupportTicketTrustedReplyCommand command)
    {
        var supportTicket = _supportTicketWriter.GetOrThrow(command.SupportTicketGuid);
        if (!command.InternalNote && IsFinished(supportTicket))
            throw new FasApiErrorException("Reopen the ticket before replying", 409);

        var authorName = SupportTicketInputValidation.RequireText(command.AuthorName,
            SupportTicketContentLimits.PersonName, "Author name");
        var message = _supportTicketWriter.AppendMessage(supportTicket, new SupportTicketMessageCommand
        {
            Direction = command.InternalNote
                ? SupportTicketMessageDirection.InternalNote
                : SupportTicketMessageDirection.Outbound,
            AuthorName = authorName,
            Body = command.Message
        });

        if (command.InternalNote)
        {
            _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.NoteAdded, null, null, null,
                authorName);
        }
        else
        {
            _supportTicketWriter.ApplyMessageToTicket(supportTicket, message);
            _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Replied, null, null, null,
                authorName);
        }

        // ApplyMessageToTicket keeps the loaded row in step; re-reading it could return a stale tracked copy.
        return _supportTicketViewMapper.ToDetails(supportTicket, true);
    }

    public SupportTicketVm Assign(SupportTicketAssignUm supportTicketAssignUm)
    {
        _supportTicketDeskAccess.CheckDeskAccess();
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketAssignUm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        var previousAssignee = supportTicket.AssignedToGuid;
        var now = DateTime.UtcNow;

        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.AssignedToGuid = supportTicketAssignUm.AssignedToGuid;
            x.UpdatedUtc = now;
            if (x.Status == SupportTicketStatus.New && supportTicketAssignUm.AssignedToGuid.HasValue)
                x.Status = SupportTicketStatus.Open;
        });

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Assigned,
            previousAssignee?.ToString(), supportTicketAssignUm.AssignedToGuid?.ToString(),
            currentUser?.Guid, currentUser?.DisplayName);
        return _supportTicketViewMapper.ToDetails(_supportTicketWriter.GetOrThrow(supportTicket.Guid), true);
    }

    public SupportTicketVm ChangeStatus(SupportTicketStatusUm supportTicketStatusUm)
    {
        _supportTicketDeskAccess.CheckDeskAccess();
        if (supportTicketStatusUm.Status == SupportTicketStatus.Closed)
            throw new FasApiErrorException("Use the closure endpoint to close a ticket", 400);

        if (!Enum.IsDefined(supportTicketStatusUm.Status) ||
            (supportTicketStatusUm.Priority.HasValue && !Enum.IsDefined(supportTicketStatusUm.Priority.Value)))
            throw new FasApiErrorException("Invalid status or priority", 400);

        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketStatusUm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        var previousStatus = supportTicket.Status;
        var previousPriority = supportTicket.Priority;

        if (previousStatus == SupportTicketStatus.Closed)
            throw new FasApiErrorException("Reopen the ticket before changing its status", 400);

        var now = DateTime.UtcNow;
        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.Status = supportTicketStatusUm.Status;
            x.UpdatedUtc = now;
            if (supportTicketStatusUm.Priority.HasValue)
                x.Priority = supportTicketStatusUm.Priority.Value;
            x.ResolvedUtc = supportTicketStatusUm.Status == SupportTicketStatus.Resolved ? now : null;
        });

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.StatusChanged,
            previousStatus.ToString(), supportTicketStatusUm.Status.ToString(), currentUser?.Guid,
            currentUser?.DisplayName);

        if (supportTicketStatusUm.Priority.HasValue && supportTicketStatusUm.Priority != previousPriority)
            _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.PriorityChanged,
                previousPriority.ToString(), supportTicketStatusUm.Priority.ToString(), currentUser?.Guid,
                currentUser?.DisplayName);

        return _supportTicketViewMapper.ToDetails(_supportTicketWriter.GetOrThrow(supportTicket.Guid), true);
    }

    public SupportTicketVm Close(SupportTicketCloseIm supportTicketCloseIm)
    {
        _supportTicketDeskAccess.CheckDeskAccess();
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketCloseIm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();

        if (!Enum.IsDefined(supportTicketCloseIm.CloseReason))
            throw new FasApiErrorException("Invalid close reason", 400);

        if (supportTicket.Status == SupportTicketStatus.Closed)
            return _supportTicketViewMapper.ToDetails(supportTicket, true);

        if (supportTicketCloseIm.CloseReason == SupportTicketCloseReason.Arbitrary &&
            string.IsNullOrWhiteSpace(supportTicketCloseIm.CloseNote))
            throw new FasApiErrorException("An arbitrary close requires a note", 400);

        var previousStatus = supportTicket.Status;
        var now = DateTime.UtcNow;
        var closeNote = SupportTicketInputValidation.Trim(supportTicketCloseIm.CloseNote, SupportTicketContentLimits.Body);

        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.Status = SupportTicketStatus.Closed;
            x.ClosedUtc = now;
            x.ResolvedUtc ??= now;
            x.CloseReason = supportTicketCloseIm.CloseReason;
            x.ClosedByGuid = currentUser?.Guid;
            x.CloseNote = closeNote;
            x.UpdatedUtc = now;
        });

        supportTicket.Status = SupportTicketStatus.Closed;
        supportTicket.ClosedUtc = now;
        supportTicket.CloseReason = supportTicketCloseIm.CloseReason;
        supportTicket.CloseNote = closeNote;

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Closed, previousStatus.ToString(),
            supportTicketCloseIm.CloseReason.ToString(), currentUser?.Guid, currentUser?.DisplayName);

        if (supportTicketCloseIm.AskForRating)
        {
            _supportTicketRatingService.EnsureRatingRequested(supportTicket.Guid, currentUser?.Guid);
            _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.RatingRequested, null, null,
                currentUser?.Guid, currentUser?.DisplayName);
        }

        _supportTicketNotifier.TicketClosed(supportTicket);
        return _supportTicketViewMapper.ToDetails(_supportTicketWriter.GetOrThrow(supportTicket.Guid), true);
    }

    public SupportTicketVm Reopen(SupportTicketReopenIm supportTicketReopenIm)
    {
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketReopenIm.SupportTicketGuid);
        var currentUser = _aspCurrentUserService.GetCurrentUser();
        var isDeskUser = _supportTicketDeskAccess.IsDeskUser();

        if (currentUser == null || (!isDeskUser && supportTicket.RequesterGuid != currentUser.Guid))
            throw new FasApiErrorException("This ticket belongs to somebody else", 403);

        if (!IsFinished(supportTicket))
            throw new FasApiErrorException("Only a closed ticket can be reopened", 400);

        var now = DateTime.UtcNow;
        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.Status = isDeskUser ? SupportTicketStatus.Open : SupportTicketStatus.WaitingForAgent;
            x.ClosedUtc = null;
            x.ResolvedUtc = null;
            x.CloseReason = null;
            x.CloseNote = null;
            x.ClosedByGuid = null;
            x.ReopenCount++;
            x.UpdatedUtc = now;
        });

        if (!string.IsNullOrWhiteSpace(supportTicketReopenIm.Message))
        {
            _supportTicketWriter.AppendMessage(_supportTicketWriter.GetOrThrow(supportTicket.Guid),
                new SupportTicketMessageCommand
                {
                    Direction = isDeskUser
                        ? SupportTicketMessageDirection.InternalNote
                        : SupportTicketMessageDirection.Inbound,
                    AuthorGuid = currentUser.Guid,
                    AuthorName = currentUser.DisplayName,
                    AuthorEmail = currentUser.Email,
                    Body = supportTicketReopenIm.Message
                });
        }

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Reopened,
            SupportTicketStatus.Closed.ToString(), null, currentUser.Guid, currentUser.DisplayName);
        return _supportTicketViewMapper.ToDetails(_supportTicketWriter.GetOrThrow(supportTicket.Guid), isDeskUser);
    }

    private static bool IsFinished(SupportTicket supportTicket) =>
        supportTicket.Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed;

    private SupportTicketPerson ResolveRequester(SupportTicketIm supportTicketIm, CurrentUser currentUser,
        bool isDeskUser)
    {
        if (isDeskUser && !string.IsNullOrWhiteSpace(supportTicketIm.RequesterEmail))
        {
            var email = SupportTicketInputValidation.Trim(supportTicketIm.RequesterEmail, SupportTicketContentLimits.Email);
            if (!SupportTicketInputValidation.IsEmailAddress(email))
                throw new FasApiErrorException("Requester e-mail address is not valid", 400);

            return _supportTicketRequesterResolver.ResolveByEmail(email) ?? new SupportTicketPerson
            {
                Email = email,
                DisplayName = SupportTicketInputValidation.Trim(supportTicketIm.RequesterName,
                    SupportTicketContentLimits.PersonName)
            };
        }

        if (currentUser == null)
            throw new FasApiErrorException("A ticket cannot be raised without a signed-in user", 401);

        return new SupportTicketPerson
        {
            Guid = currentUser.Guid,
            Email = currentUser.Email,
            DisplayName = currentUser.DisplayName
        };
    }
}
