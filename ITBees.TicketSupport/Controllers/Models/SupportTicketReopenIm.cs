using System;
using ITBees.RestClient.Interfaces.RestModelMarkup;

namespace ITBees.TicketSupport.Controllers.Models;

public class SupportTicketReopenIm : Im
{
	public Guid SupportTicketGuid { get; set; }

	public string Message { get; set; }
}
