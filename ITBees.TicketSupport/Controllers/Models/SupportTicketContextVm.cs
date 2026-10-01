using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketContextVm : Vm
{
    public SupportTicketContextVm() { }
    public SupportTicketContextVm(string type, Guid guid, string name)
    {
        Type = type;
        Guid = guid;
        Name = name;
    }

    public string Type { get; set; }
    public Guid Guid { get; set; }
    public string Name { get; set; }
}
