namespace ITBees.TicketSupport.Interfaces;

/// <summary>
/// Turns an e-mail address into a user of the host application, and a user guid into a display
/// name. Support uses it when entering a ticket on somebody else's behalf (e.g. after a phone
/// call): a match links the ticket to that account, so the person also sees it in their panel.
/// </summary>
public interface ISupportTicketRequesterResolver
{
    /// <summary>Null when the address matches no account.</summary>
    SupportTicketPerson ResolveByEmail(string email);

    /// <summary>Null when the guid is unknown.</summary>
    SupportTicketPerson ResolveByGuid(Guid userGuid);

    /// <summary>
    /// Display names for a batch of user guids - agents on the statistics screen, authors in a
    /// conversation. Missing guids may simply be absent from the result.
    /// </summary>
    IReadOnlyDictionary<Guid, string> ResolveNames(IReadOnlyCollection<Guid> userGuids);
}

public class SupportTicketPerson
{
    public Guid Guid { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
}
