using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketRequesterClosureIm : Im
{
    public SupportTicketRequesterClosureIm() { }

    public Guid SupportTicketGuid { get; set; }

    /// <summary>Optional word from the requester on why the matter is settled; stored as the close note.</summary>
    public string CloseNote { get; set; }
}
