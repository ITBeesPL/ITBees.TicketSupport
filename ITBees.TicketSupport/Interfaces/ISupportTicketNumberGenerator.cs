namespace ITBees.TicketSupport.Interfaces;

/// <summary>
/// Produces the number a ticket is referred to by. Replaceable, because some hosts want their own
/// shape (a prefix, a per-year reset); the library only requires that it is unique and stable once
/// assigned.
/// </summary>
public interface ISupportTicketNumberGenerator
{
    long Next();
}
