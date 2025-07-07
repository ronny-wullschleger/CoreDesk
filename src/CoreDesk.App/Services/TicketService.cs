// src/CoreDesk.App/Services/TicketService.cs

using CoreDesk.App.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace CoreDesk.App.Services;

public class TicketService
{
    private readonly ConcurrentDictionary<int, Ticket> _tickets = new();
    private int _nextId = 1;
    private readonly AutomationService? _automationService;

    public TicketService(AutomationService? automationService = null)
    {
        _automationService = automationService;
        
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

    public Task<List<Ticket>> GetTicketsByTeamAsync(string teamName)
    {
        var tickets = _tickets.Values.Where(t => t.AssignedToTeam == teamName).OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult(tickets);
    }

    public Task<List<Ticket>> GetTicketsByAgentAsync(string agentId)
    {
        var tickets = _tickets.Values.Where(t => t.AssignedToAgent == agentId).OrderByDescending(t => t.CreatedAt).ToList();
        return Task.FromResult(tickets);
    }

    public async Task<int> CreateTicketAsync(string customerEmail, string subject, string content)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = customerEmail,
            Subject = subject,
            Status = TicketStatus.Offen,
            CreatedAt = DateTime.Now,
            LastUpdated = DateTime.Now,
            AssignedToTeam = "1st-Level" // Default
        };
        
        // Add initial customer message
        ticket.Updates.Add(new TicketUpdate 
        { 
            Author = customerEmail, 
            Content = content, 
            Timestamp = DateTime.Now,
            Type = TicketUpdateType.Reply
        });

        // Apply automation if available
        if (_automationService != null)
        {
            ticket = await _automationService.ProcessNewTicketAsync(ticket);
        }

        _tickets.TryAdd(id, ticket);
        return id;
    }

    public async Task AddTicketUpdateAsync(int ticketId, TicketUpdate update)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            update.Id = ticket.Updates.Count + 1;
            ticket.Updates.Add(update);
            
            // Apply automation if available
            if (_automationService != null)
            {
                await _automationService.ProcessTicketUpdateAsync(ticket, update);
            }
            else
            {
                // Fallback logic if no automation service
                if (!update.IsInternalNote)
                {
                    ticket.Status = TicketStatus.InBearbeitung;
                }
                ticket.LastUpdated = DateTime.Now;
            }
        }
    }

    public Task UpdateTicketStatusAsync(int ticketId, TicketStatus newStatus)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var oldStatus = ticket.Status;
            ticket.Status = newStatus;
            ticket.LastUpdated = DateTime.Now;
            
            // Add status change update - system messages are not internal notes
            var statusUpdate = new TicketUpdate
            {
                Id = ticket.Updates.Count + 1,
                Author = "System",
                Content = $"Status geändert von {oldStatus} zu {newStatus}",
                Timestamp = DateTime.Now,
                IsInternalNote = false,
                Type = TicketUpdateType.StatusChange
            };
            ticket.Updates.Add(statusUpdate);
        }
        return Task.CompletedTask;
    }

    public Task AssignTicketToTeamAsync(int ticketId, string teamName, string? reason = null)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var oldTeam = ticket.AssignedToTeam;
            ticket.AssignedToTeam = teamName;
            ticket.AssignedToAgent = null; // Reset agent assignment
            ticket.LastUpdated = DateTime.Now;
            
            var assignmentUpdate = new TicketUpdate
            {
                Id = ticket.Updates.Count + 1,
                Author = "System",
                Content = $"Ticket von Team '{oldTeam}' an Team '{teamName}' übertragen" + 
                         (reason != null ? $". Grund: {reason}" : ""),
                Timestamp = DateTime.Now,
                IsInternalNote = true,
                Type = TicketUpdateType.TeamAssignment
            };
            ticket.Updates.Add(assignmentUpdate);
        }
        return Task.CompletedTask;
    }

    public Task AssignTicketToAgentAsync(int ticketId, string agentId, string? reason = null)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var oldAgent = ticket.AssignedToAgent;
            ticket.AssignedToAgent = agentId;
            ticket.LastUpdated = DateTime.Now;
            
            var assignmentUpdate = new TicketUpdate
            {
                Id = ticket.Updates.Count + 1,
                Author = "System",
                Content = $"Ticket " + (oldAgent != null ? $"von Agent '{oldAgent}' " : "") + 
                         $"an Agent '{agentId}' zugewiesen" + 
                         (reason != null ? $". Grund: {reason}" : ""),
                Timestamp = DateTime.Now,
                IsInternalNote = true,
                Type = TicketUpdateType.AgentAssignment
            };
            ticket.Updates.Add(assignmentUpdate);
        }
        return Task.CompletedTask;
    }

    public Task SetTicketPriorityAsync(int ticketId, TicketPriority priority, string? reason = null)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var oldPriority = ticket.Priority;
            ticket.Priority = priority;
            ticket.LastUpdated = DateTime.Now;
            
            var priorityUpdate = new TicketUpdate
            {
                Id = ticket.Updates.Count + 1,
                Author = "System",
                Content = $"Priorität von {oldPriority} auf {priority} geändert" + 
                         (reason != null ? $". Grund: {reason}" : ""),
                Timestamp = DateTime.Now,
                IsInternalNote = true,
                Type = TicketUpdateType.InternalNote
            };
            ticket.Updates.Add(priorityUpdate);
        }
        return Task.CompletedTask;
    }

    // For testing internal notes
    public Task AddTestInternalNoteAsync(int ticketId)
    {
        if (_tickets.TryGetValue(ticketId, out var ticket))
        {
            var internalUpdate = new TicketUpdate
            {
                Id = ticket.Updates.Count + 1,
                Author = "Test Support",
                Content = "Dies ist eine TEST INTERNE NOTIZ. Sollte nur für das Support-Team sichtbar sein und in LILA angezeigt werden.",
                Timestamp = DateTime.Now,
                IsInternalNote = true,
                Type = TicketUpdateType.InternalNote
            };
            
            ticket.Updates.Add(internalUpdate);
            
            // Add a debugging log to check the note was added correctly
            Console.WriteLine($"Added internal note to ticket {ticketId}: IsInternalNote={internalUpdate.IsInternalNote}");
            
            // Log all updates for this ticket
            foreach (var update in ticket.Updates)
            {
                Console.WriteLine($"Ticket {ticketId} update: Author={update.Author}, IsInternal={update.IsInternalNote}, Content={update.Content.Substring(0, Math.Min(20, update.Content.Length))}...");
            }
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
            LastUpdated = DateTime.Now,
            AssignedToTeam = "1st-Level",
            OrderId = ExtractOrderId(subject + " " + body)
        };
        ticket.Updates.Add(new TicketUpdate 
        { 
            Id = 1,
            Author = from, 
            Content = body, 
            Timestamp = DateTime.Now,
            Type = TicketUpdateType.Reply
        });
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
            LastUpdated = createdAt,
            AssignedToTeam = "1st-Level",
            OrderId = ExtractOrderId(subject + " " + body)
        };
        ticket.Updates.Add(new TicketUpdate 
        { 
            Id = 1,
            Author = from, 
            Content = body, 
            Timestamp = createdAt,
            Type = TicketUpdateType.Reply
        });
        _tickets.TryAdd(id, ticket);
    }

    private string? ExtractOrderId(string text)
    {
        var match = Regex.Match(text, @"\b(\d{3}-\d{5,})\b"); // Sucht nach Format 100-58273
        return match.Success ? match.Value : null;
    }
}