using ITBees.Interfaces.Repository;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;

namespace ITBees.TicketSupport.Services;

/// <summary>
/// The statistics screen. Handling times come from the ticket rows, and the person a ticket counts
/// for is the one recorded when it was closed - not the current assignee, so a later reassignment
/// cannot move somebody else's numbers around.
/// </summary>
public class SupportTicketStatisticsService : ISupportTicketStatisticsService
{
    private readonly IReadOnlyRepository<SupportTicket> _supportTicketRoRepo;
    private readonly IReadOnlyRepository<SupportTicketRating> _ratingRoRepo;
    private readonly ISupportTicketDeskAccess _deskAccess;
    private readonly ISupportTicketRequesterResolver _requesterResolver;

    public SupportTicketStatisticsService(
        IReadOnlyRepository<SupportTicket> supportTicketRoRepo,
        IReadOnlyRepository<SupportTicketRating> ratingRoRepo,
        ISupportTicketDeskAccess deskAccess,
        ISupportTicketRequesterResolver requesterResolver)
    {
        _supportTicketRoRepo = supportTicketRoRepo;
        _ratingRoRepo = ratingRoRepo;
        _deskAccess = deskAccess;
        _requesterResolver = requesterResolver;
    }

    public SupportTicketStatisticsVm Get(DateTime? fromUtc, DateTime? toUtc, int lowestRatedCount = 20)
    {
        _deskAccess.CheckDeskAccess();

        var to = toUtc ?? DateTime.UtcNow;
        var from = fromUtc ?? to.AddDays(-30);

        // Everything opened in the period plus everything closed in it: a ticket opened last month
        // and closed this week belongs in this week's resolution time.
        var supportTickets = _supportTicketRoRepo
            .GetDataQueryable(x => !x.Deleted &&
                                   ((x.CreatedUtc >= from && x.CreatedUtc <= to) ||
                                    (x.ClosedUtc != null && x.ClosedUtc >= from && x.ClosedUtc <= to)))
            .ToList();

        var guids = supportTickets.Select(x => x.Guid).ToList();
        var ratings = guids.Count == 0
            ? new List<SupportTicketRating>()
            : _ratingRoRepo.GetData(x => guids.Contains(x.SupportTicketGuid) && x.Score != null).ToList();

        var ratingByTicket = ratings.ToDictionary(x => x.SupportTicketGuid);

        var opened = supportTickets.Where(x => x.CreatedUtc >= from && x.CreatedUtc <= to).ToList();
        var closed = supportTickets
            .Where(x => x.ClosedUtc != null && x.ClosedUtc >= from && x.ClosedUtc <= to)
            .ToList();

        var vm = new SupportTicketStatisticsVm
        {
            FromUtc = from,
            ToUtc = to,
            OpenedCount = opened.Count,
            ClosedCount = closed.Count,
            CurrentlyOpenCount = supportTickets.Count(x => x.Status != SupportTicketStatus.Closed),
            UnassignedCount = supportTickets.Count(x =>
                x.Status != SupportTicketStatus.Closed && x.AssignedToGuid == null),
            ArbitrarilyClosedCount = closed.Count(x => x.CloseReason == SupportTicketCloseReason.Arbitrary),
            RatedCount = ratings.Count,
            AverageRating = Average(ratings.Select(x => (double)x.Score.Value))
        };

        var firstResponseHours = opened
            .Where(x => x.FirstResponseUtc != null)
            .Select(x => (x.FirstResponseUtc.Value - x.CreatedUtc).TotalHours)
            .ToList();

        var resolutionHours = closed
            .Select(x => (x.ClosedUtc.Value - x.CreatedUtc).TotalHours)
            .ToList();

        vm.AverageFirstResponseHours = Average(firstResponseHours);
        vm.MedianFirstResponseHours = Median(firstResponseHours);
        vm.AverageResolutionHours = Average(resolutionHours);
        vm.MedianResolutionHours = Median(resolutionHours);

        vm.TopRequesters = BuildRequesters(opened, ratingByTicket);
        vm.Agents = BuildAgents(closed, ratingByTicket);
        vm.LowestRated = BuildLowestRated(supportTickets, ratings, lowestRatedCount);

        ResolveDisplayNames(vm);
        return vm;
    }

