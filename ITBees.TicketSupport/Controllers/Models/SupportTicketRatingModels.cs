using ITBees.TicketSupport.DbModels;
using RestVm = ITBees.RestClient.Interfaces.RestModelMarkup.Vm;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketRatingVm : RestVm
{
    public SupportTicketRatingVm() { }

    public SupportTicketRatingVm(SupportTicketRating rating)
    {
        Guid = rating.Guid;
        SupportTicketGuid = rating.SupportTicketGuid;
        Score = rating.Score;
        Comment = rating.Comment;
        RatedAgentGuid = rating.RatedAgentGuid;
        RequestedUtc = SupportTicketDates.Utc(rating.RequestedUtc);
        RatedUtc = SupportTicketDates.Utc(rating.RatedUtc);
    }

    public Guid Guid { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public int? Score { get; set; }
    public string Comment { get; set; }
    public Guid? RatedAgentGuid { get; set; }
    public string RatedAgentName { get; set; }
    public DateTime RequestedUtc { get; set; }
    public DateTime? RatedUtc { get; set; }
}
