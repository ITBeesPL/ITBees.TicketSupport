namespace ITBees.TicketSupport.Abstractions;

public static class SupportTicketContentLimits
{
    public const int Subject = 300;

    public const int Body = 20000;

    public const int BodyHtml = 8388608;

    public const int Email = 320;

    public const int PersonName = 200;

    public const int Number = 32;

    public const int Key = 128;

    public const int ContextType = 64;

    public const int ContextName = 300;

    public const int ReferenceType = 64;

    public const int ReferenceId = 128;

    public const int RatingComment = 2000;

    public const int Link = 500;

    public const int FileName = 300;

    public const int StorageKey = 500;
}
