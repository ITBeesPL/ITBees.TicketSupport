using ITBees.TicketSupport.Controllers.Models;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketContextsService
{
    IReadOnlyCollection<SupportTicketContextVm> GetAll();
}
