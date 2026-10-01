using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.DbModels;
using RestVm = ITBees.RestClient.Interfaces.RestModelMarkup.Vm;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketMessageVm : RestVm
{
    public SupportTicketMessageVm() { }

    public SupportTicketMessageVm(SupportTicketMessage message)
    {
        Guid = message.Guid;
        SupportTicketGuid = message.SupportTicketGuid;
        Direction = message.Direction;
        AuthorGuid = message.AuthorGuid;
        AuthorName = message.AuthorName;
        AuthorEmail = message.AuthorEmail;
        Body = message.Body;
        BodyHtml = message.BodyHtml;
        IsPublic = message.IsPublic;
        CreatedUtc = SupportTicketDates.Utc(message.CreatedUtc);
    }

    public Guid Guid { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public SupportTicketMessageDirection Direction { get; set; }
    public Guid? AuthorGuid { get; set; }
    public string AuthorName { get; set; }
    public string AuthorEmail { get; set; }
    public string Body { get; set; }
    public string BodyHtml { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedUtc { get; set; }
    public List<SupportTicketAttachmentVm> Attachments { get; set; } = new();
}

public class SupportTicketAttachmentVm : RestVm
{
    public SupportTicketAttachmentVm() { }

    public SupportTicketAttachmentVm(SupportTicketAttachment attachment)
    {
        Guid = attachment.Guid;
        SupportTicketGuid = attachment.SupportTicketGuid;
        SupportTicketMessageGuid = attachment.SupportTicketMessageGuid;
        FileName = attachment.FileName;
        ContentType = attachment.ContentType;
        SizeBytes = attachment.SizeBytes;
        StorageKey = attachment.StorageKey;
        CreatedUtc = SupportTicketDates.Utc(attachment.CreatedUtc);
    }

    public Guid Guid { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public Guid? SupportTicketMessageGuid { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>Whatever the host attachment store returned; the panel resolves it to a URL.</summary>
    public string StorageKey { get; set; }

    public DateTime CreatedUtc { get; set; }
}

public class SupportTicketEventVm : RestVm
{
    public SupportTicketEventVm() { }

    public SupportTicketEventVm(SupportTicketEvent supportTicketEvent)
    {
        Guid = supportTicketEvent.Guid;
        SupportTicketGuid = supportTicketEvent.SupportTicketGuid;
        EventType = supportTicketEvent.EventType;
        FromValue = supportTicketEvent.FromValue;
        ToValue = supportTicketEvent.ToValue;
        ActorGuid = supportTicketEvent.ActorGuid;
        ActorName = supportTicketEvent.ActorName;
        CreatedUtc = SupportTicketDates.Utc(supportTicketEvent.CreatedUtc);
    }

    public Guid Guid { get; set; }
    public Guid SupportTicketGuid { get; set; }
    public string EventType { get; set; }
    public string FromValue { get; set; }
    public string ToValue { get; set; }
    public Guid? ActorGuid { get; set; }
    public string ActorName { get; set; }
    public DateTime CreatedUtc { get; set; }
}
