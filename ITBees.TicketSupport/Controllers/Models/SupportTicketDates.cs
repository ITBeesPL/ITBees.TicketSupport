namespace ITBees.TicketSupport.Controllers.Models;

internal static class SupportTicketDates
{
    // MySQL datetime columns do not retain DateTime.Kind. All ticket timestamps are stored in UTC.
    public static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public static DateTime? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
}
