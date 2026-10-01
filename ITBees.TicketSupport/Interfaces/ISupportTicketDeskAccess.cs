namespace ITBees.TicketSupport.Interfaces;

/// <summary>
/// The whole authorisation surface of the desk: is the current user support staff, or somebody who
/// reports problems? The host answers it, because only the host knows what its roles mean.
/// <para>
/// A requester needs no permission of their own - anybody signed in may raise a ticket and read
/// the tickets they raised, and nothing else.
/// </para>
/// </summary>
public interface ISupportTicketDeskAccess
{
    /// <summary>Throws when the current user is not support staff.</summary>
    void CheckDeskAccess();

    /// <summary>The same question without the exception, for view models and list filtering.</summary>
    bool IsDeskUser();
}
