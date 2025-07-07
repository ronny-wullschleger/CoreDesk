// src/CoreDesk.App/Services/AuthenticationService.cs

using CoreDesk.App.Models;

namespace CoreDesk.App.Services;

public class AuthenticationService
{
    private readonly TeamService _teamService;
    
    // For demo purposes, we'll simulate a logged-in agent
    private readonly Agent _currentAgent;

    public AuthenticationService(TeamService teamService)
    {
        _teamService = teamService;
        
        // Simulate logged-in agent (in real app, this would come from authentication)
        _currentAgent = _teamService.GetAgent("agent1") ?? new Agent 
        { 
            Id = "agent1", 
            Name = "Sarah Weber", 
            Email = "sarah.weber@coredesk.com", 
            Team = "1st-Level" 
        };
    }

    public Agent GetCurrentAgent()
    {
        return _currentAgent;
    }

    public bool IsCurrentAgent(string? agentId)
    {
        return _currentAgent.Id == agentId;
    }

    public string GetCurrentAgentName()
    {
        return _currentAgent.Name;
    }

    public string GetCurrentAgentId()
    {
        return _currentAgent.Id;
    }
}
