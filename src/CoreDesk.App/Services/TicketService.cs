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
        // Start mit Demo-Tickets für verschiedene Filter-Tests
        CreateNewTicket("anna.meier@privat.com", "Problem mit Bestellung 100-58273", "Hallo, meine Lieferung ist noch nicht angekommen.", TicketStatus.Offen);
        CreateNewTicket("john.doe@business.com", "Anfrage zu Rechnung 9855", "Können Sie mir bitte eine Kopie der Rechnung zukommen lassen?", TicketStatus.InBearbeitung);
        CreateNewTicket("maria.garcia@privat.com", "Defekter Artikel 200-12345", "Das gelieferte Produkt funktioniert nicht richtig.", TicketStatus.Offen);
        CreateNewTicket("tech@innovate.corp", "Technische Unterstützung benötigt", "Wir benötigen Hilfe bei der Integration Ihrer API.", TicketStatus.InBearbeitung);
        CreateNewTicket("customer@retail.shop", "Bestellung 300-67890 fehlt", "Unsere Bestellung ist nicht vollständig angekommen.", TicketStatus.Gelöst);
        CreateNewTicket("peter.mueller@home.de", "Rückgabe möglich?", "Kann ich diesen Artikel zurückgeben?", TicketStatus.Gelöst);
        
        // Ältere Tickets für Datums-Filter
        CreateOlderTicket("support@acme.inc", "Wartungsvertrag verlängern", "Unser Wartungsvertrag läuft bald ab.", TicketStatus.Offen, DateTime.Now.AddDays(-14));
        CreateOlderTicket("anna.meier@privat.com", "Alte Anfrage 400-11111", "Eine ältere Anfrage.", TicketStatus.Gelöst, DateTime.Now.AddDays(-60));
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

    public Task UpdateTicketStatusAsync(int ticketId, TicketStatus newStatus)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            
            // Add status change update
            var statusUpdate = new TicketUpdate
            {
                Author = "System",
                Content = $"Status geändert von {oldStatus} zu {newStatus}",
                Timestamp = DateTime.Now,
                IsInternalNote = true
            };
            ticket.Updates.Add(statusUpdate);
        }
        return Task.CompletedTask;
    }

    private void CreateNewTicket(string from, string subject, string body, TicketStatus status = TicketStatus.Offen)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = from,
            Subject = subject,
            Status = status,
            CreatedAt = DateTime.Now,
            OrderId = ExtractOrderId(subject + " " + body)
        };
        ticket.Updates.Add(new TicketUpdate { Author = from, Content = body, Timestamp = DateTime.Now });
        _tickets.TryAdd(id, ticket);
    }

    private void CreateOlderTicket(string from, string subject, string body, TicketStatus status, DateTime createdAt)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = from,
            Subject = subject,
            Status = status,
            CreatedAt = createdAt,
            OrderId = ExtractOrderId(subject + " " + body)
        };
        ticket.Updates.Add(new TicketUpdate { Author = from, Content = body, Timestamp = createdAt });
        _tickets.TryAdd(id, ticket);
    }

    private string? ExtractOrderId(string text)
    {
        var match = Regex.Match(text, @"\b(\d{3}-\d{5,})\b"); // Sucht nach Format 100-58273
        return match.Success ? match.Value : null;
    }
}