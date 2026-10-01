using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketRequesterClosureService
{
    SupportTicketVm Create(SupportTicketRequesterClosureIm supportTicketRequesterClosureIm);
}
