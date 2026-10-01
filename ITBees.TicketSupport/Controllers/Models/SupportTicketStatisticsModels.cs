using RestVm = ITBees.RestClient.Interfaces.RestModelMarkup.Vm;

namespace ITBees.TicketSupport.Controllers.Models;

/// <summary>
/// The statistics screen in one payload: how many tickets, how fast they are answered and closed,
/// who reports the most, who closes the most, and which closed tickets were graded badly.
/// </summary>
public class SupportTicketStatisticsVm : RestVm
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }

    public int OpenedCount { get; set; }
    public int ClosedCount { get; set; }
    public int CurrentlyOpenCount { get; set; }
    public int UnassignedCount { get; set; }
    public int ArbitrarilyClosedCount { get; set; }

    public double? AverageFirstResponseHours { get; set; }
    public double? MedianFirstResponseHours { get; set; }
    public double? AverageResolutionHours { get; set; }
    public double? MedianResolutionHours { get; set; }
    public double? AverageRating { get; set; }
    public int RatedCount { get; set; }

    /// <summary>Who reports the most, most first.</summary>
    public List<SupportTicketRequesterStatVm> TopRequesters { get; set; } = new();

    /// <summary>One row per support person who closed at least one ticket in the period.</summary>
    public List<SupportTicketAgentStatVm> Agents { get; set; } = new();

    /// <summary>Recently closed tickets the requester graded poorly - the review queue.</summary>
    public List<SupportTicketLowRatingVm> LowestRated { get; set; } = new();
}

public class SupportTicketRequesterStatVm : RestVm
{
    public Guid? RequesterGuid { get; set; }
    public string RequesterEmail { get; set; }
    public string RequesterName { get; set; }
    public int SupportTicketCount { get; set; }
    public double? AverageRating { get; set; }
}

public class SupportTicketAgentStatVm : RestVm
{
    public Guid AgentGuid { get; set; }
    public string AgentName { get; set; }
    public int ClosedCount { get; set; }
    public int ArbitrarilyClosedCount { get; set; }
    public double? AverageFirstResponseHours { get; set; }
    public double? AverageResolutionHours { get; set; }
    public double? AverageRating { get; set; }
    public int RatedCount { get; set; }
}

public class SupportTicketLowRatingVm : RestVm
{
    public Guid SupportTicketGuid { get; set; }
    public long Number { get; set; }
    public string Subject { get; set; }
    public string RequesterName { get; set; }
    public Guid? AgentGuid { get; set; }
    public string AgentName { get; set; }
    public int Score { get; set; }
    public string Comment { get; set; }
    public bool ClosedArbitrarily { get; set; }
    public DateTime? ClosedUtc { get; set; }
    public DateTime? RatedUtc { get; set; }
}
