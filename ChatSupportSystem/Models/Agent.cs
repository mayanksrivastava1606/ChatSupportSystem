namespace ChatSupportSystem.Models;

public class Agent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Seniority Seniority { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public bool IsOverflow { get; set; }
    public ShiftType Shift { get; set; }
    public bool IsShiftOver { get; set; }

    private readonly List<Guid> _activeSessions = [];
    public IReadOnlyList<Guid> ActiveSessions => _activeSessions.AsReadOnly();

    public int MaxConcurrency => (int)(10 * SeniorityMultiplier);

    public double SeniorityMultiplier => Seniority switch
    {
        Seniority.Junior => 0.4,
        Seniority.MidLevel => 0.6,
        Seniority.Senior => 0.8,
        Seniority.TeamLead => 0.5,
        _ => 0.4
    };

    public int AvailableSlots => MaxConcurrency - _activeSessions.Count;
    public bool CanAcceptChat => !IsShiftOver && AvailableSlots > 0;

    public bool AssignChat(Guid sessionId)
    {
        if (!CanAcceptChat) return false;
        _activeSessions.Add(sessionId);
        return true;
    }

    public void RemoveChat(Guid sessionId)
    {
        _activeSessions.Remove(sessionId);
    }
}