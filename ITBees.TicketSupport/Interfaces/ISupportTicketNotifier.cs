using ITBees.TicketSupport.DbModels;

namespace ITBees.TicketSupport.Interfaces;

public interface ISupportTicketNotifier
{
    void TicketCreated(SupportTicket ticket);

    void RequesterReplied(SupportTicket ticket, SupportTicketMessage message);

    void TicketClosed(SupportTicket ticket);

    void LowRatingReceived(SupportTicket ticket, int score, string comment);
}
