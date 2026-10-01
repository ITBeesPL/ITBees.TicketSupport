namespace ITBees.TicketSupport.Interfaces;

/// <summary>Builds the links carried by the desk alerts.</summary>
public interface ISupportTicketLinkBuilder
{
    /// <summary>Deep link to the ticket in the desk panel. Null when the host configured no template.</summary>
    string BuildDeskTicketLink(long ticketNumber, Guid ticketGuid);
}