    private static List<SupportTicketRequesterStatVm> BuildRequesters(List<SupportTicket> opened,
        Dictionary<Guid, SupportTicketRating> ratingByTicket) =>
        opened
            .GroupBy(x => x.RequesterGuid != null
                ? x.RequesterGuid.Value.ToString()
                : (x.RequesterEmail ?? string.Empty).ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(group => new SupportTicketRequesterStatVm
            {
                RequesterGuid = group.First().RequesterGuid,
                RequesterEmail = group.First().RequesterEmail,
                RequesterName = group.First().RequesterName,
                SupportTicketCount = group.Count(),
                AverageRating = Average(group
                    .Where(x => ratingByTicket.ContainsKey(x.Guid))
                    .Select(x => (double)ratingByTicket[x.Guid].Score.Value))
            })
            .OrderByDescending(x => x.SupportTicketCount)
            .Take(20)
            .ToList();

    private static List<SupportTicketAgentStatVm> BuildAgents(List<SupportTicket> closed,
        Dictionary<Guid, SupportTicketRating> ratingByTicket) =>
        closed
            // A ticket the requester closed is not a desk closure - it belongs to nobody on the desk.
            .Where(x => x.ClosedByGuid != null && x.CloseReason != SupportTicketCloseReason.ClosedByRequester)
            .GroupBy(x => x.ClosedByGuid.Value)
            .Select(group => new SupportTicketAgentStatVm
            {
                AgentGuid = group.Key,
                ClosedCount = group.Count(),
                ArbitrarilyClosedCount = group.Count(x => x.CloseReason == SupportTicketCloseReason.Arbitrary),
                AverageFirstResponseHours = Average(group
                    .Where(x => x.FirstResponseUtc != null)
                    .Select(x => (x.FirstResponseUtc.Value - x.CreatedUtc).TotalHours)),
                AverageResolutionHours = Average(group
                    .Select(x => (x.ClosedUtc.Value - x.CreatedUtc).TotalHours)),
                AverageRating = Average(group
                    .Where(x => ratingByTicket.ContainsKey(x.Guid))
                    .Select(x => (double)ratingByTicket[x.Guid].Score.Value)),
                RatedCount = group.Count(x => ratingByTicket.ContainsKey(x.Guid))
            })
            .OrderByDescending(x => x.ClosedCount)
            .ToList();

    private static List<SupportTicketLowRatingVm> BuildLowestRated(List<SupportTicket> supportTickets,
        List<SupportTicketRating> ratings, int count)
    {
        var byGuid = supportTickets.ToDictionary(x => x.Guid);

        return ratings
            .Where(x => byGuid.ContainsKey(x.SupportTicketGuid))
            .OrderBy(x => x.Score)
            .ThenByDescending(x => x.RatedUtc)
            .Take(count < 1 ? 20 : count)
            .Select(rating =>
            {
                var supportTicket = byGuid[rating.SupportTicketGuid];
                return new SupportTicketLowRatingVm
                {
                    SupportTicketGuid = supportTicket.Guid,
                    Number = supportTicket.Number,
                    Subject = supportTicket.Subject,
                    RequesterName = supportTicket.RequesterName ?? supportTicket.RequesterEmail,
                    AgentGuid = rating.RatedAgentGuid ??
                                (supportTicket.CloseReason == SupportTicketCloseReason.ClosedByRequester
                                    ? null
                                    : supportTicket.ClosedByGuid),
                    Score = rating.Score.Value,
                    Comment = rating.Comment,
                    ClosedArbitrarily = supportTicket.CloseReason == SupportTicketCloseReason.Arbitrary,
                    ClosedUtc = supportTicket.ClosedUtc,
                    RatedUtc = rating.RatedUtc
                };
            })
            .ToList();
    }

    private void ResolveDisplayNames(SupportTicketStatisticsVm vm)
    {
        var guids = vm.Agents.Select(x => x.AgentGuid)
            .Concat(vm.LowestRated.Where(x => x.AgentGuid != null).Select(x => x.AgentGuid.Value))
            .Concat(vm.TopRequesters.Where(x => x.RequesterGuid != null).Select(x => x.RequesterGuid.Value))
            .Distinct()
            .ToList();

        if (guids.Count == 0)
            return;

        var names = _requesterResolver.ResolveNames(guids);

        foreach (var agent in vm.Agents)
            agent.AgentName = names.GetValueOrDefault(agent.AgentGuid);

        foreach (var row in vm.LowestRated.Where(x => x.AgentGuid != null))
            row.AgentName = names.GetValueOrDefault(row.AgentGuid.Value);

        foreach (var requester in vm.TopRequesters.Where(x => x.RequesterGuid != null))
            requester.RequesterName ??= names.GetValueOrDefault(requester.RequesterGuid.Value);
    }

    private static double? Average(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    private static double? Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return null;

        var sorted = values.OrderBy(x => x).ToList();
        var middle = sorted.Count / 2;

        var median = sorted.Count % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2;

        return Math.Round(median, 2);
    }
}
