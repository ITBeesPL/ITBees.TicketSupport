using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// The requester ends the thread from their panel once the matter is settled. Anybody who may
/// write to the ticket may close it - the original requester, or a coworker on a shared ticket.
/// </summary>
public class SupportTicketRequesterClosureService : ISupportTicketRequesterClosureService
{
    private readonly IWriteOnlyRepository<SupportTicket> _supportTicketWoRepo;
    private readonly SupportTicketWriter _supportTicketWriter;
    private readonly SupportTicketViewMapper _supportTicketViewMapper;
    private readonly SupportTicketRequesterAccess _supportTicketRequesterAccess;
    private readonly ISupportTicketRatingService _supportTicketRatingService;
    private readonly ISupportTicketNotifier _supportTicketNotifier;
    private readonly IAspCurrentUserService _aspCurrentUserService;

    public SupportTicketRequesterClosureService(IWriteOnlyRepository<SupportTicket> supportTicketWoRepo,
        SupportTicketWriter supportTicketWriter, SupportTicketViewMapper supportTicketViewMapper,
        SupportTicketRequesterAccess supportTicketRequesterAccess, ISupportTicketRatingService supportTicketRatingService,
        ISupportTicketNotifier supportTicketNotifier, IAspCurrentUserService aspCurrentUserService)
    {
        _supportTicketWoRepo = supportTicketWoRepo;
        _supportTicketWriter = supportTicketWriter;
        _supportTicketViewMapper = supportTicketViewMapper;
        _supportTicketRequesterAccess = supportTicketRequesterAccess;
        _supportTicketRatingService = supportTicketRatingService;
        _supportTicketNotifier = supportTicketNotifier;
        _aspCurrentUserService = aspCurrentUserService;
    }

    public SupportTicketVm Create(SupportTicketRequesterClosureIm supportTicketRequesterClosureIm)
    {
        var currentUser = _aspCurrentUserService.GetCurrentUser()
                          ?? throw new FasApiErrorException("Sign in to close your ticket", 401);
        var supportTicket = _supportTicketWriter.GetOrThrow(supportTicketRequesterClosureIm.SupportTicketGuid);
        _supportTicketRequesterAccess.Check(supportTicket, true);

        if (supportTicket.Status == SupportTicketStatus.Closed)
            return ToRequesterDetails(supportTicket, currentUser.Guid);

        var previousStatus = supportTicket.Status;
        var now = DateTime.UtcNow;
        var closeNote = SupportTicketInputValidation.Trim(supportTicketRequesterClosureIm.CloseNote,
            SupportTicketContentLimits.Body);

        _supportTicketWoRepo.UpdateData(x => x.Guid == supportTicket.Guid, x =>
        {
            x.Status = SupportTicketStatus.Closed;
            x.ClosedUtc = now;
            x.ResolvedUtc ??= now;
            x.CloseReason = SupportTicketCloseReason.ClosedByRequester;
            x.ClosedByGuid = currentUser.Guid;
            x.CloseNote = closeNote;
            x.UpdatedUtc = now;
        });

        // Keep the loaded row in step - re-reading it in this request could return a stale tracked copy.
        supportTicket.Status = SupportTicketStatus.Closed;
        supportTicket.ClosedUtc = now;
        supportTicket.ResolvedUtc ??= now;
        supportTicket.CloseReason = SupportTicketCloseReason.ClosedByRequester;
        supportTicket.ClosedByGuid = currentUser.Guid;
        supportTicket.CloseNote = closeNote;
        supportTicket.UpdatedUtc = now;

        _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.Closed, previousStatus.ToString(),
            SupportTicketCloseReason.ClosedByRequester.ToString(), currentUser.Guid, currentUser.DisplayName);

        // Once support has answered there is handling to grade, so the requester is asked right away.
        if (supportTicket.FirstResponseUtc != null)
        {
            _supportTicketRatingService.EnsureRatingRequested(supportTicket.Guid, supportTicket.AssignedToGuid);
            _supportTicketWriter.LogEvent(supportTicket.Guid, SupportTicketEventTypes.RatingRequested, null, null,
                currentUser.Guid, currentUser.DisplayName);
        }

        _supportTicketNotifier.TicketClosed(supportTicket);
        return ToRequesterDetails(supportTicket, currentUser.Guid);
    }

    private SupportTicketVm ToRequesterDetails(SupportTicket supportTicket, Guid currentUserGuid)
    {
        var vm = _supportTicketViewMapper.ToDetails(supportTicket, false);
        vm.CanRate = SupportTicketViewMapper.CanRate(supportTicket, vm, currentUserGuid);
        return vm;
    }
}
