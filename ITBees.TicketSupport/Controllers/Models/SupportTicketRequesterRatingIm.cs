using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketRequesterRatingIm : Im
{
    public Guid SupportTicketGuid { get; set; }
    public int Score { get; set; }
    public string Comment { get; set; }
}
