using ITBees.TicketSupport.DbModels;

namespace ITBees.TicketSupport.Interfaces;

public sealed class NullSupportTicketNotifier : ISupportTicketNotifier
{
    public void TicketCreated(SupportTicket ticket)
    {
    }

    public void RequesterReplied(SupportTicket ticket, SupportTicketMessage message)
    {
    }

    public void TicketClosed(SupportTicket ticket)
    {
    }

    public void LowRatingReceived(SupportTicket ticket, int score, string comment)
    {
    }
}
