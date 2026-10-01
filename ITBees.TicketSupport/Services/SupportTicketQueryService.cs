using ITBees.Interfaces.Repository;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.UserManager.Interfaces;

namespace ITBees.TicketSupport.Services;

public class SupportTicketQueryService : ISupportTicketQueryService
{
    private readonly IReadOnlyRepository<SupportTicket> _supportTicketRoRepo;
    private readonly IReadOnlyRepository<SupportTicketEvent> _eventRoRepo;
    private readonly ISupportTicketDeskAccess _deskAccess;
    private readonly ISupportTicketRequesterResolver _requesterResolver;
    private readonly SupportTicketViewMapper _viewMapper;
    private readonly IAspCurrentUserService _aspCurrentUserService;
    private readonly SupportTicketRequesterAccess _supportTicketRequesterAccess;

    public SupportTicketQueryService(
        IReadOnlyRepository<SupportTicket> supportTicketRoRepo,
        IReadOnlyRepository<SupportTicketEvent> eventRoRepo,
        ISupportTicketDeskAccess deskAccess,
        ISupportTicketRequesterResolver requesterResolver,
        SupportTicketViewMapper viewMapper,
        IAspCurrentUserService aspCurrentUserService,
        SupportTicketRequesterAccess supportTicketRequesterAccess)
    {
        _supportTicketRoRepo = supportTicketRoRepo;
        _eventRoRepo = eventRoRepo;
        _deskAccess = deskAccess;
        _requesterResolver = requesterResolver;
        _viewMapper = viewMapper;
        _aspCurrentUserService = aspCurrentUserService;
        _supportTicketRequesterAccess = supportTicketRequesterAccess;
    }

    public PaginatedResult<SupportTicketListItemVm> GetForDesk(SupportTicketListFilter filter)
    {
        _deskAccess.CheckDeskAccess();
        return Paginate(BuildBaseQuery(filter), filter);
    }

    public PaginatedResult<SupportTicketListItemVm> GetMine(SupportTicketListFilter filter)
    {
        var query = _supportTicketRequesterAccess.Filter(BuildBaseQuery(filter));
        return Paginate(query, filter);
    }

    public SupportTicketVm GetDetails(Guid supportTicketGuid, bool forRequester = false)
    {
        var supportTicket = _supportTicketRoRepo.GetFirst(x => x.Guid == supportTicketGuid && !x.Deleted)
                            ?? throw new ResultNotFoundException("Ticket was not found");

        var isDeskUser = !forRequester && _deskAccess.IsDeskUser();

        if (!isDeskUser)
            _supportTicketRequesterAccess.Check(supportTicket, false);

        var vm = _viewMapper.ToDetails(supportTicket, isDeskUser);
        vm.CanRate = !isDeskUser &&
                     SupportTicketViewMapper.CanRate(supportTicket, vm, _aspCurrentUserService.GetCurrentUserGuid());
        return vm;
    }

    public List<SupportTicketEventVm> GetEvents(Guid supportTicketGuid)
    {
        _deskAccess.CheckDeskAccess();

        var events = _eventRoRepo.GetData(x => x.SupportTicketGuid == supportTicketGuid)
            .OrderBy(x => x.CreatedUtc)
            .ToList();

        var actorGuids = events.Where(x => x.ActorGuid != null).Select(x => x.ActorGuid.Value)
            .Distinct().ToList();
        var names = actorGuids.Count == 0
            ? new Dictionary<Guid, string>()
            : _requesterResolver.ResolveNames(actorGuids);

        return events.Select(x =>
        {
            var vm = new SupportTicketEventVm(x);
            if (string.IsNullOrWhiteSpace(vm.ActorName) && x.ActorGuid != null)
                vm.ActorName = names.GetValueOrDefault(x.ActorGuid.Value);
            return vm;
        }).ToList();
    }

    private IQueryable<SupportTicket> BuildBaseQuery(SupportTicketListFilter filter)
    {
        var query = _supportTicketRoRepo.GetDataQueryable(x => !x.Deleted);

        if (filter.Status != null)
            query = query.Where(x => x.Status == filter.Status);

        if (filter.OnlyOpen)
            query = query.Where(x => x.Status != SupportTicketStatus.Closed && x.Status != SupportTicketStatus.Resolved);

        if (filter.Priority != null)
            query = query.Where(x => x.Priority == filter.Priority);

        if (filter.OnlyUnassigned)
            query = query.Where(x => x.AssignedToGuid == null);
        else if (filter.AssignedToGuid != null)
            query = query.Where(x => x.AssignedToGuid == filter.AssignedToGuid);

        if (filter.CreatedFromUtc != null)
            query = query.Where(x => x.CreatedUtc >= filter.CreatedFromUtc);

        if (filter.CreatedToUtc != null)
            query = query.Where(x => x.CreatedUtc <= filter.CreatedToUtc);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            // A support desk is searched by ticket number more often than by anything else, so a
            // numeric search matches the number exactly and the text fields as well.
            if (long.TryParse(search, out var number))
                query = query.Where(x => x.Number == number || x.Subject.Contains(search) ||
                                         x.RequesterEmail.Contains(search));
            else
                query = query.Where(x => x.Subject.Contains(search) ||
                                         x.RequesterEmail.Contains(search) ||
                                         x.RequesterName.Contains(search));
        }

        return query;
    }

    private PaginatedResult<SupportTicketListItemVm> Paginate(IQueryable<SupportTicket> query,
        SupportTicketListFilter filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 200 ? 25 : filter.PageSize;

        var allElements = query.Count();
        var ordered = ApplySort(query, filter);

        var supportTickets = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var data = _viewMapper.ToListItems(supportTickets);

        var pages = pageSize == 0 ? 0 : (int)Math.Ceiling(allElements / (double)pageSize);
        return new PaginatedResult<SupportTicketListItemVm>(allElements, pages, page, pageSize, data);
    }

    private static IQueryable<SupportTicket> ApplySort(IQueryable<SupportTicket> query,
        SupportTicketListFilter filter)
    {
        var descending = filter.SortOrder != SortOrder.Ascending;

        return filter.SortColumn?.ToLowerInvariant() switch
        {
            "number" => descending ? query.OrderByDescending(x => x.Number) : query.OrderBy(x => x.Number),
            "createdutc" or "created" => descending
                ? query.OrderByDescending(x => x.CreatedUtc)
                : query.OrderBy(x => x.CreatedUtc),
            "status" => descending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "priority" => descending ? query.OrderByDescending(x => x.Priority) : query.OrderBy(x => x.Priority),
            "subject" => descending ? query.OrderByDescending(x => x.Subject) : query.OrderBy(x => x.Subject),
            "requester" => descending
                ? query.OrderByDescending(x => x.RequesterName)
                : query.OrderBy(x => x.RequesterName),
            // The desk works the most recently active thread first; that is the default the queue
            // opens with, not the creation order.
            _ => descending ? query.OrderByDescending(x => x.UpdatedUtc) : query.OrderBy(x => x.UpdatedUtc)
        };
    }
}
