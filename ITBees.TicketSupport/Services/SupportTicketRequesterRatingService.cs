using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

public class SupportTicketRequesterRatingService : ISupportTicketRequesterRatingService
{
    private readonly IReadOnlyRepository<SupportTicket> _supportTicketRoRepo;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly ISupportTicketRatingService _supportTicketRatingService;
    private readonly SupportTicketViewMapper _supportTicketViewMapper;
    private readonly SupportTicketRequesterAccess _supportTicketRequesterAccess;

    public SupportTicketRequesterRatingService(IReadOnlyRepository<SupportTicket> supportTicketRoRepo,
        IAspCurrentUserService aspCurrentUserService, ISupportTicketRatingService supportTicketRatingService,
        SupportTicketViewMapper supportTicketViewMapper, SupportTicketRequesterAccess supportTicketRequesterAccess)
    {
        _supportTicketRoRepo = supportTicketRoRepo;
        _aspCurrentUserService = aspCurrentUserService;
        _supportTicketRatingService = supportTicketRatingService;
        _supportTicketViewMapper = supportTicketViewMapper;
        _supportTicketRequesterAccess = supportTicketRequesterAccess;
    }

    public SupportTicketVm Create(SupportTicketRequesterRatingIm supportTicketRequesterRatingIm)
    {
        var currentUserGuid = _aspCurrentUserService.GetCurrentUserGuid()
                              ?? throw new FasApiErrorException("Sign in to rate your ticket", 401);
        var ticket = _supportTicketRoRepo.GetFirst(x => x.Guid == supportTicketRequesterRatingIm.SupportTicketGuid && !x.Deleted)
                     ?? throw new ResultNotFoundException("Ticket was not found");

        // Desk access never grants permission to submit a rating on behalf of another requester.
        if (ticket.RequesterGuid != currentUserGuid)
            throw new FasApiErrorException("Only the requester can rate this ticket", 403);
        _supportTicketRequesterAccess.Check(ticket, true);

        _supportTicketRatingService.Submit(ticket, supportTicketRequesterRatingIm.Score,
            supportTicketRequesterRatingIm.Comment);

        return _supportTicketViewMapper.ToDetails(ticket, false);
    }
}
