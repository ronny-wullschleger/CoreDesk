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
        CreateNewTicket("customer@retail.shop", "Bestellung 300-67890 fehlt", "Unsere Bestellung ist nicht vollständig angekommen.", TicketStatus.Gelöst);
        CreateNewTicket("peter.mueller@home.de", "Rückgabe möglich?", "Kann ich diesen Artikel zurückgeben?", TicketStatus.Gelöst);
        
        // 2nd Level / Technical Support Tickets
        CreateNewTicket("tech@innovate.corp", "API Integration Problem", "Wir haben Probleme bei der Integration Ihrer REST API. Die Authentifizierung schlägt fehl.", TicketStatus.InBearbeitung, "2nd-Level", TicketPriority.Hoch);
        CreateNewTicket("developer@startup.io", "Webhook Fehler 500", "Unsere Webhooks erhalten ständig 500 Fehler. Bitte prüfen Sie die Serverlogik.", TicketStatus.Offen, "2nd-Level", TicketPriority.Kritisch);
        CreateNewTicket("admin@company.com", "Bulk Import Bug", "Der Bulk-Import von Produkten funktioniert nicht korrekt. Einige Felder werden nicht übernommen.", TicketStatus.Offen, "2nd-Level");
        CreateNewTicket("support@webshop.de", "Performance Issues", "Die API-Antwortzeiten sind sehr langsam geworden. Bitte untersuchen Sie das Problem.", TicketStatus.InBearbeitung, "2nd-Level", TicketPriority.Hoch);
        
        // After-Sales Tickets
        CreateNewTicket("kunde1@email.de", "Retoure Artikel 400-99876", "Ich möchte den Artikel 400-99876 zurücksenden. Er entspricht nicht der Beschreibung.", TicketStatus.Offen, "After-Sales");
        CreateNewTicket("shopping@family.com", "Umtausch defektes Produkt", "Das gelieferte Produkt ist defekt angekommen. Ich möchte es umtauschen.", TicketStatus.InBearbeitung, "After-Sales", TicketPriority.Normal);
        CreateNewTicket("buyer@home.net", "Garantiefall 500-11223", "Artikel 500-11223 ist nach 3 Monaten kaputt gegangen. Ist das ein Garantiefall?", TicketStatus.Offen, "After-Sales");
        CreateNewTicket("return@customer.org", "Rückgabe ohne Verpackung", "Kann ich einen Artikel auch ohne Originalverpackung zurückgeben?", TicketStatus.Gelöst, "After-Sales");
        
        // Financial Support Tickets
        CreateNewTicket("buchhaltung@firma.de", "Zahlungserinnerung Rechnung 7788", "Wir haben eine Mahnung erhalten, aber die Rechnung bereits bezahlt.", TicketStatus.Offen, "Finanzen");
        CreateNewTicket("finance@corporation.com", "Stornierung Bestellung 600-55443", "Wir möchten die Bestellung 600-55443 stornieren und eine Rückerstattung erhalten.", TicketStatus.InBearbeitung, "Finanzen");
        
        // Ältere Tickets für Datums-Filter
        CreateOlderTicket("support@acme.inc", "Wartungsvertrag verlängern", "Unser Wartungsvertrag läuft bald ab.", TicketStatus.Offen, DateTime.Now.AddDays(-14));
        CreateOlderTicket("anna.meier@privat.com", "Alte Anfrage 400-11111", "Eine ältere Anfrage.", TicketStatus.Gelöst, DateTime.Now.AddDays(-60));
        CreateOlderTicket("legacy@oldcustomer.com", "Alte technische Anfrage", "Probleme mit der alten API-Version.", TicketStatus.Geschlossen, DateTime.Now.AddDays(-90), "2nd-Level");
        
        // Assign some tickets to the current agent (agent1 - Sarah Weber) for testing "My Tickets" filter
        AssignTestTicketsToAgent();
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

    public Task AssignTicketToAgentAsync(int ticketId, string? agentId, string? reason = null)
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
                Content = agentId != null 
                    ? $"Ticket " + (oldAgent != null ? $"von Agent '{oldAgent}' " : "") + 
                      $"an Agent '{agentId}' zugewiesen" + 
                      (reason != null ? $". Grund: {reason}" : "")
                    : $"Ticket-Zuweisung von Agent '{oldAgent}' aufgehoben" + 
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
        }
        return Task.CompletedTask;
    }

    private void CreateNewTicket(string from, string subject, string body, TicketStatus status = TicketStatus.Offen, string team = "1st-Level", TicketPriority priority = TicketPriority.Normal)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = from,
            Subject = subject,
            Status = status,
            Priority = priority,
            CreatedAt = DateTime.Now,
            LastUpdated = DateTime.Now,
            AssignedToTeam = team,
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

    private void CreateOlderTicket(string from, string subject, string body, TicketStatus status, DateTime createdAt, string team = "1st-Level", TicketPriority priority = TicketPriority.Normal)
    {
        var id = Interlocked.Increment(ref _nextId);
        var ticket = new Ticket
        {
            Id = id,
            CustomerEmail = from,
            Subject = subject,
            Status = status,
            Priority = priority,
            CreatedAt = createdAt,
            LastUpdated = createdAt,
            AssignedToTeam = team,
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
    
    private void AssignTestTicketsToAgent()
    {
        // Assign some tickets to agent1 (Sarah Weber) for testing "My Tickets" filter
        var ticketsList = _tickets.Values.ToList();
        var ticketsToAssign = new[] { 1, 3, 5, 7, 9 }; // Assign every other ticket to agent1
        
        foreach (var ticketId in ticketsToAssign)
        {
            if (_tickets.TryGetValue(ticketId, out var ticket))
            {
                ticket.AssignedToAgent = "agent1";
                ticket.LastUpdated = DateTime.Now;
                
                // Add assignment update
                ticket.Updates.Add(new TicketUpdate
                {
                    Id = ticket.Updates.Count + 1,
                    Author = "System",
                    Content = "Ticket automatisch an Sarah Weber zugewiesen (Demo-Daten)",
                    Timestamp = DateTime.Now,
                    IsInternalNote = true,
                    Type = TicketUpdateType.AgentAssignment
                });
            }
        }
    }
}