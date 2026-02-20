using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

public class TeamConfigurationService
{
    public virtual List<Agent> CreateAllAgents()
    {
        var agents = new List<Agent>();

        // Team A — Day shift
        agents.Add(new Agent { Name = "A-Lead", Seniority = Seniority.TeamLead, TeamName = "TeamA", Shift = ShiftType.Day });
        agents.Add(new Agent { Name = "A-Mid1", Seniority = Seniority.MidLevel, TeamName = "TeamA", Shift = ShiftType.Day });
        agents.Add(new Agent { Name = "A-Mid2", Seniority = Seniority.MidLevel, TeamName = "TeamA", Shift = ShiftType.Day });
        agents.Add(new Agent { Name = "A-Jnr",  Seniority = Seniority.Junior,   TeamName = "TeamA", Shift = ShiftType.Day });

        // Team B — Afternoon shift
        agents.Add(new Agent { Name = "B-Snr",  Seniority = Seniority.Senior,   TeamName = "TeamB", Shift = ShiftType.Afternoon });
        agents.Add(new Agent { Name = "B-Mid",  Seniority = Seniority.MidLevel, TeamName = "TeamB", Shift = ShiftType.Afternoon });
        agents.Add(new Agent { Name = "B-Jnr1", Seniority = Seniority.Junior,   TeamName = "TeamB", Shift = ShiftType.Afternoon });
        agents.Add(new Agent { Name = "B-Jnr2", Seniority = Seniority.Junior,   TeamName = "TeamB", Shift = ShiftType.Afternoon });

        // Team C — Night shift
        agents.Add(new Agent { Name = "C-Mid1", Seniority = Seniority.MidLevel, TeamName = "TeamC", Shift = ShiftType.Night });
        agents.Add(new Agent { Name = "C-Mid2", Seniority = Seniority.MidLevel, TeamName = "TeamC", Shift = ShiftType.Night });

        // Overflow — all treated as Junior
        for (int i = 1; i <= 6; i++)
        {
            agents.Add(new Agent
            {
                Name = $"Overflow-{i}",
                Seniority = Seniority.Junior,
                TeamName = "Overflow",
                IsOverflow = true,
                Shift = ShiftType.Day // Available during office hours only
            });
        }

        return agents;
    }
}