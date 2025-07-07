// src/CoreDesk.App/Models/Ticket.cs

namespace CoreDesk.App.Models;

public class Ticket
{
    public int Id { get; set; }
    public string Subject { get; set; } = "No Subject";
    public string CustomerEmail { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string AssignedToTeam { get; set; } = "1st-Level"; // Default team
    public string? AssignedToAgent { get; set; }
    public List<TicketUpdate> Updates { get; set; } = new();
    public List<string> Tags { get; set; } = new(); // For automation categorization
}

public class TicketUpdate
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; }
    public TicketUpdateType Type { get; set; } = TicketUpdateType.Reply;
}

public enum TicketStatus 
{ 
    Offen, 
    InBearbeitung, 
    WartetAufKunde, 
    Eskaliert, 
    Gelöst, 
    Geschlossen 
}

public enum TicketPriority
{
    Niedrig,
    Normal,
    Hoch,
    Kritisch
}

public enum TicketUpdateType
{
    Reply,
    InternalNote,
    StatusChange,
    TeamAssignment,
    AgentAssignment
}

// Customer and Order models for ERP integration
public class Customer
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<Order> Orders { get; set; } = new();
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
}

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public string ShippingAddress { get; set; } = string.Empty;
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum CustomerType
{
    Privat,
    Business
}

public enum CustomerStatus
{
    Active,
    Inactive,
    Blocked
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Returned
}

// Team and Agent models
public class Team
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Agents { get; set; } = new();
    public List<string> Skills { get; set; } = new(); // For routing
}

public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<string> Skills { get; set; } = new();
}