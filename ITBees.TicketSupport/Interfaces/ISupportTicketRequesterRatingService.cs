using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketRequesterRatingService
{
    SupportTicketVm Create(SupportTicketRequesterRatingIm supportTicketRequesterRatingIm);
}
