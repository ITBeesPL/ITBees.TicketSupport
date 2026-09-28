using ITBees.Interfaces.Repository;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// Builds the list rows and the detail view. <c>includeInternal</c> decides between the desk view
/// (internal notes and the full audit trail) and the requester view.
/// </summary>
public class SupportTicketViewMapper
{
    private static readonly HashSet<string> RequesterVisibleEvents = new()
    {
        SupportTicketEventTypes.Created,
        SupportTicketEventTypes.Reopened,
        SupportTicketEventTypes.Closed,
        SupportTicketEventTypes.StatusChanged,
        SupportTicketEventTypes.RatingRequested,
        SupportTicketEventTypes.Rated
    };

    private readonly IReadOnlyRepository<SupportTicketMessage> _supportTicketMessageRoRepo;
    private readonly IReadOnlyRepository<SupportTicketAttachment> _supportTicketAttachmentRoRepo;
    private readonly IReadOnlyRepository<SupportTicketRating> _supportTicketRatingRoRepo;
    private readonly IReadOnlyRepository<SupportTicketEvent> _supportTicketEventRoRepo;
    private readonly ISupportTicketRequesterResolver _supportTicketRequesterResolver;

    public SupportTicketViewMapper(IReadOnlyRepository<SupportTicketMessage> supportTicketMessageRoRepo,
        IReadOnlyRepository<SupportTicketAttachment> supportTicketAttachmentRoRepo,
        IReadOnlyRepository<SupportTicketRating> supportTicketRatingRoRepo,
        IReadOnlyRepository<SupportTicketEvent> supportTicketEventRoRepo,
        ISupportTicketRequesterResolver supportTicketRequesterResolver)
    {
        _supportTicketMessageRoRepo = supportTicketMessageRoRepo;
        _supportTicketAttachmentRoRepo = supportTicketAttachmentRoRepo;
        _supportTicketRatingRoRepo = supportTicketRatingRoRepo;
        _supportTicketEventRoRepo = supportTicketEventRoRepo;
        _supportTicketRequesterResolver = supportTicketRequesterResolver;
    }

    public List<SupportTicketListItemVm> ToListItems(IReadOnlyCollection<SupportTicket> supportTickets)
    {
        var items = supportTickets.Select(x => new SupportTicketListItemVm(x)).ToList();
        if (items.Count == 0)
            return items;

        var guids = supportTickets.Select(x => x.Guid).ToList();

        var scores = _supportTicketRatingRoRepo
            .GetData(x => guids.Contains(x.SupportTicketGuid) && x.Score != null)
            .ToDictionary(x => x.SupportTicketGuid, x => x.Score);

        // Counted by the database. Loading the rows would pull every body - up to 8 MB of HTML each -
        // into memory for every list page, so a requester posting large messages slowed the desk queue.
        var messageCounts = _supportTicketMessageRoRepo
            .GetDataQueryable(x => guids.Contains(x.SupportTicketGuid) && x.IsPublic)
            .GroupBy(x => x.SupportTicketGuid)
            .Select(x => new { SupportTicketGuid = x.Key, Count = x.Count() })
            .ToDictionary(x => x.SupportTicketGuid, x => x.Count);

        var assigneeNames = ResolveNames(supportTickets
            .Where(x => x.AssignedToGuid.HasValue)
            .Select(x => x.AssignedToGuid.Value));

        foreach (var item in items)
        {
            if (item.AssignedToGuid.HasValue)
                item.AssignedToName = assigneeNames.GetValueOrDefault(item.AssignedToGuid.Value);

            item.RatingScore = scores.GetValueOrDefault(item.Guid);
            item.MessageCount = messageCounts.GetValueOrDefault(item.Guid);
        }

        return items;
    }

    public SupportTicketVm ToDetails(SupportTicket supportTicket, bool includeInternal)
    {
        var vm = new SupportTicketVm(supportTicket);
        var listItem = ToListItems(new[] { supportTicket }).First();
        vm.AssignedToName = listItem.AssignedToName;
        vm.RatingScore = listItem.RatingScore;
        vm.MessageCount = listItem.MessageCount;

        var messages = _supportTicketMessageRoRepo
            .GetData(x => x.SupportTicketGuid == supportTicket.Guid)
            .Where(x => includeInternal || x.IsPublic)
            .OrderBy(x => x.CreatedUtc)
            .ToList();
        var attachments = _supportTicketAttachmentRoRepo
            .GetData(x => x.SupportTicketGuid == supportTicket.Guid)
            .ToList();
        var authorNames = ResolveNames(messages.Where(x => x.AuthorGuid.HasValue).Select(x => x.AuthorGuid.Value));

        vm.Messages = messages.Select(x =>
        {
            var messageVm = new SupportTicketMessageVm(x);
            if (string.IsNullOrWhiteSpace(messageVm.AuthorName) && x.AuthorGuid.HasValue)
                messageVm.AuthorName = authorNames.GetValueOrDefault(x.AuthorGuid.Value);
            // The address on a desk answer is the login of a support account; the requester side gets
            // the name only.
            if (!includeInternal && x.Direction != SupportTicketMessageDirection.Inbound)
                messageVm.AuthorEmail = null;
            messageVm.Attachments = attachments
                .Where(a => a.SupportTicketMessageGuid == x.Guid)
                .Select(a => new SupportTicketAttachmentVm(a))
                .ToList();
            return messageVm;
        }).ToList();

        // A requester sees only the attachments of messages they are allowed to read.
        var visibleMessageGuids = messages.Select(x => x.Guid).ToHashSet();
        vm.Attachments = attachments
            .Where(x => includeInternal ||
                        (x.SupportTicketMessageGuid.HasValue && visibleMessageGuids.Contains(x.SupportTicketMessageGuid.Value)))
            .Select(x => new SupportTicketAttachmentVm(x))
            .ToList();

        vm.Events = _supportTicketEventRoRepo
            .GetData(x => x.SupportTicketGuid == supportTicket.Guid)
            .Where(x => includeInternal || RequesterVisibleEvents.Contains(x.EventType))
            .OrderBy(x => x.CreatedUtc)
            .Select(x => new SupportTicketEventVm(x))
            .ToList();

        if (includeInternal && supportTicket.ClosedByGuid.HasValue)
            vm.ClosedByName = _supportTicketRequesterResolver.ResolveByGuid(supportTicket.ClosedByGuid.Value)?.DisplayName;

        var rating = _supportTicketRatingRoRepo.GetFirst(x => x.SupportTicketGuid == supportTicket.Guid);
        if (rating != null)
            vm.Rating = new SupportTicketRatingVm(rating);

        return vm;
    }

    /// <summary>Whether the rating form is offered: only to the requester, once, on a finished ticket.</summary>
    public static bool CanRate(SupportTicket supportTicket, SupportTicketVm vm, Guid? currentUserGuid) =>
        currentUserGuid != null && supportTicket.RequesterGuid == currentUserGuid &&
        vm.Rating != null && vm.Rating.Score == null &&
        supportTicket.Status is SupportTicketStatus.Closed or SupportTicketStatus.Resolved;

    private IReadOnlyDictionary<Guid, string> ResolveNames(IEnumerable<Guid> userGuids)
    {
        var guids = userGuids.Distinct().ToList();
        return guids.Count == 0
            ? new Dictionary<Guid, string>()
            : _supportTicketRequesterResolver.ResolveNames(guids);
    }
}
