// src/CoreDesk.App/Services/TeamService.cs

using CoreDesk.App.Models;

namespace CoreDesk.App.Services;

public class TeamService
{
    private readonly Dictionary<string, Team> _teams;
    private readonly Dictionary<string, Agent> _agents;

    public TeamService()
    {
        _teams = new Dictionary<string, Team>();
        _agents = new Dictionary<string, Agent>();
        InitializeTeamsAndAgents();
    }

    private void InitializeTeamsAndAgents()
    {
        // Initialize teams
        var teams = new List<Team>
        {
            new Team
            {
                Name = "1st-Level",
                Description = "First Level Support - Allgemeine Anfragen",
                Skills = new List<string> { "Allgemein", "Bestellungen", "Versand" }
            },
            new Team
            {
                Name = "2nd-Level",
                Description = "Second Level Support - Technische Probleme",
                Skills = new List<string> { "Technisch", "API", "Integration", "Bugs" }
            },
            new Team
            {
                Name = "Finanzen",
                Description = "Financial Support - Rechnungen und Zahlungen",
                Skills = new List<string> { "Rechnung", "Zahlung", "Mahnung", "Buchhaltung" }
            },
            new Team
            {
                Name = "After-Sales",
                Description = "After-Sales Support - Retouren und Umtausch",
                Skills = new List<string> { "Retoure", "Umtausch", "Rückgabe", "Garantie" }
            }
        };

        // Initialize agents
        var agents = new List<Agent>
        {
            new Agent { Id = "agent1", Name = "Sarah Weber", Email = "sarah.weber@coredesk.com", Team = "1st-Level", Skills = new List<string> { "Allgemein", "Bestellungen" } },
            new Agent { Id = "agent2", Name = "Michael Schmidt", Email = "michael.schmidt@coredesk.com", Team = "1st-Level", Skills = new List<string> { "Allgemein", "Versand" } },
            new Agent { Id = "agent3", Name = "Lisa Müller", Email = "lisa.mueller@coredesk.com", Team = "2nd-Level", Skills = new List<string> { "Technisch", "API" } },
            new Agent { Id = "agent4", Name = "Thomas Klein", Email = "thomas.klein@coredesk.com", Team = "2nd-Level", Skills = new List<string> { "Integration", "Bugs" } },
            new Agent { Id = "agent5", Name = "Julia Fischer", Email = "julia.fischer@coredesk.com", Team = "Finanzen", Skills = new List<string> { "Rechnung", "Zahlung" } },
            new Agent { Id = "agent6", Name = "Robert Wagner", Email = "robert.wagner@coredesk.com", Team = "After-Sales", Skills = new List<string> { "Retoure", "Garantie" } }
        };

        // Add agents to teams
        foreach (var agent in agents)
        {
            _agents[agent.Id] = agent;
            var team = teams.FirstOrDefault(t => t.Name == agent.Team);
            if (team != null)
            {
                team.Agents.Add(agent.Id);
            }
        }

        // Add teams to dictionary
        foreach (var team in teams)
        {
            _teams[team.Name] = team;
        }
    }

    public Task<List<Team>> GetAllTeamsAsync()
    {
        return Task.FromResult(_teams.Values.ToList());
    }

    public Task<Team?> GetTeamAsync(string teamName)
    {
        _teams.TryGetValue(teamName, out var team);
        return Task.FromResult(team);
    }

    public Task<List<Agent>> GetTeamAgentsAsync(string teamName)
    {
        if (_teams.TryGetValue(teamName, out var team))
        {
            var agents = team.Agents.Select(agentId => _agents.TryGetValue(agentId, out var agent) ? agent : null)
                                   .Where(a => a != null)
                                   .Cast<Agent>()
                                   .ToList();
            return Task.FromResult(agents);
        }
        return Task.FromResult(new List<Agent>());
    }

    public Task<Agent?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<List<Agent>> GetAllAgentsAsync()
    {
        return Task.FromResult(_agents.Values.ToList());
    }

    public Task<string> SuggestTeamForTicketAsync(Ticket ticket)
    {
        var content = ticket.Subject + " " + (ticket.Updates.FirstOrDefault()?.Content ?? "");
        
        // Check for finance keywords
        if (ContainsFinanceKeywords(content))
        {
            return Task.FromResult("Finanzen");
        }
        
        // Check for after-sales keywords
        if (ContainsAfterSalesKeywords(content))
        {
            return Task.FromResult("After-Sales");
        }
        
        // Check for technical keywords
        if (ContainsTechnicalKeywords(content))
        {
            return Task.FromResult("2nd-Level");
        }
        
        // Default to 1st-Level
        return Task.FromResult("1st-Level");
    }

    public Task<string?> SuggestAgentForTicketAsync(Ticket ticket)
    {
        var suggestedTeam = SuggestTeamForTicketAsync(ticket).Result;
        var teamAgents = GetTeamAgentsAsync(suggestedTeam).Result;
        
        if (teamAgents.Any())
        {
            // Simple round-robin assignment (in real app, consider workload)
            var randomIndex = new Random().Next(teamAgents.Count);
            return Task.FromResult<string?>(teamAgents[randomIndex].Id);
        }
        
        return Task.FromResult<string?>(null);
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
}
