// src/CoreDesk.App/Services/TicketService.cs

using CoreDesk.App.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace CoreDesk.App.Services;

public class TicketService
{
    private readonly ConcurrentDictionary<int, Ticket> _tickets = new();
    private int _nextId = 1;

    public TicketService()
    {
        // Start mit einem Demoticket
        CreateNewTicket("anna.meier@privat.com", "Problem mit Bestellung 100-58273", "Hallo, meine Lieferung ist noch nicht angekommen.");
        CreateNewTicket("john.doe@business.com", "Anfrage zu Rechnung 9855", "Können Sie mir bitte eine Kopie der Rechnung zukommen lassen?");
    }
    
    public Task<List<Ticket>> GetAllTicketsAsync()
    {
        return Task.FromResult(_tickets.Values.OrderByDescending(t => t.CreatedAt).ToList());
    }

    public Task<Ticket?> GetTicketByIdAsync(int id)
    {
        _tickets.TryGetValue(id, out var ticket);
        return Task.FromResult(ticket);
    }

    public Task AddTicketUpdateAsync(int ticketId, TicketUpdate update)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            ticket.Updates.Add(update);
            ticket.Status = TicketStatus.InBearbeitung;
        }
        return Task.CompletedTask;
    }

    private void CreateNewTicket(string from, string subject, string body)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = from,
            Subject = subject,
            Status = TicketStatus.Offen,
            CreatedAt = DateTime.Now,
            OrderId = ExtractOrderId(subject + " " + body)
        };
        ticket.Updates.Add(new TicketUpdate { Author = from, Content = body, Timestamp = DateTime.Now });
        _tickets.TryAdd(id, ticket);
    }

    private string? ExtractOrderId(string text)
    {
        var match = Regex.Match(text, @"\b(\d{3}-\d{5,})\b"); // Sucht nach Format 100-58273
        return match.Success ? match.Value : null;
    }
}