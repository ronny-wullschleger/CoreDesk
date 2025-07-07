// src/CoreDesk.App/Services/AutomationService.cs

using CoreDesk.App.Models;
using System.Text.RegularExpressions;

namespace CoreDesk.App.Services;

public class AutomationService
{
    private readonly MockErpService _erpService;
    private readonly List<AutomationRule> _rules;

    public AutomationService(MockErpService erpService)
    {
        _erpService = erpService;
        _rules = InitializeRules();
    }

    private List<AutomationRule> InitializeRules()
    {
        return new List<AutomationRule>
        {
            // Team Assignment Rules
            new AutomationRule
            {
                Name = "Finance Team Assignment",
                Condition = ticket => ContainsFinanceKeywords(ticket.Subject + " " + GetFirstUpdate(ticket)),
                Action = ticket => { ticket.AssignedToTeam = "Finanzen"; ticket.Tags.Add("Finance"); }
            },
            new AutomationRule
            {
                Name = "After-Sales Team Assignment",
                Condition = ticket => ContainsAfterSalesKeywords(ticket.Subject + " " + GetFirstUpdate(ticket)),
                Action = ticket => { ticket.AssignedToTeam = "After-Sales"; ticket.Tags.Add("Return"); }
            },
            new AutomationRule
            {
                Name = "2nd-Level Escalation",
                Condition = ticket => ContainsTechnicalKeywords(ticket.Subject + " " + GetFirstUpdate(ticket)),
                Action = ticket => { ticket.AssignedToTeam = "2nd-Level"; ticket.Tags.Add("Technical"); }
            },
            
            // Priority Rules
            new AutomationRule
            {
                Name = "Business Customer High Priority",
                Condition = ticket => IsBusinessCustomer(ticket.CustomerEmail),
                Action = ticket => { ticket.Priority = TicketPriority.Hoch; ticket.Tags.Add("Business-Customer"); }
            },
            new AutomationRule
            {
                Name = "Critical Keywords",
                Condition = ticket => ContainsCriticalKeywords(ticket.Subject + " " + GetFirstUpdate(ticket)),
                Action = ticket => { ticket.Priority = TicketPriority.Kritisch; ticket.Tags.Add("Critical"); }
            },
            
            // Status Rules
            new AutomationRule
            {
                Name = "Order Issue Auto-Processing",
                Condition = ticket => !string.IsNullOrEmpty(ticket.OrderId),
                Action = ticket => { ticket.Status = TicketStatus.InBearbeitung; ticket.Tags.Add("Order-Related"); }
            }
        };
    }

    public async Task<Ticket> ProcessNewTicketAsync(Ticket ticket)
    {
        // Extract order ID from subject and content
        var firstUpdate = GetFirstUpdate(ticket);
        var extractedOrderId = await _erpService.ExtractOrderIdFromTextAsync(ticket.Subject + " " + firstUpdate);
        if (!string.IsNullOrEmpty(extractedOrderId))
        {
            ticket.OrderId = extractedOrderId;
        }

        // Apply automation rules
        foreach (var rule in _rules)
        {
            try
            {
                if (rule.Condition(ticket))
                {
                    rule.Action(ticket);
                    
                    // Log the automation action
                    var automationUpdate = new TicketUpdate
                    {
                        Author = "Automation",
                        Content = $"Automatisch verarbeitet: {rule.Name}",
                        Timestamp = DateTime.Now,
                        IsInternalNote = true,
                        Type = TicketUpdateType.InternalNote
                    };
                    ticket.Updates.Add(automationUpdate);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the ticket creation
                Console.WriteLine($"Automation rule '{rule.Name}' failed: {ex.Message}");
            }
        }

        // Set last updated
        ticket.LastUpdated = DateTime.Now;

        return ticket;
    }

    public Task<Ticket> ProcessTicketUpdateAsync(Ticket ticket, TicketUpdate update)
    {
        // Auto-status changes based on update type
        if (!update.IsInternalNote)
        {
            if (update.Author == ticket.CustomerEmail)
            {
                // Customer replied
                if (ticket.Status == TicketStatus.WartetAufKunde)
                {
                    ticket.Status = TicketStatus.Offen;
                    
                    var statusUpdate = new TicketUpdate
                    {
                        Author = "Automation",
                        Content = "Status automatisch auf 'Offen' gesetzt (Kundenantwort erhalten)",
                        Timestamp = DateTime.Now,
                        IsInternalNote = true,
                        Type = TicketUpdateType.StatusChange
                    };
                    ticket.Updates.Add(statusUpdate);
                }
            }
            else
            {
                // Agent replied
                if (ticket.Status == TicketStatus.Offen)
                {
                    ticket.Status = TicketStatus.WartetAufKunde;
                    
                    var statusUpdate = new TicketUpdate
                    {
                        Author = "Automation",
                        Content = "Status automatisch auf 'Wartet auf Kunde' gesetzt (Agent-Antwort)",
                        Timestamp = DateTime.Now,
                        IsInternalNote = true,
                        Type = TicketUpdateType.StatusChange
                    };
                    ticket.Updates.Add(statusUpdate);
                }
            }
        }

        ticket.LastUpdated = DateTime.Now;
        return Task.FromResult(ticket);
    }

    private string GetFirstUpdate(Ticket ticket)
    {
        return ticket.Updates.FirstOrDefault()?.Content ?? "";
    }

    private bool IsBusinessCustomer(string email)
    {
        // This would typically be cached for performance
        var task = _erpService.IsBusinessCustomerAsync(email);
        task.Wait();
        return task.Result;
    }

    private bool ContainsFinanceKeywords(string text)
    {
        var keywords = new[] { "rechnung", "mahnung", "zahlung", "bezahlung", "invoice", "payment", "billing" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsAfterSalesKeywords(string text)
    {
        var keywords = new[] { "retoure", "rücksendung", "umtausch", "rückgabe", "return", "exchange", "refund", "defekt" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsTechnicalKeywords(string text)
    {
        var keywords = new[] { "api", "integration", "technisch", "technical", "bug", "error", "problem", "installation" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool ContainsCriticalKeywords(string text)
    {
        var keywords = new[] { "urgent", "kritisch", "sofort", "notfall", "emergency", "critical", "asap" };
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

public class AutomationRule
{
    public string Name { get; set; } = string.Empty;
    public Func<Ticket, bool> Condition { get; set; } = _ => false;
    public Action<Ticket> Action { get; set; } = _ => { };
}
