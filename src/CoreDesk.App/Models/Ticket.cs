// src/CoreDesk.App/Models/Ticket.cs

namespace CoreDesk.App.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Subject { get; set; } = "No Subject";
    public string CustomerEmail { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TicketUpdate> Updates { get; set; } = new();
}

public class TicketUpdate
{
    public DateTime Timestamp { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; }
}

public enum TicketStatus { Offen, InBearbeitung, Gelöst }