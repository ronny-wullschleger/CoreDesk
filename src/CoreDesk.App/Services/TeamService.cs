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

    public List<Team> GetAllTeams()
    {
        return _teams.Values.ToList();
    }

    public Team? GetTeam(string teamName)
    {
        _teams.TryGetValue(teamName, out var team);
        return team;
    }

    public List<Agent> GetTeamAgents(string teamName)
    {
        if (_teams.TryGetValue(teamName, out var team))
        {
            var agents = team.Agents.Select(agentId => _agents.TryGetValue(agentId, out var agent) ? agent : null)
                                   .Where(a => a != null)
                                   .Cast<Agent>()
                                   .ToList();
            return agents;
        }
        return new List<Agent>();
    }

    public Agent? GetAgent(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return agent;
    }

    public List<Agent> GetAllAgents()
    {
        return _agents.Values.ToList();
    }

    public string SuggestTeamForTicket(Ticket ticket)
    {
        var content = ticket.Subject + " " + (ticket.Updates.FirstOrDefault()?.Content ?? "");
        
        // Check for finance keywords
        if (ContainsFinanceKeywords(content))
        {
            return "Finanzen";
        }
        
        // Check for after-sales keywords
        if (ContainsAfterSalesKeywords(content))
        {
            return "After-Sales";
        }
        
        // Check for technical keywords
        if (ContainsTechnicalKeywords(content))
        {
            return "2nd-Level";
        }
        
        // Default to 1st-Level
        return "1st-Level";
    }

    public string? SuggestAgentForTicket(Ticket ticket)
    {
        var suggestedTeam = SuggestTeamForTicket(ticket);
        var teamAgents = GetTeamAgents(suggestedTeam);
        
        if (teamAgents.Any())
        {
            // Simple round-robin assignment (in real app, consider workload)
            var selectedAgent = teamAgents[ticket.Id % teamAgents.Count];
            return selectedAgent.Id;
        }
        
        return null;
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
